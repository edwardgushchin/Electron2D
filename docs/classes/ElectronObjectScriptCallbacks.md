# ElectronObjectScriptCallbacks

Last updated: 2026-10-28

**Base:** ordinary C# object · **Source:** [ElectronObjectScriptCallbacks.cs](../../src/Core/Object/ElectronObjectScriptCallbacks.cs) · **Component:** [Scripting](../components/scripting.md#common-native-object-callbacks)

`ElectronObjectScriptCallbacks<TObject> where TObject : ElectronObject`

Single-use immutable callbacks for a compatible live owner. Call `owner.SetScriptCallbacks(set)` only within that owner's synchronous registered Bind. Null, mismatched, worker, reused and duplicate installations reject before transferring another owner's state.

| Member | Contract |
| --- | --- |
| `ElectronObjectScriptCallbacks()` | Creates an unused set. |
| `Notification` | Typed numeric notification and reverse-direction flag; independent of signal blocking. |
| `PropertyDescriptors` | Appends typed script descriptors to the ordinary CLR list. Existing registration identity, native-name collisions and storage validation still apply. |
| `ValidateProperty` | Runs after native validation; returns a descriptor or null to hide it. |
| `CanRevertProperty` | Overrides revert availability; descriptors retain actual typed defaults and writes. |
| `StringRepresentation` | Supplies ToString while installed. CLR overrides retain their normal base-call control. |
| `Retired` | Releases state after Clear and removal of all installed slots, including partial-bind failure and committed disposal. Cancellation retains the set. |

Each query returns an ordinary validated snapshot. Prepared frames and direct callbacks do not rediscover metadata. Source disposal leaves installed code active. Callback data are not engine objects or serialized state; copies and file construction execute Bind to create independent state. [NodeScriptCallbacks](NodeScriptCallbacks.md) and [ResourceScriptCallbacks](ResourceScriptCallbacks.md) extend this same installation and retirement.
