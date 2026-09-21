# Tween.TransitionType

Last updated: 2026-09-21

Source: [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
Declaration: `public enum Tween.TransitionType`

This nested enum chooses the normalized interpolation family: `Linear = 0`, `Sine = 1`, `Quint = 2`, `Quart = 3`, `Quad = 4`, `Expo = 5`, `Elastic = 6`, `Cubic = 7`, `Circ = 8`, `Bounce = 9`, `Back = 10`, and `Spring = 11`. Linear is the Tween default. `EaseType` determines direction. Elastic, Bounce, Back, and Spring may overshoot; extrapolation outside the duration follows the equation and may yield non-finite values for a mathematically undefined region. Undefined values are rejected. Tests cover stable identities, exact endpoints, linear values, the distinct Expo/Elastic/Back in-out equations, and invalid enums; they do not numerically certify every point of every curve against another implementation.
