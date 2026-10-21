using Electron2D;
using Path = System.IO.Path;

internal static partial class DefaultChecks
{
    private static byte[] Stage(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", name + ".spv"));
    private static Shader Paired(string language) => Shader.CreateFromSPIRV(Stage("Defaults.vert." + language), Stage("Defaults.frag." + language));
    private static void Stages()
    {
        foreach (var language in new[] { "hlsl", "glsl" })
        {
            var vertex = Stage("Defaults.vert." + language); var fragment = Stage("Defaults.frag." + language);
            using var shader = Shader.CreateFromSPIRV(vertex, fragment); using var material = new ShaderMaterial { Shader = shader }; Roles(material);
            Check(material.GetShaderParameter<float>("vertexOnly") == .5f, "Vertex-only defaults initialize their retained buffer.");
            shader.SetSPIRV(vertex, WithoutDefaults(WithoutDefaults(fragment, "f1"), "m3")); Roles(material);
            shader.SetSPIRV(WithoutDefaults(WithoutDefaults(vertex, "f1"), "m3"), fragment); Roles(material);
            var before = shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex);
            Reject<ArgumentException>(() => shader.SetSPIRV(Annotate(WithoutDefaults(vertex, "f1"), "f1", "f1", 0, [Bits(.75f)]), fragment));
            Check(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(before), "Conflicting stage defaults preserve the old pair.");
            Reject<ArgumentException>(() => shader.SetSPIRV(Annotate(WithoutDefaults(vertex, "m3"), "m3", "m3", 0, new uint[9]), fragment));
            Reject<NotSupportedException>(() => shader.SetSPIRV(Annotate(vertex, "size", "f2", 0, [Bits(100), Bits(100)]), fragment));
            shader.SetSPIRV(Annotate(WithoutDefaults(vertex, "f1"), "f1", "f1", 0, [Bits(-0f)]), Annotate(WithoutDefaults(fragment, "f1"), "f1", "f1", 0, [Bits(0f)]));
            Check(material.GetShaderParameter<float>("f1") == 0, "Stage defaults treat signed floating zero as equal.");
        }
        Console.WriteLine("Paired-stage defaults adopt either stage, translate matrix storage, reject conflicts and protect Frame.");
    }
    private static void InstanceRoles(CanvasItem item, bool query = false)
    {
        void Value<T>(string name, T expected) where T : unmanaged
        {
            Check(EqualityComparer<T?>.Default.Equals(item.GetInstanceShaderParameter<T>(name), expected), "Typed instance default: " + name);
            if (query) Check(EqualityComparer<T>.Default.Equals(RenderingServer.ShaderGetParameterDefault<T>(((ShaderMaterial)item.Material!).Shader!.GetRID(), name), expected), "Public instance default query: " + name);
        }
        Value("f1", .125f); Value("f2", new Vector2(.125f, .25f)); Value("f3", new Vector3(.125f, .25f, .375f)); Value("f4", new Vector4(.125f, .25f, .375f, .5f));
        Value("i1", -7); Value("i2", new Vector2i(-7, 2)); Value("i3", new Vector3i(-7, 2, -3)); Value("i4", new Vector4i(-7, 2, -3, 4));
        Value("u1", uint.MaxValue); Value("u2", new Vector2i(-1, int.MinValue)); Value("u3", new Vector3i(-1, int.MinValue, 7)); Value("u4", new Vector4i(-1, int.MinValue, 7, 1));
        Value("b1", true); Value("b2", 1); Value("b3", 5); Value("b4", 5);
    }
    private static void Instances()
    {
        foreach (var language in new[] { "hlsl", "glsl" })
        {
            using var shader = Shader.CreateFromSPIRV(Stage("Instance.frag." + language)); using var material = new ShaderMaterial { Shader = shader };
            using var item = new Entity { Material = material }; InstanceRoles(item);
            item.SetInstanceShaderParameter<float>("f1", .125f);
            var original = shader.GetSPIRV(); shader.SetSPIRV(Annotate(WithoutDefaults(original, "f1"), "f1", "f1", 0, [Bits(.75f)]));
            Check(item.GetInstanceShaderParameter<float>("f1") == .125f, "Explicit instance defaults survive reload.");
            item.SetInstanceShaderParameter<float>("f1", null);
            Check(item.GetInstanceShaderParameter<float>("f1") == .75f, "Cleared instance values follow new shader defaults.");
            shader.SetSPIRV(original); InstanceRoles(item);
            item.SetInstanceShaderParameter<bool>("b1", false); using var packed = new PackedScene(); packed.Pack(item);
            using var clone = (Entity)packed.Instantiate(); Check(clone.GetInstanceShaderParameter<bool>("b1") == false && clone.GetInstanceShaderParameter<Vector2>("f2") == new Vector2(.125f, .25f), "PackedScene distinguishes stored instance overrides from embedded defaults.");
            var path = Path.Combine(Path.GetTempPath(), "electron2d-instance-default-" + Guid.NewGuid().ToString("N") + ".e2dscene");
            try
            {
                foreach (var flags in new[] { SaverFlags.None, SaverFlags.Compress | SaverFlags.SaveBigEndian })
                { ResourceSaver.Save(packed, path, flags); Fresh("read-instance", path); }
            }
            finally { File.Delete(path); }
        }
        Console.WriteLine("All sixteen instance roles, explicit/unset reload and PackedScene defaults passed.");
    }
    private static void InstanceArchive(Entity item)
    {
        Check(item.GetInstanceShaderParameter<bool>("b1") == false && item.GetInstanceShaderParameter<Vector2>("f2") == new Vector2(.125f, .25f), "Fresh scene retains defaults and explicit override.");
        var shader = ((ShaderMaterial)item.Material!).Shader!; var original = shader.GetSPIRV();
        try
        {
            shader.SetSPIRV(Annotate(WithoutDefaults(original, "f1"), "f1", "f1", 0, [Bits(.75f)]));
            Check(item.GetInstanceShaderParameter<float>("f1") == .75f && item.GetInstanceShaderParameter<bool>("b1") == false, "Fresh scene distinguishes unstored defaults after reload.");
        }
        finally { shader.SetSPIRV(original); }
    }
}
