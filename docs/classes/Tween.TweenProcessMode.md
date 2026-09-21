# Tween.TweenProcessMode

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
Declaration: `public enum Tween.TweenProcessMode`

This nested enum selects the SceneTree frame lane for a [`Tween`](Tween.md). `Physics = 0` advances after physics-node callbacks and matching tree timers; `Idle = 1` advances after ordinary process-node callbacks and matching timers and is the default. Undefined values are rejected without state change. Lane mutation is owner-thread-only; changing it during an earlier phase can affect which later frame first captures the tween. Tests verify numeric identities and lane isolation. There is no physics simulation implication: the enum selects only callback timing.
