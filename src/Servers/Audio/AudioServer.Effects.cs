namespace Electron2D;

public sealed partial class AudioServer
{
    private sealed record BusEffect(AudioEffect Resource, bool Enabled = true);
    private bool _effectCallback;
    [ThreadStatic] private static int _audioProcessing;
    internal static void EnterAudioProcessing() => _audioProcessing++;
    internal static void ExitAudioProcessing() => _audioProcessing--;
    private void CheckAudioReentrancy() { if (_audioProcessing != 0 || Environment.CurrentManagedThreadId == _owner && _effectCallback) throw new InvalidOperationException("Audio callbacks cannot reenter audio configuration."); }
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
    /// <summary>Inserts an enabled borrowed effect resource into a bus's ordered chain.</summary>
    /// <param name="busIndex">Live bus index, including Master.</param>
    /// <param name="effect">Live resource; each stereo pair receives independent instance state.</param>
    /// <param name="atPosition">Insertion index; negative or at/after the end appends.</param>
    /// <remarks>Prepares output and fresh instances for the complete edited chain. Factory failure preserves
    /// the chain; a native graph failure closes output with committed configuration retained for retry.</remarks>
    /// <exception cref="ArgumentNullException">The resource is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/reentrant or preparation fails.</exception>
    public void AddBusEffect(int busIndex, AudioEffect effect, int atPosition = -1)
    {
        var bus = GetBus(busIndex); ArgumentNullException.ThrowIfNull(effect); ObjectDisposedException.ThrowIf(effect.IsDisposed, effect);
        var records = new List<BusEffect>(bus.Effects); if (atPosition < 0 || atPosition >= records.Count) atPosition = records.Count; records.Insert(atPosition, new(effect)); EditEffects(bus, records);
    }
    /// <summary>Gets the number of effects in a bus's configured chain.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <returns>The count, including disabled effects.</returns>
    public int GetBusEffectCount(int busIndex) => GetBus(busIndex).Effects.Count;
    /// <summary>Gets a borrowed configured effect resource.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <returns>The exact resource supplied to AddBusEffect.</returns>
    public AudioEffect GetBusEffect(int busIndex, int effectIndex) => GetEffect(GetBus(busIndex), effectIndex).Resource;
    /// <summary>Gets a borrowed live effect instance for an output stereo pair.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <param name="channel">Stereo pair, zero by default.</param>
    /// <returns>Bus-owned state; structural effect edits/output closure invalidate it. Routing edits retain it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid.</exception>
    public AudioEffectInstance GetBusEffectInstance(int busIndex, int effectIndex, int channel = 0)
    {
        var bus = GetBus(busIndex); _ = GetEffect(bus, effectIndex); EnsureNative();
        var instances = bus.RuntimeEffects![effectIndex].Instances; if ((uint)channel >= (uint)instances.Length) throw new ArgumentOutOfRangeException(nameof(channel)); return instances[channel];
    }
    /// <summary>Removes an effect and recreates the edited chain's processing state.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <remarks>The removed resource remains caller-owned; old instances are disposed after native detachment.</remarks>
    public void RemoveBusEffect(int busIndex, int effectIndex) { var bus = GetBus(busIndex); _ = GetEffect(bus, effectIndex); var records = new List<BusEffect>(bus.Effects); records.RemoveAt(effectIndex); EditEffects(bus, records); }
    /// <summary>Swaps two configured effects and recreates their bus's complete processing state.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">First effect.</param>
    /// <param name="byEffectIndex">Second effect; equal indices do nothing.</param>
    public void SwapBusEffects(int busIndex, int effectIndex, int byEffectIndex)
    {
        var bus = GetBus(busIndex); _ = GetEffect(bus, effectIndex); _ = GetEffect(bus, byEffectIndex); if (effectIndex == byEffectIndex) return;
        var records = new List<BusEffect>(bus.Effects); (records[effectIndex], records[byEffectIndex]) = (records[byEffectIndex], records[effectIndex]); EditEffects(bus, records);
    }
    /// <summary>Gets an effect's requested enabled flag independently of bus bypass.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <returns>True initially.</returns>
    public bool IsBusEffectEnabled(int busIndex, int effectIndex) => GetEffect(GetBus(busIndex), effectIndex).Enabled;
    /// <summary>Enables or disables an effect without recreating its state.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <param name="enabled">Requested flag; bypass still suppresses processing.</param>
    public void SetBusEffectEnabled(int busIndex, int effectIndex, bool enabled)
    {
        var bus = GetBus(busIndex); var effect = GetEffect(bus, effectIndex); if (effect.Enabled == enabled) return;
        lock (_gate) { if (_native is not null) _native.EnableBusEffect(bus.Voice, effectIndex, enabled && !bus.Bypass); bus.Effects[effectIndex] = effect with { Enabled = enabled }; }
    }
    /// <summary>Gets whether a bus bypasses its public effects.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <returns>False initially; gain and peak metering remain active.</returns>
    public bool IsBusBypassingEffects(int busIndex) => GetBus(busIndex).Bypass;
    /// <summary>Bypasses or restores a bus's public effects while retaining instance state/enabled flags.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="enable">True to bypass.</param>
    public void SetBusBypassEffects(int busIndex, bool enable)
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
