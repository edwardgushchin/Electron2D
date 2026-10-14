namespace Electron2D;

/// <summary>Retains one portable one-way episode by local logical collider and piece identities.</summary>
/// <param name="A">A in the owning operation's engine-unit contract.</param>
/// <param name="ShapeA">Shape A in the owning operation's engine-unit contract.</param>
/// <param name="PieceA">Piece A in the owning operation's engine-unit contract.</param>
/// <param name="B">B in the owning operation's engine-unit contract.</param>
/// <param name="ShapeB">Shape B in the owning operation's engine-unit contract.</param>
/// <param name="PieceB">Piece B in the owning operation's engine-unit contract.</param>
/// <param name="Allowed">Allowed in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsBackendOneWay(RID A, int ShapeA, int PieceA, RID B, int ShapeB, int PieceB, bool Allowed);

/// <summary>Supplies one shape-query contact without a solver handle.</summary>
/// <param name="RID">RID in the owning operation's engine-unit contract.</param>
/// <param name="ShapeIndex">Shape Index in the owning operation's engine-unit contract.</param>
/// <param name="Piece">Piece in the owning operation's engine-unit contract.</param>
/// <param name="QueryPoint">Query Point in the owning operation's engine-unit contract.</param>
/// <param name="ColliderPoint">Collider Point in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsBackendShapeContact(RID RID, int ShapeIndex, int Piece, Vector2 QueryPoint, Vector2 ColliderPoint);

/// <summary>Supplies an Area's logical remote/local overlap pair.</summary>
/// <param name="Other">Other in the owning operation's engine-unit contract.</param>
/// <param name="RemoteShape">Remote Shape in the owning operation's engine-unit contract.</param>
/// <param name="LocalShape">Local Shape in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsBackendOverlap(RID Other, int RemoteShape, int LocalShape);

/// <summary>Returns completed active-body, collision-pair and island counts.</summary>
/// <param name="Active">Nonnegative awake nonstatic body count, including kinematic bodies and excluding Areas.</param>
/// <param name="Pairs">Nonnegative backend collision candidate count, including sensor pairs rather than contact points or events.</param>
/// <param name="Islands">Nonnegative active dynamic constraint-group count containing contacts or joints, at most <paramref name="Active"/>.
/// Static supports and Areas do not join otherwise independent dynamic groups; isolated unconstrained bodies do not create islands.</param>
public readonly record struct PhysicsBackendStatistics(int Active, int Pairs, int Islands);
