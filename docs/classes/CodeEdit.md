# CodeEdit

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.CodeEdit`. **Source:** [source](../../src/Scene/GUI/CodeEdit.cs). **Component:** [Code authoring](../components/code-authoring.md).

**Inherits:** [TextEdit](TextEdit.md).

## Description

CodeEdit specializes TextEdit rather than introducing another document or renderer. Logical lines, Unicode scalar columns, multiple carets, selections, undo/redo, wrapped geometry, highlighters, IME, clipboard and context menus use the inherited engine workflow. Three required custom gutters present line numbers, breakpoints/bookmarks/execution and folding. Marker metadata follows line identity through insertion, movement and history; breakpoint shifts publish the old and new line indices. Resources and application option values are borrowed. The hover Timer and base editor children are owned internal nodes.

Automatic indentation inherits leading whitespace and expands the configured scalar prefixes. Indent/dedent and conversion use tab stops and configured spaces/tabs. Paired Unicode input wraps selections, skips an existing closer and deletes both keys on backspace; paired newline creates an indented body. Explicit line move, duplicate, delete, join and named-region operations preserve grouped history. Read-only input is ignored; explicit document line commands remain available programmatically. Duplicate operations advance caret identity to the copy. Joining rejects LF in its separator.

String/comment keys are validated symbols, unique across both kinds and longest-first. Parsing uses scalar coordinates and escaped keys; comments and strings do not nest. Delimiter start positions are one scalar after the opening key begins; end positions follow the closer, and line-only end positions are line length plus one. Unknown or unclosed boundaries are (-1,-1). Whole-line queries require whitespace outside the region. Folds cover indentation, consecutive whole-line comments/strings, multiline delimiters and nested comment regions. Trailing blank lines stay outside indentation folds. Folded/hidden lines cannot fold again; header identity follows document edits. CreateCodeRegion requires a selection and a line-only comment delimiter, inserts a localized name, folds the new block and selects its name.

The application produces completion candidates through CodeCompletionRequested or the typed request hook. UpdateCodeCompletionOptions replaces the submitted source queue; the default filter ranks case-insensitive scalar subsequences by contiguous segments, prefix position, case, scope, matching positions and natural text order. String candidates adapt their quotes to the current string. Numeric and exact single matches cancel. A custom filter owns candidate ordering and uses an empty insertion prefix. Immutable CodeCompletionOption preserves kind, display/insert text, color, borrowed texture, exact generic value and integer scope distance. Confirmation merges or replaces following text across all active carets as one undo operation, avoids duplicate string closers and positions function-call carets inside empty parentheses.

Completion uses the existing canvas, theme and GUI route. The fitted menu supports navigation, paging, wheel, scrollbar dragging and pointer confirmation; actions resolve InputMap bindings. Code hints use U+FFFF argument boundaries to align, shade and underline the current argument. Guidelines and balanced multiline matching/mismatch underlines share text geometry. Validated command-click lookup and delayed hover expose application symbol callbacks; the project tooltip delay is sampled at Ready. CodeHighlighter can supply language colors but there is no implicit language parser or compiler.

Stored configuration includes delimiters, brace pairs, indentation, gutter policies, completion prefixes, folding, symbols and guidelines. A registered CodeEdit factory and string dictionary codec restore independently in a fresh process; runtime markers/folds/history/carets/options/hints/callbacks are omitted. Cold editing/history snapshots, parsing, matching and shaping are distinct from prepared draw intervals. Native virtual-keyboard/symbol-picker sessions, native semantic accessibility, editor authoring and foreign-platform/physical/native-allocator acceptance retain their exact prerequisites.

## Example

```csharp
var code = new CodeEdit { CodeCompletionEnabled = true,
    IndentAutomatic = true, IndentUseSpaces = true,
    AutoBraceCompletionEnabled = true, GuttersDrawLineNumbers = true };
