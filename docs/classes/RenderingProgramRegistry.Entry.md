# RenderingProgramRegistry.Entry

Last updated: 2026-10-07

- Visibility: internal
- Source: [RenderingProgramRegistry.cs](../../src/Servers/Rendering/RenderingProgramRegistry.cs)
- Component: [Shader materials](../components/shader-materials.md)

## Description

One program identity record retains its owner and optional owned resource while tracking the weak resource and shader path hint.

## Verification

[RenderingProgramTests](../../tests/Electron2D.Tests/RenderingProgramTests.cs) exercises identity, ownership, actual native replay and teardown. [The program RID contract](../components/shader-materials.md#shader-and-material-identities) records limits.
