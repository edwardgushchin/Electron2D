# Button

Last updated: 2026-09-27

**Inherits:** [BaseButton](BaseButton.md) · **Inherited By:** [CheckBox](CheckBox.md), [CheckButton](CheckButton.md)

**Declaration:** `public partial class Button : BaseButton` · **Source:** [Button.cs](../../src/Scene/GUI/Button.cs), [Button.Layout.cs](../../src/Scene/GUI/Button.Layout.cs) · **Component:** [GUI controls](../components/canvas-rendering.md)

## Description

A themed action control built on [BaseButton](BaseButton.md). It combines translated, shaped text with an optional borrowed icon. Its thirteen own properties control text, wrapping, overrun, alignment, icon fitting and flat decoration; inherited input, toggling, groups and shortcuts remain the BaseButton contract.

The selected state style supplies both decoration and content margins. RTL swaps horizontal text/icon placement and selects a mirrored style when one exists. With `align_to_largest_stylebox`, the maximum margins across state styles provide stable content placement. Flat mode suppresses the state background while retaining focus decoration and minimum-size margins. Focus colors override the normal state only.

Icons preserve their aspect ratio. `icon_max_width` limits both ordinary and expanded icons. Centered icon placement permits overlap with text; other horizontal placements reserve icon width plus nonnegative separation. Top/bottom icon placement also reserves vertical space. Expanded icons do not contribute their intrinsic size to the minimum. Empty intrinsic icon dimensions produce no expanded icon geometry.

Text draws as a paragraph: mandatory and selected wrapping boundaries, bidirectional shaping, language-sensitive segmentation, trimming and ellipsis are provided by [TextLayout](TextLayout.md). Drawing omits glyphs whose advance crosses the paragraph clip edge. `ClipText` removes natural text width from minimum size; it does not introduce a clip on descendants. The text outline precedes the main glyph pass. The current paragraph width determines wrapped height after layout/drawing.

Fonts and icons remain caller-owned. Equal property writes are silent, except assigning the same source text refreshes a changed translation. Attached reads and mutations require the scene owner; mutation during capture is rejected. Resource callbacks from another thread are deferred with membership-generation checks. Revision polling recovers changes hidden by an earlier throwing resource observer; querying size cannot consume pending retained redraw. Prepared layout/recording reuses storage.

Attached buttons retain renderer residency for their known icon, indicator-state and StyleBoxTexture dependencies, including nested atlas sources. Exact identity tracking detects a changed inner atlas even when an earlier callback prevents its change from reaching the outer atlas. Repeated references count once per button; separate controls retain independent counts. Detachment, replacement and disposal release retention without disposing borrowed resources. Detached configuration does not acquire a lease. Textures hidden inside arbitrary custom virtual drawing remain outside this automatic discovery.

## Example

```csharp
var control = new Button("Save");
control.Pressed += SaveSettings;
root.AddChild(control);
```

The surrounding application supplies `root` and the callback.

## API summary

