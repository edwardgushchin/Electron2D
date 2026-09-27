# BaseButton

Last updated: 2026-09-27

**Declaration:** `public abstract class BaseButton : Control` · **Source:** [BaseButton.cs](../../src/Scene/GUI/BaseButton.cs)

**Inherits:** [Control](Control.md). **Inherited By:** [Button](Button.md), [TextureButton](TextureButton.md).

Provides mouse, touch, action and shortcut interaction for button controls. The base records no decoration; concrete controls use GetDrawMode when drawing. FocusMode defaults to All. A button borrows its [Shortcut](Shortcut.md) and [ButtonGroup](ButtonGroup.md); disposing the button removes group membership without disposing either resource.

Attached getters obey the scene owner thread. Mutations additionally reject scene capture. Committed state survives callback errors: remaining current signal and cleanup phases execute, then errors are reported as AggregateException. A nested newer activation or state mutation cancels subsequent phases of the old operation. Group iterations use captured membership generations and stop when the initiating operation is replaced.

## Example

```csharp
using var choices = new ButtonGroup();
var first = new Button { Text = "First", ToggleMode = true, ButtonGroup = choices };
var second = new Button { Text = "Second", ToggleMode = true, ButtonGroup = choices };
choices.Pressed += button => Console.WriteLine(button.Name);
first.ButtonPressed = true;
// Add these nodes to the desired scene and assign scene ownership when packing.
```

## API summary

| Signature | Contract |
| --- | --- |
| `protected BaseButton()` | Release activation, left button, full focus, no toggle or pressed state. |
| `public bool Disabled { get; set; }` | Suppresses GUI and shortcut activation; initially false. |
| `public bool ToggleMode { get; set; }` | Persistent toggle state; initially false. |
| `public bool ButtonPressed { get; set; }` | Reads held/toggled state; setter only acts in toggle mode. |
| `public ButtonActionMode ActionMode { get; set; }` | [ButtonRelease](ButtonActionMode.md) initially. |
| `public MouseButtonMask ButtonMask { get; set; }` | [Left](MouseButtonMask.md) initially. |
| `public bool KeepPressedOutside { get; set; }` | Visual feedback outside the hit shape; initially false. |
| `public ButtonGroup? ButtonGroup { get; set; }` | Borrowed radio group; initially null. |
| `public Shortcut? Shortcut { get; set; }` | Borrowed shortcut; initially null. |
| `public bool ShortcutFeedback { get; set; }` | Enables timed shortcut highlight; initially true. |
| `public bool ShortcutInTooltip { get; set; }` | Annotates the default tooltip; initially true. |
| `public void SetPressedNoSignal(bool pressed)` | Silent toggle assignment without group enforcement. |
| `public bool IsHovered()` | Whether the pointer entered and has not exited. |
| `public DrawMode GetDrawMode()` | Current visual state. |
| `public override string[] GetConfigurationWarnings()` | Adds a warning for a non-toggle group member. |
| `public event Action? ButtonDown` | First eligible press begins holding. |
| `public event Action? ButtonUp` | Holding ends or is cancelled through disable/focus. |
| `public event Action? Pressed` | Configured activation, after OnPressed. |
| `public event Action<bool>? Toggled` | Signalled toggle, after OnToggled. |
| `protected virtual void OnPressed()` | Activation extension point. |
| `protected virtual void OnToggled(bool toggledOn)` | Toggle extension point. |
| `protected override void OnGUIInput(InputEvent inputEvent)` | Mouse, touch and exact ui_accept processing. |
| `protected override void OnShortcutInput(InputEvent inputEvent)` | Shortcut matching, activation and feedback. |
| `protected override void OnNotification(int what)` | Hover, focus, visibility, tree and feedback lifecycle. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Typed stored button state and the full-focus default. |
| `protected override void Dispose(bool disposing)` | Removes membership and drops borrowed references/events. |

## Property descriptions

### Disabled

Rejects user input when true; programmatic ButtonPressed and SetPressedNoSignal remain usable. Disabling cancels the attempt, releases ButtonUp if needed, queues redraw and updates minimum size. Toggle selection survives disabling; momentary persistent state resets. Equal assignments are silent. Callback failures cannot leave the old gesture active.

