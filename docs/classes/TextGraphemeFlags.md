# TextGraphemeFlags

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.TextGraphemeFlags`. **Source:** [source](../../src/Scene/GUI/TextGraphemeFlags.cs). **Component:** [Rich text](../components/rich-text.md).

## Description

Shared bit flags describing a glyph cluster, RTL direction, virtual/control/break/space/tab state, joining, punctuation, underscore, elongation and embedded objects. Native shaping flags remain internal; custom effects receive these typed semantic flags.

## Members

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.TextGraphemeFlags BreakHard = 16`](#member-7e528de3a6bd) | break hard. |
| [`public const Electron2D.TextGraphemeFlags BreakSoft = 32`](#member-e16f0f505dff) | break soft. |
| [`public const Electron2D.TextGraphemeFlags Connected = 1024`](#member-d606b3df63a1) | connected. |
| [`public const Electron2D.TextGraphemeFlags Elongation = 128`](#member-6abcdef3d551) | elongation. |
| [`public const Electron2D.TextGraphemeFlags EmbeddedObject = 4096`](#member-d60f295c1e31) | embedded object. |
| [`public const Electron2D.TextGraphemeFlags None = 0`](#member-e600412525d4) | No grapheme properties. |
| [`public const Electron2D.TextGraphemeFlags Punctuation = 256`](#member-4c7a11682b77) | punctuation. |
| [`public const Electron2D.TextGraphemeFlags RTL = 2`](#member-166b45df7195) | rtl. |
| [`public const Electron2D.TextGraphemeFlags SafeToInsertTatweel = 2048`](#member-c1b561af5476) | safe to insert tatweel. |
| [`public const Electron2D.TextGraphemeFlags SoftHyphen = 8192`](#member-ce5ecf51be3e) | soft hyphen. |
| [`public const Electron2D.TextGraphemeFlags Space = 8`](#member-8b3996a24933) | space. |
| [`public const Electron2D.TextGraphemeFlags Tab = 64`](#member-d7d4c8114106) | tab. |
| [`public const Electron2D.TextGraphemeFlags Underscore = 512`](#member-b6048f5b6da2) | underscore. |
| [`public const Electron2D.TextGraphemeFlags Valid = 1`](#member-1be50488f3e6) | valid. |
| [`public const Electron2D.TextGraphemeFlags Virtual = 4`](#member-d115a24ec010) | virtual. |

## Member descriptions

<a id="member-7e528de3a6bd"></a>

### BreakHard

`public const Electron2D.TextGraphemeFlags BreakHard = 16`

break hard.

<a id="member-e16f0f505dff"></a>

### BreakSoft

`public const Electron2D.TextGraphemeFlags BreakSoft = 32`

break soft.

<a id="member-d606b3df63a1"></a>

### Connected

`public const Electron2D.TextGraphemeFlags Connected = 1024`

connected.

<a id="member-6abcdef3d551"></a>

### Elongation

`public const Electron2D.TextGraphemeFlags Elongation = 128`

elongation.

<a id="member-d60f295c1e31"></a>

### EmbeddedObject

`public const Electron2D.TextGraphemeFlags EmbeddedObject = 4096`

embedded object.

<a id="member-e600412525d4"></a>

### None

`public const Electron2D.TextGraphemeFlags None = 0`

No grapheme properties.

<a id="member-4c7a11682b77"></a>

### Punctuation

`public const Electron2D.TextGraphemeFlags Punctuation = 256`

punctuation.

<a id="member-166b45df7195"></a>

### RTL

`public const Electron2D.TextGraphemeFlags RTL = 2`

rtl.

<a id="member-c1b561af5476"></a>

### SafeToInsertTatweel

`public const Electron2D.TextGraphemeFlags SafeToInsertTatweel = 2048`

safe to insert tatweel.

<a id="member-ce5ecf51be3e"></a>

### SoftHyphen

`public const Electron2D.TextGraphemeFlags SoftHyphen = 8192`

soft hyphen.

<a id="member-8b3996a24933"></a>

### Space

`public const Electron2D.TextGraphemeFlags Space = 8`

space.

<a id="member-d7d4c8114106"></a>

### Tab

`public const Electron2D.TextGraphemeFlags Tab = 64`

tab.

<a id="member-b6048f5b6da2"></a>

### Underscore

`public const Electron2D.TextGraphemeFlags Underscore = 512`

underscore.

<a id="member-1be50488f3e6"></a>

### Valid

`public const Electron2D.TextGraphemeFlags Valid = 1`

valid.

<a id="member-d115a24ec010"></a>

### Virtual

`public const Electron2D.TextGraphemeFlags Virtual = 4`

virtual.

## Verification and limits

See the rich-text component for actual tests, native artifacts, prepared allocation bounds and exact missing dependencies.
