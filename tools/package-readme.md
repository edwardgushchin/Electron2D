# Electron2D

A free, open-source cross-platform 2D engine written in C# for developers and AI agents to build games together.

This package supplies the managed runtime, `Electron2D.dll`. Add the platform packages for the systems your game targets: `Electron2D.Windows`, `Electron2D.Linux`, `Electron2D.MacOS`, `Electron2D.Android`, `Electron2D.iOS`, `Electron2D.tvOS` and `Electron2D.Web`.

For example, a Linux game uses:

```bash
dotnet add package Electron2D --prerelease
dotnet add package Electron2D.Linux --prerelease
```

Each platform package selects its native SDL, graphics, font, audio and networking dependencies. Publishing for a specific RID includes only the matching native assets. The platform package version identifies the minimum supported Electron2D version; use matching release versions unless a different combination has been verified.

See the [repository quick start](https://github.com/edwardgushchin/Electron2D#quick-start), [API documentation](https://github.com/edwardgushchin/Electron2D/wiki), and [platform verification report](https://github.com/edwardgushchin/Electron2D/blob/main/docs/platform-verification.md). Package availability and platform execution are separate checks.
