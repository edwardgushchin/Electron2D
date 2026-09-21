# 0001: Use typed C# without Variant

Last updated: 2026-09-20

- Status: Accepted
- Scope: Entire engine API

## Context

Godot uses `Variant` as a universal value container for scripting, dynamic property access, generic signals, serialization, and editor integration. Electron2D is a C# engine library and currently has no GDScript-compatible runtime or editor that requires this dynamic boundary.

## Decision

Electron2D will not implement `Variant`. Public APIs use concrete types, generics, overloads, typed collections, properties, methods, delegates, and events. The engine will not recreate Variant through pervasive `object`, `dynamic`, or untyped metadata dictionaries.

## Consequences

- Invalid type combinations are rejected at compile time.
- IDE completion, navigation, and refactoring work normally.
- The engine avoids boxing and dynamic dispatch imposed by a universal container.
- Electron2D is not source-compatible with GDScript or Godot's string-based `Get`, `Set`, and `Call` APIs.
- Serialization, when introduced, must use typed models rather than a universal runtime value.

## Rejected alternatives

- Clone Godot `Variant`: rejected because its main consumers do not exist in Electron2D.
- Use `object` or `dynamic` as an implicit Variant: rejected because it loses type safety while providing a weaker contract.
- Add Variant pre-emptively for a future editor or scripting language: rejected until such a boundary is actually designed.
