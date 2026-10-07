# GraphFrame

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.GraphFrame`. **Source:** [source](../../src/Scene/GUI/GraphFrame.cs). **Component:** [Graph authoring](../components/graph-authoring.md).

## Description

Logical grouping frame whose titlebar and edge margin accept pointer hits while the interior passes through. Nested attachments belong to GraphEdit and never reparent elements. Autoshrink includes attachment bounds, margin and titlebar; disabling it preserves existing bounds while expanding to include members. Moving a frame moves its descendants once.

## Members

| Declaration | Contract |
| --- | --- |
| [`public GraphFrame()`](#member-e1a45b1265e8) | Creates an empty automatically shrinking frame with a centered title. |
| [`public event System.Action AutoshrinkChanged`](#member-f52be7e7b6b0) | Reports changed automatic sizing configuration. |
| [`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`](#member-301046cec155) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-b44dabe7c23d) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-bc5f9ab34fa7) | Inherited scene/input/layout/storage lifetime hook. |
| [`public Electron2D.HBoxContainer GetTitlebarHBox()`](#member-eb8dc6b0a3a6) | Returns the borrowed owned titlebar for additional application controls. |
| [`protected override System.Boolean HasPoint(Electron2D.Vector2 point)`](#member-e6eff2fb114d) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override Electron2D.Vector2 OnGetMinimumSize()`](#member-a7129e7e24ef) | Inherited scene/input/layout/storage lifetime hook. |
| [`protected override System.Void OnNotification(System.Int32 what)`](#member-235a8e9c176a) | Inherited scene/input/layout/storage lifetime hook. |
| [`public System.Boolean AutoshrinkEnabled { get; set; }`](#member-e67f2d7fe7de) | Gets or sets automatic bounds around attached elements. |
| [`public System.Int32 AutoshrinkMargin { get; set; }`](#member-c70bfdeaece3) | Gets or sets nonnegative padding around automatic attachment bounds. |
| [`public System.Int32 DragMargin { get; set; }`](#member-1fdc11f779dc) | Gets or sets the nonnegative draggable edge width. |
| [`public Electron2D.Color TintColor { get; set; }`](#member-7a7a55ef5863) | Gets or sets optional panel tint. |
| [`public System.Boolean TintColorEnabled { get; set; }`](#member-87d6631470bc) | Gets or sets whether panel tint overrides the themed panel colors. |
| [`public System.String Title { get; set; }`](#member-c276ae80ee4a) | Gets or sets the displayed title. |

## Example

```csharp
using var graph = new GraphEdit();
var frame = new GraphFrame { Name = "Group", Title = "Processing" };
var node = new GraphNode { Name = "Value", PositionOffset = new(100, 100) };
graph.AddChild(frame);
graph.AddChild(node);
graph.AttachGraphElementToFrame("Value", "Group");
frame.PositionOffset += new Vector2(20, 0);
```

## Lifetime and verification

Attached reads and mutations use the scene owner thread. Own mutations reject active port/connection drawing; borrowed resources remain caller-owned. GraphNode and GraphFrame titlebars and title labels, and GraphEdit menu/layers cannot be disposed independently. GraphElement/GraphEdit disposal releases owned children and event subscriptions. See the component for executable checks, preparation boundaries and remaining dependencies.

## Member details

<a id="member-e1a45b1265e8"></a>
### `GraphFrame()`

```csharp
public GraphFrame()
```

Creates an empty automatically shrinking frame with a centered title.

<a id="member-f52be7e7b6b0"></a>
### `AutoshrinkChanged`

```csharp
public event System.Action AutoshrinkChanged
```

Reports changed automatic sizing configuration.

<a id="member-301046cec155"></a>
### `CreateSceneInstanceFactory()`

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-b44dabe7c23d"></a>
### `Dispose(System.Boolean)`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-bc5f9ab34fa7"></a>
### `GetPropertyDescriptors()`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-eb8dc6b0a3a6"></a>
### `GetTitlebarHBox()`

```csharp
public Electron2D.HBoxContainer GetTitlebarHBox()
```

Returns the borrowed owned titlebar for additional application controls.

The horizontal titlebar.

<a id="member-e6eff2fb114d"></a>
### `HasPoint(Electron2D.Vector2)`

```csharp
protected override System.Boolean HasPoint(Electron2D.Vector2 point)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-a7129e7e24ef"></a>
### `OnGetMinimumSize()`

```csharp
protected override Electron2D.Vector2 OnGetMinimumSize()
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-235a8e9c176a"></a>
### `OnNotification(System.Int32)`

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

Inherited scene/input/layout/storage lifetime hook.

<a id="member-e67f2d7fe7de"></a>
### `AutoshrinkEnabled`

```csharp
public System.Boolean AutoshrinkEnabled { get; set; }
```

Gets or sets automatic bounds around attached elements.

<a id="member-c70bfdeaece3"></a>
### `AutoshrinkMargin`

```csharp
public System.Int32 AutoshrinkMargin { get; set; }
```

Gets or sets nonnegative padding around automatic attachment bounds.

<a id="member-1fdc11f779dc"></a>
### `DragMargin`

```csharp
public System.Int32 DragMargin { get; set; }
```

Gets or sets the nonnegative draggable edge width.

<a id="member-7a7a55ef5863"></a>
### `TintColor`

```csharp
public Electron2D.Color TintColor { get; set; }
```

Gets or sets optional panel tint.

<a id="member-87d6631470bc"></a>
### `TintColorEnabled`

```csharp
public System.Boolean TintColorEnabled { get; set; }
```

Gets or sets whether panel tint overrides the themed panel colors.

<a id="member-c276ae80ee4a"></a>
### `Title`

```csharp
public System.String Title { get; set; }
```

Gets or sets the displayed title.
