using Electron2D;

internal static class CanvasItemContractTests
{
    private sealed class Reader : Node
    {
        internal Action Read = null!;
        protected override void OnPhysicsProcess(double delta) => Read();
    }
    private sealed class ProcessWriter : Node
    {
        internal Action Write = null!;
        protected override void OnProcess(double delta) => Write();
    }
    private sealed class PaintedBody : RigidBody
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 12, 12), Colors.White);
    }
    private readonly record struct Metadata(ClipChildrenMode Clip, uint Lights, uint Visibility, Color Modulation, Color SelfModulation,
        Material? Material, TextureFilter Filter, TextureRepeat Repeat, OversamplingWithScale Oversampling, int Z, int Flags, World? World, CanvasLayer? Layer, RID Canvas, RID Item);
    private static Metadata Capture(CanvasItem item) => new(item.ClipChildren, item.LightMask, item.VisibilityLayer, item.Modulate, item.SelfModulate,
        item.Material, item.TextureFilter, item.TextureRepeat, item.OversamplingWithScale, item.ZIndex,
        (item.Visible ? 1 : 0) | (item.IsVisibleInTree ? 2 : 0) | (item.TopLevel ? 4 : 0) | (item.ZAsRelative ? 8 : 0) |
        (item.ShowBehindParent ? 16 : 0) | (item.YSortEnabled ? 32 : 0) | (item.UseParentMaterial ? 64 : 0) |
        (item.NotifyLocalTransformChanges ? 128 : 0) | (item.NotifyTransformChanges ? 256 : 0), item.GetWorld(), item.GetCanvasLayerNode(), item.GetCanvas(), item.GetCanvasItem());
    private sealed class Canvas : CanvasItem
    {
        internal Transform Pose = Transform.Identity;
        internal int Entries, Exits, LocalChanges;
        internal bool RejectChanges;
        protected override void ValidateMutation()
        {
            base.ValidateMutation();
            if (RejectChanges) throw new InvalidOperationException("Expected derived canvas mutation rejection");
        }
        public override Transform GetTransform() => Pose;
        internal void Place(Transform value) { Pose = value; NotifyLocalTransformChanged(); }
        internal void Resize() => NotifyItemRectChanged();
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationEnterCanvas) Entries++;
            if (what == NotificationExitCanvas) Exits++;
            if (what == NotificationLocalTransformChanged) LocalChanges++;
        }
    }

    internal static void Run(PhysicsServer.Backend backend, bool separate)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        try
        {
            using var world = new World(backend);
            using var root = new SubViewport { World = world, Size = new(240, 160), PhysicsObjectPicking = false };
            using var shape = new CircleShape { Radius = 2 };
            using var material = new CanvasItemMaterial();
            var layer = new CanvasLayer { Offset = new(10, 15) }; root.AddChild(layer);
            var canvas = new Canvas { NotifyLocalTransformChanges = true, NotifyTransformChanges = true, Material = material };
            canvas.Place(new(.13f, new(7, 11))); layer.AddChild(canvas);
            var body = new RigidBody { Position = new(20, 20), GravityScale = 0, CanSleep = false, CollisionLayer = 2, CollisionMask = 0, LinearVelocity = new(6, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, UseParentMaterial = true };
            body.AddChild(new CollisionShape { Shape = shape }); canvas.AddChild(body);
            var control = new Control { Position = new(80, 10), Size = new(12, 8), Scale = new(1.1f, .9f) }; canvas.AddChild(control);
            var neutral = new Node(); var independent = new Entity { Position = new(3, 4) }; neutral.AddChild(independent); canvas.AddChild(neutral);
            using var tree = new SceneTree(root);
            using var query = new PhysicsPointQueryParameters { CollisionMask = 2, CanvasInstanceID = layer.InstanceID };
            var hits = new PhysicsPointResult[1]; var identity = body.GetRID();
            Check(canvas.Entries == 1 && canvas.Exits == 0 && ReferenceEquals(body.GetWorld(), world) && ReferenceEquals(control.GetWorld(), world), "Custom, spatial, UI and physical branches share the selected world");
            Check(ReferenceEquals(body.GetCanvasLayerNode(), layer) && ReferenceEquals(control.GetCanvasLayerNode(), layer) && body.GetCanvas() == layer.GetCanvas() && body.Material is null && body.UseParentMaterial && ReferenceEquals(canvas.Material, material), "Canvas association and borrowed material authoring preserve branch identity");
            Check(Near(body.GetGlobalTransform(), canvas.Pose * body.GetTransform()) && Near(control.GetGlobalTransform(), canvas.Pose * control.GetTransform()) && Near(independent.GetGlobalTransform(), independent.GetTransform()), "Custom canvas placement composes through both concrete branches and stops at a neutral parent");
            var local = new Vector2(4, 5); var point = body.GetGlobalTransformWithCanvas() * local;
            using (var mouse = new InputEventMouseMotion { Position = point, GlobalPosition = point })
            using (var converted = (InputEventMouseMotion)body.MakeInputLocal(mouse))
                Check(!ReferenceEquals(converted, mouse) && (converted.Position - local).Length() < .001f && mouse.Position == point && (body.MakeCanvasPositionLocal(point) - local).Length() < .001f, "Physical receivers localize copied input through the same logical canvas transform");
            var rectangles = 0; canvas.ItemRectChanged += _ => rectangles++; canvas.Resize();
            Check(rectangles == 1, "A direct canvas subclass publishes geometry changes through the protected contract");
            canvas.Hide(); Check(!body.IsVisibleInTree && !control.IsVisibleInTree && independent.IsVisibleInTree, "Visibility follows direct canvas ancestry and stops at neutral nodes");
            body.TopLevel = true; Check(!body.IsVisibleInTree && Near(body.GetGlobalTransform(), body.GetTransform()) && ReferenceEquals(body.GetCanvasLayerNode(), layer), "TopLevel changes transform inheritance while retaining visibility and canvas identity");
            body.TopLevel = false; canvas.Show();
            Reject<InvalidOperationException>(() => body.DrawPrimitive([Vector2.Zero], [], []));
            Reject<InvalidOperationException>(() => Task.Run(() => body.GetGlobalTransform()).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => canvas.Hide()).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => Capture(body)).GetAwaiter().GetResult());
            var tick = PhysicsServer.SpaceGetTick(world.Space); layer.RemoveChild(canvas);
            Check(canvas.GetWorld() is null && body.GetWorld() is null && !body.IsVisibleInTree && canvas.Exits == 1 && body.GetRID() == identity, "Detaching retires membership without replacing retained public identities");
            layer.AddChild(canvas);
            Check(canvas.Entries == 2 && body.GetRID() == identity && PhysicsServer.SpaceGetTick(world.Space) == tick, "Reentry restores membership without a hidden physical step");
            var reader = new Reader { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread }; root.AddChild(reader);
            var own = new Canvas(); reader.AddChild(own);
            var workerBody = new RigidBody { Position = new(120, 30), GravityScale = 0, CanSleep = false, LinearVelocity = new(2, 0), CollisionLayer = 4, CollisionMask = 0 };
            workerBody.AddChild(new CollisionShape { Shape = shape }); reader.AddChild(workerBody);
            using var workerQuery = new PhysicsPointQueryParameters { CollisionMask = 4 };
            var workerIdentity = workerBody.GetRID();
            var coldIdentityNode = new Entity(); root.AddChild(coldIdentityNode); RID coldIdentity = default;
            CanvasItem[] observed = [canvas, body, control, independent, own];
            var expected = observed.Select(Capture).ToArray(); var reads = 0; var ownerThread = Environment.CurrentManagedThreadId;
            reader.Read = () =>
            {
                Check(Environment.CurrentManagedThreadId != ownerThread, "Metadata observations execute on a real processing-group worker");
                for (var i = 0; i < observed.Length; i++) Check(Capture(observed[i]) == expected[i], "Own/foreign-group canvas metadata matches coordinated owner observations");
                var alternate = (reads & 1) != 0;
                Author(workerBody, alternate, material);
                if (reads == 0)
                {
                    Reject<InvalidOperationException>(() => canvas.LightMask = 2);
                    Reject<InvalidOperationException>(() => workerBody.Visible = false);
                    Reject<InvalidOperationException>(() => workerBody.TopLevel = true);
                    coldIdentity = coldIdentityNode.GetCanvasItem();
                    Check(coldIdentity.IsValid() && coldIdentity == coldIdentityNode.GetCanvasItem(), "First-use foreign-group logical identities are stable without native startup");
                    Reject<InvalidOperationException>(() => RenderingServer.CanvasItemClear(workerBody.GetCanvasItem()));
                    Reject<InvalidOperationException>(() => workerBody.GetGlobalMousePosition());
                    own.RejectChanges = true;
                    Reject<InvalidOperationException>(() => own.Modulate = own.Modulate);
                    Reject<InvalidOperationException>(own.QueueRedraw);
                    own.RejectChanges = false;
                }
                reads++;
            };
            for (var i = 0; i < 128; i++) Step(i);
            AllocationWarmupCheck.Collect();
            tick = PhysicsServer.SpaceGetTick(world.Space); var position = body.Position; var changed = canvas.LocalChanges;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Step(i);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && reads == 256 && canvas.LocalChanges == changed + 128 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && body.Position != position, $"Composed canvas/physical frames retain exact motion, worker/notification counts and 0/0 managed bytes: {owner}/{all}");
            Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Canvas consumers preserve the requested numerical backend");
            tree.Dispose();
            Check(body.IsDisposed && canvas.IsDisposed && control.IsDisposed && !material.IsDisposed && !shape.IsDisposed, "Scene retirement preserves borrowed resource ownership");
            Reject<ObjectDisposedException>(() => canvas.GetWorld());
            Reject<ObjectDisposedException>(() => body.GetCanvasItem());
            Console.WriteLine($"CanvasItem contract {backend}/{separate}: custom/UI/physical ancestry, canvas/query identity, localization, lifecycle and 128 complete moving frames {owner}/{all} B.");

            void Step(int index)
            {
                canvas.Place(new(.13f, new(7 + (index & 1), 11)));
                canvas.ForceUpdateTransform();
                tree.PhysicsFrame(.01);
                query.Position = body.GlobalPosition;
                Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && ReferenceEquals(hits[0].Collider, body) && hits[0].ColliderRID == identity && ReferenceEquals(body.GetCanvasLayerNode(), layer), "Actual selected-backend queries retain identity after custom canvas transform publication");
                Check(workerBody.LightMask == (((reads - 1) & 1) != 0 ? 2u : 1u) && workerBody.IsVisibleInTree && workerBody.Position.X > 120, "Owner observations see committed worker canvas state alongside actual physical movement");
                Check(coldIdentityNode.GetCanvasItem() == coldIdentity, "Worker-created logical identities retain their owner-observed meaning");
                workerQuery.Position = workerBody.GlobalPosition;
                Check(world.DirectSpaceState.IntersectPoint(workerQuery, hits) == 1 && hits[0].ColliderRID == workerIdentity && ReferenceEquals(hits[0].Collider, workerBody), "Worker rendering metadata preserves its collider, canvas filter and query participation");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void Author(CanvasItem item, bool alternate, Material material)
    {
        item.LightMask = alternate ? 2u : 1u;
        item.Modulate = alternate ? Colors.Green : Colors.White;
        item.SelfModulate = alternate ? Colors.Gray : Colors.White;
        item.VisibilityLayer = alternate ? 2u : 1u;
        item.SetVisibilityLayerBit(31, alternate);
        Check(item.GetVisibilityLayerBit(31) == alternate, "Own-group indexed visibility bits preserve the authored high bit");
        item.ShowBehindParent = alternate;
        item.YSortEnabled = alternate;
        item.ZIndex = alternate ? 1 : 0;
        item.ZAsRelative = !alternate;
        item.Material = alternate ? material : null;
        item.UseParentMaterial = alternate;
        item.ClipChildren = alternate ? ClipChildrenMode.AndDraw : ClipChildrenMode.Disabled;
        item.TextureFilter = alternate ? TextureFilter.Nearest : TextureFilter.ParentNode;
        item.TextureRepeat = alternate ? TextureRepeat.Enabled : TextureRepeat.ParentNode;
        item.OversamplingWithScale = alternate ? OversamplingWithScale.Disabled : OversamplingWithScale.ParentNode;
        item.QueueRedraw();
        item.SetInstanceShaderParameter<float>("contract_value", alternate ? 1 : 0);
        Check(item.GetInstanceShaderParameter<float>("contract_value") == (alternate ? 1 : 0), "Own-group typed instance parameter authoring is immediately observable");
    }

    internal static void RunNative(PhysicsServer.Backend backend, string renderer, bool separate)
    {
        var previousRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var previousFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var previousThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var previousFPS = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate); Engine.MaxFPS = 240;
        try
        {
            using var world = new World(backend); using var shape = new CircleShape { Radius = 2 }; using var material = new CanvasItemMaterial();
            using var shader = renderer == "gpu" ? LoadInstanceShader() : null;
            using var shaderMaterial = shader is null ? null : new ShaderMaterial { Shader = shader };
            shaderMaterial?.SetShaderParameter("scale", 1f);
            using var window = new Window { Title = "Canvas metadata", Size = new(320, 240), World = world, CanvasCullMask = 1 };
            var writer = new ProcessWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread }; window.AddChild(writer);
            var body = new PaintedBody { Position = new(24, 24), GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0), CollisionMask = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); writer.AddChild(body);
            var frame = 0; var writes = 0; var ownerThread = Environment.CurrentManagedThreadId;
            long before = 0, allBefore = 0, active = 0, activeAll = 0, idle = 0, idleAll = 0;
            writer.Write = () =>
            {
                Check(Environment.CurrentManagedThreadId != ownerThread, "Native metadata authoring runs on a real worker");
                if (frame >= 160) return;
                // Prepare custom-pipeline eviction and reconstruction before measuring one-frame visibility cycles.
                Author(body, (frame & 1) != 0, material); body.VisibilityLayer = frame is >= 30 and < 35 || (frame & 3) == 3 ? 2u : 1u; writes++;
                if (shaderMaterial is not null)
                {
                    body.Material = shaderMaterial; body.UseParentMaterial = false;
                    body.SetInstanceShaderParameter<Color>("tint", (frame & 1) != 0 ? Colors.White : Colors.Red);
                    body.SetInstanceShaderParameter<bool>("enabled", true);
                    body.SetInstanceShaderParameter<Vector2i>("channel", new(1, 1));
                    body.SetInstanceShaderParameter<uint>("number", 2u);
                }
            };
            window.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The selected canvas renderer runs without fallback");
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    if (frame is >= 96 and < 160) { active += GC.GetAllocatedBytesForCurrentThread() - before; activeAll += GC.GetTotalAllocatedBytes(true) - allBefore; }
                    if (frame >= 160) { idle += GC.GetAllocatedBytesForCurrentThread() - before; idleAll += GC.GetTotalAllocatedBytes(true) - allBefore; }
                    if (frame is 0 or 1 or 3 or 223)
                    {
                        using var image = window.GetTexture().GetImage()!;
                        var scale = new Vector2(image.Width, image.Height) / window.GetVisibleRect().Size;
                        var point = (body.GlobalPosition + new Vector2(4, 4)) * scale;
                        var expected = (body.VisibilityLayer & window.CanvasCullMask) == 0 ? Colors.Black : body.Modulate * body.SelfModulate;
                        if (shaderMaterial is not null) expected *= body.GetInstanceShaderParameter<Color>("tint")!.Value;
                        var pixel = image.GetPixel((int)point.X, (int)point.Y);
                        Check(MathF.Abs(pixel.R - expected.R) < .03f && MathF.Abs(pixel.G - expected.G) < .03f && MathF.Abs(pixel.B - expected.B) < .03f, "Joined worker metadata reaches actual moving-body pixels");
                        if (Environment.GetEnvironmentVariable("ELECTRON2D_CANVAS_CONTRACT_CAPTURE") is { } folder)
                        { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, $"frame-{frame}.png")); }
                    }
                    if (frame == 95) AllocationWarmupCheck.Collect();
                    if (++frame != 224) return;
                    Check(active == 0 && activeAll == 0 && idle == 0 && idleAll == 0 && writes == 160, $"Complete native scheduling/physics/process/render intervals allocated active={active}/{activeAll}, idle={idle}/{idleAll} B");
                    Check(body.Position.X > 24.05f && PhysicsServer.SpaceGetTick(world.Space) > 2 && world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Metadata authoring retains actual selected-backend motion without fallback");
                    window.Tree!.Quit();
                };
            };
            Check(Engine.Run((_, tick) =>
            {
                if (tick == 1)
                {
                    using var mouse = new InputEventMouseMotion { Position = new(8, 8), GlobalPosition = new(8, 8) }; window.PushInput(mouse, true);
                }
                if (frame >= 96) Check(window.Size == new Vector2i(320, 240), "Native allocation intervals require the prepared client size");
                before = GC.GetAllocatedBytesForCurrentThread(); allBefore = GC.GetTotalAllocatedBytes(true);
            }, window) == 0 && frame == 224, "The complete native canvas metadata loop finishes");
            Console.WriteLine($"Canvas metadata native {renderer}/{backend}/{separate}: 160 worker updates, actual pixels, moving physics; active={active}/{activeAll}, idle={idle}/{idleAll} B in 128 complete intervals.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, previousRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, previousFallback);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previousThread); Engine.MaxFPS = previousFPS;
        }
    }

    private static Shader LoadInstanceShader()
    {
        var language = Environment.GetEnvironmentVariable("ELECTRON2D_CANVAS_CONTRACT_LANGUAGE") == "HLSL" ? "HLSL" : "GLSL";
        using var input = typeof(CanvasItemContractTests).Assembly.GetManifestResourceStream("TestShaders.ItemUniforms" + language + ".spv")!;
        using var bytes = new MemoryStream(); input.CopyTo(bytes);
        return Shader.CreateFromSPIRV(bytes.ToArray());
    }

    private static bool Near(Transform left, Transform right) => (left.X - right.X).Length() < .001f && (left.Y - right.Y).Length() < .001f && (left.Origin - right.Origin).Length() < .001f;
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }
}
