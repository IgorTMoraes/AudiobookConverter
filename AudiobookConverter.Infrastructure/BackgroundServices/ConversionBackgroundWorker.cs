using AudiobookConverter.Application.Jobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using AudiobookConverter.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AudiobookConverter.Infrastructure.BackgroundServices
{
    public class ConversionBackgroundWorker : BackgroundService
    {
        private readonly ConversionQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ConversionBackgroundWorker> _logger;

        public ConversionBackgroundWorker(
            ConversionQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<ConversionBackgroundWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
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

                    using var scope = _scopeFactory.CreateScope();
                    var conversionService = scope.ServiceProvider.GetRequiredService<ConversionService>();
                    await conversionService.ProcessAsync(conversionId, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no processamento da fila de conversão.");
                }
            }
        }
    }
}
