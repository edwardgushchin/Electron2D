# ExtensionInitializationLevel

Last updated: 2026-10-27

One shared module level contract: Core = 0, Servers = 1, Scene = 2, Editor = 3.
The initialization value and Extension minimum-level observation use this same
namespace-level enum under [ADR 0051](../decisions/enum-identities.md#adr-0051).
The manager captures available library-host levels before invoking the entry,
initializes upward and deinitializes the completed prefix in reverse.

See [ADR 0116](../decisions/extensions.md#adr-0116) and the
[module component](../components/extensions.md) for lifecycle and scope.
