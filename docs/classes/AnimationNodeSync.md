# AnimationNodeSync

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public class Electron2D.AnimationNodeSync` · **Source:** [AnimationNode.cs](../../src/Scene/Animation/AnimationNode.cs).

**Inherits:** [AnimationNode](AnimationNode.md).

**Inherited By:** [AnimationNodeBlend2](AnimationNodeBlend2.md), [AnimationNodeBlend3](AnimationNodeBlend3.md), [AnimationNodeAdd2](AnimationNodeAdd2.md), [AnimationNodeAdd3](AnimationNodeAdd3.md), [AnimationNodeSub2](AnimationNodeSub2.md).

## Description

Shared zero-weight input-clock synchronization policy for multi-input graph resources.

Sync defaults to false. A branch whose per-track weights are all effectively zero receives delta zero when Sync is false. Seeking still runs all branches. Sync true advances their clocks even while silent; nonzero branches always advance.

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

This member-use snippet assumes the indicated node is installed at the matching controller path and all required inputs are connected.

```csharp
using var node = new AnimationNodeBlend2 { Sync = true };
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeSync()` | Creates a new independent instance with the defaults described above. |

## Constructor Descriptions

<a id="member-35a1f9eb18ec"></a>
### .ctor

`public AnimationNodeSync()`

Creates a new independent instance with the defaults described above.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean Sync { get; set; }` | Gets or sets whether zero-weight inputs advance their clocks; defaults to false. |

## Property Descriptions

<a id="member-a4f35ad48d6d"></a>
### Sync

`public System.Boolean Sync { get; set; }`

Gets or sets whether zero-weight inputs advance their clocks; defaults to false.

Value: The typed value described in the summary.

System.ObjectDisposedException: This resource/controller or a required borrowed resource has been disposed.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited resource copy, metadata or cleanup contract for this concrete type. |

## Method Descriptions

<a id="member-fc05a962d0f3"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`

Copies derived stored state into a duplicate or copy target.

target: A live resource with the exact same runtime type.

deep: Whether typed collection containers should be cloned recursively.

subresourceMode: The nested-resource policy for this copy.

duplicateSubresource: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource. Pass every nested resource through this function when deep is true.

forceDuplicateSubresource: A graph-preserving function that duplicates a nested resource even when the current policy would share it. Use it for typed properties whose contract requires duplication; assign the original reference directly for properties whose contract forbids duplication.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Derived implementations must copy all stored custom state and call the base implementation only when they intentionally want its validation. Assigning the original nested-resource reference directly expresses a never-duplicate property.

System.NotSupportedException: A derived resource has not explicitly implemented custom-state copying.

<a id="member-bb5988eed7b8"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-d274d822d80b"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited resource copy, metadata or cleanup contract for this concrete type.

Remarks: Appends resource identity and scene-instancing configuration descriptors.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines, transitions, OneShot, BlendSpaces, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
