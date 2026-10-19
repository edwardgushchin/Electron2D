# TextGraphemeFlags

Last updated: 2026-10-20

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.TextGraphemeFlags`. **Source:** [source](../../src/Scene/GUI/TextGraphemeFlags.cs). **Component:** [Rich text](../components/rich-text.md).

## Description

Shared bit flags describing a glyph cluster, RTL direction, virtual/control/break/space/tab state, joining, punctuation, underscore, elongation and embedded objects. Native shaping flags remain internal; custom effects receive these typed semantic flags.

## Members

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.TextGraphemeFlags BreakHard = 16`](#member-7e528de3a6bd) | A mandatory line separator. |
| [`public const Electron2D.TextGraphemeFlags BreakSoft = 32`](#member-e16f0f505dff) | A soft line opportunity on whitespace, a soft hyphen or a virtual word boundary. |
| [`public const Electron2D.TextGraphemeFlags Connected = 1024`](#member-d606b3df63a1) | The native shaper marks this cluster as unsafe to break. |
| [`public const Electron2D.TextGraphemeFlags Elongation = 128`](#member-6abcdef3d551) | An actual or virtual Arabic elongation opportunity. |
| [`public const Electron2D.TextGraphemeFlags EmbeddedObject = 4096`](#member-d60f295c1e31) | A complete caller-authored inline object. |
| [`public const Electron2D.TextGraphemeFlags None = 0`](#member-e600412525d4) | No grapheme properties. |
| [`public const Electron2D.TextGraphemeFlags Punctuation = 256`](#member-4c7a11682b77) | Punctuation under the active custom or pinned Unicode policy. |
| [`public const Electron2D.TextGraphemeFlags RTL = 2`](#member-166b45df7195) | The resolved run advances right to left. |
| [`public const Electron2D.TextGraphemeFlags SafeToInsertTatweel = 2048`](#member-c1b561af5476) | The native shaper permits insertion of an Arabic elongation glyph. |
| [`public const Electron2D.TextGraphemeFlags SoftHyphen = 8192`](#member-ce5ecf51be3e) | A source soft-hyphen cluster. |
| [`public const Electron2D.TextGraphemeFlags Space = 8`](#member-8b3996a24933) | Source whitespace or a virtual word-space opportunity. |
| [`public const Electron2D.TextGraphemeFlags Tab = 64`](#member-d7d4c8114106) | A horizontal or vertical tabulation source unit. |
| [`public const Electron2D.TextGraphemeFlags Underscore = 512`](#member-b6048f5b6da2) | A source underscore, independently of punctuation. |
| [`public const Electron2D.TextGraphemeFlags Valid = 1`](#member-1be50488f3e6) | A resolved native glyph or an explicit inline object. |
| [`public const Electron2D.TextGraphemeFlags Virtual = 4`](#member-d115a24ec010) | A synthesized shaping or justification record. |

## Member descriptions

<a id="member-7e528de3a6bd"></a>

### BreakHard

`public const Electron2D.TextGraphemeFlags BreakHard = 16`

A mandatory line separator.

<a id="member-e16f0f505dff"></a>

### BreakSoft

`public const Electron2D.TextGraphemeFlags BreakSoft = 32`

A soft line opportunity on whitespace, a soft hyphen or a virtual word boundary.

<a id="member-d606b3df63a1"></a>

### Connected

`public const Electron2D.TextGraphemeFlags Connected = 1024`

The native shaper marks this cluster as unsafe to break.

<a id="member-6abcdef3d551"></a>

### Elongation

`public const Electron2D.TextGraphemeFlags Elongation = 128`

An actual or virtual Arabic elongation opportunity.

<a id="member-d60f295c1e31"></a>

### EmbeddedObject

`public const Electron2D.TextGraphemeFlags EmbeddedObject = 4096`

A complete caller-authored inline object.

<a id="member-e600412525d4"></a>

### None

`public const Electron2D.TextGraphemeFlags None = 0`

No grapheme properties.

<a id="member-4c7a11682b77"></a>

### Punctuation

`public const Electron2D.TextGraphemeFlags Punctuation = 256`

Punctuation under the active custom or pinned Unicode policy.

<a id="member-166b45df7195"></a>

### RTL

`public const Electron2D.TextGraphemeFlags RTL = 2`

The resolved run advances right to left.

<a id="member-c1b561af5476"></a>

### SafeToInsertTatweel

`public const Electron2D.TextGraphemeFlags SafeToInsertTatweel = 2048`

The native shaper permits insertion of an Arabic elongation glyph.

<a id="member-ce5ecf51be3e"></a>

### SoftHyphen

`public const Electron2D.TextGraphemeFlags SoftHyphen = 8192`

A source soft-hyphen cluster.

<a id="member-8b3996a24933"></a>

### Space

`public const Electron2D.TextGraphemeFlags Space = 8`

Source whitespace or a virtual word-space opportunity.

<a id="member-d7d4c8114106"></a>

### Tab

`public const Electron2D.TextGraphemeFlags Tab = 64`

A horizontal or vertical tabulation source unit.

<a id="member-b6048f5b6da2"></a>

### Underscore

`public const Electron2D.TextGraphemeFlags Underscore = 512`

A source underscore, independently of punctuation.

<a id="member-1be50488f3e6"></a>

### Valid

`public const Electron2D.TextGraphemeFlags Valid = 1`

A resolved native glyph or an explicit inline object.

<a id="member-d115a24ec010"></a>

### Virtual

`public const Electron2D.TextGraphemeFlags Virtual = 4`

A synthesized shaping or justification record.

## Verification and limits

See the rich-text component for actual tests, native artifacts, prepared allocation bounds and exact missing dependencies.

ShapedTextGetWordBreaks consumes these same masks. Undefined bits are rejected; default delimiters are Space and Punctuation with Virtual suppressed. A virtual delimiter marks a point and does not discard the source cluster. Native shaping, custom punctuation, mandatory/soft separators and inline validity share the classification used by CharFXTransform. See [span and shaped-word contracts](../components/text-server.md#span-font-replacement-and-shaped-word-ranges).
