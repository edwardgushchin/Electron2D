using System.Diagnostics;
using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkSession
{
    private const int PresentationFrames = 16;
    private readonly record struct RenderPose(ulong ID, uint Generation, Vector2 Position);
    private readonly RenderPose[] _renderHistory = new RenderPose[PresentationFrames * NetworkWorld.Capacity];
    private readonly ulong[] _renderTicks = new ulong[PresentationFrames];
    private readonly int[] _renderCounts = new int[PresentationFrames];
    private readonly Vector2[] _visual = new Vector2[NetworkWorld.Capacity], _oldPredicted = new Vector2[NetworkWorld.Capacity], _offsets = new Vector2[NetworkWorld.Capacity];
    private readonly ObjectSpec[] _oldSpecs = new ObjectSpec[NetworkWorld.Capacity];
    private readonly GameEvent[] _confirmedLog = new GameEvent[EventCapacity];
    private int _nextRender, _renderFrames;
    private double _authorArrival, _lastPresentation;
    internal int InterpolatedSamples, SmoothedSamples, PostDriftCorrections;
    internal double DriftCorrectionDistance;
    internal int ForeignCommands, EpochCommands;
    internal ulong JoinTick;
    private bool _sentForgery, _injectedDrift, _sentStaleGeneration;
    private void BroadcastState()
    {
        foreach (var peer in _peers) if (peer is { Ready: true }) SendState(peer);
    }
    private void SendState(PeerState peer)
    {
        Simulation.Map.Capture(_snapshot); var length = _snapshot.WriteTo(_physicsBytes);
        var writer = new WireWriter(_send); writer.Header(3); writer.U64(++_frameSerial); writer.U64(Simulation.Tick); writer.Bool(_finalState);
        writer.U64(peer.Baseline); writer.U64(Simulation.EventSequence);
        var count = Simulation.Describe(_manifest); writer.I32(count);
        foreach (var spec in _manifest.AsSpan(0, count))
        {
            writer.Spec(spec); var actor = Simulation.Find(spec.ID)!;
            writer.F32(actor.Held.Move); writer.Bool(false); writer.U64(actor.LastInputTick);
        }
        var first = Math.Max(peer.EventAck, peer.Baseline) + 1;
        var events = checked((int)(Simulation.EventSequence + 1 - first)); writer.I32(events);
        for (var sequence = first; sequence <= Simulation.EventSequence; sequence++)
        {
            var item = _events[(int)(sequence % EventCapacity)];
            if (item.Sequence != sequence) throw new InvalidOperationException("Authority event history was overwritten before acknowledgement.");
            writer.Event(item);
        }
        writer.I32(length); writer.Bytes(_physicsBytes.AsSpan(0, length));
        _link.Send(peer.ID, _send.AsSpan(0, writer.Count), _now);
    }
    private void ReceiveState(ref WireReader reader)
    {
        var serial = reader.U64(); var tick = reader.U64(); var final = reader.Bool(); var baseline = reader.U64(); var eventEnd = reader.U64();
        if (_started && (tick < _confirmedTick || tick == _confirmedTick && serial <= _frameSerial)) { StalePackets++; return; }
        WireReader.Require(baseline <= eventEnd && tick <= (ulong)_ticks && (!final || tick == (ulong)_ticks));
        var count = reader.Count(NetworkWorld.Capacity, 64);
        for (var i = 0; i < count; i++)
        {
            _manifest[i] = reader.Spec(); _held[i] = new(reader.F32(), reader.Bool()); _appliedTicks[i] = reader.U64();
            WireReader.Require(Math.Abs(_held[i].Move) <= 1 && _appliedTicks[i] <= tick);
        }
        var events = reader.Count(EventCapacity - 1, 44);
        ulong previous = 0;
        for (var i = 0; i < events; i++)
        {
            var item = reader.Event(); WireReader.Require(item.Sequence > previous && item.Sequence <= eventEnd && item.Tick <= tick);
            previous = item.Sequence; _receivedEvents[i] = item;
        }
        var bytes = reader.Count(_physicsBytes.Length, 1); var payload = reader.Bytes(bytes); reader.End();
        _snapshot.ReadFrom(payload); WireReader.Require(_snapshot.Tick == tick && _snapshot.ObjectCount == count - CountJoints(count));
        if (!_started) { _baseline = baseline; _confirmedEvent = baseline; JoinTick = tick; }
        else WireReader.Require(baseline == _baseline);
        var eventAck = _confirmedEvent;
        for (var i = 0; i < events; i++)
        {
            var item = _receivedEvents[i]; if (item.Sequence <= eventAck) continue;
            WireReader.Require(item.Sequence == eventAck + 1); eventAck = item.Sequence;
        }
        WireReader.Require(eventAck == eventEnd);
        var oldTick = _started ? Simulation.Tick : tick;
        if (oldTick < tick || oldTick - tick >= History) oldTick = tick;
        oldTick = Math.Max(oldTick, Math.Min((ulong)_ticks, tick + Lead));
        if (final) oldTick = tick;
        // Inject at a validated correction boundary so intervening collisions cannot erase the test displacement.
        if (!_injectedDrift && _started && tick >= 190 && Simulation.Find(10)?.Body is RigidBody body)
        { body.Position += new Vector2(35, -15); body.ApplyCentralImpulse(new(40, -20)); _injectedDrift = true; }
        var started = Stopwatch.GetTimestamp();
        for (var i = 0; i < Simulation.Actors.Length; i++)
        { _oldPredicted[i] = Simulation.Actors[i]?.Position ?? default; _oldSpecs[i] = Simulation.Actors[i]?.Spec ?? default; }
        var created = Simulation.Created; var owners = Simulation.OwnershipChanges;
        Simulation.Phase = SimulationPhase.Correction;
        try
        {
            Simulation.Reconcile(_manifest.AsSpan(0, count)); Simulation.Map.Apply(_snapshot);
            Simulation.EventSequence = eventEnd;
            for (var i = 0; i < count; i++)
            {
                var actor = Simulation.Find(_manifest[i].ID)!; actor.Held = _held[i]; actor.LastInputTick = _appliedTicks[i];
            }
            CapturePresentation(count);
            while (Simulation.Tick < oldTick)
            {
                PreparePredictedInputs(Simulation.Tick + 1, replay: true); MeasureStep(SimulationPhase.Replay); ReplayedTicks++;
            }
        }
        catch (Exception error) { throw new InvalidOperationException("Authoritative correction failed; this client must stop.", error); }
        for (var i = 0; i < Simulation.Actors.Length; i++)
            if (Simulation.Actors[i] is { } actor)
            {
                var same = actor.Spec.ID == _oldSpecs[i].ID && actor.Spec.Generation == _oldSpecs[i].Generation;
                var distance = same ? actor.Position.DistanceTo(_oldPredicted[i]) : 0;
                MaxCorrectionDistance = Math.Max(MaxCorrectionDistance, distance);
                if (same && actor.Spec.Owner == _network.GetUniqueID() && _oldSpecs[i].Owner == actor.Spec.Owner)
                    _offsets[i] += _oldPredicted[i] - actor.Position;
                else _offsets[i] = default;
                if (_injectedDrift && actor.Spec.ID == 10) { PostDriftCorrections++; DriftCorrectionDistance = Math.Max(DriftCorrectionDistance, distance); }
            }
        SpawnCorrections += Simulation.Created - created; OwnershipCorrections += Simulation.OwnershipChanges - owners;
        for (var i = 0; i < events; i++)
        {
            var item = _receivedEvents[i]; if (item.Sequence <= _confirmedEvent) continue;
            _confirmedLog[(int)(item.Sequence % EventCapacity)] = item; ConfirmedEffects++; _confirmedEvent = item.Sequence;
        }
        _frameSerial = serial; _confirmedTick = tick; _finalState = final;
        if (!_started) { _started = true; _nextStep = _now + NetworkWorld.FixedStep; }
        Corrections++; var duration = Stopwatch.GetElapsedTime(started).TotalMilliseconds; CorrectionMS += duration;
        if (CorrectionSampleCount < CorrectionSamples.Length) CorrectionSamples[CorrectionSampleCount++] = duration;
    }
    private int CountJoints(int count)
    { var joints = 0; for (var i = 0; i < count; i++) if (_manifest[i].Kind == ObjectKind.Pin) joints++; return joints; }
    private void PredictStep()
    {
        if (Simulation.Tick >= (ulong)_ticks || Simulation.Tick - _confirmedTick >= History - 1) return;
        PreparePredictedInputs(Simulation.Tick + 1, replay: false); MeasureStep(SimulationPhase.Prediction);
    }
    private void PreparePredictedInputs(ulong tick, bool replay)
    {
        var local = _network.GetUniqueID();
        foreach (var actor in Simulation.Actors)
        {
            if (actor?.Body is not RigidBody || actor.Spec.Owner != local) continue;
            var index = CommandSlot(tick, actor.Spec.ID); var saved = _commands[index];
            if (!replay || saved.Tick != tick || saved.ID != actor.Spec.ID || saved.Generation != actor.Spec.Generation || saved.Epoch != actor.Spec.ControlEpoch)
            {
                var move = (tick / 90) % 2 == 0 ? 1f : -1f;
                saved = new(tick, actor.Spec.ID, actor.Spec.Generation, actor.Spec.ControlEpoch, new(move, tick % 72 == 24));
                _commands[index] = saved;
            }
            Simulation.SetInput(saved);
        }
    }
    private void SendInputs()
    {
        var writer = new WireWriter(_send); writer.Header(2); writer.U64(_confirmedTick); writer.U64(_confirmedEvent);
        Span<InputCommand> batch = stackalloc InputCommand[32]; var count = 0;
        var start = Simulation.Tick > 15 ? Simulation.Tick - 15 : 1;
        for (var tick = start; tick <= Simulation.Tick; tick++)
            foreach (var actor in Simulation.Actors)
            {
                if (actor is null || actor.Spec.Owner != _network.GetUniqueID() || count == batch.Length) continue;
                var command = _commands[CommandSlot(tick, actor.Spec.ID)];
                if (command.Tick == tick && command.ID == actor.Spec.ID && command.Generation == actor.Spec.Generation && command.Epoch == actor.Spec.ControlEpoch) batch[count++] = command;
            }
        writer.I32(count); foreach (var command in batch[..count]) writer.Input(command);
        _link.Send(1, _send.AsSpan(0, writer.Count), _now);
        SendStaleGeneration();
        if (!_sentForgery && Simulation.Tick > 60 && Simulation.Find(20) is { } other)
        {
            var forged = new WireWriter(_send); forged.Header(2); forged.U64(_confirmedTick); forged.U64(_confirmedEvent); forged.I32(1);
            forged.Input(new(Simulation.Tick + 1, other.Spec.ID, other.Spec.Generation, other.Spec.ControlEpoch, new(1, true)));
            _network.SendBytes(_send.AsSpan(0, forged.Count), 1, TransferMode.Reliable); _sentForgery = true;
        }
    }
    private void SendStaleGeneration()
    {
        if (_sentStaleGeneration || Simulation.Find(30) is not { Spec.Generation: 2 } actor) return;
        var writer = new WireWriter(_send); writer.Header(2); writer.U64(_confirmedTick); writer.U64(_confirmedEvent); writer.I32(1);
        writer.Input(new(Simulation.Tick + 1, actor.Spec.ID, 1, actor.Spec.ControlEpoch, new(1, false)));
        _network.SendBytes(_send.AsSpan(0, writer.Count), 1, TransferMode.Reliable); _sentStaleGeneration = true;
    }
    private void CapturePresentation(int count)
    {
        var slot = _nextRender; _nextRender = (_nextRender + 1) % PresentationFrames; _renderFrames = Math.Min(PresentationFrames, _renderFrames + 1);
        _renderTicks[slot] = _snapshot.Tick; var written = 0;
        foreach (var spec in _manifest.AsSpan(0, count))
            if (Simulation.Find(spec.ID)?.Body is { } body)
                _renderHistory[slot * NetworkWorld.Capacity + written++] = new(spec.ID, spec.Generation, body.GlobalPosition);
        _renderCounts[slot] = written; _authorArrival = _now;
    }
    private Vector2 Sample(int frame, ObjectSpec spec, Vector2 fallback)
    {
        for (var i = 0; i < _renderCounts[frame]; i++)
        {
            var item = _renderHistory[frame * NetworkWorld.Capacity + i];
            if (item.ID == spec.ID && item.Generation == spec.Generation) return item.Position;
        }
        return fallback;
    }
    private void UpdatePresentation()
    {
        if (_authority || !_started || _renderFrames == 0) return;
        var renderTick = Math.Max(0, _confirmedTick - 8d + Math.Min(_now - _authorArrival, 4 * NetworkWorld.FixedStep) / NetworkWorld.FixedStep);
        var first = -1; var second = -1;
        for (var frame = 0; frame < _renderFrames; frame++)
        {
            var tick = _renderTicks[frame];
            if (tick <= renderTick && (first < 0 || tick > _renderTicks[first])) first = frame;
            if (tick >= renderTick && (second < 0 || tick < _renderTicks[second])) second = frame;
        }
        if (first < 0) first = second; if (second < 0) second = first;
        if (first < 0) return;
        var duration = (double)_renderTicks[second] - _renderTicks[first];
        var alpha = duration > 0 ? (float)Math.Clamp((renderTick - _renderTicks[first]) / duration, 0, 1) : 0;
        var decay = MathF.Exp(-(float)Math.Max(0, _now - _lastPresentation) / .1f); _lastPresentation = _now;
        for (var i = 0; i < Simulation.Actors.Length; i++)
        {
            if (Simulation.Actors[i] is not { Body: not null } actor) continue;
            var solved = actor.Position;
            if (actor.Spec.Owner == _network.GetUniqueID())
            {
                _offsets[i] *= decay; _visual[i] = solved + _offsets[i];
                if (_offsets[i].LengthSquared() > .0001f) SmoothedSamples++;
            }
            else
            {
                _visual[i] = Sample(first, actor.Spec, solved).Lerp(Sample(second, actor.Spec, solved), alpha);
                if (first != second && alpha is > 0 and < 1) InterpolatedSamples++;
            }
            MaxVisualOffset = Math.Max(MaxVisualOffset, _visual[i].DistanceTo(solved));
            if (actor.Position != solved) throw new InvalidOperationException("Presentation changed solved physics state.");
        }
    }
}
