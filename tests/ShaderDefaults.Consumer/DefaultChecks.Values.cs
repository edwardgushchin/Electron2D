using Electron2D;

internal static partial class DefaultChecks
{
    private static void Roles(ShaderMaterial material, bool query = false, bool author = false)
    {
        var shader = material.Shader!;
        void Value<T>(string name, T first, T second) where T : unmanaged
        {
            if (author) { material.SetShaderParameter(name, first); material.SetShaderParameter<T>(name + "a", new T[] { first, second }.AsSpan()); }
            Check(EqualityComparer<T>.Default.Equals(material.GetShaderParameter<T>(name), first), "Imported scalar/vector/mat2 default: " + name);
            var array = material.GetShaderParameterArray<T>(name + "a");
            Check(array.Length == 2 && EqualityComparer<T>.Default.Equals(array[0], first) && EqualityComparer<T>.Default.Equals(array[1], second), "Imported fixed-array default: " + name);
            array[0] = default;
            Check(EqualityComparer<T>.Default.Equals(material.GetShaderParameterArray<T>(name + "a")[0], first), "Default arrays are independently copied.");
            foreach (var field in new[] { name, name + "a" }) Check(!shader.GetShaderUniformList().Single(p => p.Name == field).CanRevert(material), "Descriptors use each compiled default: " + field);
            if (!query) return;
            var rid = shader.GetRID();
            Check(EqualityComparer<T>.Default.Equals(RenderingServer.ShaderGetParameterDefault<T>(rid, name), first), "Public shader default query: " + name);
            Span<T> target = stackalloc T[2]; RenderingServer.ShaderGetParameterDefault(rid, name + "a", target);
            Check(EqualityComparer<T>.Default.Equals(target[0], first) && EqualityComparer<T>.Default.Equals(target[1], second), "Public default span query: " + name);
            var copied = RenderingServer.ShaderGetParameterDefaultArray<T>(rid, name + "a");
            Check(copied.AsSpan().SequenceEqual(target), "Public default array query: " + name);
        }
        Value("f1", .125f, .75f); Value("f2", new Vector2(.125f, .25f), new Vector2(.75f, .625f));
        Value("f3", new Vector3(.125f, .25f, .375f), new Vector3(.75f, .625f, .5f));
        Value("f4", new Vector4(.125f, .25f, .375f, .5f), new Vector4(.75f, .625f, .5f, .375f));
        Value("i1", -7, 8); Value("i2", new Vector2i(-7, 2), new Vector2i(8, -4));
        Value("i3", new Vector3i(-7, 2, -3), new Vector3i(8, -4, 6)); Value("i4", new Vector4i(-7, 2, -3, 4), new Vector4i(8, -4, 6, -2));
        Value("u1", uint.MaxValue, 1u); Value("u2", new Vector2i(-1, int.MinValue), new Vector2i(1, 2));
        Value("u3", new Vector3i(-1, int.MinValue, 7), new Vector3i(1, 2, 3)); Value("u4", new Vector4i(-1, int.MinValue, 7, 1), new Vector4i(1, 2, 3, 4));
        Value("b1", true, false); Value("b2", 1, 2); Value("b3", 5, 2); Value("b4", 5, 10);
        Value("m2", new Transform(new(1, 2), new(3, 4), Vector2.Zero), new Transform(new(5, 6), new(7, 8), Vector2.Zero));
        Check(material.GetShaderParameter<Color>("f3") == new Color(.125f, .25f, .375f, 1), "RGB defaults supply opaque alpha for Color aliases.");
        Check(material.GetShaderParameter<Rect2>("f4") == new Rect2(.125f, .25f, .375f, .5f), "Rectangle aliases preserve four default components.");
        Span<float> copy = stackalloc float[32];
        for (var width = 3; width <= 4; width++)
        {
            var count = width * width; var name = "m" + width;
            foreach (var array in new[] { false, true })
            {
                var field = name + (array ? "a" : ""); var expected = Enumerable.Range(1, count * (array ? 2 : 1)).Select(i => (float)i).ToArray();
                if (author) material.SetShaderMatrixParameter(field, expected);
                Check(material.GetShaderMatrixParameter(field).SequenceEqual(expected), "Canonical matrix default: " + field);
                Check(!shader.GetShaderUniformList().Single(p => p.Name == field).CanRevert(material), "Matrix descriptor default: " + field);
                if (!query) continue;
                Check(RenderingServer.ShaderGetMatrixParameterDefault(shader.GetRID(), field).SequenceEqual(expected), "Public wide matrix default query: " + field);
                var target = copy[..expected.Length];
                RenderingServer.ShaderGetMatrixParameterDefault(shader.GetRID(), field, target);
                Check(target.SequenceEqual(expected), "Public wide matrix span query: " + field);
            }
        }
        Console.WriteLine("All nineteen typed defaults and thirty-eight scalar/array declarations passed.");
    }
    private static void ResetChecks(ShaderMaterial material)
    {
        var changes = 0; material.Changed += _ => changes++;
        material.SetShaderParameter("f1", 0f); material.SetShaderParameter("f1", null);
        material.SetShaderParameter<float>("f1a", new float[] { 0, 0 }.AsSpan()); material.SetShaderLayeredParameter("f1a", null);
        material.SetShaderMatrixParameter("m3", new float[9]); material.SetShaderParameter("m3", null);
        Check(changes == 6, "Writes and successful resets each emit Changed.");
        Reject<ArgumentException>(() => material.SetShaderParameter("missing", null));
        Reject<ArgumentException>(() => material.SetShaderParameter("F1", null));
        Reject<ArgumentException>(() => material.SetShaderParameter("f1", float.NaN));
        Reject<ArgumentException>(() => material.SetShaderParameter<float>("f1a", new float[] { .5f, float.NaN }.AsSpan()));
        Check(changes == 6, "Rejected resets and writes preserve state and events."); Roles(material);
    }
}
