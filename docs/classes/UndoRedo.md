# UndoRedo

Last updated: 2026-10-15

**Namespace:** `Electron2D`. **Declaration:** `public class UndoRedo : ElectronObject`. **Inherits:** [ElectronObject](ElectronObject.md). **Inherited By:** —. **Source:** [UndoRedo.cs](../../src/Core/History/UndoRedo.cs). **Component:** [Undo history](../components/undo-history.md).

Records reversible runtime edits through typed callbacks and property descriptors. It is owned by its creating thread. A completed action has do/undo operation lists, a name, monotonic creation time and undo ordering policy. Cursor minus one means no action is currently applied. Version starts one: ordinary commits/redo increment, undo decrements, merged commits retain the version, and clearing can optionally increment. Runtime history and delegates are not serialized; their resulting scene/resource authoring can be saved normally.

Every operation is attempted. Exceptions aggregate after the cursor/version transition and notifications; a failure does not mean user side effects were rolled back. Stable queries are available during dispatch, while history mutation and disposal are rejected. Disposed direct engine callback/property targets are skipped. A callback closure remains responsible for any indirectly captured object's lifetime.

## Example

```csharp
using var history = new UndoRedo();
var fill = new PropertyDescriptor<ColorRect, Color>(
    "Color", node => node.Color, (node, value) => node.Color = value);
var node = new ColorRect();
history.CreateAction("Paint");
history.AddDoProperty(node, fill, Colors.Blue);
history.AddUndoProperty(node, fill, node.Color);
history.CommitAction();
history.Undo();
history.Redo();
node.Dispose();
```

The descriptor is compiled and typed; no runtime member-name lookup occurs. Attached targets continue to enforce their own owner/capture constraints.

## Constructors

| Signature | Description |
| --- | --- |
| `public UndoRedo()` | Empty thread-owned history, version one, unlimited steps. |

## Properties

| Signature | Default |
| --- | --- |
| `public int MaxSteps { get; set; }` | `0` |

## Methods

| Signature | Description |
| --- | --- |
| `public void CreateAction(string name, MergeMode mergeMode = MergeMode.Disable, bool backwardUndoOps = false)` | Opens or nests action authoring. |
| `public void AddDoMethod(Action callback)` | Registers retained do callback. |
| `public void AddUndoMethod(Action callback)` | Registers retained undo callback. |
| `public void AddDoProperty<TOwner,TValue>(TOwner owner, PropertyDescriptor<TOwner,TValue> property, TValue value) where TOwner : ElectronObject` | Registers typed do assignment. |
| `public void AddUndoProperty<TOwner,TValue>(TOwner owner, PropertyDescriptor<TOwner,TValue> property, TValue value) where TOwner : ElectronObject` | Registers typed undo assignment. |
| `public void AddDoReference(ElectronObject owner)` | Transfers conditional do-side ownership. |
| `public void AddUndoReference(ElectronObject owner)` | Transfers conditional undo-side ownership. |
| `public void StartForceKeepInMergeEnds()` | Marks subsequent do/undo registrations as retained across Ends merges. |
| `public void EndForceKeepInMergeEnds()` | Stops force marking. |
| `public void CommitAction(bool execute = true)` | Closes one nesting level and commits outer action. |
| `public bool Undo()` | Plays current undo list and moves cursor back. |
| `public bool Redo()` | Plays next do list and moves cursor forward. |
| `public void ClearHistory(bool increaseVersion = true)` | Retires history and optionally changes version. |
| `public bool HasUndo()` | Whether committed cursor is nonnegative. |
| `public bool HasRedo()` | Whether committed action follows cursor. |
| `public int GetHistoryCount()` | Completed action count. |
| `public int GetCurrentAction()` | Completed cursor, or -1. |
| `public string GetActionName(int id)` | Name at a valid committed index. |
| `public string GetCurrentActionName()` | Completed cursor name or empty. |
| `public ulong GetVersion()` | Current unsigned version. |
| `public bool IsCommittingAction()` | Initial commit execution/notification/retirement flag. |

## Events

| Signature | Description |
| --- | --- |
| `public event Action? VersionChanged` | Ordered notification after version transitions, including merged commits with unchanged version. |

## Property Descriptions

### MaxSteps

Positive limits apply after top-level commit and its version notification. Old applied prefix is removed in one storage movement. Zero/negative values are retained and mean unlimited. Setting a smaller value alone does not trim existing history. Off-owner/dispatch mutation fails; disposed access fails.

## Method Descriptions

### CreateAction

Validates nonnull name and defined merge mode before discarding the redo branch. Empty names are valid. Nested calls join the outer action and reset force marking; nested names/policies do not replace the outer ones. Committed history remains intact while new operations are authored. This safely preserves an applied action if unfinished merge authoring is disposed.

Merging requires the same name, same backwards policy and elapsed monotonic time strictly below 800 ms. The signed clock counter's wrap uses unsigned elapsed arithmetic. A later matching merge refreshes its creation time. Invalid inputs fail before changing history. Redo-owned cleanup failures are reported after all selected objects retire and before a new action opens.

### AddDoMethod / AddUndoMethod

