# LineEdit

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public class LineEdit : Control`.

**Inherits:** [Control](Control.md). **Source:** [LineEdit.cs](../../src/Scene/GUI/LineEdit.cs), [editing](../../src/Scene/GUI/LineEdit.Editing.cs), [input](../../src/Scene/GUI/LineEdit.Input.cs), [layout](../../src/Scene/GUI/LineEdit.Layout.cs), [commands](../../src/Scene/GUI/LineEdit.Menu.cs), [storage](../../src/Scene/GUI/LineEdit.Storage.cs).

## Description

Edits one Unicode line through native committed text and IME preedit, or typed C# operations. Columns count Unicode scalars, including supplementary characters. Caret positions and selection rectangles come from the complete shaped line, preserving ligature clusters and independent BiDi boundary positions. Arrow/pointer movement respects shaped grapheme boundaries unless CaretMidGrapheme is enabled. Direct CaretColumn writes clamp without snapping. Font/Language/structured contexts use the existing text backend; structured options are defensive copies. Password display uses the first assigned mask scalar; an empty mask displays a bullet. Secret copying, cutting and dragging are suppressed; GetSelectedText intentionally returns source text to programmatic callers.

Text assignment and MaxLength assignment reset caret, scroll, selection and history; they suppress TextChanged. InsertTextAtCaret inserts at the scalar caret, preserves selection and does not emit TextChanged or create its own history entry. TextChangeRejected reports the truncated suffix after the accepted prefix commits. Native edits, command insertion, backward deletion and clear commit history/state before observers. Clear resets history and emits only when nonempty. Attached DeleteText changes text immediately and coalesces its notification/history entry in the scene deferred queue; detached deletion does not schedule a signal. Replacement cancels stale deferred deletion notifications. User insertions remove CR/LF/tab. Read-only fields permit selection/copy and programmatic mutation while suppressing user edits, cut, paste and undo/redo.

Focus starts editing; Edit can acquire hidden focus, and Unedit commits IME then applies the deselection policy. Submit sends TextSubmitted and ends editing unless KeepEditingOnTextSubmit is enabled; observer failures aggregate after necessary state transitions. Native committed text replaces preedit exactly once. Composition rendering underlines the full range and emphasizes the selected preedit range. The containing native window owns IME activation and candidate position. Embedded viewport focus uses the existing deepest-focus route. The IME candidate area follows the complete containing-window transform, including viewport-container shrink and placement. Ending the scene does not enqueue new layout work.

The default keyboard actions are permanent typed [ProjectSettings](ProjectSettings.md) entries and are remappable through [InputMap](InputMap.md): text submission, scalar/grapheme/word/line movement, deletion, selection, clipboard, undo/redo and input-direction swapping. Shift extends movement selection. A selection drag preserves source text until a successful target drop, supports moving within the same line and command/control copying, and uses an untranslated Label preview. Incoming typed string drops remain accepted when outgoing selection dragging is disabled. Primary selection and middle paste use the display driver's separate clipboard.

The field draws themed normal/read-only/focus styles, glyph outlines, visual selection colors, caret, placeholder and clear/right icons. Text and selection clip to the content rectangle. Empty text still has font-height caret geometry. Left/right alignment follows layout direction; Center centers unscrolled text, Fill uses existing word/Kashida expansion. ExpandToTextLength affects minimum width. Original icon mode uses intrinsic size, FitToText uses a font-height square, and FitToLineEdit fits field bounds then applies the finite RightIconScale. Icons/fonts are borrowed; notifications are polled on the scene thread, and font generations recover changes hidden by throwing observers. Typed in-memory PackedScene reconstruction stores MaxLength, Text and CaretColumn in the required order.

MenuOption executes [LineEditMenuAction](LineEditMenuAction.md) without popup presentation. PopupMenu/Popup/Window transient ownership, native virtual keyboards and native symbol-picker presentation are exact remaining prerequisites. EmojiAndSymbols currently throws NotSupportedException. The class remains Partial in [coverage](../coverage/classes/LineEdit.md); this slice completes editing/display and available command behavior, not those absent hosts. Accessibility/editor file authoring, native allocator totals, broad-scene performance, other platforms and owner acceptance remain separate.

## Example

```csharp
var window = new Window();
var input = new LineEdit
{
    Size = new Vector2(240, 32),
    PlaceholderText = "Name",
    MaxLength = 80,
    ClearButtonEnabled = true,
};
input.TextSubmitted += value => Console.WriteLine(value);
window.AddChild(input);
Engine.Run(window);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public LineEdit()` | Creates an empty editable field with keyboard focus and an I-beam cursor. |

## Constructor Descriptions

<a id="member-13e2dbafc604"></a>
### .ctor

`public LineEdit()`

Creates an empty editable field with keyboard focus and an I-beam cursor.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.HorizontalAlignment Alignment { get; set; }` | Gets or sets horizontal text placement, mirrored under RTL layout. |
| `public System.Boolean BackspaceDeletesCompositeCharacterEnabled { get; set; }` | Gets or sets whether backspace removes the preceding complete grapheme when CaretMidGrapheme is false. |
| `public System.Boolean CaretBlink { get; set; }` | Gets or sets whether the caret blinks while editing. |
| `public System.Double CaretBlinkInterval { get; set; }` | Gets or sets the positive finite blink interval in seconds. |
| `public System.Int32 CaretColumn { get; set; }` | Gets or sets the caret's scalar column, clamped to the current text length. |
| `public System.Boolean CaretForceDisplayed { get; set; }` | Gets or sets whether an editable caret is drawn while the field is not editing. |
| `public System.Boolean CaretMidGrapheme { get; set; }` | Gets or sets whether pointer and arrow movement can address interior grapheme scalars. |
| `public System.Boolean ClearButtonEnabled { get; set; }` | Gets or sets whether a nonempty editable field displays a clear button. |
| `public System.Boolean DeselectOnFocusLossEnabled { get; set; }` | Gets or sets whether ending editing clears selection. |
| `public System.Boolean DragAndDropSelectionEnabled { get; set; }` | Gets or sets whether selected text can be dragged and strings dropped into this editable field. |
| `public System.Boolean DrawControlChars { get; set; }` | Gets or sets whether nonprinting control scalars display hexadecimal placeholder glyphs. |
| `public System.Boolean Editable { get; set; }` | Gets or sets whether native edits are allowed. Programmatic assignments remain available. |
| `public System.Boolean ExpandToTextLength { get; set; }` | Gets or sets whether natural text width contributes to minimum width. |
| `public System.Boolean Flat { get; set; }` | Gets or sets whether the normal or read-only background is omitted. |
| `public Electron2D.LineEditIconExpandMode IconExpandMode { get; set; }` | Gets or sets how the trailing icon fits the field. |
| `public System.Boolean KeepEditingOnTextSubmit { get; set; }` | Gets or sets whether submission leaves editing active. |
| `public System.String Language { get; set; }` | Gets or sets the shaping language. Empty follows the current translation locale. |
| `public System.Int32 MaxLength { get; set; }` | Gets or sets the scalar length limit and resets the current text through that limit. |
| `public System.Boolean MiddleMousePasteEnabled { get; set; }` | Gets or sets whether the middle button pastes the primary clipboard. |
| `public System.String PlaceholderText { get; set; }` | Gets or sets translated text displayed while the field and IME composition are empty. |
| `public Electron2D.Texture RightIcon { get; set; }` | Gets or sets the borrowed trailing icon. |
| `public System.Single RightIconScale { get; set; }` | Gets or sets the finite multiplier for the trailing icon. |
| `public System.Boolean Secret { get; set; }` | Gets or sets whether displayed text uses SecretCharacter and clipboard copying is suppressed. |
| `public System.String SecretCharacter { get; set; }` | Gets or sets a single-scalar password mask. An empty string displays the default bullet. |
| `public System.Boolean SelectAllOnFocus { get; set; }` | Gets or sets whether entering editing through focus selects the full line. |
| `public System.Boolean SelectingEnabled { get; set; }` | Gets or sets whether pointer and keyboard selection are allowed. |
| `public System.Boolean ShortcutKeysEnabled { get; set; }` | Gets or sets whether selection, clipboard and undo keyboard shortcuts are active. |
| `public Electron2D.StructuredTextParser StructuredTextBIDIOverride { get; set; }` | Gets or sets structured bidirectional context parsing. |
| `public System.String[] StructuredTextBIDIOverrideOptions { get; set; }` | Gets a copied read-only view of parser options, or replaces them with a defensive copy. |
| `public System.String Text { get; set; }` | Gets or replaces the text, resetting selection, scroll, caret and history without TextChanged. |
| `public Electron2D.TextDirection TextDirection { get; set; }` | Gets or sets paragraph writing direction. |

