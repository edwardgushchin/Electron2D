# MissingNode

Last updated: 2026-10-21

- **Source:** [MissingNode.cs](../../src/Scene/Main/MissingNode.cs), [property metadata](../../src/Scene/Main/MissingNode.Properties.cs).
- **Declaration:** `public sealed partial class MissingNode : Node`
- **Inherits:** [Node](Node.md)

Retains the authored data of an unavailable compiled node role or linked scene. Only tolerant scene loading constructs this type; it has no public constructor and cannot supply the unavailable compiled behavior. It still has the ordinary Node hierarchy, ownership, groups and lifecycle. Inherited state is applied when its stored schema matches; other fields remain bounded typed archive data.

## Example

```csharp
ResourceLoader.SetAbortOnMissingResources(false);
using var scene = ResourceLoader.Load<PackedScene>("res://level.e2dscene", ResourceLoader.CacheMode.IgnoreDeep);
using var root = scene.Instantiate();
var missing = root.GetNode<MissingNode>("UnavailableActor");
var speed = missing.GetStoredValue<float>("Speed");
missing.SetStoredValue("Speed", speed * 2);
ResourceSaver.SaveScene(root, "res://recovered.e2dscene");
```

The snippet requires a scene with the named unavailable role and a float Speed field. It edits archive state through a declared type; it does not invoke an unavailable member by name.

## Properties

| Signature | Contract |
| --- | --- |
| `public string OriginalClass { get; set; }` | Original portable compiled factory ID, empty for an unavailable linked scene. |
| `public string OriginalScene { get; set; }` | Unavailable linked-scene path; empty for a missing class. |
| `public bool RecordingProperties { get; set; }` | Allows new retained field names; false after loading. Existing names remain editable and may change schema. |
| `public bool RecordingSignals { get; set; }` | Allows new event identities in explicit authored connection records; false after loading. Existing recorded event identities accept further records. |

### Recovery identities

OriginalClass controls the factory identity saved for a class placeholder. A save requires a valid portable ID; a later process that registers that factory reconstructs its real compiled role. OriginalScene describes an unavailable instance and its save warning. Both controls are transient metadata rather than ordinary stored fields. Saving an unavailable scene instance discards that missing template and unavailable inherited overrides, keeping an ordinary hierarchy; it cannot reconstruct the missing file.

### Recording controls

RecordingProperties governs the bounded retained field table. Normal C# Node properties retain their normal runtime behavior. Stored-value operations change archive data, including future fields, rather than arbitrary live members. RecordingSignals governs event labels in the explicit recording method below. It does not synthesize CLR events or replace ordinary typed event subscriptions. New records can execute only after compatible endpoint factories and the original compiled adapter are available again.

## Methods and overrides

| Signature | Contract |
| --- | --- |
| `public string[] GetStoredPropertyNames()` | Independent field-name snapshot in archive order. |
| `public TValue GetStoredValue<TValue>(string name)` | Compatible scalar/array snapshot or borrowed Resource/Node reference; a Node field can also be queried as its current string path. |
| `public void SetStoredValue<TValue>(string name, TValue value)` | Snapshots a portable typed value; a new name requires RecordingProperties. |
| `public IDisposable RecordSceneConnection<TPublisher, TSubscriber, TBindings>(SceneConnectionBinding<TPublisher, TSubscriber, TBindings> binding, Node subscriber, TBindings bindings) where TPublisher : Node where TSubscriber : Node` | Retains a compiled connection definition and typed payload without executing it. The returned lease removes that record; either endpoint's disposal also removes it. |
| `public override string[] GetConfigurationWarnings()` | Explains the unavailable class/scene and resave consequences, preserving base warnings. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Inherited state, transient recovery controls and typed retained descriptors. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Internal fresh-role factory for capture and duplication. |
| `protected override void Dispose(bool disposing)` | Ordinary node cleanup followed by clearing retained references and identities. |

### Stored values

Absent names throw KeyNotFoundException; incompatible requested types throw InvalidCastException. Adding a name with recording disabled throws InvalidOperationException. Existing names may change schema, so a later real factory must declare a compatible stored field or loading fails explicitly. Arrays are copied; resources and nodes are borrowed. Retained node endpoints follow ordinary hierarchy names and are remapped by duplication. Unknown scalar codecs remain encoded until their exact codec is registered; there is no guessed decoder.

The shared archive limits apply: bounded field/name/reference counts, scalar byte budgets, validated roles/codecs and explicit byte order. Disposal and attached wrong-owner access reject. Edits are allocating authoring operations; callbacks observe committed state, and an authoring notification failure can propagate after a field edit.

### Recorded connections

The binding is a registered typed [SceneConnectionBinding](SceneConnectionBinding.md). Its labels are diagnostics and its adapter is compiled C#; no member is invoked by a string. Endpoint and payload schemas are checked when the real connection is reconstructed. Duplicate records remain distinct. Explicit lease removal is idempotent; endpoint disposal retires the metadata. Failed authoring notification rolls back a newly retained record and its newly introduced event identity.

Archive version seven preserves connection metadata and typed payload field roles without requiring the unavailable adapter to run. Earlier versions retain their original layouts; unknown legacy connection schemas and fields without legacy role metadata cannot be inferred. Runtime data does not load assemblies or discover arbitrary types.

