# Undo history decisions

Last updated: 2026-10-15

<a id="adr-0105"></a>
## ADR 0105: Typed runtime action history

### Status

Accepted. Refines [typed values](product.md#adr-0001), [typed notifications](product.md#adr-0002), [deterministic lifetime](core-object-runtime.md#adr-0003), [typed properties](core-object-runtime.md#adr-0005), [runtime/editor direction](product.md#adr-0027) and [agent authoring](agent-native.md#adr-0090).

### Decision

UndoRedo is a reusable Core `ElectronObject`, not an editor application service. Runtime consumers can use it for scene/resource authoring through public `Action` callbacks and `PropertyDescriptor<TOwner,TValue>` writes. No string dispatch, reflection, universal value container or separate assembly is introduced. Property values are captured by normal C# assignment; reference values and callback targets remain borrowed and retained until their operations retire. Disposed direct engine targets are skipped. Validators/setters run at playback.

One creating thread owns the history. Mutating history or disposing it during operation/notification/cleanup dispatch is rejected; stable queries remain available. Nested action creation joins one outer action. The committed history stays intact while new operations are authored; metadata queries that require a completed action reject unfinished authoring. A top-level CreateAction discards the redo branch. Commit(false) records an already-applied edit without executing callbacks.

Retain named actions, cursor, version starting one, 800 ms monotonic name/backward-policy merge window, forward/default or reversed undo order, MergeAll and MergeEnds, force-kept operations, and positive MaxSteps trimming at commit. Unlimited MaxSteps retains nonpositive authored values. MergeAll executes only the new do suffix on commit; later redo executes the complete merged list. MergeEnds keeps the earliest undo state and newest do state, plus explicitly forced operations. Both do and undo operations honor force keeping: the pinned implementation omits the flag on do registrations despite the documented two-sided contract; this implementation corrects that omission. Merged commits preserve version; ordinary commit/redo increment and undo decrements; clearing optionally increments and notifies.

Actions are not automatic state transactions. Every scheduled operation is attempted; failures are aggregated after cursor/version and remaining notifications reach their documented committed state. An operation that fails does not imply its user side effects were rolled back. History notifications run in registration order and continue after an observer failure. IsCommittingAction reports commit execution/notification only, matching its scope rather than pretending undo/redo are initial commits.

AddDoReference/AddUndoReference explicitly transfer conditional logical ownership of non-Resource ElectronObjects. Redo-branch retirement disposes do-side claims; applied-prefix retirement disposes undo-side claims. Opposite claims release retention without disposal. Resource arguments are rejected, preserving borrowed asset/lease ownership under ADR 0014. Count aliases so replacement/merging does not dispose an object still retained by another history claim; final matching-side retirement attempts deterministic disposal once. Pending authoring disposal retires its new do claims and preserves unexecuted undo claims. Committed history is cleared according to its actual applied/unapplied state; building a merge never falsely makes an already-applied action unapplied.

Authoring, registration, branch retirement, merging, cleanup and diagnostics are cold transitions. Prepared successful Undo/Redo and notification loops reuse operation lists with no managed allocations. User callback/native allocation is independently reported. No serialization of delegates, history or editor policy is promised. A future EditorUndoRedoManager consumes this public runtime capability and supplies its separate context/UI integration.

### Verification boundaries

Require managed ordering/merging/ownership/error/reentry/clock/limit cases, actual public scene/resource authoring and persistence, real rendered undo/redo on current backends, and a reflection-disabled independent consumer. Headless authoring and native rendering remain distinct. Other platforms, native allocation and owner/editor acceptance are separate gates.
