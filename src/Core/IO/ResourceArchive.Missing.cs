namespace Electron2D;

internal static partial class ResourceArchive
{
    internal static ResourceArchiveRole RoleFor(Type type, int version = 4) => typeof(Mesh).IsAssignableFrom(type) ? ResourceArchiveRole.Mesh : version >= 4 && typeof(Texture).IsAssignableFrom(type) ? ResourceArchiveRole.Texture : ResourceArchiveRole.Resource;
    internal static Type MissingType(ResourceArchiveRole role) => role switch { ResourceArchiveRole.Mesh => typeof(PlaceholderMesh), ResourceArchiveRole.Texture => typeof(PlaceholderTexture), _ => typeof(MissingResource) };
    internal static Type RoleType(ResourceArchiveRole role) => role switch { ResourceArchiveRole.Mesh => typeof(Mesh), ResourceArchiveRole.Texture => typeof(Texture), _ => typeof(Resource) };
    internal static Rect2 BoundsFor(Resource resource)
    {
        if (resource is Mesh mesh) return mesh.GetAABB();
        if (resource is not Texture texture) return default;
        var size = PlaceholderTexture.ToPixelSize(texture.GetSize());
        return new(0, 0, size.X, size.Y);
    }
    internal static ResourceFileType ResolveType(string id, ResourceArchiveRole role, bool abort, bool missingPayload = false, int version = 4)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if ((byte)role > 2) throw new InvalidDataException("Invalid archive resource role.");
        var known = ResourceFileTypes.Find(id);
        if (known is not null && (!typeof(Resource).IsAssignableFrom(known.Type) || RoleFor(known.Type, version) != role)) throw new InvalidDataException("Resource factory role mismatch.");
        if (known is not null && !missingPayload) return known;
        if (abort) throw new InvalidDataException(missingPayload ? "The archive retains metadata for an unavailable resource payload." : "Unknown resource factory ID.");
        return new(id, MissingType(role), () => CreateMissing(id, role, default, missingPayload));
    }
    internal static Resource CreateMissing(string id, ResourceArchiveRole role, Rect2 bounds, bool missingPayload = false)
    {
        Resource result = role switch { ResourceArchiveRole.Mesh => new PlaceholderMesh { AABB = bounds }, ResourceArchiveRole.Texture => new PlaceholderTexture { Size = bounds.Size }, _ => new MissingResource() };
        var data = MissingResourceData.For(result)!; data.OriginalClass = id; data.MissingPayload = missingPayload;
        return result;
    }
    internal static ArchiveDefinition ReadDefinition(StreamPeerBuffer stream, int version, bool abort)
    {
        var id = ResourceArchiveStrings.Read(stream);
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidDataException("Invalid resource factory ID.");
        var (role, bounds, missing) = ReadRoleMetadata(stream, version);
        if (version == 1) role = RoleFor(ResourceFileTypes.Get(id).Type, version);
        var type = ResolveType(id, role, version == 1 || abort, missing, version);
        var name = ResourceArchiveStrings.Read(stream); var marker = stream.GetU8(); if (marker > 1) throw new InvalidDataException("Invalid resource metadata flag.");
        var sceneID = ResourceArchiveStrings.Read(stream); var path = ResourceArchiveStrings.Read(stream); var uid = stream.Get64(); var size = stream.Get32();
        if (size < 0 || size > Limit || size > stream.GetAvailableBytes() || uid < -1 || path.Length != 0 && (size != 0 || missing)) throw new InvalidDataException("Invalid resource definition.");
        return new(type, name, marker == 1, sceneID, path, uid, stream.GetData(size), role, bounds, missing);
    }
    internal static (ResourceArchiveRole Role, Rect2 Bounds, bool MissingPayload) ReadRoleMetadata(StreamPeerBuffer stream, int version)
    {
        if (version < 2) return (ResourceArchiveRole.Resource, default, false);
        var role = (ResourceArchiveRole)stream.GetU8();
        if ((byte)role > (version >= 4 ? 2 : 1)) throw new InvalidDataException("Invalid archive resource role.");
        var missing = version >= 4 ? stream.GetU8() : (byte)0;
        if (missing > 1) throw new InvalidDataException("Invalid missing-payload marker.");
        var bounds = role == ResourceArchiveRole.Texture ? new Rect2(0, 0, stream.GetFloat(), stream.GetFloat()) : role == ResourceArchiveRole.Mesh ? new Rect2(stream.GetFloat(), stream.GetFloat(), stream.GetFloat(), stream.GetFloat()) : default;
        if (role == ResourceArchiveRole.Texture)
        {
            try { _ = PlaceholderTexture.ToPixelSize(bounds.Size); }
            catch (ArgumentOutOfRangeException error) { throw new InvalidDataException("Invalid stored texture dimensions.", error); }
        }
        else if (!bounds.IsFinite() || !bounds.End.IsFinite() || bounds.Size.X < 0 || bounds.Size.Y < 0) throw new InvalidDataException("Invalid stored mesh bounds.");
        return (role, bounds, missing == 1);
    }
    internal static (int Reference, int Root, string Path) ReadSceneLink(StreamPeerBuffer stream, int version, int index, bool[] linkedRoots, int resources)
    {
        if (version < 5) return (-1, -1, string.Empty);
        var reference = stream.Get32(); var root = stream.Get32(); var path = ResourceArchiveStrings.Read(stream);
        if (reference < -1 || reference >= resources || reference >= 0 && (index == 0 || root >= 0) ||
            root < -1 || root >= index || root >= 0 && (!linkedRoots[root] || path.Length == 0 || path.Split('/').Any(part => !Node.IsValidStoredNodeName(part))) ||
            root < 0 && path.Length != 0) throw new InvalidDataException("Invalid linked scene record.");
        linkedRoots[index] = reference >= 0;
        return (reference, root, path);
    }
    internal static void WriteRole(StreamPeerBuffer stream, ArchiveDefinition definition, int version = 4)
    {
        stream.PutU8((byte)definition.Role);
        if (version >= 4) stream.PutU8(definition.MissingPayload ? (byte)1 : (byte)0);
        if (definition.Role == ResourceArchiveRole.Mesh) { stream.PutFloat(definition.Bounds.Position.X); stream.PutFloat(definition.Bounds.Position.Y); stream.PutFloat(definition.Bounds.Size.X); stream.PutFloat(definition.Bounds.Size.Y); }
        else if (definition.Role == ResourceArchiveRole.Texture) { stream.PutFloat(definition.Bounds.Size.X); stream.PutFloat(definition.Bounds.Size.Y); }
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
