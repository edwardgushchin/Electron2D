# Platformer

This is a valid `Electron2D.Editor` project used as the 0.1-preview Platformer.

Play from this directory or from the repository root:

```powershell
dotnet run --project ..\..\src\Electron2D.Cli\Electron2D.Cli.csproj -- run --project .
```

Controls: `A`/Left, `D`/Right, Space jump, `P` pause, `S` save, `Q` quit.

Automated playable script:

```powershell
dotnet run --project ..\..\src\Electron2D.Cli\Electron2D.Cli.csproj -- run --project . --play-script "right,jump,right,pause,save,quit"
```

Local verification from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-Platformer.ps1
```

The project intentionally keeps `.electron2d/tasks/**` as Editor metadata. Export and runtime packaging checks must exclude that metadata from production asset contents.
