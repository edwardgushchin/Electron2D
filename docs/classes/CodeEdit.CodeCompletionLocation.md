# CodeEdit.CodeCompletionLocation

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.CodeEdit.CodeCompletionLocation`. **Source:** [source](../../src/Scene/GUI/CodeEdit.cs). **Component:** [Code authoring](../components/code-authoring.md).

## Description

Completion scope sentinels are Local (0), ParentMask (256), OtherUserCode (512) and Other (1024). The integer option location also permits ancestor distances 1 through 256; this enum is not a bitmask.

## Enumeration values

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.CodeEdit.CodeCompletionLocation Local = 0` | The query's local scope. |
| `public const Electron2D.CodeEdit.CodeCompletionLocation Other = 1024` | An external scope. |
| `public const Electron2D.CodeEdit.CodeCompletionLocation OtherUserCode = 512` | Another user-code scope. |
| `public const Electron2D.CodeEdit.CodeCompletionLocation ParentMask = 256` | Maximum encoded ancestor distance. |

## Enumeration values descriptions

<a id="member-fd7127bdba7c"></a>

### Local

`public const Electron2D.CodeEdit.CodeCompletionLocation Local = 0`

The query's local scope.

<a id="member-256758077f22"></a>

### Other

`public const Electron2D.CodeEdit.CodeCompletionLocation Other = 1024`

An external scope.

<a id="member-a9590ac5d187"></a>

### OtherUserCode

`public const Electron2D.CodeEdit.CodeCompletionLocation OtherUserCode = 512`

Another user-code scope.

<a id="member-0185d5ad9d5a"></a>

### ParentMask

`public const Electron2D.CodeEdit.CodeCompletionLocation ParentMask = 256`

Maximum encoded ancestor distance.

## Lifecycle and verification

Attached widget operations require the scene owner thread and a live control. Mutating CodeEdit configuration inside its overlay draw scope is rejected. Null/invalid keys, indices, enum values, required nonfinite colors and invalid ownership are rejected before changes. See [Code authoring](../components/code-authoring.md) for executable tests and backend/acceptance boundaries; [coverage](../coverage/classes/CodeEdit.md) records the full owning reference family.
