# InputEventFromWindow

Last updated: 2026-09-21

- Source/declaration: [`InputEvent.cs`](../../src/Core/Input/InputEvent.cs), `public abstract class InputEventFromWindow : InputEvent`.
- Responsibility: adds a host-defined `long WindowId`; zero means primary/unspecified.
- Complete declared API: `WindowId { get; set; }`; protected overrides `CopyEventStateTo` and `GetPropertyDescriptors`. `WindowId` is a stored typed descriptor.
- Lifecycle/errors: mutable Resource state; disposed access throws; copying preserves the identifier.
- Threading/dependencies: caller-coordinated, backend-neutral; a real window registry begins with the SDL window-host trigger in ADR 0038.
- Verification: duplication and concrete positional-event tests exercise preservation. Inherited API is documented by [`InputEvent`](InputEvent.md).
