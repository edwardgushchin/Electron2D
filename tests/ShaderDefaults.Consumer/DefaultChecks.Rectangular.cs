using System.Runtime.InteropServices;
using Electron2D;

internal static partial class DefaultChecks
{
    private sealed record RectangularField(string Name, int Columns, int Rows, int Count, float[] Initial, float[] Alternate);
    private static readonly RectangularField[] RectangularFields = MakeRectangularFields();
    private static RectangularField[] MakeRectangularFields()
    {
        var result = new List<RectangularField>();
        for (var columns = 2; columns <= 4; columns++)
            for (var rows = 2; rows <= 4; rows++)
                if (columns != rows)
                    foreach (var kind in "cra")
                    {
                        var name = $"{kind}{columns}r{rows}"; var count = kind == 'a' ? 2 : 1; var size = columns * rows;
                        var initial = new float[size * count]; var alternate = new float[initial.Length];
                        for (var element = 0; element < count; element++)
                            for (var index = 0; index < size; index++)
                            {
                                var value = (kind == 'r' || element == 1 ? -1 : 1) * (index + 1f) / (kind == 'c' ? 32 : kind == 'r' ? 16 : 8);
                                initial[element * size + index] = value; alternate[element * size + index] = value + .25f;
                            }
                        if (name == "c3r2") { initial = [1, 0, 0, 1, 4, 0]; alternate = [1, 0, 0, 1, 8, 0]; }
                        result.Add(new(name, columns, rows, count, initial, alternate));
                    }
        return result.ToArray();
    }
    private static byte[] RectangularCode(string folder, string language, string profile = "fragment") => File.ReadAllBytes(System.IO.Path.Combine(folder, profile + "." + language + ".spv"));
    private static void RectangularSet(ShaderMaterial material, bool alternate)
    {
        foreach (var field in RectangularFields) material.SetShaderMatrixParameter(field.Name, alternate ? field.Alternate : field.Initial);
        material.SetShaderParameter("phase", alternate ? 1f : 0f); material.SetShaderParameter("tail", .875f);
    }
    private static void RectangularReset(ShaderMaterial material)
    { foreach (var field in RectangularFields) material.SetShaderParameter(field.Name, (Texture?)null); material.SetShaderParameter("phase", (Texture?)null); }
    private static void RectangularValues(ShaderMaterial material, bool alternate)
    {
        foreach (var field in RectangularFields) Check(material.GetShaderMatrixParameter(field.Name).SequenceEqual(alternate ? field.Alternate : field.Initial), "Rectangular values: " + field.Name);
        Check(material.GetShaderParameter<float>("tail") == .875f && material.GetShaderParameter<float>("phase") == (alternate ? 1 : 0), "Adjacent scalars retain their values.");
    }
    private static float[] RectangularIdentity(RectangularField field)
    {
        var result = new float[field.Initial.Length]; var size = field.Columns * field.Rows;
        for (var element = 0; element < field.Count; element++) for (var axis = 0; axis < Math.Min(field.Columns, field.Rows); axis++) result[element * size + axis * (field.Rows + 1)] = 1;
        return result;
    }
    private static byte[] RectangularWiden(byte[] code)
    {
        var result = (byte[])code.Clone(); var words = MemoryMarshal.Cast<byte, uint>(result.AsSpan());
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            if ((words[at] & 65535) == 72 && (words[at] >> 16) == 5 && words[at + 3] is 7 or 35) words[at + 4] *= 2;
            if ((words[at] & 65535) == 71 && (words[at] >> 16) == 4 && words[at + 2] == 6) words[at + 3] *= 2;
        }
        return result;
    }
    private static void RectangularCPU(string folder)
    {
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            var code = RectangularCode(folder, language);
            using var shader = Shader.CreateFromSPIRV(code); using var material = new ShaderMaterial { Shader = shader };
            RectangularValues(material, false);
            foreach (var field in RectangularFields)
            {
                var descriptor = (PropertyDescriptor<ShaderMaterial, float[]>)shader.GetShaderUniformList().Single(p => p.Name == field.Name);
                Check(descriptor.TryGetRevertValue(material, out var value) && value.SequenceEqual(field.Initial), "Authored rectangular default shape/order.");
                value[0] = 500; Check(descriptor.TryGetRevertValue(material, out value) && value[0] == field.Initial[0], "Independent descriptor defaults.");
                Reject<ArgumentException>(() => material.SetShaderMatrixParameter(field.Name, field.Initial.AsSpan(1)));
                Reject<ArgumentException>(() => material.GetShaderParameterArray<float>(field.Name));
                var destination = Enumerable.Repeat(-123f, field.Initial.Length + 1).ToArray(); Reject<ArgumentException>(() => material.CopyShaderMatrixParameter(field.Name, destination));
                Check(destination.All(v => v == -123), "Bad destination is unchanged.");
                for (var index = 0; index < field.Initial.Length; index++)
                {
                    var invalid = (float[])field.Initial.Clone(); invalid[index] = index % 2 == 0 ? float.NaN : float.PositiveInfinity;
                    Reject<ArgumentException>(() => material.SetShaderMatrixParameter(field.Name, invalid));
                    Check(material.GetShaderMatrixParameter(field.Name).SequenceEqual(field.Initial), "Every invalid component rejects atomically.");
                }
            }
            using (var implicitShader = Shader.CreateFromSPIRV(WithoutDefaults(code)))
            using (var implicitMaterial = new ShaderMaterial { Shader = implicitShader })
                foreach (var field in RectangularFields) Check(implicitMaterial.GetShaderMatrixParameter(field.Name).SequenceEqual(RectangularIdentity(field)), "Rectangular identity follows the shorter diagonal.");
            var invalidMetadata = Annotate(WithoutDefaults(code, "c2r3"), "c2r3", "m3x2", 0, RectangularFields[0].Initial.Select(Bits).ToArray());
            Reject<ArgumentException>(() => Shader.CreateFromSPIRV(invalidMetadata));
            RectangularSet(material, true);
            using (var deep = (ShaderMaterial)material.DuplicateDeep(DeepDuplicateMode.All))
            using (var deepShader = deep.Shader!) { RectangularValues(deep, true); RectangularReset(deep); RectangularValues(material, true); }
            using (var shallow = (ShaderMaterial)material.Duplicate()) { RectangularValues(shallow, true); RectangularReset(shallow); }
            foreach (var replacement in new[] { RectangularCode(folder, language, "reordered"), RectangularWiden(RectangularCode(folder, language, "reordered")), code })
            { shader.SetSPIRV(replacement); RectangularValues(material, true); }
            shader.SetSPIRV(RectangularCode(folder, language, "changed"));
            Check(material.GetShaderMatrixParameter("c2r3").SequenceEqual(RectangularFields[0].Initial), "Equal-sized transposed shapes reset incompatible overrides.");
            Check(material.GetShaderMatrixParameter("r2r3").SequenceEqual(RectangularFields[1].Alternate), "Compatible siblings survive a shape change.");
            shader.SetSPIRV(code); RectangularSet(material, true);
            shader.SetSPIRV(RectangularCode(folder, language, "single-array"));
            Check(material.GetShaderMatrixParameter("a2r3").SequenceEqual(RectangularFields[2].Initial.AsSpan(0, 6).ToArray()), "Length-one rectangular arrays reset incompatible whole-array overrides.");
            Check(material.GetShaderMatrixParameter("c2r3").SequenceEqual(RectangularFields[0].Alternate), "A scalar stays distinct from a length-one array of the same component count.");
            Reject<ArgumentException>(() => material.SetShaderMatrixParameter("a2r3", RectangularFields[2].Alternate));
            shader.SetSPIRV(code); RectangularSet(material, true);
            Reject<ArgumentException>(() => Shader.CreateFromSPIRV(RectangularCode(folder, language, "changed-vertex"), code));
            RectangularArchive(material);
            var result = new float[RectangularFields[^1].Initial.Length];
            for (var index = 0; index < 200; index++) { RectangularSet(material, true); material.CopyShaderMatrixParameter(RectangularFields[^1].Name, result); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 2000; index++) { RectangularSet(material, true); material.CopyShaderMatrixParameter(RectangularFields[^1].Name, result); }
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared rectangular matrix updates/copies allocate zero managed bytes.");
            RectangularReset(material); RectangularValues(material, false);
            Console.WriteLine("Rectangular CPU: all six shapes, both storage orders, arrays, defaults, shape-safe reload, guards, fresh archives and 2000 zero-byte updates passed: " + language);
        }
    }
    private static void RectangularArchive(ShaderMaterial material)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-rectangular-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(material, path, SaverFlags.Compress | SaverFlags.SaveBigEndian | SaverFlags.BundleResources);
            using var loaded = ResourceLoader.Load<ShaderMaterial>(path, ResourceLoader.CacheMode.IgnoreDeep); using var shader = loaded.Shader!; RectangularValues(loaded, true);
            Fresh("rectangular-read", path);
        }
        finally { File.Delete(path); }
    }
}
