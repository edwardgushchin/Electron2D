namespace Electron2D;

/// <summary>Stores a reusable snapshot of audio buses, routing, controls and ordered effect resources.</summary>
/// <remarks>Create snapshots with AudioServer.GenerateBusLayout and apply them with AudioServer.SetBusLayout.
/// Configuration containers are independent; effect resources are borrowed. Shallow copies retain effect identity,
/// deep copies follow Resource graph policy. Typed stored descriptors support resource archives without storing
/// native voices, processing histories or active playback. A new layout contains only the default Master bus.</remarks>
public sealed class AudioBusLayout : Resource
{
    internal sealed record Effect(AudioEffect? Resource, bool Enabled);
    internal sealed class Bus
    {
        internal string Name = "", Send = "";
        internal bool Solo, Mute, Bypass;
        internal float VolumeDB;
        internal Effect[] Effects = [];
        internal Bus Copy() => new() { Name = Name, Send = Send, Solo = Solo, Mute = Mute, Bypass = Bypass, VolumeDB = VolumeDB, Effects = (Effect[])Effects.Clone() };
    }
    private readonly object _gate = new();
    private Bus[] _buses = [new() { Name = "Master" }];
    /// <summary>Creates a layout containing one unity-gain Master bus and no effects.</summary>
    public AudioBusLayout() { }
    internal Bus[] Snapshot() { lock (_gate) { ThrowIfDisposed(); return _buses.Select(b => b.Copy()).ToArray(); } }
    internal void SetSnapshot(Bus[] buses) { lock (_gate) { ThrowIfDisposed(); _buses = buses; } }
    private T Read<T>(Func<Bus[], T> read) { lock (_gate) { ThrowIfDisposed(); return read(_buses); } }
    private void Write(Action<Bus[]> write) { lock (_gate) { ThrowIfDisposed(); write(_buses); } }
    private void Resize(int count)
    {
        if ((uint)count > 255) throw new ArgumentOutOfRangeException(nameof(count));
        lock (_gate) { ThrowIfDisposed(); var old = _buses.Length; Array.Resize(ref _buses, count); for (var i = old; i < count; i++) _buses[i] = new(); }
    }
    private void ResizeEffects(int bus, int count)
    {
        if ((uint)count > 65536) throw new ArgumentOutOfRangeException(nameof(count));
        Write(buses => { var old = buses[bus].Effects.Length; Array.Resize(ref buses[bus].Effects, count); for (var i = old; i < count; i++) buses[bus].Effects[i] = new(null, false); });
    }
    /// <summary>Creates independent default configuration storage for inherited duplication.</summary>
    /// <returns>A new caller-owned layout with no native state.</returns>
    protected override Resource CreateDuplicateInstance() => new AudioBusLayout();
    /// <summary>Copies bus containers and resolves each effect through the resource copy session.</summary>
    /// <param name="target">The destination AudioBusLayout.</param>
    /// <param name="deep">Whether the inherited session performs deep copying.</param>
    /// <param name="subresourceMode">The inherited child-resource policy.</param>
    /// <param name="duplicateSubresource">The session callback preserving effect aliases and copy policy.</param>
    /// <param name="forceDuplicateSubresource">The session callback for forced copies; not needed by this layout.</param>
    /// <remarks>Only configuration is copied. Effect instances, playback and native voices are absent.</remarks>
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var buses = Snapshot(); foreach (var bus in buses) for (var i = 0; i < bus.Effects.Length; i++) bus.Effects[i] = bus.Effects[i] with { Resource = (AudioEffect?)duplicateSubresource(bus.Effects[i].Resource) };
        ((AudioBusLayout)target).SetSnapshot(buses);
    }
    /// <summary>Adds typed stored bus and effect configuration fields after inherited metadata.</summary>
    /// <returns>Cold allocating descriptors with count fields preceding their indexed values.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        var result = new List<PropertyDescriptor>(base.GetPropertyDescriptors()) { new PropertyDescriptor<AudioBusLayout, int>("BusCount", p => p.Read(b => b.Length), (p, v) => p.Resize(v), _ => 1, stored: true) };
        var buses = Snapshot();
        for (var i = 0; i < buses.Length; i++)
        {
            var index = i; var prefix = $"Bus_{i}/";
            result.Add(new PropertyDescriptor<AudioBusLayout, string>(prefix + "Name", p => p.Read(b => b[index].Name), (p, v) => { ArgumentNullException.ThrowIfNull(v); p.Write(b => b[index].Name = v); }, _ => index == 0 ? "Master" : "", stored: true));
            result.Add(new PropertyDescriptor<AudioBusLayout, bool>(prefix + "Solo", p => p.Read(b => b[index].Solo), (p, v) => p.Write(b => b[index].Solo = v), _ => false, stored: true));
            result.Add(new PropertyDescriptor<AudioBusLayout, bool>(prefix + "Mute", p => p.Read(b => b[index].Mute), (p, v) => p.Write(b => b[index].Mute = v), _ => false, stored: true));
            result.Add(new PropertyDescriptor<AudioBusLayout, bool>(prefix + "Bypass", p => p.Read(b => b[index].Bypass), (p, v) => p.Write(b => b[index].Bypass = v), _ => false, stored: true));
            result.Add(new PropertyDescriptor<AudioBusLayout, float>(prefix + "VolumeDB", p => p.Read(b => b[index].VolumeDB), (p, v) => { if (float.IsNaN(v) || float.IsPositiveInfinity(v) || !float.IsFinite(Mathf.DBToLinear(v))) throw new ArgumentOutOfRangeException(nameof(v)); p.Write(b => b[index].VolumeDB = v); }, _ => 0, stored: true));
            result.Add(new PropertyDescriptor<AudioBusLayout, string>(prefix + "Send", p => p.Read(b => b[index].Send), (p, v) => { ArgumentNullException.ThrowIfNull(v); p.Write(b => b[index].Send = v); }, _ => "", stored: true));
            result.Add(new PropertyDescriptor<AudioBusLayout, int>(prefix + "EffectCount", p => p.Read(b => b[index].Effects.Length), (p, v) => p.ResizeEffects(index, v), _ => 0, stored: true));
            for (var e = 0; e < buses[i].Effects.Length; e++)
            {
                var effect = e; var path = prefix + $"Effect_{e}/";
                result.Add(new PropertyDescriptor<AudioBusLayout, AudioEffect?>(path + "Resource", p => p.Read(b => b[index].Effects[effect].Resource), (p, v) => p.Write(b => b[index].Effects[effect] = b[index].Effects[effect] with { Resource = v }), _ => null, stored: true));
                result.Add(new PropertyDescriptor<AudioBusLayout, bool>(path + "Enabled", p => p.Read(b => b[index].Effects[effect].Enabled), (p, v) => p.Write(b => b[index].Effects[effect] = b[index].Effects[effect] with { Enabled = v }), _ => false, stored: true));
            }
        }
        return result;
    }
    /// <summary>Clears borrowed configuration and releases inherited file graph ownership.</summary>
    /// <param name="disposing">Whether deterministic managed cleanup was requested.</param>
    /// <remarks>Effects created outside the internal file graph remain borrowed.</remarks>
    protected override void Dispose(bool disposing) { lock (_gate) _buses = []; base.Dispose(disposing); }
}
