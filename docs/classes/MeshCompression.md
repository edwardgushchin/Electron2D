# MeshCompression

Last updated: 2026-10-12

Internal sealed canonical attribute codec in [MeshCompression.cs](../../src/Scene/Resources/MeshCompression.cs), owned by [ArrayMesh](ArrayMesh.md) and its independent [MeshMorphPose](MeshMorphPose.md) output. Copies own UV/UV2/frame arrays and immutable UV scale; decoded caches remain private surface data shared by LOD views. Structural capture/copy may allocate. Four-byte UV and six-byte frame records use little endian UNORM16 and remain exact across partial edits, copies and AMS7 archives. Decode and partial update do not allocate. Frame synthesis uses the existing [MeshFrameCodec](MeshFrameCodec.md); angle extraction handles identity/pi singularities. No public API, native handle or renderer lifetime is introduced. See [the component](../components/meshes.md#compressed-attributes) for precision, malformed data handling and measured limits.
