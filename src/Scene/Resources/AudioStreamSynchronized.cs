namespace Electron2D;

/// <summary>Combines borrowed audio streams that start together and finish after the last child.</summary>
/// <remarks>Slot indices cover the full fixed capacity independently of StreamCount. Child replacement and
/// changed count stop/rebuild existing playbacks; volume edits remain live. Factories are cold operations;
/// prepared mixing reuses bounded stereo storage. Resource operations serialize composite graph authoring.</remarks>
public sealed class AudioStreamSynchronized : AudioStream
{
    /// <summary>The maximum number of simultaneously configured child slots.</summary>
    public const int MaxStreams = 32;
    private AudioStream?[] _streams = new AudioStream?[MaxStreams];
    private readonly float[] _volumes = new float[MaxStreams], _gains = new float[MaxStreams];
    private readonly List<WeakReference<AudioStreamPlaybackSynchronized>> _playbacks = [];
    private int _count;
    private bool _editing;
    internal bool Editing => _editing;
    /// <summary>Creates an empty synchronized resource with zero dB in every slot.</summary>
    public AudioStreamSynchronized() { Array.Fill(_gains, 1); }
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
    public AudioStream? GetSyncStream(int streamIndex) { lock (GraphGate) { ThrowIfDisposed(); Index(streamIndex); return _streams[streamIndex]; } }
    /// <summary>Replaces a child slot and rebuilds existing stopped playback state.</summary>
    /// <param name="streamIndex">Zero through MaxStreams minus one, independent of StreamCount.</param>
    /// <param name="audioStream">Borrowed resource or null.</param>
    /// <remarks>Equal assignments also rebuild. No Changed notification is raised. New child factories finish
    /// before configuration commits; old child cleanup failures report after the new stopped state commits.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside capacity.</exception>
    /// <exception cref="InvalidOperationException">The assignment creates a composite cycle or reenters editing.</exception>
    /// <exception cref="ObjectDisposedException">This resource or an assigned child is disposed.</exception>
    public void SetSyncStream(int streamIndex, AudioStream? audioStream)
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
    /// <summary>Gets a slot's stored volume in decibels.</summary>
    /// <param name="streamIndex">Zero through MaxStreams minus one.</param>
    /// <returns>Zero dB initially, independently of StreamCount.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside capacity.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public float GetSyncStreamVolume(int streamIndex) { lock (GraphGate) { ThrowIfDisposed(); Index(streamIndex); return _volumes[streamIndex]; } }
    /// <summary>Sets the live volume of one child without restarting playback.</summary>
    /// <param name="streamIndex">Zero through MaxStreams minus one.</param>
    /// <param name="volumeDB">Decibels producing a finite float multiplier; negative infinity mutes and very negative finite values may underflow to silence.</param>
    /// <remarks>Does not raise Changed. Mixing reads the coefficient before each child block; a zero coefficient
    /// still advances the child in sync. Coefficient publication is atomic on 32-bit hosts too.
    /// Successful edits do not allocate.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index or decibel/multiplier value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public void SetSyncStreamVolume(int streamIndex, float volumeDB)
    {
        if (float.IsNaN(volumeDB) || float.IsPositiveInfinity(volumeDB)) throw new ArgumentOutOfRangeException(nameof(volumeDB));
        var gain = (float)Mathf.DBToLinear(volumeDB); if (!float.IsFinite(gain)) throw new ArgumentOutOfRangeException(nameof(volumeDB));
        lock (GraphGate) { ThrowIfDisposed(); Index(streamIndex); _volumes[streamIndex] = volumeDB; AtomicFloatingPoint.Write(ref _gains[streamIndex], gain); }
    }
    internal float Gain(int index) => AtomicFloatingPoint.Read(ref _gains[index]);
    private static void Index(int index) { if ((uint)index >= MaxStreams) throw new ArgumentOutOfRangeException(nameof(index)); }
    private void CheckEdit() { ThrowIfDisposed(); if (_editing) throw new InvalidOperationException("Synchronized factories/cleanup cannot reenter structural authoring."); }
    internal override void AppendChildren(Stack<AudioStream> pending) { foreach (var child in _streams) if (child is not null) pending.Push(child); }
    internal override void AppendPlaybackChildren(Stack<AudioStream> pending) { for (var i = 0; i < _count; i++) if (_streams[i] is { } child) pending.Push(child); }
    private AudioStreamPlayback?[] CreateChildren(AudioStream?[] streams, int count)
    {
        for (var i = 0; i < count; i++) streams[i]?.EnsurePlaybackOwner();
        var children = new AudioStreamPlayback?[MaxStreams];
        try
        {
            for (var i = 0; i < count; i++) if (streams[i] is { } child) { children[i] = child.InstantiatePlayback(); ThrowIfDisposed(); ObjectDisposedException.ThrowIf(child.IsDisposed, child); }
            return children;
        }
        catch (Exception error) { var cleanup = AudioStreamPlaybackSynchronized.ReleaseChildren(children); if (cleanup is not null) ThrowCombined(error, cleanup); throw; }
    }
    // Caller holds the audio gate; factories and owned-child cleanup are cold work outside the graph gate.
    private Exception? Configure(AudioStream?[] streams, int count)
    {
        var plans = new List<(AudioStreamPlaybackSynchronized Target, AudioStreamPlayback?[] Children)>();
        lock (GraphGate)
        {
            CheckEdit();
            for (var i = _playbacks.Count - 1; i >= 0; i--) if (!_playbacks[i].TryGetTarget(out var target) || target.IsDisposed) _playbacks.RemoveAt(i); else { target.EnsureIdle(); target.CheckControlOwner(); plans.Add((target, [])); }
            _editing = true;
        }
        try
        {
            for (var i = 0; i < plans.Count; i++) plans[i] = (plans[i].Target, CreateChildren(streams, count));
            lock (GraphGate) { ThrowIfDisposed(); foreach (var child in streams) ValidateChild(child); _streams = streams; _count = count; }
            for (var i = 0; i < plans.Count; i++)
            {
                var plan = plans[i]; if (!plan.Target.IsDisposed) plans[i] = (plan.Target, plan.Target.ReplaceChildren(plan.Children, count));
            }
            Exception? cleanup = null;
            foreach (var plan in plans) cleanup = Combine(cleanup, AudioStreamPlaybackSynchronized.ReleaseChildren(plan.Children));
            return cleanup;
        }
        catch (Exception error)
        {
            Exception? cleanup = null; foreach (var plan in plans) cleanup = Combine(cleanup, AudioStreamPlaybackSynchronized.ReleaseChildren(plan.Children));
            if (cleanup is not null) ThrowCombined(error, cleanup); throw;
        }
        finally { lock (GraphGate) _editing = false; }
    }
    internal void Unregister(AudioStreamPlaybackSynchronized playback)
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
                    var children = CreateChildren(streams, count);
                    try { lock (GraphGate) { ThrowIfDisposed(); foreach (var child in streams) ValidateChild(child); var playback = new AudioStreamPlaybackSynchronized(this, children, count); _playbacks.Add(new(playback)); return playback; } }
                    catch (Exception error) { var cleanup = AudioStreamPlaybackSynchronized.ReleaseChildren(children); if (cleanup is not null) ThrowCombined(error, cleanup); throw; }
                }
                finally { lock (GraphGate) _editing = false; }
            }
            finally { ExitCall(); }
        }
        finally { server.UnlockCore(); }
    }
    private double NumericMetadata(int kind)
    {
        EnterCall(kind == 0 ? 1 : kind + 2);
        try
        {
            AudioStream?[] streams; int count; lock (GraphGate) { ThrowIfDisposed(); streams = _streams; count = _count; }
            double result = 0;
            for (var i = 0; i < count; i++) if (streams[i] is { } child)
                {
                    var value = kind switch { 0 => child.GetLength(), 1 => child.ReadBPM(), 2 => child.ReadBeatCount(), _ => child.ReadBarBeats() };
                    if (!double.IsFinite(value)) throw new InvalidOperationException("Child audio metadata must be finite.");
                    ThrowIfDisposed(); ObjectDisposedException.ThrowIf(child.IsDisposed, child); if (kind is 1 or 3) { if (value != 0) return value; } else result = Math.Max(result, value);
                }
            return result;
        }
        finally { ExitCall(); }
    }
    /// <inheritdoc />
    protected override double OnGetLength() => NumericMetadata(0);
    /// <inheritdoc />
    protected override double OnGetBPM() => NumericMetadata(1);
    /// <inheritdoc />
    protected override int OnGetBeatCount() => (int)NumericMetadata(2);
    /// <inheritdoc />
    protected override int OnGetBarBeats() => (int)NumericMetadata(3);
    /// <inheritdoc />
    protected override bool OnHasLoop()
    {
        EnterCall(6);
        try { AudioStream?[] streams; int count; lock (GraphGate) { ThrowIfDisposed(); streams = _streams; count = _count; } for (var i = 0; i < count; i++) if (streams[i]?.ReadLoop() == true) { ThrowIfDisposed(); return true; } ThrowIfDisposed(); return false; }
        finally { ExitCall(); }
    }
    /// <inheritdoc />
    protected override string OnGetStreamName() => "Synchronized";
    /// <inheritdoc />
    public override bool IsMetaStream() { ThrowIfDisposed(); return true; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamSynchronized();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var streams = new AudioStream?[MaxStreams]; var volumes = new float[MaxStreams]; int count;
        lock (GraphGate) { ThrowIfDisposed(); count = _count; Array.Copy(_streams, streams, count); Array.Copy(_volumes, volumes, count); }
        for (var i = 0; i < count; i++) streams[i] = (AudioStream?)duplicateSubresource(streams[i]);
        var other = (AudioStreamSynchronized)target; var server = AudioServer.Service; server.LockCore();
        try { var cleanup = other.Configure(streams, count); for (var i = 0; i < MaxStreams; i++) other.SetSyncStreamVolume(i, volumes[i]); if (cleanup is not null) throw cleanup; }
        finally { server.UnlockCore(); }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        var result = new List<PropertyDescriptor>(base.GetPropertyDescriptors()) { new PropertyDescriptor<AudioStreamSynchronized, int>(nameof(StreamCount), p => p.StreamCount, (p, v) => p.StreamCount = v, _ => 0, stored: true) };
        var count = StreamCount;
        for (var i = 0; i < count; i++)
        {
            var index = i;
            result.Add(new PropertyDescriptor<AudioStreamSynchronized, AudioStream?>($"Stream_{index}/Stream", p => p.GetSyncStream(index), (p, v) => p.SetSyncStream(index, v), _ => null, stored: true));
            result.Add(new PropertyDescriptor<AudioStreamSynchronized, float>($"Stream_{index}/Volume", p => p.GetSyncStreamVolume(index), (p, v) => p.SetSyncStreamVolume(index, v), _ => 0, stored: true));
        }
        return result;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (GraphGate) { _streams = new AudioStream?[MaxStreams]; _count = 0; _playbacks.Clear(); } base.Dispose(disposing); }
}
