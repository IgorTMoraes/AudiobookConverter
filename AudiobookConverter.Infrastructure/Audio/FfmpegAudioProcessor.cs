using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudiobookConverter.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AudiobookConverter.Infrastructure.Audio
{
    public class FfmpegAudioProcessor : IAudioProcessor
    {
        private readonly string _ffmpegExecutablePath;
        private readonly ILogger<FfmpegAudioProcessor> _logger;

        public FfmpegAudioProcessor(IConfiguration configuration, ILogger<FfmpegAudioProcessor> logger)
        {
            _logger = logger;
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            _ffmpegExecutablePath = configuration["Audio:FfmpegPath"] ?? Path.Combine(basePath, "TTS", "bin", "ffmpeg.exe");
        }

        public async Task ConvertWavToMp3Async(string inputWavPath, string outputMp3Path, CancellationToken cancellationToken = default)
        {
            var arguments = $"-y -i \"{inputWavPath}\" -codec:a libmp3lame -qscale:a 2 \"{outputMp3Path}\"";
            await ExecuteFfmpegAsync(arguments, cancellationToken);
        }

        public async Task MergeWavFilesAsync(IEnumerable<string> inputWavPaths, string outputMp3Path, CancellationToken cancellationToken = default)
        {
            var wavList = inputWavPaths.ToList();
            if (!wavList.Any())
                throw new ArgumentException("Nenhum arquivo WAV fornecido para mesclar.", nameof(inputWavPaths));

            var tempListFile = Path.Combine(Path.GetTempPath(), $"ffmpeg_list_{Guid.NewGuid()}.txt");
            var fileLines = wavList.Select(path => $"file '{path.Replace("\\", "/")}'");
            await File.WriteAllLinesAsync(tempListFile, fileLines, Encoding.UTF8, cancellationToken);

            try
            {
                var arguments = $"-y -f concat -safe 0 -i \"{tempListFile}\" -codec:a libmp3lame -qscale:a 2 \"{outputMp3Path}\"";
                await ExecuteFfmpegAsync(arguments, cancellationToken);
            }
            finally
            {
                if (File.Exists(tempListFile))
                {
                    File.Delete(tempListFile);
                }
            }
        }

        private async Task ExecuteFfmpegAsync(string arguments, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegExecutablePath,
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };

            process.Start();
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var errorOutput = await errorTask;

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Falha no FFmpeg: {errorOutput}");
        }
    }
}