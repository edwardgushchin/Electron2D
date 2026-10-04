# Animation.LoopedFlag

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.Animation.LoopedFlag` · **Source:** [../Resources/Animation.cs](../../src/Scene/Animation/../Resources/Animation.cs).

## Description

Identifies an endpoint crossed by clip playback.

None, End and Start identify a crossed clip endpoint for graph scheduling. Property sampling derives traversal from time/delta; flag-specific effects remain attached to future method/audio/nested-animation schedulers.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.Animation.LoopedFlag End = 1` | The end endpoint was crossed. |
| `public const Electron2D.Animation.LoopedFlag None = 0` | No endpoint was crossed. |
| `public const Electron2D.Animation.LoopedFlag Start = 2` | The start endpoint was crossed. |

## Enumeration Descriptions

<a id="member-71a27b212f57"></a>
### End

`public const Electron2D.Animation.LoopedFlag End = 1`

The end endpoint was crossed.

<a id="member-503e27ddc04a"></a>
### None

`public const Electron2D.Animation.LoopedFlag None = 0`

No endpoint was crossed.

<a id="member-6cd3ec680bc6"></a>
### Start

`public const Electron2D.Animation.LoopedFlag Start = 2`

The start endpoint was crossed.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines/grouped controllers, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
