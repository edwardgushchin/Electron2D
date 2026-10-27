# ExtensionManager

Last updated: 2026-10-27

Permanent Engine service with static function-entry loading, exact-address lookup,
address snapshots and unloading. It captures the first mutating caller as owner;
readers can query the registry from other threads. The typed entry returns an
[ExtensionInitialization](ExtensionInitialization.md) or declines with null.

`ExtensionLoaded` precedes registry/path publication on first loading; its observer errors propagate
after publication. `ExtensionUnloading` precedes reverse cleanup and retirement;
cleanup attempts every initialized level despite errors. Nested loads of another
address work; recursive mutation of the same address rejects. The shared event
publisher supports signal suppression and exact EventConnection membership.

`Engine.GetSingleton(nameof(ExtensionManager))` retains one borrowed identity that
cannot be disposed or unregistered. There is no public Instance accessor. See
[module lifecycle](../components/extensions.md) and [ADR 0116](../decisions/extensions.md#adr-0116).
`ReloadExtension(path)` reopens the retained typed entry, restores surviving
registered instances and then sends ExtensionLoaded. `ReloadExtensions(paths)`
attempts a deduplicated changed-address batch and sends ExtensionsReloaded once
after the loaded entries, including failed/restart-required attempts. Its returned
ordinal status dictionary is independent of the registry. Callback errors do not
skip later batch entries and propagate after completion. The host owns change
detection. File/native discovery and actual assembly unloading remain open.
