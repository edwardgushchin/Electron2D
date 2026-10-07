# CharFXTransform

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.CharFXTransform`. **Source:** [source](../../src/Scene/Resources/CharFXTransform.cs). **Component:** [Rich text](../components/rich-text.md).

## Description

Mutable callback state owned by an effect block, reused for every glyph and draw pass. Font, range, relative index, flags, glyph count and outline describe the current glyph; writes to those fields do not redirect drawing. Color, GlyphIndex, Offset, Transform and Visible change drawing. Env is the borrowed exact typed argument context. GetGlyphIndex resolves a scalar in the actual callback face. Independent public construction starts black, visible, identity transform and empty context. Disposed state rejects all property access; borrowed callback state cannot be disposed independently.

## Members

| Declaration | Contract |
| --- | --- |
| [`public CharFXTransform()`](#member-5b1d32ce496d) | Creates independent default glyph state. |
| [`public System.Int32 GetGlyphIndex(System.Int32 character)`](#member-e1d37bf94eb2) | Finds a replacement scalar's glyph index in the actual borrowed face of this callback. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-b6892f46ff95) | Inherited typed lifecycle contract. |
| [`public Electron2D.Color Color { get; set; }`](#member-573944eb6d35) | Gets or sets the finite glyph color. |
| [`public System.Double ElapsedTime { get; set; }`](#member-abe7e0db5356) | Gets or sets the effect's elapsed seconds. |
| [`public Electron2D.RichTextEffectEnvironment Env { get; set; }`](#member-da4e85987bf8) | Gets or assigns the borrowed typed argument context. |
| [`public Electron2D.Font Font { get; set; }`](#member-fcd7a8c4c00d) | Gets or assigns descriptive borrowed font ownership. |
| [`public System.Int32 GlyphCount { get; set; }`](#member-1a7e5dab682d) | Gets or sets descriptive glyph count for the cluster. |
| [`public Electron2D.TextGraphemeFlags GlyphFlags { get; set; }`](#member-1f192a9c0bed) | Gets or sets descriptive grapheme flags. |
| [`public System.Int32 GlyphIndex { get; set; }`](#member-ca51ed7498ac) | Gets or sets the actual font-local glyph index. |
| [`public Electron2D.Vector2 Offset { get; set; }`](#member-ea2a3cf706b1) | Gets or sets finite pixel translation. |
| [`public System.Boolean Outline { get; set; }`](#member-8df2646ac159) | Gets or sets descriptive outline-pass state. |
| [`public Electron2D.Vector2i Range { get; set; }`](#member-21b8b3b06904) | Gets or sets the descriptive absolute scalar interval. |
| [`public System.Int32 RelativeIndex { get; set; }`](#member-e4f3eae7475f) | Gets or sets the descriptive scalar offset into the effect block. |
| [`public Electron2D.Transform Transform { get; set; }`](#member-8d1b36cc0295) | Gets or sets the finite glyph transform. |
| [`public System.Boolean Visible { get; set; }`](#member-c7aba5cd8736) | Gets or sets whether this glyph is drawn and occupies visual advance. |

## Member descriptions

<a id="member-5b1d32ce496d"></a>

### CharFXTransform()

`public CharFXTransform()`

Creates independent default glyph state.

<a id="member-e1d37bf94eb2"></a>

### GetGlyphIndex(System.Int32)

`public System.Int32 GetGlyphIndex(System.Int32 character)`

Finds a replacement scalar's glyph index in the actual borrowed face of this callback.

**Character:** Valid Unicode scalar.

**Returns:** Font-local index or zero if missing/unbound.

<a id="member-b6892f46ff95"></a>

### GetPropertyDescriptors()

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

<a id="member-573944eb6d35"></a>

### Color

`public Electron2D.Color Color { get; set; }`

Gets or sets the finite glyph color.

**Value:** Opaque black initially.

<a id="member-abe7e0db5356"></a>

### ElapsedTime

`public System.Double ElapsedTime { get; set; }`

Gets or sets the effect's elapsed seconds.

**Value:** Zero initially; the label supplies its paused-aware block clock.

<a id="member-da4e85987bf8"></a>

### Env

`public Electron2D.RichTextEffectEnvironment Env { get; set; }`

Gets or assigns the borrowed typed argument context.

**Value:** Independent empty context initially.

<a id="member-fcd7a8c4c00d"></a>

### Font

`public Electron2D.Font Font { get; set; }`

Gets or assigns descriptive borrowed font ownership.

**Value:** Null initially; writes do not change the actual glyph face.

<a id="member-1a7e5dab682d"></a>

### GlyphCount

`public System.Int32 GlyphCount { get; set; }`

Gets or sets descriptive glyph count for the cluster.

**Value:** Zero initially; writes do not change drawing.

<a id="member-1f192a9c0bed"></a>

### GlyphFlags

`public Electron2D.TextGraphemeFlags GlyphFlags { get; set; }`

Gets or sets descriptive grapheme flags.

**Value:** None initially; writes do not change drawing.

<a id="member-ca51ed7498ac"></a>

### GlyphIndex

`public System.Int32 GlyphIndex { get; set; }`

Gets or sets the actual font-local glyph index.

**Value:** Zero initially.

<a id="member-ea2a3cf706b1"></a>

### Offset

`public Electron2D.Vector2 Offset { get; set; }`

Gets or sets finite pixel translation.

**Value:** Zero initially.

<a id="member-8df2646ac159"></a>

### Outline

`public System.Boolean Outline { get; set; }`

Gets or sets descriptive outline-pass state.

**Value:** False initially; writes do not change drawing.

<a id="member-21b8b3b06904"></a>

### Range

`public Electron2D.Vector2i Range { get; set; }`

Gets or sets the descriptive absolute scalar interval.

**Value:** Zero initially; writes do not change drawing.

<a id="member-e4f3eae7475f"></a>

### RelativeIndex

`public System.Int32 RelativeIndex { get; set; }`

Gets or sets the descriptive scalar offset into the effect block.

**Value:** Zero initially; writes do not change drawing.

<a id="member-8d1b36cc0295"></a>

### Transform

`public Electron2D.Transform Transform { get; set; }`

Gets or sets the finite glyph transform.

**Value:** Identity initially.

<a id="member-c7aba5cd8736"></a>

### Visible

`public System.Boolean Visible { get; set; }`

Gets or sets whether this glyph is drawn and occupies visual advance.

**Value:** True initially.

## Verification and limits

See the rich-text component for actual tests, native artifacts, prepared allocation bounds and exact missing dependencies.
