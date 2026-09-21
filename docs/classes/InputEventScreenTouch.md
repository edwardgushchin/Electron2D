# InputEventScreenTouch

Last updated: 2026-09-21

- Source/declaration: [`InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs), `public sealed class InputEventScreenTouch : InputEventFromWindow`.
- Responsibility: begin/end/cancel state for one touch contact.
- Complete declared API: non-negative `Index`, finite `Position`, `Pressed`, `Canceled`, `DoubleTap`; overrides `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. All five declared values are stored typed descriptors.
- Transform/lifecycle: returns an independent duplicate with transformed position. A canceled contact is neither pressed nor released. Caller owns/disposes event resources.
- Errors/threading/verification: negative index, non-finite position, or disposed access fails; copy, state, text, and transform are covered. Native touch generation and emulation begin with the SDL event-adapter trigger.
