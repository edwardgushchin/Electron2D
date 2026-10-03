namespace Electron2D;

/// <summary>Selects borrowed audio streams with weighted or sequential selection and per-start pitch/volume variation.</summary>
/// <remarks>Selection occurs at InstantiatePlayback, with shared history per resource. Start retains that choice
/// and samples variation again. Pool edits do not retarget captured playback. Mix uses the child PCM path.
/// Authoring and property discovery may allocate; warmed successful mixing does not.</remarks>
public sealed class AudioStreamRandomizer : AudioStream
{
    /// <summary>Selects the next borrowed stream.</summary>
    public enum PlaybackMode
    {
        /// <summary>Use positive weights and avoid the preceding stream identity when another is eligible.</summary>
        RandomNoRepeats = 0,
        /// <summary>Use positive weights and allow repeated identities.</summary>
        Random = 1,
        /// <summary>Traverse distinct non-null stream identities in pool order, ignoring weights.</summary>
        Sequential = 2
    }

    private readonly record struct Entry(AudioStream? Stream, float Weight);
    private Entry[] _entries = [];
    private AudioStream? _last;
    private PlaybackMode _mode;
    private float _pitch = 1, _volume;

    /// <summary>Creates an empty pool, no-repeat selection and disabled pitch/volume variation.</summary>
    public AudioStreamRandomizer() { }

