# Editor startup component

Last updated: 2026-10-05

The separate [editor executable](../../editor/Electron2D.Editor.csproj) starts Electron2D through its public API. [Program.cs](../../editor/Program.cs) loads the canonical [dark mark](../design/assets/sprite/mark-dark.svg), [stacked SVG](../design/assets/sprite/logo-stacked-dark.svg) and bundled Regular font from the executable's `Assets/` directory. An AtlasTexture views the original outlined name at `(24, 160, 440, 68)` without rewriting the letters. The entry point limits the loop to 60 FPS and passes the root from [EditorScene](../classes/EditorScene.md) to Engine.Run.

The client area initially measures 1152×800, matching the supplied reference window excluding system decorations. Its title is `Electron2D`. The background is the identity's `#241B2C`. The user-approved startup composition places a 144×144 visible mark above the outlined name and a gray 32 px `Game engine` caption in IBM Plex Sans Regular. A 192×192 TextureRect scales the mark canvas by 1.5 with nearest filtering, preserving its square 12 px modules. The wordmark uses its existing antialiased raster with linear filtering. Center anchors place the 488×334 block, and root transform snapping removes half-pixel translation at odd client dimensions. The measured visible title/caption gap is approximately 16 px and the visible composition center is within two pixels of the client center. All three controls ignore mouse input; window closing follows the ordinary native host behavior.

The editor borrows the two image textures, atlas view and font for the window's lifetime; the entry point disposes them after Engine.Run has completed scene/native cleanup. Asset loading happens before opening a window; a missing or malformed asset fails explicitly. Build and publish outputs contain the two existing SVGs, the font and its license. The runtime assembly gains no dependency or rendering implementation.

The bundled [IBM Plex Sans Regular TTF](../../editor/Assets/IBMPlexSans-Regular.ttf) is version 3.005, weight 400, from [IBM/plex revision 763c36ef9117782905ae010056dfbe8fd2653a25](https://github.com/IBM/plex/blob/763c36ef9117782905ae010056dfbe8fd2653a25/packages/plex-sans/fonts/complete/ttf/IBMPlexSans-Regular.ttf). SHA-256: `975dcda37d80f038dcd143c22e33ca2d97a0cc5a929aace1c749153b0fe1afa5`. Its corresponding [SIL OFL 1.1](../../editor/Assets/IBMPlexSans-OFL.txt) is copied from that package into the same output directory, with line endings and trailing spaces normalized. The font is an editor-owned content asset; it requires no system font installation.

## White outline trial

The user requested a separate outline trial on 2026-10-05. `--outline` selects [mark-dark-outlined.svg](../../editor/Assets/mark-dark-outlined.svg); without it, the entry point still loads the canonical mark. The trial draws a white miter-joined stroke behind the original pink silhouette, giving a 3 px outward border at the current 1.5× scale. The pink core, face, placement, wordmark and caption retain their previous geometry and colors. The outer mark becomes 150×150; the core remains 144×144. This is an editor preview, not approval of a new identity master.

The current VS Code launch passes `--outline` so F5 displays the trial. For a terminal comparison, add `-- --outline` to the launch command below. Omitting that argument displays the baseline.

Native GPU and compatibility checks passed for the trial and baseline. Frame comparison found exactly 1,908 changed pixels per initial frame, all from background to pure white within `(501, 254)` through `(650, 403)`; the core, face and text pixels are unchanged. The 150×150 outer mark and 3 px edge remain crisp after odd resize. The initial trial frame was visually inspected. Debug and self-contained entry points opened with `--outline` from `/tmp`, with the exact trial SVG in their output. Temporarily removing that Debug asset produced an explicit startup failure for `--outline`, while the baseline still opened; the file was restored.

The shared checkout's coverage check encountered an unrelated in-progress networking page. Release runtime, coverage, wiki generator tests/generation/check and the outline native tests passed separately on a clean `edd2104b` snapshot plus only these preview changes, with existing Linux x64 native binaries supplied through the supported prebuilt-library properties. This verification excludes parallel physics/networking edits and physical F5 input.

## Launch and verification

Open the repository root in VS Code and press F5. [launch.json](../../.vscode/launch.json) selects `Electron2D Editor`; its [prelaunch task](../../.vscode/tasks.json) builds the Debug editor and launches `editor/bin/Debug/net10.0/Electron2D.Editor.dll`. The Microsoft C# extension is required. The task selects the existing native source-build mode because the current development native packages are unpublished; see [native prerequisites](../native-packaging.md). It requires the existing CMake, Ninja and C/C++ toolchain.

Terminal launch:

```bash
dotnet run --project editor/Electron2D.Editor.csproj -p:Electron2DBuildNativeFromSource=true
```

[EditorSceneTests](../../tests/Electron2D.Tests/EditorSceneTests.cs) compiles the same scene source into the test consumer and exercises real Engine.Run hosts. It checks native title/client size, a sharp mark/background boundary, face and outlined-name pixels, the explicit caption font, center anchors after native resize to odd dimensions, native close delivery, zero exit status and cleanup without disposing borrowed textures or font. Internal readback stays in tests; it is not an editor capture API.

Set `ELECTRON2D_TEST_EDITOR_OUTLINE=1` alongside the existing test selector to exercise the trial, including an adjacent white border pixel. Its four captures go to ignored `bin/editor-outline/`; the baseline still uses `bin/editor-smoke/`.

```bash
env -u LD_LIBRARY_PATH ELECTRON2D_TEST_EDITOR=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -p:Electron2DBuildNativeFromSource=true
```

Linux x64/Wayland checks passed on 2026-10-05 for GPU and hardware compatibility, including four initial/resized PNG captures in ignored `bin/editor-smoke/`. The initial GPU capture was visually inspected; both initial captures contain rendered descriptor pixels. Debug and self-contained Release editor processes also opened from `/tmp` with `LD_LIBRARY_PATH` unset, proving executable-relative asset lookup and native title/client geometry. These test-owned startup processes were stopped by SIGTERM; graceful native close and cleanup are verified by the separate scene tests. Removing the Debug output logo produced FileNotFoundException and a nonzero exit before native window creation; the asset was restored.

The refined composition passed those scene checks at 1152×800 and 1281×901 on both backends. Pixel measurements report a 144×144 mark, approximately 439×65 visible wordmark and 185×30 caption, with 15 empty pixel rows between name and caption. The initial visible vertical center is y=399 versus client center y=400; after odd resize it is y=450 versus y=450.5. Neighboring background/mark pixels verify a sharp edge, and a wordmark pixel verifies the atlas crop. Font identity and borrowed-resource cleanup are checked in the native host. Both production entry points passed startup with the refined assets from `/tmp`; Debug and publish copies match all source SVG/font/license bytes. Removing the Debug font produced FileNotFoundException before native window creation; the file was restored.

The Debug build used by the F5 task, Release runtime build, scoped editor/test formatting, coverage snapshot, wiki generator tests and generation/check passed. The C# extension/debugger and launch/task linkage were checked locally. Physical F5 input in VS Code, human acceptance, foreign desktop execution, complete package/license auditing and public publication remain unverified. This component introduces no project editing or batch tooling.
