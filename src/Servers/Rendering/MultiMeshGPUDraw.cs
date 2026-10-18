namespace Electron2D;

internal readonly record struct MultiMeshGPUDraw(MultiMeshGPUState? State, int Surface, int[]? Indices, int IndexCount, MeshLOD[]? LODs, Transform Canvas, Color Modulation, int Stride, int Capacity, int Visible, bool Colors, bool Custom, bool Indirect, float Fraction, MultiMesh.PhysicsInterpolationQuality Quality);
