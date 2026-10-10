# NavigationServer.IterationWork

Last updated: 2026-10-10

Internal implementation. **Source:** [NavigationServer.Iterations.cs](../../src/Servers/Navigation/NavigationServer.Iterations.cs).

## Description

Owns captured pure geometry/topology input, its worker task identity, prepared immutable result and typed failure. Workers access no live scene/resource state or server gate. Every task is retired at owner completion, explicit host drain or RID teardown. See [background iterations](../components/navigation-maps.md#background-iterations).
