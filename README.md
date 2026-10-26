<p align="right">English · <a href="README.ru.md">Русский</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a></p>

Native accessibility on Linux publishes scene names, roles, state, button/focus actions and numeric Range values through AT-SPI. Editable sliders, scroll bars and spin boxes accept native numeric requests; progress displays are read-only. Set `ProjectSettings.AccessibilitySupport` before opening a window; native table protocols and other platform adapters remain in progress. Native Text/EditableText reads and edits LineEdit/TextEdit/CodeEdit, including Unicode selections, shaped geometry and password masking. AT-SPI also exposes stored labels/descriptions/control links, radio groups, validation errors, active descendants and internal hyperlink anchors. Relations follow node paths and target lifetime.

<h1 align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D - Agent-native cross-platform 2D game engine" src="docs/design/assets/sprite/logo-primary-light.svg" width="640" height="148">
  </picture>
</h1>

<p align="center">
  <a href="#installation"><img alt="Required .NET version" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2FElectron2D.csproj&amp;query=substring-after%28%2FProject%2FPropertyGroup%2FTargetFramework%5Bnot%28%40Condition%29%5D%5B1%5D%2C+%27net%27%29&amp;label=.NET&amp;suffix=+SDK&amp;color=brightgreen" height="28"></a>
  <a href="#license"><img alt="Engine license" src="https://img.shields.io/badge/dynamic/regex?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2Flicence%2FElectron2D-LICENSE.txt&amp;search=%5E%5Cs%2A%28%5CS%2B%29%5Cs%2BLicense&amp;replace=%241&amp;label=License&amp;color=brightgreen" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="Latest published release" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fgithub.com%2Fedwardgushchin%2FElectron2D%2Freleases.atom&amp;query=concat%28substring-after%28string%28%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%5B1%5D%2F%2A%5Blocal-name%28%29%3D%22link%22%5D%2F%40href%29%5B1%5D%29%2C+%22%2Ftag%2F%22%29%2C+substring%28%22no+releases%22%2C+1+div+not%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%29%29%29&amp;label=Release&amp;color=brightgreen" height="28"></a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="Last commit on main" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Last+commit&amp;display_timestamp=committer&amp;color=brightgreen" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="Automated build status" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Build&amp;nameFilter=Build" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="Tests (GitHub Actions)" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Tests&amp;nameFilter=Tests" height="28"></a>
</p>

<p align="center">
  <a href="#quick-start">Get started</a> ·
  <a href="#features">Features</a> ·
  <a href="#platforms">Platforms</a> ·
  <a href="#documentation">Documentation</a> ·
  <a href="#feedback-and-contributing">Contribute</a>
</p>

<p align="center">⭐ <a href="https://github.com/edwardgushchin/Electron2D">Star us on GitHub</a> - it motivates us a lot!</p>

<a id="about"></a>

## <img src="docs/design/assets/sprite/readme-about.svg" width="24" height="28" align="absmiddle" alt=""> About

Electron2D is a **free and open-source cross-platform 2D engine written in C# for developers and AI agents to build games together**.

Create game worlds and mechanics with familiar .NET tools. Codex, Claude Code and other AI assistants can help with the code.

<a id="features"></a>

## <img src="docs/design/assets/sprite/readme-features.svg" width="24" height="28" align="absmiddle" alt=""> Features

This is an overview of the main features already implemented in the engine. Individual subsystem APIs are still evolving; the linked documentation describes their scope and limitations.

