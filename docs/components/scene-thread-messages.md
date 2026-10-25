# Typed processing-group invocation contract

Last updated: 2026-10-25

This audit covers the complete six-member Node invocation family inherited by physical nodes. ADRs 0004/0006/0104 project named calls and property values to typed delegates and numeric notifications. No runtime name lookup or universal value container is introduced. The pinned Node input at `ed1daf0bf001b61586d9930840f2f1394092c079` declares these six nondeprecated operations; Node's declaring type is also nondeprecated.

| Pinned operation | Public projection | Delivery |
| --- | --- | --- |
| call_thread_safe | CallThreadSafe<TNode>(Action<TNode>) | Immediate on the scene owner/current target group; otherwise queued |
| set_thread_safe | SetThreadSafe<TNode,TValue>(Action<TNode,TValue>,TValue) | Same policy, preserving the ordinary typed setter |
| notify_thread_safe | NotifyThreadSafe(int) | Same policy, ordinary numeric notification dispatch |
| call_deferred_thread_group | CallDeferredThreadGroup<TNode>(Action<TNode>) | Always queued to the attached receiver group |
| set_deferred_thread_group | SetDeferredThreadGroup<TNode,TValue>(Action<TNode,TValue>,TValue) | Same deferred policy with captured typed value |
| notify_deferred_thread_group | NotifyDeferredThreadGroup(int) | Same deferred policy with captured notification |

The pinned `node.cpp:3627–3721` wrappers return nil for both call forms and use exactly this immediate-versus-queued choice. They invoke the ordinary receiver operation. They do not make an otherwise forbidden renderer, geometry, force or hierarchy operation legal. This distinction also appears explicitly in accepted ADR 0006. Coverage previously kept all six wrappers Partial because some receiver operations remained unavailable from workers; that requirement belongs to those receiver/type contracts. General Node/CanvasItem/Entity and physical receiver gaps remain open independently of these six completed operations.

## Queue, errors and lifetime

Null delegates and incompatible typed receivers reject before publication. Deferred calls require a live attached group; detached safe calls retain ordinary detached access. The receiver is weak and identity-qualified. Disposed/collected receivers are skipped; a transferred live receiver follows its current tree owner without changing target identity. Captured reference values and closures retain ordinary caller-owned references until delivery/discard.

Active groups drain FIFO before and after their callbacks. Message masks additionally permit otherwise inactive groups in the selected idle/physics lane; paused/disabled members and default-group delivery have explicit checks. Messages appended during delivery join that drain. Ordinary SceneTree deferred work retains its separate queue/batch contract. Immediate callback errors propagate directly; queued errors join frame errors while later messages and physical work continue. Tree finalization closes acceptance and discards pending callback/value references.

Nested pool jobs helped on the same physical worker retain the current scene group until completion; the enclosing group is restored after successful and failed waits. Later unrelated pool jobs receive no such permission. Group ordering is captured for a frame; changed order applies to later captures, while mask changes govern later eligible stages. No implementation thread or public resource identity is substituted by the wrapper.

## Current executable evidence

The public-only [ThreadMessages](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ThreadMessages.cs), [ProcessGroups](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ProcessGroups.cs), [GroupContext](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.GroupContext.cs) and [GroupMotion](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.GroupMotion.cs) consumers exercise all six operations, actual worker callbacks, nested success/errors, masks, reconfiguration, weak lifetime, cross-owner transfer, queue closure and real selected simulation/point queries. Their warmed full-frame brackets retain strict zero sender/owner/all-thread managed bytes, including separate numerical execution where selected.

Current qualification also exposed a stale regression: GroupContext still required own-group LinearVelocity assignment to fail after the accepted motion-authoring implementation had made it legal. The unchanged frozen consumer failed on that assertion. The corrected fixture calls and sets velocity through the two safe wrappers, checks each accepted value immediately and restores the preceding velocity before owner publication. A force operation retains an explicit rejected-worker control; hierarchy and foreign-group controls remain unchanged. This updates the regression to the accepted behavior without widening numerical permissions or changing runtime code.

Evidence is under `bin/physics-thread-message-contract/2026-10-25/`: verified pinned source hashes, exact commands, source/payload manifests, negative receipt, CLR/strict NativeAOT summaries and `qualification.json`. GPU receipts require NVIDIA mappings and reject software ICDs; CPU processes use dummy video and unavailable ICDs. NativeAOT receipts reject CoreCLR mappings. All 48 profiles passed: 24 CLR and 24 strict NativeAOT, covering all four consumers on CPU/GPU/registered implementations with owner/separate numerical policies. Printed small-scene timings are functional observations, not performance qualification.

These six invocation rows are complete; generic notification propagation is qualified separately below. Node itself, reserved PropagateNotification receivers, general worker scene/GUI/physical authoring, full-suite qualification, cross-process networking, native allocation, foreign platforms, mass-scene advantage and real-window FPS keep their separate open gates. The physical motion component documents the currently supported worker mutation boundary.

## Notification propagation contract

`Node.PropagateNotification(int)` is separately audited against the pinned
[node.cpp traversal](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/node.cpp#L2577-L2587).
The receiver is called immediately before its children, in tree order, including
internal/unowned children. Traversed child topology stays blocked until unwinding.
ADR 0008's typed error adaptation attempts later descendants and siblings before
reporting collected exceptions; traversal depth balances after failures.

Owner/current-group validation precedes entry to each branch. An inaccessible
explicit-group branch rejects before its callback or descendants, while later
accessible siblings continue. Reentrant traversal is synchronous; custom delivery
during capture retains ordinary mutation guards. These rules do not alter the
permissions of a callback, its receiver or its selected numerical implementation.
Manual delivery does not synthesize a lifecycle transition or another physics tick.

The existing public `--group-propagation` consumer checks 18 ordinary/canvas/
physical roles, concurrent groups, inaccessible branches, direct and pooled scene
owners, separate numerical execution, callback failure continuation, capture,
reentry, disposal/topology guards and retained body/shape/query identities.
Related snapshot tests verify immediate logical interpolation/reset observations.
Tree diagnostics check deterministic traversal and recovery; live scene saving
verifies parent-first pre/post delivery to an unsaved internal child and a physical
body, including failed authoring cleanup and real fresh-process reconstruction.

Current receipts are retained in
`bin/physics-propagation-contract/2026-10-25/qualification.json`: all 31 profiles
pass (18 CLR and 13 strict NativeAOT), including 28 zero-byte group-frame brackets.
They use the same unchanged runtime/test source content, selected CPU,
registered and NVIDIA Vulkan GPU worlds, owner/separate numerical policies and
strict zero owner/all-thread managed-byte frame brackets. Software ICDs are
rejected; native consumers reject CoreCLR mappings. These functional/allocation
checks run alongside the ordinary suite and provide no performance comparison.

The generic propagation row closes under the existing ADR. Node, inherited
Object notification reversal/initialization and receiver-specific reserved GUI,
rendering or numerical publication requirements remain open on their own rows.
