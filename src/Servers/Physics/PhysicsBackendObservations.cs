namespace Electron2D;

/// <summary>Retains one portable one-way episode by local logical collider and piece identities.</summary>
internal readonly record struct PhysicsBackendOneWay(RID A, int ShapeA, int PieceA, RID B, int ShapeB, int PieceB, bool Allowed);

/// <summary>Supplies one shape-query contact without a solver handle.</summary>
internal readonly record struct PhysicsBackendShapeContact(RID RID, int ShapeIndex, int Piece, Vector2 QueryPoint, Vector2 ColliderPoint);

/// <summary>Supplies an Area's logical remote/local overlap pair.</summary>
internal readonly record struct PhysicsBackendOverlap(RID Other, int RemoteShape, int LocalShape);

/// <summary>Returns completed active-body, collision-pair and island counts.</summary>
internal readonly record struct PhysicsBackendStatistics(int Active, int Pairs, int Islands);
