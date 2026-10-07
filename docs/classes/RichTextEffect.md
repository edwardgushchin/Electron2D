# RichTextEffect

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.RichTextEffect`. **Source:** [source](../../src/Scene/Resources/RichTextEffect.cs). **Component:** [Rich text](../components/rich-text.md).

## Description

A borrowed Resource installed by its BBCode tag name or pushed directly with a typed environment. Override OnProcessCustomFX to transform live glyph color, font-local index, offset, transform or visibility. Returning false stops subsequent custom effects on that glyph while builtin effects continue. Resource factories and BBCode configuration survive scene/resource files; application-derived behavior needs its application factory. Compiled Script source/assembly installation remains a separate scripting capability.

## Members

| Declaration | Contract |
| --- | --- |
| [`public RichTextEffect()`](#member-2b1a3b37f21c) | Creates an unnamed effect whose default hook leaves text untransformed. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`](#member-f26a02553164) | Inherited typed lifecycle contract. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-49626ca298c1) | Inherited typed lifecycle contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-801247112e78) | Inherited typed lifecycle contract. |
| [`protected virtual System.Boolean OnProcessCustomFX(Electron2D.CharFXTransform charFX)`](#member-e3e33711963d) | Transforms the current glyph state. |
| [`public System.String BBCode { get; set; }`](#member-6b5db1b0c6d2) | Gets or sets this effect's tag identifier. |

## Member descriptions

<a id="member-2b1a3b37f21c"></a>

### RichTextEffect()

`public RichTextEffect()`

Creates an unnamed effect whose default hook leaves text untransformed.

<a id="member-f26a02553164"></a>

### CopyCustomStateTo(Electron2D.Resource, System.Boolean, Electron2D.DeepDuplicateMode, System.Func<Electron2D.Resource, Electron2D.Resource>, System.Func<Electron2D.Resource, Electron2D.Resource>)

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`

<a id="member-49626ca298c1"></a>

### CreateDuplicateInstance()

`protected override Electron2D.Resource CreateDuplicateInstance()`

<a id="member-801247112e78"></a>

### GetPropertyDescriptors()

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

<a id="member-e3e33711963d"></a>

### OnProcessCustomFX(Electron2D.CharFXTransform)

`protected virtual System.Boolean OnProcessCustomFX(Electron2D.CharFXTransform charFX)`

Transforms the current glyph state.

**Charfx:** Borrowed state, reset for subsequent glyphs.

**Returns:** True when this transform succeeds; false stops later custom effects for that glyph.

<a id="member-6b5db1b0c6d2"></a>

### BBCode

`public System.String BBCode { get; set; }`

Gets or sets this effect's tag identifier.

**Value:** Empty initially; unnamed effects can still be pushed directly.

## Verification and limits

See the rich-text component for actual tests, native artifacts, prepared allocation bounds and exact missing dependencies.