## Property Descriptions

<a id="member-b56fc8ec5eb8"></a>
### Alignment

`public Electron2D.HorizontalAlignment Alignment { get; set; }`

Gets or sets horizontal text placement, mirrored under RTL layout.

Value: HorizontalAlignment.Left initially.

<a id="member-76a36b983262"></a>
### BackspaceDeletesCompositeCharacterEnabled

`public System.Boolean BackspaceDeletesCompositeCharacterEnabled { get; set; }`

Gets or sets whether backspace removes the preceding complete grapheme when CaretMidGrapheme is false.

Value: false initially.

<a id="member-61125fea565f"></a>
### CaretBlink

`public System.Boolean CaretBlink { get; set; }`

Gets or sets whether the caret blinks while editing.

Value: false initially.

<a id="member-6c30db352598"></a>
### CaretBlinkInterval

`public System.Double CaretBlinkInterval { get; set; }`

Gets or sets the positive finite blink interval in seconds.

Value: 0.65 initially.

System.ArgumentOutOfRangeException: The interval is nonpositive or not finite.

<a id="member-ff458657835c"></a>
### CaretColumn

`public System.Int32 CaretColumn { get; set; }`

Gets or sets the caret's scalar column, clamped to the current text length.

Value: Zero initially. Direct assignments may address the interior of a grapheme.

