using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudiobookConverter.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

            // Converte para caminho absoluto e resolve barras
            var configuredPiperPath = configuration["TTS:PiperPath"] ?? Path.Combine(basePath, "TTS", "bin", "piper.exe");
            var configuredModelPath = configuration["TTS:ModelPath"] ?? Path.Combine(basePath, "TTS", "models", "pt_BR-faber-medium.onnx");

            _piperExecutablePath = Path.GetFullPath(configuredPiperPath);
            _modelPath = Path.GetFullPath(configuredModelPath);

            // Validação preventiva no arranque
            if (!File.Exists(_piperExecutablePath))
            {
                _logger.LogError("Executável do Piper NÃO encontrado em: {Path}", _piperExecutablePath);
            }

            if (!File.Exists(_modelPath))
            {
                _logger.LogError("Modelo ONNX do Piper NÃO encontrado em: {Path}", _modelPath);
            }
        }

        public async Task SynthesizeAudioChunkAsync(string text, string outputWavPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("O texto para síntese não pode estar vazio.", nameof(text));

            if (!File.Exists(_piperExecutablePath))
                throw new FileNotFoundException($"Piper não encontrado: {_piperExecutablePath}");

            if (!File.Exists(_modelPath))
                throw new FileNotFoundException($"Modelo não encontrado: {_modelPath}");

            var directory = Path.GetDirectoryName(outputWavPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var startInfo = new ProcessStartInfo
            {
                FileName = _piperExecutablePath,
                Arguments = $"--model \"{_modelPath}\" --output_file \"{outputWavPath}\"",
                WorkingDirectory = Path.GetDirectoryName(_piperExecutablePath),
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardInputEncoding = new UTF8Encoding(false), // sem BOM
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();

                // 1. Começa a ler o stderr JÁ — antes de escrever/fechar o stdin.
                //    ReadToEndAsync() retorna uma Task que roda "em background" via I/O
                //    assíncrono; ela vai drenando o pipe conforme o Piper escreve nele.
                var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

                // 2. Só depois escreve o texto e fecha o stdin (isso sinaliza EOF pro Piper).
                await using (var writer = process.StandardInput)
                {
                    await writer.WriteLineAsync(text.AsMemory(), cancellationToken);
                }

                // 3. Espera o processo terminar...
                await process.WaitForExitAsync(cancellationToken);

                // 4. ...e SÓ ENTÃO pega o conteúdo já acumulado do stderr.
                var errorOutput = await errorTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Erro ao executar Piper TTS. ExitCode: {ExitCode}. Detalhes: {Error}", process.ExitCode, errorOutput);
                    throw new InvalidOperationException($"Falha no Piper TTS (ExitCode {process.ExitCode}): {errorOutput}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção ao sintetizar áudio com Piper.");
                throw;
            }
        }
    }
}