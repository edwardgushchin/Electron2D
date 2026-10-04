# SubViewportContainer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class SubViewportContainer : Container`.

**Inherits:** [Container](Container.md). **Source:** [SubViewportContainer.cs](../../src/Scene/GUI/SubViewportContainer.cs).

## Description

Displays direct [SubViewport](SubViewport.md) textures at the local origin in sibling order. Nonstretched minimum size is the componentwise maximum of child native sizes. Stretch removes that intrinsic minimum, fills the control rectangle and owns child resolution, divided by positive StretchShrink and clamped to two pixels per axis. Manual child Size writes reject before changing native state while stretched. Visibility sets direct child update mode to Always/Disabled; attachment and visibility configuration set HandleInputLocally=false. Removed viewports retain their committed policies. Size changes notify the container's minimum/redraw; native resize is cold. Control transforms also transform the displayed image and pointer localization.

Positional input forwards from GUIInput after control-local conversion, optional shrink division and the child final-transform inverse. Nonpositional input forwards before root GUI when this container has focus, otherwise at unhandled input. PropagateInputEvent runs once on the borrowed pre-shrink event; false suppresses delivery without accepting it. Callback failures aggregate after later child dispatch. Public reentrant PushInput remains rejected; only the internal direct-child forwarding path nests dispatch and restores its parent context.

Each viewport owns independent focus, hover, mouse/touch capture and tooltip state. Focusing an embedded control also focuses its containing containers for keyboard routing. Native committed text/IME finds the deepest focused embedded control. Connected viewport sections share drag payload, target, preview and result; drag travel/attempt state stays local. MouseTarget chooses this container instead of embedded controls for cursor/drop targeting, without suppressing forwarding or hover. Root/child and child/root drops convert positions between their actual viewport coordinate spaces. Cursor selection ignores this container's default cursor and warns when it is not Arrow. Missing direct viewports produce a configuration warning. Other container/Control inherited contracts remain documented on their owning pages.

The scene owns child nodes; native targets remain RenderingServer-owned. Attached mutation/query uses the scene owner, capture/native mutation guards remain enforced, and disposal releases GUI state and native targets through existing lifetime paths. In-memory PackedScene stores these typed properties; derived container classes supply their own reconstruction factory. File/editor workflows and native subwindows remain separate.

## Example

Public programmatic workflow, exercised by SubViewportContainerTests; provide your authored drawable under the view before running:

```csharp
var window = new Window();
var pane = new SubViewportContainer
{
    Size = new Vector2(320, 180),
    Stretch = true,
    StretchShrink = 2,
};
var view = new SubViewport();
view.AddChild(authoredDrawable);
pane.AddChild(view);
window.AddChild(pane);
Engine.Instance.Run(window);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public SubViewportContainer()` | Creates a click-focus container with nonpositional unhandled-input forwarding enabled. |

## Constructor Descriptions

<a id="member-2f23235bb233"></a>
### .ctor

`public SubViewportContainer()`

Creates a click-focus container with nonpositional unhandled-input forwarding enabled.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean MouseTarget { get; set; }` | Gets or sets whether mouse cursor and drag targeting select this container instead of embedded controls. |
| `public System.Boolean Stretch { get; set; }` | Gets or sets whether this control owns child viewport sizes and fills its rectangle. |
| `public System.Int32 StretchShrink { get; set; }` | Gets or sets the positive integer divisor of stretched child resolution. |

## Property Descriptions

<a id="member-a7f4f7e8f29a"></a>
### MouseTarget

`public System.Boolean MouseTarget { get; set; }`

Gets or sets whether mouse cursor and drag targeting select this container instead of embedded controls.

Value: False initially. Input forwarding and child hover remain active in either mode.

System.InvalidOperationException: Mutation is off-owner or capture-owned.

System.ObjectDisposedException: The container is disposed.

<a id="member-76ad238935eb"></a>
### Stretch

`public System.Boolean Stretch { get; set; }`

Gets or sets whether this control owns child viewport sizes and fills its rectangle.

Value: False initially.

Remarks: Equal assignments are silent. Changed assignments commit before resizing children and requesting layout/redraw.

System.InvalidOperationException: Mutation is off-owner or during scene capture/native submission.

System.ObjectDisposedException: The container is disposed.

<a id="member-2f88ab474c77"></a>
### StretchShrink

`public System.Int32 StretchShrink { get; set; }`

Gets or sets the positive integer divisor of stretched child resolution.

Value: One initially. Has no sizing effect while Stretch is false.

System.ArgumentOutOfRangeException: The divisor is less than one.

System.InvalidOperationException: Mutation is off-owner or capture-owned.

System.ObjectDisposedException: The container is disposed.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| `protected override Electron2D.Control.SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Returns the available horizontal sizing choices for a host inspector. |
| `protected override Electron2D.Control.SizeFlags[] GetAllowedSizeFlagsVertical()` | Returns the available vertical sizing choices for a host inspector. |
| `public override System.String[] GetConfigurationWarnings()` | Returns this node's current configuration warnings for tooling. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` |  |
| `protected override System.Void OnDraw()` | Records this node's retained canvas commands before its first visible frame and after QueueRedraw. |
| `protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)` | Processes a temporary control-local event before Electron2D.Control.GUIInput subscribers. |
| `protected override Electron2D.CursorShape OnGetCursorShape(Electron2D.Vector2 atPosition)` | Chooses a cursor for a control-local position; defaults to Electron2D.Control.MouseDefaultCursorShape. |
| `protected override Electron2D.Vector2 OnGetMinimumSize()` | Supplies the intrinsic minimum size; the base control has none. |
| `protected override System.Void OnInput(Electron2D.InputEvent inputEvent)` | Receives an input event during the first scene-input propagation stage. |
| `protected override System.Void OnNotification(System.Int32 what)` |  |
| `protected override System.Void OnUnhandledInput(Electron2D.InputEvent inputEvent)` | Receives an event that remains unhandled after earlier scene-input stages. |
| `protected virtual System.Boolean PropagateInputEvent(Electron2D.InputEvent inputEvent)` | Decides whether a borrowed event is forwarded to live direct child viewports. |

