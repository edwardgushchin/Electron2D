# SaveAndSwapStream

Last updated: 2026-10-21

Internal sealed Stream in [SaveAndSwapStream.cs](../../src/Core/IO/SaveAndSwapStream.cs). Owns an exclusive FileStream sibling and one captured final target for ordinary write-only FileAccess. The OS policy selects it only at open. It forwards seek/resize/write without allocating on prepared writes, flushes physical data, and publishes only after successful deterministic close. Failed writes abort and remove partial staging; failed final replacement retains completed recovery data. Internal producer cancellation prevents invalid PCK/OGV output from being committed. It neither owns project path resolution nor replaces the existing transformed/configuration atomic helper. See [file save and swap](../components/file-save-and-swap.md) for paths, permissions, platform behavior and verification limits.
