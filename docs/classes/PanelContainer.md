# PanelContainer

Last updated: 2026-09-27

**Inherits:** [Container](Container.md), [Control](Control.md), CanvasItem, Node, ElectronObject · **Inherited By:** —

**Declaration:** `public class PanelContainer : Container` · **Source:** [Panel.cs](../../src/Scene/GUI/Panel.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

Draws its resolved `panel` StyleBox and fits every eligible direct control into the same content rectangle. Style margins contribute to minimum size and reduce propagated maximum bounds. This is an overlay/content-padding container; use BoxContainer or GridContainer to distribute children into separate cells.

```csharp
var panel = new PanelContainer { Size = new Vector2(120, 60) };
panel.AddChild(new Control { Name = "Content", CustomMinimumSize = new Vector2(20, 10) });
using var tree = new SceneTree(panel);
tree.ProcessFrame(0);
```

The built-in Theme supplies a real default style. The hierarchy owns the child, while external themes/styles remain borrowed. Set a child's Owner when it should be part of a packed scene.

## API summary

| Signature | Contract |
| --- | --- |
| `public PanelContainer()` | MouseFilter.Stop and inherited PropagateMaximumSize=true. |
| `protected override Vector2 OnGetMinimumSize()` | Largest eligible child bound minimum plus style minimum. |
| `protected override void OnNotification(int what)` | Paints during draw, arranges during sort and requests sorting on theme changes. |
| `protected override SizeFlags[] GetAllowedSizeFlagsHorizontal()` | Fill and three shrink choices. |
| `protected override SizeFlags[] GetAllowedSizeFlagsVertical()` | Fill and three shrink choices. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores the actual inherited Stop default. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Preserves exact PanelContainer identity. |

## Method descriptions

<a id="panelcontainer"></a>
**Constructor:** creates a detached Container that stops pointer input. Other Control/Container defaults remain inherited; no additional style property or automatic scene owner is introduced.

<a id="ongetminimumsize"></a>
**Minimum:** considers locally visible, non-top-level direct Control children, including when an ancestor is hidden. Non-Control children and hidden/top-level controls contribute nothing. It takes the componentwise largest child bound minimum and adds the style's GetMinimumSize. Before reading each bound minimum, maximum propagation supplies the container's finite maximum minus style minimum, clamped to zero; negative axes remain unbounded. If propagation is disabled, no per-container maximum cache is applied.

**Drawing:** resolves the inherited `panel` theme key and draws it over Rect2(Vector2.Zero, Size), independently of child fitting. Background painting occurs during NotificationDraw before the Draw event and user OnDraw; custom overlays need no base.OnDraw call. A missing style contributes zero spacing to layout but throws InvalidOperationException when drawing. The actual built-in theme normally supplies the style.

<a id="onnotification"></a>
**Arrangement:** during NotificationSortChildren, tree-visible eligible children receive the same rectangle at style.GetOffset(), with size `Max(Size - style.GetMinimumSize(), Vector2.Zero)`. Missing style uses zero offset/minimum. Every child is fitted through Container.FitChildInRect, including fill/shrink, RTL, anchor/transform reset and effective bounds. Negative available content size is clamped before that finite nonnegative fit contract. A style change requests sorting even when a base notification callback fails.

Arrangement uses a reusable child snapshot. Removed/disposed/ineligible children are skipped; changed tree membership stops stale work. Child fit failures are collected after later children are attempted, and snapshot state clears in finally. A reentrant request queues a later sort through the inherited Container mechanism.

<a id="getallowedsizeflagshorizontal"></a><a id="getallowedsizeflagsvertical"></a>
**Allowed flags:** returns caller-owned arrays containing Fill, ShrinkBegin, ShrinkCenter and ShrinkEnd. These are advisory choices; the container does not divide extra space between children and does not restrict arbitrary stored flags.

<a id="getpropertydescriptors"></a><a id="createsceneinstancefactory"></a>
**Storage/factory:** the MouseFilter descriptor uses Stop rather than Container.Pass, so revert and packing preserve the actual default. Exact PanelContainer factories preserve inherited Theme/variation/override and node ownership state; further subclasses provide their own factory.

## Lifecycle and verification

Container subscriptions, owner/capture guards and stale-sort protection remain inherited. Changes to child bounds/visibility/order, size and theme invalidate layout; ThemeOwner supplies style lookup and content-change notification. Neither the node nor the shared lookup helper disposes a caller-owned style.

[ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified. Inherited semantic accessibility, missing Font resources and project-theme loading remain separate. See [coverage](../coverage/classes/PanelContainer.md) and [ADR 0083](../decisions/rendering.md#adr-0083).
