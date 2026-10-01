# TextureRectExpandMode

Last updated: 2026-10-01

**Declaration:** `public enum TextureRectExpandMode` · **Source:** [TextureRect.cs](../../src/Scene/GUI/TextureRect.cs). Consumed by [TextureRect.ExpandMode](TextureRect.md#expandmode).

## Enumeration descriptions

Let `(w,h)` be the current control size and `(tw,th)` the natural texture size. Null texture contributes `(0,0)` in every mode. Values other than 0–5 reject before assignment.

| Value | Intrinsic minimum |
| --- | --- |
| `KeepSize = 0` | `(tw,th)`; constructor default. |
| `IgnoreSize = 1` | `(0,0)`. |
| `FitWidth = 2` | `(h,0)`. |
| `FitWidthProportional = 3` | `(h*tw/th,0)`; zero if `th == 0`. |
| `FitHeight = 4` | `(0,w)`. |
| `FitHeightProportional = 5` | `(0,w*th/tw)`; zero if `tw == 0`. |

Control combines these with custom minimum and effective maximum bounds. Fit modes depend on current size; multi-wrap FlowContainer preserves current child size before ordinary fitting to avoid wrap feedback. See the [control verification and limits](TextureRect.md#verification-and-limits).
