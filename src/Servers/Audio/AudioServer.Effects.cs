namespace Electron2D;

public sealed partial class AudioServer
{
    private sealed record BusEffect(AudioEffect Resource, bool Enabled = true);
    private bool _effectCallback;
    [ThreadStatic] private static int _audioProcessing;
    internal static void EnterAudioProcessing() => _audioProcessing++;
    internal static void ExitAudioProcessing() => _audioProcessing--;
    private void CheckAudioReentrancy() { if (_audioProcessing != 0 || Environment.CurrentManagedThreadId == _owner && (_effectCallback || _sampling)) throw new InvalidOperationException("Audio callbacks cannot reenter audio configuration."); }
    internal AudioEffectInstance CreateEffectInstance(AudioEffect effect)
    {
        _effectCallback = true; try { return effect.Instantiate(); } finally { _effectCallback = false; }
    }
    internal void DisposeEffectInstance(AudioEffectInstance instance)
    {
        _effectCallback = true; try { instance.Dispose(); } finally { _effectCallback = false; }
    }
    private FAudioBusEffect[] PrepareEffects(List<BusEffect> records)
    {
        var result = new FAudioBusEffect[records.Count];
        try { for (var i = 0; i < result.Length; i++) result[i] = new(_native!.Channels, _native.QuantumFrames, records[i].Resource); return result; }
        catch (Exception error) { try { ReleaseEffects(result); } catch (Exception cleanup) { throw new AggregateException("Effect chain preparation and cleanup failed.", error, cleanup); } throw; }
    }
    private static void ReleaseEffects(FAudioBusEffect[]? effects)
    {
        if (effects is null) return; List<Exception>? errors = null;
        foreach (var effect in effects) if (effect is not null)
            {
                effect.Dispose(); try { effect.ReleaseInstances(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            }
        Node.ThrowCollected("Effect chain teardown failed.", errors);
    }
    private void EditEffects(Bus bus, List<BusEffect> records)
    {
        EnsureNative(); lock (_gate)
        {
            var prepared = PrepareEffects(records); var old = bus.RuntimeEffects; bus.Effects = records; bus.RuntimeEffects = prepared;
            List<Exception>? errors = null;
            try { RebuildGraph(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            try { ReleaseEffects(old); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Audio effect edit failed.", errors);
        }
    }
    private BusEffect GetEffect(Bus bus, int index) { if ((uint)index >= (uint)bus.Effects.Count) throw new ArgumentOutOfRangeException(nameof(index)); return bus.Effects[index]; }
    internal void AddBusEffectCore(int busIndex, AudioEffect effect, int atPosition = -1)
    {
        var bus = GetBus(busIndex); ArgumentNullException.ThrowIfNull(effect); ObjectDisposedException.ThrowIf(effect.IsDisposed, effect);
        var records = new List<BusEffect>(bus.Effects); if (atPosition < 0 || atPosition >= records.Count) atPosition = records.Count; records.Insert(atPosition, new(effect)); EditEffects(bus, records);
    }
    internal int GetBusEffectCountCore(int busIndex) => GetBus(busIndex).Effects.Count;
    internal AudioEffect GetBusEffectCore(int busIndex, int effectIndex) => GetEffect(GetBus(busIndex), effectIndex).Resource;
    internal AudioEffectInstance GetBusEffectInstanceCore(int busIndex, int effectIndex, int channel = 0)
    {
        var bus = GetBus(busIndex); _ = GetEffect(bus, effectIndex); EnsureNative();
        var instances = bus.RuntimeEffects![effectIndex].Instances; if ((uint)channel >= (uint)instances.Length) throw new ArgumentOutOfRangeException(nameof(channel)); return instances[channel];
    }
    internal void RemoveBusEffectCore(int busIndex, int effectIndex) { var bus = GetBus(busIndex); _ = GetEffect(bus, effectIndex); var records = new List<BusEffect>(bus.Effects); records.RemoveAt(effectIndex); EditEffects(bus, records); }
    internal void SwapBusEffectsCore(int busIndex, int effectIndex, int byEffectIndex)
    {
        var bus = GetBus(busIndex); _ = GetEffect(bus, effectIndex); _ = GetEffect(bus, byEffectIndex); if (effectIndex == byEffectIndex) return;
        var records = new List<BusEffect>(bus.Effects); (records[effectIndex], records[byEffectIndex]) = (records[byEffectIndex], records[effectIndex]); EditEffects(bus, records);
    }
    internal bool IsBusEffectEnabledCore(int busIndex, int effectIndex) => GetEffect(GetBus(busIndex), effectIndex).Enabled;
    internal void SetBusEffectEnabledCore(int busIndex, int effectIndex, bool enabled)
    {
        var bus = GetBus(busIndex); var effect = GetEffect(bus, effectIndex); if (effect.Enabled == enabled) return;
        lock (_gate) { if (_native is not null) _native.EnableBusEffect(bus.Voice, effectIndex, enabled && !bus.Bypass); bus.Effects[effectIndex] = effect with { Enabled = enabled }; }
    }
    internal bool IsBusBypassingEffectsCore(int busIndex) => GetBus(busIndex).Bypass;
    internal void SetBusBypassEffectsCore(int busIndex, bool enable)
    {
        var bus = GetBus(busIndex); if (bus.Bypass == enable) return;
        lock (_gate)
        {
            if (_native is not null) for (var i = 0; i < bus.Effects.Count; i++) _native.EnableBusEffect(bus.Voice, i, bus.Effects[i].Enabled && !enable);
            bus.Bypass = enable;
        }
    }
    internal static void ReportEffectErrors()
    {
        if (!Singleton.IsValueCreated) return; var server = Singleton.Value; if (server._native is null) return; server.Check(); List<Exception>? errors = null;
        lock (server._gate)
        {
            server._native.CollectBusGainErrors(ref errors);
            foreach (var bus in server._buses) if (bus.RuntimeEffects is { } effects)
                    foreach (var effect in effects) if (effect.TakeError() is { } error) Node.CollectException(ref errors, error);
        }
        Node.ThrowCollected("Audio bus effect processing failed; failed effects remain silent until recreated.", errors);
    }
}
