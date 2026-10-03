namespace Electron2D;

/// <summary>Owns process-wide audio bus configuration, the native output graph and recording input.</summary>
/// <remarks>Configuration requires its owner thread. Native resources open for playback or explicit device
/// preparation and close at engine teardown. Input activation is separately gated by AudioDriverEnableInput;
/// copied array queries and graph/device edits are explicit preparation operations.</remarks>
public sealed partial class AudioServer : ElectronObject
{
    /// <summary>Selects the output playback implementation.</summary>
    public enum PlaybackType
    {
        /// <summary>Use the configured default.</summary>
        Default = 0,
        /// <summary>Use the stream mixing path.</summary>
        Stream = 1,
        /// <summary>Use prepared native samples where available.</summary>
        Sample = 2,
        /// <summary>Exclusive upper selector bound.</summary>
        Max = 3
    }
    /// <summary>Identifies the actual output channel arrangement.</summary>
    public enum SpeakerMode
    {
        /// <summary>Left and right output.</summary>
        Stereo = 0,
        /// <summary>Three full-range channels and low frequency.</summary>
        Surround31 = 1,
        /// <summary>Five full-range channels and low frequency.</summary>
        Surround51 = 2,
        /// <summary>Seven full-range channels and low frequency.</summary>
        Surround71 = 3
    }
    private static readonly Lazy<AudioServer> Singleton = new(() => new AudioServer());
    private int _owner;
    private readonly object _gate = new();
    private FAudioContext? _native;
    private float _speed = 1;
    private sealed class Bus(string name)
    {
        internal string Name = name, Send = "Master";
        internal float VolumeDB;
        internal bool Mute, Solo;
        internal bool Bypass;
        internal List<BusEffect> Effects = [];
        internal FAudioBusEffect[]? RuntimeEffects;
        internal FAudioBusEffect.Activity? Activity;
        internal nint Voice;
    }
    private readonly List<Bus> _buses = [new("Master")];
    private readonly List<AudioStreamPlayer> _players = [];
    private AudioServer() { }
    /// <summary>Gets the process-wide audio service.</summary>
    /// <value>The borrowed singleton; applications configure it rather than disposing it.</value>
    public static AudioServer Instance => Singleton.Value;
    internal void Check()
    {
        ThrowIfDisposed(); CheckAudioReentrancy(); var thread = Environment.CurrentManagedThreadId; Interlocked.CompareExchange(ref _owner, thread, 0); if (thread != Volatile.Read(ref _owner)) throw new InvalidOperationException("Audio configuration requires its owner thread.");
    }
    private Bus GetBus(int index) { Check(); if ((uint)index >= (uint)_buses.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _buses[index]; }
    /// <summary>Gets or sets the number of bus records, including the required Master.</summary>
    /// <value>One initially; values must be between one and 255.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside the valid range.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    public int BusCount
    {
        get { Check(); return _buses.Count; }
        set { Check(); if (value is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(value)); while (_buses.Count < value) AddBus(); while (_buses.Count > value) RemoveBus(_buses.Count - 1); }
    }
    /// <summary>Gets or sets the positive global playback-rate multiplier.</summary>
    /// <value>One initially; actual player pitch combines this value with its local scale.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonpositive or nonfinite.</exception>
    public float PlaybackSpeedScale
    {
        get { ThrowIfDisposed(); return Volatile.Read(ref _speed); }
        set { Check(); if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); Volatile.Write(ref _speed, value); foreach (var player in _players) player.RefreshPitch(); }
    }
    /// <summary>Occurs after the bus list or routing graph changes.</summary>
    public event Action? BusLayoutChanged;
    /// <summary>Occurs after a bus rename commits.</summary>
    /// <remarks>Arguments are the index, old name and unique new name.</remarks>
    public event Action<int, string, string>? BusRenamed;
    /// <summary>Adds a uniquely named bus at an index or appends it.</summary>
    /// <param name="atPosition">Insertion index; minus one appends. Master remains at zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">The insertion range or bus limit is invalid.</exception>
    public void AddBus(int atPosition = -1)
    {
        Check(); if (_buses.Count == 255) throw new ArgumentOutOfRangeException(nameof(atPosition)); if (atPosition < 0) atPosition = _buses.Count;
        if (atPosition == 0 || atPosition > _buses.Count) throw new ArgumentOutOfRangeException(nameof(atPosition));
        var name = "New Bus"; var number = 2; while (GetBusIndex(name) >= 0) name = "New Bus " + number++;
        _buses.Insert(atPosition, new(name)); RebuildGraph(); BusLayoutChanged?.Invoke();
    }
    /// <summary>Removes a non-Master bus, redirecting unresolved sends to Master.</summary>
    /// <param name="index">Live nonzero bus index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid or identifies Master.</exception>
    public void RemoveBus(int index)
    {
        var bus = GetBus(index); if (index == 0) throw new ArgumentOutOfRangeException(nameof(index)); _buses.RemoveAt(index); List<Exception>? errors = null;
        try { RebuildGraph(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { ReleaseEffects(bus.RuntimeEffects); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { BusLayoutChanged?.Invoke(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Audio bus removal failed.", errors);
    }
    /// <summary>Moves a non-Master bus; minus one moves it to the end.</summary>
    /// <param name="index">Live non-Master source index.</param>
    /// <param name="toIndex">Destination insertion index; minus one appends.</param>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or identifies Master.</exception>
    public void MoveBus(int index, int toIndex)
    {
        var bus = GetBus(index); if (index == 0) throw new ArgumentOutOfRangeException(nameof(index)); if (toIndex < 0) toIndex = _buses.Count;
        if (toIndex == 0 || toIndex > _buses.Count) throw new ArgumentOutOfRangeException(nameof(toIndex)); _buses.RemoveAt(index); if (toIndex > index) toIndex--; _buses.Insert(toIndex, bus); RebuildGraph(); BusLayoutChanged?.Invoke();
    }
    /// <summary>Gets the index of a bus by exact name.</summary>
    /// <param name="busName">Nonnull exact bus name.</param>
    /// <returns>Minus one when absent.</returns>
    public int GetBusIndex(string busName) { Check(); ArgumentNullException.ThrowIfNull(busName); for (var i = 0; i < _buses.Count; i++) if (_buses[i].Name == busName) return i; return -1; }
    /// <summary>Gets a bus name.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>The unique exact name.</returns>
    public string GetBusName(int index) => GetBus(index).Name;
    /// <summary>Renames a bus, updating named sends and resolving duplicate names.</summary>
    /// <param name="index">Live non-Master index.</param>
    /// <param name="name">Requested nonnull name.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid or identifies Master.</exception>
    public void SetBusName(int index, string name)
    {
        var bus = GetBus(index); ArgumentNullException.ThrowIfNull(name); if (index == 0) throw new ArgumentOutOfRangeException(nameof(index)); if (bus.Name == name) return;
        var proposed = name; var number = 2; while (GetBusIndex(proposed) >= 0) proposed = name + " " + number++;
        var old = bus.Name; bus.Name = proposed; foreach (var other in _buses) if (other.Send == old) other.Send = proposed; foreach (var player in _players) player.RefreshRouting(); ApplyGains(); BusRenamed?.Invoke(index, old, proposed);
    }
    /// <summary>Gets the requested named send target.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>The requested target; invalid or later targets resolve to Master during mixing.</returns>
    public string GetBusSend(int index) => GetBus(index).Send;
    /// <summary>Sets a non-Master bus send target.</summary>
    /// <param name="index">Live non-Master bus index.</param>
    /// <param name="send">Nonnull requested target name.</param>
    public void SetBusSend(int index, string send) { var bus = GetBus(index); ArgumentNullException.ThrowIfNull(send); if (index == 0) throw new ArgumentOutOfRangeException(nameof(index)); if (bus.Send == send) return; bus.Send = send; RebuildGraph(); BusLayoutChanged?.Invoke(); }
    /// <summary>Gets bus gain in decibels.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>Gain in dB.</returns>
    public float GetBusVolumeDB(int index) => GetBus(index).VolumeDB;
    /// <summary>Gets the linear bus gain.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>Ten raised to dB/20.</returns>
    public float GetBusVolumeLinear(int index) => (float)Mathf.DBToLinear(GetBusVolumeDB(index));
    /// <summary>Sets finite bus gain in decibels.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="volumeDB">Finite gain.</param>
    /// <exception cref="ArgumentOutOfRangeException">The gain is not finite.</exception>
    public void SetBusVolumeDB(int index, float volumeDB) { var bus = GetBus(index); if (!float.IsFinite(volumeDB)) throw new ArgumentOutOfRangeException(nameof(volumeDB)); bus.VolumeDB = volumeDB; ApplyGains(); }
    /// <summary>Sets nonnegative finite linear bus gain.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="volumeLinear">Zero mutes through negative infinity decibels.</param>
    public void SetBusVolumeLinear(int index, float volumeLinear) { var bus = GetBus(index); if (!float.IsFinite(volumeLinear) || volumeLinear < 0) throw new ArgumentOutOfRangeException(nameof(volumeLinear)); bus.VolumeDB = (float)Mathf.LinearToDB(volumeLinear); ApplyGains(); }
    /// <summary>Gets whether a bus is mute.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>The current flag.</returns>
    public bool IsBusMute(int index) => GetBus(index).Mute;
    /// <summary>Sets the bus mute flag and updates live native gains.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="enable">New flag.</param>
    public void SetBusMute(int index, bool enable) { GetBus(index).Mute = enable; ApplyGains(); }
    /// <summary>Gets whether a bus is solo.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>The current flag.</returns>
    public bool IsBusSolo(int index) => GetBus(index).Solo;
    /// <summary>Sets the bus solo flag and updates live native gains.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="enable">New flag.</param>
    public void SetBusSolo(int index, bool enable) { GetBus(index).Solo = enable; ApplyGains(); }
    /// <summary>Gets the number of stereo channel pairs on a bus.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>Actual native output channels divided by two.</returns>
    public int GetBusChannels(int index) { _ = GetBus(index); EnsureNative(); return _native!.Channels / 2; }
    /// <summary>Gets the measured left-channel bus peak in decibels.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="channel">Stereo pair index.</param>
    /// <returns>Measured peak, with silence bounded at minus 200 dB.</returns>
    public float GetBusPeakVolumeLeftDB(int index, int channel) => PeakDB(index, channel, false);
    /// <summary>Gets the measured right-channel bus peak in decibels.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="channel">Stereo pair index.</param>
    /// <returns>Measured peak, with silence bounded at minus 200 dB.</returns>
    public float GetBusPeakVolumeRightDB(int index, int channel) => PeakDB(index, channel, true);
    private float PeakDB(int index, int channel, bool right) { var bus = GetBus(index); EnsureNative(); if ((uint)channel >= (uint)(_native!.Channels / 2)) throw new ArgumentOutOfRangeException(nameof(channel)); return Math.Max(-200, (float)Mathf.LinearToDB(_native.BusPeak(bus.Voice, channel * 2 + (right ? 1 : 0)))); }
    /// <summary>Gets the active audio mix frequency.</summary>
    /// <returns>44100 Hz before output preparation, otherwise actual native mix frequency.</returns>
    public float GetMixRate() { ThrowIfDisposed(); return _native?.MixRate ?? 44100; }
    /// <summary>Gets the actual native audio driver.</summary>
    /// <returns>The SDL audio driver after output preparation.</returns>
    public string GetDriverName() { Check(); EnsureNative(); return SDL3.SDL.GetCurrentAudioDriver() ?? throw new InvalidOperationException("Audio has no active driver."); }
    /// <summary>Lists actual output devices.</summary>
    /// <returns>A caller-owned device-name array.</returns>
    public string[] GetOutputDeviceList() { Check(); EnsureNative(); return _native!.Devices(); }
    /// <summary>Gets the native output channel arrangement.</summary>
    /// <returns>The current speaker selector.</returns>
    public SpeakerMode GetSpeakerMode() { Check(); EnsureNative(); return _native!.Channels switch { 2 => SpeakerMode.Stereo, 4 => SpeakerMode.Surround31, 6 => SpeakerMode.Surround51, 8 => SpeakerMode.Surround71, _ => throw new NotSupportedException("The native channel arrangement has no supported speaker mode.") }; }
    /// <summary>Gets elapsed time since the last actual native mix quantum.</summary>
    /// <returns>Seconds, zero before native output exists.</returns>
    public double GetTimeSinceLastMix() { Check(); return _native?.SinceMix ?? 0; }
    /// <summary>Gets the estimated time until the next native quantum.</summary>
    /// <returns>Nonnegative seconds based on actual quantum size and mix timestamp.</returns>
    public double GetTimeToNextMix() { Check(); return _native is null ? 0 : Math.Max(0, _native.QuantumFrames / (double)_native.MixRate - _native.SinceMix); }
    /// <summary>Locks configuration for an explicit caller-owned critical section.</summary>
    /// <remarks>Pair with Unlock in finally; this protects the owned bus/playback state, not arbitrary game code.</remarks>
    public void Lock() { ThrowIfDisposed(); CheckAudioReentrancy(); Monitor.Enter(_gate); }
    /// <summary>Releases one matching configuration lock.</summary>
    /// <exception cref="SynchronizationLockException">The calling thread owns no matching lock.</exception>
    public void Unlock() { CheckAudioReentrancy(); Monitor.Exit(_gate); }
    internal void EnsureNative()
    {
        Check(); if (_native is not null) return; _native = new FAudioContext(gate: _gate);
        try { RebuildGraph(); } catch { _native?.Dispose(); _native = null; foreach (var bus in _buses) bus.Voice = 0; throw; }
    }
    internal FAudioContext Native { get { EnsureNative(); return _native!; } }
    internal nint ResolveBus(string name) { EnsureNative(); var index = GetBusIndex(name); return _buses[index < 0 ? 0 : index].Voice; }
    internal void Attach(AudioStreamPlayer player) { Check(); if (!_players.Contains(player)) _players.Add(player); }
    internal void Detach(AudioStreamPlayer player) { Check(); _players.Remove(player); if (_players.Count == 0) CloseNative(closeInput: false); }
    private void RebuildGraph()
    {
        try { RebuildGraphCore(); }
        catch (Exception error)
        {
            try { CloseNative(); } catch (Exception cleanup) { throw new AggregateException("Audio graph update and teardown failed.", error, cleanup); }
            throw;
        }
    }
    private void RebuildGraphCore()
    {
        if (_native is null) return;
        lock (_gate)
        {
            var old = _native.BusVoices(); var replacement = new nint[_buses.Count];
            try
            {
                for (var i = 0; i < replacement.Length; i++)
                {
                    var bus = _buses[i]; bus.RuntimeEffects ??= PrepareEffects(bus.Effects);
                    bus.Activity ??= _native.CreateBusActivity(); replacement[i] = _native.CreateBus((uint)(65535 - i), _native.Master, bus.RuntimeEffects, bus.Effects.Select(e => e.Enabled && !bus.Bypass).ToArray(), bus.Activity);
                }
                for (var i = 1; i < replacement.Length; i++) { var target = GetBusIndex(_buses[i].Send); if (target < 0 || target >= i) target = 0; _native.SetSend(replacement[i], replacement[target]); }
            }
            catch { for (var i = replacement.Length - 1; i >= 0; i--) if (replacement[i] != 0) _native.DestroyBus(replacement[i]); throw; }
            List<Exception>? errors = null;
            for (var i = 0; i < replacement.Length; i++) _buses[i].Voice = replacement[i];
            foreach (var player in _players) try { player.RefreshRouting(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Audio bus routing update failed.", errors);
            for (var i = old.Length - 1; i >= 0; i--) _native.DestroyBus(old[i]);
            try { ApplyGains(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Audio bus graph restoration failed.", errors);
        }
    }

    internal float ResolveSourceGain(string name, float gain)
    {
        var solo = false; foreach (var bus in _buses) solo |= bus.Solo; if (!solo) return gain;
        var index = GetBusIndex(name); if (index < 0) index = 0;
        for (var steps = 0; steps < _buses.Count; steps++)
        {
            if (_buses[index].Solo) return gain; if (index == 0) break;
            var next = GetBusIndex(_buses[index].Send); index = next < 0 || next >= index ? 0 : next;
        }
        return 0;
    }
    private void ApplyGains()
    {
        if (_native is null) return; var solo = false; foreach (var bus in _buses) solo |= bus.Solo;
        for (var i = 0; i < _buses.Count; i++)
        {
            var audible = !solo || i == 0 || _buses[i].Solo;
            if (solo && !audible) for (var j = 0; j < _buses.Count; j++) if (_buses[j].Solo)
                    {
                        var current = j; for (var steps = 0; steps < _buses.Count; steps++) { var next = GetBusIndex(_buses[current].Send); if (next < 0 || next >= current) next = 0; if (next == i) { audible = true; break; } if (next == 0) break; current = next; }
                    }
            var gain = _buses[i].Mute || !audible ? 0 : (float)Mathf.DBToLinear(_buses[i].VolumeDB); _native.SetBusVolume(_buses[i].Voice, gain);
        }
        foreach (var player in _players) player.RefreshVolume();
    }
    internal static void CloseForEngine() { if (Singleton.IsValueCreated && !Singleton.Value.IsDisposed) Singleton.Value.CloseNative(); }
    internal void CloseNative(bool closeInput = true)
    {
        Check(); List<Exception>? errors = null;
        foreach (var player in _players.ToArray()) try { player.ReleaseVoices(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { _native?.Dispose(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        _native = null; foreach (var bus in _buses) bus.Voice = 0;
        foreach (var bus in _buses) { var effects = bus.RuntimeEffects; bus.RuntimeEffects = null; bus.Activity = null; try { ReleaseEffects(effects); } catch (Exception error) { Node.CollectException(ref errors, error); } }
        if (closeInput) try { CloseInput(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Audio native teardown failed.", errors);
    }
    /// <inheritdoc />
    /// <remarks>The process-wide service has process lifetime; engine teardown closes its native resources.</remarks>
    /// <exception cref="InvalidOperationException">Always thrown for the borrowed singleton.</exception>
    protected override void ValidateDisposal() => throw new InvalidOperationException("The process-wide AudioServer instance cannot be disposed.");
}
