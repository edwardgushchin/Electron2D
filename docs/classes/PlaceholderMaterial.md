# PlaceholderMaterial

Last updated: 2026-10-26

- Declaration: `public sealed class PlaceholderMaterial : Material`
- Source: [PlaceholderMaterial.cs](../../src/Scene/Resources/PlaceholderMaterial.cs)
- Inherits: [Material](Material.md)
- Component: [Material placeholders](../components/material-placeholders.md)

## Description and API

`public PlaceholderMaterial()` creates a program-free canvas material with default Resource metadata. CanvasItem and retained RenderingServer canvas items use ordinary color drawing; no absent fragment effect is synthesized. GetRID returns the inherited stable borrowed material identity. Particle process submission rejects this canvas role.

Material.CreatePlaceholder produces this empty resource from any live material. Automatic version-8 tolerant archive recovery also preserves unavailable factory identity, opaque fields and actual resource references privately. Copies, scene-local graphs and file resaves retain those records for later compiled restoration. An explicitly authored placeholder remains strictly loadable under its own built-in factory.

## Lifecycle and verification

Protected overrides CreateDuplicateInstance, CopyCustomStateTo, RemapResourceReferences and Dispose implement the inherited typed Resource graph contract. Shallow CopyFromResource retains ordinary source reference edges and target identity; deep All duplication remaps cycles to the duplicate root. Opaque metadata is released on disposal. Dependencies remain borrowed or graph-owned under ADR 0014.

[MaterialPlaceholderTests](../../tests/Electron2D.Tests/MaterialPlaceholderTests.cs) and [render checks](../../tests/Electron2D.Tests/MaterialPlaceholderTests.Rendering.cs) exercise missing/returned file graphs and actual canvas RID switching. Current Linux software GPU/Compatibility evidence does not establish physical-GPU, native-heap, foreign-platform or owner acceptance.
