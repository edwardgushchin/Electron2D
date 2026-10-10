using System.Diagnostics;
using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkSession : IDisposable
{
    internal const int History = 256, EventCapacity = 256;
    private const int Lead = 12;
    private sealed class PeerState(int id, ulong baseline)
    {
        internal readonly int ID = id;
        internal readonly ulong Baseline = baseline;
        internal ulong StateAck, EventAck = baseline;
        internal bool Ready;
    }
    private readonly bool _authority;
    private readonly Guid _token;
    private readonly int _ticks;
    internal readonly NetworkWorld Simulation;
    private readonly SceneMultiplayer _network = new() { MaxPacketBytes = ImpairedLink.PacketBytes + 64, ServerRelay = false };
    private readonly ENetMultiplayerPeer _peer = new();
    private readonly ImpairedLink _link;
    private readonly byte[] _pendingState = new byte[ImpairedLink.PacketBytes];
    private int _pendingStateLength;
    private ulong _pendingSerial, _pendingTick;
    private readonly byte[] _send = new byte[ImpairedLink.PacketBytes], _physicsBytes = new byte[24000];
    private readonly PhysicsSnapshot _snapshot = new(24000);
    private readonly ObjectSpec[] _manifest = new ObjectSpec[NetworkWorld.Capacity];
    private readonly PlayerInput[] _held = new PlayerInput[NetworkWorld.Capacity];
    private readonly ulong[] _appliedTicks = new ulong[NetworkWorld.Capacity];
    private readonly InputCommand[] _commands = new InputCommand[History * NetworkWorld.Capacity];
    private readonly GameEvent[] _events = new GameEvent[EventCapacity], _receivedEvents = new GameEvent[EventCapacity];
    private readonly PeerState?[] _peers = new PeerState?[4];
    private readonly HashSet<int> _connected = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _now, _nextStep, _nextSend, _nextHello;
    private double _finishedAt;
    private bool _started, _finalState, _done, _disposed;
    private int _firstPeer, _secondPeer;
    private ulong _confirmedTick, _confirmedEvent, _baseline, _frameSerial;
    private int _manifestCount;
    internal int Corrections, ReplayedTicks, RejectedCommands, StalePackets, MalformedPackets, PredictedEvents, ReplayEvents, ConfirmedEffects;
    internal int SpawnCorrections, OwnershipCorrections, SleepObservations, WakeObservations;
    private readonly bool[] _wasSleeping = new bool[NetworkWorld.Capacity];
    private readonly uint[] _sleepGenerations = new uint[NetworkWorld.Capacity];
    internal long StepAllocations, ReplayAllocations, WarmReplayAllocations, WarmStepTotalAllocations, WarmReplayTotalAllocations, ReceivedBytes;
    internal int AcceptedCommands, AppliedCommands, CoalescedStates, WarmSteps, WarmReplaySteps, InvalidHello, GenerationCommands;
    internal double MaxCorrectionDistance, MaxVisualOffset, StepMS, CorrectionMS;
    internal readonly double[] StepSamples = new double[4096], CorrectionSamples = new double[4096];
    internal int StepSampleCount, CorrectionSampleCount;
    internal int Port => _peer.Host!.GetLocalPort();
    internal bool Done => _done;
    internal NetworkSession(bool authority, PhysicsServer.Backend backend, int port, Guid token, int ticks, bool impaired)
    {
        _authority = authority; _token = token; _ticks = ticks;
        Simulation = new(backend); Simulation.Observed = Observe;
        _peer.SetBindIP("127.0.0.1");
        if (authority) _peer.CreateServer(port, 4, 1); else _peer.CreateClient("127.0.0.1", port, 1);
        _network.MultiplayerPeer = _peer;
        _network.PeerConnected += id => _connected.Add(id);
        _network.PeerDisconnected += id =>
        {
            _connected.Remove(id); _link!.Forget(id);
            if (FindPeer(id) is { } peer) peer.Ready = false;
            if (!_authority)
            {
                if (_finalState) _done = true;
                else if (!_done) throw new IOException("The authority disconnected before completing the session.");
            }
        };
        _network.PeerPacket += Receive;
        _link = new(_network, impaired);
        if (authority)
        {
            _manifestCount = 7;
            _manifest[0] = new(1, 1, ObjectKind.Floor, 0, 1, new(320, 320));
            _manifest[1] = new(2, 1, ObjectKind.Sensor, 0, 1, new(240, 260));
            _manifest[2] = new(10, 1, ObjectKind.Box, 0, 1, new(80, 285));
            _manifest[3] = new(11, 1, ObjectKind.Ball, 0, 1, new(160, 285));
            _manifest[4] = new(20, 1, ObjectKind.Ball, 0, 1, new(340, 160));
            _manifest[5] = new(21, 1, ObjectKind.Ball, 0, 1, new(375, 180));
            _manifest[6] = new(100, 1, ObjectKind.Pin, 0, 1, new(360, 170), 20, 21);
            Simulation.Reconcile(_manifest.AsSpan(0, _manifestCount));
        }
    }
    internal void Poll()
    {
        _now = _clock.Elapsed.TotalSeconds;
        if (_now > _ticks * NetworkWorld.FixedStep + 30) throw new TimeoutException($"Physics network deadline: authority={_authority}, started={_started}, tick={Simulation.Tick}, corrections={Corrections}, malformed={MalformedPackets}, received={ReceivedBytes}, event={_confirmedEvent}, peer={_peer.GetConnectionStatus()}.");
        _network.Poll();
        if (_pendingStateLength > 0)
        {
            var reader = new WireReader(_pendingState.AsSpan(0, _pendingStateLength)); _pendingStateLength = 0; _ = reader.Header();
            ReceiveState(ref reader);
        }
        _now = _clock.Elapsed.TotalSeconds;
        _link.Flush(_now);
        if (!_authority && !_started && _network.GetUniqueID() > 1 && _peer.GetConnectionStatus() == MultiplayerConnectionStatus.Connected && _now >= _nextHello)
        { SendHello(); _nextHello = _now + .1; }
        if (_started && !_finalState && _now >= _nextStep)
        {
            if (_authority) AuthorityStep(); else PredictStep();
            _nextStep += NetworkWorld.FixedStep;
            if (_nextStep < _now - .25) _nextStep = _now;
        }
        if (_started && _now >= _nextSend)
        {
            if (_authority) BroadcastState(); else SendInputs();
            _nextSend = _now + (_authority ? 4 * NetworkWorld.FixedStep : 2 * NetworkWorld.FixedStep);
        }
        if (_authority && _finalState)
        {
            var all = _firstPeer != 0;
            foreach (var peer in _peers) if (peer is { Ready: true } && (peer.StateAck < Simulation.Tick || peer.EventAck < Simulation.EventSequence)) all = false;
            if (all)
            {
                var writer = new WireWriter(_send); writer.Header(4); writer.U64(Simulation.Tick);
                foreach (var peer in _peers) if (peer is { Ready: true }) _network.SendBytes(_send.AsSpan(0, writer.Count), peer.ID, TransferMode.Reliable);
                _peer.Host!.Flush(); _done = true;
            }
            else if (_now - _finishedAt > 5) throw new TimeoutException("Clients did not acknowledge the final authority state/events.");
        }
        UpdatePresentation();
    }
    private void Receive(int sender, ReadOnlySpan<byte> bytes)
    {
        ReceivedBytes += bytes.Length;
        if (!_connected.Contains(sender)) { RejectedCommands++; return; }
        try
        {
            var reader = new WireReader(bytes); var kind = reader.Header();
            if (_authority)
            {
                if (kind == 1) ReceiveHello(sender, ref reader);
                else if (kind == 2) ReceiveInputs(sender, ref reader);
                else RejectedCommands++;
            }
            else
            {
                if (sender != 1) { RejectedCommands++; return; }
                if (kind == 3)
                {
                    var serial = reader.U64(); var tick = reader.U64();
                    if (_pendingStateLength > 0 && (tick < _pendingTick || tick == _pendingTick && serial <= _pendingSerial)) { StalePackets++; return; }
                    if (_pendingStateLength > 0) CoalescedStates++;
                    WireReader.Require(bytes.Length <= _pendingState.Length); bytes.CopyTo(_pendingState); _pendingStateLength = bytes.Length; _pendingTick = tick; _pendingSerial = serial;
                }
                else if (kind == 4) { var tick = reader.U64(); reader.End(); if (_finalState && tick == _confirmedTick) _done = true; }
                else RejectedCommands++;
            }
        }
        catch (InvalidDataException error) { MalformedPackets++; if (MalformedPackets < 3) Console.Error.WriteLine($"Rejected protocol packet ({bytes.Length} bytes): {error}"); }
    }
    private bool _sentInvalidHello;
    private void SendHello()
    {
        var writer = new WireWriter(_send); writer.Header(1); Span<byte> token = stackalloc byte[16]; _token.TryWriteBytes(token);
        if (!_sentInvalidHello)
        {
            token[0] ^= 1; var invalid = new WireWriter(_send); invalid.Header(1); invalid.Bytes(token);
            _network.SendBytes(_send.AsSpan(0, invalid.Count), 1, TransferMode.Reliable); token[0] ^= 1; _sentInvalidHello = true;
        }
        writer.Bytes(token);
        _link.Send(1, _send.AsSpan(0, writer.Count), _now);
    }
    private PeerState? FindPeer(int id) { foreach (var peer in _peers) if (peer?.ID == id) return peer; return null; }
    private void ReceiveHello(int sender, ref WireReader reader)
    {
        var token = new Guid(reader.Bytes(16)); reader.End();
        if (token != _token) { RejectedCommands++; InvalidHello++; return; }
        if (FindPeer(sender) is { } existing) { SendState(existing); return; }
        var slot = Array.FindIndex(_peers, static peer => peer is null);
        if (slot < 0) { RejectedCommands++; return; }
        var peer = new PeerState(sender, Simulation.EventSequence) { Ready = true }; _peers[slot] = peer;
        if (_firstPeer == 0) { _firstPeer = sender; SetOwner(10, sender); }
        else if (_secondPeer == 0) { _secondPeer = sender; SetOwner(11, sender); }
        if (!_started) { _started = true; _nextStep = _now; }
        SendState(peer);
    }
    private void SetOwner(ulong id, int peer)
    {
        var count = Simulation.Describe(_manifest);
        for (var i = 0; i < count; i++) if (_manifest[i].ID == id) _manifest[i] = _manifest[i] with { Owner = peer, ControlEpoch = _manifest[i].ControlEpoch + 1 };
        Simulation.Reconcile(_manifest.AsSpan(0, count));
    }
    private void AuthorityStep()
    {
        var tick = Simulation.Tick + 1; Simulation.BeginTick(tick, SimulationPhase.Authority);
        if (tick is 90 or 140 or 160) ChangeCrate(tick);
        if (tick == 300)
        {
            var sleeper = (RigidBody)Simulation.Find(30)!.Body!;
            if (!sleeper.Sleeping) throw new InvalidOperationException("The scripted wake requires a previously sleeping crate.");
            sleeper.ApplyCentralImpulse(new(25, -120));
        }
        if (tick == 240 && _secondPeer != 0) { SetOwner(10, _secondPeer); SetOwner(11, _firstPeer); }
        foreach (var actor in Simulation.Actors)
        {
            if (actor is null) continue;
            var slot = CommandSlot(tick, actor.Spec.ID); var command = _commands[slot];
            if (command.Tick == tick && command.ID == actor.Spec.ID && command.Generation == actor.Spec.Generation && command.Epoch == actor.Spec.ControlEpoch) { Simulation.SetInput(command); AppliedCommands++; }
        }
        MeasureStep(SimulationPhase.Authority);
        if (Simulation.Tick >= (ulong)_ticks) { _finalState = true; _finishedAt = _now; }
    }
    private void ChangeCrate(ulong tick)
    {
        var count = Simulation.Describe(_manifest);
        if (tick == 140)
        {
            for (var i = 0; i < count; i++) if (_manifest[i].ID == 30) { _manifest[i] = _manifest[--count]; break; }
        }
        else _manifest[count++] = new(30, tick == 90 ? 1u : 2u, ObjectKind.Box, 0, 1, new(460, 280));
        Simulation.Reconcile(_manifest.AsSpan(0, count));
    }
    // shortcut: controlled IDs 10/11 have distinct slots; use an explicit slot map for arbitrary controlled IDs.
    private static int CommandSlot(ulong tick, ulong id) => (int)(tick % History) * NetworkWorld.Capacity + (int)(id % NetworkWorld.Capacity);
    private void ReceiveInputs(int sender, ref WireReader reader)
    {
        var stateAck = reader.U64(); var eventAck = reader.U64(); var count = reader.Count(32, 32);
        Span<InputCommand> batch = stackalloc InputCommand[count];
        for (var i = 0; i < count; i++) batch[i] = reader.Input(); reader.End();
        var peer = FindPeer(sender);
        if (peer is null || stateAck > Simulation.Tick || eventAck > Simulation.EventSequence) { RejectedCommands++; return; }
        peer.StateAck = Math.Max(peer.StateAck, stateAck); peer.EventAck = Math.Max(peer.EventAck, eventAck);
        foreach (var command in batch)
        {
            var actor = Simulation.Find(command.ID);
            if (actor is not null && actor.Spec.Owner != sender) ForeignCommands++;
            if (actor is not null && actor.Spec.ControlEpoch != command.Epoch) EpochCommands++;
            if (actor is not null && actor.Spec.Generation != command.Generation) GenerationCommands++;
            if (actor is null || actor.Spec.Owner != sender || actor.Spec.Generation != command.Generation || actor.Spec.ControlEpoch != command.Epoch ||
                command.Tick <= Simulation.Tick || command.Tick - Simulation.Tick >= History)
            { RejectedCommands++; continue; }
            var index = CommandSlot(command.Tick, command.ID); var current = _commands[index];
            if (current.Tick == command.Tick && current.ID == command.ID)
            { if (current != command) RejectedCommands++; continue; }
            _commands[index] = command; AcceptedCommands++;
        }
    }
    private void Observe(GameEvent value)
    {
        if (Simulation.Phase == SimulationPhase.Authority)
        {
            foreach (var peer in _peers) if (peer is { Ready: true } && value.Sequence - peer.EventAck >= EventCapacity)
                    throw new InvalidOperationException("Unacknowledged event history exceeded its configured budget.");
            _events[(int)(value.Sequence % EventCapacity)] = value;
        }
        else if (Simulation.Phase == SimulationPhase.Replay) ReplayEvents++;
        else PredictedEvents++;
    }
    private void MeasureStep(SimulationPhase phase)
    {
        var total = GC.GetTotalAllocatedBytes(true); var before = GC.GetAllocatedBytesForCurrentThread(); var started = Stopwatch.GetTimestamp(); Simulation.Step(phase);
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds; var allocated = GC.GetAllocatedBytesForCurrentThread() - before; var allAllocated = GC.GetTotalAllocatedBytes(true) - total;
        if (phase == SimulationPhase.Replay) { ReplayAllocations += allocated; if (Simulation.Tick >= 300) { WarmReplayAllocations += allocated; WarmReplayTotalAllocations += allAllocated; WarmReplaySteps++; } }
        else
        {
            StepMS += elapsed; if (StepSampleCount < StepSamples.Length) StepSamples[StepSampleCount++] = elapsed;
            if (Simulation.Tick >= 300) { StepAllocations += allocated; WarmStepTotalAllocations += allAllocated; WarmSteps++; }
        }
        for (var i = 0; i < Simulation.Actors.Length; i++)
        {
            if (Simulation.Actors[i] is not { Body: RigidBody body } actor) continue;
            var sleeping = body.Sleeping;
            if (sleeping) SleepObservations++;
            if (_sleepGenerations[i] == actor.Spec.Generation && _wasSleeping[i] && !sleeping) WakeObservations++;
            _sleepGenerations[i] = actor.Spec.Generation; _wasSleeping[i] = sleeping;
        }
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; _network.Dispose(); _peer.Dispose(); Simulation.Dispose();
    }
}
