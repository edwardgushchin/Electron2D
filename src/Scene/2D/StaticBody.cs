using Box2D.NET;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>A stationary collision body that constrains simulated dynamic bodies.</summary>
public sealed class StaticBody : PhysicsBody
{
    /// <summary>Creates a detached static body with no collision shapes.</summary>
    public StaticBody() { }

    internal override bool MovesWithSimulation => false;

    internal override B2BodyDef CreateBodyDefinition()
    {
        var definition = b2DefaultBodyDef();
        definition.type = B2BodyType.b2_staticBody;
        return definition;
    }

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(StaticBody)
        ? CreateStaticBody : base.CreateSceneInstanceFactory();

    private static Node CreateStaticBody() => new StaticBody();
}
