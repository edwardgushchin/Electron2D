# PhysicsAreaRuntime

Last updated: 2026-10-19

**Declaration:** `internal sealed class PhysicsAreaRuntime`

**Source:** [PhysicsAreaRuntime.cs](../../src/Servers/Physics/PhysicsAreaRuntime.cs)
**Component:** [Physics server and direct queries](../components/physics-queries.md#area-observer-registration-transactions)

Retains one scene/raw Area RID's optional body and Area observers, logical pair
tracker, reusable changes and queued-event generation. Scene/raw owners supply
current world, filter and monitorability; this object owns no solver or geometry.
Scene-owned snapshots remain independent of these external observers.

SetCallback validates owner/lifetime/numerical/own-dispatch access, prepares the
attached world's monitoring capacity, then publishes the delegate and resets both
histories. Failure before publication retains the old delegate references, pair
history and generation; no compensating fallible backend call is required.
Successful equal/null assignments still reset both histories. Reset advances the
generation to suppress obsolete queued events. Detach retains callback authoring
while resetting history. Free retires registry membership; queued events reject
the retired identity and release their runtime references when the queue drains.

Raise ignores object-level duplicates and projects logical shape changes through
the current lane. Its dispatch flag is retired in finally, including callback
failure. The space owns later-event continuation and queued-epoch validation.

The public-only transaction profile verifies 24 registered preparation failures,
rejected candidate weak targets, both callback lanes, logical index gaps and exact
queued delivery, together with complete CPU/software-GPU/registered warm frames.
Broader numerical/native/foreign/performance acceptance remains separate.

[Prepared Area field inputs](../components/physics-areas.md#prepared-area-field-inputs)
permit all ten configured getters for the owning scene/raw/default profile inside
selected numerical hooks, including the captured solver thread. Existing common
input guards preserve ordinary owner/failure/lifetime checks; writes remain guarded.
Readonly profiles match selected preparation and require no live field-state query.
The public 64-frame edited/acceleration fixture retains strict zero managed bytes.
