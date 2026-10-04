# ResourceFileType

Last updated: 2026-10-05

Internal/private runtime implementation. **Source:** [ResourceFileTypes.cs](../../src/Core/IO/ResourceFileTypes.cs).

## Description

Stable compiled ID/type/factory plus issued-identity weak tracking and exact detached type validation.

## State and lifecycle

See [resource-file component](../components/resource-files.md) for ownership, input budgets, typed schema, synchronous execution and error invariants. Graph preparation/file operations allocate; cache publication follows decode, borrowed external resources remain outside rollback ownership and owned resources are deterministically cleaned up. These types do not expose a user-facing dynamic invocation or public lifetime counter.

## Verification

ResourceArchiveTests exercises the public producer/consumer boundary, graph and scene state, metadata, malformed rollback, exact file ownership and separate-process lifecycle. Foreign/AOT/rendered/editor/native-allocation and human gates remain separate.
