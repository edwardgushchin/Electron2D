# EngineCopyrightPart

Last updated: 2026-11-14

**Inherits:** —

- **Source:** [EngineCopyrightInfo.cs](../../src/Core/Config/EngineCopyrightInfo.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class EngineCopyrightPart`

An immutable source and legal-attribution part of an
[EngineCopyrightInfo](EngineCopyrightInfo.md) catalog entry. All lists reject
mutation through mutable collection interfaces; consumers do not own the value.

| Property | Type | Meaning |
| --- | --- | --- |
| `Files` | `IReadOnlyList<string>` | Exact repository-relative notice paths and recorded vendored source glob scopes |
| `Copyright` | `IReadOnlyList<string>` | Verbatim legal-document/source-header attribution; empty for terms without a recorded specific attribution |
| `License` | `string` | Exact document key in `Engine.GetLicenseInfo()` |

Document keys preserve provider versions and composite terms. File scopes identify
catalog sources, not every compiled file or loaded native dependency. See
[distribution metadata](../components/engine-runtime.md#distribution-metadata).
