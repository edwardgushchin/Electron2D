# NavigationAgentDebugCanvas

Last updated: 2026-10-18

- Visibility: internal
- Source: [NavigationAgent.Debug.cs](../../src/Navigation/2D/NavigationAgent.Debug.cs)
- Component: [Agent path diagnostics](../components/navigation-maps.md#agent-path-diagnostics)

## Description

Internal Entity owned by NavigationAgent, lazily created for explicit diagnostics and omitted from ordinary child enumeration/scene archives. Tracks immutable path-array identity and parent visibility, invalidates retained commands, and records world-space polylines/square waypoints without navigation progression. Absolute overlay Z is MaximumZIndex minus one. Existing canvas transforms and renderer storage supply replay; agent disposal owns its lifetime.

## Verification

NavigationDebugTests checks source/project restoration, geometry/default selection, visibility/lifetime and actual GPU/Compatibility pixels. Warm recording/replay and complete owner-frame managed allocation measurements are separate from native/hardware/foreign acceptance.