### ToggleMode

False gives momentary buttons. True flips persistent state on the configured activation edge. Turning it off first performs an ordinary ButtonPressed=false assignment while toggle mode is still active, then changes mode and refreshes configuration warnings. Property changes do not activate Pressed.

### ButtonPressed

Returns persistent state in toggle mode and the live press attempt otherwise. Setter does nothing for a momentary button or unchanged value. A transition to true unpresses group peers, emits ButtonGroup.Pressed, then OnToggled and Toggled. A transition to false emits only the local toggle phases and is allowed even when ButtonGroup.AllowUnpress=false. The local Pressed event is reserved for activation.

### ActionMode

ButtonPress activates on press; ButtonRelease requires a preceding eligible press and release while still inside. Unknown numeric enum values remain stored, delivering holding signals but activating on neither edge.

### ButtonMask

Chooses accepted mouse buttons using their bit positions. Unknown bits remain stored. It does not change touch, ui_accept or shortcut activation.

### KeepPressedOutside

Only changes appearance while an attempt is active. A release outside never activates regardless of this value. Pointer coordinates supplied to the GUI hook are local to the control; HasPoint supplies the shape.

### ButtonGroup

Membership changes preserve current pressed states and queue redraw, allowing concrete checkbox controls to switch radio decoration. Group peers are borrowed node references, and dead/disposed entries are removed. Assigning a disposed group is rejected before changing membership. Scene packing duplicates scene-local groups and preserves aliases within each instance.

### Shortcut

Changing identity enables or disables inherited ShortcutInputEnabled. A shortcut matches only a pressed, non-echo event while the button is enabled and visible in its tree and inherited ShortcutContext allows the current focus. Shortcut activation does not synthesize ButtonDown/ButtonUp. An accepted shortcut marks scene input handled even when an activation observer fails.

### ShortcutFeedback

On accepted shortcut activation, draws HoverPressed for a short interval. The duration is sampled from ProjectSettings.ButtonShortcutFeedbackHighlightTime at the first highlighted activation, defaults to 0.2 seconds, ignores time scale, respects processing pause and expires when remaining time becomes negative. Repeated activation restarts the interval. Disabling this property does not cancel a running highlight. The internal countdown shares its processing lane with concrete button resource polling without adding visible child nodes.

### ShortcutInTooltip

The default tooltip uses the shortcut resource name, appends its readable event text in parentheses when present, and appends the supplied tooltip text on a new line unless it equals the shortcut name ignoring case. Empty name and event data adds no custom control. A user's OnMakeCustomTooltip sees the original text first and a nonnull custom result takes precedence. Built-in annotation produces an owned Label with TooltipLabel theme variation and disabled automatic retranslation. GetTooltip itself remains the inherited unannotated query.

## Method and hook descriptions

### BaseButton

The protected constructor initializes the defaults above and supports user-defined concrete controls. Custom scene subclasses supply the normal typed scene-instance factory inherited from Node.

### SetPressedNoSignal

`public void SetPressedNoSignal(bool pressed)` changes toggle state and requests redraw, but calls neither toggle hook nor event and does not unpress group peers. Multiple selected members after silent setup are valid. It does nothing outside toggle mode.

### IsHovered

`public bool IsHovered()` reflects enter/exit notifications independently of disabled state. Hide or tree exit clears it.

### GetDrawMode

`public DrawMode GetDrawMode()` first chooses Disabled, then active shortcut HoverPressed. Idle hover combines with persistent selection. During an attempt, inside/KeepPressedOutside previews pressing, inverted when the button was already toggled on. This preview does not modify stored selection.

### GetConfigurationWarnings

`public override string[] GetConfigurationWarnings()` returns inherited warnings and adds a warning when ButtonGroup is assigned but ToggleMode is false. It does not change the configuration.

### OnPressed

`protected virtual void OnPressed()` executes before Pressed on actual activation. An exception is aggregated after remaining valid phases; disposal or a replacement interaction suppresses stale phases.

### OnToggled

