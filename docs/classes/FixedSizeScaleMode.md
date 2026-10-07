# FixedSizeScaleMode

Last updated: 2026-10-07

**Visibility:** public enum · **Source:** [FontFile.Bitmap.cs](../../src/Scene/Resources/FontFile.Bitmap.cs) · **Component:** [Bitmap fonts](../components/bitmap-fonts.md)

Controls [FontFile.FixedSizeScaleMode](FontFile.md#fixedsizescalemode) for authored/imported bitmap data. Scalable native faces retain ordinary source sizing.

| Value | Integer | Behavior |
| --- | --- | --- |
| `Disable` | 0 | Keeps source glyph pixels, advances and metrics. |
| `IntegerOnly` | 1 | Rounds the positive size ratio away from zero at half; ratios below one half yield zero. |
| `Enabled` | 2 | Uses the exact requested-to-source ratio. |

```csharp
using var font = new FontFile();
font.LoadBitmapFont("res://fonts/ui.fnt");
font.FixedSizeScaleMode = FixedSizeScaleMode.IntegerOnly;
```

FixedSize zero disables source-size selection. Unknown modes fail atomically. FontCacheTests checks ratios and shared layout; native bitmap pixels execute on current Linux GPU/compatibility. Foreign/native-allocator acceptance remains unverified. [ADR 0046](../decisions/rendering.md#adr-0046) owns the text boundary.
