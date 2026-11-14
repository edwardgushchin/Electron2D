# Mesh topology consumer

Run from the repository root with the native assets appropriate for the host:

```sh
dotnet run --project tests/MeshTopology.Consumer -c Release -- managed
dotnet run --project tests/MeshTopology.Consumer -c Release -- gpu
dotnet run --project tests/MeshTopology.Consumer -c Release -- compatibility
```

The managed profile loads and duplicates an ArrayMesh archive, loads and
converts an ImporterMesh archive, and checks copied vertices and invalid
topology rejection. The renderer profiles also draw the five topologies
from a resource, an owned packed RID, and an owned array RID. They require
the requested backend and verify pixels from the completed viewport.

The two fixtures were saved through ResourceSaver using the preceding API
at revision `513affcbf752aa70b949256e132ef352bd4e3eef`. That writer iterated
the five values of the former nested enum, added the same geometry as
`Geometry` in the consumer, and saved both resources (AMS8 and IMP2).

| Fixture | SHA-256 |
| --- | --- |
| `legacy-array.e2dres` | `006823c4e9bc749500fb4f0fc95d5654597bac78ad5ce167182eba71fd7ee313` |
| `legacy-importer.e2dres` | `c8071aa6f90b0526cbbb725d42be60ec738e1c256626979477a44c0f2fdb4ac9` |
