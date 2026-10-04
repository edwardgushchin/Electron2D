# AnimationNode.FilterAction

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AnimationNode.FilterAction` · **Source:** [AnimationNode.cs](../../src/Scene/Animation/AnimationNode.cs).

## Description

Controls how an enabled track filter affects a child contribution.

Ignore multiplies all input paths; Pass only selected paths; Stop only unselected paths; Blend multiplies selected paths and passes other paths unchanged. Actions take effect only when the processing parent supports and enables filtering.

Graph resources borrow child resources; the blend tree owns only its reserved output. Resource duplication copies definition containers, with shallow/deep resource aliases governed by Resource. Evaluation state and typed parameter cells belong to each AnimationTree and graph path. A definition may be shared sequentially across trees; concurrent processing of the same definition is unsupported. Scene controllers and parameter writes obey the inherited Node owner-thread rule. Resource edits invalidate prepared graph/binding state; cold preparation may allocate. No graph factory, disk format, editor UI or expression evaluator is supplied.

Input/child/parameter names use ordinal comparison. Missing names, invalid input indices, wrong typed keys, duplicate schemas and disconnected required inputs fail explicitly. Disposed resources throw ObjectDisposedException; processing helpers outside evaluation throw InvalidOperationException. Nonfinite times/weights/authoring positions reject. Graph cycles reject before connection/self-containment edits or during preparation for custom graphs. Mutation commits before observer notification; TreeChanged reaches every subscribed tree even when another handler throws, collecting graph notification failures. Authoring callback exceptions propagate; the caller can repair the committed graph. Disposal detaches observers and borrows child resources. Nested graph processing guards connection cycles; inherited mixer Advance rejects recursion and invalidates stale property commits after reentrant edits.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationNode.FilterAction Blend = 3` | Selected paths blend; other paths pass unchanged. |
| `public const Electron2D.AnimationNode.FilterAction Ignore = 0` | Multiply all incoming track weights. |
| `public const Electron2D.AnimationNode.FilterAction Pass = 1` | Only selected paths pass. |
| `public const Electron2D.AnimationNode.FilterAction Stop = 2` | Selected paths are stopped. |

## Enumeration Descriptions

<a id="member-968abad62c77"></a>
### Blend

`public const Electron2D.AnimationNode.FilterAction Blend = 3`

Selected paths blend; other paths pass unchanged.

<a id="member-1045cc9853d7"></a>
### Ignore

`public const Electron2D.AnimationNode.FilterAction Ignore = 0`

Multiply all incoming track weights.

<a id="member-b51e5c8724fc"></a>
### Pass

`public const Electron2D.AnimationNode.FilterAction Pass = 1`

Only selected paths pass.

<a id="member-23ebf6e25eb1"></a>
### Stop

`public const Electron2D.AnimationNode.FilterAction Stop = 2`

Selected paths are stopped.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines, transitions, OneShot, BlendSpaces, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
