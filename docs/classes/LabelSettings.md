# LabelSettings

Last updated: 2026-09-27

**Inherits:** [Resource](Resource.md) · **Source:** [LabelSettings.cs](../../src/Scene/Resources/LabelSettings.cs) · **Component:** [Text](../components/text.md)

## Description

Shared label text styling with a borrowed optional [Font](Font.md), a font size/color, line and paragraph spacing, ordinary outline/shadow settings and ordered additional outline/shadow layers. Assigning an equal scalar value or equal borrowed font is silent. Font changes/disposal invalidate the settings; replacing or disposing settings detaches subscriptions without disposing the font. Structural layer edits notify the property list and Changed after committing; callback failures do not undo the mutation. Indexed layers retain ordering and use typed values, never a dynamic property map. Resource duplication uses the normal shallow/deep alias policy.

## Example

```csharp
using var font = ResourceLoader.Load<FontFile>("res://fonts/interface.woff2");
using var settings = new LabelSettings { Font = font, FontSize = 20, FontColor = Colors.White };
settings.AddStackedOutline();
settings.SetStackedOutlineSize(0, 4);
```

## API summary

| Signature | Contract |
| --- | --- |
| `public LabelSettings()` | [LabelSettings](#labelsettings): Creates default sixteen-unit white text settings with no stacked effects. |
| `public float LineSpacing { get; set; }` | [LineSpacing](#linespacing): Gets or sets the finite signed gap added between lines. |
| `public float ParagraphSpacing { get; set; }` | [ParagraphSpacing](#paragraphspacing): Gets or sets the finite signed additional paragraph gap. |
| `public Font? Font { get; set; }` | [Font](#font): Gets or sets the borrowed font override. |
| `public int FontSize { get; set; }` | [FontSize](#fontsize): Gets or sets the stored signed font size. |
| `public Color FontColor { get; set; }` | [FontColor](#fontcolor): Gets or sets the finite text color. |
| `public int OutlineSize { get; set; }` | [OutlineSize](#outlinesize): Gets or sets the stored signed primary outline size. |
| `public Color OutlineColor { get; set; }` | [OutlineColor](#outlinecolor): Gets or sets the finite primary outline color. |
| `public int ShadowSize { get; set; }` | [ShadowSize](#shadowsize): Gets or sets the stored signed primary shadow outline size. |
| `public Color ShadowColor { get; set; }` | [ShadowColor](#shadowcolor): Gets or sets the finite primary shadow color. |
| `public Vector2 ShadowOffset { get; set; }` | [ShadowOffset](#shadowoffset): Gets or sets the finite signed primary shadow offset. |
| `public int StackedOutlineCount { get; set; }` | [StackedOutlineCount](#stackedoutlinecount): Gets or sets the number of stacked outlines, initializing new layers to size zero and opaque black. |
| `public int StackedShadowCount { get; set; }` | [StackedShadowCount](#stackedshadowcount): Gets or sets the number of stacked shadows, initializing new layers to offset (1,1), opaque black and outline size zero. |
| `public void AddStackedOutline(int index = -1)` | [AddStackedOutline](#addstackedoutline): Inserts a default outline layer and emits structural notifications. |
| `public void AddStackedShadow(int index = -1)` | [AddStackedShadow](#addstackedshadow): Inserts a default shadow layer and emits structural notifications. |
| `public void MoveStackedOutline(int fromIndex, int toPosition)` | [MoveStackedOutline](#movestackedoutline): Moves an outline to an insertion position and notifies even when the resulting order is unchanged. |
| `public void MoveStackedShadow(int fromIndex, int toPosition)` | [MoveStackedShadow](#movestackedshadow): Moves a shadow to an insertion position and notifies even when the resulting order is unchanged. |
| `public void RemoveStackedOutline(int index)` | [RemoveStackedOutline](#removestackedoutline): Removes an outline layer and emits structural notifications. |
| `public void RemoveStackedShadow(int index)` | [RemoveStackedShadow](#removestackedshadow): Removes a shadow layer and emits structural notifications. |
| `public int GetStackedOutlineSize(int index)` | [GetStackedOutlineSize](#getstackedoutlinesize): Gets a stacked outline's signed size. |
| `public void SetStackedOutlineSize(int index, int size)` | [SetStackedOutlineSize](#setstackedoutlinesize): Sets a stacked outline's signed size, suppressing equal writes. |
| `public Color GetStackedOutlineColor(int index)` | [GetStackedOutlineColor](#getstackedoutlinecolor): Gets a stacked outline's color. |
| `public void SetStackedOutlineColor(int index, Color color)` | [SetStackedOutlineColor](#setstackedoutlinecolor): Sets a stacked outline's finite color, suppressing equal writes. |
| `public Vector2 GetStackedShadowOffset(int index)` | [GetStackedShadowOffset](#getstackedshadowoffset): Gets a stacked shadow's finite offset. |
| `public void SetStackedShadowOffset(int index, Vector2 offset)` | [SetStackedShadowOffset](#setstackedshadowoffset): Sets a stacked shadow's finite offset, suppressing equal writes. |
| `public Color GetStackedShadowColor(int index)` | [GetStackedShadowColor](#getstackedshadowcolor): Gets a stacked shadow's color. |
| `public void SetStackedShadowColor(int index, Color color)` | [SetStackedShadowColor](#setstackedshadowcolor): Sets a stacked shadow's finite color, suppressing equal writes. |
| `public int GetStackedShadowOutlineSize(int index)` | [GetStackedShadowOutlineSize](#getstackedshadowoutlinesize): Gets a stacked shadow's signed outline size. |
| `public void SetStackedShadowOutlineSize(int index, int size)` | [SetStackedShadowOutlineSize](#setstackedshadowoutlinesize): Sets a stacked shadow's signed outline size, suppressing equal writes. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | [GetPropertyDescriptors](#getpropertydescriptors): Inherited resource graph hook. |
| `protected override Resource CreateDuplicateInstance()` | [CreateDuplicateInstance](#createduplicateinstance): Inherited resource graph hook. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | [CopyCustomStateTo](#copycustomstateto): Inherited resource graph hook. |
| `protected override void Dispose(bool disposing)` | [Dispose](#dispose): Inherited resource graph hook. |

## Member descriptions

<a id="labelsettings"></a>
### LabelSettings

`public LabelSettings()`

Creates default sixteen-unit white text settings with no stacked effects.


<a id="linespacing"></a>
### LineSpacing

`public float LineSpacing { get; set; }`

Gets or sets the finite signed gap added between lines.

Three initially; equal writes are silent.

Errors: `ArgumentOutOfRangeException` — The spacing is nonfinite.; `ObjectDisposedException` — The settings are disposed.

<a id="paragraphspacing"></a>
### ParagraphSpacing

`public float ParagraphSpacing { get; set; }`

Gets or sets the finite signed additional paragraph gap.

Zero initially; equal writes are silent.

Errors: `ArgumentOutOfRangeException` — The spacing is nonfinite.; `ObjectDisposedException` — The settings are disposed.

<a id="font"></a>
### Font

`public Font? Font { get; set; }`

Gets or sets the borrowed font override.

Null initially. Equal identities are silent; replacing the font updates subscriptions before Changed.

Errors: `ObjectDisposedException` — These settings or the assigned font are disposed.

<a id="fontsize"></a>
### FontSize

`public int FontSize { get; set; }`

Gets or sets the stored signed font size.

Sixteen initially; equal writes are silent.

Errors: `ObjectDisposedException` — The settings are disposed.

<a id="fontcolor"></a>
### FontColor

`public Color FontColor { get; set; }`

Gets or sets the finite text color.

Opaque white initially.

Errors: `ArgumentException` — A color component is nonfinite.; `ObjectDisposedException` — The settings are disposed.

<a id="outlinesize"></a>
### OutlineSize

`public int OutlineSize { get; set; }`

Gets or sets the stored signed primary outline size.

Zero initially.

Errors: `ObjectDisposedException` — The settings are disposed.

<a id="outlinecolor"></a>
### OutlineColor

`public Color OutlineColor { get; set; }`

Gets or sets the finite primary outline color.

Opaque white initially.

Errors: `ArgumentException` — A color component is nonfinite.; `ObjectDisposedException` — The settings are disposed.

<a id="shadowsize"></a>
### ShadowSize

`public int ShadowSize { get; set; }`

Gets or sets the stored signed primary shadow outline size.

One initially.

Errors: `ObjectDisposedException` — The settings are disposed.

<a id="shadowcolor"></a>
### ShadowColor

`public Color ShadowColor { get; set; }`

Gets or sets the finite primary shadow color.

Transparent black initially.

Errors: `ArgumentException` — A color component is nonfinite.; `ObjectDisposedException` — The settings are disposed.

<a id="shadowoffset"></a>
### ShadowOffset

`public Vector2 ShadowOffset { get; set; }`

Gets or sets the finite signed primary shadow offset.

One unit on each axis initially.

Errors: `ArgumentException` — An offset component is nonfinite.; `ObjectDisposedException` — The settings are disposed.

<a id="stackedoutlinecount"></a>
### StackedOutlineCount

`public int StackedOutlineCount { get; set; }`

Gets or sets the number of stacked outlines, initializing new layers to size zero and opaque black.

Zero initially; equal counts are silent. Shrinking discards removed layers.

Errors: `ArgumentOutOfRangeException` — The count is negative.; `ObjectDisposedException` — The settings are disposed.

<a id="stackedshadowcount"></a>
### StackedShadowCount

`public int StackedShadowCount { get; set; }`

Gets or sets the number of stacked shadows, initializing new layers to offset (1,1), opaque black and outline size zero.

Zero initially; equal counts are silent. Shrinking discards removed layers.

Errors: `ArgumentOutOfRangeException` — The count is negative.; `ObjectDisposedException` — The settings are disposed.

<a id="addstackedoutline"></a>
### AddStackedOutline

`public void AddStackedOutline(int index = -1)`

Inserts a default outline layer and emits structural notifications.

`index`: An insertion position from zero through Count; any negative value appends.

Errors: `ArgumentOutOfRangeException` — The insertion position exceeds Count.; `ObjectDisposedException` — The settings are disposed.

<a id="addstackedshadow"></a>
### AddStackedShadow

`public void AddStackedShadow(int index = -1)`

Inserts a default shadow layer and emits structural notifications.

`index`: An insertion position from zero through Count; any negative value appends.

Errors: `ArgumentOutOfRangeException` — The insertion position exceeds Count.; `ObjectDisposedException` — The settings are disposed.

<a id="movestackedoutline"></a>
### MoveStackedOutline

`public void MoveStackedOutline(int fromIndex, int toPosition)`

Moves an outline to an insertion position and notifies even when the resulting order is unchanged.

`fromIndex`: The existing layer index.; `toPosition`: An insertion position from zero through Count, measured before removal.

Errors: `ArgumentOutOfRangeException` — Either index is outside its allowed range.; `ObjectDisposedException` — The settings are disposed.

<a id="movestackedshadow"></a>
### MoveStackedShadow

`public void MoveStackedShadow(int fromIndex, int toPosition)`

Moves a shadow to an insertion position and notifies even when the resulting order is unchanged.

`fromIndex`: The existing layer index.; `toPosition`: An insertion position from zero through Count, measured before removal.

Errors: `ArgumentOutOfRangeException` — Either index is outside its allowed range.; `ObjectDisposedException` — The settings are disposed.

<a id="removestackedoutline"></a>
### RemoveStackedOutline

`public void RemoveStackedOutline(int index)`

Removes an outline layer and emits structural notifications.

`index`: The existing layer index.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ObjectDisposedException` — The settings are disposed.

<a id="removestackedshadow"></a>
### RemoveStackedShadow

`public void RemoveStackedShadow(int index)`

Removes a shadow layer and emits structural notifications.

`index`: The existing layer index.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ObjectDisposedException` — The settings are disposed.

<a id="getstackedoutlinesize"></a>
### GetStackedOutlineSize

`public int GetStackedOutlineSize(int index)`

Gets a stacked outline's signed size.

The stored size.

`index`: The existing layer index.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ObjectDisposedException` — The settings are disposed.

<a id="setstackedoutlinesize"></a>
### SetStackedOutlineSize

`public void SetStackedOutlineSize(int index, int size)`

Sets a stacked outline's signed size, suppressing equal writes.

`index`: The existing layer index.; `size`: The stored signed size.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ObjectDisposedException` — The settings are disposed.

<a id="getstackedoutlinecolor"></a>
### GetStackedOutlineColor

`public Color GetStackedOutlineColor(int index)`

Gets a stacked outline's color.

The stored color.

`index`: The existing layer index.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ObjectDisposedException` — The settings are disposed.

<a id="setstackedoutlinecolor"></a>
### SetStackedOutlineColor

`public void SetStackedOutlineColor(int index, Color color)`

Sets a stacked outline's finite color, suppressing equal writes.

`index`: The existing layer index.; `color`: The finite color.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ArgumentException` — The color is nonfinite.; `ObjectDisposedException` — The settings are disposed.

<a id="getstackedshadowoffset"></a>
### GetStackedShadowOffset

`public Vector2 GetStackedShadowOffset(int index)`

Gets a stacked shadow's finite offset.

The stored offset.

`index`: The existing layer index.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ObjectDisposedException` — The settings are disposed.

<a id="setstackedshadowoffset"></a>
### SetStackedShadowOffset

`public void SetStackedShadowOffset(int index, Vector2 offset)`

Sets a stacked shadow's finite offset, suppressing equal writes.

`index`: The existing layer index.; `offset`: The finite signed offset.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ArgumentException` — The offset is nonfinite.; `ObjectDisposedException` — The settings are disposed.

<a id="getstackedshadowcolor"></a>
### GetStackedShadowColor

`public Color GetStackedShadowColor(int index)`

Gets a stacked shadow's color.

The stored color.

`index`: The existing layer index.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ObjectDisposedException` — The settings are disposed.

<a id="setstackedshadowcolor"></a>
### SetStackedShadowColor

`public void SetStackedShadowColor(int index, Color color)`

Sets a stacked shadow's finite color, suppressing equal writes.

`index`: The existing layer index.; `color`: The finite color.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ArgumentException` — The color is nonfinite.; `ObjectDisposedException` — The settings are disposed.

<a id="getstackedshadowoutlinesize"></a>
### GetStackedShadowOutlineSize

`public int GetStackedShadowOutlineSize(int index)`

Gets a stacked shadow's signed outline size.

The stored signed size.

`index`: The existing layer index.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ObjectDisposedException` — The settings are disposed.

<a id="setstackedshadowoutlinesize"></a>
### SetStackedShadowOutlineSize

`public void SetStackedShadowOutlineSize(int index, int size)`

Sets a stacked shadow's signed outline size, suppressing equal writes.

`index`: The existing layer index.; `size`: The signed size.

Errors: `ArgumentOutOfRangeException` — The layer does not exist.; `ObjectDisposedException` — The settings are disposed.

<a id="getpropertydescriptors"></a>
### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Uses the inherited [Resource graph/lifecycle contract](Resource.md) for the concrete resource state described above.

<a id="createduplicateinstance"></a>
### CreateDuplicateInstance

`protected override Resource CreateDuplicateInstance()`

Uses the inherited [Resource graph/lifecycle contract](Resource.md) for the concrete resource state described above.

<a id="copycustomstateto"></a>
### CopyCustomStateTo

`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`

Uses the inherited [Resource graph/lifecycle contract](Resource.md) for the concrete resource state described above.

<a id="dispose"></a>
### Dispose

`protected override void Dispose(bool disposing)`

Uses the inherited [Resource graph/lifecycle contract](Resource.md) for the concrete resource state described above.

## Verification and limits

[LabelSettingsTests](../../tests/Electron2D.Tests/LabelSettingsTests.cs) verifies defaults, scalar notifications, finite guards, layer insertion/removal/movement, bounds failures, structural callback behavior, typed storage, alias ownership, duplication and warm allocation behavior. Actual Label layout and native effects are verified with the consumer's tests; a resource test alone does not establish native rendering. See [coverage](../coverage/classes/LabelSettings.md) and [ADR 0046](../decisions/rendering.md#adr-0046).
