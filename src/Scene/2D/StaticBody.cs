using Box2D.NET;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>A stationary collision body that constrains simulated dynamic bodies.</summary>
public sealed class StaticBody : PhysicsBody
{
    private static readonly PropertyDescriptor[] BodyProperties =
    [
        new PropertyDescriptor<StaticBody, PhysicsMaterial?>(nameof(PhysicsMaterialOverride), body => body.PhysicsMaterialOverride,
            (body, value) => body.PhysicsMaterialOverride = value, _ => null, stored: true)
    ];

    /// <summary>Creates a detached static body with no collision shapes.</summary>
    public StaticBody() { }

    /// <summary>Gets or sets a borrowed surface material for every child collision shape.</summary>
    /// <value>Null by default, which uses friction one and bounce zero.</value>
    /// <remarks>Property edits rebuild fixtures before the next physics step. The caller owns the material.</remarks>
    /// <exception cref="ObjectDisposedException">The assigned material or body has been disposed.</exception>
    public PhysicsMaterial? PhysicsMaterialOverride
    {
        get { ThrowIfDisposed(); return MaterialOverride; }
        set => SetMaterialOverride(value);
    }

    internal override bool MovesWithSimulation => false;

    internal override B2BodyDef CreateBodyDefinition()
    {
        var definition = b2DefaultBodyDef();
        definition.type = B2BodyType.b2_staticBody;
        return definition;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(BodyProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(StaticBody)
        ? CreateStaticBody : base.CreateSceneInstanceFactory();

    private static Node CreateStaticBody() => new StaticBody();
}
