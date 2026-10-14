# TouchScreenButton

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public partial class TouchScreenButton : Entity`. **Source:** [runtime](../../src/Scene/2D/TouchScreenButton.cs), [storage](../../src/Scene/2D/TouchScreenButton.Storage.cs). **Inherits:** [Entity](Entity.md). **Component:** [Touch game controls](../components/touch-game-controls.md).

## Description

A scene-space touch button with one signed contact, independent action ownership and normal/pressed texture drawing. Viewport coordinates transform through the global canvas inverse. Shape and bitmap regions form a union; a texture rectangle is used only without either mask. The unit-pixel shape query tests pixel area, and ShapeCentered offsets the shape by half the normal texture. ShapeVisible draws the actual shape only during collision debugging or authoring. TouchscreenOnly hides decoration and input when the display service reports no touchscreen; selected authoring nodes remain drawable. Resources are borrowed and texture residency is retained while attached. Entry enables inherited InputEnabled. Exit releases silently; normal cancellation emits Released. Generated InputEventAction transitions are delivered through the containing viewport after the current traversal, with committed action state.

Attached getters and mutations use the scene owner. Mutation rejects scene capture and disposal; undefined enum values, nonfinite geometry and disposed borrowed resources reject. Unknown or empty actions publish no contribution; configuration warnings identify nonempty unknown names. Lifecycle hide, pause, disable, focus loss, exit and disposal retire only owned contributions. Discrete button action events allocate; prepared joystick motion and ordinary drawing reuse retained storage. Selected authoring-subtree input produces no gameplay actions.

## Example

```csharp
InputMap.AddAction("jump");
var button = new TouchScreenButton { Action = "jump", TextureNormal = icon }; // caller-owned Texture icon
scene.AddChild(button);
```

## API summary

| Complete signature | Contract |
| --- | --- |
| `public TouchScreenButton()` | Public defaults. |
| `public string Action { get; set; }` | Empty initially; rebinding a held contact retires the old action and applies the new one. |
| `public Texture? TextureNormal { get; set; }` | Null initially; borrowed normal visual and fallback hit rectangle. |
| `public Texture? TexturePressed { get; set; }` | Null initially; borrowed active visual, falling back to TextureNormal. |
| `public BitMap? Bitmask { get; set; }` | Null initially; set pixels restrict the bitmap part of the hit union. |
| `public Shape? Shape { get; set; }` | Null initially; borrowed collision geometry contributes to the hit union. |
| `public bool ShapeCentered { get; set; }` | True initially; center against TextureNormal size, or zero when absent. |
| `public bool ShapeVisible { get; set; }` | True initially; enables shape decoration in collision debug or selected authoring mode. |
| `public bool PassbyPress { get; set; }` | False initially; drag entry/exit presses/releases the tracked contact. Without it a press lasts to release regardless of position. |
| `public TouchButtonVisibility VisibilityMode { get; set; }` | Always initially; TouchscreenOnly additionally checks current device availability. |
| `public event Action? Pressed` | Raised after contact/action state is committed. |
| `public event Action? Released` | Raised after contact/action state is retired, including cancellation. Exit and disposal are silent. |
| `public bool IsPressed()` | Active signed contact ownership. |
| `public override string[] GetConfigurationWarnings()` | Unknown action names plus inherited diagnostics. |

## Property descriptions

### Action

`public string Action { get; set; }`. Empty initially; rebinding a held contact retires the old action and applies the new one.

### TextureNormal

`public Texture? TextureNormal { get; set; }`. Null initially; borrowed normal visual and fallback hit rectangle.

### TexturePressed

`public Texture? TexturePressed { get; set; }`. Null initially; borrowed active visual, falling back to TextureNormal.

### Bitmask

`public BitMap? Bitmask { get; set; }`. Null initially; set pixels restrict the bitmap part of the hit union.

### Shape

`public Shape? Shape { get; set; }`. Null initially; borrowed collision geometry contributes to the hit union.

### ShapeCentered

`public bool ShapeCentered { get; set; }`. True initially; center against TextureNormal size, or zero when absent.

### ShapeVisible

`public bool ShapeVisible { get; set; }`. True initially; enables shape decoration in collision debug or selected authoring mode.

### PassbyPress

`public bool PassbyPress { get; set; }`. False initially; drag entry/exit presses/releases the tracked contact. Without it a press lasts to release regardless of position.

### VisibilityMode

`public TouchButtonVisibility VisibilityMode { get; set; }`. Always initially; TouchscreenOnly additionally checks current device availability.

## Method descriptions

### Constructor

`public TouchScreenButton()` initializes the defaults listed above. It borrows no resources until assigned. It does not start a native host.

### IsPressed

`public bool IsPressed()` returns true when a contact is active; signed index -1 is valid. It does not report physical or peer action presses.

### GetConfigurationWarnings

`public override string[] GetConfigurationWarnings()` returns diagnostics for an unknown action; it does not register or mutate actions.

## Event descriptions

### Pressed

`public event Action? Pressed`. Raised after contact/action state is committed.

### Released

`public event Action? Released`. Raised after contact/action state is retired, including cancellation. Exit and disposal are silent.

## Protected extension points

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` adds authored typed storage. `protected override Func<Node> CreateSceneInstanceFactory()` supports the exact built-in type; subclasses supply an exact noncapturing factory. `protected override void OnNotification(int what)` performs required lifetime/drawing work after inherited handling; required cleanup survives callback failure. `protected override void OnInput(InputEvent input)` handles its input lane. `protected override void Dispose(bool disposing)` retires owned bindings and preserves borrowed resources.

## Verification and limits

[Managed checks](../../tests/Electron2D.Tests/TouchControlTests.cs) exercise independent actions, contact cancellation, gesture modes, reentry/failure, owner/capture guards and typed scene storage in a fresh process. [Native checks](../../tests/Electron2D.Tests/TouchControlNativeTests.cs) exercise queued SDL contacts, actual button/tip pixels and warmed motion/rendering on Linux GPU and compatibility. Physical touchscreens, mobile hardware, foreign rendering, inherited semantic accessibility and native allocator totals remain unverified. See ADR 0038, ADR 0083, ADR 0008 and ADR 0051 for shared contracts.

An active contact uses the existing internal processing lane to retire input at the next frame when inherited InputEnabled, effective pointer policy, viewport input disabling or authoring selection prevents further contact delivery. This lane stops after retirement; ordinary user processing flags remain independent.
