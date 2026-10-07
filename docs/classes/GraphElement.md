# GraphElement

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.GraphElement`. **Source:** [source](../../src/Scene/GUI/GraphElement.cs). **Component:** [Graph authoring](../components/graph-authoring.md).

## Description

Base graph-space position, selectable/draggable policies and request-driven resize/raise/delete events. PositionOffset is unscaled model placement; GraphEdit owns projection and stack ordering. Selection is ignored while Selectable is false. Direct eligible child controls share an overlay rectangle.

## Members

| Declaration | Contract |
| --- | --- |
| [`public GraphElement()`](#member-eed4a3c354e9) | Creates a movable, selectable graph element without a resize handle. |
| [`public event System.Action DeleteRequest`](#member-2d51b9e28475) | Requests deletion by the owning application. |
| [`public event System.Action<Electron2D.Vector2, Electron2D.Vector2> Dragged`](#member-054e4e6b2147) | Reports graph placement before and after a completed drag. |
| [`public event System.Action NodeDeselected`](#member-5612d8304e4e) | Reports deselection. |
| [`public event System.Action NodeSelected`](#member-5a81264425d5) | Reports selection. |
| [`public event System.Action PositionOffsetChanged`](#member-d625874fc8d8) | Reports a changed graph position. |
| [`public event System.Action RaiseRequest`](#member-bc148e7f52ee) | Requests raising this element in the graph's stacking order. |
| [`public event System.Action<Electron2D.Vector2> ResizeEnd`](#member-8dca08c1eb9a) | Reports the actual size when resizing ends. |
| [`public event System.Action<Electron2D.Vector2> ResizeRequest`](#member-dfebc27e16b3) | Requests a finite new size during pointer resizing. |
| [`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`](#member-62ccbe2a41ad) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-2006b2ac2a8c) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-00dfc71cd44b) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`](#member-fea9e3bd113e) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override Electron2D.CursorShape OnGetCursorShape(Electron2D.Vector2 atPosition)`](#member-aecc5d7a4484) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override Electron2D.Vector2 OnGetMinimumSize()`](#member-04e52fe829cb) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Void OnNotification(System.Int32 what)`](#member-f0486525ec5c) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Void ValidateDisposal()`](#member-1beaa040dee3) | Inherited scene/input/layout/storage lifetime hook. |
| [`public System.Boolean Draggable { get; set; }`](#member-7125d51c7beb) | Gets or sets whether pointer movement may move this element. |
| [`public Electron2D.Vector2 PositionOffset { get; set; }`](#member-2ca54630a91a) | Gets or sets finite placement in unscaled graph coordinates. |
| [`public System.Boolean Resizable { get; set; }`](#member-1e8137ed4dd5) | Gets or sets whether a resize handle issues resize requests. |
| [`public System.Boolean ScalingMenus { get; set; }`](#member-04f98f62ca4c) | Gets or sets whether descendant popup menus follow graph zoom. |
| [`public System.Boolean Selectable { get; set; }`](#member-c7c987bbdfd0) | Gets or sets selection eligibility; disabling it clears selection. |
| [`public System.Boolean Selected { get; set; }`](#member-e483a2819599) | Gets or sets selection, ignored while Selectable is false. |

## Example

```csharp
using var element = new GraphElement { Size = new(140, 80), Resizable = true };
element.ResizeRequest += size => element.Size = size;
element.Dragged += (from, to) => Console.WriteLine($"{from} -> {to}");
```

## Lifetime and verification

Attached reads and mutations use the scene owner thread. Own mutations reject active port/connection drawing; borrowed resources remain caller-owned. GraphNode and GraphFrame titlebars and title labels, and GraphEdit menu/layers cannot be disposed independently. GraphElement/GraphEdit disposal releases owned children and event subscriptions. See the component for executable checks, preparation boundaries and remaining dependencies.

## Member details

<a id="member-eed4a3c354e9"></a>
### `GraphElement()`

```csharp
public GraphElement()
```

Creates a movable, selectable graph element without a resize handle.

<a id="member-2d51b9e28475"></a>
### `DeleteRequest`

```csharp
public event System.Action DeleteRequest
```

Requests deletion by the owning application.

<a id="member-054e4e6b2147"></a>
### `Dragged`

```csharp
public event System.Action<Electron2D.Vector2, Electron2D.Vector2> Dragged
```

Reports graph placement before and after a completed drag.

<a id="member-5612d8304e4e"></a>
### `NodeDeselected`

```csharp
public event System.Action NodeDeselected
```

Reports deselection.

<a id="member-5a81264425d5"></a>
### `NodeSelected`

```csharp
public event System.Action NodeSelected
```

Reports selection.

<a id="member-d625874fc8d8"></a>
### `PositionOffsetChanged`

```csharp
public event System.Action PositionOffsetChanged
```

Reports a changed graph position.

<a id="member-bc148e7f52ee"></a>
### `RaiseRequest`

```csharp
public event System.Action RaiseRequest
```

Requests raising this element in the graph's stacking order.

<a id="member-8dca08c1eb9a"></a>
### `ResizeEnd`

```csharp
public event System.Action<Electron2D.Vector2> ResizeEnd
```

Reports the actual size when resizing ends.

<a id="member-dfebc27e16b3"></a>
### `ResizeRequest`

```csharp
public event System.Action<Electron2D.Vector2> ResizeRequest
```

Requests a finite new size during pointer resizing.

<a id="member-62ccbe2a41ad"></a>
### `CreateSceneInstanceFactory()`

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-2006b2ac2a8c"></a>
### `Dispose(System.Boolean)`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-00dfc71cd44b"></a>
### `GetPropertyDescriptors()`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-fea9e3bd113e"></a>
### `OnGUIInput(Electron2D.InputEvent)`

```csharp
protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-aecc5d7a4484"></a>
### `OnGetCursorShape(Electron2D.Vector2)`

```csharp
protected override Electron2D.CursorShape OnGetCursorShape(Electron2D.Vector2 atPosition)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-04e52fe829cb"></a>
### `OnGetMinimumSize()`

```csharp
protected override Electron2D.Vector2 OnGetMinimumSize()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-f0486525ec5c"></a>
### `OnNotification(System.Int32)`

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-1beaa040dee3"></a>
### `ValidateDisposal()`

```csharp
protected override System.Void ValidateDisposal()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-7125d51c7beb"></a>
### `Draggable`

```csharp
public System.Boolean Draggable { get; set; }
```

Gets or sets whether pointer movement may move this element.

<a id="member-2ca54630a91a"></a>
### `PositionOffset`

```csharp
public Electron2D.Vector2 PositionOffset { get; set; }
```

Gets or sets finite placement in unscaled graph coordinates.

<a id="member-1e8137ed4dd5"></a>
### `Resizable`

```csharp
public System.Boolean Resizable { get; set; }
```

Gets or sets whether a resize handle issues resize requests.

<a id="member-04f98f62ca4c"></a>
### `ScalingMenus`

```csharp
public System.Boolean ScalingMenus { get; set; }
```

Gets or sets whether descendant popup menus follow graph zoom.

<a id="member-c7c987bbdfd0"></a>
### `Selectable`

```csharp
public System.Boolean Selectable { get; set; }
```

Gets or sets selection eligibility; disabling it clears selection.

<a id="member-e483a2819599"></a>
### `Selected`

```csharp
public System.Boolean Selected { get; set; }
```

Gets or sets selection, ignored while Selectable is false.
