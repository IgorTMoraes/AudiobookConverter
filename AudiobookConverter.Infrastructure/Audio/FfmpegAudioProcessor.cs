using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudiobookConverter.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AudiobookConverter.Infrastructure.Audio
{
    public class FfmpegAudioProcessor : IAudioProcessor
    {
        private readonly ILogger<FfmpegAudioProcessor> _logger;

        // No Linux (Docker), 'ffmpeg' é instalado globalmente no sistema pelo apt-get.
        // No Windows, usa o arquivo local ffmpeg.exe na pasta TTS/bin/.
        private string FfmpegPath => OperatingSystem.IsWindows()
            ? Path.Combine(AppContext.BaseDirectory, "TTS", "bin", "ffmpeg.exe")
            : "ffmpeg";

        public FfmpegAudioProcessor(ILogger<FfmpegAudioProcessor> logger)
        {
            _logger = logger;
        }

        private void ValidateFfmpegExists()
        {
            if (OperatingSystem.IsWindows() && !File.Exists(FfmpegPath))
            {
                _logger.LogError("FFmpeg não encontrado em: {FfmpegPath}", FfmpegPath);
                throw new FileNotFoundException($"FFmpeg não encontrado em: {FfmpegPath}", FfmpegPath);
            }
        }

        public async Task ConvertWavToMp3Async(
            string inputWavPath,
            string outputMp3Path,
            CancellationToken cancellationToken)
        {
            ValidateFfmpegExists();

            var startInfo = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = $"-i \"{inputWavPath}\" -c:a libmp3lame -q:a 2 \"{outputMp3Path}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            process.Start();

            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var errorOutput = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("Falha ao converter WAV para MP3. Erro: {Error}", errorOutput);
                throw new InvalidOperationException($"Falha no FFmpeg: {errorOutput}");
            }
        }

        public async Task MergeWavFilesAsync(
            IEnumerable<string> wavFiles,
            string outputMp3Path,
            CancellationToken cancellationToken)
        {
            var filesList = wavFiles.ToList();

            if (filesList.Count == 0)
            {
                throw new ArgumentException("A lista de arquivos de áudio não pode estar vazia.", nameof(wavFiles));
            }

            ValidateFfmpegExists();

            var listFilePath = Path.Combine(
                Path.GetTempPath(),
                $"ffmpeg_list_{Guid.NewGuid()}.txt");

            try
            {
                var utf8WithoutBom = new UTF8Encoding(false);
                var content = new StringBuilder();

                foreach (var wavFile in filesList)
                {
                    var formattedPath = wavFile.Replace(@"\", "/");
                    content.AppendLine($"file '{formattedPath}'");
                }

                await File.WriteAllTextAsync(
                    listFilePath,
                    content.ToString(),
                    utf8WithoutBom,
                    cancellationToken);

                var startInfo = new ProcessStartInfo
                {
                    FileName = FfmpegPath,
                    Arguments = $"-f concat -safe 0 -i \"{listFilePath}\" -c:a libmp3lame -q:a 2 \"{outputMp3Path}\" -y",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };

                process.Start();

                var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

                await process.WaitForExitAsync(cancellationToken);

                var errorOutput = await errorTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Falha ao unir arquivos WAV no FFmpeg. Erro: {Error}", errorOutput);
                    throw new InvalidOperationException($"Falha no FFmpeg: {errorOutput}");
                }
            }
            finally
            {
                if (File.Exists(listFilePath))
                {
                    File.Delete(listFilePath);
                }
            }
        }
    }
}