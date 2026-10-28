# RigidBodyScriptCallbacks

Last updated: 2026-10-28

**Base:** CollisionObjectScriptCallbacks · **Source:** [RigidBodyScriptCallbacks.cs](../../src/Scene/2D/RigidBodyScriptCallbacks.cs) · **Component:** [Scripting](../components/scripting.md#standard-node-callbacks)

`RigidBodyScriptCallbacks<TBody> where TBody : RigidBody`

Adds native post-solver direct-state integration for a compatible RigidBody receiver.

IntegrateForces runs on the ordinary owner integration lane. The borrowed PhysicsDirectBodyState follows its existing attachment/query lifetime. Native bodies with this callback participate in the same required-state publication and dispatch as compiled integration overrides.

Create a fresh set and call `owner.SetScriptCallbacks(set)` only from its own synchronous registered Script Bind. Receiver mismatch, reuse, duplicate installation and foreign/worker/post-Bind calls reject. A set is not an engine object or serialized state; copy/file reconstruction executes the registered binding to construct independent callbacks. The remaining fixed CLR/native-callback family and source-free NativeAOT registration remain unqualified. See [the owning contract](../decisions/scripting.md#live-attachment-and-rebuild-boundaries).

Canvas recording is inherited through [CanvasItemScriptCallbacks](CanvasItemScriptCallbacks.md).
