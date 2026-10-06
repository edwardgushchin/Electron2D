# SyntaxHighlighter

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.SyntaxHighlighter`. **Source:** [source](../../src/Scene/Resources/SyntaxHighlighter.cs). **Component:** [Multiline editing](../components/multiline-editing.md).

## Behavior

A resource extension point for immutable scalar-column/color transitions. Each transition continues until the next column. The base hook returns an empty map, which means ordinary font color; CodeHighlighter supplies executable parsing. One live editor is borrowed through a weak association. Queries cache sorted copied maps and reject negative indices, nonfinite colors and recursive computation. Document versions and line notifications invalidate stale results. ClearHighlightingCache and UpdateCache invoke the typed subclass hooks; custom resource subclasses retain the ordinary exact-type duplication contract.

## Members

| Declaration | Contract |
| --- | --- |
| `public SyntaxHighlighter()` | Creates an unbound highlighter with no color overrides. |
| `public System.Void ClearHighlightingCache()` | Clears retained transitions and invokes the subclass cache-clear hook. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` |  |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `public System.Collections.Generic.IReadOnlyDictionary<System.Int32, Electron2D.Color> GetLineSyntaxHighlighting(System.Int32 line)` | Returns the cached immutable color transitions for a logical line. |
| `public Electron2D.TextEdit GetTextEdit()` | Returns the live borrowed editor or null. |
| `protected virtual System.Void OnClearHighlightingCache()` | Clears subclass caches after the base cache has been cleared. |
| `protected virtual System.Collections.Generic.IReadOnlyDictionary<System.Int32, Electron2D.Color> OnGetLineSyntaxHighlighting(System.Int32 line)` | Calculates scalar-column color transitions for an uncached logical line. |
| `protected virtual System.Void OnUpdateCache()` | Updates subclass state using the bound editor. |
| `public System.Void UpdateCache()` | Clears all cached transitions, then updates subclass state when bound. |

## Verification and limits

TextEditTests covers scalar/grapheme edits, multicaret ordering, grouped undo/redo, observer failure, copied metadata/configuration, syntax continuation/cache invalidation, committed/preedit input and fresh-process scene loading. Native GPU/compatibility readbacks exercise actual glyph/selection/gutter/minimap/placeholder recording, SDL event producers and clipboard restoration. The warm interval measures prepared managed owner recording and selection; it excludes cold parsing/edit snapshots, native events and native allocator totals. See [coverage](../coverage/classes/SyntaxHighlighter.md) for inherited/native/editor dependencies.
