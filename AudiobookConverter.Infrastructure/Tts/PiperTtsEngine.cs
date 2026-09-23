using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudiobookConverter.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace AudiobookConverter.Infrastructure.Tts
{
    public class PiperTtsEngine : ITextToSpeechEngine
    {
        private readonly string _piperExecutablePath;
        private readonly string _modelPath;
        private readonly ILogger<PiperTtsEngine> _logger;

        public PiperTtsEngine(IConfiguration configuration, ILogger<PiperTtsEngine> logger)
        {
            _logger = logger;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            // No Linux (Docker), o piper fica instalado globalmente em /usr/local/bin/piper.
            // No Windows local, usa o caminho relativo do projeto com piper.exe.
            var defaultPiperPath = OperatingSystem.IsWindows()
                ? Path.Combine(basePath, "TTS", "bin", "piper.exe")
                : "/usr/local/bin/piper";

            var configuredPiperPath = configuration["TTS:PiperPath"] ?? defaultPiperPath;
            var configuredModelPath = configuration["TTS:ModelPath"] ?? Path.Combine(basePath, "TTS", "models", "pt_BR-faber-medium.onnx");

            _piperExecutablePath = Path.GetFullPath(configuredPiperPath);
            _modelPath = Path.GetFullPath(configuredModelPath);

            // Validação preventiva no arranque
            if (!File.Exists(_piperExecutablePath) && OperatingSystem.IsWindows())
            {
                _logger.LogError("Executável do Piper NÃO encontrado em: {Path}", _piperExecutablePath);
            }

            if (!File.Exists(_modelPath))
            {
                _logger.LogError("Modelo ONNX do Piper NÃO encontrado em: {Path}", _modelPath);
            }
        }

        public async Task SynthesizeAudioChunkAsync(string text, string outputWavPath, CancellationToken cancellationToken)
        {
            // Determina o executável do Piper dinamicamente
            string piperExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TTS", "bin", "piper.exe")
                : "/usr/local/bin/piper";

            // Caminho do modelo
            string modelPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TTS", "models", "pt_BR-faber-medium.onnx")
                : "/app/TTS/models/pt_BR-faber-medium.onnx";

            var startInfo = new ProcessStartInfo
            {
                FileName = piperExecutable,
                Arguments = $"--model \"{modelPath}\" --output_file \"{outputWavPath}\"",
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // Capturar StandardError para diagnosticar erros do Piper caso ocorram
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await using (var writer = process.StandardInput)
                {
                    if (writer.BaseStream.CanWrite)
                    {
                        await writer.WriteLineAsync(text.AsMemory(), cancellationToken);
                        await writer.FlushAsync(cancellationToken);
                    }
                }

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    string errorOutput = await stderrTask;
                    throw new Exception($"Piper encerrou com código {process.ExitCode}: {errorOutput}");
                }
            }
            catch (IOException ex)
            {
                string errorOutput = await stderrTask;
                throw new Exception($"Pipe broken ao comunicar com o Piper. Detalhes do StdErr: {errorOutput}", ex);
            }
        }
    }
}