code.CodeCompletionRequested += () => {
    code.AddCodeCompletionOption(CodeEdit.CodeCompletionKind.Function, "print()", "print()");
    code.UpdateCodeCompletionOptions(false);
};
```

## Constructors

| Declaration | Contract |
| --- | --- |
| `public CodeEdit()` | Creates an LTR empty code document with three required gutters and an owned hover timer. |

## Constructors descriptions

<a id="member-29a3b45fddd6"></a>

### CodeEdit()

`public CodeEdit()`

Creates an LTR empty code document with three required gutters and an owned hover timer.

## Events

| Declaration | Contract |
| --- | --- |
| `public event System.Action<System.Int32> BreakpointToggled` | Occurs when a breakpoint is set, cleared or shifted to another line. |
| `public event System.Action CodeCompletionRequested` | Requests that the application submit completion candidates. |
| `public event System.Action<System.String, System.Int32, System.Int32> SymbolHovered` | Reports a symbol after the configured hover delay. |
| `public event System.Action<System.String, System.Int32, System.Int32> SymbolLookup` | Requests application lookup of a symbol at a logical line/scalar column. |
| `public event System.Action<System.String> SymbolValidate` | Requests validation of the symbol under the pointer. |

## Events descriptions

<a id="member-64989f746c5a"></a>

### BreakpointToggled

`public event System.Action<System.Int32> BreakpointToggled`

Occurs when a breakpoint is set, cleared or shifted to another line.

<a id="member-d2b8b56b2a6f"></a>

### CodeCompletionRequested

`public event System.Action CodeCompletionRequested`

Requests that the application submit completion candidates.

<a id="member-124b88581836"></a>

### SymbolHovered

`public event System.Action<System.String, System.Int32, System.Int32> SymbolHovered`

Reports a symbol after the configured hover delay.

<a id="member-c178f014963d"></a>

### SymbolLookup

`public event System.Action<System.String, System.Int32, System.Int32> SymbolLookup`

Requests application lookup of a symbol at a logical line/scalar column.

<a id="member-8a6803852c49"></a>

### SymbolValidate

`public event System.Action<System.String> SymbolValidate`

Requests validation of the symbol under the pointer.

## Methods

| Declaration | Contract |
| --- | --- |
| `public System.Void AddAutoBraceCompletionPair(System.String startKey, System.String endKey)` | Adds a unique symbol-key brace pair. |
| `public System.Void AddCodeCompletionOption(Electron2D.CodeEdit.CodeCompletionKind type, System.String displayText, System.String insertText, System.Nullable<Electron2D.Color> textColor = null, Electron2D.Texture icon = null, System.Int32 location = 1024)` | Submits a typed completion candidate without a default value. |
| `public System.Void AddCodeCompletionOption<T>(Electron2D.CodeEdit.CodeCompletionKind type, System.String displayText, System.String insertText, System.Nullable<Electron2D.Color> textColor = null, Electron2D.Texture icon = null, T value = null, System.Int32 location = 1024)` | Submits a completion candidate with a borrowed exact generic default value. |
| `public System.Void AddCommentDelimiter(System.String startKey, System.String endKey, System.Boolean lineOnly = false)` | Adds a symbol-delimited comment region. |
| `public System.Void AddStringDelimiter(System.String startKey, System.String endKey, System.Boolean lineOnly = false)` | Adds a symbol-delimited string region, ordered longest start key first. |
| `public System.Boolean CanFoldLine(System.Int32 line)` | Reports whether enabled folding can hide a region, multiline delimiter or indented block. |
| `public System.Void CancelCodeCompletion()` | Cancels the menu and clears visible candidates. |
| `public System.Void ClearBookmarkedLines()` | Clears bookmarks. |
| `public System.Void ClearBreakpointedLines()` | Clears all breakpoints, notifying each changed line. |
| `public System.Void ClearCommentDelimiters()` | Clears comment delimiters while retaining strings. |
| `public System.Void ClearExecutingLines()` | Clears executing-line markers. |
| `public System.Void ClearStringDelimiters()` | Clears string delimiters while retaining comments. |
| `public System.Void ConfirmCodeCompletion(System.Boolean replace = false)` | Confirms the selected candidate through the virtual insertion hook. |
| `public System.Void ConvertIndent(System.Int32 fromLine = -1, System.Int32 toLine = -1)` | Converts leading indentation of a logical-line interval to the configured tab/space policy. |
| `public System.Void CreateCodeRegion()` | Wraps selected lines in named comment region tags, folds them and selects the new region name. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `public System.Void DeleteLines()` | Deletes each selected/caret logical line once. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `public System.Void DoIndent()` | Indents selections, or inserts indentation at each caret. |
| `public System.Void DuplicateLines()` | Duplicates complete selected/caret lines and advances their caret identities. |
| `public System.Void DuplicateSelection()` | Duplicates selected text, or complete lines for unselected carets. |
| `public System.Void FoldAllLines()` | Folds every supported block. |
| `public System.Void FoldLine(System.Int32 line)` | Folds a supported block and moves carets from hidden descendants onto its header. |
| `public System.String GetAutoBraceCompletionCloseKey(System.String openKey)` | Returns a matching closing key or an empty string. |
| `public System.Int32[] GetBookmarkedLines()` | Returns an ascending bookmark array. |
| `public System.Int32[] GetBreakpointedLines()` | Returns an independent ascending breakpoint line array. |
| `public Electron2D.CodeCompletionOption GetCodeCompletionOption(System.Int32 index)` | Gets a stable borrowed immutable visible candidate. |
| `public Electron2D.CodeCompletionOption[] GetCodeCompletionOptions()` | Gets an independent array of immutable visible candidates. |
| `public System.Int32 GetCodeCompletionSelectedIndex()` | Gets the selected visible candidate index. |
| `public System.String GetCodeRegionEndTag()` | Returns the source region end tag. |
| `public System.String GetCodeRegionStartTag()` | Returns the source region start tag. |
| `public System.String GetDelimiterEndKey(System.Int32 delimiterIndex)` | Returns the combined ordered delimiter end key. |
| `public Electron2D.Vector2 GetDelimiterEndPosition(System.Int32 line, System.Int32 column)` | Returns the delimiter end boundary after its closing key, or line-length plus one for line-only. |
| `public System.String GetDelimiterStartKey(System.Int32 delimiterIndex)` | Returns the combined ordered delimiter start key. |
| `public Electron2D.Vector2 GetDelimiterStartPosition(System.Int32 line, System.Int32 column)` | Returns the delimiter start boundary one scalar after its opening key begins. |
| `public System.Int32[] GetExecutingLines()` | Returns an ascending executing-line array. |
| `public System.Int32[] GetFoldedLines()` | Returns an independent ascending folded-header array. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `public System.String GetTextForCodeCompletion()` | Returns cursor-marked text at the primary caret. |
| `public System.String GetTextForSymbolLookup()` | Returns cursor-marked text at the current symbol-lookup position. |
| `public System.String GetTextWithCursorChar(System.Int32 line, System.Int32 column)` | Returns the complete text with U+FFFF at a logical/scalar coordinate. |
| `public System.Boolean HasAutoBraceCompletionCloseKey(System.String closeKey)` | Reports a closing brace key. |
| `public System.Boolean HasAutoBraceCompletionOpenKey(System.String openKey)` | Reports an opening brace key. |
| `public System.Boolean HasCommentDelimiter(System.String startKey)` | Reports an exact comment start key. |
| `public System.Boolean HasStringDelimiter(System.String startKey)` | Reports an exact string start key. |
| `public System.Void IndentLines()` | Indents each selected/caret line once, omitting empty lines. |
| `public System.Int32 IsInComment(System.Int32 line, System.Int32 column = -1)` | Returns a combined comment delimiter index at a scalar position, or for a wholly covered line. |
| `public System.Int32 IsInString(System.Int32 line, System.Int32 column = -1)` | Returns a combined string delimiter index at a scalar position, or for a wholly covered line. |
| `public System.Boolean IsLineBookmarked(System.Int32 line)` | Reports a bookmark. |
| `public System.Boolean IsLineBreakpointed(System.Int32 line)` | Reports a breakpoint marker. |
| `public System.Boolean IsLineCodeRegionEnd(System.Int32 line)` | Reports a single-line-comment code-region end. |
| `public System.Boolean IsLineCodeRegionStart(System.Int32 line)` | Reports a single-line-comment code-region start. |
| `public System.Boolean IsLineExecuting(System.Int32 line)` | Reports an executing-line marker. |
| `public System.Boolean IsLineFolded(System.Int32 line)` | Reports an explicit folded header. |
| `public System.Void JoinLines(System.String lineEnding = " ")` | Joins selected/caret lines with following lines, trimming connecting whitespace. |
| `public System.Void MoveLinesDown()` | Moves selected/caret line ranges downward with their gutters and backgrounds. |
| `public System.Void MoveLinesUp()` | Moves selected/caret line ranges upward with their gutters and backgrounds. |
| `protected override System.Void OnBackspace(System.Int32 caretIndex)` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `protected virtual System.Void OnConfirmCodeCompletion(System.Boolean replace)` | Inserts the active candidate as one undo group and cancels completion. |
| `protected override System.Void OnCut(System.Int32 caretIndex)` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `protected virtual Electron2D.CodeCompletionOption[] OnFilterCodeCompletionCandidates(Electron2D.CodeCompletionOption[] candidates)` | Filters application candidates into their visible order. An override owns the complete filter and insertion prefix is empty. |
| `protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `protected override System.Void OnHandleUnicodeInput(System.Int32 unicodeChar, System.Int32 caretIndex)` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `protected override System.Void OnNotification(System.Int32 what)` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `protected override System.Void OnPaste(System.Int32 caretIndex)` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `protected virtual System.Void OnRequestCodeCompletion(System.Boolean force)` | Requests candidate production after validating ordinary query context. |
| `protected override System.Void OnTextInput(System.String text)` | Overrides the inherited typed scene/input/storage lifecycle contract. |
| `public System.Void RemoveCommentDelimiter(System.String startKey)` | Removes a comment delimiter by start key. |
| `public System.Void RemoveStringDelimiter(System.String startKey)` | Removes a string delimiter by start key. |
| `public System.Void RequestCodeCompletion(System.Boolean force = false)` | Requests application completion using the typed virtual request hook. |
| `public System.Void SetCodeCompletionSelectedIndex(System.Int32 index)` | Changes selection while completion is active. |
| `public System.Void SetCodeHint(System.String codeHint)` | Sets source code-hint text; empty clears it. U+FFFF toggles highlighted argument spans. |
| `public System.Void SetCodeHintDrawBelow(System.Boolean drawBelow)` | Chooses whether the code hint is below or above the primary caret. |
| `public System.Void SetCodeRegionTags(System.String start = "region", System.String end = "endregion")` | Sets source region tags without a comment prefix. |
| `public System.Void SetLineAsBookmarked(System.Int32 line, System.Boolean bookmarked)` | Sets a runtime bookmark. |
| `public System.Void SetLineAsBreakpoint(System.Int32 line, System.Boolean breakpointed)` | Sets a runtime breakpoint marker. |
| `public System.Void SetLineAsExecuting(System.Int32 line, System.Boolean executing)` | Sets an executing-line marker. |
| `public System.Void SetSymbolLookupWordAsValid(System.Boolean valid)` | Marks the most recently requested pointer symbol as eligible for lookup. |
| `public System.Void ToggleFoldableLine(System.Int32 line)` | Toggles a supported folded header. |
| `public System.Void ToggleFoldableLinesAtCarets()` | Toggles each distinct caret header. |
| `public System.Void UnfoldAllLines()` | Clears all folds and restores every logical line. |
| `public System.Void UnfoldLine(System.Int32 line)` | Unfolds the containing folded header while preserving nested folded blocks. |
| `public System.Void UnindentLines()` | Removes one indentation stop from each selected/caret line. |
| `public System.Void UpdateCodeCompletionOptions(System.Boolean force)` | Replaces the current candidate sources with the submitted queue and filters them. |

