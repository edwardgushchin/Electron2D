using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Electron2D;

internal static partial class DefaultChecks
{
    internal static void Run(string[] args)
    {
        if (args.Length != 0 && args[0] == "rectangular-cpu") { RectangularCPU(args[1]); return; }
        if (args.Length != 0 && args[0] == "rectangular-read") { using var rectMaterial = ResourceLoader.Load<ShaderMaterial>(args[1], ResourceLoader.CacheMode.IgnoreDeep); using var rectShader = rectMaterial.Shader!; RectangularValues(rectMaterial, true); return; }
        if (args.Length != 0 && args[0] == "rectangular-gpu") { RectangularRender(args); return; }
        if (args.Length != 0 && args[0] == "gpu") { Render(args); return; }
        if (args.Length != 0 && args[0] == "read") { ReadArchive(args[1]); return; }
        if (args.Length != 0 && args[0] == "read-instance") { using var scene = ResourceLoader.Load<PackedScene>(args[1], ResourceLoader.CacheMode.IgnoreDeep); using var item = (Entity)scene.Instantiate(); InstanceArchive(item); return; }
        foreach (var language in new[] { "hlsl", "glsl" })
        {
            using var compiled = Load(language); using var values = new ShaderMaterial { Shader = compiled };
            Roles(values); ResetChecks(values); Archive(values);
        }
        InvalidMetadata();
        Stages(); Instances(); DomainResources();
        var code = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders/ValuesGLSL.spv"));
        var initial = Annotate(code, "rgb", "f3", 0, [Bits(.25f), Bits(.5f), Bits(.75f)]);
        initial = Annotate(initial, "pair", "u2", 0, [uint.MaxValue, 0x80000000]);
        initial = Annotate(initial, "rgbArray", "f3", 2, [Bits(.1f), Bits(.2f), Bits(.3f), Bits(.4f), Bits(.5f), Bits(.6f)]);
        using var shader = Shader.CreateFromSPIRV(initial); using var material = new ShaderMaterial { Shader = shader };
        var oldDescriptor = (PropertyDescriptor<ShaderMaterial, Vector3>)shader.GetShaderUniformList().Single(p => p.Name == "rgb");
        Check(material.GetShaderParameter<Vector3>("rgb") == new Vector3(.25f, .5f, .75f), "Compiled defaults initialize actual material bytes.");
        Check(material.GetShaderParameter<Vector2i>("pair") == new Vector2i(-1, int.MinValue), "Unsigned defaults preserve every component bit.");
        Check(material.GetShaderParameterArray<Vector3>("rgbArray")[1] == new Vector3(.4f, .5f, .6f), "Distinct array elements retain separate defaults.");
        Check(!shader.GetShaderUniformList().Single(p => p.Name == "rgb").CanRevert(material), "Descriptor reverts use authored defaults.");
        Console.WriteLine("Compiled scalar/vector/array defaults and descriptor values passed.");
        shader.SetSPIRV(Annotate(code, "rgb", "f3", 0, [Bits(.75f), Bits(.5f), Bits(.25f)]));
        Check(oldDescriptor.TryGetRevertValue(material, out var oldDefault) && oldDefault == new Vector3(.25f, .5f, .75f), "Old descriptors retain their immutable authored default.");
        Check(material.GetShaderParameter<Vector3>("rgb") == new Vector3(.75f, .5f, .25f), "Unset material parameters follow new defaults after reload.");
        material.SetShaderParameter("rgb", new Vector3(.2f, .3f, .4f));
        shader.SetSPIRV(initial);
        Check(material.GetShaderParameter<Vector3>("rgb") == new Vector3(.2f, .3f, .4f), "Explicit values survive compatible reload.");
        Console.WriteLine("Authored defaults and explicit reload ownership passed.");
        material.SetShaderParameter("rgb", null);
        shader.SetSPIRV(Annotate(code, "rgb", "f3", 0, [Bits(.75f), Bits(.5f), Bits(.25f)]));
        Check(material.GetShaderParameter<Vector3>("rgb") == new Vector3(.75f, .5f, .25f), "Reset removes explicit ownership before reload.");
    }
    private static Shader Load(string language) => Shader.CreateFromSPIRV(File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "Defaults.frag." + language + ".spv")));
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static byte[] Annotate(byte[] code, string name, string type, int length, uint[] values)
    {
        var data = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(i * 4), values[i]);
        return AnnotateText(code, $"Electron2D:default:1:{type}:{length}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(name))}:{Convert.ToBase64String(data)}");
    }
    internal static byte[] AnnotateText(byte[] code, string text)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code).ToArray(); var at = 5;
        while (at < words.Length && (words[at] & 0xffff) is 10 or 11 or 14 or 15 or 16 or 17 or 331) at += (int)(words[at] >> 16);
        var payload = Encoding.UTF8.GetBytes(text + '\0'); var count = (payload.Length + 3) / 4 + 2;
        using var output = new MemoryStream(); using var writer = new BinaryWriter(output); var id = words[3]++;
        writer.Write(MemoryMarshal.AsBytes(words.AsSpan(0, at))); writer.Write((uint)count << 16 | 7); writer.Write(id); writer.Write(payload);
        while (output.Position % 4 != 0) writer.Write((byte)0);
        writer.Write(MemoryMarshal.AsBytes(words.AsSpan(at))); return output.ToArray();
    }
}
