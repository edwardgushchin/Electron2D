# FontFeatureInfo

Last updated: 2026-10-13

**Source:** [FontFeatureInfo.cs](../../src/Servers/Text/FontFeatureInfo.cs) · **Component:** [Text](../components/text.md)

**Declaration:** `public readonly record struct FontFeatureInfo(string? Label, bool IsBoolean, bool Hidden)`

An immutable description from [Font.GetSupportedFeatureList](Font.md#getsupportedfeaturelist), representing the bounded OpenType feature metadata role. It owns no native object. Record equality compares all three values; the generated deconstructor returns them in constructor order. Its default value has null Label and false IsBoolean/Hidden, representing an unlabeled visible integer selector.

## Constructor and properties

| Member | Contract |
| --- | --- |
| `public FontFeatureInfo(string? Label, bool IsBoolean, bool Hidden)` | Copies the three immutable metadata values. |
| `public string? Label { get; init; }` | Font-defined localized label; null when no label record exists, possibly empty when its locale has no text. |
| `public bool IsBoolean { get; init; }` | True for registered on/off features, false for integer selectors including unknown tags. |
| `public bool Hidden { get; init; }` | True when ordinary feature-selection UI should omit a registered shaping control. |

```csharp
var features = font.GetSupportedFeatureList();
foreach (var pair in features)
    if (!pair.Value.Hidden)
        Console.WriteLine($"{pair.Key:X8}: {pair.Value.Label ?? "OpenType feature"}");
```

The example assumes a live Font. Metadata does not restrict a caller's authored feature values; existing FontFile/FontVariation validation and shaping remain authoritative. FontMetadataTests verifies boolean/integer/hidden/optional-label behavior and applies a discovered feature to real shaping and native glyph pixels. This typed C# result projects the existing feature-description dictionary under ADR 0004 and ADR 0046.
