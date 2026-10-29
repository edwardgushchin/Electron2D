# TextServer.Feature

Last updated: 2026-10-29

Flags describing a text implementation’s layout, font and Unicode capabilities. GetFeatures returns a mask; HasFeature accepts one defined value. Capabilities describe implementation behavior and do not replace renderer/platform gates.

Source: [TextServer.Protocols.cs](../../src/Servers/Text/TextServer.Protocols.cs). See [external implementations](../components/text-server.md#external-implementations).

| Value | Flag |
| --- | --- |
| `BIDILayout` | `2` |
| `BreakIterators` | `32` |
| `ContextSensitiveCaseConversion` | `2048` |
| `FontBitmap` | `64` |
| `FontDynamic` | `128` |
| `FontMSDF` | `256` |
| `FontSystem` | `512` |
| `FontVariable` | `1024` |
| `KashidaJustification` | `16` |
| `Shaping` | `8` |
| `SimpleLayout` | `1` |
| `UnicodeIdentifiers` | `8192` |
| `UnicodeSecurity` | `16384` |
| `UseSupportData` | `4096` |
| `VerticalLayout` | `4` |
