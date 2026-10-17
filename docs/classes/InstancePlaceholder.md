# InstancePlaceholder

Last updated: 2026-10-18

**Inherits:** [Node](Node.md)

- **Source:** [InstancePlaceholder.cs](../../src/Scene/Main/InstancePlaceholder.cs), [InstancePlaceholder.Properties.cs](../../src/Scene/Main/InstancePlaceholder.Properties.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed partial class InstancePlaceholder : Node`

Stores a deferred scene path and typed pending values without constructing the deferred scene. It has ordinary Node hierarchy behavior and no spatial transform. [Deferred scenes](../components/deferred-scenes.md) defines capture, files, ownership and verification limits.

| API | Behavior |
| --- | --- |
| `InstancePlaceholder()` | Constructs an empty placeholder. A supplied custom scene can instantiate it after attachment. |
| `GetInstancePath()` | Returns the late lookup path, empty for a manual placeholder. |
| `GetStoredPropertyNames()` | Returns an independent array in pending application order. Ordinary Node fields already apply to the placeholder. |
| `GetStoredValue<T>(name)` | Reads a compatible pending value; arrays are copied and node references are string paths. Missing names and incompatible types throw. |
| `CreateInstance(bool replace = false, PackedScene? customScene = null)` | Creates a sibling at the old position; replacement removes and queues this node. A detached or parentless placeholder returns null. |

An attached placeholder is accessed on the scene owner. Creation is a cold operation; reentry, unfinished hierarchy mutation and disposed state reject. Loading or typed property failures leave the placeholder intact. Callback failures after structural commitment propagate with completed progress retained.

Name and multiplayer authority follow the placeholder. Template notification precedes pending root overrides; tree entry follows them. Pending node paths first resolve inside the new hierarchy and then relative to this placeholder. Existing children stay here; inherited-child overrides skipped during deferred loading are not replayed during creation.

Default loading retains engine-owned templates internally until their final instance/capture/state lease is released. An existing caller-owned cache entry remains borrowed. Pending decoded resources survive parent-template disposal and materialization preserves local aliases and cycles. Dispose the ordinary scene hierarchy and exported SceneState views when finished.
