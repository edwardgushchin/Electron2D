using System.Buffers.Binary;
using Electron2D;
using Path = System.IO.Path;
using static TextureArrayTests;

internal static partial class IntegerSourceTextureTests
{
    internal static readonly Image.Format[] Formats = [Image.Format.R16I, Image.Format.Rg16I, Image.Format.Rgb16I, Image.Format.Rgba16I];

    internal static Image Source(Image.Format format, int seed = 0)
    {
        using var layout = Image.CreateEmpty(7, 5, true, format); var data = layout.GetData(); var channels = (int)format - (int)Image.Format.R16I + 1;
        for (var mip = 0; mip <= layout.MIPMapCount; ++mip)
            for (var pixel = 0; pixel < Math.Max(1, 7 >> mip) * Math.Max(1, 5 >> mip); ++pixel)
                for (var channel = 0; channel < channels; ++channel)
                {
                    var value = channel == 3 ? pixel % 3 == 0 ? 0 : pixel % 3 == 1 ? 1 : ushort.MaxValue : 123 + seed * 4000 + channel * 7000 + mip * 13000 + pixel * 101;
                    BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(layout.GetMIPMapOffset(mip) + (pixel * channels + channel) * 2), (ushort)value);
                }
        return Image.CreateFromData(7, 5, true, format, data);
    }

    internal static void RunManaged()
    {
        var directory = Path.Combine(Path.GetTempPath(), "e2d-integer-source-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var format in Formats)
            {
                using var source = Source(format); var original = source.GetData(); using var texture = ImageTexture.CreateFromImage(source);
                foreach (var bigEndian in new[] { false, true })
                {
                    using var decoded = new Image(); decoded.LoadKTXFromBuffer(ImageKTXTests.KTX1(source, bigEndian: bigEndian));
                    using var ktxTexture = ImageTexture.CreateFromImage(decoded);
                    Check(decoded.PixelFormat == format && decoded.GetData().AsSpan().SequenceEqual(original) && ShaderTexture.ResourcePixelType(ktxTexture) == 2, "Integer KTX byte order, row padding, mips and texture consumer.");
                }
                using var array = new TextureArray(); array.CreateFromImages([source, source]); var identity = texture.GetRID(); var arrayIdentity = array.GetRID();
                Check(texture.PixelFormat == format && texture.HasMIPMaps && texture.MIPMapCount == 2 && texture.HasAlpha == (format == Image.Format.Rgba16I), "Integer source metadata and complete mips.");
                using (var copy = texture.GetImage()!) Check(copy.PixelFormat == format && copy.GetData().AsSpan().SequenceEqual(original), "ImageTexture keeps original integer lanes.");
                using (var copy = array.GetLayerData(1)!) Check(copy.PixelFormat == format && copy.GetData().AsSpan().SequenceEqual(original), "Array keeps original integer lanes.");
                var prepared = texture.CapturePixels()!;
                Check(prepared.Upload.Format == Image.Format.Rgba16I && prepared.BytesPerPixel == 8, "Prepared unsigned storage uses four 16-bit lanes.");
                var packed = new byte[42 * 8]; var channels = (int)format - (int)Image.Format.R16I + 1;
                for (var pixel = 0; pixel < 42; ++pixel)
                    for (var channel = 0; channel < 4; ++channel)
                        BinaryPrimitives.WriteUInt16LittleEndian(packed.AsSpan(pixel * 8 + channel * 2), channel < channels ? BinaryPrimitives.ReadUInt16LittleEndian(original.AsSpan((pixel * channels + channel) * 2)) : channel == 3 ? (ushort)1 : (ushort)0);
                Check(prepared.Upload.Data.AsSpan().SequenceEqual(packed), "Prepared complete chain preserves every integer bit; missing RGB is zero and missing alpha is one.");
                var budget = original.Length + packed.Length; _ = TexturePixels.FromImage(source, budget); Reject<ArgumentException>(() => TexturePixels.FromImage(source, budget - 1));
                for (var y = 0; y < 5; ++y) for (var x = 0; x < 7; ++x) Check(texture.IsPixelOpaque(x, y) == (source.GetPixel(x, y).A > .1f), "Opacity retains raw integer scalar meaning.");
                using var shader = Load("IntegerCanvasGLSL"); shader.SetDefaultTextureParameter("values", texture);
                using var material = new ShaderMaterial { Shader = shader }; material.SetShaderParameter("values", texture);
                Check(ReferenceEquals(material.GetShaderParameter("values"), texture), "Ordinary unsigned sampler accepts source textures.");
                Reject<ArgumentException>(() => shader.SetDefaultLayeredTextureParameter("layers", array));
                using var floatShader = Load("FloatCanvas"); Reject<ArgumentException>(() => floatShader.SetDefaultTextureParameter("values", texture));
                var ordinary = Path.Combine(directory, "ordinary.ctex"); var layered = Path.Combine(directory, "layers.ctexarray");
                File.WriteAllBytes(ordinary, CompressedTextureTests.Container(source)); File.WriteAllBytes(layered, CompressedArrayTests.Container([(source, 0, null), (source, 0, null)]));
                using var imported = ResourceLoader.Load<CompressedTexture>(ordinary, ResourceLoader.CacheMode.Ignore); using var importedArray = ResourceLoader.Load<CompressedTextureArray>(layered, ResourceLoader.CacheMode.Ignore);
                Check(imported.PixelFormat == format && imported.GetSize() == new Vector2(14, 10) && importedArray.GetFormat() == format && importedArray.GetLayers() == 2, "Both imported resource families accept integer sources.");
                ResourceLoader.LoadThreadedRequest<Texture>(ordinary, cacheMode: ResourceLoader.CacheMode.Ignore);
                using var worker = ResourceLoader.LoadThreadedGet<Texture>(ordinary);
                ResourceLoader.LoadThreadedRequest<TextureLayered>(layered, cacheMode: ResourceLoader.CacheMode.Ignore);
                using var layeredWorker = ResourceLoader.LoadThreadedGet<TextureLayered>(layered);
                using (var copied = worker.GetImage()!) Check(copied.GetData().AsSpan().SequenceEqual(original), "Worker ordinary integer preparation preserves all bits.");
                using (var copied = layeredWorker.GetLayerData(1)!) Check(copied.GetData().AsSpan().SequenceEqual(original), "Worker layered integer preparation preserves all bits.");
                using var cached = ResourceLoader.Load<CompressedTexture>(ordinary); using var cachedArray = ResourceLoader.Load<CompressedTextureArray>(layered);
                var cachedRID = cached.GetRID(); var cachedArrayRID = cachedArray.GetRID();
                using (var next = Source(format, 1))
                {
                    File.WriteAllBytes(ordinary, CompressedTextureTests.Container(next)); File.WriteAllBytes(layered, CompressedArrayTests.Container([(source, 0, null), (next, 0, null)]));
                    Check(ReferenceEquals(cached, ResourceLoader.Load<CompressedTexture>(ordinary, ResourceLoader.CacheMode.Replace)) && ReferenceEquals(cachedArray, ResourceLoader.Load<CompressedTextureArray>(layered, ResourceLoader.CacheMode.Replace)), "Integer cache replacement keeps both typed identities.");
                    using var copied = cached.GetImage()!; using var layer = cachedArray.GetLayerData(1)!;
                    Check(copied.GetData().AsSpan().SequenceEqual(next.GetData()) && layer.GetData().AsSpan().SequenceEqual(next.GetData()), "Integer cache replacement publishes exact new pixels.");
                }
                File.WriteAllBytes(ordinary, [1]); File.WriteAllBytes(layered, [1]);
                Reject<InvalidDataException>(() => cached.Load(ordinary)); Reject<InvalidDataException>(() => cachedArray.Load(layered));
                Check(cached.GetRID() == cachedRID && cachedArray.GetRID() == cachedArrayRID && cached.PixelFormat == format && cachedArray.GetFormat() == format, "Malformed reload preserves integer resource identities and metadata.");
                var resources = new Resource[] { texture, array, imported, importedArray };
                for (var i = 0; i < resources.Length; ++i) ResourceSaver.Save(resources[i], Path.Combine(directory, i + ".e2dres"));
                File.Delete(ordinary); File.Delete(layered);
                for (var i = 0; i < resources.Length; ++i)
                {
                    using var restored = ResourceLoader.Load<Resource>(Path.Combine(directory, i + ".e2dres"), ResourceLoader.CacheMode.Ignore);
                    using var copied = resources[i].Duplicate(true);
                    using var a = restored is Texture t ? t.GetImage()! : ((TextureLayered)restored).GetLayerData(0)!;
                    using var b = copied is Texture u ? u.GetImage()! : ((TextureLayered)copied).GetLayerData(0)!;
                    Check(a.PixelFormat == format && b.PixelFormat == format && a.GetData().AsSpan().SequenceEqual(original) && b.GetData().AsSpan().SequenceEqual(original), "Archives and copies retain original integer formats without source I/O.");
                }
                using var replacement = Source(format, 1); texture.Update(replacement); array.UpdateLayer(replacement, 1);
                Check(texture.GetRID() == identity && array.GetRID() == arrayIdentity, "Compatible integer updates retain resource identities.");
                using var wrong = Image.CreateEmpty(7, 5, true, Image.Format.Rgba8);
                var retained = texture.CapturePixels(); var layers = array.CaptureLayers();
                Reject<ArgumentException>(() => texture.Update(wrong)); Reject<ArgumentException>(() => array.UpdateLayer(wrong, 1));
                Check(ReferenceEquals(retained, texture.CapturePixels()) && ReferenceEquals(layers, array.CaptureLayers()), "Rejected integer updates preserve prepared state.");
                Action<Resource> failed = _ => throw new ArithmeticException("integer observer");
                texture.Changed += failed; array.Changed += failed;
                Reject<ArithmeticException>(() => texture.Update(source)); Reject<ArithmeticException>(() => array.UpdateLayer(source, 1));
                texture.Changed -= failed; array.Changed -= failed;
                using (var copied = texture.GetImage()!) Check(copied.GetData().AsSpan().SequenceEqual(original), "Observer failure retains published integer image.");
                using (var copied = array.GetLayerData(1)!) Check(copied.GetData().AsSpan().SequenceEqual(original), "Observer failure retains published integer layer.");
                using var typed = Load("IntegerSourcesGLSL"); using var typedMaterial = new ShaderMaterial { Shader = typed };
                typed.SetDefaultTextureParameter("values", texture); typed.SetDefaultLayeredTextureParameter("layers", array);
                typedMaterial.SetShaderParameter("values", texture); typedMaterial.SetShaderLayeredParameter("layers", array);
                using var floating = ImageTexture.CreateFromImage(wrong); using var floatingArray = new TextureArray(); floatingArray.CreateFromImages([wrong, wrong]);
                Reject<ArgumentException>(() => typedMaterial.SetShaderParameter("values", floating)); Reject<ArgumentException>(() => typedMaterial.SetShaderLayeredParameter("layers", floatingArray));
                Check(ReferenceEquals(typedMaterial.GetShaderParameter("values"), texture) && ReferenceEquals(typedMaterial.GetShaderLayeredParameter("layers"), array), "Scalar mismatch is atomic for both sampler shapes.");
                typed.SetSPIRV(floatShader.GetSPIRV());
                Check(typed.GetDefaultTextureParameter("values") is null && typedMaterial.GetShaderParameter("values") is null, "Reload drops incompatible ordinary scalar defaults and overrides.");
                using var originalShader = Load("IntegerSourcesGLSL"); typed.SetSPIRV(originalShader.GetSPIRV());
                typed.SetDefaultLayeredTextureParameter("layers", array); typedMaterial.SetShaderLayeredParameter("layers", array);
                using var floatLayers = Load("ArrayGLSL"); typed.SetSPIRV(floatLayers.GetSPIRV());
                Check(typed.GetDefaultLayeredTextureParameter("layers") is null && typedMaterial.GetShaderLayeredParameter("layers") is null, "Reload drops incompatible layered scalar defaults and overrides.");
                for (var i = 0; i < 64; ++i) { _ = ShaderTexture.ResourcePixelType(texture); _ = ShaderTexture.ResourcePixelType(array); _ = texture.IsPixelOpaque(i, i); }
                var before = GC.GetAllocatedBytesForCurrentThread(); var sum = 0;
                for (var i = 0; i < 1000; ++i) sum += ShaderTexture.ResourcePixelType(texture) + ShaderTexture.ResourcePixelType(array) + texture.GetWidth();
                Check(GC.GetAllocatedBytesForCurrentThread() == before && sum == 11000, "Prepared scalar metadata reads allocate zero managed bytes.");
            }
        }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("Integer source textures: all four formats, exact lanes/mips and storage limits, metadata/opacity, typed samplers, imported images/arrays, copies/archives, updates and zero-allocation reads passed.");
    }
}