## Integration and verification

See [packed scenes](../components/packed-scenes.md), [resource archives](../components/resource-files.md) and [ADR 0023](../decisions/packed-scenes.md#adr-0023). Missing-node recovery is an authoring and runtime loading capability; it does not establish editor acceptance or recover the unavailable component's rendering/gameplay.

## Property Descriptions

<a id="p-electron2d-missingnode-originalclass"></a>
### `public string OriginalClass { get; set; }`

The unavailable portable factory identity, initially empty. Setting null throws ArgumentNullException. A class save with an empty, overlong or control-containing ID fails before replacing the destination. It is a recovery control rather than an archived member of the original role.

<a id="p-electron2d-missingnode-originalscene"></a>
### `public string OriginalScene { get; set; }`

The unavailable linked template path, initially empty. Setting null throws ArgumentNullException. A nonempty value selects the documented missing-instance save consequences; it does not load or reconstruct that file.

<a id="p-electron2d-missingnode-recordingproperties"></a>
### `public bool RecordingProperties { get; set; }`

False after tolerant load. Enables new retained field names; existing names remain editable with compatible new portable schemas even after recording is disabled. Duplication copies this transient control only with InternalState.

<a id="p-electron2d-missingnode-recordingsignals"></a>
### `public bool RecordingSignals { get; set; }`

False after tolerant load. Enables previously unseen diagnostic event identities in explicit compiled connection records. Existing identities permit further records. Duplication copies this transient control only with InternalState.

## Method Descriptions

<a id="m-electron2d-missingnode-getstoredpropertynames"></a>
### `public string[] GetStoredPropertyNames()`

Returns an independent archive-order snapshot. Mutating it changes no state. Disposal and attached wrong-owner access reject.

<a id="m-electron2d-missingnode-getstoredvalue-1-system-string"></a>
### `public TValue GetStoredValue<TValue>(string name)`

Reads name using a requested compatible schema. Scalar/array results are independent snapshots; Resource and Node values are borrowed. Node references also support string-path reads and follow actual endpoint renaming. Absent names throw KeyNotFoundException, incompatible types throw InvalidCastException, and null/blank names reject. Disposed targets read as null; disposing a borrowed live target remains the caller's responsibility.

<a id="m-electron2d-missingnode-setstoredvalue-1-system-string"></a>
### `public void SetStoredValue<TValue>(string name, TValue value)`

Snapshots a portable value into name. New names require RecordingProperties; existing fields may change schema. A Node value records its declared typed endpoint role and relative path; the corresponding string descriptor edits that path. No CLR member is invoked by name. Invalid roles, disposed references, unavailable codecs, unrelated-tree Node paths and archive budget excesses reject. Editor-change handlers run after the field change and may throw without reverting that change.

<a id="m-electron2d-missingnode-recordsceneconnection"></a>
### `public IDisposable RecordSceneConnection<TPublisher, TSubscriber, TBindings>(SceneConnectionBinding<TPublisher, TSubscriber, TBindings> binding, Node subscriber, TBindings bindings) where TPublisher : Node where TSubscriber : Node`

Retains binding and copied bindings for a later reconstruction; returns a removal lease. Null arguments, a known incompatible receiver, disposed endpoints, wrong owner, authoring conflicts and new event labels with RecordingSignals disabled reject. The unavailable receiver may itself be MissingNode. Notification failure removes the attempted record and new event identity. Removing a lease does not dispose borrowed payload resources. It does not execute the original adapter.

<a id="m-electron2d-missingnode-getconfigurationwarnings"></a>
### `public override string[] GetConfigurationWarnings()`

Returns base diagnostics followed by the unavailable identity and save consequence, or an unidentified-recovery warning. It supplies data for an authoring host; it has no editor dependency.

<a id="m-electron2d-missingnode-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns inherited descriptors, nonstored recovery controls and noncolliding typed retained descriptors. Unknown scalar codecs expose independent encoded bytes instead of guessed values. The original schema, role and endianness remain available for resaving.

<a id="m-electron2d-missingnode-createsceneinstancefactory"></a>
### `protected override Func<Node> CreateSceneInstanceFactory()`

Returns the internal fresh recovery factory used by capture and duplication. Restoration installs original identity separately. MissingNode is sealed and exposes no public creation path.

<a id="m-electron2d-missingnode-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Runs ordinary Node cleanup, including connection retirement, then clears retained fields, endpoint references and recovery metadata even if deterministic base cleanup throws. Resources are borrowed and follow the ordinary loaded-scene graph lifetime.

## Verification and limits

Fresh CLR processes write a scene with compiled actors, load it without their factories/adapters, edit and duplicate it, resave it, then restore actual actor processing and scalar/resource-bound compiled connections. Checks cover aliases, scene-local Resource identity, Node reference remapping, independent array snapshots, connection-resource aliases, recording flags, schema changes, owner/disposal guards, authoring failure rollback, atomic failed writes, unavailable linked-scene flattening and independent valid/malformed archives for versions 3–7. Six actual RGB frames per display pair (three per GPU/compatibility backend) verify healthy sibling output, inactive unavailable behavior and normal cleanup on software Vulkan/OpenGL Wayland and X11. These checks establish neither missing gameplay/rendering nor hardware, foreign-platform, editor or native allocator acceptance.
