# CodeHighlighter

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.CodeHighlighter`. **Source:** [source](../../src/Scene/Resources/CodeHighlighter.cs). **Component:** [Multiline editing](../components/multiline-editing.md).

## Behavior

A syntax resource for ordinary/member keywords, numeric literals, symbols, functions, member access and symbol-delimited color regions. Configuration dictionaries are copied on input/output. Regions prefer the longest start delimiter and can continue across logical lines; quote escapes and line-only endings participate in parsing. Category colors default to black. Cache parsing is cold and Unicode scalar positions feed the already shaped editor glyphs. Resource duplication preserves independent dictionaries and region configuration. Portable color dictionaries and built-in factories allow fresh-process scene restoration. The bound editor is borrowed and each scene instance obtains its own resource.

## Members

| Declaration | Contract |
| --- | --- |
| `public CodeHighlighter()` | Creates a highlighter with black category colors and no keywords/regions. |
| `public System.Void AddColorRegion(System.String startKey, System.String endKey, Electron2D.Color color, System.Boolean lineOnly = false)` | Adds a unique symbol-delimited color region. |
| `public System.Void AddKeywordColor(System.String keyword, Electron2D.Color color)` | Adds or replaces one ordinary keyword color. |
| `public System.Void AddMemberKeywordColor(System.String memberKeyword, Electron2D.Color color)` | Adds or replaces one member keyword color. |
| `public System.Void ClearColorRegions()` | Clears color regions and their continuation cache. |
| `public System.Void ClearKeywordColors()` | Clears ordinary keywords and highlighting cache. |
| `public System.Void ClearMemberKeywordColors()` | Clears member keywords and highlighting cache. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` |  |
| `protected override Electron2D.Resource CreateDuplicateInstance()` |  |
| `public Electron2D.Color GetKeywordColor(System.String keyword)` | Returns an existing keyword color. |
| `public Electron2D.Color GetMemberKeywordColor(System.String memberKeyword)` | Returns an existing member keyword color. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` |  |
| `public System.Boolean HasColorRegion(System.String startKey)` | Reports whether a start delimiter is configured. |
| `public System.Boolean HasKeywordColor(System.String keyword)` | Reports exact ordinary keyword membership. |
| `public System.Boolean HasMemberKeywordColor(System.String memberKeyword)` | Reports exact member keyword membership. |
| `protected override System.Void OnClearHighlightingCache()` |  |
| `protected override System.Collections.Generic.IReadOnlyDictionary<System.Int32, Electron2D.Color> OnGetLineSyntaxHighlighting(System.Int32 line)` |  |
| `public System.Void RemoveColorRegion(System.String startKey)` | Removes the region with an exact start key. |
| `public System.Void RemoveKeywordColor(System.String keyword)` | Removes a keyword if present. |
| `public System.Void RemoveMemberKeywordColor(System.String memberKeyword)` | Removes a member keyword if present. |
| `public System.Collections.Generic.Dictionary<System.String, Electron2D.Color> ColorRegions { get; set; }` | Gets or replaces copied delimiter-pair colors; start-only keys are line regions. |
| `public Electron2D.Color FunctionColor { get; set; }` | Gets or sets names followed by an opening parenthesis. |
| `public System.Collections.Generic.Dictionary<System.String, Electron2D.Color> KeywordColors { get; set; }` | Gets or replaces copied ordinary keyword colors. |
| `public System.Collections.Generic.Dictionary<System.String, Electron2D.Color> MemberKeywordColors { get; set; }` | Gets or replaces copied member keywords, recognized without preceding member access. |
| `public Electron2D.Color MemberVariableColor { get; set; }` | Gets or sets member names following a dot. |
| `public Electron2D.Color NumberColor { get; set; }` | Gets or sets the numeric literal color. |
| `public Electron2D.Color SymbolColor { get; set; }` | Gets or sets the symbol and string escape marker color. |

## Verification and limits

TextEditTests covers scalar/grapheme edits, multicaret ordering, grouped undo/redo, observer failure, copied metadata/configuration, syntax continuation/cache invalidation, committed/preedit input and fresh-process scene loading. Native GPU/compatibility readbacks exercise actual glyph/selection/gutter/minimap/placeholder recording, SDL event producers and clipboard restoration. The warm interval measures prepared managed owner recording and selection; it excludes cold parsing/edit snapshots, native events and native allocator totals. See [coverage](../coverage/classes/CodeHighlighter.md) for inherited/native/editor dependencies.
