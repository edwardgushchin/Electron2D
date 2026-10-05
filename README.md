<p align="right">English · <a href="README.ru.md">Русский</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a></p>

<h1 align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D" src="docs/design/assets/sprite/logo-primary-light.svg" width="640">
  </picture>
</h1>

<p align="center">
  <a href="#installation"><img alt="Required .NET version" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2FElectron2D.csproj&amp;query=substring-after%28%2FProject%2FPropertyGroup%2FTargetFramework%5Bnot%28%40Condition%29%5D%5B1%5D%2C+%27net%27%29&amp;label=.NET&amp;suffix=+SDK&amp;color=A63B75" height="28"></a>
  <a href="#license"><img alt="Engine license" src="https://img.shields.io/badge/dynamic/regex?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2Flicence%2FElectron2D-LICENSE.txt&amp;search=%5E%5Cs%2A%28%5CS%2B%29%5Cs%2BLicense&amp;replace=%241&amp;label=License&amp;color=A63B75" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="Latest published release" src="https://img.shields.io/github/v/release/edwardgushchin/Electron2D?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Release&amp;color=A63B75" height="28"></a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="Last commit on main" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Last+commit&amp;display_timestamp=committer&amp;color=A63B75" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/build.yml"><img alt="Automated build status" src="https://img.shields.io/github/actions/workflow/status/edwardgushchin/Electron2D/build.yml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Build&amp;branch=main" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/tests.yml"><img alt="Tests (GitHub Actions)" src="https://img.shields.io/github/actions/workflow/status/edwardgushchin/Electron2D/tests.yml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=Tests&amp;branch=main" height="28"></a>
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

## 🧭 About

Electron2D is a **free and open-source cross-platform 2D engine written in C# for developers and AI agents to build games together**.

Create game worlds and mechanics with familiar .NET tools and AI assistants such as Codex or Claude Code.

<a id="features"></a>

## ✨ Features

- [Graphics](docs/domains/rendering.md). Sprites and atlases, cameras, parallax, shape and text drawing. HLSL and GLSL shader import for materials.
- [Scenes and animation](docs/domains/scene.md). Reusable objects and levels, frame animation, property animation and timers.
- [Physics](docs/domains/physics.md). Rigid bodies, collisions, areas, intersection queries, hinges and springs.
- [Game UI](docs/domains/scene.md). Buttons, text fields, scrolling, layout containers, fonts and themes.
- [Audio](docs/domains/audio.md). WAV, MP3 and Ogg Vorbis, positional audio, mixing, effects and recording.
- [Input](docs/domains/input.md). Keyboard, mouse, touch and controllers. Map input to game actions.
- [Pathfinding](docs/domains/navigation.md). Routes on a grid or between specified points, accounting for obstacles and movement costs.
- [Resources](docs/domains/resources.md). Image, font and audio loading. Gradients, curves and procedural textures.
- [Localization](docs/domains/localization.md). Translations, plural forms and language selection.
- [Networking](docs/domains/networking.md). TCP, UDP and local sockets, TLS-encrypted connections.

Shader materials require the GPU renderer. The compatibility renderer supports basic 2D graphics. See the documentation linked above for details and limitations.

<a id="quick-start"></a>

## 🚀 Quick start

Start with the “Window and input” example. These commands are for Linux x64 with Wayland.

<a id="installation"></a>

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Git.

Private native components are restored as NuGet dependencies. See [native package delivery](docs/native-packaging.md) for the initial package availability and full native rebuild instructions.

### Build and run

Clone the repository and build the library:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Publish the example with its own .NET runtime and launch it:

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

A window opens. The arrow keys move a game object, and its coordinates appear in the terminal. Escape or closing the window exits the application. This example does not draw the scene; it shows engine startup and input handling.

[Example source](examples/HostExample/Program.cs) · [Run instructions](examples/HostExample/README.md)

### Use Electron2D in your game

Create a .NET 10 console project alongside the `Electron2D` directory and add a project reference to the engine. Run these commands from the repository root:

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
```

Replace the contents of `MyGame/Program.cs` with this code:

```csharp
using Electron2D;

var window = new Window
{
    Title = "My game",
    Size = new Vector2i(960, 540)
};