## Methods descriptions

<a id="member-bfa8d4f0f04b"></a>

### AddAutoBraceCompletionPair(System.String, System.String)

`public System.Void AddAutoBraceCompletionPair(System.String startKey, System.String endKey)`

Adds a unique symbol-key brace pair.

**Param `startKey`:** Nonempty symbols.

**Param `endKey`:** Nonempty symbols.

<a id="member-cb7a7abda78c"></a>

### AddCodeCompletionOption(Electron2D.CodeEdit.CodeCompletionKind, System.String, System.String, System.Nullable<Electron2D.Color>, Electron2D.Texture, System.Int32)

`public System.Void AddCodeCompletionOption(Electron2D.CodeEdit.CodeCompletionKind type, System.String displayText, System.String insertText, System.Nullable<Electron2D.Color> textColor = null, Electron2D.Texture icon = null, System.Int32 location = 1024)`

Submits a typed completion candidate without a default value.

**Param `type`:** Candidate kind.

**Param `displayText`:** Menu text.

**Param `insertText`:** Inserted text.

**Param `textColor`:** Finite color or white.

**Param `icon`:** Borrowed texture.

**Param `location`:** Relative scope location.

<a id="member-55d74503eee6"></a>

### AddCodeCompletionOption(Electron2D.CodeEdit.CodeCompletionKind, System.String, System.String, System.Nullable<Electron2D.Color>, Electron2D.Texture, T, System.Int32)

`public System.Void AddCodeCompletionOption<T>(Electron2D.CodeEdit.CodeCompletionKind type, System.String displayText, System.String insertText, System.Nullable<Electron2D.Color> textColor = null, Electron2D.Texture icon = null, T value = null, System.Int32 location = 1024)`

