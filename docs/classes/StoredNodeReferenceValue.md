# StoredNodeReferenceValue

Last updated: 2026-10-05

Source: [`PropertyDescriptor.cs`](../../src/Core/Object/PropertyDescriptor.cs). Internal sealed implementation of the internal StoredPropertyValue base used by [typed property descriptors](PropertyDescriptor.Generic.md).

Stores a declared node-reference type and nullable relative path captured from a node owner. It holds no original node reference. PackedScene defers restoration until all nodes and owners exist, then resolves the path in each new hierarchy. Missing targets become null; incompatible target types fail reconstruction and use ordinary rollback. Resource transformations preserve this value because it contains no resources.

SceneState string queries return the relative path, with an empty string for null. Other value queries fail. ShortcutTests verifies forward context references across two independent instances and disposal of the original hierarchy. See [ADR 0023](../decisions/scene.md#adr-0023).

ResourceArchiveTests also verifies typed file storage and reconstruction of forward/backward references after separate-process load. See [resource files](../components/resource-files.md) for schema and verification limits.
