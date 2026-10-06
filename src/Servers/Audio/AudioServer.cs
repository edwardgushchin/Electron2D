namespace Electron2D;

/// <summary>Owns process-wide audio bus configuration, the native output graph and recording input.</summary>
/// <remarks>Public static operations delegate to the retained service object; state, identity and ownership remain object-scoped.
/// Configuration requires its owner thread. Native resources open for playback or explicit device
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
    internal object StreamGate => _gate;
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
        internal nint Voice, InputVoice;
        internal FAudioBusBuffer? Buffer;
    }
    private readonly List<Bus> _buses = [new("Master")];
    private readonly List<AudioStreamPlayer> _players = [];
    private readonly Dictionary<string, FAudioBusBuffer> _sidechainBuffers = new(StringComparer.Ordinal);
    private AudioServer() { }
    internal static AudioServer Service => Singleton.Value;
    internal void Check()
    {
        ThrowIfDisposed(); CheckAudioReentrancy(); var thread = Environment.CurrentManagedThreadId; Interlocked.CompareExchange(ref _owner, thread, 0); if (thread != Volatile.Read(ref _owner)) throw new InvalidOperationException("Audio configuration requires its owner thread.");
    }
    private Bus GetBus(int index) { Check(); if ((uint)index >= (uint)_buses.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _buses[index]; }
    internal int BusCountCore
    {
        get { Check(); return _buses.Count; }
        set { Check(); if (value is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(value)); while (_buses.Count < value) AddBusCore(); while (_buses.Count > value) RemoveBusCore(_buses.Count - 1); }
    }
    internal float PlaybackSpeedScaleCore
    {
        get { ThrowIfDisposed(); return AtomicFloatingPoint.Read(ref _speed); }
        set { Check(); if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); var previous = _speed; AtomicFloatingPoint.Write(ref _speed, value); try { foreach (var player in _players) player.RefreshPitch(); _native?.RefreshStandaloneSamplePitch(); } catch { AtomicFloatingPoint.Write(ref _speed, previous); foreach (var player in _players) player.RefreshPitch(); _native?.RefreshStandaloneSamplePitch(); throw; } }
    }
    internal event Action? BusLayoutChangedCore;
    internal event Action<int, string, string>? BusRenamedCore;
    internal void AddBusCore(int atPosition = -1)
    {
        Check(); if (_buses.Count == 255) throw new ArgumentOutOfRangeException(nameof(atPosition)); if (atPosition < 0) atPosition = _buses.Count;
        if (atPosition == 0 || atPosition > _buses.Count) throw new ArgumentOutOfRangeException(nameof(atPosition));
        var name = "New Bus"; var number = 2; while (GetBusIndexCore(name) >= 0) name = "New Bus " + number++;
        _buses.Insert(atPosition, new(name)); RebuildGraph(); BusLayoutChangedCore?.Invoke();
    }
    internal void RemoveBusCore(int index)
    {
        var bus = GetBus(index); if (index == 0) throw new ArgumentOutOfRangeException(nameof(index)); _buses.RemoveAt(index); List<Exception>? errors = null;
        try { RebuildGraph(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { ReleaseEffects(bus.RuntimeEffects); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { BusLayoutChangedCore?.Invoke(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Audio bus removal failed.", errors);
    }
    internal void MoveBusCore(int index, int toIndex)
    {
        var bus = GetBus(index); if (index == 0) throw new ArgumentOutOfRangeException(nameof(index)); if (toIndex < 0) toIndex = _buses.Count;
        if (toIndex == 0 || toIndex > _buses.Count) throw new ArgumentOutOfRangeException(nameof(toIndex)); _buses.RemoveAt(index); if (toIndex > index) toIndex--; _buses.Insert(toIndex, bus); RebuildGraph(); BusLayoutChangedCore?.Invoke();
    }
    internal int GetBusIndexCore(string busName) { Check(); ArgumentNullException.ThrowIfNull(busName); for (var i = 0; i < _buses.Count; i++) if (_buses[i].Name == busName) return i; return -1; }
    internal string GetBusNameCore(int index) => GetBus(index).Name;
    internal void SetBusNameCore(int index, string name)
    {
        var bus = GetBus(index); ArgumentNullException.ThrowIfNull(name); if (index == 0) throw new ArgumentOutOfRangeException(nameof(index)); if (bus.Name == name) return;
        var proposed = name; var number = 2; while (GetBusIndexCore(proposed) >= 0) proposed = name + " " + number++;
        var old = bus.Name;
        lock (_gate)
        {
            bus.Name = proposed; if (bus.Buffer is { } buffer) { _sidechainBuffers.Remove(old); _sidechainBuffers.Add(proposed, buffer); }
            foreach (var other in _buses) if (other.Send == old) other.Send = proposed; foreach (var player in _players) player.RefreshRouting(); _native?.RefreshStandaloneSampleRouting(); ApplyGains();
        }
        BusRenamedCore?.Invoke(index, old, proposed);
    }
    internal string GetBusSendCore(int index) => GetBus(index).Send;
    internal void SetBusSendCore(int index, string send) { var bus = GetBus(index); ArgumentNullException.ThrowIfNull(send); if (index == 0) throw new ArgumentOutOfRangeException(nameof(index)); if (bus.Send == send) return; bus.Send = send; RebuildGraph(); BusLayoutChangedCore?.Invoke(); }
    internal float GetBusVolumeDBCore(int index) => GetBus(index).VolumeDB;
    internal float GetBusVolumeLinearCore(int index) => (float)Mathf.DBToLinear(GetBusVolumeDBCore(index));
    internal void SetBusVolumeDBCore(int index, float volumeDB) { var bus = GetBus(index); if (!float.IsFinite(volumeDB) || !float.IsFinite(Mathf.DBToLinear(volumeDB))) throw new ArgumentOutOfRangeException(nameof(volumeDB)); bus.VolumeDB = volumeDB; ApplyGains(); }
    internal void SetBusVolumeLinearCore(int index, float volumeLinear) { var bus = GetBus(index); if (!float.IsFinite(volumeLinear) || volumeLinear < 0) throw new ArgumentOutOfRangeException(nameof(volumeLinear)); bus.VolumeDB = (float)Mathf.LinearToDB(volumeLinear); ApplyGains(); }
    internal bool IsBusMuteCore(int index) => GetBus(index).Mute;
    internal void SetBusMuteCore(int index, bool enable) { GetBus(index).Mute = enable; ApplyGains(); }
    internal bool IsBusSoloCore(int index) => GetBus(index).Solo;
    internal void SetBusSoloCore(int index, bool enable) { GetBus(index).Solo = enable; ApplyGains(); }
    internal int GetBusChannelsCore(int index) { _ = GetBus(index); EnsureNative(); return _native!.Channels / 2; }
    internal float GetBusPeakVolumeLeftDBCore(int index, int channel) => PeakDB(index, channel, false);
    internal float GetBusPeakVolumeRightDBCore(int index, int channel) => PeakDB(index, channel, true);
    private float PeakDB(int index, int channel, bool right) { var bus = GetBus(index); EnsureNative(); if ((uint)channel >= (uint)(_native!.Channels / 2)) throw new ArgumentOutOfRangeException(nameof(channel)); return Math.Max(-200, (float)Mathf.LinearToDB(_native.BusPeak(bus.Voice, channel * 2 + (right ? 1 : 0)))); }
    internal float GetMixRateCore() { ThrowIfDisposed(); return _native?.MixRate ?? 44100; }
    internal string GetDriverNameCore() { Check(); EnsureNative(); return SDL3.SDL.GetCurrentAudioDriver() ?? throw new InvalidOperationException("Audio has no active driver."); }
    internal string[] GetOutputDeviceListCore() { Check(); EnsureNative(); return _native!.Devices(); }
    internal SpeakerMode GetSpeakerModeCore() { Check(); EnsureNative(); return _native!.Channels switch { 2 => SpeakerMode.Stereo, 4 => SpeakerMode.Surround31, 6 => SpeakerMode.Surround51, 8 => SpeakerMode.Surround71, _ => throw new NotSupportedException("The native channel arrangement has no supported speaker mode.") }; }
    internal double GetTimeSinceLastMixCore() { Check(); return _native?.SinceMix ?? 0; }
    internal double GetTimeToNextMixCore() { Check(); return _native is null ? 0 : Math.Max(0, _native.QuantumFrames / (double)_native.MixRate - _native.SinceMix); }
    internal void LockCore() { ThrowIfDisposed(); CheckAudioReentrancy(); Monitor.Enter(_gate); }
    internal void UnlockCore() { CheckAudioReentrancy(); Monitor.Exit(_gate); }
    internal void EnsureNative()
    {
        Check(); if (_native is not null) return; _native = new FAudioContext(gate: _gate);
        try { if (_outputDevice != "Default") _native.SetOutput(_outputDevice); RebuildGraph(); } catch { _native?.Dispose(); _native = null; _samples.Clear(); foreach (var bus in _buses) bus.Voice = 0; throw; }
    }
    internal FAudioContext Native { get { EnsureNative(); return _native!; } }
    internal nint ResolveBus(string name) { EnsureNative(); var index = GetBusIndexCore(name); return _buses[index < 0 ? 0 : index].InputVoice; }
    internal ReadOnlySpan<Vector2> ReadSidechain(string name, int pair, int frames)
    {
        var buffer = _sidechainBuffers.TryGetValue(name, out var found) ? found : _sidechainBuffers["Master"];
        return buffer.Read(pair, frames);
    }
    internal void Attach(AudioStreamPlayer player) { Check(); if (!_players.Contains(player)) _players.Add(player); }
    internal void Detach(AudioStreamPlayer player) { Check(); _players.Remove(player); if (_players.Count == 0 && _native?.HasStandaloneSamples != true) CloseNative(closeInput: false); }
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
            var old = _native.BusVoices(); var replacement = new nint[_buses.Count]; var inputs = new nint[_buses.Count]; var buffers = new FAudioBusBuffer[_buses.Count];
            try
            {
                for (var i = 0; i < replacement.Length; i++)
                {
                    var bus = _buses[i]; bus.RuntimeEffects ??= PrepareEffects(bus.Effects);
                    bus.Activity ??= _native.CreateBusActivity(); buffers[i] = new(_native.Channels, _native.QuantumFrames, bus.Activity);
                    replacement[i] = _native.CreateBus((uint)(65535 - i), _native.Master, bus.RuntimeEffects, bus.Effects.Select(e => e.Enabled && !bus.Bypass).ToArray(), bus.Activity, buffers[i]);
                }
                for (var i = 0; i < inputs.Length; i++) inputs[i] = _native.CreateBusInput(replacement[i], buffers[i]);
                for (var i = 1; i < replacement.Length; i++) { var target = GetBusIndexCore(_buses[i].Send); if (target < 0 || target >= i) target = 0; _native.SetSend(replacement[i], replacement[target]); buffers[i].Send = buffers[target]; }
            }
            catch
            {
                for (var i = inputs.Length - 1; i >= 0; i--) if (inputs[i] != 0) _native.DestroyBus(inputs[i]);
                for (var i = replacement.Length - 1; i >= 0; i--) if (replacement[i] != 0) _native.DestroyBus(replacement[i]); throw;
            }
            List<Exception>? errors = null;
            _sidechainBuffers.Clear();
            for (var i = 0; i < replacement.Length; i++) { _buses[i].Voice = replacement[i]; _buses[i].InputVoice = inputs[i]; _buses[i].Buffer = buffers[i]; _sidechainBuffers.Add(_buses[i].Name, buffers[i]); }
            foreach (var player in _players) try { player.RefreshRouting(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            _native.RefreshStandaloneSampleRouting();
            Node.ThrowCollected("Audio bus routing update failed.", errors);
            for (var i = old.Length - 1; i >= 0; i--) _native.DestroyBus(old[i]);
            try { ApplyGains(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Audio bus graph restoration failed.", errors);
        }
    }

    internal float ResolveSourceGain(string name, float gain)
    {
        var solo = false; foreach (var bus in _buses) solo |= bus.Solo; if (!solo) return gain;
        var index = GetBusIndexCore(name); if (index < 0) index = 0;
        for (var steps = 0; steps < _buses.Count; steps++)
        {
            if (_buses[index].Solo) return gain; if (index == 0) break;
            var next = GetBusIndexCore(_buses[index].Send); index = next < 0 || next >= index ? 0 : next;
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
                        var current = j; for (var steps = 0; steps < _buses.Count; steps++) { var next = GetBusIndexCore(_buses[current].Send); if (next < 0 || next >= current) next = 0; if (next == i) { audible = true; break; } if (next == 0) break; current = next; }
                    }
            var gain = _buses[i].Mute || !audible ? 0 : (float)Mathf.DBToLinear(_buses[i].VolumeDB); _native.SetBusVolume(_buses[i].Voice, gain);
        }
        foreach (var player in _players) player.RefreshVolume();
        _native?.RefreshStandaloneSampleGains();
    }
    internal static void CloseForEngine() { if (Singleton.IsValueCreated && !Singleton.Value.IsDisposed) Singleton.Value.CloseNative(); }
    internal void CloseNative(bool closeInput = true)
    {
        Check(); List<Exception>? errors = null;
        foreach (var player in _players.ToArray()) try { player.ReleaseVoices(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { _native?.Dispose(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        _native = null; _samples.Clear(); _sidechainBuffers.Clear(); foreach (var bus in _buses) { bus.Voice = 0; bus.InputVoice = 0; bus.Buffer = null; }
        foreach (var bus in _buses) { var effects = bus.RuntimeEffects; bus.RuntimeEffects = null; bus.Activity = null; try { ReleaseEffects(effects); } catch (Exception error) { Node.CollectException(ref errors, error); } }
        if (closeInput) try { CloseInput(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Audio native teardown failed.", errors);
    }
    /// <inheritdoc />
    /// <remarks>The process-wide service has process lifetime; engine teardown closes its native resources.</remarks>
    /// <exception cref="InvalidOperationException">Always thrown for the borrowed singleton.</exception>
    protected override void ValidateDisposal() => throw new InvalidOperationException("The process-wide AudioServer instance cannot be disposed.");
}
