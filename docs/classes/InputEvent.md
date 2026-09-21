# InputEvent

Last updated: 2026-09-21

## Declaration

- Source: [`InputEvent.cs`](../../src/Core/Input/InputEvent.cs)
- Declaration: `public abstract class InputEvent : Resource`
- Domain/component: [Input](../domains/input.md) / [Input runtime](../components/input-runtime.md)

## Responsibility and ownership

`InputEvent` is the mutable, duplicable Resource base for caller-owned input payloads and action bindings. It has no native handle.

## Complete public/protected API

- Constants: `DeviceIdEmulation = -1`, `DeviceIdKeyboard = 16`, `DeviceIdMouse = 32`.
- Property: `int Device`.
- Action API: `IsAction`, `IsActionPressed`, `IsActionReleased`, `GetActionStrength`, `GetActionRawStrength`, `IsActionType`.
- State API: `IsCanceled`, `IsPressed`, `IsReleased`, `IsEcho`.
- Value API: `IsMatch`, `Accumulate`, `XformedBy`, abstract `AsText`, `ToString`.
- Protected extension API: `PressedState`, `CanceledState`, abstract `CreateEventInstance`, virtual `CopyEventStateTo`, `ValidateFinite` for vectors/transforms, `EmitInputChanged`, `GetPropertyDescriptors`, sealed Resource duplication hooks, and binding-aware `Dispose(bool)`.

Action methods validate a registered ordinal action name and delegate matching to InputMap. Canceled events are neither pressed nor released. Base matching/accumulation return false; base transforms return the same object for non-positional events. Concrete positional transforms return independent duplicates. Disposal first removes the event from every InputMap action that registered the same reference, then follows Resource cleanup; every member rejects disposed access.

`Device` and every concrete event value are exposed through inherited, stored typed property descriptors. Descriptor revert values match each concrete constructor, including keyboard, mouse, and touch/gesture device defaults. Derived state copying preserves exact runtime type and caller ownership. `EmitInputChanged` first invalidates any registered action contribution, then emits optional property-list and public resource-change notifications; a throwing public handler therefore cannot leave Input caches stale. Matching and pure value operations are synchronous; callers coordinate concurrent mutation. Coverage includes every concrete subclass listed in the component document. MIDI and Shortcut subclasses remain absent under the exact triggers in ADR 0038.
