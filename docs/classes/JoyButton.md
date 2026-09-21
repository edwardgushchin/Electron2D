# JoyButton

Last updated: 2026-09-21

- Source/declaration: [`InputEnums.cs`](../../src/Core/Input/InputEnums.cs), `public enum JoyButton`.
- Complete values: `Invalid = -1`, `A`, `B`, `X`, `Y`, `Back`, `Guide`, `Start`, `LeftStick`, `RightStick`, `LeftShoulder`, `RightShoulder`, `DpadUp`, `DpadDown`, `DpadLeft`, `DpadRight`, `Misc1`, `Paddle1`, `Paddle2`, `Paddle3`, `Paddle4`, `Touchpad`, `Misc2`, `Misc3`, `Misc4`, `Misc5`, `Misc6`, `SdlMax = 26`, `Max = 128`.
- Responsibility: standardized controller buttons plus raw numeric indices through 127; sentinels are not event values.
- Invariants/errors: event/query APIs accept only numeric `0..127`. Immutable allocation-free value.
- Verification: standardized/raw boundaries, invalid sentinels, per-device held state, matching, and release-all are covered.
