namespace Electron2D;

internal static partial class ResourceArchive
{
    internal static ResourceFileType ResolveType(string id, ResourceArchiveRole role, bool abort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if ((byte)role > 1) throw new InvalidDataException("Invalid archive resource role.");
        if (ResourceFileTypes.Find(id) is { } known)
        {
            if (!typeof(Resource).IsAssignableFrom(known.Type) || (typeof(Mesh).IsAssignableFrom(known.Type) ? ResourceArchiveRole.Mesh : ResourceArchiveRole.Resource) != role) throw new InvalidDataException("Resource factory role mismatch.");
            return known;
        }
        if (abort) throw new InvalidDataException("Unknown resource factory ID.");
        return new(id, role == ResourceArchiveRole.Mesh ? typeof(PlaceholderMesh) : typeof(MissingResource), () => CreateMissing(id, role, default));
    }
    internal static Resource CreateMissing(string id, ResourceArchiveRole role, Rect2 bounds)
    {
        Resource result = role == ResourceArchiveRole.Mesh ? new PlaceholderMesh { AABB = bounds } : new MissingResource();
        MissingResourceData.For(result)!.OriginalClass = id;
        return result;
    }
    internal static ArchiveDefinition ReadDefinition(StreamPeerBuffer stream, int version, bool abort)
    {
        var id = ResourceArchiveStrings.Read(stream);
        var role = version >= 2 ? (ResourceArchiveRole)stream.GetU8() : typeof(Mesh).IsAssignableFrom(ResourceFileTypes.Get(id).Type) ? ResourceArchiveRole.Mesh : ResourceArchiveRole.Resource;
        var bounds = version >= 2 && role == ResourceArchiveRole.Mesh ? new Rect2(stream.GetFloat(), stream.GetFloat(), stream.GetFloat(), stream.GetFloat()) : default;
        if (!bounds.IsFinite() || !bounds.End.IsFinite() || bounds.Size.X < 0 || bounds.Size.Y < 0) throw new InvalidDataException("Invalid stored mesh bounds.");
        var type = ResolveType(id, role, version == 1 || abort);
        var name = ResourceArchiveStrings.Read(stream); var marker = stream.GetU8(); if (marker > 1) throw new InvalidDataException("Invalid resource metadata flag.");
        var sceneID = ResourceArchiveStrings.Read(stream); var path = ResourceArchiveStrings.Read(stream); var uid = stream.Get64(); var size = stream.Get32();
        if (size < 0 || size > Limit || size > stream.GetAvailableBytes() || uid < -1 || path.Length != 0 && size != 0) throw new InvalidDataException("Invalid resource definition.");
        return new(type, name, marker == 1, sceneID, path, uid, stream.GetData(size), role, bounds);
    }
    internal static void WriteRole(StreamPeerBuffer stream, ArchiveDefinition definition)
    {
        stream.PutU8((byte)definition.Role);
        if (definition.Role == ResourceArchiveRole.Mesh) { stream.PutFloat(definition.Bounds.Position.X); stream.PutFloat(definition.Bounds.Position.Y); stream.PutFloat(definition.Bounds.Size.X); stream.PutFloat(definition.Bounds.Size.Y); }
    }
}

internal sealed partial class ResourceArchiveWrite
{
    private void WriteMissing(StreamPeerBuffer stream, MissingResourceData data)
    {
        var fields = data.Fields.Values.Where(f => (flags & SaverFlags.OmitEditorProperties) == 0 || !f.Name.StartsWith("__editor", StringComparison.Ordinal)).ToArray();
        stream.Put32(fields.Length);
        foreach (var f in fields)
        {
            ResourceArchiveStrings.Write(stream, f.Name); ResourceArchiveStrings.Write(stream, f.Schema); stream.PutU8((byte)f.Role); stream.PutU8(f.Role == ResourceFieldRole.Scalar && f.BigEndian || f.Role != ResourceFieldRole.Scalar && BigEndian ? (byte)1 : (byte)0);
            using var value = NewStream();
            if (f.Role == ResourceFieldRole.Scalar) value.PutData(f.Bytes);
            else if (f.Role == ResourceFieldRole.Resource) value.Put32(Reference(f.References[0]));
            else if (f.Role == ResourceFieldRole.ResourceArray) { value.Put32(f.NullArray ? -1 : f.References.Length); foreach (var r in f.References) value.Put32(Reference(r)); }
            else throw new InvalidDataException("Missing resource fields cannot contain node references.");
            var bytes = value.DataArray; stream.Put32(bytes.Length); stream.PutData(bytes);
        }
    }
}

internal sealed partial class ResourceArchiveRead
{
    private void ReadMissing(StreamPeerBuffer stream, MissingResourceData data)
    {
        if (version < 2) throw new InvalidDataException("Missing factory recovery requires role-aware archive version 2.");
        var count = Count(stream); data.Recording = true;
        try
        {
            for (var i = 0; i < count; i++)
            {
                var name = ResourceArchiveStrings.Read(stream); var schema = ResourceArchiveStrings.Read(stream); var role = (ResourceFieldRole)stream.GetU8(); var endian = stream.GetU8(); var size = Count(stream, ResourceArchive.Limit);
                if ((byte)role > 2 || endian > 1 || size > stream.GetAvailableBytes() || data.Fields.ContainsKey(name)) throw new InvalidDataException("Invalid missing resource field.");
                using var value = new StreamPeerBuffer { DataArray = stream.GetData(size), BigEndian = endian == 1 };
                byte[] bytes = []; Resource?[] references = []; var nullArray = false;
                if (role == ResourceFieldRole.Scalar) { bytes = value.DataArray; value.Seek(value.GetSize()); }
                else if (role == ResourceFieldRole.Resource) references = [Reference(value.Get32())];
                else { var length = value.Get32(); if (length < -1 || length > 65536 || length * 4L > value.GetAvailableBytes()) throw new InvalidDataException("Invalid recorded resource array."); nullArray = length == -1; references = new Resource?[Math.Max(0, length)]; for (var j = 0; j < references.Length; j++) references[j] = Reference(value.Get32()); }
                if (value.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing recorded reference data.");
                if (ResourceFileTypes.FindValueType(schema) is { } known && MissingResourceData.Role(known) != role) throw new InvalidDataException("Recorded field role does not match its schema.");
                data.Put(new(name, schema, role, endian == 1, bytes, references, nullArray));
            }
        }
        finally { data.Recording = false; }
    }
}
