# Procedural disk meshes

A public Electron2D consumer generates a disk from radius and segment count through PrimitiveMesh. Arrow keys change the radius and tessellation; Escape closes the window. Optional secondary UVs use the shared generator controls. Ordinary frames reuse the generated mesh.

Run `dotnet run --project examples/PrimitiveMeshes/PrimitiveMeshes.csproj -c Release`. Add `-- --compatibility` to select the compatibility backend, or `-- --capture /absolute/output.png` to save a rendered frame and close. The output directory must exist. Native runtime prerequisites match the other examples.

DiskMesh includes stored descriptors, a typed factory and concrete parameter copying. Register its stable file type before loading an authored resource or scene. The executable tests use this same generator for duplication, scene-local resources and fresh-process archives, then verify actual native mesh output. Generation and archive work may allocate; prepared replay is measured separately.
