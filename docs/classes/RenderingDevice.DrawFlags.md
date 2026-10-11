# RenderingDevice.DrawFlags

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.DrawFlags`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies DrawFlags values for executable graphics operations. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.DrawFlags ClearColor0 = 1` | Selects ClearColor0. |
| `public const Electron2D.RenderingDevice.DrawFlags ClearColor1 = 2` | Selects ClearColor1. |
| `public const Electron2D.RenderingDevice.DrawFlags ClearColor2 = 4` | Selects ClearColor2. |
| `public const Electron2D.RenderingDevice.DrawFlags ClearColor3 = 8` | Selects ClearColor3. |
| `public const Electron2D.RenderingDevice.DrawFlags ClearColor4 = 16` | Selects ClearColor4. |
| `public const Electron2D.RenderingDevice.DrawFlags ClearColor5 = 32` | Selects ClearColor5. |
| `public const Electron2D.RenderingDevice.DrawFlags ClearColor6 = 64` | Selects ClearColor6. |
| `public const Electron2D.RenderingDevice.DrawFlags ClearColor7 = 128` | Selects ClearColor7. |
| `public const Electron2D.RenderingDevice.DrawFlags ClearColorAll = 255` | Selects ClearColorAll. |
| `public const Electron2D.RenderingDevice.DrawFlags DefaultAll = 0` | Selects DefaultAll. |
| `public const Electron2D.RenderingDevice.DrawFlags IgnoreColor0 = 256` | Selects IgnoreColor0. |
| `public const Electron2D.RenderingDevice.DrawFlags IgnoreColor1 = 512` | Selects IgnoreColor1. |
| `public const Electron2D.RenderingDevice.DrawFlags IgnoreColor2 = 1024` | Selects IgnoreColor2. |
| `public const Electron2D.RenderingDevice.DrawFlags IgnoreColor3 = 2048` | Selects IgnoreColor3. |
| `public const Electron2D.RenderingDevice.DrawFlags IgnoreColor4 = 4096` | Selects IgnoreColor4. |
| `public const Electron2D.RenderingDevice.DrawFlags IgnoreColor5 = 8192` | Selects IgnoreColor5. |
| `public const Electron2D.RenderingDevice.DrawFlags IgnoreColor6 = 16384` | Selects IgnoreColor6. |
| `public const Electron2D.RenderingDevice.DrawFlags IgnoreColor7 = 32768` | Selects IgnoreColor7. |
| `public const Electron2D.RenderingDevice.DrawFlags IgnoreColorAll = 65280` | Selects IgnoreColorAll. |

## Member descriptions

<a id="member-75fbee59625b"></a>
### ClearColor0

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor0 = 1
```

Selects ClearColor0.


<a id="member-bfb195345dfd"></a>
### ClearColor1

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor1 = 2
```

Selects ClearColor1.


<a id="member-c787290c5683"></a>
### ClearColor2

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor2 = 4
```

Selects ClearColor2.


<a id="member-df1512a32cd4"></a>
### ClearColor3

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor3 = 8
```

Selects ClearColor3.


<a id="member-0d21a408fbe6"></a>
### ClearColor4

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor4 = 16
```

Selects ClearColor4.


<a id="member-0021eb6ec792"></a>
### ClearColor5

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor5 = 32
```

Selects ClearColor5.


<a id="member-a4418e178377"></a>
### ClearColor6

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor6 = 64
```

Selects ClearColor6.


<a id="member-1f7d3a79e85a"></a>
### ClearColor7

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColor7 = 128
```

Selects ClearColor7.


<a id="member-7188212b3585"></a>
### ClearColorAll

```csharp
public const Electron2D.RenderingDevice.DrawFlags ClearColorAll = 255
```

Selects ClearColorAll.


<a id="member-43f8831b8ef1"></a>
### DefaultAll

```csharp
public const Electron2D.RenderingDevice.DrawFlags DefaultAll = 0
```

Selects DefaultAll.


<a id="member-2ecd4b28b086"></a>
### IgnoreColor0

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor0 = 256
```

Selects IgnoreColor0.


<a id="member-75103d97dd4e"></a>
### IgnoreColor1

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor1 = 512
```

Selects IgnoreColor1.


<a id="member-6b6c5e6ec03b"></a>
### IgnoreColor2

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor2 = 1024
```

Selects IgnoreColor2.


<a id="member-cde070718775"></a>
### IgnoreColor3

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor3 = 2048
```

Selects IgnoreColor3.


<a id="member-94f13e670c27"></a>
### IgnoreColor4

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor4 = 4096
```

Selects IgnoreColor4.


<a id="member-310ab3cd335a"></a>
### IgnoreColor5

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor5 = 8192
```

Selects IgnoreColor5.


<a id="member-7a0344aa268f"></a>
### IgnoreColor6

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor6 = 16384
```

Selects IgnoreColor6.


<a id="member-9735de6cdd8d"></a>
### IgnoreColor7

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColor7 = 32768
```

Selects IgnoreColor7.


<a id="member-893cab40c205"></a>
### IgnoreColorAll

```csharp
public const Electron2D.RenderingDevice.DrawFlags IgnoreColorAll = 65280
```

Selects IgnoreColorAll.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
