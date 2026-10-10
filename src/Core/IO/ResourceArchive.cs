using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
namespace Electron2D;

internal static class ResourceArchive
{
    internal const int Limit = 67108864;
    internal static bool IsPath(string path) => System.IO.Path.GetExtension(path).Equals(".e2dres", StringComparison.OrdinalIgnoreCase) || System.IO.Path.GetExtension(path).Equals(".e2dscene", StringComparison.OrdinalIgnoreCase);
    internal static string Absolute(string path) => ProjectSettings.GlobalizePath(ResourceUID.EnsurePath(path));
    internal static void AtomicWrite(string path, byte[] bytes)
    {
        var absolute = Absolute(path);
        var temporary = absolute + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(true);
            }
            System.IO.File.Move(temporary, absolute, true);
        }
        finally
        {
            if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary);
        }
    }
    internal static void Save(Resource resource, string path, SaverFlags flags)
    {
        var context = new ResourceArchiveWrite(resource, path, flags);
        var payload = context.Encode();
        if (payload.Length > Limit) throw new InvalidDataException("Resource archive exceeds its byte budget.");
        var encoded = payload;
        if ((flags & SaverFlags.Compress) != 0)
        {
            using var output = new MemoryStream();
            using (var compressor = new DeflateStream(output, CompressionLevel.Optimal, true)) compressor.Write(payload);
            encoded = output.ToArray();
        }
        if (encoded.Length > Limit) throw new InvalidDataException("Encoded archive exceeds its byte budget.");
        var uid = ResourceUID.FindIDForPath(path);
        if (uid == ResourceUID.InvalidID) uid = ReadUID(path);
        if (uid == ResourceUID.InvalidID) uid = ResourceUID.CreateID();
        var file = new byte[checked(64 + encoded.Length)];
        "E2DASSET"u8.CopyTo(file);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(8), 1);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(12), (int)flags);
        BinaryPrimitives.WriteInt64LittleEndian(file.AsSpan(16), uid);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(24), payload.Length);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(28), encoded.Length);
        SHA256.HashData(payload).CopyTo(file, 32);
        encoded.CopyTo(file, 64);
        AtomicWrite(path, file);
        ResourceUID.SetID(uid, path);
        context.CommitPaths();
    }
    internal static long ReadUID(string path)
    {
        var absolute = Absolute(path);
        if (!System.IO.File.Exists(absolute)) return ResourceUID.InvalidID;
        using var stream = System.IO.File.OpenRead(absolute);
        Span<byte> header = stackalloc byte[24];
        if (stream.Read(header) != header.Length || !header[..8].SequenceEqual("E2DASSET"u8)) return ResourceUID.InvalidID;
        if (BinaryPrimitives.ReadInt32LittleEndian(header[8..]) != 1 || stream.Length < 64) throw new InvalidDataException("Unsupported resource UID header.");
        var id = BinaryPrimitives.ReadInt64LittleEndian(header[16..]);
        return id >= 0 ? id : throw new InvalidDataException("Invalid resource UID.");
    }
    internal static void SetUID(string path, long uid)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(uid);
        if (!IsPath(path)) throw new NotSupportedException("Archive format required.");
        _ = ReadPayload(path, out _);
        var bytes = ReadFile(path);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(16), uid);
        AtomicWrite(path, bytes);
        ResourceUID.SetID(uid, path);
    }
    internal static byte[] ReadFile(string path)
    {
        var absolute = Absolute(path);
        var info = new FileInfo(absolute);
        if (info.Length is < 64 or > Limit + 64) throw new InvalidDataException("Invalid resource file size.");
        var bytes = System.IO.File.ReadAllBytes(absolute);
        if (!bytes.AsSpan(0, 8).SequenceEqual("E2DASSET"u8) || BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) != 1) throw new InvalidDataException("Unsupported resource archive version.");
        if (BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(16)) < 0) throw new InvalidDataException("Invalid archive UID.");
        return bytes;
    }
    internal static byte[] ReadPayload(string path, out bool bigEndian)
    {
        var bytes = ReadFile(path);
        var flags = (SaverFlags)BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12));
        if ((flags & ~(SaverFlags)127) != 0) throw new InvalidDataException("Unknown archive flags.");
        var rawSize = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(24));
        var size = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(28));
        if (rawSize is < 0 or > Limit || size != bytes.Length - 64) throw new InvalidDataException("Invalid archive payload size.");
        byte[] payload;
        if ((flags & SaverFlags.Compress) != 0)
        {
            payload = new byte[rawSize];
            using var source = new MemoryStream(bytes, 64, size, false);
            using var decompress = new DeflateStream(source, CompressionMode.Decompress);
            try
            {
                decompress.ReadExactly(payload);
            }
            catch (EndOfStreamException error)
            {
                throw new InvalidDataException("Truncated compressed payload.", error);
            }
            if (decompress.ReadByte() != -1) throw new InvalidDataException("Expanded archive exceeds its declared budget.");
        }
        else
        {
            if (size != rawSize) throw new InvalidDataException("Invalid plain payload size.");
            payload = bytes.AsSpan(64).ToArray();
        }
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(payload), bytes.AsSpan(32, 32))) throw new InvalidDataException("Resource payload checksum failed.");
        bigEndian = (flags & SaverFlags.SaveBigEndian) != 0;
        return payload;
    }
    internal static ResourceArchiveRead Decode(string path, ResourceLoader.CacheMode mode)
    {
        var payload = ReadPayload(path, out var bigEndian);
        var context = new ResourceArchiveRead(path, mode, bigEndian);
        try
        {
            context.Decode(payload);
            return context;
        }
        catch (Exception error)
        {
            var cause = error is EndOfStreamException ? new InvalidDataException("Truncated archive schema.", error) : error;
            try { context.Dispose(); } catch (Exception cleanup) { throw new AggregateException("Archive decode and rollback failed.", cause, cleanup); }
            if (!ReferenceEquals(cause, error)) throw cause;
            throw;
        }
    }
    internal static ArchiveDefinition[] Inspect(string path, out bool bigEndian)
    {
        var payload = ReadPayload(path, out bigEndian);
        using var stream = new StreamPeerBuffer
        {
            DataArray = payload,
            BigEndian = bigEndian
        };
        var count = stream.Get32();
        if (count is < 1 or > 65536) throw new InvalidDataException("Invalid resource table.");
        var definitions = new ArchiveDefinition[count];
        for (var i = 0; i < count; i++)
        {
            var type = ResourceFileTypes.Get(ResourceArchiveStrings.Read(stream));
            if (!typeof(Resource).IsAssignableFrom(type.Type)) throw new InvalidDataException("Resource metadata includes a node type.");
            var name = ResourceArchiveStrings.Read(stream);
            var marker = stream.GetU8();
            if (marker > 1) throw new InvalidDataException("Invalid metadata marker.");
            var sceneID = ResourceArchiveStrings.Read(stream);
            var dependency = ResourceArchiveStrings.Read(stream);
            var uid = stream.Get64();
            var size = stream.Get32();
            if (size < 0 || size > stream.GetAvailableBytes()) throw new InvalidDataException("Invalid metadata payload size.");
            definitions[i] = new(type, name, marker == 1, sceneID, dependency, uid, stream.GetData(size));
        }
        if (stream.GetAvailableBytes() != 0 || definitions[0].Path.Length != 0) throw new InvalidDataException("Invalid archive metadata root or trailing bytes.");
        return definitions;
    }
    internal static void RewriteDependencies(string path, IReadOnlyDictionary<string, string> renames)
    {
        ArgumentNullException.ThrowIfNull(renames);
        var definitions = Inspect(path, out var bigEndian);
        using var output = new StreamPeerBuffer
        {
            BigEndian = bigEndian
        };
        output.Put32(definitions.Length);
        foreach (var d in definitions)
        {
            var target = d.Path;
            var uid = d.UID;
            if (target.Length != 0 && renames.TryGetValue(target, out var replacement))
            {
                ArgumentException.ThrowIfNullOrEmpty(replacement);
                target = replacement;
                uid = ResourceSaver.GetResourceIDForPath(target);
            }
            ResourceArchiveStrings.Write(output, d.Type.ID);
            ResourceArchiveStrings.Write(output, d.Name);
            output.PutU8(d.Local ? (byte)1 : (byte)0);
            ResourceArchiveStrings.Write(output, d.SceneID);
            ResourceArchiveStrings.Write(output, target);
            output.Put64(uid);
            output.Put32(d.Payload.Length);
            output.PutData(d.Payload);
        }
        var payload = output.DataArray;
        if (payload.Length > Limit) throw new InvalidDataException("Dependency rewrite exceeds archive budget.");
        var original = ReadFile(path);
        var flags = (SaverFlags)BinaryPrimitives.ReadInt32LittleEndian(original.AsSpan(12));
        var encoded = payload;
        if ((flags & SaverFlags.Compress) != 0)
        {
            using var buffer = new MemoryStream();
            using (var compress = new DeflateStream(buffer, CompressionLevel.Optimal, true)) compress.Write(payload);
            encoded = buffer.ToArray();
        }
        var file = new byte[64 + encoded.Length];
        original.AsSpan(0, 64).CopyTo(file);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(24), payload.Length);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(28), encoded.Length);
        SHA256.HashData(payload).CopyTo(file, 32);
        encoded.CopyTo(file, 64);
        AtomicWrite(path, file);
    }
    internal static string[] Dependencies(string path, bool addTypes)
    {
        var payload = ReadPayload(path, out var bigEndian);
        using var stream = new StreamPeerBuffer
        {
            DataArray = payload,
            BigEndian = bigEndian
        };
        var count = stream.Get32();
        if (count is < 1 or > 65536) throw new InvalidDataException("Invalid resource table.");
        var dependencies = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var type = ResourceArchiveStrings.Read(stream);
            _ = ResourceArchiveStrings.Read(stream);
            _ = stream.GetU8();
            _ = ResourceArchiveStrings.Read(stream);
            var dependency = ResourceArchiveStrings.Read(stream);
            var uid = stream.Get64();
            var size = stream.Get32();
            if (size < 0 || size > stream.GetAvailableBytes()) throw new InvalidDataException("Invalid resource payload size.");
            stream.Seek(checked(stream.GetPosition() + size));
            if (dependency.Length != 0)
            {
                var token = uid < 0 ? dependency + (addTypes ? "::" + type : "") : ResourceUID.IDToText(uid) + "::" + (addTypes ? type : "") + "::" + dependency;
                dependencies.Add(token);
            }
        }
        if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing metadata bytes.");
        return dependencies.ToArray();
    }

}
internal sealed record ArchiveDefinition(ResourceFileType Type, string Name, bool Local, string SceneID, string Path, long UID, byte[] Payload);
internal sealed class ResourceArchiveWrite(Resource root, string path, SaverFlags flags)
{
    private readonly Dictionary<Resource, int> _ids = new(ReferenceEqualityComparer.Instance);
    private readonly List<Resource> _resources = [];
    private readonly List<Resource> _internal = [];
    private readonly Dictionary<Resource, string> _sceneIDs = new(ReferenceEqualityComparer.Instance);
    private bool BigEndian => (flags & SaverFlags.SaveBigEndian) != 0;
    internal int Reference(Resource? resource)
    {
        if (resource is null) return -1;
        ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
        if (_ids.TryGetValue(resource, out var id)) return id;
        if (_resources.Count == 65536) throw new InvalidDataException("Resource identity budget exceeded.");
        id = _resources.Count;
        _ids.Add(resource, id);
        _resources.Add(resource);
        return id;
    }
    internal byte[] Encode()
    {
        Reference(root);
        var definitions = new List<ArchiveDefinition>();
        for (var i = 0; i < _resources.Count; i++)
        {
            var resource = _resources[i];
            var type = ResourceFileTypes.Get(resource.GetType());
            var external = !ReferenceEquals(resource, root) && !resource.IsBuiltIn && (flags & SaverFlags.BundleResources) == 0;
            if (external)
            {
                var target = resource.ResourcePath;
                var uid = ResourceSaver.GetResourceIDForPath(target);
                if ((flags & SaverFlags.RelativePaths) != 0) target = System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(ResourceArchive.Absolute(path))!, ResourceArchive.Absolute(target));
                definitions.Add(new(type, resource.ResourceName, resource.ResourceLocalToScene, resource.ResourceSceneUniqueID, target, uid, []));
                continue;
            }
            using var stream = NewStream();
            if (resource is PackedScene packed) WriteScene(stream, packed.FileData);
            else if (resource is Image image) WriteImage(stream, image);
            else if (resource is ImageTexture texture)
            {
                using var snapshot = texture.GetImage();
                stream.PutU8(snapshot is null ? (byte)0 : (byte)1);
                if (snapshot is not null)
                {
                    WriteImage(stream, snapshot);
                }
                stream.Put32(texture.GetWidth());
                stream.Put32(texture.GetHeight());
            }
            else if (resource is TextureArray array)
            {
                var pixels = array.CaptureLayers(); stream.Put32(pixels?.Layers.Length ?? 0);
                if (pixels is not null) foreach (var layer in pixels.Layers) { using var layerImage = layer.CopyImage(); WriteImage(stream, layerImage); }
            }
            else WriteProperties(stream, resource, null);
            _internal.Add(resource);
            var sceneID = resource.ResourceSceneUniqueID;
            if (!ReferenceEquals(resource, root) && (flags & SaverFlags.ReplaceSubresourcePaths) != 0 && sceneID.Length == 0) sceneID = Resource.GenerateSceneUniqueID();
            _sceneIDs.Add(resource, sceneID);
            definitions.Add(new(type, resource.ResourceName, resource.ResourceLocalToScene, sceneID, "", -1, stream.DataArray));
        }
        var sceneIDs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in definitions.Skip(1).Where(d => d.Path.Length == 0 && d.SceneID.Length != 0)) if (!sceneIDs.Add(d.SceneID)) throw new InvalidDataException("Duplicate subresource scene ID.");
        using var output = NewStream();
        output.Put32(definitions.Count);
        foreach (var d in definitions)
        {
            ResourceArchiveStrings.Write(output, d.Type.ID);
            ResourceArchiveStrings.Write(output, d.Name);
            output.PutU8(d.Local ? (byte)1 : (byte)0);
            ResourceArchiveStrings.Write(output, d.SceneID);
            ResourceArchiveStrings.Write(output, d.Path);
            output.Put64(d.UID);
            output.Put32(d.Payload.Length);
            output.PutData(d.Payload);
            if (output.GetSize() > ResourceArchive.Limit) throw new InvalidDataException("Archive byte budget exceeded.");
        }
        return output.DataArray;
    }
    private static void WriteImage(StreamPeerBuffer stream, Image image)
    {
        stream.Put32(image.Width);
        stream.Put32(image.Height);
        stream.PutU8(image.HasMipmaps ? (byte)1 : (byte)0);
        stream.Put32((int)image.PixelFormat);
        var data = image.GetData();
        stream.Put32(data.Length);
        stream.PutData(data);
    }
    private StreamPeerBuffer NewStream() => new()
    {
        BigEndian = BigEndian
    };
    private void WriteProperties(StreamPeerBuffer stream, ElectronObject owner, ScenePropertyData[]? captured, bool prepareSchema = false)
    {
        var properties = captured ?? owner.GetPropertyList().Where(p => p.IsStored).Select(p => new ScenePropertyData(p.Name, p.CaptureStoredValue(owner))).ToArray();
        if ((flags & SaverFlags.OmitEditorProperties) != 0) properties = properties.Where(p => !p.Name.StartsWith("__editor", StringComparison.Ordinal)).ToArray();
        stream.Put32(properties.Length);
        var descriptors = owner.GetPropertyList();
        foreach (var property in properties)
        {
            var descriptor = descriptors.FirstOrDefault(p => p.Name == property.Name && p.IsStored && !p.IsReadOnly);
            if (descriptor is null && owner is Node node) descriptor = ThemeOwner.StoredOverride(node, property.Name, property.Value.ValueType);
            if (descriptor is null) throw new InvalidDataException("Stored schema has no matching descriptor.");
            if (descriptor.ValueType != property.Value.ValueType) throw new InvalidDataException("Stored property schema changed.");
            ResourceArchiveStrings.Write(stream, property.Name);
            ResourceArchiveStrings.Write(stream, ResourceFileTypes.ValueID(property.Value.ValueType));
            using var value = NewStream();
            descriptor.WriteFileValue(owner, property.Value, this, value);
            var data = value.DataArray;
            stream.Put32(data.Length);
            stream.PutData(data);
            if (prepareSchema && property.Value is not StoredNodeReferenceValue && !typeof(Resource).IsAssignableFrom(property.Value.ValueType)) descriptor.RestoreStoredValue(owner, property.Value, static r => r);
            descriptors = owner.GetPropertyList();
        }
    }
    private void WriteScene(StreamPeerBuffer stream, PackedSceneData data)
    {
        stream.Put32(data.Nodes.Length);
        foreach (var node in data.Nodes)
        {
            var type = ResourceFileTypes.Get(node.Factory.RuntimeType);
            ResourceArchiveStrings.Write(stream, type.ID);
            ResourceArchiveStrings.Write(stream, node.Name);
            stream.Put32(node.ParentIndex);
            stream.Put32(node.OwnerIndex);
            stream.Put32(node.SiblingIndex);
            stream.Put32(node.Groups.Count);
            foreach (var group in node.Groups) ResourceArchiveStrings.Write(stream, group);
            var temporary = Node.InvokeSceneInstanceFactory(() => (Node)type.Factory());
            try
            {
                WriteProperties(stream, temporary, node.Properties, true);
            }
            finally
            {
                temporary.Dispose();
            }
        }
    }
    internal void CommitPaths()
    {
        if ((flags & SaverFlags.ReplaceSubresourcePaths) == 0) return;
        foreach (var resource in _internal) if (!ReferenceEquals(resource, root))
            {
                var id = _sceneIDs[resource];
                resource.ResourceSceneUniqueID = id;
                resource.SetPathCache(path + "::" + id);
            }
    }

}
internal sealed class ResourceArchiveRead(string path, ResourceLoader.CacheMode mode, bool bigEndian) : IDisposable
{
    internal readonly List<Resource> Owned = [];
    private Resource?[] _resources = [];
    private ArchiveDefinition[] _definitions = [];
    internal Resource Root => _resources[0] ?? throw new InvalidDataException("Missing archive root.");
    internal Resource? Reference(int id) => id == -1 ? null : id >= 0 && id < _resources.Length ? _resources[id] : throw new InvalidDataException("Invalid resource reference.");
    private StreamPeerBuffer Stream(byte[] bytes) => new()
    {
        DataArray = bytes,
        BigEndian = bigEndian
    };
    private static int Count(StreamPeerBuffer stream, int max = 65536)
    {
        var n = stream.Get32();
        if (n < 0 || n > max) throw new InvalidDataException("Archive count exceeds its budget.");
        return n;
    }
    internal void Decode(byte[] payload)
    {
        using var stream = Stream(payload);
        var count = Count(stream);
        if (count == 0) throw new InvalidDataException("Empty resource table.");
        _resources = new Resource?[count];
        _definitions = new ArchiveDefinition[count];
        for (var i = 0; i < count; i++)
        {
            var type = ResourceFileTypes.Get(ResourceArchiveStrings.Read(stream));
            if (!typeof(Resource).IsAssignableFrom(type.Type)) throw new InvalidDataException("Resource table includes a node factory.");
            var name = ResourceArchiveStrings.Read(stream);
            var marker = stream.GetU8();
            if (marker > 1) throw new InvalidDataException("Invalid local-resource flag.");
            var sceneID = ResourceArchiveStrings.Read(stream);
            var dependency = ResourceArchiveStrings.Read(stream);
            var uid = stream.Get64();
            var length = Count(stream, ResourceArchive.Limit);
            if (length > stream.GetAvailableBytes()) throw new InvalidDataException("Truncated resource payload.");
            _definitions[i] = new(type, name, marker == 1, sceneID, dependency, uid, stream.GetData(length));
        }
        if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing archive payload.");
        var sceneIDs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in _definitions.Skip(1)) { if (definition.UID < -1 || definition.Path.Length != 0 && definition.Payload.Length != 0) throw new InvalidDataException("Invalid external resource metadata."); if (definition.Path.Length == 0 && definition.SceneID.Length != 0 && !sceneIDs.Add(definition.SceneID)) throw new InvalidDataException("Duplicate subresource scene ID."); }
        if (_definitions[0].Path.Length != 0) throw new InvalidDataException("Archive root cannot be an external reference.");
        for (var i = 0; i < count; i++)
        {
            var d = _definitions[i];
            if (d.Path.Length != 0)
            {
                var target = d.UID >= 0 && ResourceUID.HasID(d.UID) ? ResourceUID.GetIDPath(d.UID) : d.Path;
                if (!target.Contains("://", StringComparison.Ordinal) && !System.IO.Path.IsPathRooted(target)) target = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ResourceArchive.Absolute(path))!, target);
                var dependencyMode = mode is ResourceLoader.CacheMode.IgnoreDeep or ResourceLoader.CacheMode.ReplaceDeep ? mode : ResourceLoader.CacheMode.Reuse;
                var cachedDependency = Resource.GetRegisteredPath(target);
                var resource = ResourceLoader.LoadFileResource(target, dependencyMode);
                if (!ReferenceEquals(cachedDependency, resource)) Owned.Add(resource);
                if (resource.GetType() != d.Type.Type) throw new InvalidDataException("External resource type does not match its schema.");
                _resources[i] = resource;
                continue;
            }
            var created = d.Type.Factory() as Resource ?? throw new InvalidDataException("Resource factory returned a node.");
            Owned.Add(created);
            if (created.IsDisposed || created.GetType() != d.Type.Type) throw new InvalidDataException("Resource factory returned an invalid identity.");
            _resources[i] = created;
        }
        for (var i = 0; i < count; i++)
        {
            var d = _definitions[i];
            if (d.Path.Length != 0) continue;
            var resource = _resources[i]!;
            resource.ResourceName = d.Name;
            resource.ResourceLocalToScene = d.Local;
            resource.ResourceSceneUniqueID = d.SceneID;
            if (i > 0) resource.SetPathCache(path + "::" + (d.SceneID.Length == 0 ? i.ToString(System.Globalization.CultureInfo.InvariantCulture) : d.SceneID));
            using var value = Stream(d.Payload);
            if (resource is PackedScene packed) packed.LoadFileData(ReadScene(value));
            else if (resource is Image image) ReadImage(value, image);
            else if (resource is ImageTexture texture)
            {
                var marker = value.GetU8();
                if (marker > 1) throw new InvalidDataException("Invalid texture marker.");
                if (marker == 1)
                {
                    using var pixels = new Image();
                    ReadImage(value, pixels);
                    texture.SetImage(pixels);
                }
                texture.SetSizeOverride(new Vector2i(value.Get32(), value.Get32()));
            }
            else if (resource is TextureArray array)
            {
                var layerCount = value.Get32();
                if (layerCount < 0 || layerCount > value.GetAvailableBytes() / 17) throw new InvalidDataException("Invalid stored texture-array layer layerCount.");
                var layers = new Image[layerCount];
                try { for (var layer = 0; layer < layerCount; layer++) { layers[layer] = new Image(); ReadImage(value, layers[layer]); } if (layerCount != 0) array.CreateFromImages(layers); }
                finally { foreach (var layer in layers) layer?.Dispose(); }
            }
            else _ = ReadProperties(value, resource);
            if (value.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing resource bytes.");
        }
    }
    private static void ReadImage(StreamPeerBuffer stream, Image image)
    {
        var width = stream.Get32();
        var height = stream.Get32();
        var mip = stream.GetU8();
        var format = (Image.Format)stream.Get32();
        var count = Count(stream, ResourceArchive.Limit);
        if (count > stream.GetAvailableBytes() || mip > 1) throw new InvalidDataException("Invalid image metadata.");
        var data = stream.GetData(count);
        if (!Enum.IsDefined(format)) throw new InvalidDataException("Invalid image format.");
        if (width == 0 && height == 0 && count == 0 && mip == 0) return;
        image.SetData(width, height, mip == 1, format, data);
    }
    private (PropertyDescriptor Descriptor, StoredPropertyValue Value)[] ReadProperties(StreamPeerBuffer stream, ElectronObject owner)
    {
        var count = Count(stream);
        var descriptors = owner.GetPropertyList();
        var values = new (PropertyDescriptor, StoredPropertyValue)[count];
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            var name = ResourceArchiveStrings.Read(stream);
            var schema = ResourceArchiveStrings.Read(stream);
            if (!names.Add(name)) throw new InvalidDataException("Duplicate stored property.");
            var descriptor = descriptors.FirstOrDefault(p => p.Name == name && p.IsStored && !p.IsReadOnly);
            if (descriptor is null && owner is Node node && ResourceFileTypes.FindValueType(schema) is
                { }
             valueType) descriptor = ThemeOwner.StoredOverride(node, name, valueType);
            if (descriptor is null) throw new InvalidDataException("Unknown stored property.");
            if (ResourceFileTypes.ValueID(descriptor.ValueType) != schema) throw new InvalidDataException("Stored property type mismatch.");
            var size = Count(stream, ResourceArchive.Limit);
            if (size > stream.GetAvailableBytes()) throw new InvalidDataException("Truncated stored value.");
            using var value = Stream(stream.GetData(size));
            values[i] = (descriptor, descriptor.ReadFileValue(this, value));
            if (owner is not Node || values[i].Item2 is not StoredNodeReferenceValue && !typeof(Resource).IsAssignableFrom(descriptor.ValueType))
            {
                descriptor.RestoreStoredValue(owner, values[i].Item2, static r => r);
                descriptors = owner.GetPropertyList();
            }
            if (value.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing property bytes.");
        }
        return values;
    }
    private PackedSceneData ReadScene(StreamPeerBuffer stream)
    {
        var count = Count(stream);
        var nodes = new SceneNodeData[count];
        for (var i = 0; i < count; i++)
        {
            var type = ResourceFileTypes.Get(ResourceArchiveStrings.Read(stream));
            if (!typeof(Node).IsAssignableFrom(type.Type)) throw new InvalidDataException("Scene table includes a resource.");
            var name = ResourceArchiveStrings.Read(stream);
            if (!Node.IsValidNodeName(name)) throw new InvalidDataException("Invalid scene node name.");
            var parent = stream.Get32();
            var owner = stream.Get32();
            var sibling = stream.Get32();
            if (sibling < -1 || parent >= i || parent < -1 || owner >= count || owner < -1 || i == 0 && (parent != -1 || owner != -1) || i > 0 && (parent < 0 || owner < 0)) throw new InvalidDataException("Invalid scene topology.");
            var groupCount = Count(stream);
            var groups = new string[groupCount];
            for (var g = 0; g < groupCount; g++) groups[g] = ResourceArchiveStrings.Read(stream);
            var temporary = Node.InvokeSceneInstanceFactory(() => (Node)type.Factory());
            ScenePropertyData[] properties;
            try
            {
                properties = ReadProperties(stream, temporary).Select(p => new ScenePropertyData(p.Descriptor.Name, p.Value)).ToArray();
            }
            finally
            {
                temporary.Dispose();
            }
            var nodePath = i == 0 ? "." : parent == 0 ? name : nodes[parent].Path + "/" + name;
            var factory = new SceneFactoryData(() => (Node)type.Factory(), type.Type, 0);
            nodes[i] = new(factory, name, parent, owner, sibling, nodePath, parent < 0 ? "." : nodes[parent].Path, owner < 0 ? "" : owner == 0 ? "." : "", groups, properties, null, "");
        }
        if (count == 0) return new(nodes);
        var firstChild = new int[count]; Array.Fill(firstChild, -1); var nextSibling = new int[count];
        for (var i = count - 1; i > 0; i--) { var parent = nodes[i].ParentIndex; nextSibling[i] = firstChild[parent]; firstChild[parent] = i; }
        var entries = new int[count]; var exits = new int[count]; var stack = new int[count]; var cursors = (int[])firstChild.Clone(); var depth = 0; var clock = 1;
        while (depth >= 0) { var nodeIndex = stack[depth]; var child = cursors[nodeIndex]; if (child < 0) { exits[nodeIndex] = clock++; depth--; } else { cursors[nodeIndex] = nextSibling[child]; entries[child] = clock++; stack[++depth] = child; } }
        var siblings = new HashSet<(int, string)>();
        for (var i = 1; i < count; i++)
        {
            var node = nodes[i];
            if (!siblings.Add((node.ParentIndex, node.Name))) throw new InvalidDataException("Duplicate sibling scene name.");
            if (!(entries[node.OwnerIndex] < entries[i] && exits[node.OwnerIndex] > exits[i])) throw new InvalidDataException("Scene owner is not an ancestor.");
            nodes[i] = node with
            {
                OwnerPath = nodes[node.OwnerIndex].Path
            };
        }
        return new(nodes);
    }
    internal void RedirectRoot(Resource replacement)
    {
        var original = Root;
        Resource Redirect(Resource resource) => ReferenceEquals(resource, original) ? replacement : resource;
        foreach (var resource in Owned) if (resource is PackedScene packed) packed.LoadFileData(packed.FileData.TransformResources(Redirect));
            else if (resource is not Image && resource is not ImageTexture && resource is not TextureArray) foreach (var descriptor in resource.GetPropertyList().Where(p => p.IsStored))
                {
                    var stored = descriptor.CaptureStoredValue(resource);
                    descriptor.RestoreStoredValue(resource, stored.TransformResources(Redirect), static r => r);
                }
    }
    internal Resource ReleaseRoot()
    {
        var root = Root;
        Owned.Remove(root);
        root.AdoptFileResources(Owned.ToArray());
        Owned.Clear();
        return root;
    }
    public void Dispose()
    {
        List<Exception>? errors = null;
        foreach (var resource in Owned) try
            {
                resource.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= []).Add(error);
            }
        Owned.Clear();
        if (errors is not null) throw new AggregateException(errors);
    }
}
