<p align="right">English · <a href="README.ru.md">Русский</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a></p>

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
  <a href="#installation"><img alt="Required .NET version" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2FElectron2D.csproj&amp;query=substring-after%28%2FProject%2FPropertyGroup%2FTargetFramework%5Bnot%28%40Condition%29%5D%5B1%5D%2C+%27net%27%29&amp;label=.NET&amp;suffix=+SDK&amp;color=A63B75" height="28"></a>
  <a href="#license"><img alt="Engine license" src="https://img.shields.io/badge/dynamic/regex?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2Flicence%2FElectron2D-LICENSE.txt&amp;search=%5E%5Cs%2A%28%5CS%2B%29%5Cs%2BLicense&amp;replace=%241&amp;label=License&amp;color=A63B75" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="Latest published release" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fgithub.com%2Fedwardgushchin%2FElectron2D%2Freleases.atom&amp;query=concat%28substring-after%28string%28%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%5B1%5D%2F%2A%5Blocal-name%28%29%3D%22link%22%5D%2F%40href%29%5B1%5D%29%2C+%22%2Ftag%2F%22%29%2C+substring%28%22no+releases%22%2C+1+div+not%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%29%29%29&amp;label=Release&amp;color=A63B75" height="28"></a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="Last commit on main" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Last+commit&amp;display_timestamp=committer&amp;color=A63B75" height="28"></a>
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

- [Graphics](docs/domains/rendering.md). Sprites and atlases, cameras, parallax, shape and text drawing. HLSL and GLSL shader import for materials.
- [Scenes and animation](docs/domains/scene.md). Game object hierarchies, scene saving and loading, frame animation, property animation and timers.
- [Physics](docs/domains/physics.md). Rigid bodies, collisions, areas, intersection queries, hinges and springs.
- [Game UI](docs/domains/scene.md). Buttons, text fields, scrolling, layout containers, fonts and themes.
- [Audio](docs/domains/audio.md). WAV, MP3 and Ogg Vorbis, positional audio, mixing, effects and recording.
- [Input](docs/domains/input.md). Keyboard, mouse, touch and controllers. Map input to game actions.
- [Pathfinding](docs/domains/navigation.md). Routes on a grid or between specified points, accounting for obstacles and movement costs.
- [Resources](docs/domains/resources.md). Image, font and audio loading. Gradients, curves and procedural textures.
- [Localization](docs/domains/localization.md). Translations, language selection and plural forms with caller-supplied rules.
- [Networking](docs/domains/networking.md). TCP, UDP and local sockets, secure TLS and DTLS connections, HTTP/HTTPS, WebSocket and multiplayer connections through ENet.

Shader materials require the GPU renderer. The compatibility renderer supports basic 2D graphics. See the documentation linked above for details and limitations.

<a id="quick-start"></a>

## <img src="docs/design/assets/sprite/readme-quick-start.svg" width="24" height="28" align="absmiddle" alt=""> Quick start

Start with the “Character movement” example. You will see a character and move it with the arrow keys. The .NET commands below are used on Windows, Linux and macOS; see the [platform table](#platforms) for completed run checks.

<a id="installation"></a>

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Git is needed only to build from source.

Install the main `Electron2D` package and the package for your target platform through NuGet using the [commands below](#use-electron2d-in-your-game). NuGet restores native dependencies automatically. [Platform package version rules](docs/native-packaging.md).

Prerelease packages require `--prerelease`. Use matching engine and platform package versions. Current NuGet version: [`0.1.0-alpha`](https://www.nuget.org/packages/Electron2D/0.1.0-alpha).

### Build and run

To create your own project, follow [Create a game project](#use-electron2d-in-your-game). The commands below build and run the existing repository example.

To build from source, clone the repository and build the library:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Run the example:

```bash
dotnet run --project examples/CharacterMovement
```

A scene opens with a pink character on a grid. The arrow keys move it within the field; Escape or closing the window exits. Try changing the movement speed in `Player.cs` and run the example again.

![Electron2D first scene: a character on a grid with keyboard instructions](docs/images/character-movement.png)

[Example source](examples/CharacterMovement/CharacterMovementScene.cs) · [Run instructions](examples/CharacterMovement/README.md)

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

For several game targets, add each required platform package. Web, Android, iOS and tvOS need the corresponding .NET workloads and a separate application project for the chosen platform. Platform packages do not include ready-made application projects. The console example below is for Windows, Linux and macOS.

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

<a id="platforms"></a>

## <img src="docs/design/assets/sprite/readme-platforms.svg" width="24" height="28" align="absmiddle" alt=""> Platforms

The table lists what has been checked on each game target. The visual editor targets Windows, Linux and macOS.

| Game target | Checked in this repository |
| --- | --- |
| Windows, x86 / x64 / ARM64 | Full headless suites and available trimmed/AOT checks passed in CI. Rendering has not been checked |
| Linux, x64 / ARM64 | Full headless suites and trimmed/AOT checks passed on both architectures. On x64, windowing, input and rendering have been checked under Wayland, and rendering under XWayland. ARM64 rendering has not been checked |
| macOS, x64 / ARM64 | Full headless suites and trimmed/AOT checks passed in CI. Rendering has not been checked |
| Android, phones and tablets | Rendering, shader materials and physics have been checked on one ARM64 phone |
| Android TV | The compatibility renderer and physics have been checked on one 32-bit TV |
| iOS and tvOS, devices and simulators | Native contracts passed in all four simulator profiles. Device bundles build without signing; physical-device execution and rendering have not been checked |
| Browsers | Rendering and physics have been checked in a separate test application. Running a game in the browser is not implemented yet |

Android and browser checks currently cover individual scenarios. See [native library delivery](docs/native-packaging.md) for library availability on your target platform.

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
