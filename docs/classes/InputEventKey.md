# InputEventKey

Last updated: 2026-09-22

**Inherits:** [InputEventWithModifiers](InputEventWithModifiers.md)

**Inherited By:** —

- **Source:** [`src/Core/Input/InputEventKey.cs`](../../src/Core/Input/InputEventKey.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class InputEventKey : InputEventWithModifiers`

> Represents a keyboard key press, release, or operating-system repeat.

## Description

Represents a keyboard key press, release, or operating-system repeat.

- Responsibility: keyboard press/release/repeat with logical, physical, label, Unicode-scalar, location, window, device, and modifier data.
- Complete declared API: `Pressed`, `Echo`, `Keycode`, `PhysicalKeycode`, `KeyLabel`, `Unicode`, `Location`; `GetKeycodeWithModifiers`, `GetPhysicalKeycodeWithModifiers`, `GetKeyLabelWithModifiers`; `AsTextKeycode`, `AsTextPhysicalKeycode`, `AsTextKeyLabel`, `AsTextLocation`; overrides `IsEcho`, `IsMatch`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. All seven declared values are stored typed descriptors.
- Matching: label-only bindings use labels; otherwise logical code wins over physical code. Physical bindings may require location. Non-exact presses allow extra modifiers; releases ignore required modifiers; exact matching requires equality.
- Errors/threading: Unicode must be zero or a scalar, location must be defined, and disposed access fails. Mutable caller-owned state is not synchronized.
- Verification: raw logical/physical/label state, modifiers, exactness, repeat policy, text, duplication, and release matching are covered; the SDL dummy suite checks label separation and all defined left/right modifier locations on press and release.

A caller-created event can supply logical, physical, label, and Unicode data. An action binding should generally set
only one of [`InputEventKey.Keycode`](InputEventKey.md#p-electron2d-inputeventkey-keycode), [`InputEventKey.PhysicalKeycode`](InputEventKey.md#p-electron2d-inputeventkey-physicalkeycode), or [`InputEventKey.KeyLabel`](InputEventKey.md#p-electron2d-inputeventkey-keylabel).

The current SDL display adapter supplies `Keycode` from the native key event, `PhysicalKeycode` from its scancode, and `KeyLabel` independently from the unmodified scancode under the active layout. A printable non-Latin label can therefore differ from the logical key. Left/right modifier scancodes set `Location` to the matching side; other scancodes use `Unspecified`. That adapter leaves `Unicode` at zero; committed text uses a separate text-input event. SDL key events contain no produced text scalar, and a text-input event may contain multiple scalars or an IME commit without identifying a corresponding key press. Native key-event Unicode needs a per-key Unicode source and verified IME/composition semantics in the first native keyboard/text adapter slice. Code constructing this type directly may assign a valid Unicode scalar.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var keyEvent = new InputEventKey { Keycode = Key.Space, Pressed = true };
Input.Instance.ParseInputEvent(keyEvent);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public InputEventKey()`](#m-electron2d-inputeventkey-ctor) | Initializes a new InputEventKey instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public bool Pressed { get; set; }`](#p-electron2d-inputeventkey-pressed) | Gets or sets whether the key is pressed. |
| [`public bool Echo { get; set; }`](#p-electron2d-inputeventkey-echo) | Gets or sets whether this is a repeated press for a key that was already held. |
| [`public Key Keycode { get; set; }`](#p-electron2d-inputeventkey-keycode) | Gets or sets the layout-aware logical key code. |
| [`public Key PhysicalKeycode { get; set; }`](#p-electron2d-inputeventkey-physicalkeycode) | Gets or sets the physical key position expressed against a standard US keyboard layout. |
| [`public Key KeyLabel { get; set; }`](#p-electron2d-inputeventkey-keylabel) | Gets or sets the localized label printed on the key. |
| [`public int Unicode { get; set; }`](#p-electron2d-inputeventkey-unicode) | Gets or sets the Unicode scalar produced by the press. |
| [`public KeyLocation Location { get; set; }`](#p-electron2d-inputeventkey-location) | Gets or sets the side of a key that has left and right variants. |

## Methods

| Member | Description |
| --- | --- |
| [`public override bool IsEcho()`](#m-electron2d-inputeventkey-isecho) | Gets whether this is an operating-system key-repeat event. |
| [`public Key GetKeycodeWithModifiers()`](#m-electron2d-inputeventkey-getkeycodewithmodifiers) | Gets the logical key code combined with active modifier bits. |
| [`public Key GetPhysicalKeycodeWithModifiers()`](#m-electron2d-inputeventkey-getphysicalkeycodewithmodifiers) | Gets the physical key code combined with active modifier bits. |
| [`public Key GetKeyLabelWithModifiers()`](#m-electron2d-inputeventkey-getkeylabelwithmodifiers) | Gets the localized key label combined with active modifier bits. |
| [`public string AsTextKeycode()`](#m-electron2d-inputeventkey-astextkeycode) | Returns the logical key and modifier description. |
| [`public string AsTextPhysicalKeycode()`](#m-electron2d-inputeventkey-astextphysicalkeycode) | Returns the physical key and modifier description. |
| [`public string AsTextKeyLabel()`](#m-electron2d-inputeventkey-astextkeylabel) | Returns the localized key label and modifier description. |
| [`public string AsTextLocation()`](#m-electron2d-inputeventkey-astextlocation) | Returns the key-location description. |
| [`public override bool IsMatch(InputEvent event, bool exactMatch = true)`](#m-electron2d-inputeventkey-ismatch-electron2d-inputevent-system-boolean) | Tests whether this event has the same binding configuration as another event. |
| [`public override string AsText()`](#m-electron2d-inputeventkey-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override InputEvent CreateEventInstance()`](#m-electron2d-inputeventkey-createeventinstance) | Creates a default instance of the exact concrete event type. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventkey-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventkey-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventkey-ctor"></a>
### `public InputEventKey()`

Initializes a new InputEventKey instance.

## Property Descriptions

<a id="p-electron2d-inputeventkey-pressed"></a>
### `public bool Pressed { get; set; }`

Gets or sets whether the key is pressed.

**Value:** `false` for a release; `true` for a press.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventkey-echo"></a>
### `public bool Echo { get; set; }`

Gets or sets whether this is a repeated press for a key that was already held.

**Value:** `false` by default.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventkey-keycode"></a>
### `public Key Keycode { get; set; }`

Gets or sets the layout-aware logical key code.

**Value:** A special-key identifier or Unicode-compatible printable key; [`Key.None`](Key.md#f-electron2d-key-none) when absent.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventkey-physicalkeycode"></a>
### `public Key PhysicalKeycode { get; set; }`

Gets or sets the physical key position expressed against a standard US keyboard layout.

**Value:** A location-oriented key code; [`Key.None`](Key.md#f-electron2d-key-none) when absent.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventkey-keylabel"></a>
### `public Key KeyLabel { get; set; }`

Gets or sets the localized label printed on the key.

**Value:** A key identifier or Unicode-compatible printable character; [`Key.None`](Key.md#f-electron2d-key-none) when absent.

**Native adapter:** The SDL display adapter derives this value from the unmodified physical scancode under the current keyboard layout rather than copying `Keycode`; non-Latin printable scalars are converted to invariant uppercase key identities.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventkey-unicode"></a>
### `public int Unicode { get; set; }`

Gets or sets the Unicode scalar produced by the press.

**Value:** Zero when no text scalar is associated with the event.

**Native adapter:** The current SDL key-event path leaves this value at zero. Committed text is delivered separately and does not mutate the key event.

**Integration trigger:** Integrate a native per-key Unicode source with verified IME/composition semantics in the first native keyboard/text adapter slice. SDL keyboard and text-input events alone do not provide a reliable key-to-text association.

**Exceptions**

- `ArgumentOutOfRangeException`: The value is not zero or a valid Unicode scalar.
- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventkey-location"></a>
### `public KeyLocation Location { get; set; }`

Gets or sets the side of a key that has left and right variants.

**Value:** [`KeyLocation.Unspecified`](KeyLocation.md#f-electron2d-keylocation-unspecified) by default.

**Native adapter:** Left control, shift, alt, and GUI scancodes produce `KeyLocation.Left`; their right-side counterparts produce `KeyLocation.Right`; every other scancode produces `KeyLocation.Unspecified`. The rule is the same for press and release.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is not defined.
- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventkey-isecho"></a>
### `public override bool IsEcho()`

Gets whether this is an operating-system key-repeat event.

**Returns:** `false` except for a repeating [`InputEventKey`](InputEventKey.md).

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventkey-getkeycodewithmodifiers"></a>
### `public Key GetKeycodeWithModifiers()`

Gets the logical key code combined with active modifier bits.

**Returns:** The numeric union of [`InputEventKey.Keycode`](InputEventKey.md#p-electron2d-inputeventkey-keycode) and [`InputEventWithModifiers.GetModifiersMask`](InputEventWithModifiers.md#m-electron2d-inputeventwithmodifiers-getmodifiersmask).

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventkey-getphysicalkeycodewithmodifiers"></a>
### `public Key GetPhysicalKeycodeWithModifiers()`

Gets the physical key code combined with active modifier bits.

**Returns:** The numeric union of [`InputEventKey.PhysicalKeycode`](InputEventKey.md#p-electron2d-inputeventkey-physicalkeycode) and [`InputEventWithModifiers.GetModifiersMask`](InputEventWithModifiers.md#m-electron2d-inputeventwithmodifiers-getmodifiersmask).

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventkey-getkeylabelwithmodifiers"></a>
### `public Key GetKeyLabelWithModifiers()`

Gets the localized key label combined with active modifier bits.

**Returns:** The numeric union of [`InputEventKey.KeyLabel`](InputEventKey.md#p-electron2d-inputeventkey-keylabel) and [`InputEventWithModifiers.GetModifiersMask`](InputEventWithModifiers.md#m-electron2d-inputeventwithmodifiers-getmodifiersmask).

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventkey-astextkeycode"></a>
### `public string AsTextKeycode()`

Returns the logical key and modifier description.

**Returns:** A portable diagnostic representation.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventkey-astextphysicalkeycode"></a>
### `public string AsTextPhysicalKeycode()`

Returns the physical key and modifier description.

**Returns:** A portable diagnostic representation.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventkey-astextkeylabel"></a>
### `public string AsTextKeyLabel()`

Returns the localized key label and modifier description.

**Returns:** A portable diagnostic representation.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventkey-astextlocation"></a>
### `public string AsTextLocation()`

Returns the key-location description.

**Returns:** `Left`, `Right`, or an empty string for an unspecified location.

**Exceptions**

- `InvalidOperationException`: The event contains an invalid key-location value.
- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventkey-ismatch-electron2d-inputevent-system-boolean"></a>
### `public override bool IsMatch(InputEvent event, bool exactMatch = true)`

Tests whether this event has the same binding configuration as another event.

**Parameters**

- `event`: The event to compare.
- `exactMatch`: Whether modifiers and analog direction must match exactly.

**Returns:** `true` when the binding configurations match.

**Exceptions**

- `ArgumentNullException`: `event` is `null`.
- `ObjectDisposedException`: Either event is disposing or disposed.

<a id="m-electron2d-inputeventkey-astext"></a>
### `public override string AsText()`

Returns a concise, human-readable representation of the event.

**Returns:** A non-null description suitable for bindings and diagnostics.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventkey-createeventinstance"></a>
### `protected override InputEvent CreateEventInstance()`

Creates a default instance of the exact concrete event type.

**Returns:** A new event instance.

<a id="m-electron2d-inputeventkey-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventkey-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventWithModifiers](InputEventWithModifiers.md). Their lifecycle and error contracts remain applicable unless this page states an override.
