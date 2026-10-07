# FontVariationAxis

Last updated: 2026-10-07

**Declaration:** `public readonly record struct FontVariationAxis(float Minimum, float Maximum, float Default)` · **Source:** [FontInstance.cs](../../src/Servers/Text/FontInstance.cs) · **Component:** [Text](../components/text.md)

An immutable OpenType axis range in design coordinates, returned by Font.GetSupportedVariationList. These three scalars describe font coordinates, rather than a spatial vector. Native fvar metadata validates Minimum ≤ Default ≤ Maximum.

| Signature | Contract |
| --- | --- |
| `public FontVariationAxis(float Minimum, float Maximum, float Default)` | Stores the supplied design range; construction does not load a font. |
| `public float Minimum { get; init; }` | Lowest supported design coordinate. |
| `public float Maximum { get; init; }` | Highest supported design coordinate. |
| `public float Default { get; init; }` | Coordinate used when no override is supplied. |

The record supplies value equality, hashing, comparison operators, Deconstruct and ToString through ordinary C# record semantics. User-created records may contain arbitrary scalar values; native metadata queries return validated ranges. There is no native ownership or disposal obligation.
