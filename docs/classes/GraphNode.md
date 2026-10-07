# GraphNode

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.GraphNode`. **Source:** [source](../../src/Scene/GUI/GraphNode.cs). **Component:** [Graph authoring](../components/graph-authoring.md).

## Description

Titled rows with independently enabled typed input/output ports. Slot indices identify visible direct rows; enabled ports use compressed indices. Borrowed icons and exact generic runtime metadata stay separate from stored configuration. Row layout respects themed panel/slot margins and weighted min/max expansion.

## Members

| Declaration | Contract |
| --- | --- |
| [`public GraphNode()`](#member-9dce66031c3f) | Creates an empty graph node with an owned titlebar and accessibility focus policy. |
| [`public event System.Action SlotSizesChanged`](#member-67b4e15fd276) | Reports changed row geometry after layout. |
| [`public event System.Action<System.Int32> SlotUpdated`](#member-0716f4311449) | Reports a changed slot configuration. |
| [`public System.Void ClearAllSlots()`](#member-74a1bd38711a) | Removes all slot configuration and metadata. |
| [`public System.Void ClearSlot(System.Int32 slotIndex)`](#member-d5526349098c) | Removes the slot configuration and borrowed metadata. |
| [`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`](#member-af4c93e6592d) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-3c0f4403497d) | Inherited scene/input/layout/storage lifetime hook. |
| [`public Electron2D.Color GetInputPortColor(System.Int32 portIndex)`](#member-0773a018eaf5) | Reads input port color under its compressed index. |
| [`public System.Int32 GetInputPortCount()`](#member-ddb7f5bd2982) | Counts enabled input ports. |
| [`public Electron2D.Vector2 GetInputPortPosition(System.Int32 portIndex)`](#member-edd09eb3d213) | Reads input port position under its compressed index. |
| [`public System.Int32 GetInputPortSlot(System.Int32 portIndex)`](#member-40be8f6c3b7b) | Reads input port slot under its compressed index. |
| [`public System.Int32 GetInputPortType(System.Int32 portIndex)`](#member-6ebdca0fa8f8) | Reads input port type under its compressed index. |
| [`public Electron2D.Color GetOutputPortColor(System.Int32 portIndex)`](#member-2c8879d0a4d5) | Reads output port color under its compressed index. |
| [`public System.Int32 GetOutputPortCount()`](#member-3e447aafe4cd) | Counts enabled output ports. |
| [`public Electron2D.Vector2 GetOutputPortPosition(System.Int32 portIndex)`](#member-69855e96c24b) | Reads output port position under its compressed index. |
| [`public System.Int32 GetOutputPortSlot(System.Int32 portIndex)`](#member-30044777de5b) | Reads output port slot under its compressed index. |
| [`public System.Int32 GetOutputPortType(System.Int32 portIndex)`](#member-b04c7f599008) | Reads output port type under its compressed index. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-bceef4318e06) | Inherited scene/input/layout/storage lifetime hook. |
| [`public Electron2D.Color GetSlotColorLeft(System.Int32 slotIndex)`](#member-ad132bf1ee1e) | Reads the left port color for a slot. |
| [`public Electron2D.Color GetSlotColorRight(System.Int32 slotIndex)`](#member-d0ccd315d9ac) | Reads the right port color for a slot. |
| [`public Electron2D.Texture GetSlotCustomIconLeft(System.Int32 slotIndex)`](#member-96508c9053d0) | Reads the left port customicon for a slot. |
| [`public Electron2D.Texture GetSlotCustomIconRight(System.Int32 slotIndex)`](#member-cd14fae377d6) | Reads the right port customicon for a slot. |
| [`public Electron2D.GraphSlotMetadata GetSlotMetadataLeft(System.Int32 slotIndex)`](#member-231c8882facc) | Reads the left port metadata for a slot. |
| [`public Electron2D.GraphSlotMetadata GetSlotMetadataRight(System.Int32 slotIndex)`](#member-1559e059e7b3) | Reads the right port metadata for a slot. |
| [`public System.Int32 GetSlotTypeLeft(System.Int32 slotIndex)`](#member-94d6102ae865) | Reads the left port type for a slot. |
| [`public System.Int32 GetSlotTypeRight(System.Int32 slotIndex)`](#member-dff8efbe9fc2) | Reads the right port type for a slot. |
| [`public Electron2D.HBoxContainer GetTitlebarHBox()`](#member-0ae4ecc0a909) | Returns the borrowed titlebar; additional buttons may be added as normal children. |
| [`public System.Boolean IsSlotDrawStylebox(System.Int32 slotIndex)`](#member-0bc19ff03ec3) | Reports whether a slot draws its themed row decoration. |
| [`public System.Boolean IsSlotEnabledLeft(System.Int32 slotIndex)`](#member-02cd9bf4a6ba) | Reads the left port enabled for a slot. |
| [`public System.Boolean IsSlotEnabledRight(System.Int32 slotIndex)`](#member-f0ba7c30c6b9) | Reads the right port enabled for a slot. |
| [`protected virtual System.Void OnDrawPort(System.Int32 slotIndex, Electron2D.Vector2i position, System.Boolean left, Electron2D.Color color)`](#member-d84a646f035a) | Draws a themed port; overrides may submit custom canvas commands. |
| [`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`](#member-9e0804ead1d5) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override Electron2D.Vector2 OnGetMinimumSize()`](#member-836cc87cf45c) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Void OnNotification(System.Int32 what)`](#member-6b90b9ff4c1d) | Inherited scene/input/layout/storage lifetime hook. |
| [`public System.Void SetSlot(System.Int32 slotIndex, System.Boolean enableLeftPort, System.Int32 typeLeft, Electron2D.Color colorLeft, System.Boolean enableRightPort, System.Int32 typeRight, Electron2D.Color colorRight, Electron2D.Texture customIconLeft = null, Electron2D.Texture customIconRight = null, System.Boolean drawStylebox = true)`](#member-48934f979d4a) | Replaces port configuration for one slot, retaining its metadata. |
| [`public System.Void SetSlotColorLeft(System.Int32 slotIndex, Electron2D.Color value)`](#member-4e712716ed8a) | Assigns the left port color. |
| [`public System.Void SetSlotColorRight(System.Int32 slotIndex, Electron2D.Color value)`](#member-0a64a0886f13) | Assigns the right port color. |
| [`public System.Void SetSlotCustomIconLeft(System.Int32 slotIndex, Electron2D.Texture value)`](#member-c5393db8d862) | Assigns the left port customicon. |
| [`public System.Void SetSlotCustomIconRight(System.Int32 slotIndex, Electron2D.Texture value)`](#member-00e6e83300b8) | Assigns the right port customicon. |
| [`public System.Void SetSlotDrawStylebox(System.Int32 slotIndex, System.Boolean enable)`](#member-1cd32f224f76) | Configures row decoration and its content margins. |
| [`public System.Void SetSlotEnabledLeft(System.Int32 slotIndex, System.Boolean value)`](#member-14907f3b081f) | Assigns the left port enabled. |
| [`public System.Void SetSlotEnabledRight(System.Int32 slotIndex, System.Boolean value)`](#member-c87562a81aca) | Assigns the right port enabled. |
| [`public System.Void SetSlotMetadataLeft<T>(System.Int32 slotIndex, T value)`](#member-baefc79e2246) | Assigns an exact typed borrowed runtime payload. |
| [`public System.Void SetSlotMetadataRight<T>(System.Int32 slotIndex, T value)`](#member-27ff544256de) | Assigns an exact typed borrowed runtime payload. |
| [`public System.Void SetSlotTypeLeft(System.Int32 slotIndex, System.Int32 value)`](#member-96f6c77a646c) | Assigns the left port type. |
| [`public System.Void SetSlotTypeRight(System.Int32 slotIndex, System.Int32 value)`](#member-782fb60d48e3) | Assigns the right port type. |
| [`public System.Boolean IgnoreInvalidConnectionType { get; set; }`](#member-6261e846b5c0) | Gets or sets whether this node accepts differently typed connection targets. |
| [`public Electron2D.FocusMode SlotsFocusMode { get; set; }`](#member-71d6c5d5e625) | Gets or sets Click, All or Accessibility focus for graph rows. |
| [`public System.String Title { get; set; }`](#member-e66a67457f05) | Gets or sets the title displayed by the owned label. |

## Example

```csharp
using var node = new GraphNode { Title = "Value", Size = new(180, 100) };
node.AddChild(new Label { Text = "Number" });
node.SetSlot(0, true, 7, Colors.Cyan, true, 7, Colors.Orange);
node.SetSlotMetadataRight(0, "application-output");
if (node.GetSlotMetadataRight(0)!.TryGet<string>(out var value)) Console.WriteLine(value);
```

## Lifetime and verification

Attached reads and mutations use the scene owner thread. Own mutations reject active port/connection drawing; borrowed resources remain caller-owned. GraphNode and GraphFrame titlebars and title labels, and GraphEdit menu/layers cannot be disposed independently. GraphElement/GraphEdit disposal releases owned children and event subscriptions. See the component for executable checks, preparation boundaries and remaining dependencies.

## Member details

<a id="member-9dce66031c3f"></a>
### `GraphNode()`

```csharp
public GraphNode()
```

Creates an empty graph node with an owned titlebar and accessibility focus policy.

<a id="member-67b4e15fd276"></a>
### `SlotSizesChanged`

```csharp
public event System.Action SlotSizesChanged
```

Reports changed row geometry after layout.

<a id="member-0716f4311449"></a>
### `SlotUpdated`

```csharp
public event System.Action<System.Int32> SlotUpdated
```

Reports a changed slot configuration.

<a id="member-74a1bd38711a"></a>
### `ClearAllSlots()`

```csharp
public System.Void ClearAllSlots()
```

Removes all slot configuration and metadata.

<a id="member-d5526349098c"></a>
### `ClearSlot(System.Int32)`

```csharp
public System.Void ClearSlot(System.Int32 slotIndex)
```

Removes the slot configuration and borrowed metadata.

- `slotIndex`: Nonnegative slot.

<a id="member-af4c93e6592d"></a>
### `CreateSceneInstanceFactory()`

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-3c0f4403497d"></a>
### `Dispose(System.Boolean)`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-0773a018eaf5"></a>
### `GetInputPortColor(System.Int32)`

```csharp
public Electron2D.Color GetInputPortColor(System.Int32 portIndex)
```

Reads input port color under its compressed index.

Current port color.

- `portIndex`: Valid enabled-port index.

<a id="member-ddb7f5bd2982"></a>
### `GetInputPortCount()`

```csharp
public System.Int32 GetInputPortCount()
```

Counts enabled input ports.

Compressed port count.

<a id="member-edd09eb3d213"></a>
### `GetInputPortPosition(System.Int32)`

```csharp
public Electron2D.Vector2 GetInputPortPosition(System.Int32 portIndex)
```

Reads input port position under its compressed index.

Current port position.

- `portIndex`: Valid enabled-port index.

<a id="member-40be8f6c3b7b"></a>
### `GetInputPortSlot(System.Int32)`

```csharp
public System.Int32 GetInputPortSlot(System.Int32 portIndex)
```

Reads input port slot under its compressed index.

Current port slot.

- `portIndex`: Valid enabled-port index.

<a id="member-6ebdca0fa8f8"></a>
### `GetInputPortType(System.Int32)`

```csharp
public System.Int32 GetInputPortType(System.Int32 portIndex)
```

Reads input port type under its compressed index.

Current port type.

- `portIndex`: Valid enabled-port index.

<a id="member-2c8879d0a4d5"></a>
### `GetOutputPortColor(System.Int32)`

```csharp
public Electron2D.Color GetOutputPortColor(System.Int32 portIndex)
```

Reads output port color under its compressed index.

Current port color.

- `portIndex`: Valid enabled-port index.

<a id="member-3e447aafe4cd"></a>
### `GetOutputPortCount()`

```csharp
public System.Int32 GetOutputPortCount()
```

Counts enabled output ports.

Compressed port count.

<a id="member-69855e96c24b"></a>
### `GetOutputPortPosition(System.Int32)`

```csharp
public Electron2D.Vector2 GetOutputPortPosition(System.Int32 portIndex)
```

Reads output port position under its compressed index.

Current port position.

- `portIndex`: Valid enabled-port index.

<a id="member-30044777de5b"></a>
### `GetOutputPortSlot(System.Int32)`

```csharp
public System.Int32 GetOutputPortSlot(System.Int32 portIndex)
```

Reads output port slot under its compressed index.

Current port slot.

- `portIndex`: Valid enabled-port index.

<a id="member-b04c7f599008"></a>
### `GetOutputPortType(System.Int32)`

```csharp
public System.Int32 GetOutputPortType(System.Int32 portIndex)
```

Reads output port type under its compressed index.

Current port type.

- `portIndex`: Valid enabled-port index.

<a id="member-bceef4318e06"></a>
### `GetPropertyDescriptors()`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-ad132bf1ee1e"></a>
### `GetSlotColorLeft(System.Int32)`

```csharp
public Electron2D.Color GetSlotColorLeft(System.Int32 slotIndex)
```

Reads the left port color for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-d0ccd315d9ac"></a>
### `GetSlotColorRight(System.Int32)`

```csharp
public Electron2D.Color GetSlotColorRight(System.Int32 slotIndex)
```

Reads the right port color for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-96508c9053d0"></a>
### `GetSlotCustomIconLeft(System.Int32)`

```csharp
public Electron2D.Texture GetSlotCustomIconLeft(System.Int32 slotIndex)
```

Reads the left port customicon for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-cd14fae377d6"></a>
### `GetSlotCustomIconRight(System.Int32)`

```csharp
public Electron2D.Texture GetSlotCustomIconRight(System.Int32 slotIndex)
```

Reads the right port customicon for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-231c8882facc"></a>
### `GetSlotMetadataLeft(System.Int32)`

```csharp
public Electron2D.GraphSlotMetadata GetSlotMetadataLeft(System.Int32 slotIndex)
```

Reads the left port metadata for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-1559e059e7b3"></a>
### `GetSlotMetadataRight(System.Int32)`

```csharp
public Electron2D.GraphSlotMetadata GetSlotMetadataRight(System.Int32 slotIndex)
```

Reads the right port metadata for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-94d6102ae865"></a>
### `GetSlotTypeLeft(System.Int32)`

```csharp
public System.Int32 GetSlotTypeLeft(System.Int32 slotIndex)
```

Reads the left port type for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-dff8efbe9fc2"></a>
### `GetSlotTypeRight(System.Int32)`

```csharp
public System.Int32 GetSlotTypeRight(System.Int32 slotIndex)
```

Reads the right port type for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-0ae4ecc0a909"></a>
### `GetTitlebarHBox()`

```csharp
public Electron2D.HBoxContainer GetTitlebarHBox()
```

Returns the borrowed titlebar; additional buttons may be added as normal children.

The owned horizontal container.

<a id="member-0bc19ff03ec3"></a>
### `IsSlotDrawStylebox(System.Int32)`

```csharp
public System.Boolean IsSlotDrawStylebox(System.Int32 slotIndex)
```

Reports whether a slot draws its themed row decoration.

True by default.

- `slotIndex`: Nonnegative slot.

<a id="member-02cd9bf4a6ba"></a>
### `IsSlotEnabledLeft(System.Int32)`

```csharp
public System.Boolean IsSlotEnabledLeft(System.Int32 slotIndex)
```

Reads the left port enabled for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-f0ba7c30c6b9"></a>
### `IsSlotEnabledRight(System.Int32)`

```csharp
public System.Boolean IsSlotEnabledRight(System.Int32 slotIndex)
```

Reads the right port enabled for a slot.

Configured value or its default.

- `slotIndex`: Nonnegative slot.

<a id="member-d84a646f035a"></a>
### `OnDrawPort(System.Int32, Electron2D.Vector2i, System.Boolean, Electron2D.Color)`

```csharp
protected virtual System.Void OnDrawPort(System.Int32 slotIndex, Electron2D.Vector2i position, System.Boolean left, Electron2D.Color color)
```

Draws a themed port; overrides may submit custom canvas commands.

- `slotIndex`: Visible row index.
- `position`: Integer local port center.
- `left`: Whether this is an input port.
- `color`: Configured tint.

<a id="member-9e0804ead1d5"></a>
### `OnGUIInput(Electron2D.InputEvent)`

```csharp
protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-836cc87cf45c"></a>
### `OnGetMinimumSize()`

```csharp
protected override Electron2D.Vector2 OnGetMinimumSize()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-6b90b9ff4c1d"></a>
### `OnNotification(System.Int32)`

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-48934f979d4a"></a>
### `SetSlot(System.Int32, System.Boolean, System.Int32, Electron2D.Color, System.Boolean, System.Int32, Electron2D.Color, Electron2D.Texture, Electron2D.Texture, System.Boolean)`

```csharp
public System.Void SetSlot(System.Int32 slotIndex, System.Boolean enableLeftPort, System.Int32 typeLeft, Electron2D.Color colorLeft, System.Boolean enableRightPort, System.Int32 typeRight, Electron2D.Color colorRight, Electron2D.Texture customIconLeft = null, Electron2D.Texture customIconRight = null, System.Boolean drawStylebox = true)
```

Replaces port configuration for one slot, retaining its metadata.

- `slotIndex`: Zero through 65535.
- `enableLeftPort`: Input enabled.
- `typeLeft`: Input type identifier.
- `colorLeft`: Input tint.
- `enableRightPort`: Output enabled.
- `typeRight`: Output type identifier.
- `colorRight`: Output tint.
- `customIconLeft`: Borrowed input icon or theme fallback.
- `customIconRight`: Borrowed output icon or theme fallback.
- `drawStylebox`: Whether row decoration contributes margins.

<a id="member-4e712716ed8a"></a>
### `SetSlotColorLeft(System.Int32, Electron2D.Color)`

```csharp
public System.Void SetSlotColorLeft(System.Int32 slotIndex, Electron2D.Color value)
```

Assigns the left port color.

- `slotIndex`: Nonnegative slot.
- `value`: New configuration.

<a id="member-0a64a0886f13"></a>
### `SetSlotColorRight(System.Int32, Electron2D.Color)`

```csharp
public System.Void SetSlotColorRight(System.Int32 slotIndex, Electron2D.Color value)
```

Assigns the right port color.

- `slotIndex`: Nonnegative slot.
- `value`: New configuration.

<a id="member-c5393db8d862"></a>
### `SetSlotCustomIconLeft(System.Int32, Electron2D.Texture)`

```csharp
public System.Void SetSlotCustomIconLeft(System.Int32 slotIndex, Electron2D.Texture value)
```

Assigns the left port customicon.

- `slotIndex`: Nonnegative slot.
- `value`: New configuration.

<a id="member-00e6e83300b8"></a>
### `SetSlotCustomIconRight(System.Int32, Electron2D.Texture)`

```csharp
public System.Void SetSlotCustomIconRight(System.Int32 slotIndex, Electron2D.Texture value)
```

Assigns the right port customicon.

- `slotIndex`: Nonnegative slot.
- `value`: New configuration.

<a id="member-1cd32f224f76"></a>
### `SetSlotDrawStylebox(System.Int32, System.Boolean)`

```csharp
public System.Void SetSlotDrawStylebox(System.Int32 slotIndex, System.Boolean enable)
```

Configures row decoration and its content margins.

- `slotIndex`: Nonnegative slot.
- `enable`: Whether to decorate.

<a id="member-14907f3b081f"></a>
### `SetSlotEnabledLeft(System.Int32, System.Boolean)`

```csharp
public System.Void SetSlotEnabledLeft(System.Int32 slotIndex, System.Boolean value)
```

Assigns the left port enabled.

- `slotIndex`: Nonnegative slot.
- `value`: New configuration.

<a id="member-c87562a81aca"></a>
### `SetSlotEnabledRight(System.Int32, System.Boolean)`

```csharp
public System.Void SetSlotEnabledRight(System.Int32 slotIndex, System.Boolean value)
```

Assigns the right port enabled.

- `slotIndex`: Nonnegative slot.
- `value`: New configuration.

<a id="member-baefc79e2246"></a>
### `SetSlotMetadataLeft(System.Int32, T)`

```csharp
public System.Void SetSlotMetadataLeft<T>(System.Int32 slotIndex, T value)
```

Assigns an exact typed borrowed runtime payload.

- `slotIndex`: Nonnegative slot.
- `value`: Borrowed payload.

<a id="member-27ff544256de"></a>
### `SetSlotMetadataRight(System.Int32, T)`

```csharp
public System.Void SetSlotMetadataRight<T>(System.Int32 slotIndex, T value)
```

Assigns an exact typed borrowed runtime payload.

- `slotIndex`: Nonnegative slot.
- `value`: Borrowed payload.

<a id="member-96f6c77a646c"></a>
### `SetSlotTypeLeft(System.Int32, System.Int32)`

```csharp
public System.Void SetSlotTypeLeft(System.Int32 slotIndex, System.Int32 value)
```

Assigns the left port type.

- `slotIndex`: Nonnegative slot.
- `value`: New configuration.

<a id="member-782fb60d48e3"></a>
### `SetSlotTypeRight(System.Int32, System.Int32)`

```csharp
public System.Void SetSlotTypeRight(System.Int32 slotIndex, System.Int32 value)
```

Assigns the right port type.

- `slotIndex`: Nonnegative slot.
- `value`: New configuration.

<a id="member-6261e846b5c0"></a>
### `IgnoreInvalidConnectionType`

```csharp
public System.Boolean IgnoreInvalidConnectionType { get; set; }
```

Gets or sets whether this node accepts differently typed connection targets.

<a id="member-71d6c5d5e625"></a>
### `SlotsFocusMode`

```csharp
public Electron2D.FocusMode SlotsFocusMode { get; set; }
```

Gets or sets Click, All or Accessibility focus for graph rows.

<a id="member-e66a67457f05"></a>
### `Title`

```csharp
public System.String Title { get; set; }
```

Gets or sets the title displayed by the owned label.
