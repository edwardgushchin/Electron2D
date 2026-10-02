<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D — Agent-native cross-platform 2D game engine" src="docs/design/assets/sprite/logo-primary-light.svg" width="900">
  </picture>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/graphs/contributors">Contributors</a> ·
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main">Commits</a> ·
  <a href="licence/Electron2D-LICENSE.txt">MIT license</a>
</p>

<p align="center">
  <img alt=".NET 10 · Typed C# · MIT · In development" src="docs/design/assets/sprite/readme-badges.svg" width="345">
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
  ⭐ <a href="https://github.com/edwardgushchin/Electron2D">Star the project on GitHub</a> to follow its development.
</p>

<a id="about"></a>

## 🧭 About

Electron2D is a typed C# engine for 2D games on .NET 10. Build scenes from nodes and resources, write game logic as regular C# classes, and use the engine's rendering, input, physics, audio and GUI APIs.

The runtime is built as **`Electron2D.dll`**. Games and the future editor consume the same public API; platform dependencies flow from the engine project into application publishes.

Electron2D is designed to be **agent-native**: human developers and coding agents should be able to create, inspect, modify, build, run and verify projects through documented programmatic operations. The [agent-native architecture](docs/decisions/agent-native.md#adr-0090) defines shared CLI/editor authoring, headless simulation and rendered batch verification.

**Development status:** the runtime, examples, tests and API documentation are actively developed. The visual editor, unified project CLI, scene-file persistence and public capture workflow are not implemented yet. Cross-platform targets and verified execution are listed separately below.

<a id="features"></a>

## ✨ Features

- **Typed C#** — Concrete types, properties, generics and C# events, with ordinary IDE completion and refactoring.
- **Node-based scenes** — `Node`, `CanvasItem` and `Entity` hierarchies, scene scheduling, timers, tweens and reusable in-memory `PackedScene` instances.
- **2D rendering** — Sprites, animation, cameras, textures, text, canvas drawing and typed HLSL/GLSL shader materials. See [rendering capabilities and limits](docs/domains/rendering.md).
- **GUI building blocks** — Controls, containers, labels, buttons, text input, focus navigation and typed themes. See the [scene domain](docs/domains/scene.md).
- **2D physics** — Bodies, areas, collision shapes, queries and joints, driven by fixed-step simulation. See the [physics domain](docs/domains/physics.md).
- **Audio** — WAV, MP3 and Ogg Vorbis playback, procedural streams, output buses and recording APIs. See [audio behavior and verification limits](docs/domains/audio.md).
- **Resources and I/O** — Images, fonts, resource loading, typed configuration, file access and localization. See the [resource domain](docs/domains/resources.md) and [core domain](docs/domains/core.md).
- **Agent-native architecture** — Programmatic authoring and observable verification are product requirements, with the missing tooling tracked in the [implementation roadmap](docs/coverage/index.md).
- **Cross-platform runtime targets** — Windows, Linux, macOS, Android, iOS, Android TV, tvOS and Web, with one public runtime API.

These are implemented capability areas and the stated product direction; each linked reference records the remaining API and backend gaps.

<a id="platforms"></a>

## 🖥️ Platforms

| Platform | Editor target | Runtime target | Current verification |
| --- | --- | --- | --- |
| Windows | Planned | x86, x64, ARM64 | Native packages mapped; Windows execution pending |
| Linux | Planned; X11 and Wayland | x64, ARM64 | Linux x64 Wayland host/rendering checks and XWayland renderer checks; ARM64 execution pending |
| macOS | Planned | x64, ARM64 | Native packages mapped; macOS execution pending |
| Android | — | Phone and tablet ABIs | Tested ARM64 phone: canvas, GPU shaders and CPU physics; broader device/host checks pending |
| Android TV | — | Android ABIs | Tested 32-bit TV: fallback canvas and CPU physics; its device has no Vulkan backend |
| iOS | — | Device and simulator RIDs | Native packages mapped; macOS library-build CI prepared, run pending |
| tvOS | — | Device and simulator RIDs | Native packages mapped; macOS library-build CI prepared, run pending |
| Web | — | `browser-wasm` | Isolated canvas/physics and standalone WebGPU probes; product browser host/backend pending |

The editor targets desktop systems. A mapped package or successful build alone does not establish platform support. Android checks cover the tested devices and graphics/physics paths, with lifecycle, input, audio, storage and release packaging still requiring their own verification.

The [platform verification matrix](docs/platform-verification.md) records devices, renderers, commands and limits. Linux Wayland is the current required native verification gate. Shader materials require the GPU path; the compatibility renderer explicitly rejects unsupported material use.

<a id="installation"></a>

## 📦 Installation

Install the **.NET 10 SDK**, then clone and build the runtime from the repository root:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Reference `Electron2D.csproj` from your application project. The project supplies the engine's platform dependencies during restore and publish. A self-contained application publish contains the game executable, `Electron2D.dll`, the .NET runtime and the applicable native libraries.

<a id="quick-start"></a>

## 🚀 Quick Start

Run the existing **window and input** example on Linux Wayland:

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

Hold the arrow keys to move its scene node; press Escape or close the window to quit. This example reports movement in the terminal and does not draw the scene.

Its [entry point](examples/HostExample/Program.cs) configures a `Window`, adds the scene and calls `Engine.Instance.Run`. The engine owns the event pump, frame timing and teardown. See the [example guide](examples/HostExample/README.md) for the full workflow.

<a id="documentation"></a>

## 📚 Documentation

- **[Documentation index](docs/README.md)** — Domain and component guides.
- **[API reference](docs/inventory.md)** — Implemented production types and their class pages.
- **[Architecture](docs/decisions/index.md)** — Current product and runtime decisions.
- **[Implementation roadmap](docs/coverage/index.md)** — Implemented, adapted and missing capabilities.
- **[Platform verification](docs/platform-verification.md)** — Execution evidence and platform limits.
- **[Visual identity](docs/design/identity.md)** — The approved Sprite direction, logo files, colors and typography.

<a id="examples"></a>

## 🎮 Examples

- **[Window and input](examples/HostExample/README.md)** — A runnable public-API example with a window, scene node, keyboard input and clean exit.

The [Android device probe](tests/Electron2D.AndroidProbe/README.md) and [browser shader probe](tests/Electron2D.WebGpuProbe/README.md) are verification tools under `tests/`. They document their tested graphics/physics paths separately from game examples.

<a id="feedback-and-contributing"></a>

## 💬 Feedback and Contributing

Use [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues) for bug reports, feature requests and design feedback. Include the engine revision, platform, renderer and a minimal reproduction when reporting runtime behavior.

[Pull requests](https://github.com/edwardgushchin/Electron2D/pulls) are welcome. Read the [maintenance guide](docs/maintaining.md) and relevant architectural decisions before changing behavior. Update the affected API documentation and verification together.

Run the executable checks from the repository root:

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

## 👥 Contributors

Electron2D is maintained by Eduard Gushchin. See the [contributors graph](https://github.com/edwardgushchin/Electron2D/graphs/contributors) for repository contributors.

<a id="license"></a>

## 📄 License

Electron2D-authored code is distributed under the [MIT License](licence/Electron2D-LICENSE.txt). Dependencies retain their own licenses; see the [third-party notices](licence/THIRD_PARTY_NOTICES.md). License texts are kept in `licence/` and accompany applicable application publishes.
