# CodeCompletionOption

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.CodeCompletionOption`. **Source:** [source](../../src/Scene/GUI/CodeCompletionOption.cs). **Component:** [Code authoring](../components/code-authoring.md).

## Description

This sealed immutable snapshot projects one CodeEdit completion candidate. Kind, DisplayText, InsertText, FontColor, Icon and integer Location describe presentation and insertion; local is zero, ancestor distances occupy 1 through 256, and the named location sentinels remain distinct. Icon is borrowed and is skipped during drawing if disposed. WithDefaultValue<T> returns another independent immutable option sharing borrowed configuration and an exact generic runtime payload; TryGetDefaultValue<T> succeeds only for that stored T. Values/options are runtime provider state and are not scene configuration.

## Constructors

| Declaration | Contract |
| --- | --- |
| `public CodeCompletionOption(Electron2D.CodeEdit.CodeCompletionKind kind, System.String displayText, System.String insertText, System.Nullable<Electron2D.Color> fontColor = null, Electron2D.Texture icon = null, System.Int32 location = 1024)` | Creates a candidate with no default payload. |

## Constructors descriptions

<a id="member-9ee6470a8c38"></a>

### CodeCompletionOption(Electron2D.CodeEdit.CodeCompletionKind, System.String, System.String, System.Nullable<Electron2D.Color>, Electron2D.Texture, System.Int32)

`public CodeCompletionOption(Electron2D.CodeEdit.CodeCompletionKind kind, System.String displayText, System.String insertText, System.Nullable<Electron2D.Color> fontColor = null, Electron2D.Texture icon = null, System.Int32 location = 1024)`

Creates a candidate with no default payload.

**Param `kind`:** Defined candidate kind.

**Param `displayText`:** Nonnull display text.

**Param `insertText`:** Nonnull inserted text.

**Param `fontColor`:** Finite display color or white.

**Param `icon`:** Live borrowed texture or null.

**Param `location`:** Local zero, ancestor distance 1 through 256, or a location sentinel.

## Methods

| Declaration | Contract |
| --- | --- |
| `public System.Boolean TryGetDefaultValue<T>(out T value)` | Tries to read the default value under its exact generic type. |
| `public Electron2D.CodeCompletionOption WithDefaultValue<T>(T value)` | Creates an independent candidate sharing configuration and a borrowed typed payload. |

## Methods descriptions

<a id="member-48f39efe2df5"></a>

### TryGetDefaultValue(ref T)

`public System.Boolean TryGetDefaultValue<T>(out T value)`

Tries to read the default value under its exact generic type.

**Param `value`:** Stored value, or default on mismatch/absence.

**Typeparam `T`:** Stored type.

**Returns:** Whether the exact type is present.

<a id="member-9ffbf56643c5"></a>

### WithDefaultValue(T)

`public Electron2D.CodeCompletionOption WithDefaultValue<T>(T value)`

Creates an independent candidate sharing configuration and a borrowed typed payload.

**Param `value`:** Borrowed value.

**Typeparam `T`:** Exact payload type.

**Returns:** New candidate.

## Properties

| Declaration | Contract |
| --- | --- |
| `public System.String DisplayText { get;  }` | Gets menu text. |
| `public Electron2D.Color FontColor { get;  }` | Gets menu font color. |
| `public Electron2D.Texture Icon { get;  }` | Gets the borrowed icon. |
| `public System.String InsertText { get;  }` | Gets insertion text. |
| `public Electron2D.CodeEdit.CodeCompletionKind Kind { get;  }` | Gets the candidate kind. |
| `public System.Int32 Location { get;  }` | Gets relative source location. |

## Properties descriptions

<a id="member-c6b51ef9f89b"></a>

### DisplayText

`public System.String DisplayText { get;  }`

Gets menu text.

**Value:** Immutable source text.

<a id="member-7f5f2e47d335"></a>

### FontColor

`public Electron2D.Color FontColor { get;  }`

Gets menu font color.

**Value:** White by default.

<a id="member-d0fb3fac5ac9"></a>

### Icon

`public Electron2D.Texture Icon { get;  }`

Gets the borrowed icon.

**Value:** Texture or null.

<a id="member-b898578e3ff0"></a>

### InsertText

`public System.String InsertText { get;  }`

Gets insertion text.

**Value:** Immutable source text.

<a id="member-709761a09c39"></a>

### Kind

`public Electron2D.CodeEdit.CodeCompletionKind Kind { get;  }`

Gets the candidate kind.

**Value:** Constructor kind.

<a id="member-5c553131fa1c"></a>

### Location

`public System.Int32 Location { get;  }`

Gets relative source location.

**Value:** Other by default; ancestor distance remains an integer.

## Lifecycle and verification

Attached widget operations require the scene owner thread and a live control. Mutating CodeEdit configuration inside its overlay draw scope is rejected. Null/invalid keys, indices, enum values, required nonfinite colors and invalid ownership are rejected before changes. See [Code authoring](../components/code-authoring.md) for executable tests and backend/acceptance boundaries; [coverage](../coverage/classes/CodeEdit.md) records the full owning reference family.
