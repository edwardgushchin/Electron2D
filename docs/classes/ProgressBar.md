# ProgressBar

Last updated: 2026-10-26

Themed display for a shared numeric [Range](Range.md). It draws background and fill styles, followed by centered percentage text and its optional outline. Indeterminate mode draws a clipped moving segment and hides the text. Both current renderers execute the same retained canvas commands.

```csharp
public class ProgressBar : Range
```

```csharp
var progress = new ProgressBar
{
    Name = "LoadingProgress", Size = new(240, 28),
    MaxValue = 100, Value = 37.5
};
// Attach to a viewport/window; update Value through the scene owner.
```

## API summary

| Signature | Contract/default |
| --- | --- |
| `public ProgressBar()` | Inherits Step=0.01, MinValue=0, MaxValue=100, Value=0 and vertical ShrinkBegin. |
| `public ProgressFillMode FillMode { get; set; }` | BeginToEnd; every assignment resets animation phase. |
| `public bool ShowPercentage { get; set; }` | True; indeterminate mode suppresses text without changing this setting. |
| `public bool Indeterminate { get; set; }` | False; changes reset phase and internal animation processing. |
| `public bool EditorPreviewIndeterminate { get; set; }` | False; controls animation inside the selected EditedSceneRoot subtree. |

## Properties

<a id="fillmode"></a>
**FillMode:** [ProgressFillMode](ProgressFillMode.md) preserves BeginToEnd=0, EndToBegin=1, TopToBottom=2 and BottomToTop=3. Horizontal directions follow RTL; vertical directions remain physical. Fill travel subtracts the integer part of the fill style's minimum axis extent and rounds pixel lengths away from zero. The final extent adds the original fractional style minimum. Reverse placement independently rounds the remaining travel, so fractional style margins can extend the result slightly beyond the control; ordinary canvas clipping policy applies. Zero rounded travel draws no fill.

<a id="showpercentage"></a>
**ShowPercentage:** font metrics for `100%` and background content margins determine text minimum height. Text-free minimum is the component-wise maximum of both styles and one pixel. Text remains centered in every direction; outline draws before glyph fill. Percentage uses Range's linear/exponential policy and allow flags, while filled geometry uses its clamped Ratio. Thus AllowGreater may display `150%` with a full bar. Zero values use the explicit logarithmic origin for exponential percentage calculation. LocalizeNumeralSystem and the translation domain's locale determine digits and the percent sign. A bounded cache retains prepared formatted percentages; new text is cold authoring work.

<a id="indeterminate"></a>
**Indeterminate:** segment length is twice the smaller control dimension. Internal process steps advance at `max(200, max(width, height) / 2)` pixels per second, with source directional wrap and clipping. Ordinary process-mode, pause and time-scale policy applies. Hidden controls preserve phase. Range value and ShowPercentage do not affect the segment.

<a id="editorpreviewindeterminate"></a>
**EditorPreviewIndeterminate:** select this control or an ancestor through [SceneTree.EditedSceneRoot](SceneTree.md#diagnostics-editedsceneroot). False draws a static segment using the centered preview calculation; true animates it. Changing this setting in the edited subtree resets phase. Outside the selected subtree it does not reset or pause animation. Selection changes are observed on the next internal process step. This is an executable authoring preview using ordinary scene APIs; it does not start a visual editor or alter general scene processing.

## Themes, lifecycle and persistence

The built-in theme supplies all seven items: `background`, `fill`, `font`, `font_size`, `font_color`, `font_outline_color` and `outline_size`. [Control](Control.md) typed lookup and overrides preserve inherited theme resolution. Borrowed style/font edits trigger the existing theme invalidation and minimum-size flow. Font color defaults to (0.95, 0.95, 0.95, 1), black outline color and zero outline size. Styles use two-pixel content margins and six-pixel corner radii at the current scale-one theme profile.

Access follows scene owner, lifetime and capture guards. Undefined fill modes reject. Nonfinite ratios/percentages, animation overflow and unsupported integer pixel geometry fail explicitly. Indeterminate drawing can display unknown progress without inspecting the numeric value. Notification errors retain the required internal update/invalidation phase and aggregate callback failures.

Shared Range state remains executable. Exact scene factories store Range configuration before Value and restore all four progress settings. Phase, formatted strings and renderer state are transient. In-memory packed scenes retain their resource graph policy. Fresh `.e2dscene` archives restore authored Theme graphs and shared resources through [ADR 0083](../decisions/rendering.md#adr-0083). Project Theme startup applies without per-widget assignment.

## Verification

[ProgressBarTests](../../tests/Electron2D.Tests/ProgressBarTests.cs) verifies direction identities, RTL, fractional style rounding, font minimum, bounded/unbounded and exponential percentages, domain numerals, nonfinite/disposed/off-owner errors, theme mutation, shared values, hidden/pause behavior and edited-subtree preview. A fresh process restores and executes the saved exact node type.

Native GPU and Compatibility tests inspect every pixel of empty/quarter/half/full and RTL fill masks, preview clipping, text and outline. Sixty-four prepared active frames include value updates, indeterminate steps, recording and rendering with zero measured managed owner-thread bytes. Native allocation totals, semantic screen-reader delivery, foreign platform execution and owner acceptance remain unverified. See [ADR 0080](../decisions/rendering.md#adr-0080).

Native numeric accessibility follows [ADR 0114](../decisions/accessibility.md#adr-0114) and [AccessibilityServer numeric values](AccessibilityServer.md#numeric-values). Automatic output publishes the current value, bounds and semantic step. Native requests reuse the ordinary Range.Value policy; progress displays and non-editable inputs are read-only. Page supplies native page actions for editable producers. Text editing/selection is a separate protocol.
