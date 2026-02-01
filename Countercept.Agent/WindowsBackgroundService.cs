using App.RabbitMQ;
using App.ETW;

namespace App.WindowsService;

public sealed class WindowsBackgroundService(
    ILogger<WindowsBackgroundService> logger,
    IRabbitManager rabbitManager,
    IEtwEventProducer etwEventProducer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Initialize RabbitMQ connection
            await rabbitManager.ConnectAsync();
            logger.LogInformation("RabbitMQ connection established");

            // Send an example ETW event
            logger.LogInformation("Sending example ETW event...");
            var exampleEvent = etwEventProducer.CreateExampleEvent();
            var serializedEvent = etwEventProducer.SerializeEvent(exampleEvent);
            await rabbitManager.PublishEventAsync(serializedEvent);
            logger.LogInformation("Example ETW event sent successfully");

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Service is stopping due to cancellation request.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);
            Environment.Exit(1);
        }
        finally
        {
            rabbitManager.Dispose();
        }
    }
}