## Method Descriptions

<a id="member-369f72145a5e"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

Returns: A non-null factory that creates a fresh node of the exact same runtime type.

Remarks: The base implementation supports only an exact Electron2D.Node. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

System.NotSupportedException: A derived node has not explicitly supplied an instancing factory.

<a id="member-715a27a186b1"></a>
### GetAllowedSizeFlagsHorizontal

`protected override Electron2D.Control.SizeFlags[] GetAllowedSizeFlagsHorizontal()`

Returns the available horizontal sizing choices for a host inspector.

Returns: A caller-owned array. Choices are advisory and do not restrict stored flag bits.

<a id="member-8a3b53128564"></a>
### GetAllowedSizeFlagsVertical

`protected override Electron2D.Control.SizeFlags[] GetAllowedSizeFlagsVertical()`

Returns the available vertical sizing choices for a host inspector.

Returns: A caller-owned advisory array.

<a id="member-a1db2c8fabb9"></a>
### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Returns this node's current configuration warnings for tooling.

Returns: An empty array by default. Overrides return ordered warning messages and should include base warnings.

System.InvalidOperationException: An attached query runs off the scene owner thread.

System.ObjectDisposedException: This node is disposed.

Remarks: Reports attached clipping and group ancestors through physical node ancestry.

<a id="member-2d57f8cfdb33"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`



Remarks: Appends this class's typed hierarchy, ownership, processing, and automatic translation descriptors to the inherited descriptors.

<a id="member-dd52a44b6c8e"></a>
### OnDraw

`protected override System.Void OnDraw()`

Records this node's retained canvas commands before its first visible frame and after QueueRedraw.

Remarks: Runs on the scene owner thread during rendering. Geometry, texture and drawing-transform calls are valid during NotificationDraw, synchronous Draw handlers and this callback. The command list is cleared before entry. Transform and interval state start fresh on each replay.

<a id="member-745c1ff83d54"></a>
### OnGUIInput

`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`

Processes a temporary control-local event before Electron2D.Control.GUIInput subscribers.

inputEvent: Borrowed event valid only during synchronous dispatch.

<a id="member-61d2aba813ca"></a>
### OnGetCursorShape

`protected override Electron2D.CursorShape OnGetCursorShape(Electron2D.Vector2 atPosition)`

Chooses a cursor for a control-local position; defaults to Electron2D.Control.MouseDefaultCursorShape.

atPosition: Finite coordinates local to this control.

Returns: The shape selected for the given position.

<a id="member-05dd2717b0c5"></a>
### OnGetMinimumSize

`protected override Electron2D.Vector2 OnGetMinimumSize()`

Supplies the intrinsic minimum size; the base control has none.

<a id="member-24244fd03ae0"></a>
### OnInput

`protected override System.Void OnInput(Electron2D.InputEvent inputEvent)`

Receives an input event during the first scene-input propagation stage.

event: The live caller-owned event being dispatched.

Remarks: The callback runs synchronously on the scene-tree owner thread when Electron2D.Node.InputEnabled is true and Electron2D.Node.CanProcess allows the node. Call Electron2D.SceneTree.SetInputAsHandled to stop later stages.

<a id="member-41ea7698dc23"></a>
### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`



Remarks: Calls the base implementation, then maps enter, exit, ready, process, and physics-process notification IDs to the corresponding typed virtual callbacks. Pause and application-suspend notifications reset eligible physics presentation history. Manual Electron2D.ElectronObject.Notify(System.Int32) calls invoke callbacks but do not mutate tree membership, ready state, or delta values.

<a id="member-f3c9556aa5d0"></a>
### OnUnhandledInput

`protected override System.Void OnUnhandledInput(Electron2D.InputEvent inputEvent)`

Receives an event that remains unhandled after earlier scene-input stages.

event: The live caller-owned event being dispatched.

Remarks: The callback runs synchronously on the scene-tree owner thread when Electron2D.Node.UnhandledInputEnabled is true and Electron2D.Node.CanProcess allows the node.

<a id="member-71a5f454e4cc"></a>
### PropagateInputEvent

`protected virtual System.Boolean PropagateInputEvent(Electron2D.InputEvent inputEvent)`

Decides whether a borrowed event is forwarded to live direct child viewports.

inputEvent: The original container-local positional event or unchanged nonpositional event.

Returns: True by default; false suppresses forwarding without accepting the parent event.

Remarks: Called once before coordinate scaling. Overrides must not dispose the borrowed event.


## Verification and limits

[SubViewportContainerTests](../../tests/Electron2D.Tests/SubViewportContainerTests.cs) checks managed authoring, packing, nested input/focus/hover, failure recovery and cross-viewport drag. Linux Wayland GPU/hardware compatibility run eight composition phases, native cursor/click/keyboard focus pixels and stable-size rendering/mutation intervals; GPU repeats composition through HLSL/GLSL. Input copies, native allocations, other platforms, large scenes, editor/file authoring, multiview, independent native subwindows and human acceptance are not established by the rendering measurements. See [embedded viewport integration](../components/canvas-rendering.md#embedded-viewport-containers-and-gui).
