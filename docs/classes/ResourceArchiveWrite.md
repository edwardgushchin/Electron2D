# ResourceArchiveWrite

Last updated: 2026-10-12

Internal/private runtime implementation. **Source:** [ResourceArchive.cs](../../src/Core/IO/ResourceArchive.cs).

## Description

Identity discovery and typed property/scene/pixel encoder; paths commit after persistence.

## State and lifecycle

See [resource-file component](../components/resource-files.md) for ownership, input budgets, typed schema, synchronous execution and error invariants. Graph preparation/file operations allocate; cache publication follows decode, borrowed external resources remain outside rollback ownership and owned resources are deterministically cleaned up. These types do not expose a user-facing dynamic invocation or public lifetime counter.

## Verification

ResourceArchiveTests exercises the public producer/consumer boundary, graph and scene state, metadata, malformed rollback, exact file ownership and separate-process lifecycle. Foreign/AOT/rendered/editor/native-allocation and human gates remain separate.

Version-2 role-aware missing-resource storage, snapshot policy and reference relocation are described in [missing resource recovery](../components/resource-files.md#missing-resource-recovery). Known version-1 schemas remain readable; unknown legacy identities do not become safe placeholders.
