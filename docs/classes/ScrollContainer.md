# ScrollContainer

Last updated: 2026-09-30

**Inherits:** [Container](Container.md), [Control](Control.md), CanvasItem, [Node](Node.md), ElectronObject · **Inherited By:** —

**Declaration:** `public partial class ScrollContainer : Container` · **Source:** [ScrollContainer.cs](../../src/Scene/GUI/ScrollContainer.cs), [ScrollContainer.Input.cs](../../src/Scene/GUI/ScrollContainer.Input.cs) · **Component:** [Scrolling](../components/scrolling.md)

ScrollContainer lays out ordinary child controls in a clipped viewport and owns five actual internal children: two directional hint controls, an [HScrollBar](HScrollBar.md), a [VScrollBar](VScrollBar.md) and a focus-border panel. The bars returned by `GetHScrollBar` and `GetVScrollBar` are borrowed identities owned by the container. Ordinary `Children`, `ChildCount`, `GetChild` and PackedScene capture exclude internal children; the engine still processes, draws and routes input through them. One ordinary container child is recommended for a scrolling list or form. A configuration warning reports zero or multiple locally visible ordinary controls.

```csharp
var scroll = new ScrollContainer { Size = new(240, 160) };
var column = new VBoxContainer { CustomMinimumSize = new(320, 500) };
scroll.AddChild(column);
scroll.FollowFocus = true;
```

## API summary

| Signature | Contract |
| --- | --- |
| `public ScrollContainer()` | Creates the five internal children, enables clipping and samples the project touch deadzone. |
| `public enum ScrollMode` | Disabled, Auto, ShowAlways, ShowNever, Reserve, MaximizeFirst. |
| `public enum ScrollHintMode` | Disabled, All, TopAndLeft, BottomAndRight. |
| `public ScrollMode HorizontalScrollMode { get; set; }` | Initial Auto; controls horizontal scrolling and bar space. |
| `public ScrollMode VerticalScrollMode { get; set; }` | Initial Auto; controls vertical scrolling and bar space. |
| `public int ScrollHorizontal { get; set; }` | Integer horizontal offset; reads truncate the live bar value, assignment cancels an active drag. |
| `public int ScrollVertical { get; set; }` | Integer vertical offset; reads truncate the live bar value, assignment cancels an active drag. |
| `public float ScrollHorizontalCustomStep { get; set; }` | Proxies the horizontal bar's CustomStep, initially -1. |
| `public float ScrollVerticalCustomStep { get; set; }` | Proxies the vertical bar's CustomStep, initially -1. |
| `public bool ScrollHorizontalByDefault { get; set; }` | Initial false; Shift reverses the wheel axis choice. |
| `public int ScrollDeadzone { get; set; }` | Signed logical-pixel touch deadzone, sampled initially from ProjectSettings. |
| `public ScrollHintMode HintMode { get; set; }` | Initial Disabled; selects eligible directional edges. |
| `public bool TileScrollHint { get; set; }` | Initial false; repeat rather than stretch hint textures. |
| `public bool FollowFocus { get; set; }` | Initial false; reveal a newly focused descendant. |
| `public bool DrawFocusBorder { get; set; }` | Initial false; draw the separate focus panel around focused content. |
| `public HScrollBar GetHScrollBar()` | Returns the live internal horizontal bar. |
| `public VScrollBar GetVScrollBar()` | Returns the live internal vertical bar. |
| `public void EnsureControlVisible(Control control)` | Scrolls a descendant's transformed rectangle into view. |
| `public override string[] GetConfigurationWarnings()` | Reports the one-content recommendation when applicable. |
| `public event Action? ScrollStarted` | Fires once after touch drag exceeds deadzone. |
| `public event Action? ScrollEnded` | Fires once when the begun drag/inertia ends or is cancelled. |
| `protected override Vector2 OnGetMinimumSize()` | Accounts for child minimums, mode policies, bars and theme margins. |
| `protected override void OnGUIInput(InputEvent inputEvent)` | Handles wheel, pan, touch-style mouse drag and inertia. |
| `protected override void OnNotification(int what)` | Arranges content, draws panel, follows theme/focus and advances inertia. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores typed modes, offsets, focus and hint policies. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Recreates the five internals on exact-type scene instantiation. |
| `protected override void Dispose(bool disposing)` | Disconnects focus delivery and clears scroll signals. |

The constructor also sets inherited `ClipContents = true` and `PropagateMaximumSize = false`. `ScrollMode.Disabled` fits the child minimum and disallows movement; Auto displays a bar when the content exceeds the local extent; ShowAlways displays it unconditionally; ShowNever keeps scrolling available without a visible bar; Reserve holds the bar's space even while hidden; MaximizeFirst prefers the content's desired size until an enabled maximum caps it. Bar values and pages clamp through inherited Range policy. A mode change queues minimum-size and child-layout updates. Content fitting uses each ordinary child's size flags and maximum. RTL reserves the vertical bar on the leading side, while bar values still grow in their fixed axes.

Wheel input uses one eighth of the target bar's page and reaches the container through child controls with `MouseForcePassScrollEvents`. `ScrollHorizontalByDefault` XOR Shift selects the opposite axis for a vertical wheel. A pan gesture can move both enabled axes. Touch-style left-button motion starts after the signed deadzone, sends `NotificationScrollBegin` to descendants before `ScrollStarted`, and cancels a button press attempt. The end event precedes `NotificationScrollEnd`. Inertia samples motion only for a positive frame delta; deceleration clamps each axis at zero speed or a range edge. These callbacks use the scene owner thread; callback errors are collected after committed phases. Regular non-touch pointer drags remain button input.

`EnsureControlVisible` requires a descendant and uses its transformed bounds. It accounts for a queued offset that has not yet rearranged the child, so immediate focus changes remain correct. The container listens to its owning viewport's GUI focus event while ready and disconnects on exit or disposal. Hint controls choose a leading or trailing vertical gradient when only vertical overflow is active, otherwise horizontal gradients; simultaneous two-axis overflow suppresses both directional hints. Hints borrow the current theme icons and colors. The separate focus panel uses the `focus` style through an inherited local override and is inset by its style draw expansion to keep the border visible inside the clip. Source theme keys are `panel`, `focus`, two hint icons/colors, and `scrollbar_h_separation`/`scrollbar_v_separation`.

[ScrollContainerTests](../../tests/Electron2D.Tests/ScrollContainerTests.cs) checks internal ownership, typed packing, modes, offsets, touch deadzone, wheel axis choice, focus and hint visibility. [ScrollThemeTests](../../tests/Electron2D.Tests/ScrollThemeTests.cs) checks exact built-in assets; [ScrollRenderingTests](../../tests/Electron2D.Tests/ScrollRenderingTests.cs) checks clipped pixels and moved content on Linux Wayland GPU and compatibility, plus 64 warmed active offset/layout/render frames with zero managed allocation. Native allocations, other platforms and owner visual acceptance remain unverified. See [coverage](../coverage/classes/ScrollContainer.md).
