# RenderingDevice.CompareOperator

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.CompareOperator`. **Component:** [Local device](../components/local-compute.md#depthstencil-graphics-and-sampling). **Source:** [RenderingDevice.DepthStencil.cs](../../src/Servers/Rendering/Device/RenderingDevice.DepthStencil.cs).

## Description

Selects the comparison of an incoming depth or stencil reference against stored data.



## Example

Partial configuration snippet; [the executable public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) supplies the device, modules and targets.

```csharp
var comparison = RenderingDevice.CompareOperator.Less;
```

## API

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.RenderingDevice.CompareOperator Always = 7`](#member-c82c063ce63c) | Always passes. |
| [`public const Electron2D.RenderingDevice.CompareOperator Equal = 2`](#member-b64a72bc6762) | Passes when equal. |
| [`public const Electron2D.RenderingDevice.CompareOperator Greater = 4`](#member-14908c4256d6) | Passes when incoming is greater. |
| [`public const Electron2D.RenderingDevice.CompareOperator GreaterOrEqual = 6`](#member-0b9c05fea6a7) | Passes when incoming is greater or equal. |
| [`public const Electron2D.RenderingDevice.CompareOperator Less = 1`](#member-556497ce71f1) | Passes when incoming is less. |
| [`public const Electron2D.RenderingDevice.CompareOperator LessOrEqual = 3`](#member-0d9593f1db17) | Passes when incoming is less or equal. |
| [`public const Electron2D.RenderingDevice.CompareOperator Max = 8`](#member-7d7f52116dba) | Exclusive nonselectable bound. |
| [`public const Electron2D.RenderingDevice.CompareOperator Never = 0`](#member-7df76708579a) | Never passes. |
| [`public const Electron2D.RenderingDevice.CompareOperator NotEqual = 5`](#member-42c400421b72) | Passes when different. |

## Member descriptions

<a id="member-c82c063ce63c"></a>
### Always

```csharp
public const Electron2D.RenderingDevice.CompareOperator Always = 7
```

Always passes.

<a id="member-b64a72bc6762"></a>
### Equal

```csharp
public const Electron2D.RenderingDevice.CompareOperator Equal = 2
```

Passes when equal.

<a id="member-14908c4256d6"></a>
### Greater

```csharp
public const Electron2D.RenderingDevice.CompareOperator Greater = 4
```

Passes when incoming is greater.

<a id="member-0b9c05fea6a7"></a>
### GreaterOrEqual

```csharp
public const Electron2D.RenderingDevice.CompareOperator GreaterOrEqual = 6
```

Passes when incoming is greater or equal.

<a id="member-556497ce71f1"></a>
### Less

```csharp
public const Electron2D.RenderingDevice.CompareOperator Less = 1
```

Passes when incoming is less.

<a id="member-0d9593f1db17"></a>
### LessOrEqual

```csharp
public const Electron2D.RenderingDevice.CompareOperator LessOrEqual = 3
```

Passes when incoming is less or equal.

<a id="member-7d7f52116dba"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.CompareOperator Max = 8
```

Exclusive nonselectable bound.

<a id="member-7df76708579a"></a>
### Never

```csharp
public const Electron2D.RenderingDevice.CompareOperator Never = 0
```

Never passes.

<a id="member-42c400421b72"></a>
### NotEqual

```csharp
public const Electron2D.RenderingDevice.CompareOperator NotEqual = 5
```

Passes when different.

## Lifecycle, errors and verification

These values execute in the owning pipeline, sampler or draw pass. Device RIDs remain owner-thread resources. Unknown enum selectors and nonfinite bias/clear values reject. References must fit eight bits for visible stencil faces; both visible faces require equal effective masks/reference. Depth-bounds and raw aspect-transfer adapters remain explicit prerequisites in coverage. [RenderingDeviceDepthTests](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) verifies native pixels and warmed managed allocation on Linux/Vulkan; foreign platforms and native allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).
