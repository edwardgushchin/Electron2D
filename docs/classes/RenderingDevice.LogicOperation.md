# RenderingDevice.LogicOperation

Last updated: 2026-10-28

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.LogicOperation`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Logical color operations](../components/logic-operations.md).

## Description

Selects a copied pipeline-wide operation through [RDPipelineColorBlendState](RDPipelineColorBlendState.md). S and D are encoded source and stored destination component bits; complements use the component width. Integer and normalized targets execute these operations. Float and sRGB targets copy the unblended source. Per-attachment masks still apply.

## Example

```csharp
using var attachment = new RDPipelineColorBlendStateAttachment();
using var blend = new RDPipelineColorBlendState
{
    EnableLogicOp = true,
    LogicOp = RenderingDevice.LogicOperation.Xor,
    Attachments = [attachment]
};
```

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.LogicOperation Clear = 0` | `0`. |
| `public const Electron2D.RenderingDevice.LogicOperation And = 1` | `S & D`. |
| `public const Electron2D.RenderingDevice.LogicOperation AndReverse = 2` | `S & ~D`. |
| `public const Electron2D.RenderingDevice.LogicOperation Copy = 3` | `S`. |
| `public const Electron2D.RenderingDevice.LogicOperation AndInverted = 4` | `~S & D`. |
| `public const Electron2D.RenderingDevice.LogicOperation NoOp = 5` | `D`. |
| `public const Electron2D.RenderingDevice.LogicOperation Xor = 6` | `S ^ D`. |
| `public const Electron2D.RenderingDevice.LogicOperation Or = 7` | `S | D`. |
| `public const Electron2D.RenderingDevice.LogicOperation Nor = 8` | `~(S | D)`. |
| `public const Electron2D.RenderingDevice.LogicOperation Equivalent = 9` | `~(S ^ D)`. |
| `public const Electron2D.RenderingDevice.LogicOperation Invert = 10` | `~D`. |
| `public const Electron2D.RenderingDevice.LogicOperation OrReverse = 11` | `S | ~D`. |
| `public const Electron2D.RenderingDevice.LogicOperation CopyInverted = 12` | `~S`. |
| `public const Electron2D.RenderingDevice.LogicOperation OrInverted = 13` | `~S | D`. |
| `public const Electron2D.RenderingDevice.LogicOperation Nand = 14` | `~(S & D)`. |
| `public const Electron2D.RenderingDevice.LogicOperation Set = 15` | `all component bits set`. |
| `public const Electron2D.RenderingDevice.LogicOperation Max = 16` | `exclusive extent; rejected`. |

## Member descriptions

### Clear

```csharp
public const Electron2D.RenderingDevice.LogicOperation Clear = 0
```

Writes `0` before applying component masks.

### And

```csharp
public const Electron2D.RenderingDevice.LogicOperation And = 1
```

Writes `S & D` before applying component masks.

### AndReverse

```csharp
public const Electron2D.RenderingDevice.LogicOperation AndReverse = 2
```

Writes `S & ~D` before applying component masks.

### Copy

```csharp
public const Electron2D.RenderingDevice.LogicOperation Copy = 3
```

Writes `S` before applying component masks.

### AndInverted

```csharp
public const Electron2D.RenderingDevice.LogicOperation AndInverted = 4
```

Writes `~S & D` before applying component masks.

### NoOp

```csharp
public const Electron2D.RenderingDevice.LogicOperation NoOp = 5
```

Writes `D` before applying component masks.

### Xor

```csharp
public const Electron2D.RenderingDevice.LogicOperation Xor = 6
```

Writes `S ^ D` before applying component masks.

### Or

```csharp
public const Electron2D.RenderingDevice.LogicOperation Or = 7
```

Writes `S | D` before applying component masks.

### Nor

```csharp
public const Electron2D.RenderingDevice.LogicOperation Nor = 8
```

Writes `~(S | D)` before applying component masks.

### Equivalent

```csharp
public const Electron2D.RenderingDevice.LogicOperation Equivalent = 9
```

Writes `~(S ^ D)` before applying component masks.

### Invert

```csharp
public const Electron2D.RenderingDevice.LogicOperation Invert = 10
```

Writes `~D` before applying component masks.

### OrReverse

```csharp
public const Electron2D.RenderingDevice.LogicOperation OrReverse = 11
```

Writes `S | ~D` before applying component masks.

### CopyInverted

```csharp
public const Electron2D.RenderingDevice.LogicOperation CopyInverted = 12
```

Writes `~S` before applying component masks.

### OrInverted

```csharp
public const Electron2D.RenderingDevice.LogicOperation OrInverted = 13
```

Writes `~S | D` before applying component masks.

### Nand

```csharp
public const Electron2D.RenderingDevice.LogicOperation Nand = 14
```

Writes `~(S & D)` before applying component masks.

### Set

```csharp
public const Electron2D.RenderingDevice.LogicOperation Set = 15
```

Writes `all component bits set` before applying component masks.

### Max

```csharp
public const Electron2D.RenderingDevice.LogicOperation Max = 16
```

Marks the exclusive numeric extent; it is rejected as an operation.

## Lifetime and verification

Pipeline creation copies the selector and requires the enabled native feature only when logical operations are enabled. Unknown and extent values reject at assignment. DeviceGraphicsStorage.Consumer verifies all sixteen operations with signed/unsigned byte and 32-bit targets, normalized, float and sRGB formats in GLSL/HLSL. See the component for composition checks and verification limits.
