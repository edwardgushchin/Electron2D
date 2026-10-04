namespace Electron2D;

/// <summary>Plays a borrowed fixed-capacity stream list in sequential or shuffled order.</summary>
/// <remarks>Slot indices cover the full fixed capacity independently of StreamCount. Child replacement and
/// changed count stop/rebuild existing playbacks; fade, shuffle and loop edits remain live. Factories are cold operations;
/// prepared mixing reuses bounded stereo storage. Resource operations serialize composite graph authoring.</remarks>
public sealed class AudioStreamPlaylist : AudioStream
{
    /// <summary>The maximum number of simultaneously configured child slots.</summary>
    public const int MaxStreams = 64;
    private AudioStream?[] _streams = new AudioStream?[MaxStreams];
    private readonly List<WeakReference<AudioStreamPlaybackPlaylist>> _playbacks = [];
    private int _count;
    private bool _editing;
    internal bool Editing => _editing;
    /// <summary>Creates an empty looping playlist with sequential order and a 0.3-second outgoing fade.</summary>
    public AudioStreamPlaylist() { }
    /// <summary>Gets or sets the prefix of configured slots used for playback and duration/tempo queries.</summary>
    /// <value>Zero initially; zero through MaxStreams. Hidden slot assignments persist on this resource.</value>
    /// <remarks>A changed count rebuilds existing playback ownership and leaves it stopped. Equal count still
    /// notifies PropertyListChanged without rebuilding. Factory failure preserves old configuration/playback.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside zero through MaxStreams.</exception>
    /// <exception cref="InvalidOperationException">A factory/mix/control callback reenters structural editing.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int StreamCount
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _count; } }
        set
        {
            if ((uint)value > MaxStreams) throw new ArgumentOutOfRangeException(nameof(value));
            var server = AudioServer.Service; server.LockCore();
            try
            {
                AudioStream?[] streams; int count; lock (GraphGate) { CheckEdit(); streams = _streams; count = _count; }
                var cleanup = count == value ? null : Configure(streams, value);
                Exception? notification = null; if (!IsDisposed) try { NotifyPropertyListChanged(); } catch (Exception error) { notification = error; }
                ThrowCombined(cleanup, notification);
            }
            finally { server.UnlockCore(); }
        }
    }
    /// <summary>Gets the borrowed resource configured at a slot, including hidden slots.</summary>
    /// <param name="streamIndex">Zero through MaxStreams minus one.</param>
    /// <returns>The borrowed stream, null initially.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside fixed capacity.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public AudioStream? GetListStream(int streamIndex) { lock (GraphGate) { ThrowIfDisposed(); Index(streamIndex); return _streams[streamIndex]; } }
    /// <summary>Replaces a child slot and rebuilds existing stopped playback state.</summary>
    /// <param name="streamIndex">Zero through MaxStreams minus one, independent of StreamCount.</param>
    /// <param name="audioStream">Borrowed resource or null.</param>
    /// <remarks>Equal assignments also rebuild. No Changed notification is raised. New child factories finish
    /// before configuration commits; old child cleanup failures report after the new stopped state commits.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside capacity.</exception>
    /// <exception cref="InvalidOperationException">The assignment creates a composite cycle or reenters editing.</exception>
    /// <exception cref="ObjectDisposedException">This resource or an assigned child is disposed.</exception>
    public void SetListStream(int streamIndex, AudioStream? audioStream)
    {
        var server = AudioServer.Service; server.LockCore();
        try
        {
            AudioStream?[] streams; int count;
            lock (GraphGate) { CheckEdit(); Index(streamIndex); ValidateChild(audioStream); streams = (AudioStream?[])_streams.Clone(); streams[streamIndex] = audioStream; count = _count; }
            var cleanup = Configure(streams, count); if (cleanup is not null) throw cleanup;
        }
        finally { server.UnlockCore(); }
    }
    private double _fade = .3;
    private bool _shuffle, _loop = true;
    /// <summary>Gets or sets the outgoing track fade duration in seconds.</summary>
    /// <value>0.3 initially; finite nonnegative seconds. Zero stops outgoing audio immediately.</value>
    /// <exception cref="ArgumentOutOfRangeException">Time is negative or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public double FadeTime { get { ThrowIfDisposed(); return Volatile.Read(ref _fade); } set { ThrowIfDisposed(); if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); Volatile.Write(ref _fade, value); } }
    /// <summary>Gets or sets whether starts and cycle wraps generate a fresh track permutation.</summary>
    /// <value>False initially. A live edit affects the next order generation.</value>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool Shuffle { get { ThrowIfDisposed(); return Volatile.Read(ref _shuffle); } set { ThrowIfDisposed(); Volatile.Write(ref _shuffle, value); } }
    /// <summary>Gets or sets whether the playlist restarts after its final eligible track.</summary>
    /// <value>True initially; a live edit applies at the next cycle boundary.</value>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public bool Loop { get { ThrowIfDisposed(); return Volatile.Read(ref _loop); } set { ThrowIfDisposed(); Volatile.Write(ref _loop, value); } }
    private static void Index(int index) { if ((uint)index >= MaxStreams) throw new ArgumentOutOfRangeException(nameof(index)); }
    private void CheckEdit() { ThrowIfDisposed(); if (_editing || IsCalling(1) || IsCalling(2) || IsCalling(4)) throw new InvalidOperationException("Playlist factories/cleanup cannot reenter structural authoring."); }
    internal override void AppendChildren(Stack<AudioStream> pending) { foreach (var child in _streams) if (child is not null) pending.Push(child); }
    internal override void AppendPlaybackChildren(Stack<AudioStream> pending) { for (var i = 0; i < _count; i++) if (_streams[i] is { } child) pending.Push(child); }
    private AudioStreamPlayback?[] CreateChildren(AudioStream?[] streams, int count, AudioStreamPlaybackPlaylist owner)
    {
        for (var i = 0; i < count; i++) streams[i]?.EnsurePlaybackOwner();
        var children = new AudioStreamPlayback?[MaxStreams];
        try
        {
            for (var i = 0; i < count; i++) if (streams[i] is { } child)
                {
                    var playback = child.InstantiatePlayback();
                    if (playback.SceneOwner is not null || playback.CompositeOwner is not null || children.Contains(playback)) throw new InvalidOperationException("Playlist factories must return independent caller-owned playback.");
                    children[i] = playback; playback.CompositeOwner = owner; ThrowIfDisposed(); ObjectDisposedException.ThrowIf(child.IsDisposed, child); playback.PrepareQueuedControls();
                }
            return children;
        }
        catch (Exception error) { ThrowCombined(error, AudioStreamPlaybackPlaylist.ReleaseChildren(children)); throw; }
    }
    // Caller holds the audio gate; factories and owned-child cleanup are cold work outside the graph gate.
    private Exception? Configure(AudioStream?[] streams, int count)
    {
        var plans = new List<(AudioStreamPlaybackPlaylist Target, AudioStreamPlayback?[] Children)>();
        lock (GraphGate)
        {
            CheckEdit();
            for (var i = _playbacks.Count - 1; i >= 0; i--) if (!_playbacks[i].TryGetTarget(out var target) || target.IsDisposed) _playbacks.RemoveAt(i); else { target.EnsureIdle(); target.CheckControlOwner(); plans.Add((target, [])); }
            _editing = true;
        }
        try
        {
            for (var i = 0; i < plans.Count; i++) plans[i] = (plans[i].Target, CreateChildren(streams, count, plans[i].Target));
            lock (GraphGate) { ThrowIfDisposed(); foreach (var child in streams) ValidateChild(child); _streams = streams; _count = count; }
            for (var i = 0; i < plans.Count; i++)
            {
                var plan = plans[i]; if (!plan.Target.IsDisposed) plans[i] = (plan.Target, plan.Target.ReplaceChildren(plan.Children, streams, count));
            }
            Exception? cleanup = null;
            foreach (var plan in plans) cleanup = Combine(cleanup, AudioStreamPlaybackPlaylist.ReleaseChildren(plan.Children));
            return cleanup;
        }
        catch (Exception error)
        {
            Exception? cleanup = null; foreach (var plan in plans) cleanup = Combine(cleanup, AudioStreamPlaybackPlaylist.ReleaseChildren(plan.Children));
            ThrowCombined(error, cleanup); throw;
        }
        finally { lock (GraphGate) _editing = false; }
    }
    internal void Unregister(AudioStreamPlaybackPlaylist playback)
    {
        lock (GraphGate) for (var i = _playbacks.Count - 1; i >= 0; i--) if (!_playbacks[i].TryGetTarget(out var target) || ReferenceEquals(target, playback)) _playbacks.RemoveAt(i);
    }
    internal static Exception? Combine(Exception? first, Exception? second) => first is null ? second : second is null ? first : new AggregateException(first, second);
    /// <inheritdoc />
    protected override AudioStreamPlayback OnInstantiatePlayback()
    {
        var server = AudioServer.Service; server.LockCore();
        try
        {
            EnterCall(0);
            try
            {
                AudioStream?[] streams; int count; lock (GraphGate) { CheckEdit(); streams = _streams; count = _count; _editing = true; }
                try
                {
                    var playback = new AudioStreamPlaybackPlaylist(this, new AudioStreamPlayback?[MaxStreams], streams, count);
                    var children = CreateChildren(streams, count, playback);
                    try { lock (GraphGate) { ThrowIfDisposed(); foreach (var child in streams) ValidateChild(child); playback.ReplaceChildren(children, streams, count); _playbacks.Add(new(playback)); return playback; } }
                    catch (Exception error) { ThrowCombined(error, AudioStreamPlaybackPlaylist.ReleaseChildren(children)); throw; }
                }
                finally { lock (GraphGate) _editing = false; }
            }
            finally { ExitCall(); }
        }
        finally { server.UnlockCore(); }
    }
    internal static double Duration(AudioStream stream)
    {
        var bpm = stream.ReadBPM(); var beats = stream.ReadBeatCount();
        var duration = bpm > 0 && beats > 0 ? beats * 60.0 / bpm : stream.GetLength();
        if (!double.IsFinite(bpm) || !double.IsFinite(duration) || duration < 0) throw new InvalidOperationException("Playlist metadata must contain finite nonnegative durations and finite tempo.");
        return duration;
    }
    /// <summary>Gets the first nonzero tempo among the active resource slots.</summary>
    /// <returns>Live BPM metadata, zero if no child supplies it; independent of shuffled playback order.</returns>
    /// <exception cref="InvalidOperationException">A child tempo is nonfinite or metadata recursively calls this resource.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public double GetBPM()
    {
        EnterCall(1);
        try { AudioStream?[] streams; int count; lock (GraphGate) { ThrowIfDisposed(); streams = _streams; count = _count; } for (var i = 0; i < count; i++) if (streams[i] is { } child) { var bpm = child.ReadBPM(); ThrowIfDisposed(); if (!double.IsFinite(bpm)) throw new InvalidOperationException("Child tempo must be finite."); if (bpm != 0) return bpm; } return 0; }
        finally { ExitCall(); }
    }
    /// <inheritdoc />
    protected override double OnGetBPM() => GetBPM();
    /// <inheritdoc />
    protected override double OnGetLength()
    {
        EnterCall(2);
        try { AudioStream?[] streams; int count; lock (GraphGate) { ThrowIfDisposed(); streams = _streams; count = _count; } double length = 0; for (var i = 0; i < count; i++) if (streams[i] is { } child) { length += Duration(child); ThrowIfDisposed(); if (!double.IsFinite(length)) throw new ArithmeticException("Playlist length exceeds finite seconds."); } return length; }
        finally { ExitCall(); }
    }
    /// <inheritdoc />
    protected override bool OnHasLoop() => Loop;
    /// <inheritdoc />
    protected override string OnGetStreamName() => "Playlist";
    /// <inheritdoc />
    public override bool IsMetaStream() { ThrowIfDisposed(); return true; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamPlaylist();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var streams = new AudioStream?[MaxStreams]; int count;
        lock (GraphGate) { ThrowIfDisposed(); count = _count; Array.Copy(_streams, streams, count); }
        for (var i = 0; i < count; i++) streams[i] = (AudioStream?)duplicateSubresource(streams[i]);
        var other = (AudioStreamPlaylist)target; var server = AudioServer.Service; server.LockCore();
        try { var cleanup = other.Configure(streams, count); other.FadeTime = FadeTime; other.Shuffle = Shuffle; other.Loop = Loop; if (cleanup is not null) throw cleanup; }
        finally { server.UnlockCore(); }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        var result = new List<PropertyDescriptor>(base.GetPropertyDescriptors())
        {
            new PropertyDescriptor<AudioStreamPlaylist, int>(nameof(StreamCount), p => p.StreamCount, (p, v) => p.StreamCount = v, _ => 0, stored: true),
            new PropertyDescriptor<AudioStreamPlaylist, double>(nameof(FadeTime), p => p.FadeTime, (p, v) => p.FadeTime = v, _ => .3, stored: true),
            new PropertyDescriptor<AudioStreamPlaylist, bool>(nameof(Shuffle), p => p.Shuffle, (p, v) => p.Shuffle = v, _ => false, stored: true),
            new PropertyDescriptor<AudioStreamPlaylist, bool>(nameof(Loop), p => p.Loop, (p, v) => p.Loop = v, _ => true, stored: true)
        };
        for (var i = 0; i < StreamCount; i++) { var index = i; result.Add(new PropertyDescriptor<AudioStreamPlaylist, AudioStream?>($"Stream_{index}", p => p.GetListStream(index), (p, v) => p.SetListStream(index, v), _ => null, stored: true)); }
        return result;
    }
    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        var server = AudioServer.Service; server.LockCore();
        try { lock (GraphGate) { if (_editing || IsCalling(1) || IsCalling(2) || IsCalling(4)) throw new InvalidOperationException("Playlist callbacks cannot dispose the resource."); foreach (var weak in _playbacks) if (weak.TryGetTarget(out var playback) && !playback.IsDisposed) playback.EnsureIdle(); } }
        finally { server.UnlockCore(); }
        base.ValidateDisposal();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (GraphGate) { _streams = new AudioStream?[MaxStreams]; _count = 0; _playbacks.Clear(); } base.Dispose(disposing); }
}