<a id="member-fc2231f6f4d2"></a>
### CaretForceDisplayed

`public System.Boolean CaretForceDisplayed { get; set; }`

Gets or sets whether an editable caret is drawn while the field is not editing.

Value: false initially.

<a id="member-89844d56a698"></a>
### CaretMidGrapheme

`public System.Boolean CaretMidGrapheme { get; set; }`

Gets or sets whether pointer and arrow movement can address interior grapheme scalars.

Value: false initially.

<a id="member-9d2393570a69"></a>
### ClearButtonEnabled

`public System.Boolean ClearButtonEnabled { get; set; }`

Gets or sets whether a nonempty editable field displays a clear button.

Value: false initially.

<a id="member-c468e9b263bf"></a>
### DeselectOnFocusLossEnabled

`public System.Boolean DeselectOnFocusLossEnabled { get; set; }`

Gets or sets whether ending editing clears selection.

Value: true initially.

<a id="member-2066a8b348d3"></a>
### DragAndDropSelectionEnabled

`public System.Boolean DragAndDropSelectionEnabled { get; set; }`

Gets or sets whether selected text can be dragged and strings dropped into this editable field.

Value: true initially.

<a id="member-3feddb630635"></a>
### DrawControlChars

`public System.Boolean DrawControlChars { get; set; }`

Gets or sets whether nonprinting control scalars display hexadecimal placeholder glyphs.

Value: False initially.

