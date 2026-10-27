using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
namespace Electron2D;

internal static partial class ResourceArchive
{
    internal const int Limit = 67108864;
    internal static bool IsPath(string path) => System.IO.Path.GetExtension(path).Equals(".e2dres", StringComparison.OrdinalIgnoreCase) || System.IO.Path.GetExtension(path).Equals(".e2dscene", StringComparison.OrdinalIgnoreCase);
    internal static string Absolute(string path) => ProjectSettings.GlobalizePath(ResourceUID.EnsurePath(path));
    internal static void AtomicWrite(string path, byte[] bytes)
    {
        ResourcePacks.Runtime.EnsureWritable(path);
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
    internal static void Save(Resource resource, string path, SaverFlags flags, bool skipUnchanged = false)
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
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(8), 9);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(12), (int)flags);
        BinaryPrimitives.WriteInt64LittleEndian(file.AsSpan(16), uid);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(24), payload.Length);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(28), encoded.Length);
        SHA256.HashData(payload).CopyTo(file, 32);
        encoded.CopyTo(file, 64);
        var absolute = Absolute(path);
        if (!skipUnchanged || !System.IO.File.Exists(absolute) || new FileInfo(absolute).Length != file.Length ||
            !System.IO.File.ReadAllBytes(absolute).AsSpan().SequenceEqual(file)) AtomicWrite(path, file);
        ResourceUID.SetID(uid, path);
        context.CommitPaths();
    }
    internal static long ReadUID(string path)
    {
        if (!FileAccess.FileExists(path)) return ResourceUID.InvalidID;
        using var stream = ResourcePacks.OpenRead(path);
        Span<byte> header = stackalloc byte[24];
        if (stream.Read(header) != header.Length || !header[..8].SequenceEqual("E2DASSET"u8)) return ResourceUID.InvalidID;
        if (BinaryPrimitives.ReadInt32LittleEndian(header[8..]) is not (1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9) || stream.Length < 64) throw new InvalidDataException("Unsupported resource UID header.");
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
        var length = FileAccess.GetSize(path);
        if (length is < 64 or > Limit + 64) throw new InvalidDataException("Invalid resource file size.");
        var bytes = FileAccess.GetFileAsBytes(path);
        if (!bytes.AsSpan(0, 8).SequenceEqual("E2DASSET"u8) || BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) is not (1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9)) throw new InvalidDataException("Unsupported resource archive version.");
        if (BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(16)) < 0) throw new InvalidDataException("Invalid archive UID.");
        return bytes;
    }
    internal static byte[] ReadPayload(string path, out bool bigEndian) => ReadPayload(path, out bigEndian, out _);
    internal static byte[] ReadPayload(string path, out bool bigEndian, out int version)
    {
        var bytes = ReadFile(path);
        version = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8));
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
    internal static ResourceArchiveRead Decode(string path, ResourceLoader.CacheMode mode, string? originalPath = null)
    {
        var payload = ReadPayload(path, out var bigEndian, out var version);
        var context = new ResourceArchiveRead(path, mode, bigEndian, version, ResourceLoader.AbortOnMissingResources, originalPath);
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
        var payload = ReadPayload(path, out bigEndian, out var version);
        using var stream = new StreamPeerBuffer
        {
            DataArray = payload,
            BigEndian = bigEndian
        };
        var count = stream.Get32();
        if (count is < 1 or > 65536) throw new InvalidDataException("Invalid resource table.");
        var definitions = new ArchiveDefinition[count];
        for (var i = 0; i < count; i++) definitions[i] = ReadDefinition(stream, version, ResourceLoader.AbortOnMissingResources);
        ValidateScriptReferences(definitions);
        if (stream.GetAvailableBytes() != 0 || definitions[0].Path.Length != 0) throw new InvalidDataException("Invalid archive metadata root or trailing bytes.");
        return definitions;
    }
    internal static void ValidateScriptReferences(ArchiveDefinition[] definitions)
    {
        foreach (var definition in definitions)
            if (definition.Script >= definitions.Length || definition.Script >= 0 && definitions[definition.Script].Type.Type != typeof(Script))
                throw new InvalidDataException("Invalid compiled source reference.");
    }
    internal static void RewriteDependencies(string path, IReadOnlyDictionary<string, string> renames)
    {
        ArgumentNullException.ThrowIfNull(renames);
        var definitions = Inspect(path, out var bigEndian);
        var original = ReadFile(path);
        var version = BinaryPrimitives.ReadInt32LittleEndian(original.AsSpan(8));
        using var output = new StreamPeerBuffer
        {
            BigEndian = bigEndian
        };
        output.Put32(definitions.Length);
        foreach (var d in definitions)
        {
            var target = d.Path;
            var uid = d.UID;
            var relative = target.Length != 0 && !target.Contains("://", StringComparison.Ordinal) && !System.IO.Path.IsPathRooted(target);
            var full = target.Length == 0 ? target : uid >= 0 && ResourceUID.HasID(uid) ? ResourceUID.GetIDPath(uid) : ResolveDependencyPath(path, target);
            if (target.Length != 0 && (renames.TryGetValue(full, out var replacement) || renames.TryGetValue(target, out replacement)))
            {
                ArgumentException.ThrowIfNullOrEmpty(replacement);
                full = ResolveDependencyPath(path, replacement);
                uid = ResourceSaver.GetResourceIDForPath(full);
                target = relative ? System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(Absolute(path))!, Absolute(full)).Replace('\\', '/') : full;
            }
            ResourceArchiveStrings.Write(output, d.Type.ID);
            if (version >= 2) WriteRole(output, d, version);
            ResourceArchiveStrings.Write(output, d.Name);
            output.PutU8(d.Local ? (byte)1 : (byte)0);
            ResourceArchiveStrings.Write(output, d.SceneID);
            ResourceArchiveStrings.Write(output, target);
            output.Put64(uid);
            if (version >= 9) output.Put32(d.Script);
            output.Put32(d.Payload.Length);
            output.PutData(d.Payload);
        }
        var payload = output.DataArray;
        if (payload.Length > Limit) throw new InvalidDataException("Dependency rewrite exceeds archive budget.");
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
        var payload = ReadPayload(path, out var bigEndian, out var version);
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
            if (string.IsNullOrWhiteSpace(type)) throw new InvalidDataException("Invalid resource factory ID.");
            var metadata = ReadRoleMetadata(stream, version);
            _ = ResourceArchiveStrings.Read(stream);
            if (stream.GetU8() > 1) throw new InvalidDataException("Invalid resource metadata flag.");
            _ = ResourceArchiveStrings.Read(stream);
            var dependency = ResourceArchiveStrings.Read(stream);
            var uid = stream.Get64();
            if (version >= 9) _ = stream.Get32();
            var size = stream.Get32();
            if (size < 0 || size > stream.GetAvailableBytes() || uid < -1 || dependency.Length != 0 && (size != 0 || i == 0 || metadata.MissingPayload)) throw new InvalidDataException("Invalid resource payload size.");
            stream.Seek(checked(stream.GetPosition() + size));
            if (dependency.Length != 0)
            {
                dependency = ResolveDependencyPath(path, dependency);
                var token = uid < 0 ? dependency + (addTypes ? "::" + type : "") : ResourceUID.IDToText(uid) + "::" + (addTypes ? type : "") + "::" + dependency;
                dependencies.Add(token);
            }
        }
        if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing metadata bytes.");
        return dependencies.ToArray();
    }
    internal static string ResolveDependencyPath(string source, string target) => !target.Contains("://", StringComparison.Ordinal) && !System.IO.Path.IsPathRooted(target) ? ResourcePacks.CombineRelative(source, target) : target;

}
internal sealed record ArchiveDefinition(ResourceFileType Type, string Name, bool Local, string SceneID, string Path, long UID, byte[] Payload, ResourceArchiveRole Role = ResourceArchiveRole.Resource, Rect2 Bounds = default, bool MissingPayload = false, int Script = -1);
internal sealed partial class ResourceArchiveWrite(Resource root, string path, SaverFlags flags)
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
            resource.ValidateFileState();
            var role = ResourceArchive.RoleFor(resource.GetType());
            var bounds = ResourceArchive.BoundsFor(resource);
            var external = !ReferenceEquals(resource, root) && !resource.IsBuiltIn && (flags & SaverFlags.BundleResources) == 0;
            var missing = MissingResourceData.For(resource);
            var missingPayload = !external && missing?.MissingPayload == true;
            var type = missing is not null && missing.OriginalClass.Length != 0 ? ResourceArchive.ResolveType(missing.OriginalClass, role, false, missingPayload) : ResourceFileTypes.Get(resource.GetType());
            if (external)
            {
                var target = resource.ResourcePath;
                var uid = ResourceSaver.GetResourceIDForPath(target);
                if ((flags & SaverFlags.RelativePaths) != 0) target = System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(ResourceArchive.Absolute(path))!, ResourceArchive.Absolute(target));
                definitions.Add(new(type, resource.ResourceName, resource.ResourceLocalToScene, resource.ResourceSceneUniqueID, target, uid, [], role, bounds, false));
                continue;
            }
            using var stream = NewStream();
            if (MissingResourceData.For(resource) is { } opaque && (resource is MissingResource || opaque.OriginalClass.Length != 0)) WriteMissing(stream, opaque);
            else if (resource is PackedScene packed) WriteScene(stream, packed.FileData);
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
            else if (resource is CompressedTexture compressed)
            {
                var state = compressed.CaptureFileState(); ResourceArchiveStrings.Write(stream, state.Path);
                stream.Put32(state.Size.X); stream.Put32(state.Size.Y);
                stream.PutU8(state.Pixels is null ? (byte)0 : (byte)1);
                if (state.Pixels is not null) { using var storedImage = state.Pixels.CopyImage(); WriteImage(stream, storedImage); }
            }
            else if (resource is CompressedTextureLayered imported)
            {
                var state = imported.CaptureFileState(); ResourceArchiveStrings.Write(stream, state.Path);
                stream.Put32(state.Pixels?.Layers.Length ?? 0);
                if (state.Pixels is not null) foreach (var layer in state.Pixels.Layers) { using var layerImage = layer.CopyImage(); WriteImage(stream, layerImage); }
            }
            else if (resource is TextureArray array)
            {
                var pixels = array.CaptureLayers(); stream.Put32(pixels?.Layers.Length ?? 0);
                if (pixels is not null) foreach (var layer in pixels.Layers) { using var layerImage = layer.CopyImage(); WriteImage(stream, layerImage); }
            }
            else WriteProperties(stream, resource, null);
            _internal.Add(resource);
            var sceneID = resource.ResourceSceneUniqueID;
            _sceneIDs.Add(resource, sceneID);
            definitions.Add(new(type, resource.ResourceName, resource.ResourceLocalToScene, sceneID, "", -1, stream.DataArray, role, bounds, missingPayload, Reference(resource.GetScript())));
        }
        var reservedIDs = definitions.Skip(1).Where(d => d.Path.Length == 0 && d.SceneID.Length != 0).Select(d => d.SceneID).ToHashSet(StringComparer.Ordinal);
        var assignedIDs = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 1; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (definition.Path.Length != 0) continue;
            var id = definition.SceneID;
            if (id.Length == 0 || !assignedIDs.Add(id))
            {
                do { id = Resource.GenerateSceneUniqueID(); } while (!reservedIDs.Add(id));
                assignedIDs.Add(id);
            }
            _sceneIDs[_resources[i]] = id;
            definitions[i] = definition with { SceneID = id };
        }
        using var output = NewStream();
        output.Put32(definitions.Count);
        foreach (var d in definitions)
        {
            ResourceArchiveStrings.Write(output, d.Type.ID);
            ResourceArchive.WriteRole(output, d);
            ResourceArchiveStrings.Write(output, d.Name);
            output.PutU8(d.Local ? (byte)1 : (byte)0);
            ResourceArchiveStrings.Write(output, d.SceneID);
            ResourceArchiveStrings.Write(output, d.Path);
            output.Put64(d.UID);
            output.Put32(d.Script);
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
        stream.PutU8(image.HasMIPMaps ? (byte)1 : (byte)0);
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
        var properties = captured ?? owner.GetPropertyList().Where(p => p.ShouldStore(owner)).Select(p => new ScenePropertyData(p.Name, p.CaptureStoredValue(owner))).ToArray();
        if ((flags & SaverFlags.OmitEditorProperties) != 0) properties = properties.Where(p => !p.Name.StartsWith("__editor", StringComparison.Ordinal)).ToArray();
        stream.Put32(properties.Length);
        var descriptors = owner.GetPropertyList();
        if (owner is Node) Node.ValidateEditorPropertyNames(descriptors);
        foreach (var property in properties)
        {
            var descriptor = descriptors.FirstOrDefault(p => p.Name == property.Name && p.IsStored && !p.IsReadOnly);
            if (descriptor is null && owner is Node node) descriptor = ThemeOwner.StoredOverride(node, property.Name, property.Value.ValueType) ?? InstanceUniformState.StoredParameter(node, property.Name, property.Value.ValueType) ?? ShaderGlobalParameters.StoredSceneParameter(node, property.Name, property.Value.ValueType) ?? Node.StoredEditorProperty(property.Name, property.Value.ValueType);
            if (descriptor?.ValueType != property.Value.ValueType && owner is CanvasItem canvas) descriptor = InstanceUniformState.StoredParameter(canvas, property.Name, property.Value.ValueType) ?? descriptor;
            if (descriptor is null && owner is Theme) descriptor = Theme.StoredProperty(property.Name, property.Value.ValueType);
            if (descriptor is null && owner is FoldableContainer) descriptor = FoldableContainer.StoredTitleControl(property.Name, property.Value.ValueType);
            if (descriptor is null && owner is ResourcePreloader) descriptor = ResourcePreloader.StoredResource(property.Name, property.Value.ValueType);
            if (descriptor is null) throw new InvalidDataException("Stored schema has no matching descriptor.");
            if (descriptor.ValueType != property.Value.ValueType) throw new InvalidDataException("Stored property schema changed.");
            var storedValue = property.Value is DeferredPropertyValue pending ? descriptor.ReadDeferredValue(pending.Field) : property.Value;
            ResourceArchiveStrings.Write(stream, property.Name);
            ResourceArchiveStrings.Write(stream, ResourceFileTypes.ValueID(property.Value.ValueType));
            stream.PutU8((byte)MissingResourceData.Role(property.Value.ValueType));
            stream.PutU8(BigEndian ? (byte)1 : (byte)0);
            using var value = NewStream();
            descriptor.WriteFileValue(owner, storedValue, this, value);
            var data = value.DataArray;
            stream.Put32(data.Length);
            stream.PutData(data);
            if (prepareSchema && storedValue is not StoredNodeReferenceValue && !typeof(Resource).IsAssignableFrom(storedValue.ValueType)) descriptor.RestoreStoredValue(owner, storedValue, static r => r);
            descriptors = owner.GetPropertyList();
            if (owner is Node) Node.ValidateEditorPropertyNames(descriptors);
        }
    }
    private void WriteScene(StreamPeerBuffer stream, PackedSceneData data)
    {
        data = PackedScene.PrepareMissingSceneSave(data);
        if (data.Nodes.Length > 65536 || data.Connections.Length > 65536) throw new InvalidDataException("Scene node/connection count exceeds the archive budget.");
        stream.Put32(data.Nodes.Length);
        foreach (var node in data.Nodes)
        {
            // Skipped inherited records do not require the deferred scene's compiled factories.
            var recovering = node.Factory.RuntimeType == typeof(MissingNode);
            var type = ResourceFileTypes.Get(recovering || node.DeferredProperties is not null && node.Factory.RuntimeType != typeof(InstancePlaceholder)
                ? typeof(Node) : node.Factory.RuntimeType);
            var typeID = recovering && node.Factory.OriginalScene.Length == 0 ? node.Factory.OriginalClass : type.ID;
            if (string.IsNullOrWhiteSpace(typeID) || typeID.Length > 256 || typeID.Any(char.IsControl)) throw new InvalidDataException("A recovered class requires a valid original factory identity.");
            ResourceArchiveStrings.Write(stream, typeID);
            stream.Put32(Reference(node.Factory.Script));
            ResourceArchiveStrings.Write(stream, node.Name);
            stream.Put32(node.ParentIndex);
            stream.Put32(node.OwnerIndex);
            stream.Put32(node.SiblingIndex);
            stream.Put32(Reference(node.Instance)); stream.Put32(node.InstanceRootIndex);
            ResourceArchiveStrings.Write(stream, node.InstancePath);
            ResourceArchiveStrings.Write(stream, node.InstancePlaceholder);
            stream.Put32(node.Groups.Count);
            foreach (var group in node.Groups) ResourceArchiveStrings.Write(stream, group);
            if (node.DeferredProperties is not null)
            {
                if (node.Properties.Length != 0) throw new InvalidDataException("Deferred records use their self-describing fields.");
                stream.Put32(0); WriteMissing(stream, node.DeferredProperties); continue;
            }
            if (recovering && node.Factory.OriginalScene.Length == 0)
            {
                var fields = new MissingResourceData { Recording = true };
                foreach (var property in node.Properties) fields.Put(property.Value.CaptureField(property.Name));
                WriteMissing(stream, fields); stream.Put32(0); continue;
            }
            var temporary = Node.InvokeSceneInstanceFactory(() => (Node)type.Factory(node.Factory.Script));
            try
            {
                var properties = recovering ? node.Properties.Where(property => temporary.GetPropertyList().Any(descriptor =>
                    descriptor.Name == property.Name && descriptor.IsStored && ResourceFileTypes.ValueID(descriptor.ValueType) == property.Value.CaptureField(property.Name).Schema)).ToArray() : node.Properties;
                WriteProperties(stream, temporary, properties, true);
            }
            finally
            {
                temporary.Dispose();
            }
            stream.Put32(0);
        }
        stream.Put32(data.Connections.Length);
        foreach (var connection in data.Connections)
        {
            ResourceArchiveStrings.Write(stream, connection.Schema.ID);
            ResourceArchiveStrings.Write(stream, connection.Schema.Signal); ResourceArchiveStrings.Write(stream, connection.Schema.Method);
            stream.Put32(connection.Schema.Flags); stream.Put32(connection.Schema.Unbinds);
            stream.Put32(connection.PublisherIndex); stream.Put32(connection.SubscriberIndex);
            var payload = new MissingResourceData { Recording = true }; payload.Put(connection.Bindings.CaptureField("Bindings"));
            WriteMissing(stream, payload);
        }
    }
    internal void CommitPaths()
    {
        foreach (var resource in _internal) if (!ReferenceEquals(resource, root)) resource.ResourceSceneUniqueID = _sceneIDs[resource];
        if ((flags & SaverFlags.ReplaceSubresourcePaths) == 0) return;
        List<Exception>? errors = null;
        foreach (var resource in _internal) if (!ReferenceEquals(resource, root))
            {
                try { resource.SetPathCache(path + "::" + _sceneIDs[resource]); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
        if (errors is not null) throw new AggregateException("Saved resource path callbacks failed after persistence.", errors);
    }

}
internal sealed partial class ResourceArchiveRead(string path, ResourceLoader.CacheMode mode, bool bigEndian, int version, bool abortOnMissing, string? originalPath = null) : IDisposable
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
        for (var i = 0; i < count; i++) _definitions[i] = ResourceArchive.ReadDefinition(stream, version, abortOnMissing);
        ResourceArchive.ValidateScriptReferences(_definitions);
        if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing archive payload.");
        var sceneIDs = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 1; i < _definitions.Length; i++)
        {
            var definition = _definitions[i];
            if (definition.UID < -1 || definition.Path.Length != 0 && definition.Payload.Length != 0) throw new InvalidDataException("Invalid external resource metadata.");
            var id = definition.SceneID.Length != 0 ? definition.SceneID : i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (definition.Path.Length == 0 && !sceneIDs.Add(id)) throw new InvalidDataException("Duplicate subresource scene ID.");
        }
        if (_definitions[0].Path.Length != 0) throw new InvalidDataException("Archive root cannot be an external reference.");
        void LoadIdentity(int i)
        {
            var d = _definitions[i];
            if (d.Path.Length != 0)
            {
                var target = d.UID >= 0 && ResourceUID.HasID(d.UID) ? ResourceUID.GetIDPath(d.UID) : d.Path;
                target = ResourceArchive.ResolveDependencyPath(path, target);
                var dependencyMode = mode is ResourceLoader.CacheMode.IgnoreDeep or ResourceLoader.CacheMode.ReplaceDeep ? mode : ResourceLoader.CacheMode.Reuse;
                var stage = ResourceLoadGraph.Current;
                var cachedDependency = stage is null ? Resource.GetRegisteredPath(target) : null;
                if (!abortOnMissing && version >= 2 && (dependencyMode is ResourceLoader.CacheMode.IgnoreDeep or ResourceLoader.CacheMode.ReplaceDeep || Resource.GetRegisteredPath(target) is null) && ResourceArchive.IsPath(target) && !FileAccess.FileExists(target))
                {
                    var missing = ResourceArchive.CreateMissing(d.Type.ID, d.Role, d.Bounds, missingPayload: true);
                    Owned.Add(missing);
                    missing.ResourceName = d.Name; missing.ResourceLocalToScene = d.Local; missing.ResourceSceneUniqueID = d.SceneID;
                    missing.SetPathCache(target); _resources[i] = missing; return;
                }
                var resource = ResourceLoader.LoadFileResource(target, dependencyMode, abortOnMissing ? d.Type.Type : ResourceArchive.RoleType(d.Role));
                if (stage is null && !ReferenceEquals(cachedDependency, resource)) Owned.Add(resource);
                var original = MissingResourceData.For(resource)?.OriginalClass;
                var actualID = string.IsNullOrEmpty(original) ? ResourceFileTypes.Get(resource.GetType()).ID : original;
                if (actualID != d.Type.ID || ResourceArchive.RoleFor(resource.GetType(), version) != d.Role) throw new InvalidDataException("External resource type does not match its schema.");
                _resources[i] = resource;
            }
            else
            {
                var created = d.Type.Factory(ConstructionScript(d.Script, d.Type.Type)) as Resource ?? throw new InvalidDataException("Resource factory returned a node.");
                Owned.Add(created);
                if (created.IsDisposed || created.GetType() != d.Type.Type) throw new InvalidDataException("Resource factory returned an invalid identity.");
                _resources[i] = created;
            }
        }
        // Script is sealed and stores scalar metadata only; prepare it before invoking any compiled owner constructor.
        for (var i = 0; i < count; i++) if (_definitions[i].Type.Type == typeof(Script))
            {
                if (_definitions[i].Script != -1) throw new InvalidDataException("A source asset cannot have a compiled owner association.");
                LoadIdentity(i); RestoreIdentity(i);
            }
        if (ResourceLoadGraph.Current is { } loadGraph)
        {
            for (var i = 0; i < count; i++) if (_definitions[i].Path.Length == 0 && _resources[i] is null) LoadIdentity(i);
            var dependencies = Enumerable.Range(0, count).Where(i => _definitions[i].Path.Length != 0 && _resources[i] is null).ToArray();
            loadGraph.LoadDependencies(dependencies.Length, index => LoadIdentity(dependencies[index]));
        }
        else for (var i = 0; i < count; i++) if (_resources[i] is null) LoadIdentity(i);
        // Validate scene references against prepared root metadata, independent of archive table order.
        foreach (var i in Enumerable.Range(0, count).OrderBy(index => _resources[index] is PackedScene ? 0 : _resources[index] is Shader ? 1 : 2))
            if (_definitions[i].Type.Type != typeof(Script)) RestoreIdentity(i);
        foreach (var resource in Owned) resource.ValidateFileState();

        void RestoreIdentity(int i)
        {
            var d = _definitions[i];
            if (d.Path.Length != 0) return;
            var resource = _resources[i]!;
            resource.ResourceName = d.Name;
            resource.ResourceLocalToScene = d.Local;
            resource.ResourceSceneUniqueID = d.SceneID;
            if (i > 0) resource.SetPathCache((originalPath ?? path) + "::" + (d.SceneID.Length == 0 ? i.ToString(System.Globalization.CultureInfo.InvariantCulture) : d.SceneID));
            using var value = Stream(d.Payload);
            if (MissingResourceData.For(resource) is { } opaque && (resource is MissingResource && version >= 2 || opaque.OriginalClass.Length != 0))
            {
                if (resource is PlaceholderMesh placeholder) placeholder.AABB = d.Bounds;
                else if (resource is PlaceholderTexture texturePlaceholder) texturePlaceholder.Size = d.Bounds.Size;
                ReadMissing(value, opaque);
            }
            else if (resource is PackedScene packed) packed.LoadFileData(ReadScene(value));
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
            else if (resource is CompressedTexture compressed)
            {
                var sourcePath = ResourceArchiveStrings.Read(value); var size = new Vector2i(value.Get32(), value.Get32());
                var marker = value.GetU8();
                if (marker > 1) throw new InvalidDataException("Invalid stored imported texture marker.");
                TexturePixels? pixels = null;
                if (marker != 0)
                {
                    using var storedImage = new Image(); ReadImage(value, storedImage);
                    pixels = TexturePixels.FromImage(storedImage, LayeredTexturePixels.MaximumBytes);
                }
                compressed.RestoreFileState(pixels, size, sourcePath);
            }
            else if (resource is CompressedTextureLayered imported)
            {
                var sourcePath = ResourceArchiveStrings.Read(value); var layerCount = value.Get32();
                if (layerCount < 0 || layerCount > value.GetAvailableBytes() / 17) throw new InvalidDataException("Invalid stored compressed array layer count.");
                var layers = new TexturePixels[layerCount]; var remaining = LayeredTexturePixels.MaximumBytes;
                try
                {
                    for (var layer = 0; layer < layers.Length; ++layer)
                    {
                        using var pixels = new Image(); ReadImage(value, pixels);
                        layers[layer] = TexturePixels.FromImage(pixels, remaining);
                        remaining -= layers[layer].Source.Data.Length + layers[layer].Upload.Data.Length;
                    }
                    imported.RestoreFileState(layerCount == 0 ? null : new LayeredTexturePixels(layers), sourcePath);
                }
                catch (ArgumentException error) { throw new InvalidDataException("Invalid stored compressed array layers.", error); }
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
    private Script? ConstructionScript(int reference, Type ownerType)
    {
        if (reference == -1) return null;
        if (reference < 0 || reference >= _resources.Length || _resources[reference] is not Script script || !script.MatchesConstructionType(ownerType))
            throw new InvalidDataException("Compiled source does not match its exact owner type.");
        return script;
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
        if (owner is Node) Node.ValidateEditorPropertyNames(descriptors);
        var values = new (PropertyDescriptor, StoredPropertyValue)[count];
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            var name = ResourceArchiveStrings.Read(stream);
            var schema = ResourceArchiveStrings.Read(stream);
            var role = version >= 2 ? (ResourceFieldRole)stream.GetU8() : (ResourceFieldRole?)null;
            var endian = version >= 2 ? stream.GetU8() : bigEndian ? (byte)1 : (byte)0;
            if (endian > 1) throw new InvalidDataException("Invalid property byte order.");
            if (!names.Add(name)) throw new InvalidDataException("Duplicate stored property.");
            var descriptor = descriptors.FirstOrDefault(p => p.Name == name && p.IsStored && !p.IsReadOnly);
            if (descriptor is null && owner is Node node && ResourceFileTypes.FindValueType(schema) is
                { }
             valueType) descriptor = ThemeOwner.StoredOverride(node, name, valueType) ?? InstanceUniformState.StoredParameter(node, name, valueType) ?? ShaderGlobalParameters.StoredSceneParameter(node, name, valueType) ?? Node.StoredEditorProperty(name, valueType);
            if (owner is CanvasItem canvas && ResourceFileTypes.FindValueType(schema) is { } storedType && descriptor?.ValueType != storedType) descriptor = InstanceUniformState.StoredParameter(canvas, name, storedType) ?? descriptor;
            if (descriptor is null && owner is Theme && ResourceFileTypes.FindValueType(schema) is { } themeValueType) descriptor = Theme.StoredProperty(name, themeValueType);
            if (descriptor is null && owner is FoldableContainer && ResourceFileTypes.FindValueType(schema) is { } titleValueType) descriptor = FoldableContainer.StoredTitleControl(name, titleValueType);
            if (descriptor is null && owner is ResourcePreloader && ResourceFileTypes.FindValueType(schema) is { } preloadedType) descriptor = ResourcePreloader.StoredResource(name, preloadedType);
            if (descriptor is null) throw new InvalidDataException("Unknown stored property.");
            if (ResourceFileTypes.ValueID(descriptor.ValueType) != schema) throw new InvalidDataException("Stored property type mismatch.");
            if (role is not null && role != MissingResourceData.Role(descriptor.ValueType)) throw new InvalidDataException("Stored property role mismatch.");
            var size = Count(stream, ResourceArchive.Limit);
            if (size > stream.GetAvailableBytes()) throw new InvalidDataException("Truncated stored value.");
            using var value = new StreamPeerBuffer { DataArray = stream.GetData(size), BigEndian = endian == 1 };
            values[i] = (descriptor, descriptor.ReadFileValue(this, value));
            if (owner is not Node || values[i].Item2 is not StoredNodeReferenceValue && !typeof(Resource).IsAssignableFrom(descriptor.ValueType))
            {
                descriptor.RestoreStoredValue(owner, values[i].Item2, static r => r);
                descriptors = owner.GetPropertyList();
                if (owner is Node) Node.ValidateEditorPropertyNames(descriptors);
            }
            if (value.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing property bytes.");
        }
        return values;
    }
    private PackedSceneData ReadScene(StreamPeerBuffer stream)
    {
        var count = Count(stream);
        var nodes = new SceneNodeData[count];
        var linkedRoots = new bool[count];
        var skipped = new bool[count]; var missingSkipped = new bool[count];
        for (var i = 0; i < count; i++)
        {
            var typeID = ResourceArchiveStrings.Read(stream);
            if (string.IsNullOrWhiteSpace(typeID) || typeID.Length > 256 || typeID.Any(char.IsControl)) throw new InvalidDataException("Invalid node factory identity.");
            var type = ResourceFileTypes.Find(typeID);
            if (type is null && abortOnMissing) throw new InvalidDataException("Unknown node factory ID.");
            type ??= new ResourceFileType(typeID, typeof(MissingNode), static () => new MissingNode());
            if (!typeof(Node).IsAssignableFrom(type.Type)) throw new InvalidDataException("Scene table includes a resource.");
            var source = ConstructionScript(version >= 9 ? stream.Get32() : -1, type.Type);
            var name = ResourceArchiveStrings.Read(stream);
            if (!Node.IsValidStoredNodeName(name)) throw new InvalidDataException("Invalid scene node name.");
            var parent = stream.Get32();
            var owner = stream.Get32();
            var sibling = stream.Get32();
            if (sibling < -1 || parent >= i || parent < -1 || owner >= count || owner < -1 || i == 0 && (parent != -1 || owner != -1) || i > 0 && (parent < 0 || owner < 0)) throw new InvalidDataException("Invalid scene topology.");
            var link = ResourceArchive.ReadSceneLink(stream, version, i, linkedRoots, _definitions.Length);
            var instance = Reference(link.Reference); var instanceRoot = link.Root; var instancePath = link.Path;
            var missingScene = instance is MissingResource unavailable && unavailable.Data.OriginalClass == "PackedScene" ? instance.ResourcePath : "";
            if (instance is not null && instance is not PackedScene)
            {
                if (missingScene.Length == 0 || abortOnMissing) throw new InvalidDataException("A linked resource is not a packed scene.");
                type = new(typeID, typeof(MissingNode), static () => new MissingNode()); instance = null;
            }
            if (link.Placeholder.Length != 0 && type.Type != typeof(InstancePlaceholder)) throw new InvalidDataException("A deferred record requires the placeholder node role.");
            skipped[i] = parent >= 0 && skipped[parent] || instanceRoot >= 0 && (skipped[instanceRoot] || nodes[instanceRoot].InstancePlaceholder.Length != 0 || nodes[instanceRoot].Factory.OriginalScene.Length != 0);
            missingSkipped[i] = parent >= 0 && missingSkipped[parent] || instanceRoot >= 0 &&
                (missingSkipped[instanceRoot] || nodes[instanceRoot].Factory.OriginalScene.Length != 0);
            var groupCount = Count(stream);
            var groups = new string[groupCount];
            for (var g = 0; g < groupCount; g++) groups[g] = ResourceArchiveStrings.Read(stream);
            ScenePropertyData[] properties;
            MissingResourceData? deferred = null;
            if (version >= 6 && (link.Placeholder.Length != 0 || skipped[i] && !missingSkipped[i] || type.Type == typeof(InstancePlaceholder)))
            {
                if (Count(stream) != 0) throw new InvalidDataException("Deferred records cannot contain eager property schemas.");
                properties = []; deferred = new MissingResourceData(); ReadMissing(stream, deferred, allowNodes: true);
            }
            else if (type.Type == typeof(MissingNode) || skipped[i])
            {
                var fields = new MissingResourceData(); ReadMissing(stream, fields, allowNodes: true);
                properties = fields.Fields.Values.Select(field => new ScenePropertyData(field.Name, new DeferredPropertyValue(field))).ToArray();
                if (version >= 6 && Count(stream) != 0) throw new InvalidDataException("An ordinary recovered node cannot contain deferred fields.");
            }
            else
            {
                var temporary = Node.InvokeSceneInstanceFactory(() => (Node)type.Factory(source));
                try { properties = ReadProperties(stream, temporary).Select(p => new ScenePropertyData(p.Descriptor.Name, p.Value)).ToArray(); }
                finally { temporary.Dispose(); }
                if (version >= 6 && Count(stream) != 0) throw new InvalidDataException("An ordinary node cannot contain deferred fields.");
            }
            var nodePath = i == 0 ? "." : parent == 0 ? name : nodes[parent].Path + "/" + name;
            var factory = new SceneFactoryData(() => (Node)type.Factory(), type.Type, 0,
                type.Type == typeof(MissingNode) && missingScene.Length == 0 ? typeID : "", missingScene, source);
            nodes[i] = new(factory, name, parent, owner, sibling, nodePath, parent < 0 ? "." : nodes[parent].Path, owner < 0 ? "" : owner == 0 ? "." : "", groups, properties, (PackedScene?)instance, link.Placeholder, instanceRoot, instancePath, deferred);
        }
        if (count == 0)
        {
            if (version >= 3 && Count(stream) != 0) throw new InvalidDataException("Empty scene contains connection endpoints.");
            return new(nodes);
        }
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
            if (node.InstanceRootIndex >= 0 && !(entries[node.InstanceRootIndex] < entries[i] && exits[node.InstanceRootIndex] > exits[i]))
                throw new InvalidDataException("An overridden node is outside its linked instance.");
            nodes[i] = node with
            {
                OwnerPath = nodes[node.OwnerIndex].Path
            };
        }

        var missingAbsent = PackedScene.MissingSceneAbsentNodes(new PackedSceneData(nodes));
        var connections = version >= 3 ? new SceneConnectionData[Count(stream)] : [];
        for (var i = 0; i < connections.Length; i++)
        {
            var id = ResourceArchiveStrings.Read(stream);
            var known = ResourceFileTypes.FindSceneConnection(id);
            var signal = version >= 7 ? ResourceArchiveStrings.Read(stream) : known?.Signal ?? throw new InvalidDataException("An unknown legacy connection has no retained event schema.");
            var method = version >= 7 ? ResourceArchiveStrings.Read(stream) : known!.Method;
            var flags = version >= 7 ? stream.Get32() : known!.Flags; var unbinds = version >= 7 ? stream.Get32() : known!.Unbinds;
            foreach (var label in new[] { id, signal, method })
                if (string.IsNullOrWhiteSpace(label) || label.Length > 256 || label.Any(char.IsControl)) throw new InvalidDataException("Invalid retained connection identity.");
            if ((flags & 2) == 0 || (flags & ~7) != 0 || unbinds < 0) throw new InvalidDataException("Invalid retained connection policy.");
            var publisher = stream.Get32(); var subscriber = stream.Get32();
            if ((uint)publisher >= (uint)count || (uint)subscriber >= (uint)count) throw new InvalidDataException("Invalid connection endpoint.");
            var missingEndpoint = missingAbsent[publisher] || missingAbsent[subscriber] || nodes[publisher].Factory.RuntimeType == typeof(MissingNode) || nodes[subscriber].Factory.RuntimeType == typeof(MissingNode);
            if (known is null && (abortOnMissing || !missingEndpoint)) throw new InvalidDataException("Scene connection binding is not registered.");
            var schema = known ?? new UnresolvedSceneConnectionSchema(id, signal, method, unbinds, flags);
            if (schema.Signal != signal || schema.Method != method || schema.Flags != flags || schema.Unbinds != unbinds ||
                !missingAbsent[publisher] && nodes[publisher].Factory.RuntimeType != typeof(MissingNode) && !schema.PublisherType.IsAssignableFrom(nodes[publisher].Factory.RuntimeType) ||
                !missingAbsent[subscriber] && nodes[subscriber].Factory.RuntimeType != typeof(MissingNode) && !schema.SubscriberType.IsAssignableFrom(nodes[subscriber].Factory.RuntimeType))
                throw new InvalidDataException("Stored connection schema or endpoints changed.");
            StoredPropertyValue bindings;
            if (version >= 7)
            {
                var payload = new MissingResourceData(); ReadMissing(stream, payload);
                if (payload.Fields.Count != 1 || !payload.Fields.TryGetValue("Bindings", out var field)) throw new InvalidDataException("Invalid retained connection payload.");
                if (known is not null && field.Schema != ResourceFileTypes.ValueID(known.PayloadDescriptor.ValueType)) throw new InvalidDataException("Connection payload schema changed.");
                bindings = new DeferredPropertyValue(field);
            }
            else
            {
                var payloadSize = Count(stream, ResourceArchive.Limit);
                if (payloadSize > stream.GetAvailableBytes()) throw new InvalidDataException("Truncated connection payload.");
                using var payload = new StreamPeerBuffer { BigEndian = bigEndian, DataArray = stream.GetData(payloadSize) };
                bindings = schema.PayloadDescriptor.ReadFileValue(this, payload);
                if (payload.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing connection payload.");
            }
            connections[i] = new(schema, publisher, subscriber, bindings);
        }
        return new(nodes, connections.Where(connection => !missingAbsent[connection.PublisherIndex] && !missingAbsent[connection.SubscriberIndex]).ToArray());
    }
    internal Resource[] RedirectRoot(Resource replacement)
    {
        var original = Root;
        Resource Redirect(Resource resource) => ReferenceEquals(resource, original) ? replacement : resource;
        return RedirectResources(Redirect);
    }
    internal Resource[] RedirectResources(Func<Resource, Resource> redirect)
    {
        List<Resource>? changed = null;
        foreach (var resource in Owned) if (resource.RemapPreparedReferences(redirect)) (changed ??= []).Add(resource);
        return changed?.ToArray() ?? [];
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
