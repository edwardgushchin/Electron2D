namespace Electron2D;

public sealed partial class InstancePlaceholder
{
    /// <summary>Discovers ordinary Node properties and typed pending value descriptors.</summary>
    /// <returns>The current metadata sequence; pending descriptors preserve stored order.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(PendingDescriptors());
    private IEnumerable<PropertyDescriptor> PendingDescriptors()
    {
        foreach (var field in _fields.Fields.Values)
        {
            var name = field.Name;
            if (field.Role == ResourceFieldRole.Resource)
                yield return new PropertyDescriptor<InstancePlaceholder, Resource?>(name, node => node.GetStoredValue<Resource?>(name),
                    (node, value) => node.SetPendingField(name, field with { References = [value] }), stored: true);
            else if (field.Role == ResourceFieldRole.ResourceArray)
                yield return new PropertyDescriptor<InstancePlaceholder, Resource?[]?>(name,
                    node => node._fields.Fields[name].NullArray ? null : (Resource?[])node._fields.Fields[name].References.Clone(),
                    (node, value) => node.SetPendingField(name, field with { References = value is null ? [] : (Resource?[])value.Clone(), NullArray = value is null }), stored: true);
            else if (field.Role == ResourceFieldRole.Node)
                yield return new PropertyDescriptor<InstancePlaceholder, string>(name, node => node.GetStoredValue<string>(name),
                    (node, value) => { using var stream = new StreamPeerBuffer(); ResourceFileTypes.Codec<string>().Write(stream, value); node.SetPendingField(name, field with { Bytes = stream.DataArray, BigEndian = false }); }, stored: true);
            else if (ResourceFileTypes.FindValueType(field.Schema) is { } type && ResourceFileTypes.FindCodec(type) is { } codec)
                yield return codec.CreateDeferredDescriptor(name);
            else yield return new PropertyDescriptor<InstancePlaceholder, byte[]>(name, node => (byte[])node._fields.Fields[name].Bytes.Clone(),
                (node, value) => node.SetPendingField(name, field with { Bytes = (byte[])value.Clone() }), stored: true);
        }
    }
}
