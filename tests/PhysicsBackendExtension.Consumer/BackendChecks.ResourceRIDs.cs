using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunResourceRIDs(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("DeferredContract", () => new Solver());
        try
        {
            for (var kind = 0; kind < 9; kind++)
            {
                var abandoned = AbandonShapeRID(kind); Collect();
                Check(!Alive(abandoned.Reference), "An unused borrowed shape RID must not retain the managed resource");
                Reject<ArgumentException>(() => PhysicsServer.ShapeGetType(abandoned.RID));
                Reject<ArgumentException>(() => PhysicsServer.FreeRID(abandoned.RID));
            }
            ColdRIDCollection();
            for (var kind = 0; kind < 8; kind++) OwnedRIDLifetime(kind);
            for (var kind = 0; kind < 9; kind++) QueryRIDLifetime(kind);
            using var frames = new DeferredFrames(backend);
            for (var kind = 0; kind < 9; kind++)
                for (var mode = 0; mode < 3; mode++) BindingLifetime(frames.World, kind, mode);
            ResourceIdentityRoles(); FontIdentityRoles();
            for (var i = 0; i < 128; i++) frames.Frame();
            Collect(); var position = frames.Body.Position;
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) frames.Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && frames.Body.Position.DistanceTo(position + new Vector2(4, 1)) < .003f,
                $"RID lifecycle retains full moving physical frames at strict zero managed bytes: {own}/{all}");
            Console.WriteLine($"Resource RIDs {backend?.ToString() ?? "registered"}, threaded={threaded}: 9 shape kinds, 27 raw binding/GC cases, inherited resource roles, 128 moving full frames at {own}/{all} B.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static Shape RIDShape(int kind) => kind switch
    {
        0 => new CircleShape { Radius = 2 },
        1 => new RectangleShape { Size = new(4, 4) },
        2 => new CapsuleShape { Radius = 1, Height = 4 },
        3 => new SegmentShape { A = new(-2, 0), B = new(2, 0) },
        4 => new ConvexPolygonShape { Points = [new(-2, -2), new(2, -2), new(0, 2)] },
        5 => new ConcavePolygonShape { Segments = [new(-2, 0), new(2, 0)] },
        6 => new WorldBoundaryShape(),
        7 => new SeparationRayShape { Length = 4 },
        _ => new CustomShape(ShapeGeometry.Custom("rid-gc/1", [1], new(-1, -1, 2, 2), new(1, 1, Vector2.Zero), [], NoRIDContact)),
    };
    private static bool NoRIDContact(ShapeGeometry first, Transform firstPose, Vector2 firstMotion,
        ShapeGeometry second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
    { count = 0; return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Alive(WeakReference<Shape> reference) => reference.TryGetTarget(out _);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (RID RID, WeakReference<Shape> Reference) AbandonShapeRID(int kind)
    {
        var shape = RIDShape(kind);
        Resource resource = shape;
        var rid = resource.GetRID();
        Check(PhysicsServer.ShapeGetType(rid) == shape.GetGeometryData().Type, "A shape RID resolves its real geometry kind");
        Check(rid.IsValid() && resource.GetRID() == rid, "Inherited Resource.GetRID returns the actual stable shape identity");
        return (rid, new(shape));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (RID RID, WeakReference<Shape> Reference) AttachRID(int kind, RID body, RID area, bool disabled)
    {
        var shape = RIDShape(kind); var rid = shape.GetRID();
        PhysicsServer.BodyAddShape(body, rid, disabled: disabled);
        PhysicsServer.AreaAddShape(area, rid, disabled: disabled);
        return (rid, new(shape));
    }

    private static void BindingLifetime(World world, int kind, int mode)
    {
        var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            if (mode != 0 && kind != 8)
            {
                PhysicsServer.BodySetSpace(body, world.Space); PhysicsServer.AreaSetSpace(area, world.Space);
            }
            var first = AttachRID(kind, body, area, mode == 2); Collect();
            Check(Alive(first.Reference) && PhysicsServer.BodyGetShape(body, 0) == first.RID && PhysicsServer.AreaGetShape(area, 0) == first.RID,
                "Body and Area slots retain the source, including disabled and detached slots");
            var second = AbandonShapeRID(kind);
            // The source must stay live until the RID has been bound.
            using (var replacement = RIDShape(kind))
            {
                var replacementRID = replacement.GetRID();
                PhysicsServer.BodySetShape(body, 0, replacementRID);
                Collect(); Check(Alive(first.Reference), "The Area still retains the original source after body replacement");
                PhysicsServer.AreaClearShapes(area); Collect();
                Check(!Alive(first.Reference), "The last slot release permits the original source to collect");
                Check(PhysicsServer.BodyGetShape(body, 0) == replacementRID, "Replacing one slot preserves the replacement identity");
                PhysicsServer.BodyClearShapes(body);
            }
            Collect(); Check(!Alive(second.Reference), "A separate unbound identity does not acquire a slot lease");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); }
    }

    private static void QueryRIDLifetime(int kind)
    {
        using var query = new PhysicsShapeQueryParameters(); var source = AttachQueryRID(kind, query);
        Collect(); Check(Alive(source.Reference) && query.ShapeRID == source.RID, "Typed shape query parameters retain the actual source resource");
        query.ShapeRID = query.ShapeRID; Collect(); Check(Alive(source.Reference), "Equal RID assignment preserves the typed query source");
        query.ShapeRID = default; Collect(); Check(!Alive(source.Reference), "Clearing query ownership releases the source without explicit disposal");
    }

    private static void ColdRIDCollection()
    {
        using var live = new CircleShape(); var liveRID = live.GetRID();
        var dead = AbandonShapeRID(0); Collect();
        for (var i = 0; i < 512; i++) AbandonShapeRID(i % 9);
        Collect();
        Check(!Alive(dead.Reference) && live.GetRID() == liveRID && PhysicsServer.ShapeGetType(liveRID) == PhysicsServer.ShapeType.Circle,
            "Cold registration sweeps preserve live source identities without retaining abandoned resources");
        Reject<ArgumentException>(() => PhysicsServer.ShapeGetType(dead.RID));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (RID RID, WeakReference<Shape> Reference) AttachQueryRID(int kind, PhysicsShapeQueryParameters query)
    {
        var shape = RIDShape(kind); query.Shape = shape; return (query.ShapeRID, new(shape));
    }

    private static void OwnedRIDLifetime(int kind)
    {
        var rid = kind switch
        {
            0 => PhysicsServer.CircleShapeCreate(),
            1 => PhysicsServer.RectangleShapeCreate(),
            2 => PhysicsServer.CapsuleShapeCreate(),
            3 => PhysicsServer.SegmentShapeCreate(),
            4 => PhysicsServer.ConvexPolygonShapeCreate(),
            5 => PhysicsServer.ConcavePolygonShapeCreate(),
            6 => PhysicsServer.WorldBoundaryShapeCreate(),
            _ => PhysicsServer.SeparationRayShapeCreate()
        };
        var released = false;
        using var holder = new StaticBody();
        try
        {
            using var source = RIDShape(kind); PhysicsServer.ShapeSetData(rid, source);
            var shapeType = PhysicsServer.ShapeGetType(rid);
            PhysicsServer.BodyAddShape(holder.GetRID(), rid);
            var owner = holder.ShapeFindOwner(0); var first = holder.ShapeOwnerGetShape(owner, 0);
            Check(((Resource)first).GetRID() == rid, "A server-backed scene view exposes its existing owning RID");
            Reject<InvalidOperationException>(first.Dispose);
            Collect(); Check(PhysicsServer.ShapeGetType(rid) == shapeType, "Owned server geometry survives collection");
            PhysicsServer.ShapeSetData(rid, source);
            Check(first.IsDisposed, "Owned data replacement retires the old private view");
            Reject<ObjectDisposedException>(() => first.GetRID());
            var second = holder.ShapeOwnerGetShape(owner, 0); Check(second.GetRID() == rid, "Replacement retains the owning RID");
            PhysicsServer.FreeRID(rid); released = true;
            Check(second.IsDisposed && PhysicsServer.BodyGetShapeCount(holder.GetRID()) == 0, "Owned free retires the view and dependent logical slots");
            Reject<ArgumentException>(() => PhysicsServer.ShapeGetType(rid));
        }
        finally { if (!released) PhysicsServer.FreeRID(rid); }
    }

    private static void ResourceIdentityRoles()
    {
        Resource[] empty = [new Resource(), new Image(), new PhysicsMaterial()];
        Resource[] native = [new Shader(), new ShaderMaterial(), new CanvasItemMaterial(), new BlitMaterial(), new ParticleProcessMaterial(),
            new ImageTexture(), new TextureRD(), new TextureArray(), new TextureArrayRD(), new PlaceholderTextureArray(), new DrawableTexture(),
            new DPITexture(), new MeshTexture(), new ArrayMesh(), new ImmediateMesh(), new PlaceholderMesh(), new MultiMesh(), new OccluderPolygon()];
        var ids = new HashSet<RID>();
        try
        {
            foreach (var resource in empty) Check(!resource.GetRID().IsValid(), "Managed-only resources keep the empty base identity");
            foreach (var resource in native)
            {
                var rid = resource.GetRID(); Check(rid.IsValid() && rid == resource.GetRID() && ids.Add(rid), "Existing backend resource roles have stable distinct logical identities");
            }
            using var atlas = new AtlasTexture { Atlas = (Texture)native[5] };
            Check(((Resource)atlas).GetRID() == native[5].GetRID(), "An atlas forwards the source identity through Resource dispatch");
        }
        finally
        {
            foreach (var resource in native) resource.Dispose(); foreach (var resource in empty) resource.Dispose();
        }
        foreach (var resource in native) Reject<ObjectDisposedException>(() => resource.GetRID());
        foreach (var resource in empty) Reject<ObjectDisposedException>(() => resource.GetRID());
    }
}

public static partial class BackendChecks
{
    private static void FontIdentityRoles()
    {
        var server = TextServerManager.GetPrimaryInterface();
        using var font = new FontFile { Data = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/IBMPlexSans-Regular.ttf")), AllowSystemFallback = false };
        var rid = ((Resource)font).GetRID(); var view = server.FontGetResource(rid);
        Check(rid.IsValid() && font.GetRID() == rid && ((Resource)view).GetRID() == rid, "Font resources and borrowed source projections expose the same current source");
        Reject<InvalidOperationException>(() => server.FreeRID(rid));
        using var variation = new FontVariation { BaseFont = font, VariationEmbolden = .25f };
        var varied = ((Resource)variation).GetRID(); Check(varied.IsValid() && variation.GetRID() == varied, "A configured variation resolves a stable realized source");
        using var system = new SystemFont(); var systemRID = ((Resource)system).GetRID();
        Check(systemRID.IsValid() && system.GetRID() == systemRID, "System font selection resolves its actual native source");
        var owned = server.CreateFont(); var link = server.CreateFontLinkedVariation(owned);
        try
        {
            Check(((Resource)server.FontGetResource(owned)).GetRID() == owned && ((Resource)server.FontGetResource(link)).GetRID() == link,
                "Owned and linked text projections preserve their distinct service identities");
        }
        finally { server.FreeRID(link); server.FreeRID(owned); }
        font.Dispose(); Reject<ObjectDisposedException>(() => font.GetRID()); Reject<InvalidOperationException>(() => view.GetRID());
    }
}
