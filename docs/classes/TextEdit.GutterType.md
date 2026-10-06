# TextEdit.GutterType

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.TextEdit.GutterType`. **Source:** [source](../../src/Scene/GUI/TextEdit.cs). **Component:** [Multiline editing](../components/multiline-editing.md).

## Behavior

Typed enum identity for the multiline editor. Numeric values, defaults and selection policies are shared across the corresponding control operations. Constructor/state sentinels are not additional editing commands.

## Members

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.TextEdit.GutterType Custom = 2` | Typed custom drawing callback. |
| `public const Electron2D.TextEdit.GutterType Icon = 1` | Borrowed texture. |
| `public const Electron2D.TextEdit.GutterType String = 0` | Shaped text. |

## Verification and limits

TextEditTests covers scalar/grapheme edits, multicaret ordering, grouped undo/redo, observer failure, copied metadata/configuration, syntax continuation/cache invalidation, committed/preedit input and fresh-process scene loading. Native GPU/compatibility readbacks exercise actual glyph/selection/gutter/minimap/placeholder recording, SDL event producers and clipboard restoration. The warm interval measures prepared managed owner recording and selection; it excludes cold parsing/edit snapshots, native events and native allocator totals. See [coverage](../coverage/classes/TextEdit.md) for inherited/native/editor dependencies.
