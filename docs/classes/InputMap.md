# InputMap

Last updated: 2026-09-21

## Declaration

- Source: [`InputMap.cs`](../../src/Core/Input/InputMap.cs)
- Declaration: `public sealed class InputMap : ElectronObject`
- Domain/component: [Input](../domains/input.md) / [Input runtime](../components/input-runtime.md)

## Responsibility and ownership

`InputMap` is the non-disposable process-wide registry of ordinal action names, finite deadzones, and ordered typed event bindings. Binding `Resource` references remain caller-owned and live; they must not be disposed or mutated concurrently with matching. Disposing a registered binding outside matching removes that reference from every affected action.

## Complete public/protected API

| Member | Current behavior |
| --- | --- |
| `AllDevices = -1` | Controller binding wildcard |
| `DefaultDeadzone = 0.2f` | Default deadzone for a new action |
| `static InputMap Instance` | Process-lifetime singleton |
| `HasAction`, `GetActions`, `AddAction`, `EraseAction` | Ordinal registry and registration-order snapshots |
| `ActionGetDeadzone`, `ActionSetDeadzone` | Validated `[0,1]` deadzone |
| `ActionAddEvent`, `ActionHasEvent`, `ActionEraseEvent`, `ActionEraseEvents`, `ActionGetEvents` | At most 32 exact-deduplicated action-compatible bindings; an earlier all-device controller binding matches a concrete-device query |
| `EventIsAction` | Typed match with optional exact modifiers/direction |
| `GetActionDescription` | Human-readable disjunction of concrete bindings; synthetic action indirection is omitted |
| `ValidateDisposal()` | Always rejects disposal |

## Lifecycle, errors, threading, and interactions

Configuration operations are serialized by one lock and return snapshots. Duplicate exact bindings are ignored; missing actions and invalid values throw before mutation. A successful map mutation or internal binding-change notification clears every affected action's cached runtime contributions through `Input` before fallible public `Resource.Changed` handlers run. Copying stored state into a registered binding follows the same ordering. Binding disposal removes it and clears contributions before public disposal handlers run. No action-setting persistence exists yet; its exact trigger is ADR 0038's versioned action-schema row.

Tests cover ordering, validation, deadzones, the 32-source ceiling, duplicates, exact modifiers, matching, failure-safe action-state invalidation, singleton lifetime, and concurrent-safe snapshots. Native controller mapping databases are a distinct SDL gamepad trigger and are not represented here.
