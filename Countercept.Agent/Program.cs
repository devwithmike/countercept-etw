using Countercept.Agent;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Countercept Telemetry Agent";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        // Register your ETW and Rabbit classes here
    })
    .Build();

await host.RunAsync();