`protected virtual void OnToggled(bool toggledOn)` executes after committed toggle/group state and before Toggled. Use the supplied state for the delivered transition. Silent setup never invokes this hook.

### OnGUIInput

`protected override void OnGUIInput(InputEvent inputEvent)` accepts masked mouse buttons and exact, non-echo ui_accept events. Mouse press requires hover. Native touch uses one contact index, with local drag containment; emulated touch is ignored to avoid duplicate mouse activation. A press-mode toggle clears its touch contact immediately so a consumed release does not block later taps. Null/disposed events and nonfinite motion are rejected.

### OnShortcutInput

`protected override void OnShortcutInput(InputEvent inputEvent)` processes eligible shortcut matches, enforces radio selection, emits toggle/pressed hooks and events, accepts input and restarts optional feedback. Events and shortcut resources remain borrowed.

### OnNotification

`protected override void OnNotification(int what)` updates hover/focus visuals, cancels input on focus loss and visibility/tree exit, and advances feedback using unscaled internal process time. Base notification failures do not skip required state maintenance. Concrete overrides call base so these transitions execute.

### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` stores the ten public button properties and overrides the inherited FocusMode revert default to All. Transient hover/contact/callback/feedback state is not stored.

### Dispose

`protected override void Dispose(bool disposing)` removes group membership, invalidates current interactions and releases references/event subscribers before completing inherited node cleanup. Borrowed groups and shortcuts remain alive.

## Event descriptions

### ButtonDown

`public event Action? ButtonDown` is emitted once when eligible input first begins holding. A duplicate pressed event does not repeat this signal while held.

### ButtonUp

`public event Action? ButtonUp` follows release activation and also executes when disabling or losing focus cancels an active hold. Shortcut activation does not use this signal.

### Pressed

`public event Action? Pressed` follows OnPressed at the selected press/release edge or shortcut activation. A release outside suppresses activation. Programmatic ButtonPressed assignment does not emit it.

### Toggled

`public event Action<bool>? Toggled` follows OnToggled after committed selection and group updates. Activating an already selected disallow-unpress radio still emits true. Programmatic changes emit on actual state transitions; SetPressedNoSignal emits nothing.

## Enumeration descriptions

### DrawMode

See [BaseButton.DrawMode](BaseButton.DrawMode.md). The action edge uses [ButtonActionMode](ButtonActionMode.md) to avoid a C# nested-type/property collision.

## Errors, lifecycle and verification

Disposed button access throws ObjectDisposedException. Attached off-owner access and capture-owned mutation throw InvalidOperationException. Disposed borrowed groups/shortcuts are rejected when used. Configuration warning callbacks retain the inherited exception contract; input/group hook failures are aggregated after valid phases. Hide/tree removal clears transient attempts and hover while retaining toggle selection. Becoming effectively paused or process-disabled also cancels the current press/contact without activation; Always-mode buttons remain eligible during a tree pause. This prevents a release suppressed by processing policy from leaving a stale contact after resume. Group copies never copy node membership.

[BaseButtonTests](../../tests/Electron2D.Tests/BaseButtonTests.cs) exercises defaults, input edges, visual states, touch identity, ui_accept/echo, groups and silent setup, callback errors/reentry, shortcuts/tooltips, scene-local packing, lifecycle/capture and prepared input allocations. [GUIButtonRoutingTests](../../tests/Electron2D.Tests/GUIButtonRoutingTests.cs) covers actual viewport touch capture, multi-contact cancellation, drag/gesture transforms, inherited bubbling and processing eligibility. Sixty-four reused local mouse/button/group cycles combined with real viewport shortcut dispatch allocate zero managed bytes after 64 warm cycles. Independently, 64 complete routed mouse cycles allocate 180,736 bytes on Linux x64: this equals the measured baseline of their three required positional event copies per cycle. Hover chain management, including a nested input dispatch from an exit callback, has its own 64 warmed zero-allocation check. Positional copies retain the ownership contract in ADR 0038; events are neither pooled nor revived. Native/frame and platform results are recorded for the completed slice in the GUI component and owning ADR. Native accessibility semantics and future drag/drop/scroll-container cancellation integration remain tracked at their owning layers.
