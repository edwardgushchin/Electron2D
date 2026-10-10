using Box2D.NET;

namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal void CaptureViewContacts(PhysicsDirectBodyState view, int limit) => Attached.CaptureViewContacts(view, limit);
    internal void CaptureViewContact(PhysicsDirectBodyState view, B2Vec2 normal, B2Vec2 point, float separation,
        float depth, B2Vec2 impulse, B2BodyId collider, bool first, PhysicsFixtureTag own, PhysicsFixtureTag other, int limit) =>
        CPU.CaptureViewContact(view, normal, point, separation, depth, impulse, collider, first, own, other, limit);
}