Submits a completion candidate with a borrowed exact generic default value.

**Param `type`:** Candidate kind.

**Param `displayText`:** Menu text.

**Param `insertText`:** Inserted text.

**Param `textColor`:** Finite color or white.

**Param `icon`:** Borrowed texture.

**Param `value`:** Borrowed typed payload.

**Param `location`:** Relative scope location.

**Typeparam `T`:** Payload type.

<a id="member-1f2b9121488b"></a>

### AddCommentDelimiter(System.String, System.String, System.Boolean)

`public System.Void AddCommentDelimiter(System.String startKey, System.String endKey, System.Boolean lineOnly = false)`

Adds a symbol-delimited comment region.

**Param `startKey`:** Unique nonempty symbol key.

**Param `endKey`:** Symbol key or empty.

**Param `lineOnly`:** Prevents continuation across LF.

<a id="member-c6a0b2eef24a"></a>

### AddStringDelimiter(System.String, System.String, System.Boolean)

`public System.Void AddStringDelimiter(System.String startKey, System.String endKey, System.Boolean lineOnly = false)`

Adds a symbol-delimited string region, ordered longest start key first.

**Param `startKey`:** Unique nonempty symbol key.

**Param `endKey`:** Symbol key or empty for line-only.

**Param `lineOnly`:** Prevents continuation across LF.

<a id="member-3db8778c9c6d"></a>

### CanFoldLine(System.Int32)

`public System.Boolean CanFoldLine(System.Int32 line)`

Reports whether enabled folding can hide a region, multiline delimiter or indented block.

**Param `line`:** Existing line.

**Returns:** Whether children can fold.

<a id="member-6d60454817dd"></a>

### CancelCodeCompletion()

`public System.Void CancelCodeCompletion()`

Cancels the menu and clears visible candidates.

<a id="member-5e5d8720fd3f"></a>

### ClearBookmarkedLines()

`public System.Void ClearBookmarkedLines()`

Clears bookmarks.

<a id="member-ffe90952d885"></a>

### ClearBreakpointedLines()

`public System.Void ClearBreakpointedLines()`

Clears all breakpoints, notifying each changed line.

<a id="member-08ededa3ca2a"></a>

### ClearCommentDelimiters()

`public System.Void ClearCommentDelimiters()`

Clears comment delimiters while retaining strings.

<a id="member-8212a5510d4c"></a>

### ClearExecutingLines()

`public System.Void ClearExecutingLines()`

Clears executing-line markers.

<a id="member-8f8d989dc6d5"></a>

### ClearStringDelimiters()

`public System.Void ClearStringDelimiters()`

Clears string delimiters while retaining comments.

<a id="member-953dbe32bd59"></a>

### ConfirmCodeCompletion(System.Boolean)

`public System.Void ConfirmCodeCompletion(System.Boolean replace = false)`

Confirms the selected candidate through the virtual insertion hook.

**Param `replace`:** Replaces the following identifier instead of merging matching suffix text.

<a id="member-aab8392e60ae"></a>

### ConvertIndent(System.Int32, System.Int32)

`public System.Void ConvertIndent(System.Int32 fromLine = -1, System.Int32 toLine = -1)`

Converts leading indentation of a logical-line interval to the configured tab/space policy.

**Param `fromLine`:** Existing start or -1 for document start.

**Param `toLine`:** Existing inclusive end or -1 for document end.

<a id="member-fca555aa3740"></a>

### CreateCodeRegion()

`public System.Void CreateCodeRegion()`

Wraps selected lines in named comment region tags, folds them and selects the new region name.

<a id="member-ed362dd90c35"></a>

### CreateSceneInstanceFactory()

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-969369f16549"></a>

### DeleteLines()

`public System.Void DeleteLines()`

Deletes each selected/caret logical line once.

<a id="member-48c9231834d5"></a>

### Dispose(System.Boolean)

`protected override System.Void Dispose(System.Boolean disposing)`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-7cc2d77bb002"></a>

### DoIndent()

`public System.Void DoIndent()`

Indents selections, or inserts indentation at each caret.

<a id="member-978c68e3a587"></a>

### DuplicateLines()

`public System.Void DuplicateLines()`

Duplicates complete selected/caret lines and advances their caret identities.

<a id="member-4c6c80934053"></a>

### DuplicateSelection()

`public System.Void DuplicateSelection()`

Duplicates selected text, or complete lines for unselected carets.

<a id="member-67ec1cc9121d"></a>

### FoldAllLines()

`public System.Void FoldAllLines()`

Folds every supported block.

<a id="member-1af3762ae212"></a>

### FoldLine(System.Int32)

`public System.Void FoldLine(System.Int32 line)`

Folds a supported block and moves carets from hidden descendants onto its header.

**Param `line`:** Existing line.

<a id="member-e87e64e909f8"></a>

### GetAutoBraceCompletionCloseKey(System.String)

`public System.String GetAutoBraceCompletionCloseKey(System.String openKey)`

Returns a matching closing key or an empty string.

**Param `openKey`:** Exact key.

**Returns:** Closing key.

<a id="member-100c0830bd5d"></a>

### GetBookmarkedLines()

`public System.Int32[] GetBookmarkedLines()`

Returns an ascending bookmark array.

**Returns:** Logical indices.

<a id="member-27b01cf4aeb5"></a>

### GetBreakpointedLines()

`public System.Int32[] GetBreakpointedLines()`

Returns an independent ascending breakpoint line array.

**Returns:** Logical indices.

<a id="member-c5e8a8bb16f6"></a>

### GetCodeCompletionOption(System.Int32)

`public Electron2D.CodeCompletionOption GetCodeCompletionOption(System.Int32 index)`

Gets a stable borrowed immutable visible candidate.

**Param `index`:** Existing visible index.

**Returns:** Candidate.

<a id="member-c23a8a95f671"></a>

### GetCodeCompletionOptions()

