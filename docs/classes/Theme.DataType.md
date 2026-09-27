# Theme.DataType

Last updated: 2026-09-27

**Declaration:** `public enum Theme.DataType` · **Source:** [Theme.cs](../../src/Scene/Resources/Theme.cs) · **Owner:** [Theme](Theme.md)

Stable category identities for presence, clear, rename and list operations. All six categories execute through concrete typed resource/value APIs.

| Value | Number | Current operation support |
| --- | --- | --- |
| `Color` | `0` | Typed Color data. |
| `Constant` | `1` | Signed integer constants. |
| `Font` | `2` | Borrowed Font references, null slots and local/default fallback policy. |
| `FontSize` | `3` | Signed integer font-size data and fallback policy. |
| `Icon` | `4` | Borrowed Texture references, including null slots. |
| `StyleBox` | `5` | Borrowed StyleBox references, including null slots. |
| `Max` | `6` | Exclusive upper-bound sentinel; not a data category. |

```csharp
using var theme = new Theme();
theme.SetConstant("separation", "BoxContainer", 8);
bool hasGap = theme.HasThemeItem(Theme.DataType.Constant, "separation", "BoxContainer");
```

<a id="color"></a><a id="constant"></a><a id="font"></a><a id="fontsize"></a><a id="icon"></a><a id="stylebox"></a><a id="max"></a>
The enum is not a bitmask. Max and undefined values throw ArgumentOutOfRangeException in category operations. Typed getter/setter families supply values directly; no Variant-valued Get/Set facade is exposed. See [Theme coverage](../coverage/classes/Theme.md) and [ADR 0083](../decisions/rendering.md#adr-0083).

[ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) and [ThemeFontTests](../../tests/Electron2D.Tests/ThemeFontTests.cs) verify all six categories, Font placeholder/default semantics and invalid-category failures.
