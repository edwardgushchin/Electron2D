using Electron2D;

internal static class TextureArrayTests
{
    internal static Shader Load(string fixture)
    {
        using var stream = typeof(TextureArrayTests).Assembly.GetManifestResourceStream("TestShaders." + fixture + ".spv")!;
        using var output = new MemoryStream(); stream.CopyTo(output); return Shader.CreateFromSPIRV(output.ToArray());
    }
    internal static void Run()
    {
        using var red = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); red.Fill(Colors.Red);
        using var blue = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); blue.Fill(Colors.Blue);
        using var array = new TextureArray();
        Check(array.GetWidth() == 0 && array.GetHeight() == 0 && array.GetLayers() == 0 && array.GetFormat() == Image.Format.L8 && !array.HasMipmaps() && array.GetLayeredType() == TextureLayered.LayeredType.Array, "Empty metadata and role.");
        var rid = array.GetRID(); Check(rid.IsValid() && array.GetRID() == rid, "Resource-owned stable identity.");
        Reject<InvalidOperationException>(() => array.UpdateLayer(red, 0)); Reject<ArgumentOutOfRangeException>(() => array.GetLayerData(0)); Reject<ArgumentException>(() => array.CreateFromImages([])); Reject<ArgumentNullException>(() => array.CreateFromImages([red, null!]));
        array.CreateFromImages([red, blue]); Check(array.GetRID() == rid && array.GetLayers() == 2, "Creation preserves resource identity.");
        red.Fill(Colors.Green); using (var layer = array.GetLayerData(0)!) { Check(layer.GetPixel(0, 0) == Colors.Red, "Inputs copied."); layer.Fill(Colors.White); }
        using (var layer = array.GetLayerData(0)!) Check(layer.GetPixel(0, 0) == Colors.Red, "Outputs copied.");
        using var small = Image.CreateEmpty(2, 4, false, Image.Format.Rgba8); using var other = Image.CreateEmpty(4, 4, false, Image.Format.Rgb8);
        using var mips = Image.CreateEmpty(4, 4, true, Image.Format.Rgba8);
        Reject<ArgumentException>(() => array.CreateFromImages([blue, small])); Reject<ArgumentException>(() => array.CreateFromImages([blue, other])); Reject<ArgumentException>(() => array.CreateFromImages([blue, mips]));
        Reject<ArgumentException>(() => array.UpdateLayer(small, 0)); Reject<ArgumentOutOfRangeException>(() => array.UpdateLayer(blue, -1)); Reject<ArgumentOutOfRangeException>(() => array.GetLayerData(2));
        var snapshot = array.CaptureLayers()!; array.UpdateLayer(red, 1); Check(ReferenceEquals(snapshot.Allocation, array.CaptureLayers()!.Allocation), "Compatible update keeps native allocation.");
        using (var layer = array.GetLayerData(0)!) Check(layer.GetPixel(0, 0) == Colors.Red, "Failed replacements and other-layer update preserve layer zero.");
        using var copy = (TextureArray)array.Duplicate(true); copy.UpdateLayer(blue, 0); using (var layer = array.GetLayerData(0)!) Check(layer.GetPixel(0, 0) == Colors.Red, "Duplicates have independent publication.");
        Action<Resource> failing = _ => throw new ApplicationException("fixture"); array.Changed += failing; Reject<ApplicationException>(() => array.UpdateLayer(blue, 0)); array.Changed -= failing;
        using (var layer = array.GetLayerData(0)!) Check(layer.GetPixel(0, 0) == Colors.Blue, "Notification failure retains committed pixels.");
        using var placeholder = array.CreatePlaceholder(); Check(placeholder.Size == new Vector2i(4, 4) && placeholder.Layers == 2 && placeholder.GetFormat() == Image.Format.Rgb8 && !placeholder.HasMipmaps() && placeholder.GetLayerData(int.MinValue) is null, "Metadata-only placeholder.");
        var changes = 0; placeholder.Changed += _ => changes++; placeholder.Size = placeholder.Size; placeholder.Layers = -3; Check(changes == 1 && placeholder.Layers == -3, "Pinned placeholder notification policy.");
        using var placeholderCopy = (PlaceholderTextureArray)placeholder.Duplicate(); Check(placeholderCopy.Size == placeholder.Size && placeholderCopy.Layers == -3, "Placeholder stored state.");
        using var custom = new Producer(blue); var first = custom.CaptureLayers(); Check(ReferenceEquals(first, custom.CaptureLayers()) && custom.Reads == 1, "Custom producer snapshot cache."); custom.Invalidate(); Check(!ReferenceEquals(first, custom.CaptureLayers()) && custom.Reads == 2, "Custom Changed invalidation."); custom.Role = (TextureLayered.LayeredType)99; custom.Invalidate(); Reject<NotSupportedException>(() => custom.CaptureLayers());
        using var mono = Image.CreateEmpty(1, 1, false, Image.Format.L8); using var monoArray = new TextureArray(); monoArray.CreateFromImages([mono]);
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-array-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = System.IO.Path.Combine(directory, "layers.e2dres"); ResourceSaver.Save(array, path); using var loaded = ResourceLoader.Load<TextureArray>(path, ResourceLoader.CacheMode.Ignore); Check(loaded.GetLayers() == 2, "Typed archive reconstruction.");
            using (var layer = loaded.GetLayerData(0)!) Check(layer.GetPixel(0, 0) == Colors.Blue, "Archive pixels.");
            ResourceSaver.Save(monoArray, path); using var loadedMono = ResourceLoader.Load<TextureArray>(path, ResourceLoader.CacheMode.Ignore); Check(loadedMono.GetFormat() == Image.Format.L8, "Smallest archive payload.");
            ResourceSaver.Save(placeholder, path); using var loadedPlaceholder = ResourceLoader.Load<PlaceholderTextureArray>(path, ResourceLoader.CacheMode.Ignore); Check(loadedPlaceholder.Layers == -3 && loadedPlaceholder.Size == placeholder.Size, "Placeholder archive.");
        }
        finally { Directory.Delete(directory, true); }
        foreach (var fixture in new[] { "ArrayGLSL", "ArrayHLSL" })
        {
            using var shader = Load(fixture); using var material = new ShaderMaterial { Shader = shader }; using var white = ImageTexture.CreateFromImage(blue);
            Check(shader.GetShaderUniformList().Single(p => p.Name == "layers") is PropertyDescriptor<ShaderMaterial, TextureLayered?>, "Layered property descriptor.");
            shader.SetDefaultLayeredTextureParameter("layers", array); shader.SetDefaultTextureParameter("detailMap", white);
            material.SetShaderLayeredParameter("layers", copy); Check(ReferenceEquals(material.GetShaderLayeredParameter("layers"), copy) && ReferenceEquals(shader.GetDefaultLayeredTextureParameter("layers"), array), "Default/override typed access.");
            Reject<ArgumentException>(() => material.SetShaderParameter("layers", white)); Reject<ArgumentException>(() => material.SetShaderLayeredParameter("detailMap", array)); Reject<ArgumentException>(() => shader.GetDefaultTextureParameter("layers")); Reject<ArgumentOutOfRangeException>(() => shader.GetDefaultLayeredTextureParameter("layers", 1));
            using var ordinary = Load("ArrayOrdinary"); shader.SetSPIRV(ordinary.GetSPIRV()); Check(material.GetShaderParameter("layers") is null && shader.GetDefaultTextureParameter("layers") is null, "Reload drops incompatible shape defaults and overrides.");
            shader.SetSPIRV(LoadBytes(fixture)); material.SetShaderLayeredParameter("layers", array); shader.SetDefaultLayeredTextureParameter("layers", array);
            using var materialCopy = (ShaderMaterial)material.Duplicate(true); Check(!ReferenceEquals(materialCopy.GetShaderLayeredParameter("layers"), array) && materialCopy.GetShaderLayeredParameter("layers")!.GetLayers() == 2, "Deep material resource policy.");
            materialCopy.GetShaderLayeredParameter("layers")!.Dispose(); materialCopy.Shader!.GetDefaultTextureParameter("detailMap")?.Dispose(); materialCopy.Shader.Dispose();
        }
        Reject<ArgumentException>(() => TexturePixels.FromImage(blue, 1));
        _ = array.CaptureLayers(); for (var i = 0; i < 64; i++) { _ = array.GetRID(); _ = array.CaptureLayers(); }
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) { _ = array.GetWidth(); _ = array.GetLayers(); _ = array.GetRID(); _ = array.CaptureLayers(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed metadata/snapshot reads allocate zero managed bytes.");
        array.Dispose(); Reject<ObjectDisposedException>(() => array.GetWidth()); Reject<ArgumentException>(() => RenderingTextureRegistry.ResolveLayered(rid));
        Console.WriteLine("Texture arrays: copied homogeneous layers, atomic updates, callbacks, custom snapshots, typed samplers/reload/duplication, archives/placeholders and warmed reads passed.");
    }
    private static byte[] LoadBytes(string fixture) { using var shader = Load(fixture); return shader.GetSPIRV(); }
    private sealed class Producer(Image image) : TextureLayered
    {
        internal int Reads;
        internal LayeredType Role;
        public override LayeredType GetLayeredType() => Role;
        internal void Invalidate() => EmitChanged();
        public override Image.Format GetFormat() => image.PixelFormat;
        public override int GetWidth() => image.Width;
        public override int GetHeight() => image.Height;
        public override int GetLayers() => 1;
        public override bool HasMipmaps() => image.HasMipmaps;
        public override Image? GetLayerData(int layerIndex) { Reads++; return (Image)image.Duplicate(); }
    }
    internal static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
