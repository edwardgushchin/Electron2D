using System.Diagnostics;
using Electron2D;
using Kind = Electron2D.RenderingServer.GlobalShaderParameterType;

internal static class ShaderGlobalsOverrideTests
{
    private static readonly float[] Matrix3 = [1, 0, 0, 0, 1, 0, .25f, 0, 1];
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Author(ShaderGlobalsOverride node, Texture texture, TextureLayered array)
    {
        node.SetShaderParameter<bool>("b", Kind.Bool, true);
        node.SetShaderParameter<int>("b2", Kind.BVec2, 3); node.SetShaderParameter<int>("b3", Kind.BVec3, 5); node.SetShaderParameter<int>("b4", Kind.BVec4, 9);
        node.SetShaderParameter<int>("i", Kind.Int, -2); node.SetShaderParameter<uint>("u", Kind.UInt, uint.MaxValue);
        node.SetShaderParameter<Vector2i>("i2", Kind.IVec2, new(1, -2)); node.SetShaderParameter<Vector3i>("i3", Kind.IVec3, new(1, 2, -3)); node.SetShaderParameter<Vector4i>("i4", Kind.IVec4, new(1, 2, 3, -4));
        node.SetShaderParameter<Vector2i>("u2", Kind.UVec2, new(-1, 2)); node.SetShaderParameter<Vector3i>("u3", Kind.UVec3, new(1, -2, 3)); node.SetShaderParameter<Vector4i>("u4", Kind.UVec4, new(1, 2, -3, 4));
        node.SetShaderParameter<Rect2i>("ri", Kind.Rect2i, new(1, 2, 3, 4)); node.SetShaderParameter<float>("weather/gain", Kind.Float, .75f);
        node.SetShaderParameter<Vector2>("v2", Kind.Vec2, new(1, 2)); node.SetShaderParameter<Vector3>("v3", Kind.Vec3, new(1, 2, 3)); node.SetShaderParameter<Vector4>("v4", Kind.Vec4, new(1, 2, 3, 4));
        node.SetShaderParameter<Color>("color", Kind.Color, Colors.Blue); node.SetShaderParameter<Rect2>("rect", Kind.Rect2, new(1, 2, 3, 4));
        node.SetShaderParameter<Transform>("basis", Kind.Mat2, Transform.Identity); node.SetShaderParameter<Transform>("affine", Kind.Transform2D, new(0, new Vector2(24, 0)));
        node.SetShaderParameter("matrix3", Kind.Mat3, Matrix3); node.SetShaderParameter("matrix4", Kind.Mat4, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 });
        node.SetShaderParameter("map", texture); node.SetShaderParameter("map-alias", texture); node.SetShaderParameter("layers", array);
    }
    private static void Verify(ShaderGlobalsOverride node)
    {
        Check(node.GetShaderParameterList().Length == 26, "All 25 logical roles and a shared texture alias survive storage.");
        Check(node.GetShaderParameter<float>("weather/gain") == .75f && node.GetShaderParameter<uint>("u") == uint.MaxValue, "Typed values and path-like names survive.");
        Check(node.GetShaderParameterType("basis") == Kind.Mat2 && node.GetShaderParameterType("affine") == Kind.Transform2D && node.GetShaderParameterType("b3") == Kind.BVec3, "Shared carriers retain distinct roles.");
        Check(node.GetShaderParameter<Transform>("affine")!.Value.Origin == new Vector2(24, 0) && node.GetShaderParameter<Rect2i>("ri") == new Rect2i(1, 2, 3, 4), "Nullable planar/integer rectangles retain portable codecs.");
        Check(node.GetShaderParameterMatrix("matrix3")!.SequenceEqual(Matrix3) && node.GetShaderParameterTexture("map") is ImageTexture && node.GetShaderParameterLayeredTexture("layers") is TextureArray, "Matrices and resource graphs restore.");
        Check(ReferenceEquals(node.GetShaderParameterTexture("map"), node.GetShaderParameterTexture("map-alias")), "Repeated scene image references preserve alias identity.");
    }
    internal static void RunManaged()
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image); using var array = new TextureArray(); array.CreateFromImages([image]);
        using var node = new ShaderGlobalsOverride(); Check(node.GetShaderParameterList().Length == 0 && node.GetShaderParameter<float>("none") is null && node.GetShaderParameterType("none") == Kind.Max, "Detached empty defaults.");
        Author(node, texture, array); Verify(node);
        Reject<ArgumentException>(() => node.SetShaderParameter<float>("weather/gain", Kind.Float, float.NaN));
        Reject<ArgumentException>(() => node.SetShaderParameter<int>("weather/gain", Kind.Int, 2));
        Reject<ArgumentException>(() => node.SetShaderParameter<float>("TIME", Kind.Float, 1));
        Reject<ArgumentException>(() => node.SetShaderParameter("matrix3", Kind.Mat3, new float[8]));
        Reject<ArgumentException>(() => node.GetShaderParameter<int>("weather/gain"));
        var copy = node.GetShaderParameterMatrix("matrix3")!; copy[0] = 99; Check(node.GetShaderParameterMatrix("matrix3")![0] == 1, "Matrix getter copies.");
        using var packed = new PackedScene(); packed.Pack(node); using var restored = (ShaderGlobalsOverride)packed.Instantiate(); Verify(restored);
        using var duplicate = (ShaderGlobalsOverride)packed.Instantiate(); Verify(duplicate);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "scene-globals-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "globals.e2dscene"); ResourceSaver.Save(packed, path);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add(typeof(ShaderGlobalsOverrideTests).Assembly.Location); start.ArgumentList.Add("--scene-globals-archive"); start.ArgumentList.Add(path); start.Environment.Remove("ELECTRON2D_TEST_SCENE_GLOBALS");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh scene-global load timed out."); }
            Check(child.ExitCode == 0, output.Result + error.Result);
        }
        finally { Directory.Delete(folder, true); }
        var root = new Node(); var branchA = new Node(); var branchB = new Node(); root.AddChild(branchA); root.AddChild(branchB);
        var first = new ShaderGlobalsOverride(); var second = new ShaderGlobalsOverride(); branchB.AddChild(second); branchA.AddChild(first);
        first.Notify(Node.NotificationEnterTree); Check(!first.IsInGroup("shader_overrides_group"), "Synthetic notifications cannot activate detached nodes.");
        using var tree = new SceneTree(root);
        Check(first.IsInGroup("shader_overrides_group_active") && !second.IsInGroup("shader_overrides_group_active") && second.GetConfigurationWarnings().Length == 1, "Selection follows tree order rather than registration order.");
        root.MoveChild(branchB, 0); Check(second.IsInGroup("shader_overrides_group_active") && !first.IsInGroup("shader_overrides_group_active"), "Ancestor reordering immediately changes selection.");
        second.Notify(Node.NotificationExitTree); Check(second.IsInGroup("shader_overrides_group_active"), "Synthetic exit preserves real membership.");
        second.Dispose(); Check(first.IsInGroup("shader_overrides_group_active"), "Disposal promotes a waiting node.");
        using var other = new SceneTree(new Node()); first.Reparent(other.Root); Check(other.SelectedShaderGlobals == first && tree.SelectedShaderGlobals is null, "Reparenting transfers tree ownership.");
        Task.Run(() => { Reject<InvalidOperationException>(() => first.SetShaderParameter<float>("gain", Kind.Float, .5f)); Reject<InvalidOperationException>(() => first.GetShaderParameterList()); }).GetAwaiter().GetResult();
        var capture = first.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => first.SetShaderParameter<float>("gain", Kind.Float, .5f)); } finally { Node.EndSceneCapture(capture); }
        first.SetShaderParameter<float>("gain", Kind.Float, .5f);
        for (var i = 0; i < 1024; i++) first.SetShaderParameter<float>("gain", Kind.Float, .5f);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) first.SetShaderParameter<float>("gain", Kind.Float, .75f);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared authoring updates allocate zero owner-thread bytes.");
        var registry = new ShaderGlobalParameters(); registry.Add("gain", Kind.Float, .1f); registry.BindScene(first.Parameters);
        var wrong = new ShaderGlobalParameters(); wrong.Add("gain", Kind.Int, 1); Reject<ArgumentException>(() => registry.BindScene(wrong));
        registry.BindScene(null);
        var uniform = new ShaderUniform("gain", typeof(float), 0, 0, 4, 0, 0, false, 0, false, 0);
        var program = new ShaderProgram([], [16], new(), globalUniforms: new() { ["gain"] = uniform });
        byte[][] buffers = [new byte[16]]; registry.Write(program, buffers);
        Check(uniform.Read<float>(buffers[0].AsSpan(0, 4)) == .1f, "Rejected scene binding clears previous effective values.");
        using var stale = new ShaderGlobalsOverride(); stale.SetShaderParameter<float>("gain", Kind.Float, .8f);
        var expired = ImageTexture.CreateFromImage(image); stale.SetShaderParameter("map", expired); expired.Dispose();
        registry.AddTexture("map", texture, false); Reject<ObjectDisposedException>(() => registry.BindScene(stale.Parameters)); registry.Write(program, buffers);
        Check(uniform.Read<float>(buffers[0].AsSpan(0, 4)) == .1f, "Disposed images reject before publishing any new scene entries.");
        var throwing = new FailingExit(); throwing.SetShaderParameter<float>("failure", Kind.Float, .5f); other.Root.AddChild(throwing); other.Root.MoveChild(throwing, 0);
        Reject<AggregateException>(() => throwing.Dispose()); Check(other.SelectedShaderGlobals == first && throwing.Parameters.Names().Length == 0, "Failed exit callbacks still retire values and promote the next node.");
        var lifecycleRoot = new Node(); var west = new Node(); var east = new Node(); lifecycleRoot.AddChild(west); lifecycleRoot.AddChild(east);
        var pending = new PreDeleteOverride(); var waiting = new ShaderGlobalsOverride(); west.AddChild(pending); east.AddChild(waiting);
        using var lifecycleTree = new SceneTree(lifecycleRoot);
        lifecycleTree.EditedSceneRoot = lifecycleRoot;
        var warningStates = new List<int>();
        lifecycleTree.NodeConfigurationWarningChanged += (_, changed) => { if (ReferenceEquals(changed, pending)) warningStates.Add(pending.GetConfigurationWarnings().Length); };
        pending.SetShaderParameter<float>("retained", Kind.Float, .75f);
        var notifications = 0; pending.Disposed += _ => notifications++;
        var propertyChanges = 0; pending.PropertyListChanged += _ => propertyChanges++;
        pending.BeforeDelete = () =>
        {
            lifecycleRoot.MoveChild(west, 1); Check(lifecycleTree.SelectedShaderGlobals == waiting, "Pre-delete reordering selects actual tree order.");
            lifecycleRoot.MoveChild(west, 0); Check(lifecycleTree.SelectedShaderGlobals == pending, "Pending deletion remains a real member until exit, including a cancellable pre-delete phase.");
            Check(warningStates.SequenceEqual(new[] { 1, 0 }), $"Pre-delete selection warnings must deliver [1, 0], got [{string.Join(", ", warningStates)}].");
            pending.CancelDisposal();
        };
        pending.Dispose();
        Check(!pending.IsDisposed && pending.Tree == lifecycleTree && lifecycleTree.SelectedShaderGlobals == pending && pending.GetShaderParameter<float>("retained") == .75f && notifications == 0, "Canceled disposal preserves active selection, membership and authored data.");
        pending.SetShaderParameter<float>("after-veto", Kind.Float, .25f); Check(propertyChanges == 1, "Canceled disposal preserves property subscriptions.");
        pending.BeforeDelete = () => { pending.CancelDisposal(); throw new InvalidOperationException("Vetoed notification failure."); };
        Reject<Exception>(() => pending.Dispose());
        Check(!pending.IsDisposed && lifecycleTree.SelectedShaderGlobals == pending && pending.IsInGroup("shader_overrides_group_active") && notifications == 0, "Veto plus callback failure preserves live active state.");
        pending.BeforeDelete = null; pending.Dispose();
        Check(lifecycleTree.SelectedShaderGlobals == waiting && notifications == 1 && pending.Parameters.Names().Length == 0, "Later committed disposal retires values and delivers the retained subscriber once.");
        Console.WriteLine("Scene globals: 25 roles, fresh-file graph, lifecycle/order/ownership and 1024 updates=0 B passed.");
    }
    internal static void RunArchive(string path)
    {
        Check(!RenderingServer.IsAvailable, "Fresh scene authoring has no renderer.");
        using var scene = ResourceLoader.Load<PackedScene>(path)!; using var node = (ShaderGlobalsOverride)scene.Instantiate(); Verify(node);
        Console.WriteLine("Fresh scene-global archive passed.");
    }
    private static void AuthorCurrent(ShaderGlobalsOverride node)
    {
        foreach (var name in RenderingServer.GlobalShaderParameterGetList())
        {
            var kind = RenderingServer.GlobalShaderParameterGetType(name);
            switch (kind)
            {
                case Kind.Bool: node.SetShaderParameter<bool>(name, kind, RenderingServer.GlobalShaderParameterGet<bool>(name)); break;
                case Kind.BVec2 or Kind.BVec3 or Kind.BVec4 or Kind.Int: node.SetShaderParameter<int>(name, kind, RenderingServer.GlobalShaderParameterGet<int>(name)); break;
                case Kind.UInt: node.SetShaderParameter<uint>(name, kind, RenderingServer.GlobalShaderParameterGet<uint>(name)); break;
                case Kind.IVec2 or Kind.UVec2: node.SetShaderParameter<Vector2i>(name, kind, RenderingServer.GlobalShaderParameterGet<Vector2i>(name)); break;
                case Kind.IVec3 or Kind.UVec3: node.SetShaderParameter<Vector3i>(name, kind, RenderingServer.GlobalShaderParameterGet<Vector3i>(name)); break;
                case Kind.IVec4 or Kind.UVec4: node.SetShaderParameter<Vector4i>(name, kind, RenderingServer.GlobalShaderParameterGet<Vector4i>(name)); break;
                case Kind.Rect2i: node.SetShaderParameter<Rect2i>(name, kind, RenderingServer.GlobalShaderParameterGet<Rect2i>(name)); break;
                case Kind.Float: node.SetShaderParameter<float>(name, kind, RenderingServer.GlobalShaderParameterGet<float>(name)); break;
                case Kind.Vec2: node.SetShaderParameter<Vector2>(name, kind, RenderingServer.GlobalShaderParameterGet<Vector2>(name)); break;
                case Kind.Vec3: node.SetShaderParameter<Vector3>(name, kind, RenderingServer.GlobalShaderParameterGet<Vector3>(name)); break;
                case Kind.Vec4: node.SetShaderParameter<Vector4>(name, kind, RenderingServer.GlobalShaderParameterGet<Vector4>(name)); break;
                case Kind.Color: node.SetShaderParameter<Color>(name, kind, RenderingServer.GlobalShaderParameterGet<Color>(name)); break;
                case Kind.Rect2: node.SetShaderParameter<Rect2>(name, kind, RenderingServer.GlobalShaderParameterGet<Rect2>(name)); break;
                case Kind.Mat2 or Kind.Transform2D: node.SetShaderParameter<Transform>(name, kind, RenderingServer.GlobalShaderParameterGet<Transform>(name)); break;
                case Kind.Mat3 or Kind.Mat4: node.SetShaderParameter(name, kind, RenderingServer.GlobalShaderParameterGetMatrix(name)); break;
                case Kind.Sampler2D: node.SetShaderParameter(name, RenderingServer.GlobalShaderParameterGetTexture(name)); break;
                case Kind.Sampler2DArray: node.SetShaderParameter(name, RenderingServer.GlobalShaderParameterGetLayeredTexture(name)); break;
            }
        }
    }
    internal static void RunNative(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var language = profile.Contains("hlsl", StringComparison.Ordinal) ? "HLSL" : "GLSL";
        using var shader = Shader.CreateFromSPIRV(ShaderGlobalParametersTests.Module(language, "vert"), ShaderGlobalParametersTests.Module(language, "frag"));
        using var material = new ShaderMaterial { Shader = shader };
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image); using var array = new TextureArray(); array.CreateFromImages([image]);
        image.Fill(Colors.Red); using var red = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(96, 48) }; window.AddChild(new Box { Position = new(8, 8), Material = material });
        var first = new ShaderGlobalsOverride(); first.SetShaderParameter<float>("gain", Kind.Float, .75f);
        var second = new ShaderGlobalsOverride(); second.SetShaderParameter<float>("gain", Kind.Float, .25f);
        window.AddChild(first); window.AddChild(second);
        var vetoNode = new PreDeleteOverride(); vetoNode.SetShaderParameter<float>("gain", Kind.Float, .75f); window.AddChild(vetoNode); window.MoveChild(vetoNode, 0);
        var frame = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "Programmable backend is effective.");
            ShaderGlobalParametersTests.PopulatePublic(texture, array);
            Check(second.GetPropertyList().Count(p => p.Name.StartsWith("params/", StringComparison.Ordinal)) == 25 && second.GetPropertyList().Count(p => p.Name.StartsWith("params/", StringComparison.Ordinal) && p.IsStored) == 1, "Declared unset globals are discoverable without persisting defaults.");
            AuthorCurrent(first); first.SetShaderParameter<float>("gain", Kind.Float, .75f); RenderingServer.SetDefaultClearColor(Colors.Black);
            using (var livePacked = new PackedScene())
            {
                livePacked.Pack(first); using var liveRestored = (ShaderGlobalsOverride)livePacked.Instantiate();
                Check(liveRestored.GetShaderParameterList().Length == 25 && liveRestored.GetShaderParameter<float>("gain") == .75f, "Live nonstored declaration descriptors restore authored scene properties.");
            }
            Reject<ArgumentException>(() => first.SetShaderParameter<int>("gain", Kind.Int, 1));
            var incompatible = new ShaderGlobalsOverride(); incompatible.SetShaderParameter<int>("gain", Kind.Int, 1); window.AddChild(incompatible);
            Reject<AggregateException>(() => window.MoveChild(incompatible, 0));
            Check(incompatible.IsInGroup("shader_overrides_group_active"), "Incompatible activation retains inspectable selection.");
            var notified = false; incompatible.PropertyListChanged += _ => notified = true;
            Reject<AggregateException>(() => incompatible.SetShaderParameter<float>("deferred", Kind.Float, .2f));
            Check(notified && incompatible.GetShaderParameter<float>("deferred") == .2f, "Committed descriptor changes remain observable after unrelated publication failure.");
            incompatible.SetShaderParameter<float>("gain", Kind.Float, null); window.RemoveChild(incompatible); incompatible.Dispose();
            vetoNode.BeforeDelete = () => { window.MoveChild(first, 0); window.MoveChild(vetoNode, 0); vetoNode.CancelDisposal(); };
            vetoNode.Dispose(); Check(!vetoNode.IsDisposed && vetoNode.IsInGroup("shader_overrides_group_active") && vetoNode.GetShaderParameter<float>("gain") == .75f, "Canceled node remains the rendered global override after actual reorder.");
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frame is >= 32 and < 96) first.SetShaderParameter<float>("gain", Kind.Float, (frame & 1) == 0 ? .5f : .75f);
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame is >= 32 and < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame is >= 96 and < 160) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame < 12)
                {
                    var gain = frame switch { 0 or 1 => .75f, 2 or 7 or 11 => .1f, 3 or 6 or 8 => .5f, 4 or 5 => .25f, 9 => .4f, _ => .6f };
                    using var pixels = window.GetTexture().GetImage()!; var color = pixels.GetPixel(8 + 24 + (int)(16 * gain) + 4, 12);
                    Check(MathF.Abs(color.R - gain * gain) < .01f && MathF.Abs(color.G - gain * gain * (frame == 5 ? 0 : .5f)) < .01f, $"Scene {profile} phase {frame}: {color}, gain={gain}.");
                    switch (frame)
                    {
                        case 0: RenderingServer.GlobalShaderParameterSet("gain", .1f); vetoNode.BeforeDelete = null; vetoNode.Dispose(); break;
                        case 1: first.SetShaderParameter<float>("gain", Kind.Float, null); break;
                        case 2: first.SetShaderParameter<float>("gain", Kind.Float, .5f); break;
                        case 3: window.MoveChild(second, 0); break;
                        case 4: second.SetShaderParameter("map", red); break;
                        case 5: second.Dispose(); break;
                        case 6: window.RemoveChild(first); break;
                        case 7: window.AddChild(first); break;
                        case 8: RenderingServer.GlobalShaderParameterSetOverride<float>("gain", .4f); break;
                        case 9: first.SetShaderParameter<float>("gain", Kind.Float, .6f); break;
                        case 10: window.RemoveChild(first); break;
                        case 11: window.AddChild(first); break;
                    }
                }
                frame++; if (frame == 160) { Check(active == 0 && idle == 0, $"Scene frames active={active}, idle={idle} B."); window.Tree!.Quit(); }
            };
        };
        try { Check(Engine.Run(window) == 0 && frame == 160, "Scene-global frame lifecycle completed."); }
        finally { vetoNode.BeforeDelete = null; if (!window.IsDisposed) window.Dispose(); if (!vetoNode.IsDisposed) vetoNode.Dispose(); if (!first.IsDisposed) first.Dispose(); }
        Check(!texture.IsDisposed && !array.IsDisposed && !red.IsDisposed, "Borrowed images survive scene/session cleanup.");
        Console.WriteLine($"Scene globals {profile}: 12 pixel phases, 64 active={active} B/64 idle={idle} B passed.");
    }
    private sealed class PreDeleteOverride : ShaderGlobalsOverride
    {
        internal Action? BeforeDelete;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) BeforeDelete?.Invoke(); }
    }
    private sealed class FailingExit : ShaderGlobalsOverride
    {
        protected override void OnExitTree() => throw new InvalidOperationException("Injected exit failure.");
    }
    private sealed class Box : Entity
    {
        protected override void OnDraw() => DrawRect(new Rect2(0, 0, 16, 16), Colors.White);
    }
}
