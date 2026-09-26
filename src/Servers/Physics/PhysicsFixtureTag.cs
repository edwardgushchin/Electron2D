using Box2D.NET;

namespace Electron2D;

internal readonly record struct SeparationRayData(B2Vec2 From, B2Vec2 To, bool SlideOnSlope);
internal sealed record PhysicsFixtureTag(RID ColliderRID, int ShapeIndex, OneWayContactData? OneWay,
    SeparationRayData? SeparationRay = null);
