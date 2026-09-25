using System.Runtime.InteropServices;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyShaderTriples()
    {
        using var shader = LoadShader("Triples");
        using var material = new ShaderMaterial { Shader = shader };
        var descriptors = shader.GetShaderUniformList();
        Check(descriptors.Count == 6 && descriptors.Single(p => p.Name == "numeric") is PropertyDescriptor<ShaderMaterial, Vector3> &&
            descriptors.Single(p => p.Name == "signedTriple") is PropertyDescriptor<ShaderMaterial, Vector3i> &&
            descriptors.Single(p => p.Name == "unsignedArray") is PropertyDescriptor<ShaderMaterial, Vector3i[]>,
            "Float3 and signed/unsigned int3 values use numeric descriptors.");
        Check(material.GetShaderParameter<Vector3>("numeric") == Vector3.Zero &&
            material.GetShaderParameter<Color>("numeric") == Colors.Black,
            "Numeric float3 defaults to zero and Color reconstructs RGB alpha.");
        material.SetShaderParameter("numeric", new Vector3(.25f, .5f, .75f));
        material.SetShaderParameter("signedTriple", new Vector3i(-1, 2, -3));
        material.SetShaderParameter("unsignedTriple", new Vector3i(-1, 2, 3));
        material.SetShaderParameter<Vector3>("numericArray", [Vector3.Right, Vector3.Up]);
        material.SetShaderParameter<Vector3i>("signedArray", [new(-1, 2, -3), new(4, 5, 6)]);
        material.SetShaderParameter<Vector3i>("unsignedArray", [new(-1, 2, 3), new(4, 5, 6)]);
        Check(material.GetShaderParameter<Color>("numeric") == new Color(.25f, .5f, .75f, 1f) &&
            material.GetShaderParameter<Vector3i>("unsignedTriple") == new Vector3i(-1, 2, 3) &&
            material.GetShaderParameterArray<Vector3i>("signedArray")[1] == new Vector3i(4, 5, 6),
            "Numeric triples roundtrip with signedness and RGB alias semantics.");
        Reject<ArgumentException>(() => material.SetShaderParameter("numeric", new Vector3(0, 0, float.NaN)));
        Reject<ArgumentException>(() => material.SetShaderParameter("signedTriple", new Vector4i(1, 2, 3, 4)));
        Reject<ArgumentException>(() => material.SetShaderParameter<Vector3>("numericArray", [Vector3.Zero]));
        Check(material.GetShaderParameter<Vector3>("numeric") == new Vector3(.25f, .5f, .75f) &&
            material.GetShaderParameterArray<Vector3>("numericArray")[0] == Vector3.Right,
            "Invalid triple updates leave prior values intact.");
        Console.WriteLine("Shader float3/int3/uint3 material contract passed.");
    }

    private static void VerifyShaderTripleFrame()
    {
        using var shader = LoadShader("Triples");
        using var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("numeric", new Vector3(.25f, .5f, .75f));
        material.SetShaderParameter("signedTriple", new Vector3i(-1, 2, -3));
        material.SetShaderParameter("unsignedTriple", new Vector3i(-1, 2, 3));
        material.SetShaderParameter<Vector3>("numericArray", [Vector3.Right, Vector3.Up]);
        material.SetShaderParameter<Vector3i>("signedArray", [new(-1, 2, -3), new(4, 5, 6)]);
        material.SetShaderParameter<Vector3i>("unsignedArray", [new(-1, 2, 3), new(4, 5, 6)]);

        var window = new Window { Size = new(16, 16) };
        var node = new CanvasNode { Material = material, DrawAction = n => n.DrawRect(new(0, 0, 12, 12), Colors.White) };
        window.AddChild(node);
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!;
            server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var image = server.Readback();
                Pixel(image, 4, 4, new(.25f, .5f, .75f, 1));
                window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window);
        Released(window);
        Console.WriteLine("Shader float3/int3/uint3 GPU pixel and cleanup passed.");
    }

    private static void InitializeVectorValues(ShaderMaterial material, Color rgb)
    {
        material.SetShaderParameter("rgb", rgb);
        material.SetShaderParameter("tail", .75f);
        material.SetShaderParameter<Color>("rgbArray", [Colors.White, Colors.Black]);
        material.SetShaderParameter("rectangle", new Rect2(.125f, .25f, .5f, 1));
        material.SetShaderParameter<Rect2>("rectangles", [new(1, 2, 3, 4), new(-1, -2, -3, -4)]);
        material.SetShaderParameter("pair", new Vector2i(int.MinValue, -1));
        material.SetShaderParameter("quad", new Vector4i(-1, int.MinValue, int.MaxValue, 123456789));
        material.SetShaderParameter<Vector2i>("pairs", [new(int.MinValue, -1), new(0, 1)]);
        material.SetShaderParameter<Vector4i>("quads", [new(-1, int.MinValue, int.MaxValue, 123456789), new(1, 2, 3, 4)]);
    }

    private static void VerifyShaderVectorValues(string fixture)
    {
        using var shader = LoadShader(fixture);
        using var material = new ShaderMaterial { Shader = shader };
        var descriptors = shader.GetShaderUniformList();
        Check(descriptors.Count == 9, "Both source languages expose all vector value descriptors.");
        Check(material.GetShaderParameter<Vector3>("rgb") == Vector3.Zero && material.GetShaderParameterArray<Vector3>("rgbArray").All(c => c == Vector3.Zero) && material.GetShaderParameter<Color>("rgb") == Colors.Black, "Float3 defaults are numeric zero, with RGB alpha reconstructed as one.");
        var rgb = (PropertyDescriptor<ShaderMaterial, Vector3>)descriptors.Single(p => p.Name == "rgb");
        var colors = (PropertyDescriptor<ShaderMaterial, Vector3[]>)descriptors.Single(p => p.Name == "rgbArray");
        Check(rgb.TryGetRevertValue(material, out var black) && black == Vector3.Zero && colors.TryGetRevertValue(material, out var blacks) && blacks.All(c => c == Vector3.Zero), "Float3 scalar/array revert values match typed readers.");
        Check(descriptors.Single(p => p.Name == "rectangle") is PropertyDescriptor<ShaderMaterial, Vector4> && descriptors.Single(p => p.Name == "pairs") is PropertyDescriptor<ShaderMaterial, Vector2i[]>, "Aliases preserve canonical vector descriptors.");
        InitializeVectorValues(material, new(.25f, .5f, .75f, .125f));
        Check(material.GetShaderParameter<Vector3>("rgb") == new Vector3(.25f, .5f, .75f), "Numeric float3 reads preserve all components.");
        Check(material.GetShaderParameter<Color>("rgb") == new Color(.25f, .5f, .75f, 1) && material.GetShaderParameter<float>("tail") == .75f, "RGB writes do not overwrite the adjacent scalar with alpha.");
        Check(material.GetShaderParameter<Vector4>("rectangle") == new Vector4(.125f, .25f, .5f, 1) && material.GetShaderParameter<Color>("rectangle") == new Color(.125f, .25f, .5f, 1), "Rect2, Vector4 and Color share the float4 component order.");
        Check(material.GetShaderParameterArray<Rect2>("rectangles")[1] == new Rect2(-1, -2, -3, -4), "Rect2 uniform values retain signed sizes.");
        Check(material.GetShaderParameter<Vector2i>("pair") == new Vector2i(int.MinValue, -1) && material.GetShaderParameterArray<Vector4i>("quads")[0] == new Vector4i(-1, int.MinValue, int.MaxValue, 123456789), "Unsigned vectors roundtrip all component bits.");
        var returned = material.GetShaderParameterArray<Color>("rgbArray"); returned[0] = Colors.Red;
        Check(material.GetShaderParameterArray<Color>("rgbArray")[0] == Colors.White, "RGB array getters return independent copies.");
        Reject<ArgumentException>(() => material.SetShaderParameter("rgb", new Vector4(1, 2, 3, 4)));
        Reject<ArgumentException>(() => material.GetShaderParameter<Rect2>("rgb"));
        Reject<ArgumentException>(() => material.SetShaderParameter("pair", new Vector2(1, 2)));
        Reject<ArgumentException>(() => material.SetShaderParameter("rgb", new Color(1, 2, 3, float.NaN)));
        Reject<ArgumentException>(() => material.SetShaderParameter("rectangle", new Rect2(0, 0, float.PositiveInfinity, 1)));
        Reject<ArgumentException>(() => material.SetShaderParameter<Color>("rgbArray", [Colors.Red, new(1, float.NaN, 1)]));
        Reject<ArgumentException>(() => material.SetShaderParameter<Rect2>("rectangles", [new(9, 9, 9, 9), new(0, 0, 1, float.NaN)]));
        Reject<ArgumentException>(() => material.SetShaderParameter<Vector2i>("pairs", [new(1, 2)]));
        Reject<ArgumentException>(() => material.GetShaderParameterArray<Color>("rgb"));
        Check(material.GetShaderParameterArray<Color>("rgbArray")[0] == Colors.White && material.GetShaderParameterArray<Rect2>("rectangles")[0] == new Rect2(1, 2, 3, 4), "Invalid arrays preserve every prior element.");
        using var copy = (ShaderMaterial)material.Duplicate(true); using var copiedShader = copy.Shader!;
        using var target = new ShaderMaterial(); target.CopyFromResource(material);
        rgb.SetValue(material, Vector3.Right); colors.Revert(material);
        Check(copy.GetShaderParameter<Color>("rgb") == new Color(.25f, .5f, .75f) && target.GetShaderParameterArray<Color>("rgbArray")[0] == Colors.White, "Deep duplication and copying preserve independent vector storage.");
        Check(material.GetShaderParameterArray<Color>("rgbArray").All(c => c == Colors.Black), "RGB array descriptor reverts every element.");
        InitializeVectorValues(material, Colors.Blue);
        using var reordered = LoadShader("ValuesReordered"); shader.SetSPIRV(reordered.GetSPIRV());
        Check(material.GetShaderParameter<Color>("rgb") == Colors.Blue && material.GetShaderParameterArray<Vector4i>("quads")[1] == new Vector4i(1, 2, 3, 4) && material.GetShaderParameterArray<Rect2>("rectangles")[1] == new Rect2(-1, -2, -3, -4), "Reload migrates values across offsets, strides and buffers.");
        rgb.SetValue(material, Vector3.Up);
        Check(material.GetShaderParameter<Color>("rgb") == Colors.Green, "Old descriptor snapshots use the new compatible layout.");
        using var signed = LoadShader("ValuesSigned"); shader.SetSPIRV(signed.GetSPIRV());
        Check(material.GetShaderParameter<Vector2i>("pair") == Vector2i.Zero && material.GetShaderParameter<Vector4i>("quad") == Vector4i.Zero && material.GetShaderParameterArray<Vector2i>("pairs").All(v => v == Vector2i.Zero) && material.GetShaderParameterArray<Vector4i>("quads").All(v => v == Vector4i.Zero), "A signedness change resets scalar and array vectors despite identical C# carrier types.");
        Check(material.GetShaderParameter<Color>("rgb") == Colors.Green, "Compatible values survive a signedness change elsewhere.");
        InitializeVectorValues(material, Colors.Blue); shader.SetSPIRV(reordered.GetSPIRV());
        Check(material.GetShaderParameter<Vector2i>("pair") == Vector2i.Zero, "Signed-to-unsigned migration also resets incompatible values.");
        InitializeVectorValues(material, Colors.Blue);
        var uniform = shader.GetProgram().Uniforms["rgb"];
        foreach (var offset in new[] { 4u, 16u })
        {
            var malformed = shader.GetSPIRV(); var words = MemoryMarshal.Cast<byte, uint>(malformed.AsSpan()); var changed = false;
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
                if ((words[at] & 0xffff) == 72 && (words[at] >> 16) == 5 && words[at + 3] == 35 && words[at + 4] == uniform.Offset)
                { words[at + 4] = offset; changed = true; }
            Check(changed, "Malformed fixture changes a reflected RGB offset.");
            if (offset == 4) Reject<NotSupportedException>(() => shader.SetSPIRV(malformed));
            else Reject<ArgumentException>(() => shader.SetSPIRV(malformed));
        }
        var badStride = shader.GetSPIRV(); var strideWords = MemoryMarshal.Cast<byte, uint>(badStride.AsSpan()); var changedStride = false;
        for (var at = 5; at < strideWords.Length; at += (int)(strideWords[at] >> 16))
            if ((strideWords[at] & 0xffff) == 71 && (strideWords[at] >> 16) == 4 && strideWords[at + 2] == 6 && strideWords[at + 3] == 16)
            { strideWords[at + 3] = 12; changedStride = true; }
        Check(changedStride, "Malformed fixture changes actual vector array strides.");
        Reject<NotSupportedException>(() => shader.SetSPIRV(badStride));
        Check(shader.GetSPIRV().SequenceEqual(reordered.GetSPIRV()) && material.GetShaderParameter<Color>("rgb") == Colors.Blue, "Rejected external layouts preserve the previous shader and material.");
        Color[] values = [Colors.White, Colors.Black]; Rect2[] rects = [new(1, 2, 3, 4), new(-1, -2, -3, -4)];
        Vector2i[] pairs = [new(int.MinValue, -1), new(0, 1)]; Vector4i[] quads = [new(-1, int.MinValue, int.MaxValue, 123456789), new(1, 2, 3, 4)];
        for (var pass = 0; pass < 2; pass++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 2000; i++)
            {
                material.SetShaderParameter("rgb", Colors.Blue); material.SetShaderParameter<Color>("rgbArray", values.AsSpan());
                material.SetShaderParameter("rectangle", rects[0]); material.SetShaderParameter<Rect2>("rectangles", rects.AsSpan());
                material.SetShaderParameter("pair", pairs[0]); material.SetShaderParameter<Vector2i>("pairs", pairs.AsSpan());
                material.SetShaderParameter("quad", quads[0]); material.SetShaderParameter<Vector4i>("quads", quads.AsSpan());
                _ = material.GetShaderParameter<Color>("rgb"); _ = material.GetShaderParameter<Rect2>("rectangle"); _ = material.GetShaderParameter<Vector4i>("quad");
            }
            if (pass == 1) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed vector setters, span setters and value getters allocate zero bytes.");
        }
        Console.WriteLine($"Shader vector value contract passed: {fixture}; 2000 warm iterations, zero bytes.");
    }

    private static void VerifyShaderVectorFrame(string backend, string fixture)
    {
        using var shader = LoadShader(fixture); using var reordered = LoadShader("ValuesReordered");
        using var material = new ShaderMaterial { Shader = shader }; InitializeVectorValues(material, new(.25f, .5f, .75f, .125f));
        using var copy = (ShaderMaterial)material.Duplicate(); copy.SetShaderParameter("rgb", Colors.Green);
        var window = new Window { Size = new(64, 32) };
        var nodes = new[] { new CanvasNode { Name = "first", Material = material }, new CanvasNode { Name = "copy", Material = copy, Position = new(16, 0) }, new CanvasNode { Name = "shared", Material = material, Position = new(32, 0) } };
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
                Pixel(image, 4, 4, frames == 1 ? new(.25f, .5f, .75f, 1) : frames < 4 ? Colors.Blue : new(0, 0, .5f, 1));
                Pixel(image, 20, 4, Colors.Green); Pixel(image, 36, 4, image.GetPixel(4, 4));
                if (frames == 1) material.SetShaderParameter("rgb", Colors.Blue);
                if (frames == 2) shader.SetSPIRV(reordered.GetSPIRV());
                if (frames == 3) material.SetShaderParameter<Color>("rgbArray", [new(.5f, .5f, .5f, .25f), Colors.Black]);
                if (frames == 40) { Check(allocated == 0 && nodes.All(n => n.Draws == 1), "Live vector parameters retain geometry without warm render allocations."); window.Tree!.Quit(); }
            };
        };
        if (backend == "compatibility") Reject<NotSupportedException>(() => Engine.Instance.Run(window));
        else Engine.Instance.Run(window);
        Released(window);
        Console.WriteLine(backend == "compatibility" ? $"Shader vector fallback rejection and cleanup passed: {fixture}." :
            $"Shader vector pixels/reload/cleanup passed: {backend}/{fixture}; {frames} frames, {allocated} warm bytes.");
    }
}
