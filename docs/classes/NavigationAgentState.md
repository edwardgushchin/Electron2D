# NavigationAgentState

Last updated: 2026-10-07

- Visibility: internal
- Source: [NavigationServer.Agents.cs](../../src/Servers/Navigation/NavigationServer.Agents.cs)
- Component: [Navigation agents](../components/navigation-maps.md#agent-path-following)

## Description

Stable caller/node RID, weak scene owner, staged map membership and consumed last committed map iteration. Scene disposal/weak sweep releases the RID; map deletion detaches surviving agents.

## Verification

[NavigationAgentTests](../../tests/Electron2D.Tests/NavigationAgentTests.cs) exercises membership, version consumption, teardown and scene following.

Shared avoidance settings/current/preferred velocity, reusable neighbor/edge/linear-program buffers and callback delivery versions now drive the ORCA kernel. The scene callback belongs to this state and dereferences the weak node at delivery; it does not retain a detached enabled node. All outputs publish together before any observer.
