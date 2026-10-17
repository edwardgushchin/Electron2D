namespace Electron2D;

internal sealed class DeferredPropertyValue(MissingResourceData.Field field) : StoredPropertyValue
{
    internal MissingResourceData.Field Field { get; } = field;
    internal override Type ValueType => ResourceFileTypes.FindValueType(Field.Schema) ?? Field.Role switch
    {
        ResourceFieldRole.Resource => typeof(Resource),
        ResourceFieldRole.ResourceArray => typeof(Resource[]),
        ResourceFieldRole.Node => typeof(Node),
        _ => typeof(byte[])
    };
    internal override bool HasResourceReferences => Field.Role is ResourceFieldRole.Resource or ResourceFieldRole.ResourceArray;
    internal string? ReadNodePath()
    {
        using var stream = new StreamPeerBuffer { DataArray = Field.Bytes, BigEndian = Field.BigEndian };
        var path = ResourceFileTypes.Codec<string>().Read(stream);
        if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing deferred node path bytes.");
        return path;
    }
    internal override bool TryGetValue<TValue>(out TValue value)
    {
        if (Field.Role == ResourceFieldRole.Node)
        {
            if (typeof(TValue) == typeof(string)) { value = (TValue)(object?)ReadNodePath()!; return true; }
        }
        else if (Field.Role == ResourceFieldRole.Resource)
        {
            var resource = Field.References[0];
            if (resource is TValue typed) { value = typed; return true; }
            var declared = ResourceFileTypes.FindValueType(Field.Schema) ?? typeof(Resource);
            if (resource is null && default(TValue) is null && (typeof(TValue).IsAssignableFrom(declared) || Field.Schema == ResourceFileTypes.ValueID(typeof(TValue)))) { value = default!; return true; }
        }
        else if (Field.Role == ResourceFieldRole.ResourceArray && typeof(Resource[]).IsAssignableFrom(typeof(TValue)))
        {
            var declared = ResourceFileTypes.FindValueType(Field.Schema) ?? typeof(TValue);
            if (typeof(TValue).IsAssignableFrom(declared))
            {
                using var stream = new StreamPeerBuffer(); stream.Put32(Field.NullArray ? -1 : Field.References.Length);
                for (var i = 0; i < Field.References.Length; i++) stream.Put32(i);
                stream.Seek(0); value = (TValue)(object)ResourceFileTypes.ResourceArray(declared).ReadReferences(stream, Field.References); return true;
            }
        }
        else if (Field.Role == ResourceFieldRole.Scalar)
        {
            var declared = ResourceFileTypes.FindValueType(Field.Schema);
            if (declared is not null && ResourceFileTypes.FindCodec(declared) is { } codec)
            {
                using var stream = new StreamPeerBuffer { DataArray = Field.Bytes, BigEndian = Field.BigEndian };
                var stored = codec.ReadStoredValue(stream);
                if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing deferred value bytes.");
                return stored.TryGetValue(out value);
            }
            if (Field.Schema == ResourceFileTypes.ValueID(typeof(TValue)))
            {
                using var stream = new StreamPeerBuffer { DataArray = Field.Bytes, BigEndian = Field.BigEndian };
                value = ResourceFileTypes.Codec<TValue>().Read(stream);
                if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing deferred value bytes.");
                return true;
            }
        }
        value = default!; return false;
    }
    internal override MissingResourceData.Field CaptureField(string name) => Field with { Name = name };
    internal override StoredPropertyValue TransformResources(Func<Resource, Resource> transform) => new DeferredPropertyValue(
        Field with { References = Field.References.Select(resource => resource is null ? null : transform(resource)).ToArray() });
}
