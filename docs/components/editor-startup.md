# Editor startup component

Last updated: 2026-10-06

The separate [editor executable](../../editor/Electron2D.Editor.csproj) starts Electron2D through its public API. [Program.cs](../../editor/Program.cs) loads the current [character](../design/assets/sprite/mark-dark.svg), [open pixel sparkle](../design/assets/sprite/sparkle.svg), [app icon](../design/assets/sprite/app-icon.svg) and two bundled IBM Plex Sans fonts from Assets. It limits the loop to 60 FPS and passes [EditorScene](../classes/EditorScene.md)'s root to Engine.Run.

The client area is 1152×800, title Electron2D, background #241B2C. The compact composition follows the updated Penpot board 07: a shaded character with symmetric hollow ears, highlights, blush and open mouth, two sparkles, Electron2D and the descriptor `Agent-native cross-platform 2D game engine`. Character/sparkle textures use nearest filtering. The title consists of two real Label nodes in SemiBold at 82 px; Electron is paper-colored, 2D is pink. A third Label renders the descriptor in Regular at 16 px. Public font metrics center and align the text. The editor never loads the outlined communication logo or a screenshot of the lettering.

The fixed 640×320 composition with a 160×160 character canvas stays centered during resizing. Transform snapping preserves whole-pixel translation at odd client dimensions. All controls ignore pointer input. Textures and fonts remain caller-owned until Engine.Run completes; a missing asset fails before opening a window. The Ready handler supplies a temporary image copy to public DisplayServer.SetIcon where available. Wayland's desktop association supplies its icon independently of optional compositor icon support. No runtime API/backend access is added.

Bundled branding fonts are IBM Plex Sans 3.201, weights 600 and 400, the Latin files used by the Penpot export (Google Fonts ibmplexsans/v23). They supply the current English title/descriptor rather than a general editor localization font. Font outlines in communication SVGs are generated from these same files. The [SIL OFL 1.1](../../editor/Assets/IBMPlexSans-OFL.txt) accompanies build/publish output; installed system fonts are not required.

- `IBMPlexSans-SemiBold.ttf`: SHA-256 `6e0dcd02fd94e40c85974f328bb5a3521cfb81b59332559911fb08ff228bb176`.
- `IBMPlexSans-Regular.ttf`: SHA-256 `1fb42ec91b69b3545f15d7cfda148ae20bc2cb17aaed79eea9b7d3941cb71e4d`.

## Desktop identity and launch

All desktop entry points use the editor's own apphost. Launching the DLL through `dotnet` is not the supported desktop launch path. The Windows assembly/product metadata names Electron2D, and OutputType=WinExe avoids a console window. Platform identity assets use app-icon.svg: the shaded character on a paper-colored rounded square, without lettering.

| Platform | Integration |
| --- | --- |
| Linux, X11 and Wayland | Before native startup, the entry point registers `applications/Electron2D.Editor.desktop` and `icons/hicolor/512x512/apps/Electron2D.Editor.app-icon.png` under an absolute XDG_DATA_HOME, otherwise `~/.local/share`. Icon is the desktop-escaped absolute PNG path, loaded as a GIO FileIcon to bypass icon-theme lookup and stale SVG caches. The Name is Electron2D; desktop filename and StartupWMClass match the apphost's native `Electron2D.Editor` ID. Exec uses POSIX env to execute the quoted apphost, with desktop/argument escaping and literal percent handling. A later launch updates the executable path after a move or rebuild. Registration errors fail explicitly. |
| Windows | [Electron2D.ico](../../editor/Assets/Electron2D.ico) is embedded into the managed module and Windows apphost; the native window also receives the mark through DisplayServer.SetIcon. AssemblyTitle and Product are Electron2D. |
| macOS | Build/publish create `Electron2D.app` next to the flat output. Contents/MacOS holds the apphost, managed/native dependencies and Assets; Contents/Resources holds [Electron2D.icns](../../editor/Assets/Electron2D.icns). [Info.plist](../../editor/Assets/Info.plist) provides Electron2D name/display name, `org.electron2d.Editor` identifier, executable and icon. Launch the bundle or its contained apphost. |

ICO contains 16, 32, 48, 64, 128 and 256 px representations; ICNS contains 16 through 1024 px PNG representations. Each is rasterized from the current app-icon.svg at its target size, preserving its rounded background. Build uses these committed assets without an image conversion dependency. macOS bundle copying preserves executable permissions and relative asset/native paths. Signing, notarization and public distribution are outside this startup slice.

Open the repository root in VS Code and press F5. [launch.json](../../.vscode/launch.json) offers `Electron2D Editor` and `CharacterMovement`; [their prelaunch tasks](../../.vscode/tasks.json) build Debug with the SDK host RID and restored platform packages. The Microsoft C# extension is required; native compiler tools and a local NuGet feed are not. `AppendRuntimeIdentifierToOutputPath=false` keeps debugger paths stable across desktop architectures without changing RID asset selection. The debugger launches the native apphost, Windows EXE or macOS bundle apphost, with no outline argument.

```bash
dotnet run --project editor/Electron2D.Editor.csproj -p:AppendRuntimeIdentifierToOutputPath=false
```

For desktop publishing, choose the target RID and its native prerequisites from [native packaging](../native-packaging.md). The macOS bundle includes the target's normal publish contents; identity metadata alone does not supply missing target backends.

## Verification

[EditorSceneTests](../../tests/Electron2D.Tests/EditorSceneTests.cs) uses the actual source and real Engine.Run hosts. It checks the initial title/client area, live Label content and font identity, descriptor fit/rendering, character/highlight/name/sparkle pixels, center anchors after resize to 1281×901, native close delivery, zero exit and borrowed texture/font lifetime. Readback is internal to tests.

```bash
env -u LD_LIBRARY_PATH ELECTRON2D_TEST_EDITOR=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
python3 -B tools/editor/check_desktop.py editor/bin/Debug/net10.0/Electron2D.Editor
```

The refreshed scene passed Linux x64/Wayland GPU and compatibility checks with four PNG captures, including resize and native close. The Debug apphost/desktop launcher check passed with exact app-icon bytes and GTK/GIO loading of the absolute PNG at 48×48. Foreign host execution, physical F5 input and Windows/macOS taskbar/Dock appearance remain separate under ADR 0021. Existing launcher and bundle metadata remain in place; refreshed icon containers alone do not verify another OS. Signing, notarization and public distribution are outside this branding change. No project/scene authoring or batch tooling is supplied by this screen.
