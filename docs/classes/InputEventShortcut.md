# InputEventShortcut

Last updated: 2026-09-27

Source: [`src/Core/Input/InputEventShortcut.cs`](../../src/Core/Input/InputEventShortcut.cs). Declaration: `public sealed class InputEventShortcut : InputEvent`. Inherits [InputEvent](InputEvent.md).

## Description

Activates an exact borrowed [Shortcut](Shortcut.md) through the ordinary scene shortcut stage. Construction sets the inherited pressed state to true; IsReleased and IsActionType are false. No public press setter or fake hardware binding is introduced. The event itself has no special IsMatch override: Shortcut.MatchesEvent performs identity matching.

## Example

```csharp
using var shortcut = new Shortcut();
using var activation = new InputEventShortcut { Shortcut = shortcut };
viewport.PushInput(activation, inLocalCoordinates: true); // attached Viewport supplied by the host
```

## API

| Signature | Contract |
| --- | --- |
| `public InputEventShortcut()` | Creates a pressed event with null Shortcut. |
| `public Shortcut? Shortcut { get; set; }` | Borrows the shortcut; every assignment emits input/resource change. |
| `public override string AsText()` | Translated Input Event with Shortcut description, or None. |
| `public override string ToString()` | Diagnostic InputEventShortcut: shortcut= description, or None. |
| `protected override InputEvent CreateEventInstance()` | Creates an exact event instance. |
| `protected override void CopyEventStateTo(InputEvent target)` | Copies inherited state and the borrowed shortcut. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds the typed Shortcut property descriptor. |

## Property descriptions

### Shortcut

Null initially. Null clears the reference; a disposed assigned shortcut is rejected before mutation. Equal identities notify. A throwing Changed observer sees committed state. Disposing this event does not dispose the borrowed shortcut. Shallow copies borrow it; deep Resource duplication copies the shortcut and its selected event graph while preserving aliases.

## Method descriptions

### AsText

Uses the event's translation domain for the runtime template and the live shortcut's GetAsText result. Missing or disposed shortcut references return None. A disposed input event raises ObjectDisposedException.

### ToString

Returns the diagnostic type prefix and current shortcut description, without translating the prefix. Missing or disposed references return None; a disposed event raises ObjectDisposedException.

## Verification

[ShortcutTests](../../tests/Electron2D.Tests/ShortcutTests.cs) covers defaults, direct activation, descriptions and deep copies. [ADR 0038](../decisions/input.md#adr-0038) specifies shortcut-stage ordering and handling.
