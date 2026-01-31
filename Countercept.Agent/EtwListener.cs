using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Countercept.Shared.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Countercept.Agent;

public class EtwListener
{
    private readonly string _sessionName = "Countercept-Session";
    private readonly Action<EtwEvent> _onEventCaptured;
    private TraceEventSession? _session;

    public EtwListener(Action<EtwEvent> onEventCaptured)
    {
        _onEventCaptured = onEventCaptured;
    }

    public void Start(CancellationToken token)
    {
        // NOTE: Requires Administrator Privileges
        using (_session = new TraceEventSession(_sessionName))
        {
            _session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);

            _session.Source.Kernel.ProcessStart += (data) =>
            {
                var protoEvent = new EtwEvent
                {
                    EventUuid = Guid.NewGuid().ToString(),
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    MachineName = Environment.MachineName,
                    ProviderName = "Windows-Kernel-Process",
                    EventId = (uint)data.ID,
                    ProcessId = (uint)data.ProcessID,
                    TaskName = data.TaskName,
                    OpcodeName = data.OpcodeName
                };

                protoEvent.Payload.Add("ImageName", data.ImageFileName ?? "Unknown");
                protoEvent.Payload.Add("CommandLine", data.CommandLine ?? "");
                protoEvent.Payload.Add("ParentID", data.ParentID.ToString());

                _onEventCaptured(protoEvent);
            };

            token.Register(() => _session.Stop());

            _session.Source.Process();
        }
    }
}