# 0005: Use numeric notifications and typed property descriptors

Last updated: 2026-09-20

- Status: Accepted
- Scope: Engine lifecycle notifications and tooling property exposure

## Context

Godot's `Object` exposes numeric notifications plus Variant-based property discovery, access, validation, and revert hooks. Electron2D needs recognizable lifecycle IDs and a future tooling boundary, but ADR 0001 rejects a universal `Variant` and string-addressed runtime mutation.

## Decision

- Engine lifecycle notifications keep stable Godot-compatible numeric IDs where a corresponding Electron2D lifecycle exists.
- `ElectronObject.Notify(int)` synchronously invokes the protected `OnNotification(int)` override.
- Pre-delete notification `1` is automatic. Post-initialization notification `0` is only reserved because a base constructor cannot safely call virtual members.
- Tooling properties use immutable `PropertyDescriptor<TOwner, TValue>` instances with typed delegates.
- Discovery returns heterogeneous descriptors through the non-generic `PropertyDescriptor` base, but value access remains generic and typed.
- Derived classes may filter or replace descriptors with `ValidateProperty`; they notify tooling of structural changes through a typed event.

## Consequences

- Lifecycle compatibility does not require a dynamic call or value system.
- Editor tooling can inspect property metadata while compile-time types govern reads and writes.
- Consumers cannot set a property by an arbitrary string name.
- Descriptor authors explicitly provide setters, validators, and revert factories; there is no reflection scanner.
- Notification callbacks are synchronous and exceptions propagate according to the caller's lifecycle contract.

## Rejected alternatives

- Recreate Godot property dictionaries and `Variant`: rejected by ADR 0001.
- Use reflection over every public property: rejected because editor exposure and mutability must be explicit.
- Invoke post-initialization from `ElectronObject` construction: rejected because virtual dispatch can observe an incompletely constructed derived object.
