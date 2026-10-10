using System.Text.Json.Serialization;
using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed record ActorReport(ulong ID, uint Generation, int Owner, uint Epoch, float X, float Y, float VX, float VY, bool Sleeping);
internal sealed class SessionReport
{
    public string Role { get; set; } = "";
    public string Backend { get; set; } = "";
    public bool BackendInstrumented { get; set; }
    public long GPUUploadBytes { get; set; }
    public long GPUReadbackBytes { get; set; }
    public long GPUSubmissions { get; set; }
    public double GPUWaitMS { get; set; }
    public ulong Tick { get; set; }
    public ulong JoinTick { get; set; }
    public ulong EventSequence { get; set; }
    public ulong EventBaseline { get; set; }
    public int ConfirmedEffects { get; set; }
    public int Corrections { get; set; }
    public int ReplayedTicks { get; set; }
    public int CoalescedStates { get; set; }
    public int AcceptedCommands { get; set; }
    public int GenerationCommands { get; set; }
    public int InvalidHello { get; set; }
    public int WarmSteps { get; set; }
    public int WarmReplaySteps { get; set; }
    public int AppliedCommands { get; set; }
    public long WarmReplayAllocations { get; set; }
    public long WarmStepTotalAllocations { get; set; }
    public long WarmReplayTotalAllocations { get; set; }
    public int InterpolatedSamples { get; set; }
    public int SmoothedSamples { get; set; }
    public int PostDriftCorrections { get; set; }
    public double DriftCorrectionDistance { get; set; }
    public GameEvent[] Events { get; set; } = [];
    public long CommandHistoryBytes { get; set; }
    public long EventHistoryBytes { get; set; }
    public int PredictedEvents { get; set; }
    public int ReplayEvents { get; set; }
    public int MalformedPackets { get; set; }
    public int StalePackets { get; set; }
    public int ForeignCommands { get; set; }
    public int EpochCommands { get; set; }
    public int Created { get; set; }
    public int Removed { get; set; }
    public int OwnershipChanges { get; set; }
    public int SleepObservations { get; set; }
    public int WakeObservations { get; set; }
    public int Dropped { get; set; }
    public int Duplicated { get; set; }
    public int Reordered { get; set; }
    public long SentBytes { get; set; }
    public long ReceivedBytes { get; set; }
    public long WarmStepAllocations { get; set; }
    public long ReplayAllocations { get; set; }
    public double MaxCorrectionDistance { get; set; }
    public double MaxVisualOffset { get; set; }
    public double[] StepPercentilesMS { get; set; } = [];
    public double[] CorrectionPercentilesMS { get; set; } = [];
    public ActorReport[] Actors { get; set; } = [];
}
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SessionReport))]
internal partial class ReportJSON : JsonSerializerContext { }

internal sealed partial class NetworkSession
{
    internal SessionReport Report()
    {
        var actors = new List<ActorReport>();
        foreach (var actor in Simulation.Actors)
            if (actor is not null)
            {
                var rigid = actor.Body as RigidBody;
                actors.Add(new(actor.Spec.ID, actor.Spec.Generation, actor.Spec.Owner, actor.Spec.ControlEpoch,
                    actor.Position.X, actor.Position.Y, rigid?.LinearVelocity.X ?? 0, rigid?.LinearVelocity.Y ?? 0, rigid?.Sleeping ?? false));
            }
        var events = new List<GameEvent>(); var lastEvent = _authority ? Simulation.EventSequence : _confirmedEvent;
        var firstEvent = Math.Max(_baseline + 1, lastEvent >= EventCapacity ? lastEvent - EventCapacity + 1 : 1);
        for (var sequence = firstEvent; sequence <= lastEvent; sequence++) events.Add((_authority ? _events : _confirmedLog)[(int)(sequence % EventCapacity)]);
        var report = new SessionReport
        {
            Role = _authority ? "server" : "client",
            Backend = Simulation.World.PhysicsBackend.ToString(),
            Tick = Simulation.Tick,
            JoinTick = JoinTick,
            EventSequence = _authority ? Simulation.EventSequence : _confirmedEvent,
            EventBaseline = _baseline,
            ConfirmedEffects = ConfirmedEffects,
            Corrections = Corrections,
            ReplayedTicks = ReplayedTicks,
            CoalescedStates = CoalescedStates,
            AcceptedCommands = AcceptedCommands,
            InvalidHello = InvalidHello,
            GenerationCommands = GenerationCommands,
            WarmSteps = WarmSteps,
            WarmReplaySteps = WarmReplaySteps,
            AppliedCommands = AppliedCommands,
            WarmReplayAllocations = WarmReplayAllocations,
            WarmStepTotalAllocations = WarmStepTotalAllocations,
            WarmReplayTotalAllocations = WarmReplayTotalAllocations,
            PredictedEvents = PredictedEvents,
            ReplayEvents = ReplayEvents,
            MalformedPackets = MalformedPackets,
            StalePackets = StalePackets,
            ForeignCommands = ForeignCommands,
            EpochCommands = EpochCommands,
            Created = Simulation.Created,
            Removed = Simulation.Removed,
            OwnershipChanges = Simulation.OwnershipChanges,
            SleepObservations = SleepObservations,
            WakeObservations = WakeObservations,
            Dropped = _link.Dropped,
            Duplicated = _link.Duplicated,
            Reordered = _link.Reordered,
            SentBytes = _link.SentBytes,
            ReceivedBytes = ReceivedBytes,
            WarmStepAllocations = StepAllocations,
            ReplayAllocations = ReplayAllocations,
            MaxCorrectionDistance = MaxCorrectionDistance,
            MaxVisualOffset = MaxVisualOffset,
            StepPercentilesMS = Percentiles(StepSamples, StepSampleCount),
            CorrectionPercentilesMS = Percentiles(CorrectionSamples, CorrectionSampleCount),
            Actors = actors.OrderBy(actor => actor.ID).ToArray(),
            Events = events.ToArray(),
            InterpolatedSamples = InterpolatedSamples,
            SmoothedSamples = SmoothedSamples,
            PostDriftCorrections = PostDriftCorrections,
            DriftCorrectionDistance = DriftCorrectionDistance,
            CommandHistoryBytes = _commands.Length * System.Runtime.CompilerServices.Unsafe.SizeOf<InputCommand>(),
            EventHistoryBytes = (_events.Length + _receivedEvents.Length + _confirmedLog.Length) * System.Runtime.CompilerServices.Unsafe.SizeOf<GameEvent>()
        };
        FillBackendReport(report); return report;
    }
    partial void FillBackendReport(SessionReport report);
    private static double[] Percentiles(double[] values, int count)
    {
        if (count == 0) return [0, 0, 0]; Array.Sort(values, 0, count);
        return [values[(count - 1) / 2], values[(int)((count - 1) * .95)], values[(int)((count - 1) * .99)]];
    }
}
