# NodeAutoTranslateMode

Last updated: 2026-09-24

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Main/NodeAutoTranslateMode.cs`](../../src/Scene/Main/NodeAutoTranslateMode.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum NodeAutoTranslateMode`

## Description

Controls automatic message translation by [`Node`](Node.md). `Inherit` resolves through the nearest ancestor; a detached parentless node resolves to enabled. At tree construction, a root still set to `Inherit` samples `ProjectSettings.RootNodeAutoTranslate` and becomes `Always` or `Disabled`. The mode does not change explicit `Tr` and `TrN` calls or per-object `CanTranslateMessages`.

## Examples

```csharp
using var node = new Node { AutoTranslateMode = NodeAutoTranslateMode.Disabled };
```

## Constants

| Member | Description |
| --- | --- |
| [`Inherit = 0`](#f-electron2d-nodeautotranslatemode-inherit) | Uses the nearest ancestor's mode. |
| [`Always = 1`](#f-electron2d-nodeautotranslatemode-always) | Enables automatic translation. |
| [`Disabled = 2`](#f-electron2d-nodeautotranslatemode-disabled) | Disables automatic translation. |

## Constant Descriptions

<a id="f-electron2d-nodeautotranslatemode-inherit"></a>
### `Inherit = 0`

Uses the nearest ancestor's mode; detached roots resolve as enabled.

<a id="f-electron2d-nodeautotranslatemode-always"></a>
### `Always = 1`

Enables `Atr` and `AtrN` for this node and descendants that inherit the mode.

<a id="f-electron2d-nodeautotranslatemode-disabled"></a>
### `Disabled = 2`

Makes `Atr` and `AtrN` return source forms for this node and descendants that inherit the mode.

## Invariants and errors

`Node.AutoTranslateMode` rejects undefined values. An active tree root cannot be changed to `Inherit`; attached changes require the tree owner thread. Changes notify the subtree that translations may have changed.

## Verification

[`NodeLocalizationTests`](../../tests/Electron2D.Tests/NodeLocalizationTests.cs) checks inheritance, overrides, root setting, singular/plural fallback and notifications in managed execution. Native UI translation and other platforms remain unverified.