<a id="member-bd12ef46a98b"></a>
### Editable

`public System.Boolean Editable { get; set; }`

Gets or sets whether native edits are allowed. Programmatic assignments remain available.

Value: True initially.

<a id="member-1be5262cf89b"></a>
### ExpandToTextLength

`public System.Boolean ExpandToTextLength { get; set; }`

Gets or sets whether natural text width contributes to minimum width.

Value: false initially.

<a id="member-bc73798eca57"></a>
### Flat

`public System.Boolean Flat { get; set; }`

Gets or sets whether the normal or read-only background is omitted.

Value: false initially.

<a id="member-ea2aa373557c"></a>
### IconExpandMode

`public Electron2D.LineEditIconExpandMode IconExpandMode { get; set; }`

Gets or sets how the trailing icon fits the field.

Value: OriginalSize initially. Unknown values use original size.

<a id="member-921a8f62248b"></a>
### KeepEditingOnTextSubmit

`public System.Boolean KeepEditingOnTextSubmit { get; set; }`

Gets or sets whether submission leaves editing active.

Value: false initially.

<a id="member-a5dad46d76b0"></a>
### Language

`public System.String Language { get; set; }`

Gets or sets the shaping language. Empty follows the current translation locale.

Value: Empty initially.

System.ArgumentException: The language contains NUL.

<a id="member-5137219b7586"></a>
### MaxLength

`public System.Int32 MaxLength { get; set; }`

Gets or sets the scalar length limit and resets the current text through that limit.

Value: Zero initially; zero is unlimited.

System.ArgumentOutOfRangeException: The limit is negative.

<a id="member-1b94d9d0aaf4"></a>
### MiddleMousePasteEnabled

`public System.Boolean MiddleMousePasteEnabled { get; set; }`

Gets or sets whether the middle button pastes the primary clipboard.

Value: true initially.

<a id="member-4a47ef3179d0"></a>
### PlaceholderText

`public System.String PlaceholderText { get; set; }`

Gets or sets translated text displayed while the field and IME composition are empty.

Value: Empty initially.

<a id="member-c604ccc197b2"></a>
### RightIcon

`public Electron2D.Texture RightIcon { get; set; }`

Gets or sets the borrowed trailing icon.

Value: Null initially. A visible clear button takes precedence over this icon.

<a id="member-d658e6c227cd"></a>
### RightIconScale

`public System.Single RightIconScale { get; set; }`

Gets or sets the finite multiplier for the trailing icon.

Value: One initially.

<a id="member-30f383e833ce"></a>
### Secret

`public System.Boolean Secret { get; set; }`

Gets or sets whether displayed text uses SecretCharacter and clipboard copying is suppressed.

Value: false initially.

<a id="member-ad6ea01bf78d"></a>
### SecretCharacter

`public System.String SecretCharacter { get; set; }`

Gets or sets a single-scalar password mask. An empty string displays the default bullet.

Value: A bullet initially. Only the first scalar of an assigned value is retained.

<a id="member-4208fa3303be"></a>
### SelectAllOnFocus

`public System.Boolean SelectAllOnFocus { get; set; }`

Gets or sets whether entering editing through focus selects the full line.

Value: false initially.

<a id="member-e268d1ee5191"></a>
### SelectingEnabled

`public System.Boolean SelectingEnabled { get; set; }`

Gets or sets whether pointer and keyboard selection are allowed.

Value: True initially. Disabling clears an existing selection.

<a id="member-b3a0831431f2"></a>
### ShortcutKeysEnabled

`public System.Boolean ShortcutKeysEnabled { get; set; }`

Gets or sets whether selection, clipboard and undo keyboard shortcuts are active.

Value: true initially.

<a id="member-b9ad935c5977"></a>
### StructuredTextBIDIOverride

`public Electron2D.StructuredTextParser StructuredTextBIDIOverride { get; set; }`

Gets or sets structured bidirectional context parsing.

