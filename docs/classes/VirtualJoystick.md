# VirtualJoystick

Last updated: 2026-10-25

**Namespace:** `Electron2D`. **Declaration:** `public partial class VirtualJoystick : Control`. **Source:** [runtime](../../src/Scene/GUI/VirtualJoystick.cs), [storage](../../src/Scene/GUI/VirtualJoystick.Storage.cs). **Inherits:** [Control](Control.md). **Component:** [Touch game controls](../components/touch-game-controls.md).

## Description

A themed one-contact analog joystick. Fixed mode accepts contact within the base rectangle; Dynamic places the base at the accepted point; Following moves it when the contact exceeds the clamp radius while inside the control. Output direction uses local Control units. Effective radius is JoystickSize × ClampzoneRatio / 2; movement clamps to that radius and rescales outside DeadzoneRatio to [0,1]. Zero/nonpositive radius yields finite zero output. Negative finite authored ratios are retained; nonfinite inputs and overflowing effective radius/offset reject. Four owner/axis slots preserve other physical/manual/virtual producers, including duplicate action names. Base/tip styles use normal_joystick, pressed_joystick, normal_tip and pressed_tip through ordinary branch/project/default theme lookup. Geometry reset is silent; lifecycle cancellation retires state then emits Released without tap/flick. Release observers see retired state and receive the final vector; callback failure cannot skip required cleanup, and later reentrant reset suppresses older gesture outcomes. Contact, position and vector state are not stored.

Attached getters and mutations use the scene owner. Mutation rejects scene capture and disposal; undefined enum values, nonfinite geometry and disposed borrowed resources reject. Unknown or empty actions publish no contribution; configuration warnings identify nonempty unknown names. Lifecycle hide, pause, disable, focus loss, exit and disposal retire only owned contributions. Discrete button action events allocate; prepared joystick motion and ordinary drawing reuse retained storage. Selected authoring-subtree input produces no gameplay actions.

## Example

```csharp
var stick = new VirtualJoystick { Size = new Vector2(120, 120), JoystickMode = VirtualJoystickMode.Following };
scene.AddChild(stick);
// Gameplay reads Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").
```

## API summary

| Complete signature | Contract |
| --- | --- |
| `public VirtualJoystick()` | Public defaults. |
| `public VirtualJoystickMode JoystickMode { get; set; }` | Fixed initially; Fixed, Dynamic and Following choose base placement. |
| `public float JoystickSize { get; set; }` | 100 initially; finite nonnegative base diameter, changes reset contact silently. |
| `public float TipSize { get; set; }` | 50 initially; finite nonnegative tip diameter, changes reset contact silently. |
| `public float DeadzoneRatio { get; set; }` | 0 initially; finite fraction of clamp radius. At or above 1 suppresses clamped output. |
| `public float ClampzoneRatio { get; set; }` | 1 initially; finite radius multiplier. Nonpositive effective radius produces zero. |
| `public Vector2 InitialOffsetRatio { get; set; }` | (0.5,0.5) initially; finite normalized base offset within Size. Changes reset silently; values outside [0,1] remain authored. |
| `public JoystickVisibility VisibilityMode { get; set; }` | Always initially; WhenTouched draws only during contact, except selected authoring nodes. Ordinary Control visibility still applies. |
| `public string ActionLeft { get; set; }` | ui_left initially; negative horizontal slot. |
| `public string ActionRight { get; set; }` | ui_right initially; positive horizontal slot. |
| `public string ActionUp { get; set; }` | ui_up initially; negative vertical slot. |
| `public string ActionDown { get; set; }` | ui_down initially; positive vertical slot. |
| `public event Action? Pressed` | Contact, geometry and directional state are committed before callbacks. |
| `public event Action<Vector2>? Released` | Contact and axes are retired before callbacks; argument retains the final pre-release vector. |
| `public event Action? Tapped` | Normal stationary contact release; cancellation produces no tap. |
| `public event Action<Vector2>? Flicked` | Normal moved release with nonzero output; argument retains the final vector. |
| `public event Action? FlickCanceled` | Directional movement returns into the dead zone; a later renewed excursion can cancel again. |
| `public override string[] GetConfigurationWarnings()` | Unknown action names plus inherited diagnostics. |

