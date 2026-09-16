using AudiobookConverter.Application.Jobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AudiobookConverter.Infrastructure.BackgroundServices
{
    public class ConversionBackgroundWorker : BackgroundService
    {
        private readonly ConversionQueue _queue;
        private readonly ILogger<ConversionBackgroundWorker> _logger;

        public ConversionBackgroundWorker(ConversionQueue queue, ILogger<ConversionBackgroundWorker> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Worker de Conversão iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var conversionId = await _queue.DequeueAsync(stoppingToken);
                    _logger.LogInformation("Processando Job Id: {Id}", conversionId);

                    // No Dia 4 implementaremos a execução completa da pipeline aqui.
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no processamento da fila.");
                }
            }
        }
    }
}