Value: Default initially. Unknown numeric values use the default parser.

System.NotSupportedException: The language-specific parser is selected.

<a id="member-f413bd6f7577"></a>
### StructuredTextBIDIOverrideOptions

`public System.String[] StructuredTextBIDIOverrideOptions { get; set; }`

Gets a copied read-only view of parser options, or replaces them with a defensive copy.

Value: No options initially.

<a id="member-7efe63f78982"></a>
### Text

`public System.String Text { get; set; }`

Gets or replaces the text, resetting selection, scroll, caret and history without TextChanged.

Value: Empty initially. MaxLength limits Unicode scalars; zero means unlimited.

<a id="member-d0cc142310e1"></a>
### TextDirection

`public Electron2D.TextDirection TextDirection { get; set; }`

Gets or sets paragraph writing direction.

Value: Auto initially; Inherited follows layout direction.

System.ArgumentOutOfRangeException: The value is outside minus one through three.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void ApplyIME()` | Commits the current composition through InsertTextAtCaret, then closes the native IME session. |
| `public System.Void CancelIME()` | Discards composition and closes the native IME session. |
| `public System.Void Clear()` | Removes text, selection, composition and history; emits TextChanged only for a nonempty previous text. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| `public System.Void DeleteCharAtCaret()` | Deletes the scalar or configured grapheme immediately before the caret and emits TextChanged. |
| `public System.Void DeleteText(System.Int32 fromColumn, System.Int32 toColumn)` | Deletes the scalar interval and adjusts the caret; equal endpoints are accepted. |
| `public System.Void Deselect()` | Clears selection. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Void Edit(System.Boolean hideFocus = false)` | Begins editing an attached editable field, acquiring focus when needed. |
| `public System.Int32 GetNextCompositeCharacterColumn(System.Int32 column)` | Returns the following shaped-grapheme boundary. |
| `public System.Int32 GetPreviousCompositeCharacterColumn(System.Int32 column)` | Returns the preceding shaped-grapheme boundary. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public System.Single GetScrollOffset()` | Returns the horizontal text offset in pixels; scrolling is negative. |
| `public System.String GetSelectedText()` | Returns the selected source text, including password text when called programmatically. |
| `public System.Int32 GetSelectionFromColumn()` | Returns the selection start or minus one when absent. |
| `public System.Int32 GetSelectionToColumn()` | Returns the selection end or minus one when absent. |
| `public System.Boolean HasIMEText()` | Returns whether uncommitted IME text is present. |
| `public System.Boolean HasRedo()` | Returns whether redo has a later text state. |
| `public System.Boolean HasSelection()` | Returns whether a nonempty interval is selected. |
| `public System.Boolean HasUndo()` | Returns whether undo has an earlier text state. |
| `public System.Void InsertTextAtCaret(System.String text)` | Inserts text at the scalar caret without deleting selection or emitting TextChanged. |
| `public System.Boolean IsEditing()` | Returns whether editing is active. |
| `public System.Void MenuOption(Electron2D.LineEditMenuAction option)` | Executes a typed editing command without requiring popup presentation. |
| `protected override System.Boolean OnCanDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)` | Tests a potential drop target; false rejects it. |
| `protected override System.Void OnDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)` | Consumes an accepted drop after the target test succeeds. |
| `protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)` | Processes a temporary control-local event before Electron2D.Control.GUIInput subscribers. |
| `protected override Electron2D.DragPayload OnGetDragData(Electron2D.Vector2 atPosition)` | Produces drag data for an automatic pointer drag; null rejects the attempt. |
| `protected override Electron2D.Vector2 OnGetMinimumSize()` | Supplies the intrinsic minimum size; the base control has none. |
| `protected override System.Void OnIMECompositionChanged(System.String text, Electron2D.Vector2i selection)` | Receives an uncommitted composition update before Electron2D.Control.IMECompositionChanged subscribers. |
| `protected override System.Void OnNotification(System.Int32 what)` | Handles an engine notification delivered to this object. |
| `protected override System.Void OnTextInput(System.String text)` | Receives one committed text string before Electron2D.Control.TextInput subscribers. |
| `public System.Void Select(System.Int32 fromColumn = 0, System.Int32 toColumn = -1)` | Selects a scalar interval when selection is enabled. |
| `public System.Void SelectAll()` | Selects the whole text without moving a nonempty line's caret. |
| `public System.Void Unedit()` | Ends editing, commits composition and applies the focus-loss selection policy. |

## Method Descriptions

<a id="member-f87bba23af1d"></a>
### ApplyIME

`public System.Void ApplyIME()`

Commits the current composition through InsertTextAtCaret, then closes the native IME session.

<a id="member-4712bc743d3e"></a>
### CancelIME

`public System.Void CancelIME()`

Discards composition and closes the native IME session.

<a id="member-75226721c5c5"></a>
### Clear

`public System.Void Clear()`

Removes text, selection, composition and history; emits TextChanged only for a nonempty previous text.

<a id="member-39a2cea9c778"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

Returns: A non-null factory that creates a fresh node of the exact same runtime type.

Remarks: The base implementation supports only an exact Electron2D.Node. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

System.NotSupportedException: A derived node has not explicitly supplied an instancing factory.

<a id="member-cbfbc9a3e62c"></a>
### DeleteCharAtCaret

`public System.Void DeleteCharAtCaret()`

Deletes the scalar or configured grapheme immediately before the caret and emits TextChanged.

<a id="member-54ef84be3998"></a>
### DeleteText

`public System.Void DeleteText(System.Int32 fromColumn, System.Int32 toColumn)`

Deletes the scalar interval and adjusts the caret; equal endpoints are accepted.

fromColumn: Inclusive scalar start.

toColumn: Exclusive scalar end.

System.ArgumentOutOfRangeException: The ordered interval lies outside Text.

Remarks: Attached changes emit one deferred TextChanged and add one history entry. Detached deletion changes text without that notification.

<a id="member-de2a46b12e1f"></a>
### Deselect

`public System.Void Deselect()`

Clears selection.

<a id="member-ee06d29a547e"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

Remarks: Cancels queued deletion, detaches this node, recursively disposes every owned child, clears groups and event subscribers, and then calls the base implementation. Every teardown stage is attempted before failures are reported together.

<a id="member-1d3bd8ff141b"></a>
### Edit

`public System.Void Edit(System.Boolean hideFocus = false)`

Begins editing an attached editable field, acquiring focus when needed.

hideFocus: Whether focus decoration is hidden when focus is acquired.

<a id="member-b446248ccd54"></a>
### GetNextCompositeCharacterColumn

`public System.Int32 GetNextCompositeCharacterColumn(System.Int32 column)`

Returns the following shaped-grapheme boundary.

column: Scalar column, clamped to Text.

Returns: A scalar boundary.

<a id="member-4b9ce6c4fb4e"></a>
### GetPreviousCompositeCharacterColumn

`public System.Int32 GetPreviousCompositeCharacterColumn(System.Int32 column)`

Returns the preceding shaped-grapheme boundary.

column: Scalar column, clamped to Text.

Returns: A scalar boundary.

<a id="member-ca51160f78ea"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Appends this class's typed hierarchy, ownership, processing, and automatic translation descriptors to the inherited descriptors.

<a id="member-0b4c6f321c65"></a>
### GetScrollOffset

`public System.Single GetScrollOffset()`

Returns the horizontal text offset in pixels; scrolling is negative.

Returns: The current shaped-line offset.

<a id="member-00fd86ccb83c"></a>
### GetSelectedText

`public System.String GetSelectedText()`

Returns the selected source text, including password text when called programmatically.

Returns: The selected substring or empty.

<a id="member-27a0bd6478cf"></a>
### GetSelectionFromColumn

`public System.Int32 GetSelectionFromColumn()`

Returns the selection start or minus one when absent.

Returns: The inclusive scalar start or minus one.

<a id="member-f57d98f8d342"></a>
### GetSelectionToColumn

`public System.Int32 GetSelectionToColumn()`

Returns the selection end or minus one when absent.

Returns: The exclusive scalar end or minus one.

<a id="member-8402561f78f5"></a>
### HasIMEText

`public System.Boolean HasIMEText()`

Returns whether uncommitted IME text is present.

Returns: True for a nonempty composition.

<a id="member-91c0ff2b771a"></a>
### HasRedo

`public System.Boolean HasRedo()`

Returns whether redo has a later text state.

Returns: True when a later history entry exists.

<a id="member-b10fd9f0c7c1"></a>
### HasSelection

`public System.Boolean HasSelection()`

Returns whether a nonempty interval is selected.

Returns: True for an active selection.

<a id="member-7f4d491502bb"></a>
### HasUndo

`public System.Boolean HasUndo()`

Returns whether undo has an earlier text state.

Returns: True when an earlier history entry exists.

<a id="member-2d7e31bb9c76"></a>
### InsertTextAtCaret

`public System.Void InsertTextAtCaret(System.String text)`

Inserts text at the scalar caret without deleting selection or emitting TextChanged.

text: The text to insert.

Remarks: MaxLength rejection is reported after committing the accepted prefix, so observers see consistent state.

<a id="member-15a5d2137f96"></a>
### IsEditing

`public System.Boolean IsEditing()`

Returns whether editing is active.

Returns: True while this field is editing.

<a id="member-710a22042346"></a>
### MenuOption

`public System.Void MenuOption(Electron2D.LineEditMenuAction option)`

Executes a typed editing command without requiring popup presentation.

option: The editing, text-direction or Unicode-control command.

Remarks: Unknown numeric values are ignored. Native symbol-picker presentation requires a separate display service.

System.NotSupportedException: EmojiAndSymbols is selected without a native symbol-picker service.

<a id="member-7e54edc229be"></a>
### OnCanDropData

`protected override System.Boolean OnCanDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)`

Tests a potential drop target; false rejects it.

atPosition: The current local pointer position.

payload: The borrowed typed payload.

Returns: Whether the target accepts the payload.

<a id="member-c527e93a07c2"></a>
### OnDropData

`protected override System.Void OnDropData(Electron2D.Vector2 atPosition, Electron2D.DragPayload payload)`

Consumes an accepted drop after the target test succeeds.

atPosition: The current local pointer position.

payload: The borrowed typed payload.

<a id="member-cc0d388c1ba8"></a>
### OnGUIInput

`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`

Processes a temporary control-local event before Electron2D.Control.GUIInput subscribers.

inputEvent: Borrowed event valid only during synchronous dispatch.

<a id="member-f8b6e21e1083"></a>
### OnGetDragData

`protected override Electron2D.DragPayload OnGetDragData(Electron2D.Vector2 atPosition)`

Produces drag data for an automatic pointer drag; null rejects the attempt.

atPosition: The original left-press position in local coordinates.

Returns: A typed payload, or null.

<a id="member-e3e44761b2f4"></a>
### OnGetMinimumSize

`protected override Electron2D.Vector2 OnGetMinimumSize()`

Supplies the intrinsic minimum size; the base control has none.

<a id="member-32cb0edc6748"></a>
### OnIMECompositionChanged

`protected override System.Void OnIMECompositionChanged(System.String text, Electron2D.Vector2i selection)`

Receives an uncommitted composition update before Electron2D.Control.IMECompositionChanged subscribers.

text: The current preedit text; empty clears it.

selection: Unicode-codepoint selection start and length.

<a id="member-9dd3758d32eb"></a>
### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`

