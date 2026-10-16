using Electron2D;

internal static class ObjectDisposalRegistryTests
{
    internal static void Run()
    {
        AtomicVetoPublication();
        var canvas = new VetoCanvas();
        Verify(canvas, canvas.GetCanvasItem, rid => RenderingCanvasItemRegistry.Resolve(rid) as VetoCanvas,
            () => { using var value = new Entity(); _ = value.GetCanvasItem(); });
        var skeleton = new VetoSkeleton();
        Verify(skeleton, skeleton.GetSkeleton, rid => RenderingSkeletonRegistry.Resolve(rid) as VetoSkeleton,
            () => { using var value = new Skeleton(); _ = value.GetSkeleton(); });
        var viewport = new VetoWindow();
        Verify(viewport, viewport.GetViewportRID, rid => Viewport.FindViewportRID(rid) as VetoWindow,
            () => { using var value = new Window(); _ = value.GetViewportRID(); });
        var texture = new VetoTexture();
        Verify(texture, texture.GetRID, rid => RenderingTextureRegistry.ResolveResource(rid) as VetoTexture,
            () => { using var value = new ImageTexture(); _ = value.GetRID(); });
        var mesh = new VetoMesh();
        Verify(mesh, mesh.GetRID, rid => RenderingMeshRegistry.Resolve(rid) as VetoMesh,
            () => { using var value = new ArrayMesh(); _ = value.GetRID(); });
        Console.WriteLine("Disposal veto registry tests: closed lookups, 256-registration sweeps, identity restoration and terminal retirement passed across five rendering roles.");
    }

    private sealed class VetoPulse : ElectronObject
    {
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) CancelDisposal(); }
    }
    private static void AtomicVetoPublication()
    {
        var value = new VetoPulse(); using var start = new Barrier(2); var stop = 0; long samples = 0, committed = 0;
        var thread = new Thread(() => { start.SignalAndWait(); while (Volatile.Read(ref stop) == 0) { if (value.IsDisposalCommitted) committed++; samples++; } });
        thread.Start(); start.SignalAndWait();
        for (var i = 0; i < 262144; i++) value.Dispose();
        Volatile.Write(ref stop, 1); thread.Join();
        Check(samples > 0 && committed == 0 && !value.IsDisposed, "A veto-only attempt must never publish a committed phase to concurrent retirement readers");
        Console.WriteLine($"Atomic veto publication: 262144 attempts, {samples} samples, {committed} false commits.");
    }

    private interface IVeto
    {
        bool Veto { get; set; }
        Action? During { get; set; }
    }
    private static void ObservePreDelete(ElectronObject value, int what, bool veto, Action? during)
    {
        if (what != ElectronObject.NotificationPreDelete) return;
        if (veto) value.CancelDisposal(); during?.Invoke();
    }
    private static void Verify<T>(T value, Func<RID> getRID, Func<RID, T?> resolve, Action registerTemporary) where T : ElectronObject, IVeto
    {
        var rid = getRID();
        value.During = () =>
        {
            Check(!Available(), "A closed object is unavailable without losing its registered identity");
            for (var i = 0; i < 256; i++) registerTemporary();
        };
        value.Dispose(); Check(!value.IsDisposed && ReferenceEquals(resolve(rid), value) && getRID() == rid, "A veto preserves identity across lookup and registration sweeps");
        value.During = null; value.Veto = false; value.Dispose(); Check(value.IsDisposed && !Available(), "Terminal cleanup still retires the registered identity");
        bool Available() { try { return resolve(rid) is not null; } catch (ArgumentException) { return false; } }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private sealed class VetoCanvas : Entity, IVeto
    {
        public bool Veto { get; set; } = true; public Action? During { get; set; }
        protected override void OnNotification(int what) { base.OnNotification(what); ObservePreDelete(this, what, Veto, During); }
    }
    private sealed class VetoSkeleton : Skeleton, IVeto
    {
        public bool Veto { get; set; } = true; public Action? During { get; set; }
        protected override void OnNotification(int what) { base.OnNotification(what); ObservePreDelete(this, what, Veto, During); }
    }
    private sealed class VetoWindow : Window, IVeto
    {
        public bool Veto { get; set; } = true; public Action? During { get; set; }
        protected override void OnNotification(int what) { base.OnNotification(what); ObservePreDelete(this, what, Veto, During); }
    }
    private sealed class VetoTexture : Texture, IVeto
    {
        public bool Veto { get; set; } = true; public Action? During { get; set; }
        public override int GetWidth() => 1;
        public override int GetHeight() => 1;
        protected override void OnNotification(int what) { base.OnNotification(what); ObservePreDelete(this, what, Veto, During); }
    }
    private sealed class VetoMesh : Mesh, IVeto
    {
        public bool Veto { get; set; } = true; public Action? During { get; set; }
        protected override void OnNotification(int what) { base.OnNotification(what); ObservePreDelete(this, what, Veto, During); }
        protected override int OnGetSurfaceCount() => 0;
        protected override MeshSurfaceData OnSurfaceGetArrays(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        protected override int OnSurfaceGetArrayLen(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        protected override int OnSurfaceGetArrayIndexLen(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        protected override ArrayFormat OnSurfaceGetFormat(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        protected override Material? OnSurfaceGetMaterial(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        protected override void OnSurfaceSetMaterial(int index, Material? material) => throw new ArgumentOutOfRangeException(nameof(index));
    }
}
