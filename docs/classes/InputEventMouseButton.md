# InputEventMouseButton

Last updated: 2026-09-21

- Source/declaration: [`InputEventMouse.cs`](../../src/Core/Input/InputEventMouse.cs), `public sealed class InputEventMouseButton : InputEventMouse`.
- Responsibility: non-wheel button or wheel-direction press/release.
- Complete declared API: `ButtonIndex`, `Pressed`, `Canceled`, `DoubleClick`, finite non-negative `Factor`; overrides `IsMatch`, `XformedBy`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. All five declared values are stored typed descriptors.
- Matching/state: binding identity is button plus optional exact modifiers. Canceled is neither press nor release. Wheel directions never enter the held-button mask.
- Transform: returns an independent duplicate with transformed local position; global position is preserved.
- Errors/threading/verification: unknown button, invalid factor, disposed access, and disposed peers fail; caller coordinates mutation. Matching, state, text, duplication, and transforms are covered.
