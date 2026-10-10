# ResourceArchive

Last updated: 2026-10-10

Internal/private runtime implementation. **Source:** [ResourceArchive.cs](../../src/Core/IO/ResourceArchive.cs).

[Image-array resources](../components/texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Compressed/integer formats and foreign/native-allocation acceptance retain exact dependencies.

## Description

Version-1 header, bounded payload hashing/compression, atomic files and metadata inspection/rewrite.

## State and lifecycle

See [resource-file component](../components/resource-files.md) for ownership, input budgets, typed schema, synchronous execution and error invariants. Graph preparation/file operations allocate; cache publication follows decode, borrowed external resources remain outside rollback ownership and owned resources are deterministically cleaned up. These types do not expose a user-facing dynamic invocation or public lifetime counter.

## Verification

ResourceArchiveTests exercises the public producer/consumer boundary, graph and scene state, metadata, malformed rollback, exact file ownership and separate-process lifecycle. Foreign/AOT/rendered/editor/native-allocation and human gates remain separate.
