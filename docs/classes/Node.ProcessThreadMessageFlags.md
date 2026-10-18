# Node.ProcessThreadMessageFlags

Last updated: 2026-10-18

Namespace: `Electron2D`; declaring owner: [`Node`](Node.md). Source: [Node.ProcessGroups.cs](../../src/Scene/Main/Node.ProcessGroups.cs).

```csharp
var worker = new Node { ProcessThreadMessages = Node.ProcessThreadMessageFlags.Physics };
```

| Value | Number | Meaning |
| --- | --- | --- |
| Process | 1 | Permits idle message-only work. |
| Physics | 2 | Permits physics message-only work. |
| All | 3 | Permits both message-only lanes. |

The owning Node properties retain this exact value contract in packed and portable scenes. Unknown selectable values/bits reject before mutation. Zero is valid as an empty message mask. Active callback groups process their queues independently of that empty-group mask. See the [Node processing contract](Node.md#explicit-processing-groups) and [public conformance consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ProcessGroups.cs); full inherited/physical worker access remains separately Partial.

The scene owner or current processing group may set this mask on its own members, including inherited physical nodes. Changes control later message-only eligibility without interrupting an active drain; see the [group metadata consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.GroupContext.cs).
