# InputEventAction

Last updated: 2026-09-21

- Source/declaration: [`InputEventAction.cs`](../../src/Core/Input/InputEventAction.cs), `public sealed class InputEventAction : InputEvent`.
- Responsibility: injects a named registered action independently of hardware bindings.
- Complete declared API: `Action`, `EventIndex` (`-1` or `0..31`), `Pressed`, clamped finite `Strength`; overrides `IsAction`, `IsMatch`, `AsText`; protected creation/copy/property-descriptor hooks. Inherited `IsActionType` classifies this sealed built-in as bindable. All four values are stored typed descriptors. An unindexed direct event uses the slot after mapped bindings and is rejected before mutation when all 32 slots are occupied.
- Lifecycle/state: parsing a press adds one device/index source and parsing a release removes it; a zero-strength press remains logically pressed. `AsText` prefers the action's first non-action binding text and falls back to its own name, avoiding recursive descriptions when synthetic events are registered as bindings.
- Errors/threading: null action assignment, invalid index/non-finite strength, disposed use, or parsing an unregistered action throws. Caller coordinates mutation.
- Verification: direct press/release/strength, source identity, duplication, and descriptions are covered.
