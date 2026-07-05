<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="data/assets/branding/readme/electron2d_readme_dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="data/assets/branding/readme/electron2d_readme_light.svg">
    <img alt="Electron2D" src="data/assets/branding/readme/electron2d_readme_light.svg" width="900">
  </picture>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/graphs/contributors"><img alt="Contributors" src="https://img.shields.io/github/contributors/edwardgushchin/Electron2D"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="Last commit" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D"></a>
  <a href="LICENSE"><img alt="MIT license" src="https://img.shields.io/badge/license-MIT-green"></a>
</p>

<p align="center">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4">
  <img alt="C# 14" src="https://img.shields.io/badge/C%23-14-239120">
  <img alt="Version 0.1-preview" src="https://img.shields.io/badge/version-0.1--preview-blue">
</p>

<p align="center">
  <a href="#about">About</a> ·
  <a href="#features">Features</a> ·
  <a href="#platforms">Platforms</a> ·
  <a href="#installation">Installation</a> ·
  <a href="#quick-start">Quick Start</a> ·
  <a href="#documentation">Documentation</a> ·
  <a href="#examples">Examples</a> ·
  <a href="#feedback-and-contributing">Feedback</a> ·
  <a href="#license">License</a>
</p>

<p align="center">
  ⭐ <a href="https://github.com/edwardgushchin/Electron2D">Star us on GitHub</a> - it motivates us a lot!
</p>

<a id="about"></a>

## 🧭 About

Electron2D is an agent-native, cross-platform 2D game engine for .NET.

Developers and local coding agents work on the same scenes, scripts, resources, diagnostics and undo history through the editor.

<a id="features"></a>

## ✨ Features

- **Agent-native workflow** - Local coding agents can inspect and edit scenes, scripts, resources and project settings through the same project model as the Editor. Their changes, diagnostics and undo history remain visible and reversible.
- **Trello-style task board** - Coordinate work through shared task columns, cards, assignees, labels, review states and editor-visible project context.
- **Built-in editor** - Scene tree, inspector, 2D viewport, script workspace, debugger and run/output tools for everyday game development.
- **C# scripting** - Game logic is written as regular C# classes and runs inside the same .NET project as the rest of the game.
- **Node-based scenes** - Scenes are built from reusable nodes, resources, signals and serializable project files.
- **2D rendering** - Sprites, cameras, viewports, text, shaders and immediate drawing.
- **2D physics** - Bodies, areas, collision shapes, raycasts and fixed-step simulation.
- **Asset workflow** - Textures, fonts, audio, shaders and resource importing.
- **Cross-platform runtime** - Build and run games on Windows, Linux, macOS and Android. iOS and Web are planned as future runtime targets.

<a id="platforms"></a>

## 🖥️ Platforms

| Platform | Editor | Runtime |
| --- | --- | --- |
| Windows | ✅ Done | ✅ Done |
| Linux | ✅ Done | ✅ Done |
| macOS | ✅ Done | ✅ Done |
| Android | ❌ Not planned | ✅ Done |
| iOS | ❌ Not planned | 🕓 Planned |
| Web | ❌ Not planned | 🕓 Planned |

<a id="installation"></a>

## 📦 Installation

Clone the repository and build the solution:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build src/Electron2D.sln -c Release
```

The runtime package version is `0.1-preview`.

<a id="quick-start"></a>

## 🚀 Quick Start

Run the editor:

```bash
dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -c Release
```

<a id="documentation"></a>

## 📚 Documentation

Documentation, guides and API reference are available in the [Electron2D GitHub Wiki](https://github.com/edwardgushchin/Electron2D/wiki).

<a id="examples"></a>

## 🎮 Examples

- **[Platformer](https://github.com/edwardgushchin/Electron2D/tree/main/examples/platformer)** - A 2D platformer example built with Electron2D.

<a id="feedback-and-contributing"></a>

## 💬 Feedback and Contributing

Use [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues) for bug reports, feature requests and design feedback. [Pull requests](https://github.com/edwardgushchin/Electron2D/pulls) are welcome once the related behavior is covered by documentation and tests. See [CONTRIBUTING.md](CONTRIBUTING.md) before opening a change.

Please follow the [Code of Conduct](CODE_OF_CONDUCT.md). Do not include secrets, signing keys, private data or credentials in issues, examples, screenshots or build artifacts.

<a id="contributors"></a>

## 👥 Contributors

Electron2D is maintained by Eduard Gushchin. See the [contributors graph](https://github.com/edwardgushchin/Electron2D/graphs/contributors) for repository contributors.

<a id="license"></a>

## 📄 License

Electron2D is distributed under the [MIT License](LICENSE).
