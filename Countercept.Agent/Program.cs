using App.WindowsService;
using App.RabbitMQ;
using App.ETW;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Countercept Agent Service";
});

LoggerProviderOptions.RegisterProviderOptions<
    EventLogSettings, EventLogLoggerProvider>(builder.Services);

// Configure RabbitMQ settings from appsettings.json
builder.Services.Configure<RabbitMQSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

// Register RabbitMQ and ETW services
builder.Services.AddSingleton<IRabbitManager, RabbitManager>();
builder.Services.AddSingleton<IEtwEventProducer, EtwEventProducer>();

// Register the Windows background service
builder.Services.AddHostedService<WindowsBackgroundService>();

IHost host = builder.Build();
host.Run();