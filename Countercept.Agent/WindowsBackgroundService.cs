namespace App.WindowsService;

public sealed class WindowsBackgroundService(
    ILogger<WindowsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
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
    }
}