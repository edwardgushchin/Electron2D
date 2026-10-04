namespace Electron2D;

/// <summary>Configures named music clips and ordered wildcard transition rules for interactive playback.</summary>
/// <remarks>Clips are borrowed. Structural count/stream edits invalidate prepared playbacks; names, advance
/// policies and transitions are read by later queue operations. Count/stream writes and playback controls serialize
/// with mixing; immutable live authoring snapshots use the composite graph gate. Callback authoring reentry rejects.
/// Authored snapshots/copies/factories are cold; prepared switches/mixing reuse storage.</remarks>
public sealed class AudioStreamInteractive : AudioStream
{
    /// <summary>Matches every source or destination clip in a transition rule.</summary>
    public const int ClipAny = -1;
    /// <summary>Chooses the source moment of a requested transition.</summary>
    public enum TransitionFromTime
    {
        /// <summary>Begin without waiting.</summary>
        Immediate = 0,
        /// <summary>Wait for the next beat when tempo exists, otherwise begin immediately.</summary>
        NextBeat = 1,
        /// <summary>Wait for the next bar when tempo/bar metadata exists, otherwise begin immediately.</summary>
        NextBar = 2,
        /// <summary>Wait for the declared musical or sample duration, otherwise begin immediately.</summary>
        End = 3
    }
    /// <summary>Chooses the destination cursor.</summary>
    public enum TransitionToTime
    {
        /// <summary>Use the source cursor plus its wait when the destination has a finite duration.</summary>
        SamePosition = 0,
        /// <summary>Start the destination at zero.</summary>
        Start = 1,
        /// <summary>Resume the destination's remembered mixed cursor when it has a duration.</summary>
        PreviousPosition = 2
    }
    /// <summary>Chooses which clips fade during a transition.</summary>
    public enum FadeMode
    {
        /// <summary>Start at full gain; a looping source receives a one-millisecond outgoing fade.</summary>
        Disabled = 0,
        /// <summary>Fade the destination in; a looping source receives a one-millisecond outgoing fade.</summary>
        In = 1,
        /// <summary>Fade the source out and start the destination at full gain.</summary>
        Out = 2,
        /// <summary>Fade both clips.</summary>
        Cross = 3,
        /// <summary>Use Out for destination Start, otherwise Cross.</summary>
        Automatic = 4
    }
    /// <summary>Chooses automatic clip progression.</summary>
    public enum AutoAdvanceMode
    {
        /// <summary>Stay on this clip until an explicit switch.</summary>
        Disabled = 0,
        /// <summary>Schedule the configured next clip at this clip's declared end.</summary>
        Enabled = 1,
        /// <summary>Schedule the remembered held clip, if any.</summary>
        ReturnToHold = 2
    }
    internal const int Capacity = 63;
    internal readonly record struct Clip(string Name, AudioStream? Stream, AutoAdvanceMode Advance, int Next);
    internal readonly record struct Transition(TransitionFromTime From, TransitionToTime To, FadeMode Fade, float Beats, bool UseFiller, int Filler, bool Hold);
    internal sealed record Configuration(Clip[] Clips, int Count, int Initial, Dictionary<(int From, int To), Transition> Transitions, long Version);
    private Configuration _configuration;
    private int _factories;
    internal Configuration Capture() { ThrowIfDisposed(); return Volatile.Read(ref _configuration); }
    /// <summary>Creates an empty resource with initial index zero.</summary>
    public AudioStreamInteractive() { var clips = new Clip[Capacity]; Array.Fill(clips, new(string.Empty, null, AutoAdvanceMode.Disabled, 0)); _configuration = new(clips, 0, 0, [], 1); }
    private void CheckEdit() { ThrowIfDisposed(); if (_factories != 0 || IsCalling(1)) throw new InvalidOperationException("Interactive factories cannot reenter structural authoring."); }
    internal void BeginFactories() { lock (GraphGate) { CheckEdit(); _factories++; } }
    internal void EndFactories() { lock (GraphGate) _factories--; }
    private static void Index(int index) { if ((uint)index >= Capacity) throw new ArgumentOutOfRangeException(nameof(index)); }
    private void Publish(Configuration value) => Volatile.Write(ref _configuration, value);
    /// <summary>Gets or sets the active clip prefix.</summary>
    /// <value>Zero initially; zero through 63. Hidden clip configuration is retained on this resource.</value>
    /// <remarks>Changed count invalidates playback caches. Every write notifies property and parameter lists.
    /// Shrinking resets out-of-prefix initial/advance/filler targets and removes transition endpoints outside the prefix.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside capacity.</exception>
    /// <exception cref="InvalidOperationException">A factory reenters editing.</exception>
    public int ClipCount
    {
        get => Capture().Count;
        set
        {
            if ((uint)value > Capacity) throw new ArgumentOutOfRangeException(nameof(value));
            var server = AudioServer.Service; server.LockCore();
            try
            {
                lock (GraphGate)
                {
                    CheckEdit(); var old = _configuration; var clips = old.Clips; var transitions = old.Transitions; var initial = old.Initial;
                    if (value < old.Count)
                    {
                        clips = (Clip[])clips.Clone(); for (var i = 0; i < Capacity; i++) if (clips[i].Next >= value) clips[i] = clips[i] with { Next = 0, Advance = AutoAdvanceMode.Disabled };
                        transitions = new(); foreach (var pair in old.Transitions) if (pair.Key.From < value && pair.Key.To < value) transitions.Add(pair.Key, pair.Value.Filler >= value ? pair.Value with { UseFiller = false, Filler = 0 } : pair.Value);
                        if (initial >= value) initial = 0;
                    }
                    Publish(new(clips, value, initial, transitions, old.Version + (value == old.Count ? 0 : 1)));
                }
                NotifyLists(parameters: true);
            }
            finally { server.UnlockCore(); }
        }
    }
    /// <summary>Gets or sets the initial active clip index.</summary>
    /// <value>Zero initially; assigning requires an index in ClipCount.</value>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the active prefix.</exception>
    public int InitialClip
    {
        get => Capture().Initial;
        set { lock (GraphGate) { CheckEdit(); var c = _configuration; if ((uint)value >= (uint)c.Count) throw new ArgumentOutOfRangeException(nameof(value)); Publish(c with { Initial = value }); } }
    }
    private void EditClip(int index, Clip clip, bool structural)
    {
        var c = _configuration; var clips = (Clip[])c.Clips.Clone(); clips[index] = clip; Publish(c with { Clips = clips, Version = c.Version + (structural ? 1 : 0) });
    }
    /// <summary>Sets a clip's literal identifier.</summary>
    /// <param name="clipIndex">Zero through 62, including hidden slots.</param>
    /// <param name="name">Non-null name; duplicates resolve to the first active clip.</param>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside capacity.</exception>
    public void SetClipName(int clipIndex, string name) { ArgumentNullException.ThrowIfNull(name); lock (GraphGate) { CheckEdit(); Index(clipIndex); EditClip(clipIndex, _configuration.Clips[clipIndex] with { Name = name }, false); } }
    /// <summary>Gets a clip identifier or the wildcard label.</summary>
    /// <param name="clipIndex">Minus one for All Clips, otherwise zero through 62.</param>
    /// <returns>The stored name, empty initially, or All Clips.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A slot index or enum selector is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public string GetClipName(int clipIndex) { var c = Capture(); if (clipIndex == ClipAny) return "All Clips"; Index(clipIndex); return c.Clips[clipIndex].Name; }
    /// <summary>Sets a borrowed clip stream and invalidates the prepared version, including equal writes.</summary>
    /// <param name="clipIndex">Zero through 62.</param>
    /// <param name="stream">Borrowed resource or null.</param>
    /// <exception cref="InvalidOperationException">A mixed composite cycle or callback reentry is attempted.</exception>
    /// <exception cref="ObjectDisposedException">This or the assigned stream is disposed.</exception>
    public void SetClipStream(int clipIndex, AudioStream? stream)
    {
        var server = AudioServer.Service; server.LockCore();
        try { lock (GraphGate) { CheckEdit(); Index(clipIndex); ValidateChild(stream); EditClip(clipIndex, _configuration.Clips[clipIndex] with { Stream = stream }, true); } }
        finally { server.UnlockCore(); }
    }
    /// <summary>Gets a borrowed clip stream.</summary>
    /// <param name="clipIndex">Zero through 62.</param>
    /// <returns>Null initially.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A slot index or enum selector is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public AudioStream? GetClipStream(int clipIndex) { var c = Capture(); Index(clipIndex); return c.Clips[clipIndex].Stream; }
    /// <summary>Sets the automatic progression policy for future queues and notifies the property list.</summary>
    /// <param name="clipIndex">Zero through 62.</param>
    /// <param name="mode">A defined auto-advance mode.</param>
    /// <exception cref="ArgumentOutOfRangeException">A slot index or enum selector is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public void SetClipAutoAdvance(int clipIndex, AutoAdvanceMode mode) { if (mode is < AutoAdvanceMode.Disabled or > AutoAdvanceMode.ReturnToHold) throw new ArgumentOutOfRangeException(nameof(mode)); lock (GraphGate) { CheckEdit(); Index(clipIndex); EditClip(clipIndex, _configuration.Clips[clipIndex] with { Advance = mode }, false); } NotifyLists(false); }
    /// <summary>Gets a stored progression policy.</summary>
    /// <param name="clipIndex">Zero through 62.</param>
    /// <returns>Disabled initially.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A slot index or enum selector is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public AutoAdvanceMode GetClipAutoAdvance(int clipIndex) { var c = Capture(); Index(clipIndex); return c.Clips[clipIndex].Advance; }
    /// <summary>Stores a literal next-clip index; invalid or self targets are ignored during playback.</summary>
    /// <param name="clipIndex">Zero through 62.</param>
    /// <param name="autoAdvanceNextClip">Literal signed target, zero initially.</param>
    /// <exception cref="ArgumentOutOfRangeException">A slot index or enum selector is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public void SetClipAutoAdvanceNextClip(int clipIndex, int autoAdvanceNextClip) { lock (GraphGate) { CheckEdit(); Index(clipIndex); EditClip(clipIndex, _configuration.Clips[clipIndex] with { Next = autoAdvanceNextClip }, false); } }
    /// <summary>Gets a literal next-clip target.</summary>
    /// <param name="clipIndex">Zero through 62.</param>
    /// <returns>The stored target, zero initially.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A slot index or enum selector is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public int GetClipAutoAdvanceNextClip(int clipIndex) { var c = Capture(); Index(clipIndex); return c.Clips[clipIndex].Next; }
    /// <summary>Adds or replaces a transition in insertion order.</summary>
    /// <param name="fromClip">Active index or ClipAny.</param>
    /// <param name="toClip">Active index or ClipAny.</param>
    /// <param name="fromTime">Defined source timing mode.</param>
    /// <param name="toTime">Defined destination timing mode.</param>
    /// <param name="fadeMode">Defined fade policy.</param>
    /// <param name="fadeBeats">Finite nonnegative beats, or seconds without source BPM; zero is instant.</param>
    /// <param name="useFillerClip">Whether a valid distinct filler precedes the destination.</param>
    /// <param name="fillerClip">Literal filler index; unusable targets are ignored.</param>
    /// <param name="holdPrevious">Remember the source for a later ReturnToHold.</param>
    /// <exception cref="ArgumentOutOfRangeException">An endpoint, selector or duration is invalid.</exception>
    public void AddTransition(int fromClip, int toClip, TransitionFromTime fromTime, TransitionToTime toTime, FadeMode fadeMode, float fadeBeats, bool useFillerClip = false, int fillerClip = -1, bool holdPrevious = false)
    {
        if (fromTime is < TransitionFromTime.Immediate or > TransitionFromTime.End) throw new ArgumentOutOfRangeException(nameof(fromTime));
        if (toTime is < TransitionToTime.SamePosition or > TransitionToTime.PreviousPosition) throw new ArgumentOutOfRangeException(nameof(toTime));
        if (fadeMode is < FadeMode.Disabled or > FadeMode.Automatic) throw new ArgumentOutOfRangeException(nameof(fadeMode));
        if (!float.IsFinite(fadeBeats) || fadeBeats < 0) throw new ArgumentOutOfRangeException(nameof(fadeBeats));
        lock (GraphGate) { CheckEdit(); var c = _configuration; if (fromClip < ClipAny || fromClip >= c.Count) throw new ArgumentOutOfRangeException(nameof(fromClip)); if (toClip < ClipAny || toClip >= c.Count) throw new ArgumentOutOfRangeException(nameof(toClip)); var table = new Dictionary<(int, int), Transition>(c.Transitions) { [(fromClip, toClip)] = new(fromTime, toTime, fadeMode, fadeBeats, useFillerClip, fillerClip, holdPrevious) }; Publish(c with { Transitions = table }); }
    }
    private Transition Rule(int from, int to) => Capture().Transitions.TryGetValue((from, to), out var rule) ? rule : throw new KeyNotFoundException("The transition is absent.");
    /// <summary>Tests exact authored endpoints, including wildcard values.</summary>
    /// <param name="fromClip">Literal source key.</param>
    /// <param name="toClip">Literal destination key.</param>
    /// <returns>True when that exact rule exists.</returns>
    public bool HasTransition(int fromClip, int toClip) => Capture().Transitions.ContainsKey((fromClip, toClip));
    /// <summary>Erases an exact rule.</summary>
    /// <param name="fromClip">Literal source key.</param>
    /// <param name="toClip">Literal destination key.</param>
    /// <exception cref="KeyNotFoundException">The rule is absent.</exception>
    public void EraseTransition(int fromClip, int toClip) { lock (GraphGate) { CheckEdit(); var c = _configuration; var table = new Dictionary<(int, int), Transition>(c.Transitions); if (!table.Remove((fromClip, toClip))) throw new KeyNotFoundException("The transition is absent."); Publish(c with { Transitions = table }); } }
    /// <summary>Gets copied interleaved source/destination keys in insertion order.</summary>
    /// <returns>Two signed integers per rule.</returns>
    public int[] GetTransitionList() { var c = Capture(); var result = new int[c.Transitions.Count * 2]; var i = 0; foreach (var key in c.Transitions.Keys) { result[i++] = key.From; result[i++] = key.To; } return result; }
    /// <summary>Gets an exact rule's source timing.</summary>
    /// <param name="fromClip">Literal source key.</param><param name="toClip">Literal destination key.</param>
    /// <returns>The authored timing.</returns>
    /// <exception cref="KeyNotFoundException">That exact rule is absent.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public TransitionFromTime GetTransitionFromTime(int fromClip, int toClip) => Rule(fromClip, toClip).From;
    /// <summary>Gets an exact rule's destination timing.</summary>
    /// <param name="fromClip">Literal source key.</param><param name="toClip">Literal destination key.</param>
    /// <returns>The authored timing.</returns>
    /// <exception cref="KeyNotFoundException">That exact rule is absent.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public TransitionToTime GetTransitionToTime(int fromClip, int toClip) => Rule(fromClip, toClip).To;
    /// <summary>Gets an exact rule's fade policy.</summary>
    /// <param name="fromClip">Literal source key.</param><param name="toClip">Literal destination key.</param>
    /// <returns>The authored policy.</returns>
    /// <exception cref="KeyNotFoundException">That exact rule is absent.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public FadeMode GetTransitionFadeMode(int fromClip, int toClip) => Rule(fromClip, toClip).Fade;
    /// <summary>Gets an exact rule's fade duration.</summary>
    /// <param name="fromClip">Literal source key.</param><param name="toClip">Literal destination key.</param>
    /// <returns>Authored beats or seconds without BPM.</returns>
    /// <exception cref="KeyNotFoundException">That exact rule is absent.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public float GetTransitionFadeBeats(int fromClip, int toClip) => Rule(fromClip, toClip).Beats;
    /// <summary>Gets whether an exact rule requests a filler.</summary>
    /// <param name="fromClip">Literal source key.</param><param name="toClip">Literal destination key.</param>
    /// <returns>The authored flag.</returns>
    /// <exception cref="KeyNotFoundException">That exact rule is absent.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public bool IsTransitionUsingFillerClip(int fromClip, int toClip) => Rule(fromClip, toClip).UseFiller;
    /// <summary>Gets an exact rule's literal filler index.</summary>
    /// <param name="fromClip">Literal source key.</param><param name="toClip">Literal destination key.</param>
    /// <returns>The authored index.</returns>
    /// <exception cref="KeyNotFoundException">That exact rule is absent.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public int GetTransitionFillerClip(int fromClip, int toClip) => Rule(fromClip, toClip).Filler;
    /// <summary>Gets whether an exact rule remembers its source.</summary>
    /// <param name="fromClip">Literal source key.</param><param name="toClip">Literal destination key.</param>
    /// <returns>The authored flag.</returns>
    /// <exception cref="KeyNotFoundException">That exact rule is absent.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public bool IsTransitionHoldingPrevious(int fromClip, int toClip) => Rule(fromClip, toClip).Hold;
    private void NotifyLists(bool parameters) { Exception? first = null, second = null; try { NotifyPropertyListChanged(); } catch (Exception error) { first = error; } if (parameters && !IsDisposed) try { NotifyParameterListChanged(); } catch (Exception error) { second = error; } ThrowCombined(first, second); }
    internal override void AppendChildren(Stack<AudioStream> pending) { foreach (var clip in _configuration.Clips) if (clip.Stream is { } child) pending.Push(child); }
    internal override void AppendPlaybackChildren(Stack<AudioStream> pending) { var c = _configuration; for (var i = 0; i < c.Count; i++) if (c.Clips[i].Stream is { } child) pending.Push(child); }
    /// <inheritdoc />
    protected override AudioStreamPlayback OnInstantiatePlayback() { ThrowIfDisposed(); return new AudioStreamPlaybackInteractive(this); }
    /// <inheritdoc />
    protected override string OnGetStreamName() => "Transitioner";
    /// <inheritdoc />
    public override bool IsMetaStream() { ThrowIfDisposed(); return true; }
    /// <inheritdoc />
    protected override PropertyDescriptor[] OnGetParameterList() => [AudioStreamPlaybackInteractive.SwitchToClipParameter];
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamInteractive();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var c = Capture(); var clips = new Clip[Capacity]; Array.Fill(clips, new(string.Empty, null, AutoAdvanceMode.Disabled, 0));
        for (var i = 0; i < c.Count; i++) clips[i] = c.Clips[i] with { Stream = (AudioStream?)duplicateSubresource(c.Clips[i].Stream) };
        var other = (AudioStreamInteractive)target; var server = AudioServer.Service; server.LockCore();
        try { lock (GraphGate) { other.CheckEdit(); foreach (var clip in clips) other.ValidateChild(clip.Stream); other.Publish(new(clips, c.Count, c.Initial, new(c.Transitions), other._configuration.Version + 1)); } }
        finally { server.UnlockCore(); }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        var result = new List<PropertyDescriptor>(base.GetPropertyDescriptors()) { new PropertyDescriptor<AudioStreamInteractive, int>(nameof(ClipCount), p => p.ClipCount, (p, v) => p.ClipCount = v, _ => 0, stored: true), new PropertyDescriptor<AudioStreamInteractive, int>(nameof(InitialClip), p => p.InitialClip, (p, v) => p.InitialClip = v, _ => 0, stored: true) };
        var c = Capture();
        for (var i = 0; i < c.Count; i++) { var index = i; result.Add(new PropertyDescriptor<AudioStreamInteractive, string>($"Clip_{i}/Name", p => p.GetClipName(index), (p, v) => p.SetClipName(index, v), _ => string.Empty, stored: true)); result.Add(new PropertyDescriptor<AudioStreamInteractive, AudioStream?>($"Clip_{i}/Stream", p => p.GetClipStream(index), (p, v) => p.SetClipStream(index, v), _ => null, stored: true)); result.Add(new PropertyDescriptor<AudioStreamInteractive, AutoAdvanceMode>($"Clip_{i}/AutoAdvance", p => p.GetClipAutoAdvance(index), (p, v) => p.SetClipAutoAdvance(index, v), _ => AutoAdvanceMode.Disabled, stored: true)); result.Add(new PropertyDescriptor<AudioStreamInteractive, int>($"Clip_{i}/NextClip", p => p.GetClipAutoAdvanceNextClip(index), (p, v) => p.SetClipAutoAdvanceNextClip(index, v), _ => 0, stored: true)); }
        return result;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (GraphGate) { var c = _configuration; var clips = new Clip[Capacity]; Array.Fill(clips, new(string.Empty, null, AutoAdvanceMode.Disabled, 0)); Publish(new(clips, 0, 0, [], c.Version + 1)); } base.Dispose(disposing); }
}
