# Panel

Last updated: 2026-09-27

**Inherits:** [Control](Control.md), CanvasItem, Node, ElectronObject · **Inherited By:** —

**Declaration:** `public class Panel : Control` · **Source:** [Panel.cs](../../src/Scene/GUI/Panel.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

Draws the resolved `panel` StyleBox across its local rectangle. A Panel borrows its style through inherited theme lookup and stops pointer input by default. It does not arrange children or add style margins to its intrinsic minimum; use [PanelContainer](PanelContainer.md) when children need content padding.

```csharp
var panel = new Panel { Size = new Vector2(120, 60) };
// Attach panel to a scene: the built-in theme supplies its visible panel style.
```

To customize the decoration, provide a Theme or use inherited `AddThemeStyleBoxOverride("panel", style)` with a caller-owned style. Theme content changes invalidate retained drawing through the shared owner mechanism.

## API summary

| Signature | Contract |
| --- | --- |
| `public Panel()` | MouseFilter.Stop; ordinary Control geometry/theme defaults. |
| `protected override void OnNotification(int what)` | Paints the resolved panel during NotificationDraw. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Preserves exact Panel identity. |

## Member descriptions

<a id="panel"></a>
**Constructor:** creates a detached themed control. It does not create a native window or take ownership of a caller-supplied theme/style. The built-in default theme makes ordinary panel drawing executable.

<a id="onnotification"></a>
**OnNotification:** resolves the current `panel` key and delegates to DrawStyleBox in the normal recording scope. Painting occurs during NotificationDraw before the Draw event and user OnDraw, so subclass overlays need no base.OnDraw call. The current CanvasItem transform, modulation, clipping and draw ordering apply. If lookup returns no style, drawing throws InvalidOperationException instead of silently producing an undecorated panel. A disposed borrowed style follows inherited lifetime failure behavior.

<a id="createsceneinstancefactory"></a>
**Factory:** creates exact Panel instances for typed scene packing; a further subclass supplies its own exact factory under Node. Inherited stored Theme, variation, overrides and Control values follow their own contracts.

## Verification and limitations

[ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified. Inherited GUI, theme, platform and ownership limits remain explicit in [Control](Control.md), [ThemeDB](ThemeDB.md), [coverage](../coverage/classes/Panel.md) and [ADR 0083](../decisions/rendering.md#adr-0083).
