using Electron2D;

internal sealed class CircleQueries : PhysicsDirectSpaceStateExtension
{
    internal readonly record struct Actor(RID RID, CircleShape Shape, bool Area);
    private readonly Actor[] _actors;
    private readonly CircleShape? _queryShape;
    internal RID Suppressed, Injected;
    internal PhysicsPointResult? CachedPoint;
    internal int BadCount;
    internal bool BadFractions, BadPoints, Duplicate, Throw;
    internal Action? OnQuery;
    internal CircleQueries(RID space, Actor[] actors, CircleShape? queryShape = null) : base(space)
    {
        _actors = actors; _queryShape = queryShape;
        Array.Sort(_actors, (a, b) => a.RID.GetID().CompareTo(b.RID.GetID()));
    }
    private bool Accept(Actor actor, uint mask, bool bodies, bool areas, ulong? canvas = null) =>
        actor.RID != Suppressed && !IsBodyExcludedFromQuery(actor.RID) && (actor.Area ? areas : bodies) &&
        (mask & (actor.Area ? PhysicsServer.AreaGetCollisionLayer(actor.RID) : PhysicsServer.BodyGetCollisionLayer(actor.RID))) != 0 &&
        (!canvas.HasValue || canvas.Value == (actor.Area ? PhysicsServer.AreaGetCanvasInstanceID(actor.RID) : PhysicsServer.BodyGetCanvasInstanceID(actor.RID)));
    private static Vector2 Center(Actor actor) => (actor.Area ? PhysicsServer.AreaGetTransform(actor.RID) : PhysicsServer.BodyGetTransform(actor.RID)).Origin;
    private static float Entry(Vector2 from, Vector2 motion, Vector2 center, float radius)
    {
        var offset = from - center; var a = motion.LengthSquared();
        if (a == 0) return float.PositiveInfinity;
        var b = offset.Dot(motion); var discriminant = b * b - a * (offset.LengthSquared() - radius * radius);
        if (discriminant < 0) return float.PositiveInfinity;
        var fraction = (-b - MathF.Sqrt(discriminant)) / a;
        return fraction is >= 0 and <= 1 ? fraction : float.PositiveInfinity;
    }
    private float Radius(RID shape) => _queryShape is not null && _queryShape.GetRID() == shape
        ? _queryShape.Radius : throw new ArgumentException("The fixture query shape is not registered.", nameof(shape));
    private static Vector2 Closest(Vector2 from, Vector2 motion, Vector2 point) =>
        from + motion * (motion == Vector2.Zero ? 0 : Math.Clamp((point - from).Dot(motion) / motion.LengthSquared(), 0, 1));
    protected override PhysicsRayResult? IntersectRayCore(Vector2 from, Vector2 to, uint collisionMask, bool collideWithBodies, bool collideWithAreas, bool hitFromInside)
    {
        PhysicsRayResult? hit = null; var best = float.PositiveInfinity;
        foreach (var actor in _actors)
        {
            if (!Accept(actor, collisionMask, collideWithBodies, collideWithAreas)) continue;
            var center = Center(actor); var inside = from.DistanceTo(center) < actor.Shape.Radius;
            if (inside && !hitFromInside) continue;
            var fraction = inside ? 0 : Entry(from, to - from, center, actor.Shape.Radius);
            if (fraction >= best) continue;
            best = fraction; var point = from + (to - from) * fraction;
            hit = new(actor.RID, 0, point, inside ? Vector2.Zero : (point - center).Normalized());
        }
        return hit;
    }
    protected override int IntersectPointCore(Vector2 position, ulong canvasInstanceID, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<PhysicsPointResult> results)
    {
        OnQuery?.Invoke();
        if (Throw) throw new InvalidOperationException("Expected query failure.");
        if (Injected.IsValid()) { if (!results.IsEmpty) results[0] = new(Injected, 0); return 1; }
        if (CachedPoint is { } cached && !results.IsEmpty) { results[0] = cached; return 1; }
        var count = 0;
        foreach (var actor in _actors)
            if (Accept(actor, collisionMask, collideWithBodies, collideWithAreas, canvasInstanceID) && position.DistanceTo(Center(actor)) <= actor.Shape.Radius && count < results.Length)
                results[count++] = new(actor.RID, 0);
        if (Duplicate && count == 1 && results.Length > 1) { results[1] = results[0]; return 2; }
        return BadCount != 0 ? BadCount : count;
    }
    protected override int IntersectShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<PhysicsShapeResult> results)
    {
        var count = 0; var radius = Radius(shape) + margin;
        foreach (var actor in _actors)
            if (Accept(actor, collisionMask, collideWithBodies, collideWithAreas) &&
                Closest(transform.Origin, motion, Center(actor)).DistanceTo(Center(actor)) <= radius + actor.Shape.Radius && count < results.Length)
                results[count++] = new(actor.RID, 0);
        return count;
    }
    protected override (float SafeFraction, float UnsafeFraction) CastMotionCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas)
    {
        if (BadFractions) return (.8f, .2f);
        var fraction = 1f; var radius = Radius(shape) + margin;
        foreach (var actor in _actors)
            if (Accept(actor, collisionMask, collideWithBodies, collideWithAreas) && transform.Origin.DistanceTo(Center(actor)) > radius + actor.Shape.Radius)
                fraction = MathF.Min(fraction, Entry(transform.Origin, motion, Center(actor), radius + actor.Shape.Radius));
        return (fraction, fraction);
    }
    protected override int CollideShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<Vector2> results)
    {
        if (BadPoints && results.Length >= 2) { results[0] = new(float.NaN, 0); return 1; }
        var count = 0; var radius = Radius(shape) + margin;
        foreach (var actor in _actors)
        {
            if (!Accept(actor, collisionMask, collideWithBodies, collideWithAreas)) continue;
            var center = Center(actor); var point = Closest(transform.Origin, motion, center);
            if (point.DistanceTo(center) > radius + actor.Shape.Radius || count * 2 >= results.Length) continue;
            var normal = (point - center).Normalized(); if (normal == Vector2.Zero) normal = Vector2.Up;
            results[count * 2] = point - normal * radius; results[count++ * 2 + 1] = center + normal * actor.Shape.Radius;
        }
        return count;
    }
    protected override PhysicsRestInfo? GetRestInfoCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas)
    {
        PhysicsRestInfo? hit = null; var depth = float.NegativeInfinity; var radius = Radius(shape) + margin;
        foreach (var actor in _actors)
        {
            if (!Accept(actor, collisionMask, collideWithBodies, collideWithAreas)) continue;
            var center = Center(actor); var point = Closest(transform.Origin, motion, center); var overlap = radius + actor.Shape.Radius - point.DistanceTo(center);
            if (overlap < 0 || overlap <= depth) continue;
            depth = overlap; var normal = (point - center).Normalized(); if (normal == Vector2.Zero) normal = Vector2.Up;
            hit = new(actor.RID, 0, center + normal * actor.Shape.Radius, normal,
                actor.Area ? Vector2.Zero : PhysicsServer.BodyGetDirectState(actor.RID)!.GetVelocityAtLocalPosition(normal * actor.Shape.Radius));
        }
        return hit;
    }
}
