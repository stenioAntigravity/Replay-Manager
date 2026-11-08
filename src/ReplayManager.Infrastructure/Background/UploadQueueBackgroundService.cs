using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReplayManager.Application.Contracts.Upload;

namespace ReplayManager.Infrastructure.Background;

/// <summary>
/// Hosted service that continuously runs the upload queue processor in the background.
/// </summary>
public class UploadQueueBackgroundService : BackgroundService
{
    private readonly IUploadQueueProcessor _processor;
    private readonly ILogger<UploadQueueBackgroundService>? _logger;

    public UploadQueueBackgroundService(
        IUploadQueueProcessor processor,
        ILogger<UploadQueueBackgroundService>? logger = null)
    {
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation("Serviço de background da fila de upload iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _processor.ProcessQueueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro no loop de execução da fila de upload em background.");
            }

            // Aguarda 5 segundos antes de re-verificar por novos itens adicionados
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger?.LogInformation("Serviço de background da fila de upload finalizado.");
    }
}