`public Electron2D.CodeCompletionOption[] GetCodeCompletionOptions()`

Gets an independent array of immutable visible candidates.

**Returns:** Borrowed candidate elements.

<a id="member-f8cf96f98e66"></a>

### GetCodeCompletionSelectedIndex()

`public System.Int32 GetCodeCompletionSelectedIndex()`

Gets the selected visible candidate index.

**Returns:** Index, or -1 when inactive.

<a id="member-8d4924099c58"></a>

### GetCodeRegionEndTag()

`public System.String GetCodeRegionEndTag()`

Returns the source region end tag.

**Returns:** Endregion initially.

<a id="member-788f3ac2fdaf"></a>

### GetCodeRegionStartTag()

`public System.String GetCodeRegionStartTag()`

Returns the source region start tag.

**Returns:** Region initially.

<a id="member-dc92f282a075"></a>

### GetDelimiterEndKey(System.Int32)

`public System.String GetDelimiterEndKey(System.Int32 delimiterIndex)`

Returns the combined ordered delimiter end key.

**Param `delimiterIndex`:** Existing combined index.

**Returns:** Source key.

<a id="member-6bcc0a65cad0"></a>

### GetDelimiterEndPosition(System.Int32, System.Int32)

`public Electron2D.Vector2 GetDelimiterEndPosition(System.Int32 line, System.Int32 column)`

Returns the delimiter end boundary after its closing key, or line-length plus one for line-only.

**Param `line`:** Existing line.

**Param `column`:** Scalar position.

**Returns:** Column/line, or (-1,-1) if unavailable/unclosed.

<a id="member-b8a46bc8f649"></a>

### GetDelimiterStartKey(System.Int32)

`public System.String GetDelimiterStartKey(System.Int32 delimiterIndex)`

Returns the combined ordered delimiter start key.

**Param `delimiterIndex`:** Existing combined index.

**Returns:** Source key.

<a id="member-e7929ce04cec"></a>

### GetDelimiterStartPosition(System.Int32, System.Int32)

`public Electron2D.Vector2 GetDelimiterStartPosition(System.Int32 line, System.Int32 column)`

Returns the delimiter start boundary one scalar after its opening key begins.

**Param `line`:** Existing line.

**Param `column`:** Scalar position.

**Returns:** Column/line, or (-1,-1).

<a id="member-b0318a9be84a"></a>

### GetExecutingLines()

`public System.Int32[] GetExecutingLines()`

Returns an ascending executing-line array.

**Returns:** Logical indices.

<a id="member-90a5048030c0"></a>

### GetFoldedLines()

`public System.Int32[] GetFoldedLines()`

Returns an independent ascending folded-header array.

**Returns:** Logical indices.

<a id="member-6d4a3c693e04"></a>

### GetPropertyDescriptors()

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-6a46e45ac18c"></a>

### GetTextForCodeCompletion()

`public System.String GetTextForCodeCompletion()`

Returns cursor-marked text at the primary caret.

**Returns:** Source with U+FFFF.

<a id="member-7e3383b3b446"></a>

### GetTextForSymbolLookup()

`public System.String GetTextForSymbolLookup()`

Returns cursor-marked text at the current symbol-lookup position.

**Returns:** Source with U+FFFF.

<a id="member-6faf37eb4af0"></a>

### GetTextWithCursorChar(System.Int32, System.Int32)

`public System.String GetTextWithCursorChar(System.Int32 line, System.Int32 column)`

Returns the complete text with U+FFFF at a logical/scalar coordinate.

**Param `line`:** Existing line.

**Param `column`:** Scalar column.

**Returns:** Cursor-marked text.

<a id="member-346c0e6a3b2a"></a>

### HasAutoBraceCompletionCloseKey(System.String)

`public System.Boolean HasAutoBraceCompletionCloseKey(System.String closeKey)`

Reports a closing brace key.

**Param `closeKey`:** Exact key.

**Returns:** Whether present.

<a id="member-0fd90e5f4817"></a>

### HasAutoBraceCompletionOpenKey(System.String)

`public System.Boolean HasAutoBraceCompletionOpenKey(System.String openKey)`

Reports an opening brace key.

**Param `openKey`:** Exact key.

**Returns:** Whether present.

<a id="member-c384dd9855a9"></a>

### HasCommentDelimiter(System.String)

`public System.Boolean HasCommentDelimiter(System.String startKey)`

Reports an exact comment start key.

**Param `startKey`:** Exact key.

**Returns:** Whether present.

<a id="member-2bb69350de64"></a>

### HasStringDelimiter(System.String)

`public System.Boolean HasStringDelimiter(System.String startKey)`

Reports an exact string start key.

**Param `startKey`:** Exact key.

**Returns:** Whether present.

<a id="member-ec1ae004afef"></a>

### IndentLines()

`public System.Void IndentLines()`

Indents each selected/caret line once, omitting empty lines.

<a id="member-f63c1464adda"></a>

### IsInComment(System.Int32, System.Int32)

`public System.Int32 IsInComment(System.Int32 line, System.Int32 column = -1)`

Returns a combined comment delimiter index at a scalar position, or for a wholly covered line.

**Param `line`:** Existing line.

**Param `column`:** Scalar position, or -1 for whole line.

**Returns:** Combined index or -1.

<a id="member-c188fa01375c"></a>

### IsInString(System.Int32, System.Int32)

`public System.Int32 IsInString(System.Int32 line, System.Int32 column = -1)`

Returns a combined string delimiter index at a scalar position, or for a wholly covered line.

**Param `line`:** Existing line.

**Param `column`:** Scalar position, or -1 for whole line.

**Returns:** Combined index or -1.

<a id="member-ab9915259847"></a>

### IsLineBookmarked(System.Int32)

`public System.Boolean IsLineBookmarked(System.Int32 line)`

Reports a bookmark.

**Param `line`:** Existing line.

**Returns:** Marker state.

