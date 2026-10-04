namespace Electron2D;

/// <summary>A per-tree state-machine controller with queued commands, route diagnostics and state events.</summary>
/// <remarks>Commands are consumed by the next graph evaluation. The tree owns attached controllers;
/// grouped controllers accept commands only through their nearest root/nested ancestor. State events
/// are synchronous after committed selection or fade retirement. User callbacks may throw.</remarks>
public sealed class AnimationNodeStateMachinePlayback : Resource
{
    private AnimationGraphInstance? _instance;
    private AnimationNodeStateMachine? _machine;
    private AnimationNodeStateMachinePlayback? _testing;
    private long _testSerial = -1, _revision;
    private bool _playing, _stop, _next, _reset = true, _resetTeleport = true, _pendingReset;
    private string _current = "Start", _fading = "", _startRequest = "", _travelRequest = "";
    private AnimationNodeStateMachine.State? _currentState, _fadingState;
    private AnimationGraphTime _currentTime, _fadingTime;
    private double _fadeDuration, _fadePosition;
    private Curve? _curve;
    private readonly List<string> _path = [];
    private readonly List<AnimationNodeStateMachine.State> _pathStates = [];
    private string[] _names = [];
    private readonly Dictionary<string, (string Local, string Tail)> _requests = new(StringComparer.Ordinal);
    private readonly HashSet<string> _validated = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _indices = new(StringComparer.Ordinal);
    private double[] _cost = [];
    private int[] _previous = [];
    private bool[] _closed = [], _chain = [];
    private readonly Dictionary<string, (AnimationNodeStateMachinePlayback Owner, string Name)[]> _signals = new(StringComparer.Ordinal);
    internal AnimationNodeStateMachineTransition? GroupStart, GroupEnd;
    private bool _test;
    /// <summary>Creates an unattached controller; attached instances are supplied through the tree's Playback key.</summary>
    public AnimationNodeStateMachinePlayback() { ResourceLocalToScene = true; }
    /// <summary>Occurs after a state becomes current; grouped leaf names also reach ancestor controllers.</summary>
    public event Action<string>? StateStarted;
    /// <summary>Occurs when an outgoing state finishes fading, or is replaced without a fade.</summary>
    public event Action<string>? StateFinished;
    private void Command()
    {
        ThrowIfDisposed(); _instance?.Context.Tree.EnsureAnimationMutable(); if (_machine?.IsDisposed == true) throw new ObjectDisposedException(nameof(AnimationNodeStateMachine));
        if (_machine?.StateMachineType == AnimationStateMachineType.Grouped) throw new InvalidOperationException("Grouped playback is controlled by an ancestor machine.");
        _revision++; if (_instance?.Context.Tree.IsEvaluating == true) _instance.Context.Tree.CancelGraphPass();
    }
    private void Target(string target)
    {
        ThrowIfDisposed(); _instance?.Context.Tree.EnsureAnimationMutable(); ArgumentException.ThrowIfNullOrWhiteSpace(target);
        if (_machine is null) return;
        if (!target.Contains('/')) { if (!_machine.States.ContainsKey(target)) throw new ArgumentException("Requested state is absent.", nameof(target)); return; }
        if (_validated.Contains(target)) return;
        var names = target.Split('/'); var machine = _machine;
        for (var i = 0; i < names.Length; i++)
        {
            if (!machine.States.TryGetValue(names[i], out var state)) throw new ArgumentException("Requested state is absent.", nameof(target));
            if (i > 0 && names[i] is "Start" or "End") throw new ArgumentException("Group boundaries cannot be requested directly.", nameof(target));
            if (i + 1 < names.Length) machine = state.Node is AnimationNodeStateMachine { StateMachineType: AnimationStateMachineType.Grouped } child ? child : throw new ArgumentException("Slash paths require grouped state-machine children.", nameof(target));
        }
        _validated.Add(target); var slash = target.IndexOf('/'); _requests[target] = (names[0], target[(slash + 1)..]);
    }
    /// <summary>Queues a shortest-cost route, teleporting when no enabled route exists.</summary>
    /// <param name="toNode">An exact local state or grouped descendant path.</param>
    /// <param name="resetOnTeleport">Whether an unreachable destination resets; initially true.</param>
    /// <exception cref="ArgumentException">The target is invalid or absent.</exception>
    /// <exception cref="InvalidOperationException">This is grouped playback or the command is outside the attached scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The controller or machine is disposed.</exception>
    public void Travel(string toNode, bool resetOnTeleport = true) { Target(toNode); Command(); _travelRequest = toNode; _resetTeleport = resetOnTeleport; _stop = false; }
    /// <summary>Queues direct activation, discarding the route and outgoing fade.</summary>
    /// <param name="node">An exact local state or grouped descendant path.</param>
    /// <param name="reset">Whether its timeline starts at zero; initially true.</param>
    /// <exception cref="ArgumentException">The target is invalid or absent.</exception>
    /// <exception cref="InvalidOperationException">This is grouped playback or the command is outside the attached scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The controller or machine is disposed.</exception>
    public void Start(string node, bool reset = true) { Target(node); Command(); StartInternal(node, reset); }
    private void StartInternal(string node, bool reset) { _startRequest = node; _travelRequest = ""; _path.Clear(); _pathStates.Clear(); _reset = reset; _stop = false; }
    /// <summary>Queues immediate activation of the selected route/automatic edge, ending any fade.</summary>
    /// <exception cref="InvalidOperationException">This is grouped playback or the command is outside the attached scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The controller or machine is disposed.</exception>
    public void Next() { Command(); _next = true; }
    /// <summary>Queues playback stop and route/fade cleanup.</summary>
    /// <exception cref="InvalidOperationException">This is grouped playback or the command is outside the attached scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The controller or machine is disposed.</exception>
    public void Stop() { Command(); _stop = true; }
    /// <summary>Tests whether graph evaluation has activated playback; End remains playing.</summary>
    /// <returns>The active flag.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public bool IsPlaying() { ThrowIfDisposed(); return _playing; }
    /// <summary>Returns the current local state, initially Start.</summary>
    /// <returns>The local name, or empty after removal.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public string GetCurrentNode() { ThrowIfDisposed(); return _current; }
    /// <summary>Returns the outgoing local state or empty when no fade remains.</summary>
    /// <returns>The outgoing local name.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public string GetFadingFromNode() { ThrowIfDisposed(); return _fading; }
    /// <summary>Returns an independent snapshot of unvisited route states.</summary>
    /// <returns>The remaining local route names.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public string[] GetTravelPath() { ThrowIfDisposed(); return _path.ToArray(); }
    /// <summary>Returns the current state's last processed timeline position in seconds.</summary>
    /// <returns>The position.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public double GetCurrentPlayPosition() { ThrowIfDisposed(); return _currentTime.Position; }
    /// <summary>Returns the current state's last observed timeline length in seconds.</summary>
    /// <returns>The length.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public double GetCurrentLength() { ThrowIfDisposed(); return _currentTime.Length; }
    /// <summary>Returns the outgoing state's timeline position in seconds.</summary>
    /// <returns>The position, or zero after retirement.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public double GetFadingFromPlayPosition() { ThrowIfDisposed(); return _fadingTime.Position; }
    /// <summary>Returns the outgoing state's timeline length in seconds.</summary>
    /// <returns>The length, or zero after retirement.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public double GetFadingFromLength() { ThrowIfDisposed(); return _fadingTime.Length; }
    /// <summary>Returns the selected crossfade duration in seconds.</summary>
    /// <returns>The duration, retained after completion.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public double GetFadingLength() { ThrowIfDisposed(); return _fadeDuration; }
    /// <summary>Returns elapsed crossfade time in seconds.</summary>
    /// <returns>The elapsed fade clock.</returns>
    /// <exception cref="ObjectDisposedException">The controller is disposed.</exception>
    public double GetFadingPosition() { ThrowIfDisposed(); return _fadePosition; }
    internal void Attach(AnimationGraphInstance instance, bool testing = false)
    {
        _instance = instance; _machine = (AnimationNodeStateMachine)instance.Definition; _test = testing;
        var names = _machine.GetNodeList(); _names = names; _requests.Clear(); _validated.Clear(); _indices.Clear(); for (var i = 0; i < names.Length; i++) _indices.Add(names[i], i);
        _cost = new double[names.Length]; _previous = new int[names.Length]; _closed = new bool[names.Length]; _chain = new bool[names.Length]; _path.EnsureCapacity(names.Length); _pathStates.EnsureCapacity(names.Length);
        _current = Resolve(_currentState, _current); _fading = Resolve(_fadingState, _fading); if (_current.Length == 0) _playing = false;
        for (var i = _path.Count - 1; i >= 0; i--) { var name = Resolve(_pathStates[i], ""); if (name.Length == 0) { _path.Clear(); _pathStates.Clear(); break; } _path[i] = name; }
        _signals.Clear();
        foreach (var name in names)
        {
            var signals = new List<(AnimationNodeStateMachinePlayback, string)>(); var ancestor = instance; var label = name;
            while (ancestor.Definition is AnimationNodeStateMachine { StateMachineType: AnimationStateMachineType.Grouped } && ancestor.Parent?.Definition is AnimationNodeStateMachine)
            { label = ancestor.Name + "/" + label; ancestor = ancestor.Parent; signals.Add((ancestor.Get(AnimationNodeStateMachine.Playback), label)); }
            _signals.Add(name, signals.ToArray());
        }
        if (!testing) { _testing ??= new(); _testing.Attach(instance, true); }
    }
    private string Resolve(AnimationNodeStateMachine.State? state, string fallback)
    { if (state is null) return _machine!.States.ContainsKey(fallback) ? fallback : ""; foreach (var pair in _machine!.States) if (ReferenceEquals(pair.Value, state)) return pair.Key; return ""; }
    internal AnimationNodeStateMachinePlayback Working(bool test)
    {
        if (!test || _test) return this;
        var copy = _testing!; var serial = _instance!.Context.Tree.GraphEvaluationSerial;
        if (copy._testSerial != serial) { copy.CopyFrom(this); copy._testSerial = serial; }
        return copy;
    }
    private void CopyFrom(AnimationNodeStateMachinePlayback source)
    {
        _playing = source._playing; _stop = source._stop; _next = source._next; _reset = source._reset; _resetTeleport = source._resetTeleport; _pendingReset = source._pendingReset;
        _current = source._current; _fading = source._fading; _startRequest = source._startRequest; _travelRequest = source._travelRequest; _currentState = source._currentState; _fadingState = source._fadingState;
        _currentTime = source._currentTime; _fadingTime = source._fadingTime; _fadeDuration = source._fadeDuration; _fadePosition = source._fadePosition; _curve = source._curve; GroupStart = source.GroupStart; GroupEnd = source.GroupEnd;
        _path.Clear(); _path.AddRange(source._path); _pathStates.Clear(); _pathStates.AddRange(source._pathStates);
    }
    private void Signal(string state, bool start)
    {
        if (_test || state.Length == 0) return;
        if (_machine!.States.TryGetValue(state, out var entry) && entry.Node is AnimationNodeStateMachine { StateMachineType: AnimationStateMachineType.Grouped }) return;
        if (state is not "Start" and not "End" && _signals.TryGetValue(state, out var signals)) foreach (var signal in signals) { if (signal.Owner.IsDisposed) continue; if (start) signal.Owner.StateStarted?.Invoke(signal.Name); else signal.Owner.StateFinished?.Invoke(signal.Name); }
        if (!IsDisposed) { if (start) StateStarted?.Invoke(state); else StateFinished?.Invoke(state); }
    }
    private void SetCurrent(string name)
    { _current = name; _currentState = _machine!.States[name]; Signal(name, true); }
    private void ClearFade(bool signal = true)
    { var old = _fading; _fading = ""; _fadingState = null; _fadingTime = default; if (signal) Signal(old, false); }
    private bool Alive(AnimationGraphContext c, long generation, long revision) => c.IsCurrent(generation) && !IsDisposed && _revision == revision;
    private AnimationGraphTime Blend(AnimationGraphContext c, string name, double time, bool seek, bool external, double weight, bool test = false)
    { return c.Tree.BlendChild(c, c.Instance.Children[name], time, seek, external, weight, AnimationNode.FilterAction.Ignore, true, test || _test); }
    private AnimationGraphTime Probe(AnimationGraphContext c, string name, bool reset) => Blend(c, name, 0, reset, false, 0, true);
    private bool Route(string destination)
    {
        _path.Clear(); _pathStates.Clear(); if (_current == destination) return !_machine!.AllowTransitionToSelf;
        if (!_indices.TryGetValue(_current, out var first) || !_indices.TryGetValue(destination, out var last)) return false;
        Array.Fill(_cost, double.PositiveInfinity); Array.Fill(_previous, -1); Array.Clear(_closed); _cost[first] = 0;
        // ponytail: quadratic search over authored states; use a heap if large state graphs require it.
        for (var step = 0; step < _names.Length; step++)
        {
            var next = -1; for (var i = 0; i < _names.Length; i++) if (!_closed[i] && (next < 0 || _cost[i] < _cost[next])) next = i;
            if (next < 0 || double.IsPositiveInfinity(_cost[next])) break; if (next == last) break; _closed[next] = true;
            foreach (var edge in _machine!.Edges)
            {
                if (edge.From != _names[next] || edge.Policy.AdvanceMode == AnimationAdvanceMode.Disabled) continue;
                var to = _indices[edge.To]; if (_closed[to]) continue; var a = _machine.States[edge.From].Position; var b = _machine.States[edge.To].Position;
                var dx = (double)a.X - b.X; var dy = (double)a.Y - b.Y; var cost = _cost[next] + Math.Sqrt(dx * dx + dy * dy) * edge.Policy.Priority;
                if (cost < _cost[to]) { _cost[to] = cost; _previous[to] = next; }
            }
        }
        if (_previous[last] < 0) return false;
        for (var i = last; i != first; i = _previous[i]) { _path.Add(_names[i]); _pathStates.Add(_machine!.States[_names[i]]); }
        _path.Reverse(); _pathStates.Reverse();
        for (var step = -1; step < _path.Count - 1; step++)
        {
            var name = step < 0 ? _current : _path[step]; var child = Child(name, true);
            if (child is not null && !child.Route("End")) { _path.Clear(); _pathStates.Clear(); return false; }
        }
        return true;
    }
    private AnimationNodeStateMachinePlayback? Child(string name, bool test)
    { return _instance!.Children.TryGetValue(name, out var child) && child.Definition is AnimationNodeStateMachine { StateMachineType: AnimationStateMachineType.Grouped } ? child.Get(AnimationNodeStateMachine.Playback).Working(test) : null; }
    private string RequestPath(string request, bool travel, bool reset)
    {
        var slash = request.IndexOf('/'); if (slash >= 0 && !_requests.ContainsKey(request)) _requests.Add(request, (request[..slash], request[(slash + 1)..])); var local = slash < 0 ? request : _requests[request].Local; var child = Child(local, _test);
        if (child is not null) { child.GroupStart = child.GroupEnd = null; foreach (var edge in _machine!.Edges) { if (edge.To == local && child.GroupStart is null) child.GroupStart = edge.Policy; if (edge.From == local && child.GroupEnd is null) child.GroupEnd = edge.Policy; } }
        if (slash >= 0)
        { if (child is null) throw new ArgumentException("State path requires a grouped child."); var rest = _requests[request].Tail; if (travel && local == _current) { child._travelRequest = rest; child._resetTeleport = reset; } else child.StartInternal(rest, reset); }
        else if (child is not null && local != _current) child.StartInternal("Start", reset);
        return local;
    }
    private void StopChildren()
    { var instance = _instance; if (instance is null) return; foreach (var child in instance.Children.Values) if (child.Definition is AnimationNodeStateMachine) { var playback = child.Get(AnimationNodeStateMachine.Playback); if (!playback.IsDisposed) playback.Working(_test).StopInternal(); } }
    private void StopInternal() { _playing = false; _startRequest = _travelRequest = ""; _path.Clear(); _pathStates.Clear(); ClearFade(); StopChildren(); }
    internal AnimationGraphTime Process(AnimationGraphContext c)
    {
        var playback = Working(c.TestOnly); if (!ReferenceEquals(playback, this)) return playback.Process(c);
        ThrowIfDisposed(); var generation = c.Tree.GraphGeneration; var revision = _revision;
        var machine = _machine!;
        if (machine.StateMachineType == AnimationStateMachineType.Grouped && c.Instance.Parent?.Definition is not AnimationNodeStateMachine) throw new InvalidOperationException("Grouped machines require a direct state-machine parent.");
        var start = _startRequest; var travel = _travelRequest; var stop = _stop; var force = _next;
        _startRequest = _travelRequest = ""; _stop = _next = false;
        if (stop) { StopInternal(); if (!c.TestOnly) c.Tree.GraphEmptyOutput(); return default; }
        var teleport = false;
        if (c.Seek && !c.External && Math.Abs(c.Time) < 1e-5)
        { if (machine.StateMachineType != AnimationStateMachineType.Nested || !_playing || _current == "End") { if (start.Length == 0) { start = "Start"; _reset = true; } } else _pendingReset = true; }
        if (start.Length != 0)
        {
            var local = RequestPath(start, false, _reset); _ = machine.States[local]; ClearFade(); if (!Alive(c, generation, revision)) return default; var old = _current; _path.Clear(); _pathStates.Clear(); _playing = true; if (old != local && _currentState is not null) Signal(old, false); if (!Alive(c, generation, revision)) return default; SetCurrent(local); _pendingReset = _reset; teleport = true;
        }
        if (travel.Length != 0)
        {
            if (!_playing) { _playing = true; SetCurrent("Start"); }
            var local = RequestPath(travel, true, _resetTeleport); _ = machine.States[local];
            if (!Route(local)) { ClearFade(); if (!Alive(c, generation, revision)) return default; var old = _current; if (old != local) Signal(old, false); if (!Alive(c, generation, revision)) return default; SetCurrent(local); _pendingReset = _resetTeleport; teleport = true; }
            if (Child(_current, _test) is { } leaving && _path.Count > 0) { leaving._travelRequest = "End"; leaving._resetTeleport = false; }
        }
        if (!_playing || _current.Length == 0) { if (!c.TestOnly) c.Tree.GraphEmptyOutput(); return default; }
        if (!Alive(c, generation, revision)) return default;
        if (teleport) { _currentTime = Probe(c, _current, _pendingReset); TransitionChain(c, ref force, generation, revision); }
        if (!Alive(c, generation, revision)) return default;
        if (force && Child(_current, _test) is { } grouped) { grouped._next = true; force = false; ClearFade(); }
        var weight = 1d;
        if (_fading.Length != 0 && _fadeDuration > 0) { if (!c.Seek) _fadePosition += Math.Abs(c.Delta); weight = Math.Min(1, _fadePosition / _fadeDuration); if (_curve is not null) weight = _curve.Sample((float)weight); Animation.Finite(weight); }
        weight = Math.Abs(weight) < 1e-5 ? 1e-5 : weight;
        var preserveBoundary = !machine.ResetEnds || machine.StateMachineType == AnimationStateMachineType.Grouped;
        if (preserveBoundary && _fading == "Start") weight = 1;
        if (preserveBoundary && _current == "End") weight = 0;
        _currentTime = Blend(c, _current, _pendingReset ? 0 : c.Time, c.Seek || _pendingReset, c.External && !_pendingReset, weight); _pendingReset = false;
        if (!Alive(c, generation, revision)) return default;
        var sampled = _currentTime;
        if (_fading.Length != 0)
        {
            var inverse = Math.Abs(1 - weight) < 1e-5 ? 1e-5 : 1 - weight;
            if (preserveBoundary && _fading == "Start") inverse = 0; else if (preserveBoundary && _current == "End") inverse = 1;
            _fadingTime = Blend(c, _fading, c.Time, c.Seek, c.External, inverse);
            if (!Alive(c, generation, revision)) return default;
            if (_fadePosition >= _fadeDuration - 1e-5) ClearFade();
        }
        if (!Alive(c, generation, revision)) return default;
        TransitionChain(c, ref force, generation, revision);
        if (!Alive(c, generation, revision)) return default;
        var ended = _current == "End" && _fading.Length == 0;
        var terminal = machine.StateMachineType == AnimationStateMachineType.Nested; if (terminal) foreach (var edge in machine.Edges) if (edge.From == _current) { terminal = false; break; }
        if (_current == "End" && !c.TestOnly) c.Tree.GraphEmptyOutput();
        return ended ? default : terminal ? sampled : new(1e20, sampled.Position, sampled.Delta, SpriteFrames.LoopMode.Linear);
    }
    private (AnimationNodeStateMachineTransition Policy, AnimationGraphInstance Owner, bool Bypass) Effective(AnimationNodeStateMachine.Edge edge, AnimationGraphContext c)
    {
        var policy = edge.Policy; var owner = c.Instance; var bypass = false;
        if (_machine!.StateMachineType == AnimationStateMachineType.Grouped && c.Instance.Parent?.Definition is AnimationNodeStateMachine)
        {
            var parent = c.Instance.Parent.Get(AnimationNodeStateMachine.Playback).Working(_test);
            if (edge.From == "Start" && parent.GroupStart is not null) { policy = parent.GroupStart; owner = c.Instance.Parent; bypass = true; }
            else if (edge.To == "End" && parent.GroupEnd is not null) { policy = parent.GroupEnd; owner = c.Instance.Parent; }
        }
        ObjectDisposedException.ThrowIf(policy.IsDisposed, policy); return (policy, owner, bypass);
    }
    private AnimationNodeStateMachine.Edge? FindNext(AnimationGraphContext c)
    {
        AnimationNodeStateMachine.Edge? best = null; var priority = int.MaxValue;
        foreach (var edge in _machine!.Edges)
        {
            if (edge.From != _current) continue;
            var (policy, owner, bypass) = Effective(edge, c); if (policy.AdvanceMode == AnimationAdvanceMode.Disabled) continue;
            if (_path.Count > 0) { if (edge.To == _path[0]) return edge; continue; }
            if (!bypass)
            {
                if (policy.AdvanceMode != AnimationAdvanceMode.Auto) continue;
                if (policy.AdvanceCondition is { } key && !owner.Get(key)) continue;
                if (policy.AdvanceExpression is { } expression) { var generation = c.Tree.GraphGeneration; var passed = expression(c.Tree); if (!c.IsCurrent(generation) || IsDisposed || _machine.IsDisposed) return null; if (!passed) continue; }
            }
            if (policy.Priority <= priority) { priority = policy.Priority; best = edge; }
        }
        return best;
    }
    private void TransitionChain(AnimationGraphContext c, ref bool force, long generation, long revision)
    {
        Array.Clear(_chain); if (_indices.TryGetValue(_current, out var initial)) _chain[initial] = true;
        for (var pass = 0; pass < _names.Length; pass++)
        {
            var edge = FindNext(c); if (!Alive(c, generation, revision) || edge is null) return;
            var (policy, _, _) = Effective(edge, c);
            if (Child(_current, _test) is { } group)
            {
                if (force) { group._next = true; force = false; ClearFade(); }
                if (group._current != "End" || group._fading.Length != 0) return;
            }
            var forced = force; if (force) { ClearFade(); force = false; }
            if (_fading.Length != 0) return;
            if (!forced && _current != "Start" && policy.SwitchMode == AnimationSwitchMode.AtEnd && _currentTime.GetRemaining(policy.BreakLoopAtEnd) > policy.XFadeTime + 1e-5) return;
            var index = _indices[edge.To]; if (_chain[index]) return; _chain[index] = true;
            var old = _current; var oldTime = _currentTime;
            _fadeDuration = policy.XFadeTime; _fadePosition = 0; _curve = policy.XFadeCurve;
            if (_fadeDuration > 0) { _fading = old; _fadingState = _currentState; _fadingTime = oldTime; }
            else Signal(old, false);
            if (!Alive(c, generation, revision)) return;
            if (_path.Count > 0) { _path.RemoveAt(0); _pathStates.RemoveAt(0); }
            var child = Child(edge.To, _test);
            if (child is not null)
            {
                child.GroupStart = policy; child.GroupEnd = null;
                foreach (var outgoing in _machine!.Edges) if (outgoing.From == edge.To) { child.GroupEnd = outgoing.Policy; break; }
                if (child._startRequest.Length == 0 && child._travelRequest.Length == 0) child.StartInternal("Start", policy.Reset);
                if (_path.Count > 0) { child._travelRequest = "End"; child._resetTeleport = false; }
            }
            SetCurrent(edge.To); _pendingReset = policy.Reset;
            if (!Alive(c, generation, revision)) return;
            if (policy.SwitchMode == AnimationSwitchMode.Sync) { Blend(c, _current, oldTime.Position, true, false, 0); _pendingReset = false; }
            _currentTime = Probe(c, _current, _pendingReset);
            if (_fadeDuration > 0 || _current == "End") return;
        }
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeStateMachinePlayback();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (AnimationNodeStateMachinePlayback)target; copy._path.EnsureCapacity(_path.Count); copy._pathStates.EnsureCapacity(_pathStates.Count); copy.CopyFrom(this); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _testing?.Dispose(); _testing = null; _instance = null; _machine = null; _path.Clear(); _pathStates.Clear(); _indices.Clear(); _signals.Clear(); _requests.Clear(); _validated.Clear(); _currentState = _fadingState = null; _curve = null; GroupStart = GroupEnd = null; _names = []; _cost = []; _previous = []; _closed = _chain = []; StateStarted = StateFinished = null; }
        base.Dispose(disposing);
    }
}
