# RenderingProgramRegistry

Last updated: 2026-10-07

- Visibility: internal
- Source: [RenderingProgramRegistry.cs](../../src/Servers/Rendering/RenderingProgramRegistry.cs)
- Component: [Shader materials](../components/shader-materials.md)

## Description

Shared typed RID registry for Shader and Material: weak borrowed resource identity, strong renderer-owned retention, ownership validation, cold stale sweep and diagnostic shader path metadata.

## Verification

[RenderingProgramTests](../../tests/Electron2D.Tests/RenderingProgramTests.cs) exercises identity, ownership, actual native replay and teardown. [The program RID contract](../components/shader-materials.md#shader-and-material-identities) records limits.