    /// <summary>Gets or sets the stream selection policy.</summary>
    /// <value>RandomNoRepeats initially; history persists when the mode changes.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public PlaybackMode Mode
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _mode; } }
        set { if (value is < PlaybackMode.RandomNoRepeats or > PlaybackMode.Sequential) throw new ArgumentOutOfRangeException(nameof(value)); lock (GraphGate) { ThrowIfDisposed(); _mode = value; } }
    }
    /// <summary>Gets or sets the pool length, retaining existing entries before the new end.</summary>
    /// <value>Zero initially. New slots contain null streams and weight one. Resizing is silent.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int StreamsCount
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _entries.Length; } }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            lock (GraphGate)
            {
                ThrowIfDisposed();
                var old = _entries.Length; var copy = new Entry[value]; _entries.AsSpan(0, Math.Min(old, value)).CopyTo(copy);
                for (var i = old; i < value; i++) copy[i] = new(null, 1); _entries = copy;
            }
        }
    }
    /// <summary>Gets or sets the largest random frequency multiplier.</summary>
    /// <value>One initially; assignments below one clamp to one. Pitch is uniform in logarithmic space between its reciprocal and itself.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float RandomPitch
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _pitch; } }
        set { Finite(value); lock (GraphGate) { ThrowIfDisposed(); _pitch = Math.Max(1, value); } }
    }
    /// <summary>Gets or sets the equivalent semitone variation.</summary>
    /// <value>Zero initially. Writes set the multiplier to 2^(value/12); reads use 12*log2(max(1,multiplier)).</value>
    /// <remarks>Negative semitone writes retain a reciprocal multiplier while this getter reports zero.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The exponent does not produce a positive finite float multiplier.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float RandomPitchSemitones
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return 12 * MathF.Log2(Math.Max(1, _pitch)); } }
        set { Finite(value); var scale = MathF.Pow(2, value / 12); if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (GraphGate) { ThrowIfDisposed(); _pitch = scale; } }
    }
    /// <summary>Gets or sets the symmetric random volume range in decibels.</summary>
    /// <value>Zero initially; negative writes clamp to zero. Start samples a uniform offset in [-value,+value].</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float RandomVolumeOffsetDB
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _volume; } }
        set { Finite(value); lock (GraphGate) { ThrowIfDisposed(); _volume = Math.Max(0, value); } }
    }

    /// <summary>Inserts a borrowed stream and its literal probability weight.</summary>
    /// <param name="index">Insertion position through StreamsCount; any negative value appends.</param>
    /// <param name="stream">Borrowed stream, or null for an empty slot.</param>
    /// <param name="weight">Finite weight; one initially. Nonpositive weights are ignored by random modes.</param>
    /// <remarks>Commits before Changed and PropertyListChanged notifications. Nested randomizer cycles reject before mutation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The insertion position is beyond the end or the weight is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The assignment creates a randomizer cycle.</exception>
    /// <exception cref="ObjectDisposedException">This resource or the assigned stream is disposed.</exception>
    public void AddStream(int index, AudioStream? stream, float weight = 1)
    {
        Finite(weight);
        lock (GraphGate) { ThrowIfDisposed(); if (index < 0) index = _entries.Length; Index(index, insertion: true); ValidateStream(stream); _entries = [.. _entries.AsSpan(0, index), new(stream, weight), .. _entries.AsSpan(index)]; }
        NotifyPoolChange(structural: true);
    }
    /// <summary>Moves an entry to an insertion boundary in the pool before removal.</summary>
    /// <param name="indexFrom">Existing entry index.</param>
    /// <param name="indexTo">Boundary from zero through StreamsCount in the original pool.</param>
    /// <remarks>Moving index 1 to boundary 3 in [0,1,2,3] produces [0,2,1,3]. Emits both change notifications even for an unchanged order.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index is outside its stated range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void MoveStream(int indexFrom, int indexTo)
    {
        lock (GraphGate)
        {
            ThrowIfDisposed(); Index(indexFrom); Index(indexTo, insertion: true);
            var copy = (Entry[])_entries.Clone(); var entry = copy[indexFrom]; if (indexFrom < indexTo) indexTo--;
            if (indexTo < indexFrom) Array.Copy(copy, indexTo, copy, indexTo + 1, indexFrom - indexTo);
            else Array.Copy(copy, indexFrom + 1, copy, indexFrom, indexTo - indexFrom); copy[indexTo] = entry; _entries = copy;
        }
        NotifyPoolChange(structural: true);
    }
    /// <summary>Removes an entry without disposing its borrowed stream.</summary>
    /// <param name="index">Existing entry index.</param>
    /// <remarks>Emits Changed and PropertyListChanged; the previous selection remains available for GetLength.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the pool.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void RemoveStream(int index) { lock (GraphGate) { ThrowIfDisposed(); Index(index); _entries = [.. _entries.AsSpan(0, index), .. _entries.AsSpan(index + 1)]; } NotifyPoolChange(structural: true); }
    /// <summary>Replaces the borrowed stream at an existing index.</summary>
    /// <param name="index">Existing entry index.</param>
    /// <param name="stream">Borrowed stream or null.</param>
    /// <remarks>Retains the weight and emits Changed after commit, including equal assignments.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the pool.</exception>
    /// <exception cref="InvalidOperationException">The assignment creates a randomizer cycle.</exception>
    /// <exception cref="ObjectDisposedException">This resource or the assigned stream is disposed.</exception>
    public void SetStream(int index, AudioStream? stream) { lock (GraphGate) { ThrowIfDisposed(); Index(index); ValidateStream(stream); var copy = (Entry[])_entries.Clone(); copy[index] = copy[index] with { Stream = stream }; _entries = copy; } NotifyPoolChange(structural: false); }
    /// <summary>Gets the borrowed stream at an existing index.</summary>
    /// <param name="index">Existing entry index.</param>
    /// <returns>The borrowed resource or null.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the pool.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public AudioStream? GetStream(int index) { lock (GraphGate) { ThrowIfDisposed(); Index(index); return _entries[index].Stream; } }
    /// <summary>Replaces an entry's literal probability weight.</summary>
    /// <param name="index">Existing entry index.</param>
    /// <param name="weight">Finite signed weight; random modes consume only positive values.</param>
    /// <remarks>Emits Changed after commit, including equal assignments.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the pool or the weight is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetStreamProbabilityWeight(int index, float weight) { Finite(weight); lock (GraphGate) { ThrowIfDisposed(); Index(index); var copy = (Entry[])_entries.Clone(); copy[index] = copy[index] with { Weight = weight }; _entries = copy; } NotifyPoolChange(structural: false); }
    /// <summary>Gets an entry's literal probability weight.</summary>
    /// <param name="index">Existing entry index.</param>
    /// <returns>The stored signed weight.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the pool.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetStreamProbabilityWeight(int index) { lock (GraphGate) { ThrowIfDisposed(); Index(index); return _entries[index].Weight; } }

    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private void Index(int index, bool insertion = false) { if (index < 0 || index >= _entries.Length + (insertion ? 1L : 0)) throw new ArgumentOutOfRangeException(nameof(index)); }
    private void ValidateStream(AudioStream? stream)
    {
        ValidateChild(stream);
    }
    internal override void AppendChildren(Stack<AudioStream> pending) { foreach (var entry in _entries) if (entry.Stream is { } child) pending.Push(child); }
    private void NotifyPoolChange(bool structural)
    {
        Exception? first = null, second = null;
        try { EmitChanged(); } catch (Exception error) { first = error; }
        if (structural && !IsDisposed) try { NotifyPropertyListChanged(); } catch (Exception error) { second = error; }
        Resource.ThrowCombined(first, second);
    }
    private AudioStream? ChooseStream()
    {
        if (_mode == PlaybackMode.Sequential)
        {
            var seen = new HashSet<AudioStream>(ReferenceEqualityComparer.Instance); AudioStream? first = null; var afterLast = false;
            foreach (var entry in _entries)
            {
                if (entry.Stream is not { } stream || !seen.Add(stream)) continue;
                first ??= stream; if (afterLast) return stream; if (ReferenceEquals(stream, _last)) afterLast = true;
            }
            return first;
        }
        var excludeLast = _mode == PlaybackMode.RandomNoRepeats;
        var total = TotalWeight(excludeLast); if (total == 0) { excludeLast = false; total = TotalWeight(false); }
        if (total == 0) return null;
        var target = Random.Shared.NextDouble() * total; double sum = 0; AudioStream? fallback = null;
        foreach (var entry in _entries)
        {
            if (entry.Stream is null || entry.Weight <= 0 || excludeLast && ReferenceEquals(entry.Stream, _last)) continue;
            fallback = entry.Stream; sum += entry.Weight; if (sum > target) return fallback;
        }
        return fallback;
    }
    private double TotalWeight(bool excludeLast)
    {
        double result = 0; foreach (var entry in _entries) if (entry.Stream is not null && entry.Weight > 0 && (!excludeLast || !ReferenceEquals(entry.Stream, _last))) result += entry.Weight; return result;
    }
    /// <inheritdoc />
    protected override AudioStreamPlayback OnInstantiatePlayback()
    {
        EnterCall(0); AudioStreamPlayback? child = null;
        try
        {
            AudioStream? selected;
            lock (GraphGate) { ThrowIfDisposed(); selected = ChooseStream(); if (selected is not null) _last = selected; }
            selected?.EnsurePlaybackOwner(); child = selected?.InstantiatePlayback(); ThrowIfDisposed(); return new Playback(this, child);
        }
        catch (Exception error)
        {
            Exception? cleanup = null; try { child?.Dispose(); } catch (Exception failure) { cleanup = failure; }
            Resource.ThrowCombined(error, cleanup); throw;
        }
        finally { ExitCall(); }
    }
    /// <inheritdoc />
    protected override double OnGetLength()
    {
        EnterCall(1); try { AudioStream? selected; lock (GraphGate) { ThrowIfDisposed(); selected = _last; } return selected?.GetLength() ?? 0; } finally { ExitCall(); }
    }
    /// <inheritdoc />
    protected override bool OnIsMonophonic()
    {
        EnterCall(2);
        try { Entry[] entries; lock (GraphGate) { ThrowIfDisposed(); entries = _entries; } foreach (var entry in entries) if (entry.Stream?.IsMonophonic() == true) return true; return false; }
        finally { ExitCall(); }
    }
    /// <inheritdoc />
    protected override string OnGetStreamName() => "Randomizer";
    /// <inheritdoc />
    public override bool IsMetaStream() { ThrowIfDisposed(); return true; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamRandomizer();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        Entry[] entries; PlaybackMode mode; float pitch, volume;
        lock (GraphGate) { ThrowIfDisposed(); entries = (Entry[])_entries.Clone(); mode = _mode; pitch = _pitch; volume = _volume; }
        for (var i = 0; i < entries.Length; i++) entries[i] = entries[i] with { Stream = (AudioStream?)duplicateSubresource(entries[i].Stream) };
        var other = (AudioStreamRandomizer)target;
        lock (GraphGate)
        {
            other.ThrowIfDisposed(); foreach (var entry in entries) other.ValidateStream(entry.Stream);
            other._entries = entries; other._mode = mode; other._pitch = pitch; other._volume = volume; other._last = null;
        }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        var properties = new List<PropertyDescriptor>(base.GetPropertyDescriptors())
        {
            new PropertyDescriptor<AudioStreamRandomizer, PlaybackMode>(nameof(Mode), p => p.Mode, (p, v) => p.Mode = v, _ => PlaybackMode.RandomNoRepeats, stored: true),
            new PropertyDescriptor<AudioStreamRandomizer, int>(nameof(StreamsCount), p => p.StreamsCount, (p, v) => p.StreamsCount = v, _ => 0, stored: true),
            new PropertyDescriptor<AudioStreamRandomizer, float>(nameof(RandomPitch), p => p.RandomPitch, (p, v) => p.RandomPitch = v, _ => 1, stored: true),
            new PropertyDescriptor<AudioStreamRandomizer, float>(nameof(RandomPitchSemitones), p => p.RandomPitchSemitones, (p, v) => p.RandomPitchSemitones = v, _ => 0),
            new PropertyDescriptor<AudioStreamRandomizer, float>(nameof(RandomVolumeOffsetDB), p => p.RandomVolumeOffsetDB, (p, v) => p.RandomVolumeOffsetDB = v, _ => 0, stored: true)
        };
        var count = StreamsCount;
        for (var i = 0; i < count; i++)
        {
            var index = i;
            properties.Add(new PropertyDescriptor<AudioStreamRandomizer, AudioStream?>($"Stream_{index}/Stream", p => p.GetStream(index), (p, v) => p.SetStream(index, v), _ => null, stored: true));
            properties.Add(new PropertyDescriptor<AudioStreamRandomizer, float>($"Stream_{index}/Weight", p => p.GetStreamProbabilityWeight(index), (p, v) => p.SetStreamProbabilityWeight(index, v), _ => 1, stored: true));
        }
        return properties;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (GraphGate) { _entries = []; _last = null; } base.Dispose(disposing); }

    private sealed class Playback(AudioStreamRandomizer source, AudioStreamPlayback? child) : AudioStreamPlayback
    {
        internal override bool RequiresAudioOwner => base.RequiresAudioOwner || child?.RequiresAudioOwner == true;
        private bool _started;
        private float _pitch = 1, _gain = 1;
        private void Check() => ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        internal override void PrepareQueuedControls() => child?.PrepareQueuedControls();
        internal override void StartQueued(double time) => StartCore(time, queued: true);
        internal override void StopQueued() => child?.StopQueued();
        protected override void OnStart(double fromPosition)
            => StartCore(fromPosition, queued: false);
        private void StartCore(double fromPosition, bool queued)
        {
            Check(); float pitch, volume; lock (GraphGate) { source.ThrowIfDisposed(); pitch = source._pitch; volume = source._volume; }
            var log = Math.Log(pitch); var selectedPitch = (float)Math.Exp((Random.Shared.NextDouble() * 2 - 1) * log);
            var selectedGain = (float)Mathf.DBToLinear((Random.Shared.NextDouble() * 2 - 1) * volume);
            if (!float.IsFinite(selectedGain) || !float.IsFinite(selectedPitch) || selectedPitch <= 0) throw new InvalidOperationException("Random audio variation exceeds finite mixing values.");
            if (queued) child?.StartQueued(fromPosition); else child?.Start(fromPosition); _pitch = selectedPitch; _gain = selectedGain; _started = true;
        }
        protected override void OnStop() { child?.Stop(); }
        protected override bool OnIsPlaying() { Check(); return _started && child?.IsPlaying() == true; }
        protected override int OnGetLoopCount() { Check(); return _started ? child?.GetLoopCount() ?? 0 : 0; }
        protected override double OnGetPlaybackPosition() { Check(); return _started ? child?.GetPlaybackPosition() ?? 0 : 0; }
        protected override void OnSeek(double time) { Check(); if (_started) child?.Seek(time); }
        protected override int OnMix(Span<Vector2> buffer, float rateScale)
        {
            Check(); if (!_started || child is null) { buffer.Clear(); return buffer.Length; }
            var count = child.MixInto(buffer, rateScale * _pitch); for (var i = 0; i < count; i++) buffer[i] *= _gain; buffer[count..].Clear(); return count;
        }
        protected override void ValidateDisposal() { if (RequiresAudioOwner) AudioServer.Instance.Check(); base.ValidateDisposal(); }
        protected override void Dispose(bool disposing) { try { if (disposing) child?.Dispose(); } finally { base.Dispose(disposing); } }
    }
}