Handles an engine notification delivered to this object.

what: The notification identifier.

Remarks: Calls the base implementation, then maps enter, exit, ready, process, and physics-process notification IDs to the corresponding typed virtual callbacks. Pause and application-suspend notifications reset eligible physics presentation history. Manual Electron2D.ElectronObject.Notify(System.Int32) calls invoke callbacks but do not mutate tree membership, ready state, or delta values.

<a id="member-cc2424fba43e"></a>
### OnTextInput

`protected override System.Void OnTextInput(System.String text)`

Receives one committed text string before Electron2D.Control.TextInput subscribers.

text: The complete committed text, including multiple Unicode scalars when supplied.

<a id="member-6b5cf7369fb3"></a>
### Select

`public System.Void Select(System.Int32 fromColumn = 0, System.Int32 toColumn = -1)`

Selects a scalar interval when selection is enabled.

fromColumn: Inclusive start, clamped to the text.

toColumn: Exclusive end; negative means text length.

Remarks: An empty (0,0) clears selection; another reversed or empty interval leaves selection unchanged.

<a id="member-8be7a53a6589"></a>
### SelectAll

`public System.Void SelectAll()`

Selects the whole text without moving a nonempty line's caret.

<a id="member-89a394a74540"></a>
### Unedit

`public System.Void Unedit()`

