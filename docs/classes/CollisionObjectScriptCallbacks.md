# CollisionObjectScriptCallbacks

Last updated: 2026-10-28

**Base:** NodeScriptCallbacks · **Source:** [CollisionObjectScriptCallbacks.cs](../../src/Scene/2D/CollisionObjectScriptCallbacks.cs) · **Component:** [Scripting](../components/scripting.md#standard-node-callbacks)

`CollisionObjectScriptCallbacks<TObject> where TObject : CollisionObject`

Adds native physical pointer callbacks for a compatible CollisionObject receiver.

InputEvent retains the selecting viewport, borrowed event and global logical shape index. MouseEnter, MouseExit, MouseShapeEnter and MouseShapeExit execute at native hook points before their ordinary typed signals, retaining the existing error and signal-blocking policy.

Create a fresh set and call `owner.SetScriptCallbacks(set)` only from its own synchronous registered Script Bind. Receiver mismatch, reuse, duplicate installation and foreign/worker/post-Bind calls reject. A set is not an engine object or serialized state; copy/file reconstruction executes the registered binding to construct independent callbacks. The fixed CLR/Resource/cache/RPC family and source-free NativeAOT registration remain unqualified. See [the owning contract](../decisions/scripting.md#live-attachment-and-rebuild-boundaries).
