# CodeEdit.CodeCompletionKind

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.CodeEdit.CodeCompletionKind`. **Source:** [source](../../src/Scene/GUI/CodeEdit.cs). **Component:** [Code authoring](../components/code-authoring.md).

## Description

The complete candidate-kind domain retains eleven values from Class (0) through Keyword (10). It describes application provider results; no candidate kind invokes a language backend.

## Enumeration values

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.CodeEdit.CodeCompletionKind Class = 0` | A type. |
| `public const Electron2D.CodeEdit.CodeCompletionKind Constant = 6` | A constant. |
| `public const Electron2D.CodeEdit.CodeCompletionKind Enum = 5` | An enumeration. |
| `public const Electron2D.CodeEdit.CodeCompletionKind FilePath = 8` | A file path. |
| `public const Electron2D.CodeEdit.CodeCompletionKind Function = 1` | A function. |
| `public const Electron2D.CodeEdit.CodeCompletionKind Keyword = 10` | A keyword. |
| `public const Electron2D.CodeEdit.CodeCompletionKind Member = 4` | A member. |
| `public const Electron2D.CodeEdit.CodeCompletionKind NodePath = 7` | A node path. |
| `public const Electron2D.CodeEdit.CodeCompletionKind PlainText = 9` | Plain text. |
| `public const Electron2D.CodeEdit.CodeCompletionKind Signal = 2` | A signal. |
| `public const Electron2D.CodeEdit.CodeCompletionKind Variable = 3` | A variable. |

## Enumeration values descriptions

<a id="member-9e7f82c257e5"></a>

### Class

`public const Electron2D.CodeEdit.CodeCompletionKind Class = 0`

A type.

<a id="member-ae208ba21adb"></a>

### Constant

`public const Electron2D.CodeEdit.CodeCompletionKind Constant = 6`

A constant.

<a id="member-73fda0ae4574"></a>

### Enum

`public const Electron2D.CodeEdit.CodeCompletionKind Enum = 5`

An enumeration.

<a id="member-4da4ca9ea92b"></a>

### FilePath

`public const Electron2D.CodeEdit.CodeCompletionKind FilePath = 8`

A file path.

<a id="member-24a4db6de227"></a>

### Function

`public const Electron2D.CodeEdit.CodeCompletionKind Function = 1`

A function.

<a id="member-461df986df4f"></a>

### Keyword

`public const Electron2D.CodeEdit.CodeCompletionKind Keyword = 10`

A keyword.

<a id="member-4688810002cd"></a>

### Member

`public const Electron2D.CodeEdit.CodeCompletionKind Member = 4`

A member.

<a id="member-e2b088fa4c4a"></a>

### NodePath

`public const Electron2D.CodeEdit.CodeCompletionKind NodePath = 7`

A node path.

<a id="member-01ddbdc06fba"></a>

### PlainText

`public const Electron2D.CodeEdit.CodeCompletionKind PlainText = 9`

Plain text.

<a id="member-c72c74f99865"></a>

### Signal

`public const Electron2D.CodeEdit.CodeCompletionKind Signal = 2`

A signal.

<a id="member-30e80a5b3bd3"></a>

### Variable

`public const Electron2D.CodeEdit.CodeCompletionKind Variable = 3`

A variable.

## Lifecycle and verification

Attached widget operations require the scene owner thread and a live control. Mutating CodeEdit configuration inside its overlay draw scope is rejected. Null/invalid keys, indices, enum values, required nonfinite colors and invalid ownership are rejected before changes. See [Code authoring](../components/code-authoring.md) for executable tests and backend/acceptance boundaries; [coverage](../coverage/classes/CodeEdit.md) records the full owning reference family.