- [Graphics](docs/domains/rendering.md). Sprites and atlases, cameras and parallax, shapes and text, clipping, offscreen viewports, lighting, shadows and HDR. HLSL/GLSL materials, visual shader graphs and CPU/GPU particles.
- [Scenes](docs/domains/scene.md). Node hierarchies, reusable and linked scenes, groups, events, deferred calls and timers. Create scenes in C#, save them with their resources and instantiate them again.
- [Animation](docs/components/scene-animation.md). Frame and property tracks, Bézier curves, tweens, blending graphs and state machines. [Skeletons and IK](docs/components/skeletal-animation.md) animate bones and deform weighted polygons and meshes.
- [Meshes](docs/components/meshes.md) and [tile maps](docs/components/tiles.md). Indexed and procedural geometry, instanced meshes, animated tile atlases, terrain connections and square, isometric or hexagonal grids with collision and navigation layers.
- [Physics](docs/domains/physics.md). CPU and independent GPU backends, rigid and character bodies, areas, forces, joints and springs. Ray/shape queries, continuous collision detection and [state checkpoints](docs/components/physics-space-checkpoints.md).
- [Game UI](docs/domains/scene.md) and [text](docs/components/text.md). Buttons, menus, lists, trees, layout containers, scrolling, text/code editors and rich text. Themes, Unicode shaping, bidirectional layout and font fallbacks.
- [Audio](docs/domains/audio.md). WAV, MP3 and Ogg Vorbis, procedural streams, microphone input and positional playback. Audio buses, mixing, effects and recording.
- [Video playback](docs/components/video-playback.md) and [movie recording](docs/components/movie-writing.md). Ogg Theora video with audio; offline capture of rendered frames and mixed audio to PNG/WAV, AVI or OGV.
- [Input](docs/domains/input.md). Keyboard, mouse, touch, controllers, action mappings, shortcuts and text input with IME composition.
- [Windows and displays](docs/domains/display.md). Window and fullscreen modes, multiple viewports, embedded and native popup windows, display queries, scaling and presentation controls.
- [Navigation](docs/domains/navigation.md). A-star point/grid searches, polygon regions, links, navigation baking, agent path following and reciprocal obstacle avoidance.
- [Resources](docs/domains/resources.md). Typed loading/saving, background loading, images, fonts, audio, gradients, curves and noise textures. [PCK/ZIP packs](docs/components/resource-packs.md) mount assets into the resource filesystem. [Missing material recovery](docs/components/material-placeholders.md) preserves scene resource data for later restoration.
- [Core services and files](docs/domains/core.md). Project settings, JSON/XML and configuration files, directory operations, compression, encryption and ZIP archives. Math, random generation, worker tasks and runtime diagnostics.
- [Localization](docs/domains/localization.md). Translation domains and contexts, PO/MO catalogs, locale fallback, integer plural rules, pseudolocalization and localized resource selection.
- [Networking](docs/domains/networking.md). TCP/UDP/local sockets, TLS/DTLS, HTTP/HTTPS, WebSocket and ENet. [WebRTC data channels](docs/components/webrtc.md), typed RPC, authority, scene spawning and [property replication](docs/components/scene-replication.md).
- [Shared project workflow](docs/components/project-jobs.md). Create/open a source project, edit one shared unsaved scene and C# buffer, then build/run immutable snapshots and inspect real captures with stale attribution. Jobs currently require Linux; rendered capture requires a native window and renderer.
- [Agent in the editor](docs/components/project-agent.md). A native task/conversation panel connects the qualified local Codex CLI to shared unsaved documents through authenticated MCP operations, with guarded history, cancellation, reconnect and real snapshot evidence. Current executor/transport qualification requires Linux and an existing CLI login.

Shader materials, GPU particles, lighting/shadows and HDR require the GPU renderer; the compatibility renderer supports its documented 2D profile. Physics backend selection is independent of renderer selection. See the linked subsystem documents and [platform report](docs/platform-verification.md) for exact support and verification limits.

<a id="agent-support"></a>

## Agent providers and clients

