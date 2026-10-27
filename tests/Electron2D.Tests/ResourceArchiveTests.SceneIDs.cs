using System.Buffers.Binary;
using System.Security.Cryptography;
using Electron2D;

internal static partial class ResourceArchiveTests
{
    private sealed class IDPathProbe : Resource
    {
        internal IDPathProbe? Next;
        internal bool FailPath;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(
            new PropertyDescriptor<IDPathProbe, IDPathProbe?>("Next", n => n.Next, (n, v) => n.Next = v, _ => null, stored: true));
        protected override void OnPathCacheSet(string path) { if (FailPath) throw new InvalidOperationException("Expected committed path callback failure."); }
    }
    private static IDPathProbe CreateIDPathProbe() => new();

    private static void SceneIDValidation(string folder)
    {
        ResourceFileTypes.RegisterResource("Archive.IDPathProbe", CreateIDPathProbe);
        using (var source = new IDPathProbe())
        using (var firstProbe = new IDPathProbe { ResourceSceneUniqueID = "keep", FailPath = true })
        using (var secondProbe = new IDPathProbe { ResourceSceneUniqueID = "keep" })
        {
            source.Next = firstProbe; firstProbe.Next = secondProbe;
            var committed = System.IO.Path.Combine(folder, "committed-id-paths.e2dres");
            Reject<AggregateException>(() => ResourceSaver.Save(source, committed, SaverFlags.ReplaceSubresourcePaths));
            Check(firstProbe.ResourceSceneUniqueID == "keep" && secondProbe.ResourceSceneUniqueID.Length == 5 &&
                secondProbe.ResourceSceneUniqueID != "keep" && secondProbe.ResourcePath == committed + "::" + secondProbe.ResourceSceneUniqueID,
                "A committed path callback failure still publishes all planned IDs and attempts later resource paths");
            using var written = ResourceLoader.Load<IDPathProbe>(committed, ResourceLoader.CacheMode.IgnoreDeep);
            Check(written.Next!.Next!.ResourceSceneUniqueID == secondProbe.ResourceSceneUniqueID, "Committed file and source ID metadata agree after callback failure");
        }
        var path = System.IO.Path.Combine(folder, "ambiguous-ids.e2dres");
        using var root = new Data { Number = 1 }; using var first = new Data { Number = 2 }; using var second = new Data { Number = 3 };
        root.Next = first; first.Next = second; second.Next = root; ResourceSaver.Save(root, path);
        using var cached = ResourceLoader.Load<Data>(path); var oldNext = cached.Next;
        var payload = ResourceArchive.ReadPayload(path, out var endian, out var version);
        using var input = new StreamPeerBuffer { BigEndian = endian, DataArray = payload };
        var count = input.Get32(); Check(count == 3, "Malformed-ID fixture has three distinct graph identities");
        var definitions = new ArchiveDefinition[count];
        for (var i = 0; i < count; i++) definitions[i] = ResourceArchive.ReadDefinition(input, version, true);
        definitions[1] = definitions[1] with { SceneID = "2" }; definitions[2] = definitions[2] with { SceneID = "" };
        using var output = new StreamPeerBuffer { BigEndian = endian }; output.Put32(count);
        foreach (var d in definitions)
        {
            ResourceArchiveStrings.Write(output, d.Type.ID); ResourceArchive.WriteRole(output, d, version);
            ResourceArchiveStrings.Write(output, d.Name); output.PutU8(d.Local ? (byte)1 : (byte)0);
            ResourceArchiveStrings.Write(output, d.SceneID); ResourceArchiveStrings.Write(output, d.Path);
            output.Put64(d.UID); if (version >= 9) output.Put32(d.Script); output.Put32(d.Payload.Length); output.PutData(d.Payload);
        }
        var raw = output.DataArray; var original = System.IO.File.ReadAllBytes(path); var invalid = new byte[64 + raw.Length];
        original.AsSpan(0, 64).CopyTo(invalid); raw.CopyTo(invalid, 64);
        BinaryPrimitives.WriteInt32LittleEndian(invalid.AsSpan(24), raw.Length); BinaryPrimitives.WriteInt32LittleEndian(invalid.AsSpan(28), raw.Length);
        SHA256.HashData(raw).CopyTo(invalid, 32); System.IO.File.WriteAllBytes(path, invalid);
        var factories = _dataFactoryCalls;
        Reject<InvalidDataException>(() => ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.ReplaceDeep));
        Check(_dataFactoryCalls == factories && cached.Number == 1 && ReferenceEquals(cached.Next, oldNext) && !cached.IsDisposed,
            "Ambiguous legacy fallback paths reject before factories or cached graph replacement");
    }
}
