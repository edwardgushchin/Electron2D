using System.Runtime.InteropServices;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void InitializeMatrixValues(ShaderMaterial material, Transform first, Transform second)
    {
        material.SetShaderParameter("columnMatrix", first); material.SetShaderParameter("rowMatrix", second);
        material.SetShaderParameter<Transform>("columnMatrices", [first, second]);
        material.SetShaderParameter<Transform>("rowMatrices", [second, first]);
        material.SetShaderParameter("tail", .875f);
    }

    private static byte[] WidenMatrixLayout(Shader shader)
    {
        var bytes = shader.GetSPIRV(); var words = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan());
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            if ((words[at] & 0xffff) == 72 && (words[at] >> 16) == 5 && words[at + 3] is 7 or 35) words[at + 4] *= 2;
            if ((words[at] & 0xffff) == 71 && (words[at] >> 16) == 4 && words[at + 2] == 6) words[at + 3] *= 2;
        }
        return bytes;
    }

    private static void VerifyShaderMatrices(string fixture)
    {
        using var shader = LoadShader(fixture); using var reordered = LoadShader("MatricesReordered");
        var original = shader.GetSPIRV(); var wide = WidenMatrixLayout(reordered);
        using var material = new ShaderMaterial { Shader = shader };
        var first = new Transform(new(.125f, .25f), new(.5f, .75f), new(11, 22));
        var second = new Transform(new(.75f, .125f), new(.25f, .5f), new(-33, 44));
        var expectedFirst = first; expectedFirst.Origin = Vector2.Zero;
        var expectedSecond = second; expectedSecond.Origin = Vector2.Zero;
        var descriptors = shader.GetShaderUniformList();
        Check(descriptors.Count == 5 && descriptors.Count(p => p is PropertyDescriptor<ShaderMaterial, Transform>) == 2 && descriptors.Count(p => p is PropertyDescriptor<ShaderMaterial, Transform[]>) == 2, "Matrices expose typed scalar and array descriptors.");
        Check(material.GetShaderParameter<Transform>("columnMatrix") == Transform.Identity && material.GetShaderParameterArray<Transform>("rowMatrices").All(t => t == Transform.Identity), "Unassigned matrix values and arrays initially contain identity.");
        var descriptor = (PropertyDescriptor<ShaderMaterial, Transform>)descriptors.Single(p => p.Name == "columnMatrix");
        Check(descriptor.TryGetRevertValue(material, out var identity) && identity == Transform.Identity, "Matrix revert defaults match getters.");
        var arrayDescriptor = (PropertyDescriptor<ShaderMaterial, Transform[]>)descriptors.Single(p => p.Name == "rowMatrices");
        Check(arrayDescriptor.TryGetRevertValue(material, out var identities) && identities.All(t => t == Transform.Identity), "Every matrix array revert element is identity.");
        InitializeMatrixValues(material, first, second);
        Check(material.GetShaderParameter<Transform>("columnMatrix") == expectedFirst && material.GetShaderParameter<Transform>("rowMatrix") == expectedSecond, "Both storage orders roundtrip the basis and omit translation.");
        Check(material.GetShaderParameter<float>("tail") == .875f, "Matrix writes preserve adjacent fields.");
        var array = material.GetShaderParameterArray<Transform>("columnMatrices"); array[0] = default;
        Check(material.GetShaderParameterArray<Transform>("columnMatrices")[0] == expectedFirst, "Returned matrix arrays are independent.");
        Reject<ArgumentException>(() => material.SetShaderParameter("columnMatrix", Vector4.One));
        Reject<ArgumentException>(() => material.GetShaderParameter<Color>("rowMatrix"));
        Reject<ArgumentException>(() => material.SetShaderParameter<Transform>("rowMatrix", [first, second]));
        Reject<ArgumentException>(() => material.SetShaderParameter<Transform>("rowMatrices", [first]));
        for (var component = 0; component < 6; component++)
        {
            var invalid = first; invalid[component / 2, component % 2] = component % 2 == 0 ? float.NaN : float.PositiveInfinity;
            Reject<ArgumentException>(() => material.SetShaderParameter("columnMatrix", invalid));
            Reject<ArgumentException>(() => material.SetShaderParameter<Transform>("columnMatrices", [second, invalid]));
        }
        Check(material.GetShaderParameter<Transform>("columnMatrix") == expectedFirst && material.GetShaderParameterArray<Transform>("columnMatrices")[0] == expectedFirst, "Complete validation precedes every matrix or array mutation, including unused Origin.");
        using var duplicate = (ShaderMaterial)material.Duplicate(true); using var duplicateShader = duplicate.Shader!;
        using var copy = new ShaderMaterial(); copy.CopyFromResource(material);
        descriptor.Revert(material);
        Check(material.GetShaderParameter<Transform>("columnMatrix") == Transform.Identity && duplicate.GetShaderParameter<Transform>("columnMatrix") == expectedFirst && copy.GetShaderParameterArray<Transform>("rowMatrices")[0] == expectedSecond, "Revert and graph copying preserve independent matrix storage.");
        descriptor.SetValue(material, first);
        foreach (var code in new[] { reordered.GetSPIRV(), wide, original })
        {
            shader.SetSPIRV(code);
            Check(material.GetShaderParameter<Transform>("columnMatrix") == expectedFirst && material.GetShaderParameterArray<Transform>("rowMatrices").SequenceEqual(new[] { expectedSecond, expectedFirst }), "Reload converts storage order and matrix/array stride across buffers.");
            descriptor.SetValue(material, first);
        }
        // Missing order, conflicting orders, malformed stride and misaligned offsets enter runtime checks directly.
        foreach (var fault in new[] { "missing-order", "missing-stride", "both-orders", "array-stride", "short-stride", "unaligned-stride", "large-stride", "offset" })
        {
            var words = MemoryMarshal.Cast<byte, uint>(original).ToArray(); var changed = false;
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            {
                if (fault == "array-stride" && (words[at] & 0xffff) == 71 && words[at + 2] == 6)
                { words[at + 3] = 16; changed = true; break; }
                if ((words[at] & 0xffff) != 72) continue;
                if (fault == "missing-stride" && words[at + 3] == 7)
                { words = words[..at].Concat(words[(at + 5)..]).ToArray(); changed = true; break; }
                if (fault == "missing-order" && words[at + 3] is 4 or 5) { words[at + 3] = 0; changed = true; }
                if (fault == "both-orders" && words[at + 3] is 4 or 5)
                {
                    uint[] extra = [4u << 16 | 72u, words[at + 1], words[at + 2], words[at + 3] == 4 ? 5u : 4u];
                    words = words[..at].Concat(extra).Concat(words[at..]).ToArray(); changed = true; break;
                }
                if (words[at + 3] == 7 && fault is "short-stride" or "unaligned-stride" or "large-stride")
                { words[at + 4] = fault == "short-stride" ? 8u : fault == "unaligned-stride" ? 20u : uint.MaxValue; changed = true; }
                if (fault == "offset" && words[at + 3] == 35 && words[at + 4] == 0) { words[at + 4] = 4; changed = true; }
            }
            Check(changed, "Malformed matrix fixture changes the intended decoration.");
            try { shader.SetSPIRV(MemoryMarshal.AsBytes(words.AsSpan())); throw new InvalidOperationException("Accepted malformed matrix layout: " + fault); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException) { }
            Check(shader.GetSPIRV().SequenceEqual(original) && material.GetShaderParameter<Transform>("columnMatrix") == expectedFirst, "Invalid external matrix replacement preserves code and values.");
        }
        for (var pass = 0; pass < 2; pass++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 2000; i++) { InitializeMatrixValues(material, first, second); _ = material.GetShaderParameter<Transform>("columnMatrix"); _ = material.GetShaderParameter<Transform>("rowMatrix"); }
            if (pass == 1) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warm matrix scalar/array updates and getters allocate zero bytes.");
        }
        Console.WriteLine($"Shader matrix contract passed: {fixture}; 2000 warm iterations, zero bytes.");
    }

    private static void VerifyShaderMatrixFrame(string backend, string fixture)
    {
        using var shader = LoadShader(fixture); using var reordered = LoadShader("MatricesReordered");
        var original = shader.GetSPIRV(); var wide = WidenMatrixLayout(reordered);
        using var material = new ShaderMaterial { Shader = shader };
        var first = new Transform(new(.125f, .25f), new(.5f, .75f), new(11, 22));
        var second = new Transform(new(.75f, .125f), new(.25f, .5f), new(-33, 44));
        InitializeMatrixValues(material, first, second);
        using var copy = (ShaderMaterial)material.Duplicate();
        using var defaults = new ShaderMaterial { Shader = shader }; defaults.SetShaderParameter("tail", .875f);
        var nodes = new[] { new CanvasNode { Name = "first", Material = material }, new CanvasNode { Name = "copy", Material = copy, Position = new(16, 0) }, new CanvasNode { Name = "shared", Material = material, Position = new(32, 0) }, new CanvasNode { Name = "defaults", Material = defaults, Position = new(48, 0) } };
        var window = new Window { Size = new(64, 32) };
        foreach (var node in nodes) { node.DrawAction = n => n.DrawRect(new(0, 0, 12, 12), Colors.White); window.AddChild(node); }
        var frames = 0; var before = 0L; var allocated = 0L;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
            server.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (++frames > 20) allocated += bytes;
                using var image = server.Readback();
                Pixel(image, 4, 4, new(frames == 1 ? .375f : frames == 6 ? 0 : .5f, frames == 6 ? 0 : .625f, .59375f, 1));
                Pixel(image, 20, 4, new(.375f, .625f, .59375f, 1)); Pixel(image, 36, 4, image.GetPixel(4, 4));
                Pixel(image, 52, 4, new(1, .5f, .75f, 1));
                if (frames == 1) { first.X.X = .25f; InitializeMatrixValues(material, first, second); }
                if (frames == 2) shader.SetSPIRV(reordered.GetSPIRV());
                if (frames == 3) shader.SetSPIRV(wide);
                if (frames == 4) shader.SetSPIRV(original);
                if (frames == 5) InitializeMatrixValues(material, default, second);
                if (frames == 6) InitializeMatrixValues(material, first, second);
                if (frames == 40) { Check(allocated == 0 && nodes.All(n => n.Draws == 1), "Matrix values and reload retain geometry without warm render allocations."); window.Tree!.Quit(); }
            };
        };
        if (backend == "compatibility") Reject<NotSupportedException>(() => Engine.Instance.Run(window));
        else Engine.Instance.Run(window);
        Released(window);
        Console.WriteLine(backend == "compatibility" ? $"Shader matrix fallback rejection and cleanup passed: {fixture}." :
            $"Shader matrix pixels/reload/cleanup passed: {backend}/{fixture}; {frames} frames, {allocated} warm bytes.");
    }
}