## Property descriptions

### JoystickMode

`public VirtualJoystickMode JoystickMode { get; set; }`. Fixed initially; Fixed, Dynamic and Following choose base placement.

### JoystickSize

`public float JoystickSize { get; set; }`. 100 initially; finite nonnegative base diameter, changes reset contact silently.

### TipSize

`public float TipSize { get; set; }`. 50 initially; finite nonnegative tip diameter, changes reset contact silently.

### DeadzoneRatio

`public float DeadzoneRatio { get; set; }`. 0 initially; finite fraction of clamp radius. At or above 1 suppresses clamped output.

### ClampzoneRatio

`public float ClampzoneRatio { get; set; }`. 1 initially; finite radius multiplier. Nonpositive effective radius produces zero.

### InitialOffsetRatio

`public Vector2 InitialOffsetRatio { get; set; }`. (0.5,0.5) initially; finite normalized base offset within Size. Changes reset silently; values outside [0,1] remain authored.

### VisibilityMode

`public JoystickVisibility VisibilityMode { get; set; }`. Always initially; WhenTouched draws only during contact, except selected authoring nodes. Ordinary Control visibility still applies.

### ActionLeft

`public string ActionLeft { get; set; }`. ui_left initially; negative horizontal slot.

### ActionRight

`public string ActionRight { get; set; }`. ui_right initially; positive horizontal slot.

### ActionUp

`public string ActionUp { get; set; }`. ui_up initially; negative vertical slot.

### ActionDown

`public string ActionDown { get; set; }`. ui_down initially; positive vertical slot.

## Method descriptions

### Constructor

`public VirtualJoystick()` initializes the defaults listed above. It borrows no resources until assigned. It does not start a native host.

### GetConfigurationWarnings

`public override string[] GetConfigurationWarnings()` returns inherited diagnostics plus each unknown nonempty directional action. It does not register actions.

## Event descriptions

### Pressed

`public event Action? Pressed`. Contact, geometry and directional state are committed before callbacks.

### Released

`public event Action<Vector2>? Released`. Contact and axes are retired before callbacks; argument retains the final pre-release vector.

### Tapped

`public event Action? Tapped`. Normal stationary contact release; cancellation produces no tap.

### Flicked

`public event Action<Vector2>? Flicked`. Normal moved release with nonzero output; argument retains the final vector.

### FlickCanceled

`public event Action? FlickCanceled`. Directional movement returns into the dead zone; a later renewed excursion can cancel again.

## Protected extension points

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` adds authored typed storage. `protected override Func<Node> CreateSceneInstanceFactory()` supports the exact built-in type; subclasses supply an exact noncapturing factory. `protected override void OnNotification(int what)` performs required lifetime/drawing work after inherited handling; required cleanup survives callback failure. `protected override void OnGUIInput(InputEvent input)` handles its input lane. `protected override void Dispose(bool disposing)` retires owned bindings and preserves borrowed resources.

## Verification and limits

[Managed checks](../../tests/Electron2D.Tests/TouchControlTests.cs) exercise independent actions, contact cancellation, gesture modes, reentry/failure, owner/capture guards and typed scene storage in a fresh process. [Native checks](../../tests/Electron2D.Tests/TouchControlNativeTests.cs) exercise queued SDL contacts, actual button/tip pixels and warmed motion/rendering on Linux GPU and compatibility. Physical touchscreens, mobile hardware, foreign rendering, inherited semantic accessibility and native allocator totals remain unverified. See ADR 0038, ADR 0083, ADR 0008 and ADR 0051 for shared contracts.

An active contact uses the existing internal processing lane to retire input at the next frame when inherited InputEnabled, effective pointer policy, viewport input disabling or authoring selection prevents further contact delivery. This lane stops after retirement; ordinary user processing flags remain independent.

Configuration-warning queries and inherited refresh requests permit the owner/current processing group; retained authoring operations keep their existing guards. See [grouped diagnostics](../components/canvas-rendering.md#grouped-canvas-modulation-and-diagnostics).