| Signature | Contract |
| --- | --- |
| `public Button()` | [Button](#button): Creates an empty button with centered text and pointer input enabled. |
| `public Button(string text)` | [Button](#button-2): Creates a button with untranslated source text. |
| `public string Text { get; set; }` | [Text](#text): Gets or sets untranslated button text. |
| `public Texture? Icon { get; set; }` | [Icon](#icon): Gets or sets the borrowed icon override. |
| `public bool Flat { get; set; }` | [Flat](#flat): Gets or sets whether the state background is omitted. |
| `public HorizontalAlignment Alignment { get; set; }` | [Alignment](#alignment): Gets or sets text alignment along the horizontal axis. |
| `public TextAutowrapMode AutowrapMode { get; set; }` | [AutowrapMode](#autowrapmode): Gets or sets width-dependent wrapping. |
| `public TextLineBreakFlags AutowrapTrimFlags { get; set; }` | [AutowrapTrimFlags](#autowraptrimflags): Gets or sets trimming of spaces around line breaks. |
| `public bool ClipText { get; set; }` | [ClipText](#cliptext): Gets or sets whether text may shrink horizontally below its natural width. |
| `public bool ExpandIcon { get; set; }` | [ExpandIcon](#expandicon): Gets or sets whether the icon fits the available content rectangle. |
| `public HorizontalAlignment IconAlignment { get; set; }` | [IconAlignment](#iconalignment): Gets or sets horizontal icon placement. |
| `public VerticalAlignment VerticalIconAlignment { get; set; }` | [VerticalIconAlignment](#verticaliconalignment): Gets or sets vertical icon placement. |
| `public string Language { get; set; }` | [Language](#language): Gets or sets the shaping language. |
| `public TextDirection TextDirection { get; set; }` | [TextDirection](#textdirection): Gets or sets paragraph direction. |
| `public TextOverrunBehavior TextOverrunBehavior { get; set; }` | [TextOverrunBehavior](#textoverrunbehavior): Gets or sets character/word trimming and ellipsis behavior. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | [CreateSceneInstanceFactory](#createsceneinstancefactory): Overrides the inherited node/control contract. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | [GetPropertyDescriptors](#getpropertydescriptors): Overrides the inherited node/control contract. |
| `protected override Vector2 OnGetMinimumSize()` | [OnGetMinimumSize](#ongetminimumsize): Overrides the inherited node/control contract. |
| `protected override void OnNotification(int what)` | [OnNotification](#onnotification): Overrides the inherited node/control contract. |
| `protected override void Dispose(bool disposing)` | [Dispose](#dispose): Overrides the inherited node/control contract. |

## Member descriptions

<a id="button"></a>
### Button

`public Button()`

Creates an empty button with centered text and pointer input enabled.


<a id="button-2"></a>
### Button

`public Button(string text)`

Creates a button with untranslated source text.

- `text`: Initial text; may be empty.

Errors: `ArgumentNullException` — The text is null..

<a id="text"></a>
### Text

`public string Text { get; set; }`

Gets or sets untranslated button text.

Empty initially. Equal writes refresh a changed translation but otherwise remain silent.

Errors: `ArgumentNullException` — The text is null.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="icon"></a>
### Icon

`public Texture? Icon { get; set; }`

Gets or sets the borrowed icon override.

Null initially, using the optional theme icon. Equal identities are silent.

Errors: `ObjectDisposedException` — The button or assigned icon is disposed.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture..

<a id="flat"></a>
### Flat

`public bool Flat { get; set; }`

Gets or sets whether the state background is omitted.

False initially. Focus decoration and minimum-size margins remain active.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="alignment"></a>
### Alignment

`public HorizontalAlignment Alignment { get; set; }`

Gets or sets text alignment along the horizontal axis.

Center initially. Raw numeric values are retained; Fill uses the paragraph's justification behavior.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="autowrapmode"></a>
### AutowrapMode

`public TextAutowrapMode AutowrapMode { get; set; }`

Gets or sets width-dependent wrapping.

Off initially. Raw numeric values are retained.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="autowraptrimflags"></a>
### AutowrapTrimFlags

`public TextLineBreakFlags AutowrapTrimFlags { get; set; }`

Gets or sets trimming of spaces around line breaks.

TrimEndEdgeSpaces initially. Only TrimIndent, TrimStartEdgeSpaces and TrimEndEdgeSpaces are retained.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="cliptext"></a>
### ClipText

`public bool ClipText { get; set; }`

Gets or sets whether text may shrink horizontally below its natural width.

False initially. Drawing clips whole glyph advances to the available paragraph width.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="expandicon"></a>
### ExpandIcon

`public bool ExpandIcon { get; set; }`

Gets or sets whether the icon fits the available content rectangle.

False initially. Expanded icons do not contribute their intrinsic size to minimum size.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="iconalignment"></a>
### IconAlignment

`public HorizontalAlignment IconAlignment { get; set; }`

Gets or sets horizontal icon placement.

Left initially; left and right swap under RTL layout. Center permits text and icon to overlap.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="verticaliconalignment"></a>
### VerticalIconAlignment

`public VerticalAlignment VerticalIconAlignment { get; set; }`

Gets or sets vertical icon placement.

Center initially. Other values reserve icon height in addition to the text height.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="language"></a>
### Language

`public string Language { get; set; }`

Gets or sets the shaping language.

Empty initially; uses the translation-domain locale, current culture, then tool locale.

Errors: `ArgumentNullException` — The language is null.; `ArgumentException` — The language contains NUL.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="textdirection"></a>
### TextDirection

`public TextDirection TextDirection { get; set; }`

Gets or sets paragraph direction.

Auto initially. Inherited follows the control's layout direction; legacy negative one also selects Auto.

Errors: `ArgumentOutOfRangeException` — The direction is outside negative one through three.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="textoverrunbehavior"></a>
### TextOverrunBehavior

`public TextOverrunBehavior TextOverrunBehavior { get; set; }`

Gets or sets character/word trimming and ellipsis behavior.

NoTrimming initially. Unknown numeric values are retained without selecting an ellipsis policy.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during capture.; `ObjectDisposedException` — The button is disposed..

<a id="createsceneinstancefactory"></a>
### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()`

Implements the inherited [BaseButton](BaseButton.md), [Control](Control.md) and [Node](Node.md) contract for the behavior described above.


<a id="getpropertydescriptors"></a>
### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited [BaseButton](BaseButton.md), [Control](Control.md) and [Node](Node.md) contract for the behavior described above.


<a id="ongetminimumsize"></a>
### OnGetMinimumSize

`protected override Vector2 OnGetMinimumSize()`

Implements the inherited [BaseButton](BaseButton.md), [Control](Control.md) and [Node](Node.md) contract for the behavior described above.

Errors: `Exception` — A borrowed resource or text-layout operation fails..

<a id="onnotification"></a>
### OnNotification

`protected override void OnNotification(int what)`

Implements the inherited [BaseButton](BaseButton.md), [Control](Control.md) and [Node](Node.md) contract for the behavior described above.

Errors: `Exception` — A resource, layout, drawing or inherited notification callback fails..

<a id="dispose"></a>
### Dispose

`protected override void Dispose(bool disposing)`

Implements the inherited [BaseButton](BaseButton.md), [Control](Control.md) and [Node](Node.md) contract for the behavior described above.


## Theme and verification

State styles are `normal`, `pressed`, `hover`, `disabled`, optional `hover_pressed`, their optional `_mirrored` variants, and `focus`. Font/color/outline, icon modulation and sizing constants resolve through the ordinary typed theme owner. Built-in Button styles use four-unit margins; the `FlatButton` variation provides an empty normal/hover/disabled style with pressed feedback.

[ButtonTests](../../tests/Electron2D.Tests/ButtonTests.cs) passes defaults, typed scene storage, geometry, text clipping/wrapping, fill paragraph defaults, theme indicators, callback failures, nested atlas changes, balanced residency and 64 warmed active plus idle cycles at zero managed bytes. [ButtonRenderingTests](../../tests/Electron2D.Tests/ButtonRenderingTests.cs) passes ten native state/focus/pointer/keyboard/RTL/text phases on Linux Wayland GPU and compatibility, followed by 64 measured active frames after 64 warmup frames at zero managed bytes from ProcessFrameStarted through FramePostDraw. The final images match pixel-for-pixel. These checks do not imply native allocator counts, accessibility-service or other-platform support. See [coverage](../coverage/classes/Button.md).
