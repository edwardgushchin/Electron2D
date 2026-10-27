# ExtensionInitialization

Last updated: 2026-10-27

Readonly typed value returned by a module entry. Its constructor validates one
[ExtensionInitializationLevel](ExtensionInitializationLevel.md) and non-null
ascending/descending callbacks. Closures carry caller-defined state. A default
value has no callbacks and rejects when supplied to the manager; a nullable result
may instead decline loading. The minimum level describes replacement prerequisites,
while first loading calls every available level from Core.

This is the C# callback/initialization record projection under
[ADR 0116](../decisions/extensions.md#adr-0116), not a serializable script instance.
See [module lifecycle and verification](../components/extensions.md).
