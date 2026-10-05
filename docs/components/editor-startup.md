# Editor startup component

Last updated: 2026-10-05

The separate [editor executable](../../editor/Electron2D.Editor.csproj) starts Electron2D through its public API. [Program.cs](../../editor/Program.cs) loads the approved [dark stacked SVG](../design/assets/sprite/logo-stacked-dark.svg) from the executable's `Assets/` directory, limits the loop to 60 FPS and passes the root from [EditorScene](../classes/EditorScene.md) to Engine.Run.

The client area initially measures 1152×800, matching the supplied reference window excluding system decorations. Its title is `Electron2D`. A TextureRect displays the complete supplied logo at its rasterized intrinsic size on the identity's `#241B2C` background. A gray 32 px Label reads `Game engine` below the wordmark. Center anchors move the entire logo/descriptor block together as the client resizes; the supplied logo keeps its proportions. Both controls ignore mouse input. Window decorations and closing use the ordinary native host behavior.

The editor borrows the logo texture for the window's lifetime and disposes it after Engine.Run has completed scene/native cleanup. Asset loading happens before opening a window; a missing or malformed asset fails explicitly. The project links the existing design asset into build and publish outputs, without maintaining another logo copy. No custom renderer or desktop UI dependency is added.

## Launch and verification

Open the repository root in VS Code and press F5. [launch.json](../../.vscode/launch.json) selects `Electron2D Editor`; its [prelaunch task](../../.vscode/tasks.json) builds the Debug editor and launches `editor/bin/Debug/net10.0/Electron2D.Editor.dll`. The Microsoft C# extension is required. The task selects the existing native source-build mode because the current development native packages are unpublished; see [native prerequisites](../native-packaging.md). It requires the existing CMake, Ninja and C/C++ toolchain.

Terminal launch:

```bash
dotnet run --project editor/Electron2D.Editor.csproj -p:Electron2DBuildNativeFromSource=true
```

[EditorSceneTests](../../tests/Electron2D.Tests/EditorSceneTests.cs) compiles the same scene source into the test consumer and exercises real Engine.Run hosts. It checks native title/client size, rendered background/mark/face pixels, center anchors after native resize, native close delivery, zero exit status and cleanup without disposing the borrowed texture. Internal readback stays in tests; it is not an editor capture API.

```bash
env -u LD_LIBRARY_PATH ELECTRON2D_TEST_EDITOR=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -p:Electron2DBuildNativeFromSource=true
```

Linux x64/Wayland checks passed on 2026-10-05 for GPU and hardware compatibility, including four initial/resized PNG captures in ignored `bin/editor-smoke/`. The initial GPU capture was visually inspected; both initial captures contain rendered descriptor pixels. Debug and self-contained Release editor processes also opened from `/tmp` with `LD_LIBRARY_PATH` unset, proving executable-relative asset lookup and native title/client geometry. These test-owned startup processes were stopped by SIGTERM; graceful native close and cleanup are verified by the separate scene tests. Removing the Debug output logo produced FileNotFoundException and a nonzero exit before native window creation; the asset was restored.

The Debug build used by the F5 task, Release runtime build, scoped editor/test formatting, coverage snapshot, wiki generator tests and generation/check passed. The C# extension/debugger and launch/task linkage were checked locally. Physical F5 input in VS Code, human acceptance, foreign desktop execution, complete package/license auditing and public publication remain unverified. This component introduces no project editing or batch tooling.
