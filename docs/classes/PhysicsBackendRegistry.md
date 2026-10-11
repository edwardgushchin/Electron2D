# PhysicsBackendRegistry

Last updated: 2026-10-11

**Declaration:** `internal sealed class PhysicsBackendRegistry` · **Source:** [PhysicsBackendRegistry.cs](../../src/Servers/Physics/PhysicsBackendRegistry.cs) · **Component:** [Backend selection and ownership](../components/physics-backends.md#named-implementation-selection)

## Description

Retains ordinal, case-sensitive names and factories behind a cold registry lock. The retained PhysicsServer owns the production registry; existing explicit CPU/GPU creation resolves its built-in entries. Registration does not construct a solver or graphics device. CPU is the initial default at priority zero; GPU is also reserved. DEFAULT is a selection token and cannot be registered. Names are not trimmed; null, empty or whitespace-only names reject.

## Internal example

Partial runtime/test snippet; these types are inaccessible to game consumers:

```csharp
var registry = new PhysicsBackendRegistry();
registry.RegisterServer("Custom", space => CreateImplementation(space));
registry.SetDefaultServer("Custom", 1);
var selection = registry.Resolve("DEFAULT");
```

`CreateImplementation` must return a fresh PhysicsWorldBackend owned by the supplied registered space. The common construction transaction validates ownership, initializes the implementation and retires failed space/native state.

## API summary and member descriptions

| Internal member | Contract |
| --- | --- |
| `PhysicsBackendRegistry()` | Reserve CPU/GPU; select CPU at priority zero without constructing either. |
| `void RegisterServer(string name, Func<PhysicsSpace, PhysicsWorldBackend> factory)` | Retain a nonnull factory under a fresh name; duplicate/reserved names reject with ArgumentException. |
| `void SetDefaultServer(string name, int priority)` | Require an existing entry; replace the default only at strictly greater priority. Equal/lower priorities preserve it. |
| `Selection Resolve(string name)` | Capture the requested name and current registration under the gate; unknown names reject. DEFAULT captures the current default, not a later lookup. |

### Selection

`internal readonly record struct Selection(string RequestedName, Registration Registration)` is an immutable retained descriptor. Its two generated properties expose the request token and selected registration internally. WorldRuntime retains it before lazy physics creation; duplicate World resources share that runtime and descriptor. Changing the default affects future resolutions only.

### Registration and Creation

`internal sealed class Registration` retains `string Name`, `PhysicsServer.Backend? BuiltIn` and an optional custom factory. Its internal constructors take `(string name, PhysicsServer.Backend kind)` or `(string name, Func<PhysicsSpace, PhysicsWorldBackend> factory)`. `void Create(PhysicsSpace space, bool allowCPUFallback, Action<PhysicsWorldBackend> initialize)` invokes the actual factory outside the registry lock; built-ins use the existing startup/fallback policy. Custom factories own their implementation choice and failures propagate through the same construction transaction.

Private `sealed record Creation(Registration Registration, Creation? Parent)` forms an immutable execution-context chain in AsyncLocal. The chain remains borrowed through retained-owner initialization. Creating the same registration recursively, including from initialization or through a child execution context, throws InvalidOperationException. Finally restores the previous frame after success or failure; independent executions may create separate worlds. This cold construction guard is absent from warmed physics steps.

## Lifecycle, verification and limits

PhysicsSpace retains internal requested/actual names after selection: DEFAULT preserves its requested token, a custom owner reports its registration name, and a built-in startup fallback reports GPU requested / CPU actual. Public enum diagnostics remain the existing CPU/GPU contract. This registry is an internal prerequisite: public PhysicsServerManager, PhysicsServerBackend/Extension, named World/server entry points and the complete operation bridge remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

[Named factory tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.NamedFactories.cs) exercise priority ties, immutable lazy aliases, factories outside the lock, fresh scene/explicit owners, selected direct views, real doubled impulse response after a full interval, recursion, null/throwing/foreign owners and recovery. Cold cross-thread checks use dedicated joined threads, avoiding asynchronous thread-pool work in later allocation brackets. CPU/GPU run 64 warmed impulse/step/read intervals with a zero owner/all-thread managed-byte assertion. No-device checks preserve strict GPU failure and explicit startup fallback. Native allocation, foreign platforms and public third-party consumer acceptance remain unverified.
