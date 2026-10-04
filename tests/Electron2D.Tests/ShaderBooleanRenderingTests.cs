using System.Runtime.InteropServices;
using System.Text;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void SetBooleanValues(ShaderMaterial material, bool enabled)
    {
        material.SetShaderParameter("enabled", enabled);
        material.SetShaderParameter("number", 153u); material.SetShaderParameter("afterTriple", 204u);
        if (enabled) material.SetShaderParameter<bool>("switches", [true, false, true]);
        else material.SetShaderParameter<bool>("switches", [false, true, false]);
        material.SetShaderParameter("pair", -2); material.SetShaderParameter("triple", -3); material.SetShaderParameter("quad", -6);
        material.SetShaderParameter<int>("pairs", stackalloc int[] { 1, 2 }); material.SetShaderParameter<int>("triples", stackalloc int[] { 3, 4 }); material.SetShaderParameter<int>("quads", stackalloc int[] { 5, 8 });
        material.SetShaderParameter("tail", .625f);
    }

    private static byte[] ChangeBooleanMetadata(byte[] code, string name, Func<string, string?> change, bool duplicate = false)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code).ToArray();
        var result = words[..5].ToList(); var found = false;
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); var instruction = words.AsSpan(at, count);
            if ((words[at] & 0xffff) == 7)
            {
                var text = Encoding.UTF8.GetString(MemoryMarshal.AsBytes(instruction[2..])).TrimEnd('\0');
                if (text.StartsWith("Electron2D:", StringComparison.Ordinal) && text.EndsWith(":" + Convert.ToBase64String(Encoding.UTF8.GetBytes(name)), StringComparison.Ordinal))
                {
                    found = true;
                    if (duplicate) { result.AddRange(instruction.ToArray()); result[3]++; }
                    var replacement = change(text);
                    if (replacement is not null)
                    {
                        var bytes = Encoding.UTF8.GetBytes(replacement + '\0'); var padded = new byte[(bytes.Length + 3) / 4 * 4]; bytes.CopyTo(padded, 0);
                        result.Add((uint)(padded.Length / 4 + 2) << 16 | 7u); result.Add(duplicate ? result[3] - 1 : instruction[1]);
                        result.AddRange(MemoryMarshal.Cast<byte, uint>(padded).ToArray());
                    }
                    at += count; continue;
                }
            }
            result.AddRange(instruction.ToArray()); at += count;
        }
        Check(found, "The boolean metadata fixture contains the intended parameter.");
        return MemoryMarshal.AsBytes(result.ToArray().AsSpan()).ToArray();
    }

    private static void VerifyShaderBooleans(string fixture)
    {
        using var shader = LoadShader(fixture); using var reordered = LoadShader("BooleansReordered");
        var original = shader.GetSPIRV();
        using var material = new ShaderMaterial { Shader = shader };
        var descriptors = shader.GetShaderUniformList();
        Check(descriptors.Count == 11 && descriptors.Count(p => p is PropertyDescriptor<ShaderMaterial, bool>) == 1 &&
            descriptors.Count(p => p is PropertyDescriptor<ShaderMaterial, bool[]>) == 1 && descriptors.Count(p => p is PropertyDescriptor<ShaderMaterial, int[]>) == 3 &&
            descriptors.Count(p => p is PropertyDescriptor<ShaderMaterial, uint>) == 2, "Boolean type metadata distinguishes scalar/array/vector values from ordinary unsigned integers.");
        var enabled = (PropertyDescriptor<ShaderMaterial, bool>)descriptors.Single(p => p.Name == "enabled");
        var switches = (PropertyDescriptor<ShaderMaterial, bool[]>)descriptors.Single(p => p.Name == "switches");
        Check(!material.GetShaderParameter<bool>("enabled") && material.GetShaderParameter<int>("quad") == 0 &&
            material.GetShaderParameterArray<bool>("switches").All(v => !v) && enabled.TryGetRevertValue(material, out var initial) && !initial,
            "Boolean defaults and revert values are false/zero.");
        SetBooleanValues(material, true);
        Check(material.GetShaderParameter<bool>("enabled") && material.GetShaderParameter<int>("pair") == 2 && material.GetShaderParameter<int>("triple") == 5 &&
            material.GetShaderParameter<int>("quad") == 10 && material.GetShaderParameter<uint>("number") == 153 && material.GetShaderParameter<uint>("afterTriple") == 204 &&
            material.GetShaderParameter<float>("tail") == .625f, "Boolean vectors use the low component bits and preserve adjacent fields.");
        Check(material.GetShaderParameterArray<int>("triples").SequenceEqual(new[] { 3, 4 }), "Three-component boolean vectors require no spatial vector type.");
        var copied = material.GetShaderParameterArray<bool>("switches"); copied[0] = false;
        Check(material.GetShaderParameterArray<bool>("switches")[0], "Boolean array reads return independent copies.");
        Reject<ArgumentException>(() => material.SetShaderParameter("enabled", 1u));
        Reject<ArgumentException>(() => material.SetShaderParameter("number", true));
        Reject<ArgumentException>(() => material.SetShaderParameter("pair", new Vector2i(1, 1)));
        Reject<ArgumentException>(() => material.SetShaderParameter<bool>("switches", [true, false]));
        Reject<ArgumentException>(() => material.SetShaderParameter<int>("pairs", [1, 2, 3]));
        Check(material.GetShaderParameterArray<bool>("switches").SequenceEqual(new[] { true, false, true }), "Wrong types and array lengths preserve the previous values.");
        using var duplicate = (ShaderMaterial)material.Duplicate(true); using var duplicateShader = duplicate.Shader!;
        using var copy = new ShaderMaterial(); copy.CopyFromResource(material);
        enabled.Revert(material); switches.Revert(material);
        Check(!material.GetShaderParameter<bool>("enabled") && material.GetShaderParameterArray<bool>("switches").All(v => !v) &&
            duplicate.GetShaderParameter<bool>("enabled") && copy.GetShaderParameterArray<bool>("switches")[0], "Reverts and resource graph copies retain independent boolean state.");
        SetBooleanValues(material, true);
        foreach (var code in new[] { reordered.GetSPIRV(), original })
        {
            shader.SetSPIRV(code);
            Check(material.GetShaderParameter<bool>("enabled") && material.GetShaderParameterArray<int>("quads")[1] == 8 && material.GetShaderParameterArray<bool>("switches")[2],
                "Boolean values migrate across reordered offsets and buffer bindings.");
            enabled.SetValue(material, true);
        }
        shader.SetSPIRV(ChangeBooleanMetadata(original, "enabled", _ => null));
        Check(material.GetShaderParameter<uint>("enabled") == 0, "Removing logical bool resets its value to physical uint instead of migrating incompatible types.");
        Reject<ArgumentException>(() => enabled.SetValue(material, true));
        material.SetShaderParameter("enabled", uint.MaxValue);
        shader.SetSPIRV(original);
        Check(!material.GetShaderParameter<bool>("enabled"), "Restoring bool does not retain the previous unsigned value.");
        SetBooleanValues(material, true);
        foreach (var fault in new[] { "version", "width", "zero-width", "length", "large-length", "name", "blank", "physical", "encoding", "utf8", "duplicate" })
        {
            var invalid = ChangeBooleanMetadata(original, "enabled", text =>
            {
                var fields = text.Split(':');
                switch (fault)
                {
                    case "version": fields[2] = "2"; break;
                    case "width": fields[3] = "2"; break;
                    case "zero-width": fields[3] = "0"; break;
                    case "length": fields[4] = "1"; break;
                    case "large-length": fields[4] = "1025"; break;
                    case "name": fields[5] = Convert.ToBase64String("absent"u8); break;
                    case "blank": fields[5] = ""; break;
                    case "physical": fields[5] = Convert.ToBase64String("tail"u8); break;
                    case "encoding": fields[5] = "?"; break;
                    case "utf8": fields[5] = "/w=="; break;
                }
                return string.Join(':', fields);
            }, duplicate: fault == "duplicate");
            try { shader.SetSPIRV(invalid); throw new InvalidOperationException("Accepted invalid boolean metadata: " + fault); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException) { }
            Check(shader.GetSPIRV().SequenceEqual(original) && material.GetShaderParameter<bool>("enabled"), "Bad external type metadata preserves code and material values.");
        }
        for (var pass = 0; pass < 2; pass++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 2000; i++) { SetBooleanValues(material, i % 2 == 0); _ = material.GetShaderParameter<bool>("enabled"); _ = material.GetShaderParameter<int>("triple"); }
            if (pass == 1) Check(GC.GetAllocatedBytesForCurrentThread() == before, $"Warm boolean access allocated {GC.GetAllocatedBytesForCurrentThread() - before} bytes.");
        }
        Console.WriteLine($"Boolean descriptors, values, copy, migration and malformed metadata passed: {fixture}; 2000 warm iterations, zero bytes.");
    }

    private static void VerifyShaderBooleanFrame(string backend, string fixture)
    {
        using var shader = LoadShader(fixture); using var reordered = LoadShader("BooleansReordered");
        var original = shader.GetSPIRV();
        using var material = new ShaderMaterial { Shader = shader }; SetBooleanValues(material, true);
        using var copy = (ShaderMaterial)material.Duplicate(); using var defaults = new ShaderMaterial { Shader = shader };
        var nodes = new[] { new CanvasNode { Name = "first", Material = material }, new CanvasNode { Name = "copy", Material = copy, Position = new(0, 16) },
            new CanvasNode { Name = "shared", Material = material, Position = new(0, 32) }, new CanvasNode { Name = "defaults", Material = defaults, Position = new(0, 48) } };
        var window = new Window { Size = new(96, 64) };
        foreach (var node in nodes) { node.DrawAction = n => n.DrawRect(new(0, 0, 80, 12), Colors.White); window.AddChild(node); }
        var frames = 0; var before = 0L; var allocated = 0L;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!;
            RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (++frames > 20) allocated += bytes;
                using var image = server.Readback();
                for (var row = 0; row < 4; row++)
                {
                    var empty = row == 3; var on = !empty && (row == 1 || frames == 1); var y = row * 16 + 4;
                    Pixel(image, 4, y, new(on ? .75f : .125f, empty ? 0 : .6f, empty ? 0 : .625f, 1));
                    Pixel(image, 20, y, empty ? Colors.Black : new(2 / 3f, 5 / 7f, 10 / 15f, 1));
                    Pixel(image, 36, y, empty ? Colors.Black : new((on ? 5 : 2) / 7f, 1 / 3f, 2 / 3f, 1));
                    Pixel(image, 52, y, empty ? Colors.Black : new(3 / 7f, 5 / 15f, .8f, 1));
                    Pixel(image, 68, y, empty ? Colors.Black : new(4 / 7f, 8 / 15f, 0, 1));
                }
                if (frames == 1) SetBooleanValues(material, false);
                if (frames == 2) shader.SetSPIRV(reordered.GetSPIRV());
                if (frames == 3) shader.SetSPIRV(original);
                if (frames == 40) window.Tree!.Quit();
            };
        };
        if (backend == "compatibility") Reject<NotSupportedException>(() => Engine.Run(window));
        else
        {
            Engine.Run(window);
            Check(frames == 40 && allocated == 0 && nodes.All(n => n.Draws == 1), "Boolean updates/reload reuse geometry with zero warm rendering allocation.");
        }
        Released(window);
        Console.WriteLine($"Boolean material rendering passed: {backend}/{fixture}; {frames} frames, {allocated} warm bytes.");
    }
}