Require an open action and nonnull callback. They retain the delegate target under normal C# lifetime. Direct disposed ElectronObject targets are skipped at playback; static/ordinary callbacks execute. Multicast behavior within one supplied callback follows the delegate's ordinary invocation contract. Separate operations are always attempted independently. Unforced undo registration during Ends merge is ignored after validation.

### AddDoProperty / AddUndoProperty

Require live typed owner and writable descriptor. The value is captured by ordinary assignment: value types copy, reference values remain retained/borrowed, with no implied deep copy. Registration does not execute validators/setters. Playback uses `SetValue`, continuing to honor target-owned mutation guards. Disposed targets are skipped. An unforced Ends undo registration is ignored after validation. Invalid registration leaves authoring open for correction.

### AddDoReference / AddUndoReference

Explicitly transfer conditional logical ownership of live non-Resource ElectronObjects. Do-side references are disposed when their unapplied redo branch is discarded; applied do-side retirement releases retention without disposing the live object. Undo-side references are disposed when their applied history prefix is removed; unapplied undo-side retirement releases retention without disposal. Resource arguments are rejected to preserve existing asset ownership.

Claims are counted by identity: replacement/merging does not dispose an object retained by another claim. The final matching-side retirement attempts disposal once. Opposite-side retirement transfers lifetime back to caller. Callers should register a coherent creation/deletion action; reference operations themselves perform no edit during playback. Detached created nodes can be retained while undone and disposed when a new branch removes their redo history.

### StartForceKeepInMergeEnds / EndForceKeepInMergeEnds

Require open authoring. Marking applies to both operation lists, including reference claims. Ends merge keeps old forced do operations and all initial undo operations, adds latest do operations and newer forced undo operations. This implements the documented two-sided force-keeping contract.

### CommitAction

An inner commit closes one nesting level without playback. The outer commit records the action, executes do operations unless false, publishes version/notifications, then enforces MaxSteps. `execute=false` is for edits the caller already applied. Ordinary commits append; All merges append both lists but execute only their new do suffix; later redo uses the full combined list. Ends merges preserve initial undo and latest do plus forced operations. Source operation order is registration order; backwards undo reverses the complete undo list at playback.

Operations, observers and retirement can fail. All scheduled work is attempted, busy/commit flags reset, then AggregateException reports errors with history still committed. This is not automatic rollback.

### Undo / Redo

Require completed authoring and idle dispatch. Return false without changes if unavailable. Undo runs the current undo list, then decrements cursor/version; redo advances cursor before do playback and increments version after operations. Notification follows. Both commit their cursor/version even when an operation/observer fails, then throw AggregateException. IsCommittingAction remains false for ordinary playback. Prepared successful playback and ordered event snapshots reuse storage.

### ClearHistory

Requires completed, idle history. Logical history becomes empty before object retirement callbacks. Redo do-side claims retire first in action order, followed by applied undo-side claims in action order. Objects still retained by another claim survive until its final retirement. Cleanup failures do not stop other cleanup. `increaseVersion=true` increments/notifies even for empty history; false preserves version silently.

### HasUndo / HasRedo

Query the committed cursor during authoring or dispatch. Pending edits are not treated as committed redo actions. The top-level CreateAction discards actual redo immediately.

### GetHistoryCount / GetCurrentAction / GetCurrentActionName

Require completed authoring. Count includes undoable and redoable actions. Cursor is -1 before the first applied action, with empty current name. Invalid lifecycle/thread access throws rather than returning an error sentinel.

### GetActionName

Reads committed indexed name, including during authoring. Invalid indices throw ArgumentOutOfRangeException. Pending outer names are not committed entries until top-level commit.

### GetVersion / IsCommittingAction

Version follows the action/clear rules above and is suitable for comparison with a saved version, not elapsed time or globally unique identity. Commit flag includes top-level commit cleanup/notification and excludes authoring, Undo and Redo. Both queries enforce owner/lifetime.

## Event Descriptions

### VersionChanged

Callbacks execute in registration order from retained EventHandlers snapshots. Exceptions do not suppress later observers. History mutation/disposal/subscription mutation during dispatch is rejected. Observer errors join the operation error aggregate. Subscriptions are runtime-only and released at disposal.

## Enumeration Descriptions

[UndoRedo.MergeMode](UndoRedo.MergeMode.md): `Disable=0`, `Ends=1`, `All=2`.

## Protected extension points

| Signature | Description |
| --- | --- |
| `protected override void ValidateMutation()` | Extends owner/lifetime/dispatch protection to inherited mutable object operations. |
| `protected override void ValidateDisposal()` | Rejects off-owner and dispatch-time disposal before publication. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Exposes nonstored MaxSteps tooling metadata. |
| `protected override void Dispose(bool disposing)` | Retires pending/committed ownership and callbacks without serializing history. |

## Verification and limits

UndoRedoTests covers ordering/nesting/record-only commits, signed clock wrap and 800ms boundaries, backward/all/ends/forced merges, conditional reference aliases, cleanup failures/order and 4096-action trimming, properties/validation/disposed targets, thread/reentry guards and real scene/resource file authoring. Warm typed playback/event checks measure 128 Undo/Redo cycles. UndoRedoRenderingTests checks actual scene color and conditional node ownership/branch retirement on GPU/compatibility. See the component for precise native/independent-consumer/foreign-platform/owner limits.
