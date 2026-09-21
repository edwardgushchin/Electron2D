# Tween.TweenPauseMode

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
Declaration: `public enum Tween.TweenPauseMode`

This nested enum controls [`Tween`](Tween.md) eligibility while `SceneTree.Paused` is true. `Bound = 0` follows a bound node's resolved `CanProcess()` policy and otherwise behaves like Stop; `Stop = 1` pauses with the tree; `Process = 2` ignores tree pause. Bound is the default. Undefined values are rejected without state change and mutation uses the owner thread. Tests cover unbound tree pause and pause-independent processing; Node processing tests cover the inherited policies used by Bound. This is a scheduling policy, not `Tween.Pause()` state.
