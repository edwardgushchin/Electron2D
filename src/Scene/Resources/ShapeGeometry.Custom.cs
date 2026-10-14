namespace Electron2D;

/// <summary>Calculates independently swept shape contacts using immutable geometry and engine-unit values.</summary>
/// <param name="first">The custom geometry owning this algorithm.</param>
/// <param name="firstPose">Its rigid global pose.</param>
/// <param name="firstMotion">Its independent global displacement.</param>
/// <param name="second">The other immutable geometry.</param>
/// <param name="secondPose">The other rigid global pose.</param>
/// <param name="secondMotion">The other independent global displacement.</param>
/// <param name="contacts">Borrowed even-length storage for alternating first/second boundary points.</param>
/// <param name="count">Number of complete pairs written; zero is permitted for a boolean-only query.</param>
/// <returns>Whether the two represented regions collide.</returns>
public delegate bool ShapeCollisionAlgorithm(ShapeGeometry first, Transform firstPose, Vector2 firstMotion,
    ShapeGeometry second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count);

public sealed partial class ShapeGeometry
{
    private string? _customFormatID;
    private byte[] _customData = [];
    private Vector2[] _customDrawing = [];
    private PhysicsMassProperties _customMass;
    private ShapeCollisionAlgorithm? _customCollision;
    [ThreadStatic] private static PhysicsShapePairBuffers? s_customBuffers;
    [ThreadStatic] private static HashSet<(ShapeGeometry First, ShapeGeometry Second)>? s_customCalls;

    /// <summary>Creates immutable geometry interpreted by a caller-defined collision implementation.</summary>
    /// <param name="formatID">Stable nonempty format/version identity, at most 256 characters.</param>
    /// <param name="canonicalData">Copied shape-specific canonical bytes, at most one MiB. Authors define and validate their encoding.</param>
    /// <param name="bounds">Finite local bounds with nonnegative size and representable endpoints.</param>
    /// <param name="massProperties">Exact unit-density mass, polar inertia about the local center, and local center in scene units.</param>
    /// <param name="drawingSegments">Copied paired finite diagnostic line endpoints. These do not define collision geometry.</param>
    /// <param name="collision">Exact standalone shape-pair algorithm. Its object is retained locally and never serialized.</param>
    /// <returns>Independent immutable data retained across resource edits and disposal.</returns>
    /// <exception cref="ArgumentException">Identity, bounds, mass or drawing data is invalid.</exception>
    /// <exception cref="ArgumentNullException">Identity or collision algorithm is null.</exception>
    /// <remarks>World implementations consume the same format and canonical data for their own solving and queries.
    /// Built-in physics rejects this geometry before mutation. Callbacks must preserve geometry and resource lifetime;
    /// Participating Shape resources remain borrowed through callback completion; geometry/policy edits, reset/copy and disposal reject.
    /// Recursive evaluation of the same pair rejects. Portable compatibility hashes data rather than callback identity.</remarks>
    public static ShapeGeometry Custom(string formatID, ReadOnlySpan<byte> canonicalData, Rect2 bounds,
        PhysicsMassProperties massProperties, ReadOnlySpan<Vector2> drawingSegments, ShapeCollisionAlgorithm collision)
    {
        ArgumentNullException.ThrowIfNull(formatID); ArgumentNullException.ThrowIfNull(collision);
        if (string.IsNullOrWhiteSpace(formatID) || formatID.Length > 256 || canonicalData.Length > 1024 * 1024)
            throw new ArgumentException("Custom geometry requires a bounded format identity and canonical payload.");
        if (!bounds.Position.IsFinite() || !bounds.Size.IsFinite() || bounds.Size.X < 0 || bounds.Size.Y < 0 || !(bounds.Position + bounds.Size).IsFinite())
            throw new ArgumentException("Custom geometry bounds must be finite and ordered.", nameof(bounds));
        PhysicsMass.Validate(massProperties.Mass, massProperties.Inertia, massProperties.Center);
        if (!float.IsFinite(massProperties.Inertia / massProperties.Mass)) throw new ArgumentException("Custom central inertia per unit mass must be finite.", nameof(massProperties));
        if ((drawingSegments.Length & 1) != 0) throw new ArgumentException("Drawing requires paired line endpoints.", nameof(drawingSegments));
        foreach (var point in drawingSegments)
            if (!point.IsFinite()) throw new ArgumentException("Drawing coordinates must be finite.", nameof(drawingSegments));
        return new(PhysicsShapeGeometry.ShapeKind.Custom, bounds)
        {
            _customFormatID = formatID,
            _customData = canonicalData.ToArray(),
            _customDrawing = drawingSegments.ToArray(),
            _customMass = massProperties,
            _customCollision = collision
        };
    }
    /// <summary>Gets the custom shape-specific format and version identity.</summary>
    /// <returns>The immutable format identity.</returns>
    /// <exception cref="InvalidOperationException">This is built-in geometry.</exception>
    public string GetCustomFormatID() { Require(PhysicsShapeGeometry.ShapeKind.Custom); return _customFormatID!; }
    /// <summary>Borrows immutable copied custom canonical data without allocation.</summary>
    /// <returns>Shape-specific bytes valid independently of resource lifetime.</returns>
    /// <exception cref="InvalidOperationException">This is built-in geometry.</exception>
    public ReadOnlySpan<byte> GetCustomData() { Require(PhysicsShapeGeometry.ShapeKind.Custom); return _customData; }
    /// <summary>Gets exact custom unit-density mass, central polar inertia and local center.</summary>
    /// <returns>The immutable scene-unit mass profile.</returns>
    /// <exception cref="InvalidOperationException">This is built-in geometry.</exception>
    public PhysicsMassProperties GetCustomMassProperties() { Require(PhysicsShapeGeometry.ShapeKind.Custom); return _customMass; }
    /// <summary>Borrows copied paired custom diagnostic segment endpoints.</summary>
    /// <returns>Finite local line endpoints; collision geometry remains defined by the algorithm and data.</returns>
    /// <exception cref="InvalidOperationException">This is built-in geometry.</exception>
    public ReadOnlySpan<Vector2> GetCustomDrawingSegments() { Require(PhysicsShapeGeometry.ShapeKind.Custom); return _customDrawing; }

    internal static bool CollideCustom(ShapeGeometry first, Transform firstPose, Vector2 firstMotion,
        ShapeGeometry second, Transform secondPose, Vector2 secondMotion, Span<Vector2> output, out int count)
    {
        var reverse = first.Type != PhysicsServer.ShapeType.Custom;
        var calls = s_customCalls ??= [];
        var key = reverse ? (second, first) : (first, second);
        if (calls.Contains((key.Item2, key.Item1)) || !calls.Add(key)) throw new InvalidOperationException("Custom shape collision cannot recursively evaluate the same pair.");
        var buffers = s_customBuffers ??= new(); var scratch = buffers.Begin(output.Length);
        try
        {
            var hit = reverse ? second._customCollision!(second, secondPose, secondMotion, first, firstPose, firstMotion, scratch, out count) :
                first._customCollision!(first, firstPose, firstMotion, second, secondPose, secondMotion, scratch, out count);
            PhysicsShapePairBuffers.Commit(hit, count, scratch, scratch);
            if (reverse) for (var i = 0; i < count * 2; i += 2) (scratch[i], scratch[i + 1]) = (scratch[i + 1], scratch[i]);
            scratch[..(count * 2)].CopyTo(output); return hit;
        }
        finally { buffers.End(); calls.Remove(key); }
    }
}
