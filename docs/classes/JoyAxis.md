# JoyAxis

Last updated: 2026-09-21

- Source/declaration: [`InputEnums.cs`](../../src/Core/Input/InputEnums.cs), `public enum JoyAxis`.
- Complete values: `Invalid = -1`, `LeftX = 0`, `LeftY = 1`, `RightX = 2`, `RightY = 3`, `TriggerLeft = 4`, `TriggerRight = 5`, `SdlMax = 6`, `Max = 10`.
- Responsibility: standardized first six controller axes plus raw indices through 9; sentinels are not event values.
- Invariants/errors: event/query APIs accept only numeric `0..9`. Immutable allocation-free value.
- Verification: standardized/raw boundaries, invalid sentinels, per-device state, direction, and deadzones are covered.
