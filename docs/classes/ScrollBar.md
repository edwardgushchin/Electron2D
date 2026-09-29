# ScrollBar

Last updated: 2026-09-30

**Inherits:** [Range](Range.md), [Control](Control.md), CanvasItem, [Node](Node.md), ElectronObject · **Inherited By:** [HScrollBar](HScrollBar.md), [VScrollBar](VScrollBar.md)

**Declaration:** `public abstract class ScrollBar : Range` · **Source:** [ScrollBar.cs](../../src/Scene/GUI/ScrollBar.cs) · **Component:** [Scrolling](../components/scrolling.md)

ScrollBar records a themed track, thumb, focus decoration, and optional state arrows. HScrollBar and VScrollBar fix the axis. They borrow theme styles and textures, poll their revisions while attached, and retain texture cache residency only while needed. The built-in arrows are intentionally empty textures; a theme may provide real arrows. Range owns bounds, page, snapping, sharing, and the `ValueChanged` signal. ScrollBar starts with continuous `Step = 0` and `FocusMode.Accessibility`; no screen-reader service is active in this runtime, so the default does not receive keyboard focus. Set `FocusMode.All` to enable keyboard control.

```csharp
var bar = new HScrollBar { MinValue = 0, MaxValue = 300, Page = 100 };
bar.Scrolling += () => Console.WriteLine(bar.Value);
```

## API summary

| Signature | Contract |
| --- | --- |
| `protected ScrollBar(bool vertical = true)` | Fixes axis; derived concrete bars supply the public constructors. |
| `public float CustomStep { get; set; }` | Initially -1; nonnegative values replace `Step` for arrow/key increments. |
| `public event Action? Scrolling` | Fires after an interaction changes the final value; `ValueChanged` fires first. |
| `protected override Vector2 OnGetMinimumSize()` | Resolves icon, track, grabber and padding theme geometry. |
| `protected override void OnGUIInput(InputEvent inputEvent)` | Handles pointer/gesture/wheel and focused actions. |
| `protected override void OnNotification(int what)` | Draws and tracks resource/lifecycle changes. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores typed axis defaults, value, step and custom step. |
| `protected override void Dispose(bool disposing)` | Cancels interaction and releases renderer residency. |

## Member behavior

`CustomStep` retains raw float values, including NaN or infinity. A negative value or NaN uses inherited `Step`; a nonnegative value is the amount for the two optional arrow icons and matching keyboard actions. Assignment does not resnap `Value`. Wheel input moves by one eighth of `Page` (or one sixteenth of range when Page is zero), bounded below by `Step`; pan gestures use their axis delta and `Step`. Clicking the track outside the thumb moves by a page. Dragging maps thumb position to `Ratio` and ignores zero-length tracks instead of dividing by zero. Repeated key events repeat naturally; a held pointer button has no synthetic timer repeat.

`Scrolling` reports the final value only after a real interaction changes it beyond the approximate-equality tolerance. Direct `Value` assignment is silent for this event. Callback failures are aggregated after the committed value and remaining notification phases. Interaction state clears when the bar is hidden, disabled, paused, removed or disposed. `FocusMode.Accessibility` retains numeric value 3 and is intentionally ineligible without an accessibility service.

Minimum geometry uses the current theme's `scroll`, `grabber`, `decrement`, `increment`, and orientation padding keys. Drawing selects highlighted/pressed variants. Absent or disposed required theme items fail explicitly. The renderer consumes the current theme resource revision on a later redraw; the node does not own borrowed resources.

[ScrollBarTests](../../tests/Electron2D.Tests/ScrollBarTests.cs) checks defaults, wheel/event order, custom keyboard step, RTL direction, page click and degenerate dragging. [ScrollThemeTests](../../tests/Electron2D.Tests/ScrollThemeTests.cs) checks built-in resources. [ScrollRenderingTests](../../tests/Electron2D.Tests/ScrollRenderingTests.cs) covers bars as part of a scroll container on Linux Wayland GPU and compatibility backends, including 64 warmed active offset/layout/render frames with zero managed allocation. Native allocation, other platforms and owner visual acceptance remain unverified. See [coverage](../coverage/classes/ScrollBar.md).
