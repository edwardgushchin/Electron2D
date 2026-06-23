# Electron2D Reference Platformer

This is a valid `Electron2D.Editor` project used as the 0.1.0 Preview reference platformer.

Local verification from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ReferencePlatformer.ps1
```

The project intentionally keeps `.electron2d/tasks/**` as Editor metadata. Export and runtime packaging checks must exclude that metadata from production asset contents.
