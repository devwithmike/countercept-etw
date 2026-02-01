using Countercept.Shared.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace App.ETW;

public interface IEtwEventProducer
{
    EtwEvent CreateExampleEvent();
    byte[] SerializeEvent(EtwEvent etwEvent);
}

public class EtwEventProducer : IEtwEventProducer
{
    private readonly ILogger<EtwEventProducer> _logger;

    public EtwEventProducer(ILogger<EtwEventProducer> logger)
    {
        _logger = logger;
    }

    public EtwEvent CreateExampleEvent()
    {
        var etwEvent = new EtwEvent
        {
            EventUuid = Guid.NewGuid().ToString(),
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            MachineName = Environment.MachineName,
            ProviderName = "Microsoft-Windows-Kernel-Process",
            EventId = 1, // Process Start
            ProcessId = 1234,
            ThreadId = 5678,
            TaskName = "Process tracking",
            OpcodeName = "Start"
        };

        // Add example payload data
        etwEvent.Payload["CommandLine"] = "C:\\Program Files\\Example\\app.exe --arg1 value1";
        etwEvent.Payload["Image"] = "C:\\Program Files\\Example\\app.exe";
        etwEvent.Payload["User"] = "DOMAIN\\Username";
        etwEvent.Payload["ParentImage"] = "C:\\Windows\\System32\\explorer.exe";
        etwEvent.Payload["ParentProcessId"] = "5678";

        _logger.LogInformation("Created example ETW event: {EventId} from {ProviderName}", 
            etwEvent.EventId, etwEvent.ProviderName);

        return etwEvent;
    }

    public byte[] SerializeEvent(EtwEvent etwEvent)
    {
        try
        {
            return etwEvent.ToByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize ETW event");
            throw;
        }
    }
}
