# Shortcut

Last updated: 2026-09-27

Source: [`src/Core/Input/Shortcut.cs`](../../src/Core/Input/Shortcut.cs). Declaration: `public class Shortcut : Resource`. Inherits [Resource](Resource.md).

## Description

Stores ordered borrowed input alternatives for GUI actions. Arrays are copied on both assignment and retrieval. Null slots are allowed; an InputEventShortcut cannot be a binding. Assignment validates the complete list before committing, and every successful assignment emits Changed, including equal lists. Editing a borrowed event affects subsequent matching directly without forwarding its Changed signal.

List replacement and snapshot capture are lock-protected. Event hooks run outside that lock; callers must not concurrently mutate or dispose an event during comparison. Disposed alternatives are ignored. Disposing the shortcut releases references without disposing borrowed events.

## Example

```csharp
using var key = new InputEventKey { Keycode = Key.S, ControlPressed = true };
using var shortcut = new Shortcut { Events = [key] };
using var button = new Button { Text = "Save", Shortcut = shortcut };
```

## API

| Signature | Contract |
| --- | --- |
| `public Shortcut()` | Creates an empty resource. |
| `public InputEvent?[] Events { get; set; }` | Copied ordered alternatives. |
| `public bool HasValidEvent()` | Any live event reference, including an unconfigured key. |
| `public bool MatchesEvent(InputEvent @event)` | Direct resource identity or exact event matching. |
| `public string GetAsText()` | First live alternative description, or literal None. |
| `protected override Resource CreateDuplicateInstance()` | Creates the exact base Shortcut resource. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies arrays; deep copies use the shared graph scope and preserve aliases. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds the typed Events descriptor. |
| `protected override void Dispose(bool disposing)` | Releases references before inherited cleanup. |

## Property descriptions

### Events

Defaults to an empty array. A null array raises ArgumentNullException; shortcut-event elements raise ArgumentException; disposed assigned events raise ObjectDisposedException. Invalid assignments leave the previous list intact. Observer failure occurs after publication. Returned arrays belong to the caller, while their event references remain borrowed. Resource duplication copies this state; scene-local resource duplication follows Resource policy.

## Method descriptions

### HasValidEvent

Ignores null and disposed entries. Does not validate key codes or action registration. A disposed shortcut raises ObjectDisposedException.

### MatchesEvent

Rejects null or disposed input. An InputEventShortcut that refers to this exact resource matches even when Events is empty. Otherwise queries alternatives in order through IsMatch with its exact-match default and stops on the first match. Comparison-hook failures propagate. Warmed matching does not allocate.

### GetAsText

Returns the first live event's AsText result, including an empty result; later alternatives are not concatenated. With no live event, returns None. Description-hook failures propagate.

## Verification and limits

[ShortcutTests](../../tests/Electron2D.Tests/ShortcutTests.cs) covers exact alternatives, copied arrays, identity events, notification failure, shallow/deep alias-preserving copies, weak scene contexts and warm matching. [ADR 0038](../decisions/input.md#adr-0038) defines routing; inherited resource behavior follows [ADR 0013](../decisions/resources.md#adr-0013). Native key delivery and rendered button behavior are separate checks.
