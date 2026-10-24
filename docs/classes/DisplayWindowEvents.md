# DisplayWindowEvents

Last updated: 2026-10-25

**Inherits:** [ElectronObject](ElectronObject.md)

**Source:** `src/Servers/Display/DisplayWindowEvents.cs`

**Declaration:** `public sealed class DisplayWindowEvents : ElectronObject`

A borrowed typed publisher for one live native window, obtained through `DisplayServer.GetWindowEvents(windowID)`. It has no public constructor and exposes no backend handle. `WindowID` is a read-only diagnostic property, also present in property discovery, retained after disposal. Zero identifies the main window; native child IDs are not reused during a display session.

## Lifetime and delivery

Live subscription changes and disposal require the display owner thread. Adding to a disposed publisher throws `ObjectDisposedException`; removing from one is harmless. Explicit disposal clears handlers without closing the window. A subsequent getter creates a fresh publisher. Native retirement clears and disposes its publisher before pressed-state release. Hiding an ordinary native scene child retires its identity; showing it again creates a new ID and publisher.

`Input` receives borrowed native, manually parsed and emulated events after raw/action state commits and before scene input. Window-tagged events select only their ID; untagged events broadcast in main/ascending child-ID order. The recipient cohort is captured at parse entry, with handlers captured as each publisher emits. New recipients wait for the next parse and disposed recipients are skipped. With active subscriptions, off-owner parsing rejects before state changes. Callbacks may stop the active main loop; stale scene delivery is then omitted. Duplicate an event that must outlive synchronous delivery.

Other notifications follow the existing native window/scene/static callback. Both callbacks are attempted if the first fails. The publisher and multicast delegate are captured before that callback; retiring/replacing the captured publisher omits its old notification and does not call the replacement. Ordinary C# multicast semantics stop later handlers of one publisher after a failure; independent input publishers and scene delivery continue and failures aggregate. `SetBlockSignals` affects this publisher only. Native pumping continues later queued events before throwing collected failures.

## Example

```csharp
var events = DisplayServer.GetWindowEvents(window.GetWindowID());
events.Input += input => Console.WriteLine(input.GetType().Name);
events.CloseRequested += () => window.Hide();
```

Subscribe while the native window is live on its owner thread. Retain the publisher to remove handlers; calling its `Dispose` clears all subscribers on that shared publisher.

## Public members

| Member | Payload and behavior |
| --- | --- |
| `int WindowID` | Retained native session ID. |
| `event Action<InputEvent>? Input` | Borrowed event after input-state commitment, before scene input. |
| `event Action<string>? TextInput` | Complete committed string after composition clears. |
| `event Action<string, Vector2i>? TextEditing` | Composing text and Unicode-codepoint selection after IME state commit. |
| `event Action? CloseRequested` | Ordinary close request; does not automatically close the window. |
| `event Action? ForceCloseRequested` | Native popup dismissal or parent retirement before automatic hide; failure cannot cancel retirement. Explicit hide/disposal is not a forced request. |
| `event Action? GoBackRequested` | Native system-back request. |
| `event Action? MouseEntered` / `MouseExited` | Native pointer boundary changes. |
| `event Action? DPIChanged` | Native content-scale change, not physical DPI. |
| `event Action<bool>? FocusChanged` | Focus state before the application focus notification. |
| `event Action<Rect2i>? RectChanged` | Effective client rectangle after native move/resize; Wayland position is zero. Native constraints and pixel rounding may change the requested size. |
| `event Action<IReadOnlyList<string>>? FilesDropped` | Owned ordered paths for one completed drop; interrupted groups are discarded. |

The protected `ValidateDisposal`, `Dispose(bool)` and `GetPropertyDescriptors` overrides retain owner-thread disposal, deterministic handler cleanup and read-only ID discovery. Inherited object signals and lifetime rules remain available.

## Verification limits

[DisplayWindowEventsTests](../../tests/Electron2D.Tests/DisplayWindowEventsTests.cs) checks native ID routing, all channels, state/scene order, emulation, broadcast, subscription replacement, retirement, thread rejection before input mutation, failure continuation and warmed zero managed allocation. [WindowEvents.Consumer](../../tests/WindowEvents.Consumer/Program.cs) uses only public API for native child resize, actual independent canvas pixels, hide/reopen lifetime and prepared input delivery. Native popup tests cover forced close before automatic hide and failure-safe nested teardown. Software Linux Wayland/X11 evidence does not establish physical input, real IME candidate UI, compositor file data offers, HDR/titlebar/ordinary readiness producers, native heap behavior or other platforms.