<a id="member-103bc9e9e5f3"></a>

### IsLineBreakpointed(System.Int32)

`public System.Boolean IsLineBreakpointed(System.Int32 line)`

Reports a breakpoint marker.

**Param `line`:** Existing line.

**Returns:** Marker state.

<a id="member-d8ef9af78c0e"></a>

### IsLineCodeRegionEnd(System.Int32)

`public System.Boolean IsLineCodeRegionEnd(System.Int32 line)`

Reports a single-line-comment code-region end.

**Param `line`:** Existing line.

**Returns:** Whether the configured tag begins this line.

<a id="member-252702b8e7fa"></a>

### IsLineCodeRegionStart(System.Int32)

`public System.Boolean IsLineCodeRegionStart(System.Int32 line)`

Reports a single-line-comment code-region start.

**Param `line`:** Existing line.

**Returns:** Whether the configured tag begins this line.

<a id="member-4fbe443d27fa"></a>

### IsLineExecuting(System.Int32)

`public System.Boolean IsLineExecuting(System.Int32 line)`

Reports an executing-line marker.

**Param `line`:** Existing line.

**Returns:** Marker state.

<a id="member-2e7c6db8f045"></a>

### IsLineFolded(System.Int32)

`public System.Boolean IsLineFolded(System.Int32 line)`

Reports an explicit folded header.

**Param `line`:** Existing line.

**Returns:** Fold state.

<a id="member-9aab08cb1a0b"></a>

### JoinLines(System.String)

`public System.Void JoinLines(System.String lineEnding = " ")`

Joins selected/caret lines with following lines, trimming connecting whitespace.

**Param `lineEnding`:** Nonnull inserted separator.

<a id="member-0a3bfff54ad1"></a>

### MoveLinesDown()

`public System.Void MoveLinesDown()`

Moves selected/caret line ranges downward with their gutters and backgrounds.

<a id="member-1a9ff84df669"></a>

### MoveLinesUp()

`public System.Void MoveLinesUp()`

Moves selected/caret line ranges upward with their gutters and backgrounds.

<a id="member-84f0bb08f710"></a>

### OnBackspace(System.Int32)

`protected override System.Void OnBackspace(System.Int32 caretIndex)`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-555fa97221ff"></a>

### OnConfirmCodeCompletion(System.Boolean)

`protected virtual System.Void OnConfirmCodeCompletion(System.Boolean replace)`

Inserts the active candidate as one undo group and cancels completion.

**Param `replace`:** Replaces the following identifier.

<a id="member-9aba2b2d983e"></a>

### OnCut(System.Int32)

`protected override System.Void OnCut(System.Int32 caretIndex)`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-02ae1707140d"></a>

### OnFilterCodeCompletionCandidates(Electron2D.CodeCompletionOption[])

`protected virtual Electron2D.CodeCompletionOption[] OnFilterCodeCompletionCandidates(Electron2D.CodeCompletionOption[] candidates)`

Filters application candidates into their visible order. An override owns the complete filter and insertion prefix is empty.

**Param `candidates`:** Independent array of borrowed immutable candidates.

**Returns:** Non-null candidate array without null elements.

<a id="member-f332f27d6093"></a>

### OnGUIInput(Electron2D.InputEvent)

