# NodeScriptCallbacks

Last updated: 2026-10-28

**Base:** ordinary C# object · **Source:** [NodeScriptCallbacks.cs](../../src/Scene/Main/NodeScriptCallbacks.cs) · **Component:** [Scripting](../components/scripting.md#standard-node-callbacks)

`NodeScriptCallbacks<TNode> where TNode : Node`

Immutable single-use tree/ready/frame/input/notification callbacks for a compatible Node receiver.

EnterTree, ExitTree, Ready, Process, PhysicsProcess, Input, ShortcutInput, UnhandledKeyInput, UnhandledInput, Notification and Retired use typed delegates. Retired runs once after slot removal; callback targets own ordinary C# state. Ready enables authored callback lanes before delivery. Numeric notifications include an explicit reverse-direction flag.

Create a fresh set and call `owner.SetScriptCallbacks(set)` only from its own synchronous registered Script Bind. Receiver mismatch, reuse, duplicate installation and foreign/worker/post-Bind calls reject. A set is not an engine object or serialized state; copy/file reconstruction executes the registered binding to construct independent callbacks. The fixed CLR/Resource/cache/RPC family and source-free NativeAOT registration remain unqualified. See [the owning contract](../decisions/scripting.md#live-attachment-and-rebuild-boundaries).
