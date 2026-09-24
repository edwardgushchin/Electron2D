# InputEventWithModifiers

Last updated: 2026-09-24

**Inherits:** [InputEventFromWindow](InputEventFromWindow.md)

**Inherited By:** [InputEventGesture](InputEventGesture.md), [InputEventKey](InputEventKey.md), [InputEventMouse](InputEventMouse.md)

- **Source:** [`src/Core/Input/InputEvent.cs`](../../src/Core/Input/InputEvent.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class InputEventWithModifiers : InputEventFromWindow`

> Provides modifier-key state for keyboard, mouse, and gesture events.

## Description

Provides modifier-key state for keyboard, mouse, and gesture events.

- Responsibility: stores Alt, Shift, Control, Meta, and portable command-or-control state; initializes inherited `Device` to the primary keyboard, with a typed revert default of 16. Mouse and gesture descendants use their own inherited device defaults.
- Complete declared API: protected constructor; `AltPressed`, `ShiftPressed`, `ControlPressed`, `MetaPressed`, `CommandOrControlAutoremap`; `GetModifiersMask()`, `IsCommandOrControlPressed()`, `SetModifiersFromEvent(...)`; override `AsText()`; protected overrides `CopyEventStateTo` and `GetPropertyDescriptors`. All five modifier values are stored typed descriptors.
- Invariants/errors: enabling autoremap chooses Meta on macOS and Control elsewhere; concrete Control/Meta assignment is rejected while enabled; disabling clears both. Disposed resources fail.
- Threading: mutable caller-owned Resource, no internal synchronization.
- Verification/limits: `VerifyInput` covers defaults, masks, autoremap transitions, typed copy and property-list notifications. `VerifyInputText` checks displayed modifier names on the current platform; macOS uses Option/Command and Windows uses Windows for Meta. SDL dummy and Wayland injection check all eight side-specific modifier keys: each key excludes its own modifier bit and retains other held bits. Platform choice uses .NET OS detection; native macOS delivery remains unverified.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using InputEventWithModifiers inputEvent = new InputEventKey { ControlPressed = true };
KeyModifierMask modifiers = inputEvent.GetModifiersMask();
```

## Constructors

| Member | Description |
| --- | --- |
| [`protected InputEventWithModifiers()`](#m-electron2d-inputeventwithmodifiers-ctor) | Initializes a modifier-bearing event with the primary-keyboard device identifier. |

## Properties

| Member | Description |
| --- | --- |
| [`public bool AltPressed { get; set; }`](#p-electron2d-inputeventwithmodifiers-altpressed) | Gets or sets whether Alt or Option is pressed. |
| [`public bool ShiftPressed { get; set; }`](#p-electron2d-inputeventwithmodifiers-shiftpressed) | Gets or sets whether Shift is pressed. |
| [`public bool ControlPressed { get; set; }`](#p-electron2d-inputeventwithmodifiers-controlpressed) | Gets or sets whether Control is pressed. |
| [`public bool MetaPressed { get; set; }`](#p-electron2d-inputeventwithmodifiers-metapressed) | Gets or sets whether Meta, Command, Windows, or Super is pressed. |
| [`public bool CommandOrControlAutoremap { get; set; }`](#p-electron2d-inputeventwithmodifiers-commandorcontrolautoremap) | Gets or sets whether the portable command-or-control modifier is enabled. |

## Methods

| Member | Description |
| --- | --- |
| [`public KeyModifierMask GetModifiersMask()`](#m-electron2d-inputeventwithmodifiers-getmodifiersmask) | Gets the active modifier bits. |
| [`public bool IsCommandOrControlPressed()`](#m-electron2d-inputeventwithmodifiers-iscommandorcontrolpressed) | Gets the platform-specific state of the command-or-control modifier. |
| [`public void SetModifiersFromEvent(InputEventWithModifiers source)`](#m-electron2d-inputeventwithmodifiers-setmodifiersfromevent-electron2d-inputeventwithmodifiers) | Copies modifier state from another event. |
| [`public override string AsText()`](#m-electron2d-inputeventwithmodifiers-astext) | Returns a concise, human-readable representation of the event. |
| [`protected override void CopyEventStateTo(InputEvent target)`](#m-electron2d-inputeventwithmodifiers-copyeventstateto-electron2d-inputevent) | Copies this event's concrete state to another exact-type event. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-inputeventwithmodifiers-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |

## Constructor Descriptions

<a id="m-electron2d-inputeventwithmodifiers-ctor"></a>
### `protected InputEventWithModifiers()`

Initializes a modifier-bearing event with the primary-keyboard device identifier.

## Property Descriptions

<a id="p-electron2d-inputeventwithmodifiers-altpressed"></a>
### `public bool AltPressed { get; set; }`

Gets or sets whether Alt or Option is pressed.

**Value:** `false` by default.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventwithmodifiers-shiftpressed"></a>
### `public bool ShiftPressed { get; set; }`

Gets or sets whether Shift is pressed.

**Value:** `false` by default.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventwithmodifiers-controlpressed"></a>
### `public bool ControlPressed { get; set; }`

Gets or sets whether Control is pressed.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: [`InputEventWithModifiers.CommandOrControlAutoremap`](InputEventWithModifiers.md#p-electron2d-inputeventwithmodifiers-commandorcontrolautoremap) is enabled.
- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventwithmodifiers-metapressed"></a>
### `public bool MetaPressed { get; set; }`

Gets or sets whether Meta, Command, Windows, or Super is pressed.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: [`InputEventWithModifiers.CommandOrControlAutoremap`](InputEventWithModifiers.md#p-electron2d-inputeventwithmodifiers-commandorcontrolautoremap) is enabled.
- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

<a id="p-electron2d-inputeventwithmodifiers-commandorcontrolautoremap"></a>
### `public bool CommandOrControlAutoremap { get; set; }`

Gets or sets whether the portable command-or-control modifier is enabled.

**Value:** When enabled, Meta is selected on macOS and Control on every other target. Disabling clears both concrete bits.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.
- `Exception`: A property-list or [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after the value is assigned.

## Method Descriptions

<a id="m-electron2d-inputeventwithmodifiers-getmodifiersmask"></a>
### `public KeyModifierMask GetModifiersMask()`

Gets the active modifier bits.

**Returns:** The combination of Control, Shift, Alt, and Meta currently stored by the event.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventwithmodifiers-iscommandorcontrolpressed"></a>
### `public bool IsCommandOrControlPressed()`

Gets the platform-specific state of the command-or-control modifier.

**Returns:** Meta on macOS; Control on every other target.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventwithmodifiers-setmodifiersfromevent-electron2d-inputeventwithmodifiers"></a>
### `public void SetModifiersFromEvent(InputEventWithModifiers source)`

Copies modifier state from another event.

**Parameters**

- `source`: The source modifier event.

**Exceptions**

- `ArgumentNullException`: `source` is `null`.
- `ObjectDisposedException`: Either event is disposing or disposed.
- `Exception`: A property-list or [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) handler throws after all modifier values are assigned.

<a id="m-electron2d-inputeventwithmodifiers-astext"></a>
### `public override string AsText()`

Returns active modifier names in keyboard display order: Control, Alt/Option, Shift, then Meta/Command/Windows.

**Returns:** Platform-specific names joined by `+`, or an empty string when none are active.

**Exceptions**

- `ObjectDisposedException`: The event is disposing or disposed.

<a id="m-electron2d-inputeventwithmodifiers-copyeventstateto-electron2d-inputevent"></a>
### `protected override void CopyEventStateTo(InputEvent target)`

Copies this event's concrete state to another exact-type event.

**Parameters**

- `target`: The destination event.

<a id="m-electron2d-inputeventwithmodifiers-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

## Inherited API

Public and protected members inherited from [InputEventFromWindow](InputEventFromWindow.md). Their lifecycle and error contracts remain applicable unless this page states an override.
