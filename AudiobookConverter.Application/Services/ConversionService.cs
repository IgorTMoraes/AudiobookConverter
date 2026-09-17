using AudiobookConverter.Application.Abstractions;
using AudiobookConverter.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AudiobookConverter.Application.Services
{
    public class ConversionService
    {
        private readonly IConversionRepository _repository;
        private readonly IFileStorage _fileStorage;
        private readonly IEpubReader _epubReader;
        private readonly IChunkGenerator _chunkGenerator;
        private readonly ITextToSpeechEngine _tts;
        private readonly IAudioProcessor _audioProcessor;
        private readonly ILogger<ConversionService> _logger;

        public ConversionService(
            IConversionRepository repository,
            IFileStorage fileStorage,
            IEpubReader epubReader,
            IChunkGenerator chunkGenerator,
            ITextToSpeechEngine tts,
            IAudioProcessor audioProcessor,
            ILogger<ConversionService> logger)
        {
            _repository = repository;
            _fileStorage = fileStorage;
            _epubReader = epubReader;
            _chunkGenerator = chunkGenerator;
            _tts = tts;
            _audioProcessor = audioProcessor;
            _logger = logger;
        }

        public async Task ProcessAsync(Guid conversionId, CancellationToken cancellationToken)
        {
            var conversion = await _repository.GetByIdAsync(conversionId, cancellationToken);
            if (conversion is null)
            {
                _logger.LogWarning("Conversão {Id} não encontrada.", conversionId);
                return;
            }

            var tempDir = _fileStorage.GetTempDirectory(conversionId);
            var wavFiles = new List<string>();

            try
            {
                // 1. Leitura do EPUB
                conversion.UpdateProgress(ConversionStatus.ExtractingText, 10);
                await _repository.UpdateAsync(conversion, cancellationToken);

                await using var epubStream = File.OpenRead(conversion.StoredFilePath);
                var book = await _epubReader.ReadAsync(epubStream, cancellationToken);

                if (book.Chapters.Count == 0)
                    throw new InvalidOperationException("O EPUB não contém capítulos legíveis.");

                // 2. Divisão do texto em blocos
                conversion.UpdateProgress(ConversionStatus.GeneratingAudio, 20);
                await _repository.UpdateAsync(conversion, cancellationToken);

                var allChunks = new List<string>();
                foreach (var chapter in book.Chapters.OrderBy(c => c.Order))
                    allChunks.AddRange(_chunkGenerator.SplitIntoChunks(chapter.Text));

                // 3. Síntese TTS via Piper
                for (int i = 0; i < allChunks.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var wavPath = Path.Combine(tempDir, $"chunk-{i:0000}.wav");
                    await _tts.SynthesizeAudioChunkAsync(allChunks[i], wavPath, cancellationToken);
                    wavFiles.Add(wavPath);

                    var progress = 20 + (int)(60.0 * (i + 1) / allChunks.Count);
                    conversion.UpdateProgress(ConversionStatus.GeneratingAudio, progress);
                    await _repository.UpdateAsync(conversion, cancellationToken);
                }

                // 4. Junção e conversão para MP3 via FFmpeg
                conversion.UpdateProgress(ConversionStatus.MergingAudio, 85);
                await _repository.UpdateAsync(conversion, cancellationToken);

                var outputPath = _fileStorage.GetOutputPath(conversionId);
                await _audioProcessor.MergeWavFilesAsync(wavFiles, outputPath, cancellationToken);

                conversion.OutputFilePath = outputPath;
                conversion.UpdateProgress(ConversionStatus.Completed, 100);
                await _repository.UpdateAsync(conversion, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento da conversão {Id}", conversionId);
                conversion.Fail(ex.Message);
                await _repository.UpdateAsync(conversion, cancellationToken);
            }
            finally
            {
                foreach (var wav in wavFiles)
                {
                    if (File.Exists(wav)) File.Delete(wav);
                }
            }
        }
    }
}