Support is scoped to tested client/version/platform profiles. See the [support matrix and limits](docs/components/project-agent.md#provider-and-client-support).

- **OpenAI / Codex:** qualified bounded editor executor, Codex CLI 0.163.0 on Linux. External CLI real-model acceptance remains separate.
- **Anthropic / Claude Code:** planned editor executor and operations/skill profile; not yet qualified.
- **Google / Gemini CLI:** planned editor executor and operations/skill profile; not yet qualified.
- **DeepSeek:** planned backend through a compatible executor; not yet qualified.
- **Moonshot AI / Kimi:** planned Kimi Code CLI editor/MCP/skill profile; not yet qualified.
- **Alibaba / Qwen:** planned Qwen Code editor/MCP/skill profile; not yet qualified.
- **Z.ai / GLM:** planned backend through a compatible executor; not yet qualified.
- **MiniMax / M-series:** planned backend through a compatible executor; not yet qualified.
- **GitHub / Copilot CLI:** planned external operations/skill client; not yet qualified.
- **Cursor / Cursor Agent:** planned external operations/skill client; not yet qualified.

A bundled development skill, bounded documentation context and the [editor Git workspace](docs/decisions/agent-native.md#git-workspace-in-the-editor-and-operation-host), including branches, commits, push and worktrees, are accepted architecture and remain unimplemented. Manual development and ordinary .NET game builds require no agent provider.

<a id="quick-start"></a>

## <img src="docs/design/assets/sprite/readme-quick-start.svg" width="24" height="28" align="absmiddle" alt=""> Quick start

Start with [CharacterMovement](examples/CharacterMovement/README.md). Move the character with arrow keys on a computer, drag it with your finger on a phone, or use the remote's direction buttons on a TV. The same example targets all engine platforms.

<a id="installation"></a>

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Git is needed only to build from source.

Install the main `Electron2D` package and the package for your target platform through NuGet using the [commands below](#use-electron2d-in-your-game). NuGet restores native dependencies automatically. [Platform package version rules](docs/native-packaging.md).

Prerelease packages require `--prerelease`. Use matching engine and platform package versions. Published versions are listed on [NuGet](https://www.nuget.org/packages/Electron2D).

### Build and run

To create your own project, follow [Create a game project](#use-electron2d-in-your-game). The commands below build and run the existing repository example.

To build from source, clone the repository and build the library:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Run the example on a computer:

```bash
dotnet run --project examples/CharacterMovement
```

A fixed 800 × 600 window opens with a pink character on a grid. Arrow keys move it within the field; Escape or closing the window exits. The window and character do not resize. Try changing the movement speed in `Player.cs` and run the example again.

For macOS, use the [application bundle](examples/CharacterMovement/README.md#macos). Instructions for [Android](examples/CharacterMovement/README.md#android-phone-or-tablet), [Android TV](examples/CharacterMovement/README.md#android-tv), [Web](examples/CharacterMovement/README.md#web), [iOS](examples/CharacterMovement/README.md#ios) and [Apple TV](examples/CharacterMovement/README.md#apple-tv-tvos) are in the example README.

![CharacterMovement: a character on a grid](docs/images/character-movement.png)

[Example source](examples/CharacterMovement/CharacterMovementScene.cs) · [Run instructions](examples/CharacterMovement/README.md)

[WaterPlayground](examples/WaterPlayground/README.md) is a resizable water playground: 65,536 particles, CPU/GPU liquid comparison, and a draggable duck and sailboat. It uses public compute, physics, input and rendering APIs with a soft pastel palette.

```bash
dotnet run --project examples/WaterPlayground -c Release
```

<a id="use-electron2d-in-your-game"></a>

### Create a game project

For a game targeting Windows, Linux or macOS, create a .NET 10 console project and add the engine package:

```bash
dotnet new console -n MyGame --framework net10.0
cd MyGame
dotnet add package Electron2D --prerelease
```

Alternatively, use the engine source. Run these commands from the repository root instead:

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
cd ../MyGame
```

Then install the package for your target platform:

| Platform | Command |
| --- | --- |
| Windows | `dotnet add package Electron2D.Windows --prerelease` |
| Linux | `dotnet add package Electron2D.Linux --prerelease` |
| macOS | `dotnet add package Electron2D.MacOS --prerelease` |
| Web | `dotnet add package Electron2D.Web --prerelease` |
| Android and Android TV | `dotnet add package Electron2D.Android --prerelease` |
| iOS | `dotnet add package Electron2D.iOS --prerelease` |
| Apple TV (tvOS) | `dotnet add package Electron2D.tvOS --prerelease` |

For several game targets, add each required platform package. Web, Android, iOS and tvOS need the corresponding .NET workloads and platform application entry points. Platform packages do not include ready-made application projects. The console example below is for Windows, Linux and macOS.

Replace the contents of `Program.cs` with this code:

```csharp
using Electron2D;

var window = new Window
{
    Title = "My game",
    Size = new Vector2i(800, 600)
};

return Engine.Run(window);
```

Run the application:

```bash
dotnet build -c Release
dotnet run -c Release
```

Add game objects to the window with `AddChild`. The example above shows frame updates and keyboard handling.

Building the engine produces `Electron2D.dll`. Publishing a game includes the engine library and native dependencies; a self-contained publish also includes the .NET runtime.

#### Phones, TVs and browsers

Use [CharacterMovement](examples/CharacterMovement/README.md#start-your-own-project) as the starting project. Copy its source and platform files to `examples/MyGame`, rename the project to `MyGame.csproj` and set your own application identifier. The example README covers copying, app icons and installation on a device.

Then choose a target:

| Target | Command from the repository root | Install and run |
| --- | --- | --- |
| Android, ARM64 phone or tablet | `dotnet build examples/MyGame/MyGame.csproj -c Release -r android-arm64` | [USB, APK and touch](examples/CharacterMovement/README.md#android-phone-or-tablet) |
| Android TV, 32-bit ARM | `dotnet build examples/MyGame/MyGame.csproj -c Release -r android-arm` | [Network ADB and remote](examples/CharacterMovement/README.md#android-tv) |
| Web | `dotnet publish examples/MyGame/MyGame.csproj -c Release -r browser-wasm -o bin/my-game/web` | [HTTP server and Chrome](examples/CharacterMovement/README.md#web) |
| iOS, simulator on an ARM64 Mac | `dotnet build examples/MyGame/MyGame.csproj -c Release -r iossimulator-arm64 -p:EnableCodeSigning=false` | [Simulator and signed app](examples/CharacterMovement/README.md#ios) |
| Apple TV, simulator on an ARM64 Mac | `dotnet build examples/MyGame/MyGame.csproj -c Release -r tvossimulator-arm64 -p:EnableCodeSigning=false` | [tvOS simulator and device](examples/CharacterMovement/README.md#apple-tv-tvos) |

Phones and TVs also use other architectures; the example README lists every option. iOS and tvOS builds require macOS and Xcode.

The app fills the screen on phones and TVs. The browser has a fullscreen button. The field adapts to the aspect ratio, while the character, text and grid scale together.

<a id="platforms"></a>

## <img src="docs/design/assets/sprite/readme-platforms.svg" width="24" height="28" align="absmiddle" alt=""> Platforms

The table lists what has been checked on each game target. The visual editor targets Windows, Linux and macOS.

| Game target | Checked in this repository |
| --- | --- |
| Windows, x86 / x64 / ARM64 | Full headless suites and available trimmed/AOT checks passed in CI. Rendering has not been checked |
| Linux, x64 / ARM64 | Full headless suites and trimmed/AOT checks passed on both architectures. On x64, windowing, input and rendering have been checked under Wayland, and rendering under XWayland. ARM64 rendering has not been checked |
| macOS, x64 / ARM64 | Full headless suites and trimmed/AOT checks passed in CI. Rendering has not been checked |
| Android, phones and tablets | CharacterMovement startup, fullscreen layout and touch dragging have been checked on one ARM64 phone. Shader materials and physics were checked separately |
| Android TV | CharacterMovement and remote direction buttons have been checked on one 32-bit TV. The compatibility renderer and physics were checked separately |
| iOS and tvOS, devices and simulators | Native contracts passed in all four simulator profiles. Device bundles build without signing; physical-device execution and rendering have not been checked |
| Browsers | CharacterMovement, adaptive layout and arrow keys have been checked in Chrome. Portrait layout and touch dragging were checked with phone emulation; a physical mobile browser has not been checked. Rendering and physics were checked separately |

The Android and browser checks cover the example on specific devices and in Chrome. See [native library delivery](docs/native-packaging.md) for availability on your target.

The Build and Tests badges show the status of automated code checks and test applications. Running on real devices is checked separately. [Automated check details](docs/platform-verification.md#automated-rid-checks).

See the [platform report](docs/platform-verification.md) for device models, commands and verification limits.

<a id="development"></a>

## <img src="docs/design/assets/sprite/readme-development.svg" width="24" height="28" align="absmiddle" alt=""> Project status

Electron2D is in alpha. Its public API is still evolving and may change between releases.

Create scenes in code, save them with `PackedScene` and reload them from [resource and scene files](docs/components/resource-files.md). The visual editor currently shows a startup screen; game project editing is not implemented yet.

<a id="documentation"></a>

## <img src="docs/design/assets/sprite/readme-documentation.svg" width="24" height="28" align="absmiddle" alt=""> Documentation

| You want to | Read |
| --- | --- |
| Find a class or method | [API reference](docs/inventory.md) |
| Understand a subsystem | [Documentation index](docs/README.md) |
| Prepare shaders | [HLSL and GLSL import tool](tools/shaders/README.md) |
| Build/run unsaved projects and inspect captures | [Shared project jobs](docs/components/project-jobs.md) |
| Prepare a release | [Release runbook](docs/releasing.md) |

<a id="examples"></a>

Separate instructions are available for reproducing the [Android](tests/Electron2D.AndroidProbe/README.md) and [WebGPU](tests/Electron2D.WebGpuProbe/README.md) checks. The WebGPU test checks browser capabilities separately from the engine.

<a id="feedback-and-contributing"></a>

## <img src="docs/design/assets/sprite/readme-contributing.svg" width="24" height="28" align="absmiddle" alt=""> Contributing

[Ask a question](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) · [Report an issue](https://github.com/edwardgushchin/Electron2D/issues/new/choose) · [Contribution guide](CONTRIBUTING.md) · [Support](SUPPORT.md) · [Code of conduct](CODE_OF_CONDUCT.md) · [Security policy](SECURITY.md)

Report bugs and suggest features in [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues). For a bug report, include the engine version or commit, operating system and renderer. Attach a minimal example and the error output.

Send fixes through [pull requests](https://github.com/edwardgushchin/Electron2D/pulls). See the [contribution guide](CONTRIBUTING.md) for the development workflow.

<a id="contributors"></a>

The project is maintained by [Eduard Gushchin](https://github.com/edwardgushchin). All contributors are listed on the [GitHub contributors page](https://github.com/edwardgushchin/Electron2D/graphs/contributors).

<a id="license"></a>

## <img src="docs/design/assets/sprite/readme-license.svg" width="24" height="28" align="absmiddle" alt=""> License

Electron2D is distributed under the [MIT license](licence/Electron2D-LICENSE.txt). You can use the engine in commercial games; retain the copyright notice and license text.

Dependency licenses are listed in the [third-party notices](licence/THIRD_PARTY_NOTICES.md). When distributing a game, include the license texts required by those dependencies from the `licence/` directory.
