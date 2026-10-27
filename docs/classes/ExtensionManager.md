# ExtensionManager

Last updated: 2026-10-27

Permanent Engine service with static function-entry loading, exact-address lookup,
address snapshots and unloading. It captures the first mutating caller as owner;
readers can query the registry from other threads. The typed entry returns an
[ExtensionInitialization](ExtensionInitialization.md) or declines with null.

`ExtensionLoaded` precedes registry/path publication; its observer errors propagate
after publication. `ExtensionUnloading` precedes reverse cleanup and retirement;
cleanup attempts every initialized level despite errors. Nested loads of another
address work; recursive mutation of the same address rejects. The shared event
publisher supports signal suppression and exact EventConnection membership.

`Engine.GetSingleton(nameof(ExtensionManager))` retains one borrowed identity that
cannot be disposed or unregistered. There is no public Instance accessor. See
[module lifecycle](../components/extensions.md) and [ADR 0116](../decisions/extensions.md#adr-0116).
File loading, registered-class restoration and live reload remain open.
