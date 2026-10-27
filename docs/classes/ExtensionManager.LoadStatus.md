# ExtensionManager.LoadStatus

Last updated: 2026-10-27

Exact module operation identities: Ok = 0, Failed = 1, AlreadyLoaded = 2,
NotLoaded = 3, NeedsRestart = 4. Function loading returns Failed when the entry
declines; callback/validation errors retain typed exception propagation. Duplicate
addresses do not invoke another entry. Unloading an absent address returns
NotLoaded. NeedsRestart identifies a reopened entry whose earlier minimum requires host restart;
it remains open but uninitialized with captured instance state retained for explicit retry.

See [ExtensionManager](ExtensionManager.md) and [the executing component](../components/extensions.md).
