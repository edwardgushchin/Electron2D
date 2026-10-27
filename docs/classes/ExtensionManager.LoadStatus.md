# ExtensionManager.LoadStatus

Last updated: 2026-10-27

Exact module operation identities: Ok = 0, Failed = 1, AlreadyLoaded = 2,
NotLoaded = 3, NeedsRestart = 4. Function loading returns Failed when the entry
declines; callback/validation errors retain typed exception propagation. Duplicate
addresses do not invoke another entry. Unloading an absent address returns
NotLoaded. NeedsRestart retains its reference identity for the separate replacement
workflow; no reload operation is claimed by this lifecycle stage.

See [ExtensionManager](ExtensionManager.md) and [the executing component](../components/extensions.md).
