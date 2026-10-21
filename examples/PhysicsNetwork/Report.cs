using System.Text.Json.Serialization;
using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed record ActorReport(ulong ID, uint Generation, int Owner, uint Epoch, float X, float Y, float VX, float VY, float Rotation, float AngularVelocity, bool Sleeping);
internal sealed class SessionReport
{
    public string Role { get; set; } = "";
    public string Backend { get; set; } = "";
    public string Renderer { get; set; } = "";
    public string RequestedWindowVSync { get; set; } = "";
    public string WindowVSync { get; set; } = "";
    public int WindowMaxFPS { get; set; }
    public int WindowFrames { get; set; }
    public long WindowOwnerAllocations { get; set; }
    public long WindowTotalAllocations { get; set; }
    public double WindowFPS { get; set; }
    public double[] WindowFramePercentilesMS { get; set; } = [];
    public int RenderedSmoothedSamples { get; set; }
    public int RenderedInterpolatedSamples { get; set; }
    public bool RenderedPhysicsPreserved { get; set; }
    public bool BackendInstrumented { get; set; }
    public bool SeparateThread { get; set; }
    public int ClientPollDelayMS { get; set; }
    public double ElapsedMS { get; set; }
    public double PreparationMS { get; set; }
    public long PreparationManagedBytes { get; set; }
    public int PreparationFrames { get; set; }
    public double AdmissionMS { get; set; }
    public double ActiveMS { get; set; }
    public int RetiredCommandReplacements { get; set; }
    public bool CheckProgressEnabled { get; set; }
    public uint CheckPhases { get; set; }
    public int CheckWaitPolls { get; set; }
    public int CheckPhaseWaitPolls { get; set; }
    public int CheckInputWaitPolls { get; set; }
    public int CheckHandoffWaitPolls { get; set; }
    public int CheckHandoffPrepared { get; set; }
    public int CheckHandoffApplied { get; set; }
    public double CheckWaitMS { get; set; }
    public int CheckProgressSent { get; set; }
    public int CheckProgressReceived { get; set; }
    public int RejectedCheckProgress { get; set; }
    public long CheckProgressAttemptedBytes { get; set; }
    public int JointSteps { get; set; }
    public int JointKicks { get; set; }
    public float MaxPinError { get; set; }
    public float MaxGrooveError { get; set; }
    public float MaxSpringLength { get; set; }
    public int CrossedContacts { get; set; }
    public int CrossedContactCorrections { get; set; }
    public int GeometrySteps { get; set; }
    public int GeometryCorrections { get; set; }
    public int GeometryKicks { get; set; }
    public uint GeometryContacts { get; set; }
    public uint GeometryImpulses { get; set; }
    public uint GeometrySleeping { get; set; }
    public uint GeometryWakes { get; set; }
    public uint GeometryContactCorrections { get; set; }
    public uint GeometrySleepingCorrections { get; set; }
    public uint GeometryWakeCorrections { get; set; }
    public float MaxGeometryPenetration { get; set; }
    public uint GeometryCompoundContacts { get; set; }
    public uint GeometryCompoundImpulses { get; set; }
    public uint GeometryCompoundContactCorrections { get; set; }
    public int GeometryRayAngularSteps { get; set; }
    public int GeometryRayAngularCorrections { get; set; }
    public float MaxGeometryRayAngularSpeed { get; set; }
    public int SupportSteps { get; set; }
    public int SupportContacts { get; set; }
    public int SupportSleeping { get; set; }
    public int SupportWakes { get; set; }
    public int SupportKicks { get; set; }
    public int SupportImpulseSamples { get; set; }
    public int SupportCorrections { get; set; }
    public int SupportContactCorrections { get; set; }
    public int SupportSleepingCorrections { get; set; }
    public int SupportWakeCorrections { get; set; }
    public float MaxSupportPinError { get; set; }
    public float MaxSupportGrooveError { get; set; }
    public float MaxSupportImpulseError { get; set; }
    public int CompletedSteps { get; set; }
    public int WarmupSteps { get; set; }
    public int NumericalSteps { get; set; }
    public bool NumericalThreadVerified { get; set; }
    public bool GPUUnavailableVerified { get; set; }
    public string GPUDriver { get; set; } = "";
    public string GPUDeviceName { get; set; } = "";
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
    public double AngularDriftCorrection { get; set; }
    public GameEvent[] Events { get; set; } = [];
    public long CommandHistoryBytes { get; set; }
    public long EventHistoryBytes { get; set; }
    public int PredictedEvents { get; set; }
    public int ReplayEvents { get; set; }
    public int MalformedPackets { get; set; }
    public int StalePackets { get; set; }
    public int ReorderedStates { get; set; }
    public int ForeignCommands { get; set; }
    public int EpochCommands { get; set; }
    public int Created { get; set; }
    public int Removed { get; set; }
    public int OwnershipChanges { get; set; }
    public int SleepObservations { get; set; }
    public int WakeObservations { get; set; }
    public int Dropped { get; set; }
    public int Delayed { get; set; }
    public int Duplicated { get; set; }
    public int Reordered { get; set; }
    public int PendingPackets { get; set; }
    public int PeakPendingPackets { get; set; }
    public long PendingBytes { get; set; }
    public long PeakPendingBytes { get; set; }
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
                var pose = actor.Body?.GlobalTransform ?? actor.Joint!.GlobalTransform;
                actors.Add(new(actor.Spec.ID, actor.Spec.Generation, actor.Spec.Owner, actor.Spec.ControlEpoch,
                    pose.Origin.X, pose.Origin.Y, rigid?.LinearVelocity.X ?? 0, rigid?.LinearVelocity.Y ?? 0,
                    pose.Rotation, rigid?.AngularVelocity ?? 0, rigid?.Sleeping ?? false));
            }
        var events = new List<GameEvent>(); var lastEvent = _authority ? Simulation.EventSequence : _confirmedEvent;
        var firstEvent = Math.Max(_baseline + 1, lastEvent >= EventCapacity ? lastEvent - EventCapacity + 1 : 1);
        for (var sequence = firstEvent; sequence <= lastEvent; sequence++) events.Add((_authority ? _events : _confirmedLog)[(int)(sequence % EventCapacity)]);
        var report = new SessionReport
        {
            Role = _authority ? "server" : "client",
            Backend = Simulation.World.PhysicsBackend.ToString(),
            RetiredCommandReplacements = RetiredCommandReplacements,
            ElapsedMS = _clock.Elapsed.TotalMilliseconds,
            PreparationMS = Simulation.PreparationMS,
            PreparationManagedBytes = Simulation.PreparationManagedBytes,
            PreparationFrames = Simulation.PreparationFrames,
            AdmissionMS = _sessionStartedAt * 1000 - Simulation.PreparationMS,
            ActiveMS = (_clock.Elapsed.TotalSeconds - _sessionStartedAt) * 1000,
            CheckProgressEnabled = _checkProgress,
            CheckPhases = CheckPhases,
            CheckWaitPolls = CheckWaitPolls,
            CheckPhaseWaitPolls = CheckPhaseWaitPolls,
            CheckInputWaitPolls = CheckInputWaitPolls,
            CheckHandoffWaitPolls = CheckHandoffWaitPolls,
            CheckHandoffPrepared = CheckHandoffPrepared,
            CheckHandoffApplied = CheckHandoffApplied,
            CheckWaitMS = CheckWaitMS,
            CheckProgressSent = CheckProgressSent,
            CheckProgressReceived = CheckProgressReceived,
            RejectedCheckProgress = RejectedCheckProgress,
            CheckProgressAttemptedBytes = CheckProgressAttemptedBytes,
            JointSteps = Simulation.JointSteps,
            JointKicks = Simulation.JointKicks,
            MaxPinError = Simulation.MaxPinError,
            MaxGrooveError = Simulation.MaxGrooveError,
            MaxSpringLength = Simulation.MaxSpringLength,
            CrossedContacts = Simulation.CrossedContacts,
            CrossedContactCorrections = Simulation.CrossedContactCorrections,
            GeometrySteps = Simulation.GeometrySteps,
            GeometryCorrections = Simulation.GeometryCorrections,
            GeometryKicks = Simulation.GeometryKicks,
            GeometryContacts = Simulation.GeometryContacts,
            GeometryImpulses = Simulation.GeometryImpulses,
            GeometrySleeping = Simulation.GeometrySleeping,
            GeometryWakes = Simulation.GeometryWakes,
            GeometryContactCorrections = Simulation.GeometryContactCorrections,
            GeometrySleepingCorrections = Simulation.GeometrySleepingCorrections,
            GeometryWakeCorrections = Simulation.GeometryWakeCorrections,
            MaxGeometryPenetration = Simulation.MaxGeometryPenetration,
            GeometryCompoundContacts = Simulation.GeometryCompoundContacts,
            GeometryCompoundImpulses = Simulation.GeometryCompoundImpulses,
            GeometryCompoundContactCorrections = Simulation.GeometryCompoundContactCorrections,
            GeometryRayAngularSteps = Simulation.GeometryRayAngularSteps,
            GeometryRayAngularCorrections = Simulation.GeometryRayAngularCorrections,
            MaxGeometryRayAngularSpeed = Simulation.MaxGeometryRayAngularSpeed,
            SupportSteps = Simulation.SupportSteps,
            SupportContacts = Simulation.SupportContacts,
            SupportSleeping = Simulation.SupportSleeping,
            SupportWakes = Simulation.SupportWakes,
            SupportKicks = Simulation.SupportKicks,
            SupportImpulseSamples = Simulation.SupportImpulseSamples,
            SupportCorrections = Simulation.SupportCorrections,
            SupportContactCorrections = Simulation.SupportContactCorrections,
            SupportSleepingCorrections = Simulation.SupportSleepingCorrections,
            SupportWakeCorrections = Simulation.SupportWakeCorrections,
            MaxSupportPinError = Simulation.MaxSupportPinError,
            MaxSupportGrooveError = Simulation.MaxSupportGrooveError,
            MaxSupportImpulseError = Simulation.MaxSupportImpulseError,
            CompletedSteps = CompletedSteps,
            WarmupSteps = WarmupSteps,
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
            ReorderedStates = ReorderedStates,
            ForeignCommands = ForeignCommands,
            EpochCommands = EpochCommands,
            Created = Simulation.Created,
            Removed = Simulation.Removed,
            OwnershipChanges = Simulation.OwnershipChanges,
            SleepObservations = SleepObservations,
            WakeObservations = WakeObservations,
            Dropped = _link.Dropped,
            Delayed = _link.Delayed,
            Duplicated = _link.Duplicated,
            Reordered = _link.Reordered,
            PendingPackets = _link.PendingPackets,
            PeakPendingPackets = _link.PeakPendingPackets,
            PendingBytes = _link.PendingBytes,
            PeakPendingBytes = _link.PeakPendingBytes,
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
            AngularDriftCorrection = AngularDriftCorrection,
            CommandHistoryBytes = _commands.Length * System.Runtime.CompilerServices.Unsafe.SizeOf<InputCommand>(),
            EventHistoryBytes = (_events.Length + _receivedEvents.Length + _confirmedLog.Length) * System.Runtime.CompilerServices.Unsafe.SizeOf<GameEvent>()
        };
        FillBackendReport(report); FillCostReportForTesting(); return report;
    }
    partial void FillBackendReport(SessionReport report);
    internal static double[] Percentiles(double[] values, int count)
    {
        if (count == 0) return [0, 0, 0]; Array.Sort(values, 0, count);
        return [values[(count - 1) / 2], values[(int)((count - 1) * .95)], values[(int)((count - 1) * .99)]];
    }
    partial void ProfileForTesting(int stage, bool begin);
    partial void FillCostReportForTesting();
}
