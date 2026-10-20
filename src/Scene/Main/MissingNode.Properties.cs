namespace Electron2D;

public sealed partial class MissingNode
{
    private static readonly PropertyDescriptor[] RecoveryProperties =
    [
        new PropertyDescriptor<MissingNode, string>(nameof(OriginalClass), n => n.OriginalClass, (n, v) => n.OriginalClass = v, _ => ""),
        new PropertyDescriptor<MissingNode, string>(nameof(OriginalScene), n => n.OriginalScene, (n, v) => n.OriginalScene = v, _ => ""),
        new PropertyDescriptor<MissingNode, bool>(nameof(RecordingProperties), n => n.RecordingProperties, (n, v) => n.RecordingProperties = v, _ => false),
        new PropertyDescriptor<MissingNode, bool>(nameof(RecordingSignals), n => n.RecordingSignals, (n, v) => n.RecordingSignals = v, _ => false)
    ];
    /// <summary>Exposes inherited state, transient recovery controls and typed retained field metadata.</summary>
    /// <returns>The current property sequence; retained values preserve archive order.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(RecoveryProperties).Concat(RetainedDescriptors());
    internal static bool IsRecoveryControl(string name) => RecoveryProperties.Any(property => property.Name == name);
    private IEnumerable<PropertyDescriptor> RetainedDescriptors()
    {
        var known = base.GetPropertyDescriptors().Select(p => p.Name).Concat(RecoveryProperties.Select(p => p.Name)).ToHashSet(StringComparer.Ordinal);
        foreach (var field in Data.Fields.Values)
        {
            var name = field.Name; if (known.Contains(name)) continue;
            if (field.Role == ResourceFieldRole.Resource)
                yield return new PropertyDescriptor<MissingNode, Resource?>(name, node => node.GetStoredValue<Resource?>(name),
                    (node, value) => node.SetRecordedField(field with { References = [value] }), stored: true);
            else if (field.Role == ResourceFieldRole.ResourceArray)
                yield return new PropertyDescriptor<MissingNode, Resource?[]?>(name, node => node.RecordedField(name).NullArray ? null : (Resource?[])node.RecordedField(name).References.Clone(),
                    (node, value) => node.SetRecordedField(field with { References = value is null ? [] : (Resource?[])value.Clone(), NullArray = value is null }), stored: true);
            else if (field.Role == ResourceFieldRole.Node)
                yield return new PropertyDescriptor<MissingNode, string>(name, node => node.GetStoredValue<string>(name), (node, value) =>
                {
                    using var stream = new StreamPeerBuffer(); ResourceFileTypes.Codec<string>().Write(stream, value);
                    node.SetRecordedField(field with { Bytes = stream.DataArray, BigEndian = false });
                }, stored: true);
            else if (ResourceFileTypes.FindValueType(field.Schema) is { } type && ResourceFileTypes.FindCodec(type) is { } codec)
                yield return codec.CreateMissingNodeDescriptor(name);
            else yield return new PropertyDescriptor<MissingNode, byte[]>(name, node => (byte[])node.RecordedField(name).Bytes.Clone(),
                (node, value) => node.SetRecordedField(field with { Bytes = (byte[])value.Clone() }), stored: true);
        }
    }
    internal ScenePropertyData[] CaptureRecoveryProperties(ScenePropertyData[] current)
    {
        var result = Data.Fields.Values.Select(field => new ScenePropertyData(field.Name, new DeferredPropertyValue(CapturedField(field)))).ToList();
        foreach (var property in current)
        {
            if (Data.Fields.ContainsKey(property.Name)) continue;
            var existing = result.FindIndex(p => p.Name == property.Name);
            if (existing >= 0) result[existing] = property; else result.Add(property);
        }
        return result.ToArray();
    }
}
