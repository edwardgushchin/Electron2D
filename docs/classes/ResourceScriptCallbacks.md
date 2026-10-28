# ResourceScriptCallbacks

Last updated: 2026-10-28

**Base:** [ElectronObjectScriptCallbacks](ElectronObjectScriptCallbacks.md) · **Source:** [ResourceScriptCallbacks.cs](../../src/Core/IO/ResourceScriptCallbacks.cs) · **Component:** [Scripting](../components/scripting.md#common-native-object-callbacks)

`ResourceScriptCallbacks<TResource> where TResource : Resource`

Single-use immutable resource callbacks installed during the owner's registered synchronous Bind. Notification, property discovery/validation, revert availability, string representation and Retired use the common object contract.

| Member | Contract |
| --- | --- |
| `ResourceScriptCallbacks()` | Creates an unused set. |
| `Func<TResource, RID>? GetRID { get; init; }` | Supplies a borrowed identity only to the base Resource getter; concrete native overrides retain their IDs. |
| `Action<TResource>? ResetState { get; init; }` | Follows native reset on explicit reset and all copy-target reset paths, before restoration/code replacement. |
| `Action<TResource, string>? SetPathCache { get; init; }` | Follows successful native raw-path handling after commitment, including equal raw writes. |
| `Action<TResource>? SetupLocalToScene { get; init; }` | Runs after native local setup, with restored data and GetLocalScene available. Failure follows the ordinary acquired-graph rollback. |

An ordinary detached Node.Duplicate keeps borrowed resource references under its existing policy; scene instantiation performs local duplication/setup. Callback retirement and resource graph ownership remain separate: an asset is not a second owner of installed callback state. Fixed CLR replacement and source-free NativeAOT registration remain open.