Ends editing, commits composition and applies the focus-loss selection policy.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<System.Boolean> EditingToggled` | Occurs when user editing starts or ends through focus, submission or Editable changes. |
| `public event System.Action<System.String> TextChangeRejected` | Occurs when MaxLength rejects a suffix of an insertion. |
| `public event System.Action<System.String> TextChanged` | Occurs after a user edit, deletion or clear has committed its new value. |
| `public event System.Action<System.String> TextSubmitted` | Occurs when submission is requested while editing. |

## Event Descriptions

<a id="member-fc5e72fc7e6d"></a>
### EditingToggled

`public event System.Action<System.Boolean> EditingToggled`

Occurs when user editing starts or ends through focus, submission or Editable changes.

<a id="member-3a6f368b81b7"></a>
### TextChangeRejected

`public event System.Action<System.String> TextChangeRejected`

Occurs when MaxLength rejects a suffix of an insertion.

<a id="member-e55d24ca3c41"></a>
### TextChanged

`public event System.Action<System.String> TextChanged`

Occurs after a user edit, deletion or clear has committed its new value.

<a id="member-2baf543f0fbc"></a>
### TextSubmitted

`public event System.Action<System.String> TextSubmitted`

Occurs when submission is requested while editing.

## Verification

[LineEditTests](../../tests/Electron2D.Tests/LineEditTests.cs) checks scalar/grapheme boundaries, max-length rejection, quiet assignments, deferred deletion, selection, stored reconstruction, history, typed commands, IME, callback failures, text drag/drop and real native rendering/input/clipboard. Linux x64 X11 GPU and compatibility hosts pass pixel checks and 64 warmed caret/selection render plus mutation intervals with zero managed bytes. The selected native screenshot was inspected visually. Input copies, cold text shaping/changes, native allocations and other platform/owner acceptance are outside that measurement.

Keyboard fixtures use the public command-or-control remapping policy for Undo/Redo, selecting Command on macOS and Control elsewhere. The first full macOS CI run exposed the former Ctrl-only fixture; the corrected full target run remains required.

The embedded popup slice adds Popup/PopupPanel theme/type and exact file-factory integration; focused LineEdit text/IME now resolves the containing native root while retaining popup-local control focus. See [the component](../components/popup-windows.md) for the applicable portion and limits.

The owned SpinBox input reuses editing, scalar selection/caret, submission and theme behavior. An internal formatting operation restores bounded selection/caret after replacing generated numeric text; it adds no public text facade. See [numeric input](../components/numeric-input.md) for the exercised workflow and limits.
