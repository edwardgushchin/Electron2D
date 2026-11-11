using System.Buffers.Binary;
using System.Globalization;
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
        WriteArchiveDefinitions(path, endian, version, definitions);
        var factories = _dataFactoryCalls;
        Reject<InvalidDataException>(() => ResourceLoader.Load<Data>(path, ResourceLoader.CacheMode.ReplaceDeep));
        Check(_dataFactoryCalls == factories && cached.Number == 1 && ReferenceEquals(cached.Next, oldNext) && !cached.IsDisposed,
            "Ambiguous legacy fallback paths reject before factories or cached graph replacement");
        Reject<InvalidDataException>(() => ResourceLoader.Exists<Data>(path + "::2"));
        definitions[1] = definitions[1] with { SceneID = "first" };
        WriteArchiveDefinitions(path, endian, version, definitions);
        using var legacy = ResourceLoader.Load<Data>(path + "::2", ResourceLoader.CacheMode.Ignore);
        Check(legacy.Number == 3 && legacy.ResourceSceneUniqueID.Length == 0 && legacy.ResourcePath == path + "::2",
            "Legacy fallback uses the table position while preserving the empty stored ID.");
    }

    private static void WriteArchiveDefinitions(string path, bool endian, int version, ArchiveDefinition[] definitions)
    {
        using var output = new StreamPeerBuffer { BigEndian = endian }; output.Put32(definitions.Length);
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
    }

    private static void SubresourceAddresses(string folder)
    {
        var path = System.IO.Path.Combine(folder, "addressed.e2dres");
        var localized = System.IO.Path.Combine(folder, "addressed.fr.e2dres");
        var address = path + "::Palette";
        using var root = new Data { Number = 1, ResourceSceneUniqueID = "RootOnly" };
        using var child = new Data { Number = 2, ResourceSceneUniqueID = "Palette", Next = root };
        root.Next = child;
        const SaverFlags flags = SaverFlags.Compress | SaverFlags.SaveBigEndian;
        ResourceSaver.Save(root, path, flags);
        var id = ResourceSaver.GetResourceIDForPath(path);
        var uidAddress = ResourceUID.IDToText(id) + "::Palette";

        var factories = _dataFactoryCalls;
        Check(ResourceLoader.Exists<Resource>(address) && ResourceLoader.Exists<Data>(uidAddress) &&
            !ResourceLoader.Exists<Curve>(address) && !ResourceLoader.Exists<Data>(path + "::palette") &&
            !ResourceLoader.Exists<Data>(path + "::RootOnly") && !ResourceLoader.Exists<Data>(path + "::") &&
            !ResourceLoader.Exists<Data>(path + "::Palette::other") &&
            !ResourceLoader.Exists<Data>(System.IO.Path.Combine(folder, "missing.e2dres::Palette")) &&
            !ResourceLoader.HasCached(address) && _dataFactoryCalls == factories,
            "Cold subresource existence selects typed embedded IDs without construction or caching.");
        Reject<FileNotFoundException>(() => ResourceLoader.Load<Data>(path + "::Missing"));
        Check(_dataFactoryCalls == factories, "Missing IDs fail before constructing a graph.");

        using var whole = ResourceLoader.Load<Data>(path);
        using var selected = ResourceLoader.Load<Data>(uidAddress);
        var previousRoot = selected.Next!;
        Check(selected.Number == 2 && selected.ResourcePath == address && selected.ResourceSceneUniqueID == "Palette" &&
            previousRoot.ResourcePath == path && ReferenceEquals(previousRoot.Next, selected) && !ReferenceEquals(whole.Next, selected) &&
            ReferenceEquals(ResourceLoader.Load<Resource>(address), selected) && ResourceLoader.HasCached(uidAddress) &&
            ReferenceEquals(ResourceLoader.GetCachedRef<Data>(uidAddress), selected), "Direct reuse preserves an independently owned graph and a UID alias for its cache key.");
        using (var ignored = ResourceLoader.Load<Data>(address, ResourceLoader.CacheMode.IgnoreDeep))
        {
            Check(!ReferenceEquals(ignored, selected) && ReferenceEquals(ignored.Next!.Next, ignored) &&
                ReferenceEquals(ResourceLoader.GetCachedRef<Data>(address), selected), "IgnoreDeep builds an unregistered independent graph.");
        }

        root.Number = 11;
        child.Number = 12;
        ResourceSaver.Save(root, path, flags);
        Check(ReferenceEquals(ResourceLoader.Load<Data>(address, ResourceLoader.CacheMode.Replace), selected) &&
            selected.Number == 12 && selected.Next!.Number == 11 && ReferenceEquals(selected.Next.Next, selected) &&
            previousRoot.IsDisposed && whole.Number == 1 && whole.Next!.Number == 2, "Replace rebinds the cycle and retires only the old selected graph.");

        previousRoot = selected.Next!;
        child.Number = 22;
        ResourceSaver.Save(root, path);
        ResourceLoader.LoadThreadedRequest<Data>(address, useSubThreads: true, cacheMode: ResourceLoader.CacheMode.ReplaceDeep);
        Check(ResourceLoader.LoadThreadedGetStatus(uidAddress) != ResourceLoader.ThreadLoadStatus.InvalidResource &&
            ReferenceEquals(ResourceLoader.LoadThreadedGet<Data>(uidAddress), selected) &&
            selected.Number == 22 && ReferenceEquals(selected.Next!.Next, selected) && previousRoot.IsDisposed &&
            ResourceLoader.LoadThreadedGetStatus(address) == ResourceLoader.ThreadLoadStatus.InvalidResource, "Threaded replacement and collection use the same selected cache identity.");
        ResourceLoader.LoadThreadedRequest<Curve>(address, cacheMode: ResourceLoader.CacheMode.ReplaceDeep);
        Reject<InvalidDataException>(() => ResourceLoader.LoadThreadedGet<Curve>(address));
        Check(selected.Number == 22 && ReferenceEquals(selected.Next!.Next, selected) &&
            ResourceUID.GetIDPath(id) == path, "Wrong typed replacement preserves the selected graph and whole-file UID.");

        var originalCulture = TranslationServer.Culture;
        var originalRemaps = ProjectSettings.Get(ProjectSettings.LocaleResourceRemaps);
        try
        {
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
            child.Number = 32;
            ResourceSaver.Save(root, localized, flags);
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, [new(path, "fr", localized)]);
            previousRoot = selected.Next!;
            TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
            Check(selected.Number == 32 && selected.ResourcePath == address &&
                ReferenceEquals(selected.Next!.Next, selected) && previousRoot.IsDisposed,
                "A remap added after direct load refreshes the logical embedded identity and preserves its cycle.");

            previousRoot = selected.Next!;
            child.ResourceSceneUniqueID = "Other";
            ResourceSaver.Save(root, localized);
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
            Check(selected.Number == 22 && ReferenceEquals(selected.Next!.Next, selected) && previousRoot.IsDisposed,
                "Returning to the source culture reloads the original graph.");
            previousRoot = selected.Next!;
            Reject<AggregateException>(() => TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR"));
            Check(selected.Number == 22 && ReferenceEquals(selected.Next, previousRoot) && !previousRoot.IsDisposed,
                "A locale variant missing the selected ID leaves the existing graph live.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, originalRemaps);
            TranslationServer.Culture = originalCulture;
        }

        var ownedRoot = selected.Next!;
        selected.Dispose();
        Check(ownedRoot.IsDisposed && !ResourceLoader.HasCached(address) && !whole.IsDisposed && !whole.Next!.IsDisposed,
            "Disposing the direct result retires its graph without invalidating a whole-file load.");
    }
}