return Engine.Run(window);
```

Run the application:

```bash
dotnet run --project ../MyGame/MyGame.csproj -c Release
```

Add game objects to the window with `AddChild`. The example above shows frame updates and keyboard handling.

Building the engine produces `Electron2D.dll`. Publishing a game includes the engine library and native dependencies; a self-contained publish also includes the .NET runtime.

<a id="platforms"></a>

## 🖥️ Platforms

Game targets and completed checks are listed separately. The visual editor targets Windows, Linux and macOS.

| Game target | Checked in this repository |
| --- | --- |
| Windows, x86 / x64 / ARM64 | Execution has not been checked yet |
| Linux, x64 / ARM64 | On x64, windowing, input and rendering have been checked under Wayland. Rendering has also been checked under XWayland. ARM64 has not been checked yet |
| macOS, x64 / ARM64 | Execution has not been checked yet |
| Android, phones and tablets | Rendering, shader materials and physics have been checked on one ARM64 phone |
| Android TV | The compatibility renderer and physics have been checked on one 32-bit TV |
| iOS and tvOS, devices and simulators | Automated library builds have been prepared. Device checks have not been run yet |
| Browsers | Rendering and physics have been checked in a separate test application. Running a game in the browser is not implemented yet |

The full set of native text and audio libraries has been built for Linux x64. Building and integrating them for other platforms remains separate work. Android and browser checks cover individual scenarios.

The Build and Tests badges cover all 18 RIDs: analyzed library builds, the full Linux headless or portable desktop suite, and executable trimmed/AOT, Android, Apple-simulator and browser contract hosts. Physical Apple devices and complete foreign native runtime acceptance remain separate. See the [CI verification scope](docs/platform-verification.md#automated-rid-checks).

See the [platform report](docs/platform-verification.md) for device models, commands and verification limits.

<a id="development"></a>

## 🔧 Engine development

You can currently use Electron2D through C# and .NET. `PackedScene` templates support typed [resource and scene files](docs/components/resource-files.md), including loading in a new process. A visual editor and commands for managing game projects remain planned.

AI collaboration is part of the [engine architecture](docs/decisions/agent-native.md#adr-0090): project operations, game scenario execution and image verification must be available through documented tools. The full toolset is still to be implemented.

Next tasks and the status of individual methods are listed in the [development roadmap](docs/coverage/index.md). Implemented behavior is documented in the API reference.

<a id="documentation"></a>

## 📚 Documentation

| You want to | Read |
| --- | --- |
| Find a class or method | [API reference](docs/inventory.md) |
| Understand a subsystem | [Documentation index](docs/README.md) |
| Prepare shaders | [HLSL and GLSL import tool](tools/shaders/README.md) |
| Understand the architecture and decisions | [Architecture decisions](docs/decisions/index.md) |
| Choose a development task | [Development roadmap](docs/coverage/index.md) |

<a id="examples"></a>

Separate instructions are available for reproducing the [Android](tests/Electron2D.AndroidProbe/README.md) and [WebGPU](tests/Electron2D.WebGpuProbe/README.md) checks. The WebGPU test checks browser capabilities separately from the engine.

<a id="feedback-and-contributing"></a>

## 💬 Contributing

[Ask a question](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) · [Report an issue](https://github.com/edwardgushchin/Electron2D/issues/new/choose) · [Contribution guide](CONTRIBUTING.md) · [Support](SUPPORT.md) · [Code of conduct](CODE_OF_CONDUCT.md) · [Security policy](SECURITY.md)

Report bugs and suggest features in [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues). For a bug report, include the engine version or commit, operating system and renderer. Attach a minimal example and the error output.

Send fixes through [pull requests](https://github.com/edwardgushchin/Electron2D/pulls). Before starting, read the [maintenance guide](docs/maintaining.md) and architecture decisions for your topic. Update code, tests and documentation together.

Run the main checks from the repository root:

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

The project is maintained by [Eduard Gushchin](https://github.com/edwardgushchin). All contributors are listed on the [GitHub contributors page](https://github.com/edwardgushchin/Electron2D/graphs/contributors).

<a id="license"></a>

## 📄 License

Electron2D is distributed under the [MIT license](licence/Electron2D-LICENSE.txt). You can use the engine in commercial games; retain the copyright notice and license text.

Dependency licenses are listed in the [third-party notices](licence/THIRD_PARTY_NOTICES.md). When distributing a game, include the license texts required by those dependencies from the `licence/` directory.
