# FontLCDSubpixelLayout

Last updated: 2026-10-14

- Declaration: `public enum FontLCDSubpixelLayout`
- Source: [FontAntialiasing.cs](../../src/Scene/Resources/FontAntialiasing.cs)
- Component: [text coverage](../components/text.md#native-font-antialiasing-and-lcd-layout)

Selects text-startup component ordering: None=0, HorizontalRGB=1, HorizontalBGR=2, VerticalRGB=3, VerticalBGR=4, Max=5. ProjectSettings.LCDSubpixelLayout captures the preference on first native font-owner use and marks later assignments restart-required. Max is a nonselectable bound. None requests grayscale for an LCD font; enabled arrangements generate logical RGB coverage from native horizontal/vertical triples. Exact native oracles and actual pixel hosts verify all five selectable layouts.
