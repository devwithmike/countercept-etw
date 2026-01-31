namespace Countercept.Agent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private RabbitManager? _rabbit;
    private EtwListener? _etwListener;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service starting...");

        var rabbitHost = _configuration["Countercept:RabbitMqHost"] ?? "localhost";

        try
        {
            _rabbit = new RabbitManager(rabbitHost);
            _logger.LogInformation($"Connected to RabbitMQ at {rabbitHost}");
        }
        catch (Exception ex)
        {
            // In a real scenario, you might want a retry loop here
            _logger.LogError(ex, "FATAL: Could not connect to RabbitMQ.");
            throw;
        }

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _etwListener = new EtwListener((protoEvent) =>
        {
            Task.Run(async () =>
            {
                try
                {
                    await _rabbit.PublishEventAsync(protoEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to publish: {ex.Message}");
                }
            });
        });

        await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Starting ETW Trace Session...");
                _etwListener.Start(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETW Listener crashed.");
            }
        }, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service stopping...");

        if (_rabbit != null)
        {
            await _rabbit.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}