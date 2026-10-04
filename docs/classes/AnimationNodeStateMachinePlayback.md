# AnimationNodeStateMachinePlayback

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationNodeStateMachinePlayback` · **Source:** [AnimationNodeStateMachinePlayback.cs](../../src/Scene/Animation/AnimationNodeStateMachinePlayback.cs).

**Inherits:** [Resource](Resource.md).

## Description

A per-tree state-machine controller with queued commands, route diagnostics and state events.

Start/Travel/Next/Stop queue commands until graph evaluation. Current node is initially Start; IsPlaying is initially false and ResourceLocalToScene is initially true. Start discards routes/fades and optionally resets. Travel uses shortest geometric distance times nonnegative edge priority through enabled/automatic edges, teleporting on missing routes or group exits; self travel is ignored unless the definition permits it. Route getters return independent arrays. Next forces the selected route/automatic edge and propagates through the deepest group; it does not invent an edge. Stop releases route/fade state and retires participating nested/audio effects through the mixer. End remains playing. Current and outgoing timing diagnostics use seconds. Selection can already name the destination while the last sampled pose still belongs to the source. State events are synchronous after committed selection or retirement; grouped leaf events also reach ancestor controllers with prepared qualified names. Event commands invalidate stale graph/property commits and remain queued for the next pass. Predicate/observer errors propagate; already accepted earlier clocks/effects are not rolled back. Grouped public commands reject, while Root/Nested ancestry accepts grouped slash paths and forbids descendant Start/End requests. The tree owns attached controllers and disposes them when obsolete; users borrow returned instances. Sequential shared definitions retain independent state per path/tree. Test-only uses prepared scratch and preserves real requests, clocks, events and output. First preparation/distinct slash parsing may allocate; warmed command/route/cached-group and idle passes allocate no owner managed storage. Standalone duplicate copies controller memory and borrows its authored state resources; it remains unattached.

See [state machines](../components/scene-animation.md#state-machines) for runtime order, integration and verification boundaries. Scene controllers execute on their SceneTree owner thread; resource authoring retains the Resource thread/lifetime contract. No external backend dependency or second scene clock is introduced.

## Example

This snippet requires the indicated live tree, borrowed clip definitions and library/target setup. AnimationStateMachineTests compiles and exercises this public workflow.

```csharp
var playback = tree.GetParameter("", AnimationNodeStateMachine.Playback);
playback.StateStarted += state => Console.WriteLine(state);
playback.Start("group/idle");
tree.Advance(0);
playback.Travel("group/run");
tree.Advance(0.1);
string current = playback.GetCurrentNode();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationNodeStateMachinePlayback()` | Creates an unattached controller; attached instances are supplied through the tree's Playback key. |

## Constructor Descriptions

<a id="member-97342e579013"></a>
### .ctor

`public AnimationNodeStateMachinePlayback()`

Creates an unattached controller; attached instances are supplied through the tree's Playback key.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited resource lifecycle for this concrete type. |
| `public System.Double GetCurrentLength()` | Returns the current state's last observed timeline length in seconds. |
| `public System.String GetCurrentNode()` | Returns the current local state, initially Start. |
| `public System.Double GetCurrentPlayPosition()` | Returns the current state's last processed timeline position in seconds. |
| `public System.Double GetFadingFromLength()` | Returns the outgoing state's timeline length in seconds. |
| `public System.String GetFadingFromNode()` | Returns the outgoing local state or empty when no fade remains. |
| `public System.Double GetFadingFromPlayPosition()` | Returns the outgoing state's timeline position in seconds. |
| `public System.Double GetFadingLength()` | Returns the selected crossfade duration in seconds. |
| `public System.Double GetFadingPosition()` | Returns elapsed crossfade time in seconds. |
| `public System.String[] GetTravelPath()` | Returns an independent snapshot of unvisited route states. |
| `public System.Boolean IsPlaying()` | Tests whether graph evaluation has activated playback; End remains playing. |
| `public System.Void Next()` | Queues immediate activation of the selected route/automatic edge, ending any fade. |
| `public System.Void Start(System.String node, System.Boolean reset = true)` | Queues direct activation, discarding the route and outgoing fade. |
| `public System.Void Stop()` | Queues playback stop and route/fade cleanup. |
| `public System.Void Travel(System.String toNode, System.Boolean resetOnTeleport = true)` | Queues a shortest-cost route, teleporting when no enabled route exists. |

## Method Descriptions

<a id="member-383fd1c2387f"></a>
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

<a id="member-f0113f558dfa"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-c1c483f3a78d"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited resource lifecycle for this concrete type.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-2127ac7a58af"></a>
### GetCurrentLength

`public System.Double GetCurrentLength()`

Returns the current state's last observed timeline length in seconds.

Returns: The length.

System.ObjectDisposedException: The controller is disposed.

<a id="member-c53e467c9be6"></a>
### GetCurrentNode

`public System.String GetCurrentNode()`

Returns the current local state, initially Start.

Returns: The local name, or empty after removal.

System.ObjectDisposedException: The controller is disposed.

<a id="member-4934b8715232"></a>
### GetCurrentPlayPosition

`public System.Double GetCurrentPlayPosition()`

Returns the current state's last processed timeline position in seconds.

Returns: The position.

System.ObjectDisposedException: The controller is disposed.

<a id="member-dd23e9004a88"></a>
### GetFadingFromLength

`public System.Double GetFadingFromLength()`

Returns the outgoing state's timeline length in seconds.

Returns: The length, or zero after retirement.

System.ObjectDisposedException: The controller is disposed.

<a id="member-3e7c63904d4d"></a>
### GetFadingFromNode

`public System.String GetFadingFromNode()`

Returns the outgoing local state or empty when no fade remains.

Returns: The outgoing local name.

System.ObjectDisposedException: The controller is disposed.

<a id="member-dd992d6921f2"></a>
### GetFadingFromPlayPosition

`public System.Double GetFadingFromPlayPosition()`

Returns the outgoing state's timeline position in seconds.

Returns: The position, or zero after retirement.

System.ObjectDisposedException: The controller is disposed.

<a id="member-e90e4f1315a4"></a>
### GetFadingLength

`public System.Double GetFadingLength()`

Returns the selected crossfade duration in seconds.

Returns: The duration, retained after completion.

System.ObjectDisposedException: The controller is disposed.

<a id="member-6130f861aa12"></a>
### GetFadingPosition

`public System.Double GetFadingPosition()`

Returns elapsed crossfade time in seconds.

Returns: The elapsed fade clock.

System.ObjectDisposedException: The controller is disposed.

<a id="member-4f66603be930"></a>
### GetTravelPath

`public System.String[] GetTravelPath()`

Returns an independent snapshot of unvisited route states.

Returns: The remaining local route names.

System.ObjectDisposedException: The controller is disposed.

<a id="member-842402fbe772"></a>
### IsPlaying

`public System.Boolean IsPlaying()`

Tests whether graph evaluation has activated playback; End remains playing.

Returns: The active flag.

System.ObjectDisposedException: The controller is disposed.

<a id="member-3c6e8cd5fe47"></a>
### Next

`public System.Void Next()`

Queues immediate activation of the selected route/automatic edge, ending any fade.

System.InvalidOperationException: This is grouped playback or the command is outside the attached scene owner thread.

System.ObjectDisposedException: The controller or machine is disposed.

<a id="member-49b32969f73b"></a>
### Start

`public System.Void Start(System.String node, System.Boolean reset = true)`

Queues direct activation, discarding the route and outgoing fade.

node: An exact local state or grouped descendant path.

reset: Whether its timeline starts at zero; initially true.

System.ArgumentException: The target is invalid or absent.

System.InvalidOperationException: This is grouped playback or the command is outside the attached scene owner thread.

System.ObjectDisposedException: The controller or machine is disposed.

<a id="member-3f08d3989148"></a>
### Stop

`public System.Void Stop()`

Queues playback stop and route/fade cleanup.

System.InvalidOperationException: This is grouped playback or the command is outside the attached scene owner thread.

System.ObjectDisposedException: The controller or machine is disposed.

<a id="member-121f562c6b6e"></a>
### Travel

`public System.Void Travel(System.String toNode, System.Boolean resetOnTeleport = true)`

Queues a shortest-cost route, teleporting when no enabled route exists.

toNode: An exact local state or grouped descendant path.

resetOnTeleport: Whether an unreachable destination resets; initially true.

System.ArgumentException: The target is invalid or absent.

System.InvalidOperationException: This is grouped playback or the command is outside the attached scene owner thread.

System.ObjectDisposedException: The controller or machine is disposed.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<System.String> StateFinished` | Occurs when an outgoing state finishes fading, or is replaced without a fade. |
| `public event System.Action<System.String> StateStarted` | Occurs after a state becomes current; grouped leaf names also reach ancestor controllers. |

## Event Descriptions

<a id="member-8bc522bf5476"></a>
### StateFinished

`public event System.Action<System.String> StateFinished`

Occurs when an outgoing state finishes fading, or is replaced without a fade.

<a id="member-90296894bec7"></a>
### StateStarted

`public event System.Action<System.String> StateStarted`

Occurs after a state becomes current; grouped leaf names also reach ancestor controllers.


## Verification and dependencies

[AnimationStateMachineTests](../../tests/Electron2D.Tests/AnimationStateMachineTests.cs) covers public authoring, defaults, edge guards, copies/aliases, route costs/fallback, timing/fades/boundaries, typed conditions, multiple groups, state events, independent sharing, rename/removal, preparation and observer failures, reentry, tested output and method/nested effects. Warm checks use 64 prepared command/route cycles, 64 cached grouped starts and 64 idle frames with zero owner managed bytes. Separate SDL-dummy/FAudio PCM checks verify state audio start/restart/stop and departed nonblended cues. Linux Wayland GPU/compatibility hosts each run twice and check six actual pixel poses and borrowed-resource cleanup. Native/driver allocator totals, physical listening, other platforms, file/editor and human acceptance remain unverified. [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed graph/condition/ownership contract.
