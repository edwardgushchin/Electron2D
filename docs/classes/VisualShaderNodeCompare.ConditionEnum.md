# VisualShaderNodeCompare.ConditionEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeCompare.cs](../../src/Scene/Resources/VisualShaderNodeCompare.cs).

```csharp
public enum Electron2D.VisualShaderNodeCompare.ConditionEnum
```

## Description

The reduction of a vector's component comparisons.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeCompare.ConditionEnum All = 0`](#member-055e2434d23f) | Requires every component comparison to succeed. |
| [`public const Electron2D.VisualShaderNodeCompare.ConditionEnum Any = 1`](#member-e4b9cd4dd918) | Requires at least one component comparison to succeed. |
| [`public const Electron2D.VisualShaderNodeCompare.ConditionEnum Max = 2`](#member-d8b93ecd49ca) | The nonselectable condition-count sentinel. |

<a id="member-055e2434d23f"></a>
### `All`

```csharp
public const Electron2D.VisualShaderNodeCompare.ConditionEnum All = 0
```

Requires every component comparison to succeed.

<a id="member-e4b9cd4dd918"></a>
### `Any`

```csharp
public const Electron2D.VisualShaderNodeCompare.ConditionEnum Any = 1
```

Requires at least one component comparison to succeed.

<a id="member-d8b93ecd49ca"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeCompare.ConditionEnum Max = 2
```

The nonselectable condition-count sentinel.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; excluded roles do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed requests, dormant tolerance, condition presence, transactional operand conversion, copy/archives and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
