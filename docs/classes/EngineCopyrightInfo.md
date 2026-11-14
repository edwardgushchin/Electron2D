# EngineCopyrightInfo

Last updated: 2026-11-14

**Inherits:** —

- **Source:** [EngineCopyrightInfo.cs](../../src/Core/Config/EngineCopyrightInfo.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class EngineCopyrightInfo`

An immutable named component in the build's distribution catalog, returned by
`Engine.GetCopyrightInfo()`. Consumers borrow the process-lifetime value; they
cannot construct, modify or dispose it.

| Property | Type | Meaning |
| --- | --- | --- |
| `Name` | `string` | Stable distribution catalog entry name |
| `Parts` | `IReadOnlyList<EngineCopyrightPart>` | Immutable source/attribution/license parts |

See [distribution metadata](../components/engine-runtime.md#distribution-metadata)
for catalog sources, document identity and qualification limits.
