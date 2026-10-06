namespace Electron2D;

public sealed partial class AudioServer
{
    private ResourceFileOwnership? _layoutOwnership;
    /// <summary>Creates a caller-owned snapshot of the current bus configuration.</summary>
    /// <returns>Independent bus/effect containers sharing the current borrowed effect resources.</returns>
    /// <remarks>Requires the audio owner. No native device opens. File-backed dependencies retain their graph
    /// lifetime in the snapshot; effect histories, native voices and playback state are absent.</remarks>
    /// <exception cref="InvalidOperationException">Called from another owner or an audio callback.</exception>
    public static AudioBusLayout GenerateBusLayout() => Service.GenerateBusLayoutCore();
    internal AudioBusLayout GenerateBusLayoutCore()
    {
        Check(); lock (_gate)
        {
            var layout = new AudioBusLayout();
            layout.SetSnapshot(_buses.Select(b => new AudioBusLayout.Bus { Name = b.Name, Send = b.Send, Solo = b.Solo, Mute = b.Mute, Bypass = b.Bypass, VolumeDB = b.VolumeDB, Effects = b.Effects.Select(e => new AudioBusLayout.Effect(e.Resource, e.Enabled)).ToArray() }).ToArray());
            layout.AdoptFileOwnership(_layoutOwnership?.RetainOwner()); return layout;
        }
    }
    /// <summary>Replaces all audio buses with a saved configuration.</summary>
    /// <param name="busLayout">Borrowed live layout; at least one and at most 255 buses.</param>
    /// <remarks>The first bus is named Master. Null effect slots are skipped; names must be unique after that
    /// normalization. Missing/forward sends fall back to Master. New effect instances reset processing histories.
    /// Live streamed/sample playback identity and cursor remain intact. Open-output factories prepare before commit;
    /// closed output retains lazy preparation. Data/factory failure preserves the old
    /// graph. Native graph failure closes output after configuration commits. One BusLayoutChanged notification
    /// follows commitment, including cleanup failures. File graph retention does not own external effect resources.</remarks>
    /// <exception cref="ArgumentNullException">The layout is null.</exception>
    /// <exception cref="ObjectDisposedException">The layout or an effect is disposed.</exception>
    /// <exception cref="ArgumentException">Bus data is empty, duplicate or has invalid gain.</exception>
    /// <exception cref="InvalidOperationException">Owner/reentrancy validation fails.</exception>
    /// <exception cref="AggregateException">Effect preparation, native application, cleanup or notification reports failures.</exception>
    public static void SetBusLayout(AudioBusLayout busLayout) => Service.SetBusLayoutCore(busLayout);
    internal void SetBusLayoutCore(AudioBusLayout layout)
    {
        Check(); ArgumentNullException.ThrowIfNull(layout); var data = layout.Snapshot();
        if (data.Length is < 1 or > 255) throw new ArgumentException("An audio layout requires one through 255 buses.", nameof(layout));
        var names = new HashSet<string>(StringComparer.Ordinal); var replacement = new List<Bus>(data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            var source = data[i]; var name = i == 0 ? "Master" : source.Name;
            if (name is null || source.Send is null || !names.Add(name) || float.IsNaN(source.VolumeDB) || float.IsPositiveInfinity(source.VolumeDB) || !float.IsFinite(Mathf.DBToLinear(source.VolumeDB))) throw new ArgumentException("Audio layout names or gain are invalid.", nameof(layout));
            var bus = new Bus(name) { Send = i == 0 ? "Master" : source.Send, Solo = source.Solo, Mute = source.Mute, Bypass = source.Bypass, VolumeDB = source.VolumeDB };
            foreach (var effect in source.Effects) if (effect.Resource is { } resource) { ObjectDisposedException.ThrowIf(resource.IsDisposed, resource); bus.Effects.Add(new(resource, effect.Enabled)); }
            replacement.Add(bus);
        }
        lock (_gate)
        {
            var retention = layout.RetainFileOwnership();
            try
            {
                if (_native is not null) foreach (var bus in replacement) bus.RuntimeEffects = PrepareEffects(bus.Effects);
                ObjectDisposedException.ThrowIf(layout.IsDisposed, layout);
                foreach (var bus in replacement) foreach (var effect in bus.Effects) ObjectDisposedException.ThrowIf(effect.Resource.IsDisposed, effect.Resource);
            }
            catch (Exception error)
            {
                List<Exception>? failures = null; Node.CollectException(ref failures, error);
                foreach (var bus in replacement) try { ReleaseEffects(bus.RuntimeEffects); } catch (Exception cleanup) { Node.CollectException(ref failures, cleanup); }
                try { ReleaseLayoutOwnership(retention); } catch (Exception cleanup) { Node.CollectException(ref failures, cleanup); }
                Node.ThrowCollected("Audio layout preparation failed.", failures); throw;
            }
            var old = _buses.ToArray(); var oldOwnership = _layoutOwnership;
            _buses.Clear(); _buses.AddRange(replacement); _layoutOwnership = retention;
            List<Exception>? errors = null;
            try { RebuildGraph(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            foreach (var bus in old) try { ReleaseEffects(bus.RuntimeEffects); } catch (Exception error) { Node.CollectException(ref errors, error); }
            try { ReleaseLayoutOwnership(oldOwnership); } catch (Exception error) { Node.CollectException(ref errors, error); }
            try { BusLayoutChangedCore?.Invoke(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Audio layout application failed.", errors);
        }
    }
    private void ReleaseLayoutOwnership(ResourceFileOwnership? ownership)
    {
        _effectCallback = true; try { ownership?.ReleaseOwner(); } finally { _effectCallback = false; }
    }
    internal static void LoadDefaultLayout()
    {
        var path = ProjectSettings.GetWithOverride(ProjectSettings.AudioBusesDefaultBusLayout);
        if (path.Length == 0 || !ResourceLoader.Exists<AudioBusLayout>(path)) return;
        using var layout = ResourceLoader.Load<AudioBusLayout>(path, ResourceLoader.CacheMode.Ignore);
        SetBusLayout(layout);
    }
}
