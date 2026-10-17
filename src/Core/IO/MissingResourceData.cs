namespace Electron2D;

internal enum ResourceFieldRole : byte { Scalar, Resource, ResourceArray, Node }
internal enum ResourceArchiveRole : byte { Resource, Mesh, Texture }
internal sealed class MissingResourceData
{
    internal static MissingResourceData? For(Resource resource) => resource switch { MissingResource missing => missing.Data, PlaceholderMesh mesh => mesh.Data, PlaceholderTexture texture => texture.Data, _ => null };
    internal sealed record Field(string Name, string Schema, ResourceFieldRole Role, bool BigEndian, byte[] Bytes, Resource?[] References, bool NullArray);
    internal readonly Dictionary<string, Field> Fields = new(StringComparer.Ordinal);
    private long _bytes;
    internal string OriginalClass = "";
    internal bool MissingPayload;
    internal bool Recording;
    internal static ResourceFieldRole Role(Type type) => typeof(Node).IsAssignableFrom(type) ? ResourceFieldRole.Node : typeof(Resource).IsAssignableFrom(type) ? ResourceFieldRole.Resource : typeof(Resource[]).IsAssignableFrom(type) ? ResourceFieldRole.ResourceArray : ResourceFieldRole.Scalar;
    internal void Put(Field field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field.Name);
        if (field.Name.Length > 65536 || field.Schema.Length > 65536 || field.Bytes.Length > ResourceArchive.Limit || Fields.Count >= 65536 && !Fields.ContainsKey(field.Name)) throw new InvalidDataException("Missing resource field budget exceeded.");
        if (!Recording && !Fields.ContainsKey(field.Name)) throw new InvalidOperationException("Recording must be enabled before adding a field.");
        if (Fields.TryGetValue(field.Name, out var old) && (old.Schema != field.Schema || old.Role != field.Role)) throw new InvalidOperationException("Recorded field schema cannot change.");
        if (field.References.Length > 65536) throw new InvalidDataException("Recorded resource array budget exceeded.");
        if (ResourceFileTypes.FindValueType(field.Schema) is { } known)
        {
            if (Role(known) != field.Role) throw new InvalidDataException("Recorded field role mismatch.");
            var expected = field.Role == ResourceFieldRole.ResourceArray ? known.GetElementType() : field.Role == ResourceFieldRole.Resource ? known : null;
            foreach (var r in field.References) if (r is not null && expected is not null && !expected.IsInstanceOfType(r)) throw new InvalidDataException("Recorded resource reference is incompatible with its known schema.");
            if (field.Role == ResourceFieldRole.Scalar && ResourceFileTypes.FindCodec(known) is { } codec) { using var encoded = new StreamPeerBuffer { DataArray = field.Bytes, BigEndian = field.BigEndian }; codec.ValidateEncoded(encoded); }
        }
        foreach (var resource in field.References) if (resource is { IsDisposed: true }) throw new ObjectDisposedException(nameof(resource));
        var bytes = _bytes;
        if (Fields.TryGetValue(field.Name, out var previous)) bytes -= previous.Bytes.Length + previous.References.Length * 8L + (previous.Name.Length + previous.Schema.Length) * 2L;
        if (bytes + field.Bytes.Length + field.References.Length * 8L + (field.Name.Length + field.Schema.Length) * 2L > ResourceArchive.Limit) throw new InvalidDataException("Missing resource byte budget exceeded.");
        Fields[field.Name] = field;
        _bytes = bytes + field.Bytes.Length + field.References.Length * 8L + (field.Name.Length + field.Schema.Length) * 2L;
    }
    internal void Set<T>(string name, T value)
        => Put(Capture(name, value));

    internal static Field Capture<T>(string name, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var role = Role(typeof(T)); if (role == ResourceFieldRole.Node) throw new NotSupportedException("Node references require scene endpoint storage.");
        byte[] bytes = []; Resource?[] references = []; var nullArray = false;
        if (role == ResourceFieldRole.Resource) references = [value as Resource];
        else if (role == ResourceFieldRole.ResourceArray) { nullArray = value is null; references = value is Resource[] resources ? resources.Length <= 65536 ? (Resource?[])resources.Clone() : throw new InvalidDataException("Recorded resource array budget exceeded.") : []; }
        else { using var stream = new StreamPeerBuffer(); ResourceFileTypes.Codec<T>().Write(stream, value); bytes = stream.DataArray; }
        return new(name, ResourceFileTypes.ValueID(typeof(T)), role, false, bytes, references, nullArray);
    }
    internal T Get<T>(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Fields.TryGetValue(name, out var field)) throw new KeyNotFoundException("No recorded field has that name.");
        if (field.Schema != ResourceFileTypes.ValueID(typeof(T)) || field.Role != Role(typeof(T))) throw new InvalidOperationException("Recorded field type does not match the requested type.");
        if (field.Role == ResourceFieldRole.Resource) { var resource = field.References[0]; if (resource is not null && resource is not T) throw new InvalidOperationException("Missing resource does not satisfy the requested concrete type."); return resource is null ? default! : (T)(object)resource; }
        if (field.Role == ResourceFieldRole.ResourceArray) { using var stream = new StreamPeerBuffer(); stream.Put32(field.NullArray ? -1 : field.References.Length); for (var i = 0; i < field.References.Length; i++) stream.Put32(i); stream.Seek(0); return (T)(object)ResourceFileTypes.ResourceArray(typeof(T)).ReadReferences(stream, field.References); }
        using var value = new StreamPeerBuffer { DataArray = field.Bytes, BigEndian = field.BigEndian }; var result = ResourceFileTypes.Codec<T>().Read(value); if (value.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing recorded scalar bytes."); return result;
    }
    internal MissingResourceData Copy(Func<Resource?, Resource?> duplicate)
    {
        var result = new MissingResourceData { OriginalClass = OriginalClass, MissingPayload = MissingPayload, Recording = Recording, _bytes = _bytes };
        foreach (var f in Fields.Values) result.Fields.Add(f.Name, f with { Bytes = (byte[])f.Bytes.Clone(), References = f.References.Select(duplicate).ToArray() });
        return result;
    }
    internal void Remap(Func<Resource, Resource> remap)
    {
        foreach (var f in Fields.Values.ToArray()) Fields[f.Name] = f with { References = f.References.Select(r => r is null ? null : remap(r)).ToArray() };
    }
    internal IEnumerable<PropertyDescriptor> Descriptors(MissingResource owner)
    {
        foreach (var f in Fields.Values)
        {
            if (f.Name is nameof(MissingResource.OriginalClass) or nameof(MissingResource.RecordingProperties) or nameof(Resource.ResourceName) or nameof(Resource.ResourcePath) or nameof(Resource.ResourceLocalToScene) or nameof(Resource.ResourceSceneUniqueID)) continue;
            if (f.Role == ResourceFieldRole.Resource) yield return new PropertyDescriptor<MissingResource, Resource?>(f.Name, r => r.Data.Fields[f.Name].References[0], (r, v) => { var old = r.Data.Fields[f.Name]; r.Data.Put(old with { References = [v] }); r.EmitChanged(); }, _ => null);
            else if (f.Role == ResourceFieldRole.ResourceArray) yield return new PropertyDescriptor<MissingResource, Resource?[]>(f.Name, r => r.Data.Fields[f.Name].NullArray ? null! : (Resource?[])r.Data.Fields[f.Name].References.Clone(), (r, v) => { var old = r.Data.Fields[f.Name]; r.Data.Put(old with { References = v is null ? [] : (Resource?[])v.Clone(), NullArray = v is null }); r.EmitChanged(); }, _ => []);
            else yield return new PropertyDescriptor<MissingResource, byte[]>(f.Name, r => (byte[])r.Data.Fields[f.Name].Bytes.Clone(), (r, v) => { ArgumentNullException.ThrowIfNull(v); var old = r.Data.Fields[f.Name]; r.Data.Put(old with { Bytes = (byte[])v.Clone() }); r.EmitChanged(); }, _ => []);
        }
    }
}
