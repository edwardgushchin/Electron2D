# ColorPalette

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.ColorPalette`. **Base:** `Electron2D.Resource`. **Source:** [source](../../src/Scene/Resources/ColorPalette.cs). **Component:** [Color authoring](../components/color-authoring.md).

An ordered copied color array with silent assignment, independent duplication and built-in typed palette files. See the component for threading and I/O limits.


## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public ColorPalette()` | Creates an empty palette. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public Electron2D.Color[] Colors { get; set; }` | Gets a copy of, or replaces, the ordered colors. Empty initially; duplicates, alpha and HDR values are retained. The assigned array is null. |

## Members and lifecycle

| Complete declaration | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Extends the inherited typed lifecycle contract. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Extends the inherited typed lifecycle contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Extends the inherited typed lifecycle contract. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Extends the inherited typed lifecycle contract. |
