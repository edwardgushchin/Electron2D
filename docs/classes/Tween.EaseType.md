# Tween.EaseType

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
Declaration: `public enum Tween.EaseType`

This nested enum chooses transition direction: `In = 0` accelerates, `Out = 1` decelerates, `InOut = 2` is slowest at both ends, and `OutIn = 3` is fastest at both ends. InOut is the Tween default. Linear produces the same value for every direction. Undefined values are rejected without state change. Tests cover stable identities, endpoint behavior, manual interpolation, and invalid values.
