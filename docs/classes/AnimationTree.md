# AnimationTree

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public class Electron2D.AnimationTree` · **Source:** [AnimationTree.cs](../../src/Scene/Animation/AnimationTree.cs).

**Inherits:** [AnimationMixer](AnimationMixer.md).

## Description

Evaluates reusable typed animation graphs through the inherited property mixer.

The default phase is Idle, Deterministic is true and CallbackModeDiscrete is ForceContinuous. On the first evaluation, the graph receives an internal seek to zero; AnimationNodeAnimation.AdvanceOnStart controls whether it consumes that supplied delta. Active/pause/physics/manual execution uses the inherited mixer and Node scheduling. TreeRoot is borrowed, null disables output, replacement starts the new graph at zero. Parameters are addressed by relative graph paths (empty means root) and exact AnimationParameter key identity. External writes cannot change read-only timing slots. Rename preserves the affected path's cells; root replacement discards old state; removal releases subscriptions on the next preparation.

AnimPlayer optionally resolves an AnimationPlayer scene path, copies its borrowed library memberships, resolves its target root and disables its automatic playback. Provider library/cache or scene topology changes reconcile before the next use. Clearing a previously attached provider clears copied membership and restores RootNode to parent; without a provider, direct inherited libraries work normally. An invalid provider has no copied clips, and a clip leaf then fails by name. The graph submits per-track weighted frames to the existing typed mixer, so ordinary Entity/Control setters and both renderer backends consume the result. CurrentLength/CurrentPosition/CurrentDelta describe the last successfully processed node timeline. Graph start/finish signals are deferred through the SceneTree when attached.

Graph resources borrow child resources; the blend tree owns only its reserved output. Resource duplication copies definition containers, with shallow/deep resource aliases governed by Resource. Evaluation state and typed parameter cells belong to each AnimationTree and graph path. A definition may be shared sequentially across trees; concurrent processing of the same definition is unsupported. Scene controllers and parameter writes obey the inherited Node owner-thread rule. Resource edits invalidate prepared graph/binding state; cold preparation may allocate. No graph factory, disk format, editor UI or expression evaluator is supplied.

Input/child/parameter names use ordinal comparison. Missing names, invalid input indices, wrong typed keys, duplicate schemas and disconnected required inputs fail explicitly. Disposed resources throw ObjectDisposedException; processing helpers outside evaluation throw InvalidOperationException. Nonfinite times/weights/authoring positions reject. Graph cycles reject before connection/self-containment edits or during preparation for custom graphs. Mutation commits before observer notification; TreeChanged reaches every subscribed tree even when another handler throws, collecting graph notification failures. Authoring callback exceptions propagate; the caller can repair the committed graph. Disposal detaches observers and borrows child resources. Nested graph processing guards connection cycles; inherited mixer Advance rejects recursion and invalidates stale property commits after reentrant edits.

## Example

The complete public authoring pattern below requires existing live clips and a scene root. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) compiles it and the per-node variants.

```csharp
// Existing live clips a and b have typed property tracks targeting the same root.
using var library = new AnimationLibrary();
library.AddAnimation("a", a); library.AddAnimation("b", b);
using var graph = new AnimationNodeBlendTree();
using var first = new AnimationNodeAnimation { Animation = "a" };
using var second = new AnimationNodeAnimation { Animation = "b" };
using var blend = new AnimationNodeBlend2();
graph.AddNode("a", first); graph.AddNode("b", second); graph.AddNode("mix", blend);
graph.ConnectNode("mix", 0, "a"); graph.ConnectNode("mix", 1, "b");
graph.ConnectNode("output", 0, "mix");
var controller = new AnimationTree { TreeRoot = graph };
controller.AddAnimationLibrary("", library);
sceneRoot.AddChild(controller); // Existing owner-thread scene root/targets.
controller.SetParameter("mix", AnimationNodeBlend2.BlendAmount, 0.5);
controller.Advance(0);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationTree()` | Initializes deterministic mixing and ForceContinuous discrete evaluation. |

## Constructor Descriptions

<a id="member-16af00ad541a"></a>
### .ctor

`public AnimationTree()`

Initializes deterministic mixing and ForceContinuous discrete evaluation.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.String AnimPlayer { get; set; }` | Gets or sets the relative scene path to an optional AnimationPlayer library provider. |
| `public Electron2D.AnimationRootNode TreeRoot { get; set; }` | Gets or sets the borrowed graph root; null disables graph output. |

## Property Descriptions

<a id="member-f36dc1c3e476"></a>
### AnimPlayer

`public System.String AnimPlayer { get; set; }`

Gets or sets the relative scene path to an optional AnimationPlayer library provider.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-7b584cefb567"></a>
### TreeRoot

`public Electron2D.AnimationRootNode TreeRoot { get; set; }`

Gets or sets the borrowed graph root; null disables graph output.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `public TValue GetParameter<TValue>(System.String nodePath, AnimationParameter<TValue> parameter)` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `public Electron2D.AnimationMixer.AnimationCallbackModeProcess GetProcessCallback()` | Returns the inherited automatic animation update phase. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `protected override System.Void OnNotification(System.Int32 what)` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `public System.Void SetParameter<TValue>(System.String nodePath, AnimationParameter<TValue> parameter, TValue value)` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |
| `public System.Void SetProcessCallback(Electron2D.AnimationMixer.AnimationCallbackModeProcess mode)` | Sets the inherited automatic animation update phase. |

## Method Descriptions

<a id="member-b7d45b19b59b"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

<a id="member-5d05211ea28a"></a>
### GetParameter

`public TValue GetParameter<TValue>(System.String nodePath, AnimationParameter<TValue> parameter)`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

<a id="member-397b13188e75"></a>
### GetProcessCallback

`public Electron2D.AnimationMixer.AnimationCallbackModeProcess GetProcessCallback()`

Returns the inherited automatic animation update phase.

Returns: The current phase.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

<a id="member-8827b39e6e59"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

<a id="member-743dd227feac"></a>
### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

<a id="member-f133cbb5cd92"></a>
### SetParameter

`public System.Void SetParameter<TValue>(System.String nodePath, AnimationParameter<TValue> parameter, TValue value)`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

<a id="member-711f78676212"></a>
### SetProcessCallback

`public System.Void SetProcessCallback(Electron2D.AnimationMixer.AnimationCallbackModeProcess mode)`

Sets the inherited automatic animation update phase.

mode: A defined phase.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action AnimationPlayerChanged` | Occurs after assigning the animation-player path. |

## Event Descriptions

<a id="member-66f5922458d1"></a>
### AnimationPlayerChanged

`public event System.Action AnimationPlayerChanged`

Occurs after assigning the animation-player path.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines, transitions, OneShot, BlendSpaces, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
