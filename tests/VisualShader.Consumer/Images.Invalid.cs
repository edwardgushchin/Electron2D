using Electron2D;
using System.Runtime.InteropServices;
using System.Text;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void ImageMetadataChecks(string folder)
    {
        using var graph = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "img-fallback-0-0-0-0.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        var vertex = graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex); var fragment = graph.GetSPIRV(RenderingDevice.ShaderStage.Fragment);
        var plain = RemoveImageMetadata(fragment);
        static string B(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        var symbol = B("image_value"); var good = $"Electron2D:image:2:{symbol}:{symbol}::0:0:0:0:0:0";
        var invalid = new[]
        {
            good.Replace("image:2:", "image:3:", StringComparison.Ordinal), "Electron2D:image:2:short",
            $"Electron2D:image:2:{symbol}:???:  :0:0:0:0:0:0", $"Electron2D:image:2:{symbol}:/w==::0:0:0:0:0:0",
            $"Electron2D:image:2:{symbol}:::0:0:0:0:0:0", $"Electron2D:image:2:{symbol}:{B("bad name")}::0:0:0:0:0:0",
            $"Electron2D:image:2:{symbol}:{B("gl_bad")}::0:0:0:0:0:0", $"Electron2D:image:2:{symbol}:{B("TEXTURE")}::0:0:0:0:0:0",
            $"Electron2D:image:2:{symbol}:{symbol}:{B("value")}:0:0:0:0:0:0", $"Electron2D:image:2:{symbol}:{symbol}:{B("GLOBAL_value")}:0:0:0:0:0:0",
            "Electron2D:image:2:???:aW1hZ2VfdmFsdWU=::0:0:0:0:0:0", "Electron2D:image:2:/w==:aW1hZ2VfdmFsdWU=::0:0:0:0:0:0",
            $"Electron2D:image:2:{symbol}:{symbol}::2:0:0:0:0:0", $"Electron2D:image:2:{symbol}:{symbol}::0:2:0:0:0:0",
            $"Electron2D:image:2:{symbol}:{symbol}::0:0:5:0:0:0", $"Electron2D:image:2:{symbol}:{symbol}::0:0:0:7:0:0",
            $"Electron2D:image:2:{symbol}:{symbol}::0:0:0:0:3:0", $"Electron2D:image:2:{symbol}:{symbol}::0:0:0:0:0:2",
            $"Electron2D:image:2:{symbol}:{symbol}::1:0:0:0:0:0", $"Electron2D:image:2:{B("absent")}:{B("absent")}::0:0:0:0:0:0",
            $"Electron2D:image:2:{B("TEXTURE")}:{B("TEXTURE")}::0:0:0:0:0:0", $"Electron2D:image:2:{symbol}:{symbol}::0:1:0:0:0:1",
            $"Electron2D:image:2:{symbol}:{symbol}::0:0:1:0:0:1", $"Electron2D:image:2:{symbol}:{symbol}::0:0:0:00:0:0"
        };
        foreach (var record in invalid)
        {
            RejectImageModule(vertex, AppendHint(plain, record));
            Check(graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).AsSpan().SequenceEqual(vertex) && graph.GetSPIRV().AsSpan().SequenceEqual(fragment), "Invalid image metadata preserves both live stages.");
        }
        RejectImageModule(vertex, AppendHint(AppendHint(plain, good), good));
        RejectImageModule(AppendHint(vertex, good), fragment);
        using (var shared = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "img-shared.e2dres"), ResourceLoader.CacheMode.IgnoreDeep))
        {
            var v = shared.GetSPIRV(RenderingDevice.ShaderStage.Vertex); var f = shared.GetSPIRV();
            var changed = AppendHint(RemoveImageMetadata(v), $"Electron2D:image:2:{symbol}:{symbol}::0:0:0:1:0:0"); var failed = false;
            try { shared.SetSPIRV(changed, f); } catch (ArgumentException) { failed = true; }
            Check(failed && shared.GetSPIRV(RenderingDevice.ShaderStage.Vertex).AsSpan().SequenceEqual(v) && shared.GetSPIRV().AsSpan().SequenceEqual(f), "Conflicting paired-stage image policies preserve both live modules.");
        }
        ImageReloadChecks(folder);
        Console.WriteLine($"Rejected {invalid.Length + 3} malformed, misplaced or duplicate sampled-image metadata modules without changing either stage.");

        void RejectImageModule(byte[] v, byte[] f)
        {
            var failed = false;
            try { graph.SetSPIRV(v, f); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException) { failed = true; }
            Check(failed, "Invalid image metadata was rejected.");
        }
    }

    private static void ImageReloadChecks(string folder)
    {
        using var imageShader = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "img-fallback-0-0-0-0.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        using var colorShader = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "img-param-0-1-0-0-0.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        using var screenShader = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "img-screen-0-0-0.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        using var material = new ShaderMaterial { Shader = imageShader };
        var vertex = imageShader.GetSPIRV(RenderingDevice.ShaderStage.Vertex); var fragment = imageShader.GetSPIRV();
        material.SetShaderParameter("image_value", texture); imageShader.SetDefaultTextureParameter("image_value", texture);
        imageShader.SetSPIRV(colorShader.GetSPIRV(RenderingDevice.ShaderStage.Vertex), colorShader.GetSPIRV());
        Check(ReferenceEquals(material.GetShaderParameter("image_value"), texture) && ReferenceEquals(imageShader.GetDefaultTextureParameter("image_value"), texture),
            "Changing an image interpretation preserves compatible material overrides and defaults.");
        imageShader.SetSPIRV(screenShader.GetSPIRV(RenderingDevice.ShaderStage.Vertex), screenShader.GetSPIRV());
        Reject<ArgumentException>(() => material.GetShaderParameter("image_value"));
        imageShader.SetSPIRV(vertex, fragment);
        Check(material.GetShaderParameter("image_value") is null && imageShader.GetDefaultTextureParameter("image_value") is null,
            "Reload through a renderer-owned screen image drops the former material override and default.");
        Check(!texture.IsDisposed, "Image ownership changes preserve caller-owned resources.");
    }

    private static byte[] RemoveImageMetadata(byte[] code)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code); using var output = new MemoryStream();
        output.Write(code.AsSpan(0, 20));
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var image = (words[at] & 0xffff) == 7 && count > 2 && MemoryMarshal.AsBytes(words.Slice(at + 2, count - 2)).StartsWith("Electron2D:image:"u8);
            if (!image) output.Write(MemoryMarshal.AsBytes(words.Slice(at, count)));
        }
        return output.ToArray();
    }
}
