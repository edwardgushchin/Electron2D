# TextEdit

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.TextEdit`. **Source:** [source](../../src/Scene/GUI/TextEdit.cs). **Component:** [Multiline editing](../components/multiline-editing.md).

## Behavior

A multiline Unicode editor using logical line indices and Unicode scalar columns. LF separates lines; CR is removed on input. One logical line and one primary caret always remain. Programmatic buffer operations, selections and user input share the same document/history. Explicit grouped operations form one undo step; version tags follow undo/redo. The configured history limit is sampled at construction. Empty-selection copy/cut uses whole lines, single-caret linewise paste preserves line structure, and equally sized multicaret clipboard lines distribute in document order.

Text shaping uses the existing font, Unicode/BIDI, grapheme and paragraph layout. Wrapped rows, gutters, pixel/column queries, caret viewport fitting and both owned scrollbars consume that layout. Read-only mode suppresses user mutation while allowing typed programmatic authoring and selection/copy. Context menus use the shared TextMenuAction identity, with direction and Unicode submenus. Pointer, keyboard, committed strings and IME preedit enter through ordinary Control/Viewport routing. Font/texture invalidation is polled on the owner; required child disposal is guarded. Themes include focus, normal/read-only, outlines, caret/selection/search, wrap margin, font and space/tab markers.

Assigning SyntaxHighlighter borrows one dedicated resource. Sharing it between live editors is rejected. Packed scenes restore an independent owned duplicate for each editor; configuration persists but carets, selections, gutters, history, callbacks and platform composition are runtime state. Source fonts and gutter icons are borrowed. Text/line/caret notifications coalesce on an attached scene; observers see committed history before notification. Editing, shaping, parsing and packing are cold operations. Prepared recording reuses scalar color and selection buffers.

Undo snapshots cost O(document size) per cold edit and are limited by TextEditUndoStackMaxSize; edit deltas would reduce the large-document ceiling. Minimap drawing is a scaled scalar overview, not font rasterization at tiny sizes. Native virtual keyboard, symbol picker, semantic services and editor authoring retain exact dependency gates in coverage. Tests exercise current Linux desktop behavior; physical IME and other targets are not inferred from those tests.

## Members

| Declaration | Contract |
| --- | --- |
| `public TextEdit()` | Creates an editable empty document with one caret, context menu and owned scrollbars. |
| `public event System.Action CaretChanged` | Occurs after caret/selection positions change. |
| `public event System.Action GutterAdded` | Occurs after a gutter is added. |
| `public event System.Action<System.Int32, System.Int32> GutterClicked` | Reports a clicked logical line and gutter index. |
| `public event System.Action GutterRemoved` | Occurs after a gutter is removed. |
| `public event System.Action<System.Int32, System.Int32> LinesEditedFrom` | Reports an inclusive affected logical line range. |
| `public event System.Action TextChanged` | Occurs after a coalesced change to document content. |
| `public event System.Action TextSet` | Occurs after replacing the complete document through Text. |
| `public System.Int32 AddCaret(System.Int32 line, System.Int32 column)` | Adds a distinct caret at a logical/scalar position. |
| `public System.Void AddCaretAtCarets(System.Boolean below)` | Adds a caret on the adjacent visible row for each current caret. |
| `public System.Void AddGutter(System.Int32 at = -1)` | Inserts a gutter and an empty cell on every line. |
| `public System.Void AddSelectionForNextOccurrence()` | Adds a selection for the next exact occurrence of the current selected text or word. |
| `public System.Void AdjustViewportToCaret(System.Int32 caretIndex = 0)` | Scrolls only as needed to reveal a caret. |
| `public System.Void ApplyIME()` | Commits current composition into the document. |
| `public System.Void Backspace(System.Int32 caretIndex = -1)` | Deletes text preceding one or all carets through the typed hook. |
| `public System.Void BeginComplexOperation()` | Starts a nested group of edits represented by one undo step. |
| `public System.Void BeginMulticaretEdit()` | Starts a nested multicaret operation, deferring overlap merging. |
| `public System.Void CancelIME()` | Discards uncommitted composition. |
| `public System.Void CenterViewportToCaret(System.Int32 caretIndex = 0)` | Centers the viewport vertically on a caret. |
| `public System.Void Clear()` | Clears text, secondary carets and undo history. |
| `public System.Void ClearUndoHistory()` | Clears undo/redo history while retaining current text. |
| `public System.Void CollapseCarets(System.Int32 fromLine, System.Int32 fromColumn, System.Int32 toLine, System.Int32 toColumn, System.Boolean inclusive = false)` | Moves carets within an ordered interval to its start, retaining affected selection anchors. |
| `public System.Void Copy(System.Int32 caretIndex = -1)` | Copies the selections, or the current logical line when empty-selection copying is enabled. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` |  |
| `public System.Void Cut(System.Int32 caretIndex = -1)` | Cuts selected text or whole lines when empty-selection copying is enabled. |
| `public System.Void DeleteSelection(System.Int32 caretIndex = -1)` | Deletes one or all selection intervals as one undo step. |
| `public System.Void Deselect(System.Int32 caretIndex = -1)` | Clears one or all selections without moving carets. |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `public System.Void EndAction()` | Ends a consecutive edit action and commits its undo step. |
| `public System.Void EndComplexOperation()` | Ends one nested undo group. |
| `public System.Void EndMulticaretEdit()` | Ends a multicaret operation and merges overlapping selections at the outer boundary. |
| `public System.Int32 GetCaretColumn(System.Int32 caretIndex = 0)` | Returns a caret's scalar column. |
| `public System.Int32 GetCaretCount()` | Returns the current number of carets. |
| `public Electron2D.Vector2 GetCaretDrawPos(System.Int32 caretIndex = 0)` | Returns the configured caret's local pixel position. |
| `public System.Int32 GetCaretLine(System.Int32 caretIndex = 0)` | Returns a caret's logical line. |
| `public System.Int32 GetCaretWrapIndex(System.Int32 caretIndex = 0)` | Returns the caret's current wrap row. |
| `public System.Int32 GetFirstNonWhitespaceColumn(System.Int32 line)` | Returns the first non-space/non-tab scalar column. |
| `public System.Int32 GetFirstVisibleLine()` | Returns the first viewport logical line. |
| `public System.Int32 GetGutterCount()` | Returns the gutter count. |
| `public System.String GetGutterName(System.Int32 gutter)` | Returns a gutter's source name. |
| `public Electron2D.TextEdit.GutterType GetGutterType(System.Int32 gutter)` | Returns the gutter's rendering kind. |
| `public System.Int32 GetGutterWidth(System.Int32 gutter)` | Returns the configured pixel width. |
| `public Electron2D.HScrollBar GetHScrollBar()` | Returns the stable borrowed horizontal scrollbar. |
| `public System.Int32 GetIndentLevel(System.Int32 line)` | Returns indentation width in scalar spaces using configured tab stops. |
| `public System.Int32 GetLastFullVisibleLine()` | Returns the last completely visible logical line. |
| `public System.Int32 GetLastFullVisibleLineWrapIndex()` | Returns the last completely visible wrap index. |
| `public System.Int32 GetLastUnhiddenLine()` | Returns the last unhidden logical line. |
| `public System.String GetLine(System.Int32 line)` | Returns one logical line without a newline. |
| `public Electron2D.Color GetLineBackgroundColor(System.Int32 line)` | Returns a line's background color. |
| `public Electron2D.Vector2i GetLineColumnAtPos(Electron2D.Vector2i position, System.Boolean clampLine = true, System.Boolean clampColumn = true)` | Returns the logical column/line nearest a local pixel point. |
| `public System.Int32 GetLineCount()` | Returns the number of logical lines, including an empty final line. |
| `public Electron2D.Texture GetLineGutterIcon(System.Int32 line, System.Int32 gutter)` | Gets a live borrowed gutter icon. |
| `public Electron2D.Color GetLineGutterItemColor(System.Int32 line, System.Int32 gutter)` | Gets a cell's text/icon color. |
| `public T GetLineGutterMetadata<T>(System.Int32 line, System.Int32 gutter)` | Gets runtime gutter metadata of the exact generic type. |
| `public System.String GetLineGutterText(System.Int32 line, System.Int32 gutter)` | Gets a cell's source text. |
| `public System.Int32 GetLineHeight()` | Returns the pixel row height. |
| `public Electron2D.Vector2i[] GetLineRangesFromCarets(System.Boolean onlySelections = false, System.Boolean mergeAdjacent = true)` | Returns selected/caret logical-line intervals in ascending order. |
| `public System.Int32 GetLineWidth(System.Int32 line, System.Int32 wrapIndex = -1)` | Returns measured pixel width of a logical line or its wrap row. |
| `public System.String GetLineWithIME(System.Int32 line)` | Returns a logical line with each caret's uncommitted composition inserted. |
| `public System.Int32 GetLineWrapCount(System.Int32 line)` | Returns the number of additional wrap rows. |
| `public System.Int32 GetLineWrapIndexAtColumn(System.Int32 line, System.Int32 column)` | Returns the wrap row containing a scalar column. |
| `public System.String[] GetLineWrappedText(System.Int32 line)` | Returns copied text for each wrap row. |
| `public Electron2D.Vector2 GetLocalMousePos()` | Returns the current local mouse coordinates, mirrored for RTL layout. |
| `public Electron2D.PopupMenu GetMenu()` | Returns the stable borrowed context menu. |
| `public System.Int32 GetMinimapLineAtPos(Electron2D.Vector2i position)` | Returns logical line nearest a minimap local point. |
| `public System.Int32 GetMinimapVisibleLines()` | Returns visual rows represented by the minimap. |
| `public System.Int32 GetNextCompositeCharacterColumn(System.Int32 line, System.Int32 column)` | Returns logical/scalar grapheme successor. |
| `public Electron2D.Vector2i GetNextVisibleLineIndexOffsetFrom(System.Int32 line, System.Int32 wrapIndex, System.Int32 visibleAmount)` | Returns wrap/logical offsets for signed visual-row traversal. |
| `public System.Int32 GetNextVisibleLineOffsetFrom(System.Int32 line, System.Int32 visibleAmount)` | Returns logical-line offset needed to traverse unhidden lines. |
| `public Electron2D.Vector2i GetPosAtLineColumn(System.Int32 line, System.Int32 column)` | Returns the local pixel coordinate of a logical scalar position. |
| `public System.Int32 GetPreviousCompositeCharacterColumn(System.Int32 line, System.Int32 column)` | Returns logical/scalar grapheme predecessor. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` |  |
| `public Electron2D.Rect2i GetRectAtLineColumn(System.Int32 line, System.Int32 column)` | Returns the local character/caret rectangle. |
| `public System.Int32 GetSavedVersion()` | Returns the last explicitly saved version. |
| `public System.Double GetScrollPosForLine(System.Int32 line, System.Int32 wrapIndex = 0)` | Returns the vertical scroll position corresponding to a logical/wrapped row. |
| `public System.String GetSelectedText(System.Int32 caretIndex = -1)` | Returns one selection or all selections joined by newline in document order. |
| `public System.Int32 GetSelectionAtLineColumn(System.Int32 line, System.Int32 column, System.Boolean includeEdges = true, System.Boolean onlySelections = true)` | Returns the selection containing a scalar position. |
| `public System.Int32 GetSelectionFromColumn(System.Int32 caretIndex = 0)` | Returns the ordered selection start column. |
| `public System.Int32 GetSelectionFromLine(System.Int32 caretIndex = 0)` | Returns the ordered selection start line. |
| `public Electron2D.TextEdit.SelectionMode GetSelectionMode()` | Returns the active selection interaction. |
| `public System.Int32 GetSelectionOriginColumn(System.Int32 caretIndex = 0)` | Returns the selection origin's scalar column. |
| `public System.Int32 GetSelectionOriginLine(System.Int32 caretIndex = 0)` | Returns the selection origin's line. |
| `public System.Int32 GetSelectionToColumn(System.Int32 caretIndex = 0)` | Returns the ordered selection end column. |
| `public System.Int32 GetSelectionToLine(System.Int32 caretIndex = 0)` | Returns the ordered selection end line. |
| `public System.Int32[] GetSortedCarets(System.Boolean includeIgnoredCarets = false)` | Returns caret indices in ascending selection-start/document order. |
| `public System.Int32 GetTabSize()` | Gets the scalar-column tab stop size. |
| `public System.Int32 GetTotalGutterWidth()` | Gets the sum of currently drawn gutter widths. |
| `public System.Int32 GetTotalVisibleLineCount()` | Returns the total unhidden visual row count. |
| `public Electron2D.VScrollBar GetVScrollBar()` | Returns the stable borrowed vertical scrollbar. |
| `public System.Int32 GetVersion()` | Returns the current content version. |
| `public System.Int32 GetVisibleLineCount()` | Returns the available viewport row count. |
| `public System.Int32 GetVisibleLineCountInRange(System.Int32 fromLine, System.Int32 toLine)` | Returns visual rows in an inclusive logical line interval. |
| `public System.String GetWordAtPos(Electron2D.Vector2 position)` | Returns the word nearest a local pixel point. |
| `public System.String GetWordUnderCaret(System.Int32 caretIndex = -1)` | Returns one caret's word or the primary caret's word. |
| `public System.Boolean HasIMEText()` | Returns whether an IME composition is active. |
| `public System.Boolean HasRedo()` | Returns whether an undone state is available. |
| `public System.Boolean HasSelection(System.Int32 caretIndex = -1)` | Reports whether one or any caret owns selected text. |
| `public System.Boolean HasUndo()` | Returns whether a prior state is available. |
| `public System.Void InsertLineAt(System.Int32 line, System.String text)` | Inserts a logical line before an existing index or at the document end. |
| `public System.Void InsertText(System.String text, System.Int32 line, System.Int32 column, System.Boolean beforeSelectionBegin = true, System.Boolean beforeSelectionEnd = false)` | Inserts text at an explicit logical position and adjusts all carets. |
| `public System.Void InsertTextAtCaret(System.String text, System.Int32 caretIndex = -1)` | Inserts at one caret or all carets, replacing their selections in reverse document order. |
| `public System.Boolean IsCaretAfterSelectionOrigin(System.Int32 caretIndex = 0)` | Returns whether a caret endpoint follows its selection origin. |
| `public System.Boolean IsCaretVisible(System.Int32 caretIndex = 0)` | Returns whether a caret is eligible for drawing inside the viewport. |
| `public System.Boolean IsDraggingCursor()` | Reports whether the pointer is extending a selection. |
| `public System.Boolean IsGutterClickable(System.Int32 gutter)` | Reports the gutter's shared click policy. |
| `public System.Boolean IsGutterDrawn(System.Int32 gutter)` | Reports gutter visibility. |
| `public System.Boolean IsGutterOverwritable(System.Int32 gutter)` | Reports the line-merge transfer policy. |
| `public System.Boolean IsInMulticaretEdit()` | Reports whether a grouped multicaret edit is active. |
| `public System.Boolean IsLineGutterClickable(System.Int32 line, System.Int32 gutter)` | Gets whether a cell-specific or gutter-wide policy permits clicks. |
| `public System.Boolean IsLineInViewport(System.Int32 line)` | Reports whether any visual row of a logical line intersects the viewport. |
| `public System.Boolean IsLineWrapped(System.Int32 line)` | Returns whether a line occupies multiple visual rows. |
| `public System.Boolean IsMenuVisible()` | Reports whether the context menu is presented. |
| `public System.Boolean IsMouseOverSelection(System.Boolean edges = true, System.Int32 caretIndex = -1)` | Reports whether the current pointer lies inside any nonempty selection. |
| `public System.Boolean IsOvertypeModeEnabled()` | Gets whether user typing replaces characters. |
| `public System.Void MenuOption(Electron2D.TextMenuAction option)` | Executes an editing, direction or Unicode control command. |
| `public System.Void MergeGutters(System.Int32 fromLine, System.Int32 toLine)` | Merges transferable nonempty gutter items between two logical lines. |
| `public System.Void MergeOverlappingCarets()` | Merges carets whose selection intervals overlap or whose positions coincide. |
| `public System.Boolean MulticaretEditIgnoreCaret(System.Int32 caretIndex)` | Reports whether an overlapping caret is suppressed in the current grouped edit. |
| `protected virtual System.Void OnBackspace(System.Int32 caretIndex)` | Handles an overridable backward deletion. |
| `protected override System.Boolean OnCanDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)` |  |
| `protected virtual System.Void OnCopy(System.Int32 caretIndex)` | Handles overridable clipboard copying. |
| `protected virtual System.Void OnCut(System.Int32 caretIndex)` | Handles overridable clipboard cutting. |
| `protected override System.Void OnDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)` |  |
| `protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)` |  |
| `protected override Electron2D.DragPayload OnGetDragData(Electron2D.Vector2 atPosition)` |  |
| `protected override Electron2D.Vector2 OnGetMinimumSize()` |  |
| `protected override System.String OnGetTooltip(Electron2D.Vector2 atPosition)` |  |
| `protected virtual System.Void OnHandleUnicodeInput(System.Int32 unicodeChar, System.Int32 caretIndex)` | Handles one typed Unicode scalar insertion. |
| `protected override System.Void OnIMECompositionChanged(System.String text, Electron2D.Vector2i selection)` |  |
| `protected override System.Void OnNotification(System.Int32 what)` |  |
| `protected virtual System.Void OnPaste(System.Int32 caretIndex)` | Handles overridable platform clipboard insertion. |
| `protected virtual System.Void OnPastePrimaryClipboard(System.Int32 caretIndex)` | Handles overridable primary clipboard insertion. |
| `protected override System.Void OnTextInput(System.String text)` |  |
| `public System.Void Paste(System.Int32 caretIndex = -1)` | Pastes the platform clipboard at the selected carets. |
| `public System.Void PastePrimaryClipboard(System.Int32 caretIndex = -1)` | Pastes the platform primary selection. |
| `public System.Void Redo()` | Restores the next undone content/caret state. |
| `public System.Void RemoveCaret(System.Int32 caret)` | Removes one caret, retaining a primary caret. |
| `public System.Void RemoveGutter(System.Int32 gutter)` | Removes a gutter and its line cells. |
| `public System.Void RemoveLineAt(System.Int32 line, System.Boolean moveCaretsDown = true)` | Removes a logical line while retaining at least one line. |
| `public System.Void RemoveSecondaryCarets()` | Removes all carets except the first. |
| `public System.Void RemoveText(System.Int32 fromLine, System.Int32 fromColumn, System.Int32 toLine, System.Int32 toColumn)` | Removes an ordered logical/scalar text interval. |
| `public Electron2D.Vector2i Search(System.String text, Electron2D.TextEdit.SearchFlags flags, System.Int32 fromLine, System.Int32 fromColumn)` | Searches logical lines once, wrapping around document boundaries. |
| `public System.Void Select(System.Int32 originLine, System.Int32 originColumn, System.Int32 caretLine, System.Int32 caretColumn, System.Int32 caretIndex = 0)` | Selects between an origin and caret endpoint. |
| `public System.Void SelectAll()` | Selects the complete document with the primary caret. |
| `public System.Void SelectWordUnderCaret(System.Int32 caretIndex = -1)` | Selects the word at one or all carets. |
| `public System.Void SetCaretColumn(System.Int32 column, System.Boolean adjustViewport = true, System.Int32 caretIndex = 0)` | Moves a caret to a clamped scalar column, respecting grapheme policy. |
| `public System.Void SetCaretLine(System.Int32 line, System.Boolean adjustViewport = true, System.Boolean canBeHidden = true, System.Int32 wrapIndex = 0, System.Int32 caretIndex = 0)` | Moves a caret to a clamped logical line and wrap row. |
| `public System.Void SetGutterClickable(System.Int32 gutter, System.Boolean clickable)` | Sets whether every gutter cell can publish a click. |
| `public System.Void SetGutterCustomDraw(System.Int32 column, System.Action<Electron2D.CanvasItem, System.Int32, System.Int32, Electron2D.Rect2> drawCallback)` | Sets a typed custom gutter draw callback invoked during this editor's canvas recording. |
| `public System.Void SetGutterDraw(System.Int32 gutter, System.Boolean draw)` | Sets visibility of a gutter. |
| `public System.Void SetGutterName(System.Int32 gutter, System.String name)` | Sets a gutter's source name. |
| `public System.Void SetGutterOverwritable(System.Int32 gutter, System.Boolean overwritable)` | Sets whether line-merging may transfer this gutter's content. |
| `public System.Void SetGutterType(System.Int32 gutter, Electron2D.TextEdit.GutterType type)` | Sets a gutter's rendering kind. |
| `public System.Void SetGutterWidth(System.Int32 gutter, System.Int32 width)` | Sets the nonnegative pixel width. |
| `public System.Void SetLine(System.Int32 line, System.String newText)` | Replaces one logical line, adjusting affected carets and preserving its gutters. |
| `public System.Void SetLineAsCenterVisible(System.Int32 line, System.Int32 wrapIndex = 0)` | Places a logical/wrapped row at the viewport center. |
| `public System.Void SetLineAsFirstVisible(System.Int32 line, System.Int32 wrapIndex = 0)` | Places a logical/wrapped row at the viewport start. |
| `public System.Void SetLineAsLastVisible(System.Int32 line, System.Int32 wrapIndex = 0)` | Places a logical/wrapped row at the viewport end. |
| `public System.Void SetLineBackgroundColor(System.Int32 line, Electron2D.Color color)` | Sets the background color of a logical line. |
| `public System.Void SetLineGutterClickable(System.Int32 line, System.Int32 gutter, System.Boolean clickable)` | Sets a cell-specific clickable policy. |
| `public System.Void SetLineGutterIcon(System.Int32 line, System.Int32 gutter, Electron2D.Texture icon)` | Sets a borrowed gutter cell texture. |
| `public System.Void SetLineGutterItemColor(System.Int32 line, System.Int32 gutter, Electron2D.Color color)` | Sets the gutter text/icon color. |
| `public System.Void SetLineGutterMetadata<T>(System.Int32 line, System.Int32 gutter, T metadata)` | Stores runtime gutter metadata with an exact generic type. |
| `public System.Void SetLineGutterText(System.Int32 line, System.Int32 gutter, System.String text)` | Sets a gutter cell's source text. |
| `public System.Void SetOvertypeModeEnabled(System.Boolean enabled)` | Sets whether user typing replaces characters. |
| `public System.Void SetSearchFlags(Electron2D.TextEdit.SearchFlags flags)` | Sets flags for highlighted search matches. |
| `public System.Void SetSearchText(System.String searchText)` | Sets the highlighted search text without moving carets. |
| `public System.Void SetSelectionMode(Electron2D.TextEdit.SelectionMode mode)` | Sets the selection interaction policy. |
| `public System.Void SetSelectionOriginColumn(System.Int32 column, System.Int32 caretIndex = 0)` | Sets a selection origin's scalar column. |
| `public System.Void SetSelectionOriginLine(System.Int32 line, System.Boolean canBeHidden = true, System.Int32 wrapIndex = -1, System.Int32 caretIndex = 0)` | Sets a selection origin's line while retaining its column. |
| `public System.Void SetTabSize(System.Int32 size)` | Sets a positive tab stop size used by layout and indentation input. |
| `public System.Void SetTooltipRequestFunc(System.Func<Electron2D.Vector2, System.String> callback)` | Sets a typed local-position tooltip provider. |
| `public System.Void SkipSelectionForNextOccurrence()` | Moves the latest selection to the next exact occurrence. |
| `public System.Void StartAction(Electron2D.TextEdit.EditAction action)` | Starts grouping consecutive edits of a named interaction. |
| `public System.Void SwapLines(System.Int32 fromLine, System.Int32 toLine)` | Exchanges complete logical lines and their gutter/background state. |
| `public System.Void TagSavedVersion()` | Marks the current edit version as saved. |
| `public System.Void Undo()` | Restores the previous committed content/caret state. |
| `public Electron2D.TextAutowrapMode AutowrapMode { get; set; }` | Gets or sets autowrap mode for the multiline editor. |
| `public System.Boolean BackspaceDeletesCompositeCharacterEnabled { get; set; }` | Gets or sets backspace deletes composite character enabled for the multiline editor. |
| `public System.Boolean CaretBlink { get; set; }` | Gets or sets caret blink for the multiline editor. |
| `public System.Double CaretBlinkInterval { get; set; }` | Gets or sets caret blink interval for the multiline editor. |
| `public System.Boolean CaretDrawWhenEditableDisabled { get; set; }` | Gets or sets caret draw when editable disabled for the multiline editor. |
| `public System.Boolean CaretMidGrapheme { get; set; }` | Gets or sets caret mid grapheme for the multiline editor. |
| `public System.Boolean CaretMoveOnRightClick { get; set; }` | Gets or sets caret move on right click for the multiline editor. |
| `public System.Boolean CaretMultiple { get; set; }` | Gets or sets caret multiple for the multiline editor. |
| `public Electron2D.TextEditCaretType CaretType { get; set; }` | Gets or sets caret type for the multiline editor. |
| `public System.Boolean ContextMenuEnabled { get; set; }` | Gets or sets context menu enabled for the multiline editor. |
| `public System.String CustomWordSeparators { get; set; }` | Gets or sets custom word separators for the multiline editor. |
| `public System.Boolean DeselectOnFocusLossEnabled { get; set; }` | Gets or sets deselect on focus loss enabled for the multiline editor. |
| `public System.Boolean DragAndDropSelectionEnabled { get; set; }` | Gets or sets drag and drop selection enabled for the multiline editor. |
| `public System.Boolean DrawControlChars { get; set; }` | Gets or sets draw control chars for the multiline editor. |
| `public System.Boolean DrawSpaces { get; set; }` | Gets or sets draw spaces for the multiline editor. |
| `public System.Boolean DrawTabs { get; set; }` | Gets or sets draw tabs for the multiline editor. |
| `public System.Boolean Editable { get; set; }` | Gets or sets editable for the multiline editor. |
| `public System.Boolean EmptySelectionClipboardEnabled { get; set; }` | Gets or sets empty selection clipboard enabled for the multiline editor. |
| `public System.Boolean HighlightAllOccurrences { get; set; }` | Gets or sets highlight all occurrences for the multiline editor. |
| `public System.Boolean HighlightCurrentLine { get; set; }` | Gets or sets highlight current line for the multiline editor. |
| `public System.Boolean IndentWrappedLines { get; set; }` | Gets or sets indent wrapped lines for the multiline editor. |
| `public System.String Language { get; set; }` | Gets or sets language for the multiline editor. |
| `public System.Boolean MiddleMousePasteEnabled { get; set; }` | Gets or sets middle mouse paste enabled for the multiline editor. |
| `public System.Boolean MinimapDraw { get; set; }` | Gets or sets minimap draw for the multiline editor. |
| `public System.Int32 MinimapWidth { get; set; }` | Gets or sets minimap width for the multiline editor. |
| `public System.String PlaceholderText { get; set; }` | Gets or sets placeholder text for the multiline editor. |
| `public System.Boolean ScrollFitContentHeight { get; set; }` | Gets or sets scroll fit content height for the multiline editor. |
| `public System.Boolean ScrollFitContentWidth { get; set; }` | Gets or sets scroll fit content width for the multiline editor. |
| `public System.Int32 ScrollHorizontal { get; set; }` | Gets or sets scroll horizontal for the multiline editor. |
| `public System.Boolean ScrollPastEndOfFile { get; set; }` | Gets or sets scroll past end of file for the multiline editor. |
| `public System.Boolean ScrollSmooth { get; set; }` | Gets or sets scroll smooth for the multiline editor. |
| `public System.Double ScrollVScrollSpeed { get; set; }` | Gets or sets scroll v scroll speed for the multiline editor. |
| `public System.Double ScrollVertical { get; set; }` | Gets or sets scroll vertical for the multiline editor. |
| `public System.Boolean SelectingEnabled { get; set; }` | Gets or sets selecting enabled for the multiline editor. |
| `public System.Boolean ShortcutKeysEnabled { get; set; }` | Gets or sets shortcut keys enabled for the multiline editor. |
| `public Electron2D.StructuredTextParser StructuredTextBIDIOverride { get; set; }` | Gets or sets structured text bidi override for the multiline editor. |
| `public System.String[] StructuredTextBIDIOverrideOptions { get; set; }` | Gets or sets structured text bidi override options for the multiline editor. |
| `public Electron2D.SyntaxHighlighter SyntaxHighlighter { get; set; }` | Gets or sets a borrowed highlighter dedicated to this editor. |
| `public System.Boolean TabInputMode { get; set; }` | Gets or sets tab input mode for the multiline editor. |
| `public System.String Text { get; set; }` | Gets or replaces the document's LF-normalized text, clearing history and secondary carets. |
| `public Electron2D.TextDirection TextDirection { get; set; }` | Gets or sets text direction for the multiline editor. |
| `public System.Boolean UseCustomWordSeparators { get; set; }` | Gets or sets use custom word separators for the multiline editor. |
| `public System.Boolean UseDefaultWordSeparators { get; set; }` | Gets or sets use default word separators for the multiline editor. |
| `public Electron2D.TextEdit.LineWrappingMode WrapMode { get; set; }` | Gets or sets wrap mode for the multiline editor. |

## Verification and limits

TextEditTests covers scalar/grapheme edits, multicaret ordering, grouped undo/redo, observer failure, copied metadata/configuration, syntax continuation/cache invalidation, committed/preedit input and fresh-process scene loading. Native GPU/compatibility readbacks exercise actual glyph/selection/gutter/minimap/placeholder recording, SDL event producers and clipboard restoration. The warm interval measures prepared managed owner recording and selection; it excludes cold parsing/edit snapshots, native events and native allocator totals. See [coverage](../coverage/classes/TextEdit.md) for inherited/native/editor dependencies.