`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-11260b611404"></a>

### OnHandleUnicodeInput(System.Int32, System.Int32)

`protected override System.Void OnHandleUnicodeInput(System.Int32 unicodeChar, System.Int32 caretIndex)`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-25c8ccf64a0a"></a>

### OnNotification(System.Int32)

`protected override System.Void OnNotification(System.Int32 what)`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-e6705f63aa4f"></a>

### OnPaste(System.Int32)

`protected override System.Void OnPaste(System.Int32 caretIndex)`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-c7d8111a0f66"></a>

### OnRequestCodeCompletion(System.Boolean)

`protected virtual System.Void OnRequestCodeCompletion(System.Boolean force)`

Requests candidate production after validating ordinary query context.

**Param `force`:** Bypasses word/prefix checks.

<a id="member-da5d7ae0608e"></a>

### OnTextInput(System.String)

`protected override System.Void OnTextInput(System.String text)`

Overrides the inherited owner lifecycle contract; see TextEdit.

<a id="member-b9b2130f012d"></a>

### RemoveCommentDelimiter(System.String)

`public System.Void RemoveCommentDelimiter(System.String startKey)`

Removes a comment delimiter by start key.

**Param `startKey`:** Exact key.

<a id="member-608c29f53d4e"></a>

### RemoveStringDelimiter(System.String)

`public System.Void RemoveStringDelimiter(System.String startKey)`

Removes a string delimiter by start key.

**Param `startKey`:** Exact key.

<a id="member-1a623fb6d2b5"></a>

### RequestCodeCompletion(System.Boolean)

`public System.Void RequestCodeCompletion(System.Boolean force = false)`

Requests application completion using the typed virtual request hook.

**Param `force`:** Bypasses ordinary prefix/context checks.

<a id="member-9a648d13d099"></a>

### SetCodeCompletionSelectedIndex(System.Int32)

`public System.Void SetCodeCompletionSelectedIndex(System.Int32 index)`

Changes selection while completion is active.

**Param `index`:** Existing visible candidate index.

<a id="member-1803b868b723"></a>

### SetCodeHint(System.String)

`public System.Void SetCodeHint(System.String codeHint)`

Sets source code-hint text; empty clears it. U+FFFF toggles highlighted argument spans.

**Param `codeHint`:** Nonnull text.

<a id="member-6e3efa6f6cd4"></a>

### SetCodeHintDrawBelow(System.Boolean)

`public System.Void SetCodeHintDrawBelow(System.Boolean drawBelow)`

Chooses whether the code hint is below or above the primary caret.

**Param `drawBelow`:** True draws below.

<a id="member-b7212ea9b662"></a>

### SetCodeRegionTags(System.String, System.String)

`public System.Void SetCodeRegionTags(System.String start = "region", System.String end = "endregion")`

Sets source region tags without a comment prefix.

**Param `start`:** Nonempty start tag.

**Param `end`:** Nonempty end tag.

<a id="member-5eee88d5cc83"></a>

### SetLineAsBookmarked(System.Int32, System.Boolean)

`public System.Void SetLineAsBookmarked(System.Int32 line, System.Boolean bookmarked)`

Sets a runtime bookmark.

**Param `line`:** Existing line.

**Param `bookmarked`:** Desired state.

<a id="member-11e971e27204"></a>

### SetLineAsBreakpoint(System.Int32, System.Boolean)

`public System.Void SetLineAsBreakpoint(System.Int32 line, System.Boolean breakpointed)`

Sets a runtime breakpoint marker.

**Param `line`:** Existing line.

**Param `breakpointed`:** Desired state.

<a id="member-75aa87bd09d9"></a>

### SetLineAsExecuting(System.Int32, System.Boolean)

`public System.Void SetLineAsExecuting(System.Int32 line, System.Boolean executing)`

Sets an executing-line marker.

**Param `line`:** Existing line.

**Param `executing`:** Desired state.

<a id="member-8fff85b1e045"></a>

### SetSymbolLookupWordAsValid(System.Boolean)

`public System.Void SetSymbolLookupWordAsValid(System.Boolean valid)`

Marks the most recently requested pointer symbol as eligible for lookup.

**Param `valid`:** Validation result.

<a id="member-3dd44a122500"></a>

### ToggleFoldableLine(System.Int32)

`public System.Void ToggleFoldableLine(System.Int32 line)`

Toggles a supported folded header.

**Param `line`:** Existing line.

<a id="member-1f9b6ad8e4fb"></a>

### ToggleFoldableLinesAtCarets()

`public System.Void ToggleFoldableLinesAtCarets()`

Toggles each distinct caret header.

<a id="member-b0d458f92a4b"></a>

### UnfoldAllLines()

`public System.Void UnfoldAllLines()`

Clears all folds and restores every logical line.

<a id="member-9987d0176747"></a>

### UnfoldLine(System.Int32)

`public System.Void UnfoldLine(System.Int32 line)`

Unfolds the containing folded header while preserving nested folded blocks.

**Param `line`:** Existing header or hidden line.

<a id="member-4101f5190d49"></a>

### UnindentLines()

`public System.Void UnindentLines()`

Removes one indentation stop from each selected/caret line.

<a id="member-eacfde192eb5"></a>

### UpdateCodeCompletionOptions(System.Boolean)

`public System.Void UpdateCodeCompletionOptions(System.Boolean force)`

Replaces the current candidate sources with the submitted queue and filters them.

**Param `force`:** Allows presentation with an empty prefix.

## Properties

| Declaration | Contract |
| --- | --- |
| `public System.Boolean AutoBraceCompletionEnabled { get; set; }` | Gets or sets automatic paired insertion, closing-key skip and paired backspace. |
| `public System.Boolean AutoBraceCompletionHighlightMatching { get; set; }` | Gets or sets matching and mismatched brace underlines. |
| `public System.Collections.Generic.IReadOnlyDictionary<System.String, System.String> AutoBraceCompletionPairs { get; set; }` | Gets or sets independent brace-key mappings. |
| `public System.Boolean CodeCompletionEnabled { get; set; }` | Gets or sets automatic completion requests after committed text. |
| `public System.String[] CodeCompletionPrefixes { get; set; }` | Gets or sets copied single-scalar completion prefixes. |
| `public System.String[] DelimiterComments { get; set; }` | Gets or sets copied comment delimiter descriptions. |
| `public System.String[] DelimiterStrings { get; set; }` | Gets or sets copied string delimiter descriptions encoded as start and optional end keys. |
| `public System.Boolean GuttersDrawBookmarks { get; set; }` | Gets or sets bookmark icons in the main gutter. |
| `public System.Boolean GuttersDrawBreakpointsGutter { get; set; }` | Gets or sets breakpoint icons and pointer toggling in the main gutter. |
| `public System.Boolean GuttersDrawExecutingLines { get; set; }` | Gets or sets execution markers in the main gutter. |
| `public System.Boolean GuttersDrawFoldGutter { get; set; }` | Gets or sets folding icons and pointer toggling. |
| `public System.Boolean GuttersDrawLineNumbers { get; set; }` | Gets or sets logical line-number presentation. |
| `public System.Int32 GuttersLineNumbersMinDigits { get; set; }` | Gets or sets the minimum number of line-number digits. |
| `public System.Boolean GuttersZeroPadLineNumbers { get; set; }` | Gets or sets zero padding instead of space padding in line numbers. |
| `public System.Boolean IndentAutomatic { get; set; }` | Gets or sets indentation inheritance and configured prefix expansion on newlines. |
| `public System.String[] IndentAutomaticPrefixes { get; set; }` | Gets or sets copied single-scalar automatic indentation prefixes. |
| `public System.Int32 IndentSize { get; set; }` | Gets or sets the positive indentation/tab width. |
| `public System.Boolean IndentUseSpaces { get; set; }` | Gets or sets space indentation instead of tab indentation. |
| `public System.Boolean LineFolding { get; set; }` | Gets or sets code-region, delimiter-block and indentation folding. |
| `public System.Int32[] LineLengthGuidelines { get; set; }` | Gets or sets copied nonnegative scalar guideline columns. |
| `public System.Boolean SymbolLookupOnClick { get; set; }` | Gets or sets validated symbol lookup on command-modified pointer clicks. |
| `public System.Boolean SymbolTooltipOnHover { get; set; }` | Gets or sets symbol hover requests after the project tooltip delay. |

## Properties descriptions

<a id="member-4b4ab51e9473"></a>

### AutoBraceCompletionEnabled

`public System.Boolean AutoBraceCompletionEnabled { get; set; }`

Gets or sets automatic paired insertion, closing-key skip and paired backspace.

**Value:** False initially.

<a id="member-ea419659bfac"></a>

### AutoBraceCompletionHighlightMatching

`public System.Boolean AutoBraceCompletionHighlightMatching { get; set; }`

Gets or sets matching and mismatched brace underlines.

**Value:** False initially.

<a id="member-f0d356b2599e"></a>

### AutoBraceCompletionPairs

`public System.Collections.Generic.IReadOnlyDictionary<System.String, System.String> AutoBraceCompletionPairs { get; set; }`

Gets or sets independent brace-key mappings.

**Value:** Parentheses, brackets, braces and quotes initially.

<a id="member-ec95d75c2f7e"></a>

### CodeCompletionEnabled

`public System.Boolean CodeCompletionEnabled { get; set; }`

Gets or sets automatic completion requests after committed text.

**Value:** False initially.

<a id="member-147c3a81bb17"></a>

### CodeCompletionPrefixes

`public System.String[] CodeCompletionPrefixes { get; set; }`

Gets or sets copied single-scalar completion prefixes.

**Value:** Empty initially.

<a id="member-e0c129a19578"></a>

### DelimiterComments

`public System.String[] DelimiterComments { get; set; }`

Gets or sets copied comment delimiter descriptions.

**Value:** Empty initially.

<a id="member-d53a28ca1a07"></a>

### DelimiterStrings

`public System.String[] DelimiterStrings { get; set; }`

Gets or sets copied string delimiter descriptions encoded as start and optional end keys.

**Value:** Single and double quotes initially.

<a id="member-8afd423245c0"></a>

### GuttersDrawBookmarks

`public System.Boolean GuttersDrawBookmarks { get; set; }`

Gets or sets bookmark icons in the main gutter.

**Value:** False initially.

<a id="member-f290b94d2f00"></a>

### GuttersDrawBreakpointsGutter

`public System.Boolean GuttersDrawBreakpointsGutter { get; set; }`

Gets or sets breakpoint icons and pointer toggling in the main gutter.

**Value:** False initially.

<a id="member-b82cf3ab2619"></a>

### GuttersDrawExecutingLines

`public System.Boolean GuttersDrawExecutingLines { get; set; }`

Gets or sets execution markers in the main gutter.

**Value:** False initially.

<a id="member-a02b3c26fa30"></a>

### GuttersDrawFoldGutter

`public System.Boolean GuttersDrawFoldGutter { get; set; }`

Gets or sets folding icons and pointer toggling.

**Value:** False initially.

<a id="member-45eabd2ee1a6"></a>

### GuttersDrawLineNumbers

`public System.Boolean GuttersDrawLineNumbers { get; set; }`

Gets or sets logical line-number presentation.

**Value:** False initially.

<a id="member-3a27e420a1fe"></a>

### GuttersLineNumbersMinDigits

`public System.Int32 GuttersLineNumbersMinDigits { get; set; }`

Gets or sets the minimum number of line-number digits.

**Value:** Three initially.

<a id="member-3f691411d5b0"></a>

### GuttersZeroPadLineNumbers

`public System.Boolean GuttersZeroPadLineNumbers { get; set; }`

Gets or sets zero padding instead of space padding in line numbers.

**Value:** False initially.

<a id="member-b46bde7bfb6a"></a>

### IndentAutomatic

`public System.Boolean IndentAutomatic { get; set; }`

Gets or sets indentation inheritance and configured prefix expansion on newlines.

**Value:** False initially.

<a id="member-a9112aff2531"></a>

### IndentAutomaticPrefixes

`public System.String[] IndentAutomaticPrefixes { get; set; }`

Gets or sets copied single-scalar automatic indentation prefixes.

**Value:** Colon and opening braces initially.

<a id="member-d26ec3a9fe41"></a>

### IndentSize

`public System.Int32 IndentSize { get; set; }`

Gets or sets the positive indentation/tab width.

**Value:** Four initially.

<a id="member-153e91582682"></a>

### IndentUseSpaces

`public System.Boolean IndentUseSpaces { get; set; }`

Gets or sets space indentation instead of tab indentation.

**Value:** False initially.

<a id="member-b3d22415dda4"></a>

### LineFolding

`public System.Boolean LineFolding { get; set; }`

Gets or sets code-region, delimiter-block and indentation folding.

**Value:** False initially.

<a id="member-665ccdf9a8d7"></a>

### LineLengthGuidelines

`public System.Int32[] LineLengthGuidelines { get; set; }`

Gets or sets copied nonnegative scalar guideline columns.

**Value:** Empty initially.

<a id="member-b09fec50754a"></a>

### SymbolLookupOnClick

`public System.Boolean SymbolLookupOnClick { get; set; }`

Gets or sets validated symbol lookup on command-modified pointer clicks.

**Value:** False initially.

<a id="member-82c17a1ba803"></a>

### SymbolTooltipOnHover

`public System.Boolean SymbolTooltipOnHover { get; set; }`

Gets or sets symbol hover requests after the project tooltip delay.

**Value:** False initially.

## Lifecycle and verification

Attached widget operations require the scene owner thread and a live control. Mutating CodeEdit configuration inside its overlay draw scope is rejected. Null/invalid keys, indices, enum values, required nonfinite colors and invalid ownership are rejected before changes. See [Code authoring](../components/code-authoring.md) for executable tests and backend/acceptance boundaries; [coverage](../coverage/classes/CodeEdit.md) records the full owning reference family.

The inherited cursor hook selects PointingHand only for a validated lookup symbol; validation never writes the stored MouseDefaultCursorShape setting. Code overlay drawing enters the shared TextEdit mutation guard, so inherited document operations also reject mutation from custom completion StyleBox callbacks before changing the document.

`protected override Electron2D.CursorShape OnGetCursorShape(Electron2D.Vector2 atPosition)` projects the ordinary Control cursor hook for the current symbol.
