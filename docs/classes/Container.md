# Container

Last updated: 2026-09-27

**Inherits:** [Control](Control.md), CanvasItem, Node, ElectronObject · **Inherited By:** [BoxContainer](BoxContainer.md)

**Declaration:** `public class Container : Control` · **Source:** [Container.cs](../../src/Scene/GUI/Container.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

Base for owner-thread arrangement of direct child controls. Container itself assigns no automatic layout; derived types consume NotificationSortChildren. Defaults are MouseFilter.Pass and PropagateMaximumSize=true. Ordinary Control input, anchors, clipping, lifetime and storage remain inherited.

```csharp
var box = new HBoxContainer { Size = new Vector2(100, 40) };
box.AddChild(new Control { Name = "Child", CustomMinimumSize = new Vector2(10, 8),
    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
using var tree = new SceneTree(box);
tree.ProcessFrame(0);
```

## API summary

| Signature | Contract |
| --- | --- |
| `public Container()` | Initializes pass-through input, maximum propagation and child listeners. |
| `public const int NotificationPreSortChildren = 50` | Pre-arrangement notification. |
| `public const int NotificationSortChildren = 51` | Arrangement notification. |
| `public void QueueSort()` | Coalesced attached-only deferred arrangement. |
| `public void FitChildInRect(Control child, Rect2 rect)` | Fits a live direct child to a finite nonnegative allocation. |
| `public event Action? PreSortChildren` | Runs after pre-sort notification. |
| `public event Action? SortChildren` | Runs after concrete sort notification. |
| `protected virtual SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Returns advisory horizontal choices. |
| `protected virtual SizeFlags[] GetAllowedSizeFlagsVertical()` | Returns advisory vertical choices. |
| `protected override void OnNotification(int what)` | Queues sort for resize/direction/visibility changes. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds exact inherited defaults. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Preserves exact Container identity. |
| `protected override void Dispose(bool disposing)` | Releases child subscriptions and events. |

## Method descriptions

<a id="queuesort"></a>
**QueueSort:** detached requests do nothing. Attached requests reuse one cached action. A pending pass stays pending during notifications/events, so repeated requests and requests made by those callbacks coalesce. SceneTree captures one action batch; work enqueued after capture waits for another flush. Each entry caches a membership-specific action; stale actions from prior tree memberships are ignored. Entry, child add/remove/order, child flags/bounds/visibility, resize, layout direction and becoming visible request layout. Exit clears pending membership. Errors in required phases are collected after later phases are attempted; ordinary multicast subscriber ordering applies within one event.

<a id="fitchildinrect"></a>
**FitChildInRect:** requires a live direct child; null throws ArgumentNullException and wrong/disposed child or invalid allocation throws ArgumentException. Fill uses the allocation and inherited bounds/growth; non-fill uses current bound minimum, aligned Begin/Center/End. End has priority, horizontal alignment respects RTL, center offsets floor. Current intrinsic desired size defaults to zero; future desired-size consumers need the associated internal dependency slice. Resets anchors to zero, commits the rectangle in one reflow, then resets rotation and scale. Geometry callbacks can fail after state commits.

<a id="getallowedsizeflagshorizontal"></a><a id="getallowedsizeflagsvertical"></a>
**Allowed flags hooks:** caller-owned arrays contain Fill, Expand, ShrinkBegin, ShrinkCenter and ShrinkEnd. These are advisory inspector choices, with no editor implementation or restriction of arbitrary stored bits. Boxes omit Expand on their cross axis.

<a id="onnotification"></a><a id="getpropertydescriptors"></a><a id="createsceneinstancefactory"></a><a id="dispose"></a>
**Overrides:** preserve base notification behavior, typed stored defaults and exact factory identity; disposal unsubscribes direct controls before base teardown. A custom subclass supplies its own packing factory under the existing Node contract.

## Event and constant descriptions

<a id="notificationpresortchildren"></a><a id="presortchildren"></a><a id="notificationsortchildren"></a><a id="sortchildren"></a>
Order is NotificationPreSortChildren (50), PreSortChildren, NotificationSortChildren (51), SortChildren. Box arrangement executes in notification 51 before SortChildren. Disposed/detached state suppresses later callbacks. Failures aggregate through the scene flush/frame after required work.

## Lifecycle, errors and verification

Owner/lifetime guards apply to reads and mutations; scene capture rejects mutation. Removing controls clears parent allocation caches and listeners. Cached action queues and reusable box slots allocate zero managed bytes after capacity preparation in the measured small hierarchy. [BoxContainerTests](../../tests/Electron2D.Tests/BoxContainerTests.cs) checks phases, coalescing, failures, membership, flags/default reverts/packing, stale cross-tree sorts and captured batches. [Native checks](../../tests/Electron2D.Tests/BoxContainerRenderingTests.cs) render actual child rectangles on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, other platforms and owner acceptance remain unverified.

AccessibilityRegion remains Blocked: native semantic landmark publication/update/removal needs the accessibility service and viewport/control semantic identity. [Coverage](../coverage/classes/Container.md) leaves the class Partial for that exact dependency. No inert semantic property is exposed. See [ADR 0081](../decisions/rendering.md#adr-0081).
