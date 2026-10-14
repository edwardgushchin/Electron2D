# FontAntialiasing

Last updated: 2026-10-14

- Declaration: `public enum FontAntialiasing`
- Source: [FontAntialiasing.cs](../../src/Scene/Resources/FontAntialiasing.cs)
- Component: [text coverage](../components/text.md#native-font-antialiasing-and-lcd-layout)

Controls native glyph coverage: None=0 produces monochrome, Gray=1 grayscale, LCD=2 component coverage. FontFile.Antialiasing and SystemFont.Antialiasing share this exact identity. Unknown numeric font preferences remain stored and rasterize grayscale. LCD uses the captured FontLCDSubpixelLayout; None layout selects grayscale. Native FreeType oracle, resource/lifetime and GPU/compatibility consumers are verified in FontAntialiasingTests.
