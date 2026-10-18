# NavigationAgentDebugSettings

Last updated: 2026-10-18

- Visibility: internal
- Source: [NavigationServer.Debug.cs](../../src/Servers/Navigation/NavigationServer.Debug.cs)
- Component: [Agent path diagnostics](../components/navigation-maps.md#agent-path-diagnostics)

## Description

Internal immutable record of agent-path enablement, default color and square point size. NavigationServer samples the three typed ProjectSettings definitions with feature overrides on first diagnostics use and retains the values for the process lifetime. It does not enable global region/link/avoidance diagnostics.

## Verification

NavigationDebugTests checks source/project restoration, geometry/default selection, visibility/lifetime and actual GPU/Compatibility pixels. Warm recording/replay and complete owner-frame managed allocation measurements are separate from native/hardware/foreign acceptance.
