# CryptoDigestState

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [HashingContext.cs](../../src/Core/Networking/HashingContext.cs). **Component:** [Crypto](../components/crypto.md).

Internal value owner for one IncrementalHash context. Start prepares provider state; nonempty chunk validation belongs to its public contexts. Finish validates destination capacity before mutation, writes into caller storage and releases the provider even on failure. Explicit Dispose closes unfinished work; the BCL handle retains its native finalization safety. No per-update engine buffers are allocated.

CryptoTests exercises public callers, lifecycle and prepared allocation boundaries. Provider-native allocations and foreign host policy remain separate.
