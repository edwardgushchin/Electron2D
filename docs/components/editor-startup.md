# Editor startup component

Last updated: 2026-10-05

The separate [editor executable](../../editor/Electron2D.Editor.csproj) starts Electron2D through its public API. [Program.cs](../../editor/Program.cs) loads the canonical [dark mark](../design/assets/sprite/mark-dark.svg), [stacked SVG](../design/assets/sprite/logo-stacked-dark.svg) and bundled IBM Plex Sans Regular from the executable's `Assets/` directory. An AtlasTexture views the original outlined name at `(24, 160, 440, 68)`. The entry point limits the loop to 60 FPS and passes the root from [EditorScene](../classes/EditorScene.md) to Engine.Run.

The initial client area is 1152×800 and its title is `Electron2D`. The background is `#241B2C`. A 144×144 visible pixel mark sits above the original wordmark and the gray 32 px caption `Agent-native cross-platform game engine`. The caption fits on one line. Center anchors position the 800×334 block; root transform snapping preserves whole-pixel translation at odd client dimensions. The mark uses nearest filtering at 1.5× its 128 px canvas, retaining 12 px modules. The wordmark keeps its existing antialiasing. All three controls ignore mouse input. The rejected white-outline asset and selector have been removed.

Textures, atlas view and font remain caller-owned until Engine.Run completes scene/native cleanup. A missing asset fails before opening a window. The Ready handler selects the clear color and supplies a temporary image copy of the mark to the existing public DisplayServer.SetIcon when Feature.Icon is available. This covers Windows, macOS and X11; Wayland's desktop association supplies its icon without requiring the optional compositor icon protocol. No runtime API or backend access is added.

The bundled [IBM Plex Sans Regular TTF](../../editor/Assets/IBMPlexSans-Regular.ttf) is version 3.005, weight 400, from [IBM/plex revision 763c36ef9117782905ae010056dfbe8fd2653a25](https://github.com/IBM/plex/blob/763c36ef9117782905ae010056dfbe8fd2653a25/packages/plex-sans/fonts/complete/ttf/IBMPlexSans-Regular.ttf). SHA-256: `975dcda37d80f038dcd143c22e33ca2d97a0cc5a929aace1c749153b0fe1afa5`. Its [SIL OFL 1.1](../../editor/Assets/IBMPlexSans-OFL.txt) is copied into build/publish output; no system font installation is required.

## Desktop identity and launch

All desktop entry points use the editor's own apphost. Launching the DLL through `dotnet` is not the supported desktop launch path. The Windows assembly/product metadata names Electron2D, and OutputType=WinExe avoids a console window. Platform identity assets contain only the canonical pink mark, with no lettering or white stroke.

| Platform | Integration |
| --- | --- |
| Linux, X11 and Wayland | Before native startup, the entry point registers `applications/Electron2D.Editor.desktop` and `icons/hicolor/scalable/apps/Electron2D.Editor.svg` under an absolute XDG_DATA_HOME, otherwise `~/.local/share`. The Name is Electron2D; desktop filename and StartupWMClass match the apphost's native `Electron2D.Editor` ID. Exec uses POSIX env to execute the quoted apphost, with desktop/argument escaping and literal percent handling. A later launch updates the executable path after a move or rebuild. Registration errors fail explicitly. |
| Windows | [Electron2D.ico](../../editor/Assets/Electron2D.ico) is embedded into the managed module and Windows apphost; the native window also receives the mark through DisplayServer.SetIcon. AssemblyTitle and Product are Electron2D. |
| macOS | Build/publish create `Electron2D.app` next to the flat output. Contents/MacOS holds the apphost, managed/native dependencies and Assets; Contents/Resources holds [Electron2D.icns](../../editor/Assets/Electron2D.icns). [Info.plist](../../editor/Assets/Info.plist) provides Electron2D name/display name, `org.electron2d.Editor` identifier, executable and icon. Launch the bundle or its contained apphost. |

ICO contains 16, 32, 48, 64, 128 and 256 px representations; ICNS contains 16 through 1024 px PNG representations. Each is exported from mark-dark.svg with nearest scaling to preserve the pixel grid. Build uses these committed assets without an image conversion dependency. macOS bundle copying preserves executable permissions and relative asset/native paths. Signing, notarization and public distribution are outside this startup slice.

Open the repository root in VS Code and press F5. [launch.json](../../.vscode/launch.json) selects `Electron2D Editor`; [its prelaunch task](../../.vscode/tasks.json) builds Debug using the native source-build mode required by the current unpublished development packages. The Microsoft C# extension and existing CMake/Ninja/C/C++ prerequisites are required. The debugger launches the Linux apphost, Windows EXE or macOS bundle apphost, with no outline argument.

```bash
dotnet run --project editor/Electron2D.Editor.csproj -p:Electron2DBuildNativeFromSource=true
```

For desktop publishing, choose the target RID and its native prerequisites from [native packaging](../native-packaging.md). The macOS bundle includes the target's normal publish contents; identity metadata alone does not supply missing target backends.

## Verification

[EditorSceneTests](../../tests/Electron2D.Tests/EditorSceneTests.cs) uses the actual scene source and real Engine.Run hosts. It checks title/client dimensions, sharp mark/face/name pixels, absence of the white border, complete caption fit and Regular font identity, center anchors after resize to 1281×901, native close delivery, zero exit and borrowed-resource cleanup. Readback remains internal to tests.

```bash
env -u LD_LIBRARY_PATH ELECTRON2D_TEST_EDITOR=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -p:Electron2DBuildNativeFromSource=true
python3 -B tools/editor/check_desktop.py editor/bin/Debug/net10.0/Electron2D.Editor
```

Linux x64/Wayland scene checks passed on GPU and hardware compatibility, with four PNG captures in ignored `bin/editor-smoke/`; the initial frame was visually inspected. The desktop check launches the production apphost and the registered launcher from an isolated directory containing spaces, Unicode, quotes, a dollar, backtick, percent and backslash. It checks the compositor's app ID/title, desktop validation and exact canonical icon bytes. The scene checks also passed on both backends through Linux X11/XWayland. A separate probe verified WM_CLASS=Electron2D.Editor, title Electron2D and a 128×128 native window icon. Debug and self-contained Linux entry points were exercised from `/tmp` with LD_LIBRARY_PATH unset. Test-owned startup processes are stopped by SIGTERM; graceful native close is verified separately by scene tests.

Debug/editor publish, Release runtime, scoped formatting, coverage, wiki generator tests/generation/check and the production desktop-launch checks passed. Coverage and formatting used the supported source-build property because development native packages are unpublished.

Windows x64 PE identity and macOS arm64 Mach-O/bundle metadata were inspected locally using the same editor project/sources with a prebuilt managed runtime reference in an ignored metadata probe. Windows GUI subsystem, embedded icon representations and product/title; macOS name/identifier/icon, executable permissions and managed/content copying are checked. These probes exclude foreign native backends and do not establish Windows/macOS launch or Dock/taskbar rendering. Foreign host execution remains pending under ADR 0021. GNOME Shell denied its private running-application introspection API; desktop identity and native IDs are checked, while a physical dock screenshot and physical F5 input remain unverified. This screen still provides no project/scene authoring, inspector or batch tooling.
