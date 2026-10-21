using System.Runtime.InteropServices;
using System.Text;
using Electron2D;
using Path = System.IO.Path;

internal static partial class DefaultChecks
{
    private static byte[] WithoutDefaults(byte[] code, string? name = null)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code); using var bytes = new MemoryStream();
        bytes.Write(MemoryMarshal.AsBytes(words[..5]));
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); var remove = false;
            if ((words[at] & 0xffff) == 7)
            {
                var data = MemoryMarshal.AsBytes(words.Slice(at + 2, count - 2));
                var text = Encoding.UTF8.GetString(data[..data.IndexOf((byte)0)]);
                if (text.StartsWith("Electron2D:default:", StringComparison.Ordinal))
                    remove = name is null || Encoding.UTF8.GetString(Convert.FromBase64String(text.Split(':')[5])) == name;
            }
            if (!remove) bytes.Write(MemoryMarshal.AsBytes(words.Slice(at, count))); at += count;
        }
        return bytes.ToArray();
    }
    private static void Ownership(ShaderMaterial material)
    {
        var shader = material.Shader!; var original = shader.GetSPIRV();
        try
        {
            var code = Annotate(WithoutDefaults(original, "f1"), "f1", "f1", 0, [Bits(.9f)]);
            code = Annotate(WithoutDefaults(code, "f2"), "f2", "f2", 0, [Bits(.8f), Bits(.7f)]);
            code = Annotate(WithoutDefaults(code, "f3"), "f3", "f3", 0, [Bits(.6f), Bits(.5f), Bits(.4f)]);
            shader.SetSPIRV(code);
            Check(material.GetShaderParameter<float>("f1") == .9f, "Unstored defaults follow reload after storage/copy.");
            Check(material.GetShaderParameter<Vector2>("f2") == new Vector2(.125f, .25f), "Explicit values equal to old defaults remain owned after storage/copy.");
            Check(material.GetShaderParameter<Vector3>("f3") == Vector3.Zero, "Explicit zero overrides survive storage/copy.");
        }
        finally { shader.SetSPIRV(original); }
        material.SetShaderParameter("f3", null); Roles(material);
    }
    private static void Archive(ShaderMaterial material)
    {
        Roles(material, author: true); material.SetShaderParameter("f1", null); material.SetShaderParameter("f3", Vector3.Zero);
        using (var shallow = (ShaderMaterial)material.Duplicate()) Ownership(shallow);
        using (var deep = (ShaderMaterial)material.DuplicateDeep(DeepDuplicateMode.All)) Ownership(deep);
        using (var copied = new ShaderMaterial()) { copied.CopyFromResource(material); Ownership(copied); }
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-default-storage-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            foreach (var flags in new[] { SaverFlags.None, SaverFlags.Compress | SaverFlags.SaveBigEndian })
            {
                var path = Path.Combine(directory, "defaults.e2dres"); ResourceSaver.Save(material, path, flags);
                using var loaded = ResourceLoader.Load<ShaderMaterial>(path, ResourceLoader.CacheMode.IgnoreDeep); Ownership(loaded);
                Fresh("read", path);
            }
        }
        finally { Directory.Delete(directory, true); }
        material.SetShaderParameter("f2", null); material.SetShaderParameter("f3", null);
        Console.WriteLine("Copied, compressed and big-endian fresh-process material defaults passed.");
    }
    private static void ReadArchive(string path)
    {
        using var material = ResourceLoader.Load<ShaderMaterial>(path, ResourceLoader.CacheMode.IgnoreDeep); Ownership(material);
    }
    private static void Fresh(string mode, string path)
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
        if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
        start.ArgumentList.Add(mode); start.ArgumentList.Add(path);
        using var process = System.Diagnostics.Process.Start(start)!;
        process.WaitForExit(); Check(process.ExitCode == 0, "Fresh process restores embedded defaults and explicit ownership.");
    }
    private static void InvalidMetadata()
    {
        var code = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders/ValuesGLSL.spv"));
        using var shader = Shader.CreateFromSPIRV(code); var before = shader.GetSPIRV();
        foreach (var invalid in new[] {
            Annotate(code, "missing", "f1", 0, [Bits(1)]), Annotate(code, "rgb", "f2", 0, [Bits(1), Bits(2)]),
            Annotate(code, "rgb", "f3", 1, [Bits(1), Bits(2), Bits(3)]), Annotate(code, "rgb", "f3", 0, [Bits(1)]),
            Annotate(code, "rgb", "f3", 0, [Bits(float.NaN), 0, 0]), Annotate(code, "rgb", "f3", 0, [Bits(float.PositiveInfinity), 0, 0]),
            Annotate(code, "pair", "u2", 0, [1, 2, 3]), AnnotateText(code, "Electron2D:default:1:f3:0:!!!!:!!!!"),
            AnnotateText(code, "Electron2D:default:1:f3:0:/w==:AAAAAA=="),
            Annotate(Annotate(code, "rgb", "f3", 0, [0, 0, 0]), "rgb", "f3", 0, [0, 0, 0]) })
        {
            Reject<ArgumentException>(() => shader.SetSPIRV(invalid)); Check(shader.GetSPIRV().SequenceEqual(before), "Rejected default metadata preserves current shader.");
        }
        Reject<NotSupportedException>(() => shader.SetSPIRV(AnnotateText(code, "Electron2D:default:2:f3:0:cmdi:AAAAAA==")));
        using var defaults = Load("hlsl"); var boolCode = WithoutDefaults(defaults.GetSPIRV(), "b1");
        Reject<ArgumentException>(() => defaults.SetSPIRV(Annotate(boolCode, "b1", "b1", 0, [2])));
        Console.WriteLine("Twelve malformed/type/shape/value/duplicate/version default records reject atomically.");
    }
}
