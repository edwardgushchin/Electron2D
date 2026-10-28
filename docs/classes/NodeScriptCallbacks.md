# NodeScriptCallbacks

Last updated: 2026-10-28

**Base:** [ElectronObjectScriptCallbacks](ElectronObjectScriptCallbacks.md) · **Source:** [NodeScriptCallbacks.cs](../../src/Scene/Main/NodeScriptCallbacks.cs) · **Component:** [Scripting](../components/scripting.md#standard-node-callbacks)

`NodeScriptCallbacks<TNode> where TNode : Node`

Immutable single-use tree/ready/frame/input callbacks for a compatible Node receiver. [CanvasItemScriptCallbacks](CanvasItemScriptCallbacks.md) adds drawing; physical callback sets inherit that canvas role.

EnterTree, ExitTree, Ready, Process, PhysicsProcess, Input, ShortcutInput, UnhandledKeyInput and UnhandledInput use typed delegates. Notification, Retired and object metadata/string hooks are inherited from ElectronObjectScriptCallbacks. Retired runs once after slot removal; callback targets own ordinary C# state. Ready enables authored callback lanes before delivery. Numeric notifications include an explicit reverse-direction flag.

Create a fresh set and call `owner.SetScriptCallbacks(set)` only from its own synchronous registered Script Bind. Receiver mismatch, reuse, duplicate installation and foreign/worker/post-Bind calls reject. A set is not an engine object or serialized state; copy/file reconstruction executes the registered binding to construct independent callbacks. The remaining fixed CLR/native-callback family and source-free NativeAOT registration remain unqualified. See [the owning contract](../decisions/scripting.md#live-attachment-and-rebuild-boundaries).
