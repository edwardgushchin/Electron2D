# Label

Last updated: 2026-09-27

**Inherits:** [Control](Control.md) · **Source:** [Label.cs](../../src/Scene/GUI/Label.cs), [Label.Layout.cs](../../src/Scene/GUI/Label.Layout.cs), [Label.Storage.cs](../../src/Scene/GUI/Label.Storage.cs) · **Component:** [Text](../components/text.md)

## Description

A [Control](Control.md) that draws shaped Unicode text using inherited theme fonts or a borrowed [LabelSettings](LabelSettings.md). Text is translated through the node's automatic-translation domain before optional full Unicode uppercase conversion. Character positions count Unicode scalars, including whitespace; display casing may expand the number of shaped scalars. The escaped paragraph separator preserves empty and trailing paragraphs.

Labels perform grapheme/word wrapping, word and Arabic justification, bidirectional reordering, vertical/horizontal alignment, tab alignment and cluster-aware trimming. Structured URI, file, email and list contexts, or a typed custom parser, feed the actual shaping path. A list delimiter may contain multiple scalars; the parser preserves every field without producing out-of-range separator contexts. A nonempty tab sequence must have a finite positive total cycle; signed individual increments remain valid. `ClipText` clips both this label and canvas descendants and removes that clip when disabled.

The normal or focus style draws first. Each visible line then records ordinary shadow outline/shadow, reversed stacked shadows, reversed cumulative stacked outlines, the ordinary outline and main text. Settings and fonts remain borrowed. Attached changes require the scene owner thread; background resource changes are deferred with membership checks. Revision polling refreshes text even when an earlier throwing resource subscriber prevented delivery to the label. Queries use current detached resource content, while detached `GetLineCount()` deliberately reports one.

Prepared transformed text and paragraph strings use a bounded sixteen-entry cache; native shaping buffers, effect snapshots and glyph textures are reused after warm-up. Layout minimums include the normal style's margins, the font's minimum line height, selected/skipped lines and clipping/wrapping constraints. The node packs its exact concrete type, typed scalar/string arrays and borrowed/local resource references under the ordinary scene graph policy.

## Example

```csharp
using var settings = new LabelSettings { FontSize = 20, OutlineSize = 1 };
var label = new Label("Hello العربية")
{
    LabelSettings = settings,
    AutowrapMode = TextAutowrapMode.WordSmart,
    ClipText = true,
    Size = new Vector2(240, 80)
};
root.AddChild(label);
// Keep settings alive while the label uses it.
```

## API summary

| Signature | Contract |
| --- | --- |
| `public Label() : this(string.Empty)` | [Label](#label): Creates an empty label that ignores pointer input and centers within vertical container surplus. |
| `public Label(string text)` | [Label](#label): Creates a label with the supplied text and default themed appearance. |
| `public string Text { get; set; }` | [Text](#text): Gets or replaces the untranslated source text. |
| `public HorizontalAlignment HorizontalAlignment { get; set; }` | [HorizontalAlignment](#horizontalalignment): Gets or sets horizontal text alignment. |
| `public VerticalAlignment VerticalAlignment { get; set; }` | [VerticalAlignment](#verticalalignment): Gets or sets vertical alignment of visible lines. |
| `public TextAutowrapMode AutowrapMode { get; set; }` | [AutowrapMode](#autowrapmode): Gets or sets width-dependent line wrapping. |
| `public TextJustificationFlags JustificationFlags { get; set; }` | [JustificationFlags](#justificationflags): Gets or sets permitted fill-alignment operations. |
| `public TextOverrunBehavior TextOverrunBehavior { get; set; }` | [TextOverrunBehavior](#textoverrunbehavior): Gets or sets trimming and ellipsis behavior. |
| `public bool ClipText { get; set; }` | [ClipText](#cliptext): Gets or sets clipping of this label and its canvas descendants to the control rectangle. |
| `public bool Uppercase { get; set; }` | [Uppercase](#uppercase): Gets or sets locale-aware uppercase display without replacing Text. |
| `public TextDirection TextDirection { get; set; }` | [TextDirection](#textdirection): Gets or sets the paragraph writing direction. |
| `public StructuredTextParser StructuredTextBIDIOverride { get; set; }` | [StructuredTextBIDIOverride](#structuredtextbidioverride): Gets or sets independent bidi contexts for structured text. |
| `public int LinesSkipped { get; set; }` | [LinesSkipped](#linesskipped): Gets or sets the number of initial shaped lines omitted from display. |
| `public int MaxLinesVisible { get; set; }` | [MaxLinesVisible](#maxlinesvisible): Gets or sets the maximum number of visible lines. |
| `public TextVisibleCharactersBehavior VisibleCharactersBehavior { get; set; }` | [VisibleCharactersBehavior](#visiblecharactersbehavior): Gets or sets whether visibility limits characters before or after shaping, or visual glyphs. |
| `public TextLineBreakFlags AutowrapTrimFlags { get; set; }` | [AutowrapTrimFlags](#autowraptrimflags): Gets or sets the line-edge whitespace trimming flags. |
| `public LabelSettings? LabelSettings { get; set; }` | [LabelSettings](#labelsettings): Gets or sets an optional borrowed font and effects resource. |
| `public string Language { get; set; }` | [Language](#language): Gets or sets the language used for shaping and casing. |
| `public string ParagraphSeparator { get; set; }` | [ParagraphSeparator](#paragraphseparator): Gets or sets the escaped separator used to split paragraphs. |
| `public string EllipsisChar { get; set; }` | [EllipsisChar](#ellipsischar): Gets or sets the ellipsis scalar. |
| `public string[] StructuredTextBIDIOverrideOptions { get; set; }` | [StructuredTextBIDIOverrideOptions](#structuredtextbidioverrideoptions): Gets or replaces the typed custom/list-parser options. |
| `public float[] TabStops { get; set; }` | [TabStops](#tabstops): Gets or replaces repeating tab-stop increments in logical pixels. |
| `public int VisibleCharacters { get; set; }` | [VisibleCharacters](#visiblecharacters): Gets or sets the stored visible-character limit. |
| `public float VisibleRatio { get; set; }` | [VisibleRatio](#visibleratio): Gets or sets the visible fraction of characters or glyphs. |
| `public int GetTotalCharacterCount()` | [GetTotalCharacterCount](#gettotalcharactercount): Returns the number of logical Unicode scalars in the translated text, including whitespace. |
| `public int GetLineCount()` | [GetLineCount](#getlinecount): Returns the shaped line count. |
| `public int GetLineHeight(int line = -1)` | [GetLineHeight](#getlineheight): Returns the height of one line, or the maximum line height when the index is outside the line range. |
| `public int GetVisibleLineCount()` | [GetVisibleLineCount](#getvisiblelinecount): Returns the number of complete lines fitting in the control after skipping and line limits. |
| `public Rect2 GetCharacterBounds(int position)` | [GetCharacterBounds](#getcharacterbounds): Returns the local rectangle occupied by a logical character's shaped cluster. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | [CreateSceneInstanceFactory](#createsceneinstancefactory): Supplies the exact Label scene factory. |
| `protected override void Dispose(bool disposing)` | [Dispose](#dispose): Detaches borrowed text-resource subscriptions during node disposal. |
| `protected override Vector2 OnGetMinimumSize()` | [OnGetMinimumSize](#ongetminimumsize): Measures the visible text and themed style margins. |
| `protected override void OnNotification(int what)` | [OnNotification](#onnotification): Integrates drawing, layout, translation and text-resource refresh with Control notifications. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | [GetPropertyDescriptors](#getpropertydescriptors): Adds typed stored Label state and inherited default overrides. |

## Member descriptions

<a id="label"></a>
### Label

`public Label() : this(string.Empty)`

Creates an empty label that ignores pointer input and centers within vertical container surplus.


<a id="label"></a>
### Label

`public Label(string text)`

Creates a label with the supplied text and default themed appearance.

`text`: Initial text; may be empty.

Errors: `ArgumentNullException` — Text is null.

<a id="text"></a>
### Text

`public string Text { get; set; }`

Gets or replaces the untranslated source text.

Empty initially; equal writes are silent.

Errors: `ArgumentNullException` — The text is null.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="horizontalalignment"></a>
### HorizontalAlignment

`public HorizontalAlignment HorizontalAlignment { get; set; }`

Gets or sets horizontal text alignment.

Left initially.

Errors: `ArgumentOutOfRangeException` — The value is outside its supported range.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="verticalalignment"></a>
### VerticalAlignment

`public VerticalAlignment VerticalAlignment { get; set; }`

Gets or sets vertical alignment of visible lines.

Top initially.

Errors: `ArgumentOutOfRangeException` — The value is outside its supported range.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="autowrapmode"></a>
### AutowrapMode

`public TextAutowrapMode AutowrapMode { get; set; }`

Gets or sets width-dependent line wrapping.

Off initially. Unknown numeric values are retained.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="justificationflags"></a>
### JustificationFlags

`public TextJustificationFlags JustificationFlags { get; set; }`

Gets or sets permitted fill-alignment operations.

Kashida, word expansion, skip-last-line and do-not-skip-single-line initially.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="textoverrunbehavior"></a>
### TextOverrunBehavior

`public TextOverrunBehavior TextOverrunBehavior { get; set; }`

Gets or sets trimming and ellipsis behavior.

NoTrimming initially. Unknown numeric values are retained.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="cliptext"></a>
### ClipText

`public bool ClipText { get; set; }`

Gets or sets clipping of this label and its canvas descendants to the control rectangle.

False initially.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="uppercase"></a>
### Uppercase

`public bool Uppercase { get; set; }`

Gets or sets locale-aware uppercase display without replacing Text.

False initially.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="textdirection"></a>
### TextDirection

`public TextDirection TextDirection { get; set; }`

Gets or sets the paragraph writing direction.

Auto initially.

Errors: `ArgumentOutOfRangeException` — The value is outside its supported range.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="structuredtextbidioverride"></a>
### StructuredTextBIDIOverride

`public StructuredTextParser StructuredTextBIDIOverride { get; set; }`

Gets or sets independent bidi contexts for structured text.

Default initially. Unknown numeric values are retained.

Errors: `NotSupportedException` — The excluded language-specific parser is selected.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="linesskipped"></a>
### LinesSkipped

`public int LinesSkipped { get; set; }`

Gets or sets the number of initial shaped lines omitted from display.

Zero initially; must be nonnegative.

Errors: `ArgumentOutOfRangeException` — The value is outside its supported range.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="maxlinesvisible"></a>
### MaxLinesVisible

`public int MaxLinesVisible { get; set; }`

Gets or sets the maximum number of visible lines.

Minus one initially. A negative value means unlimited; zero displays no lines.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="visiblecharactersbehavior"></a>
### VisibleCharactersBehavior

`public TextVisibleCharactersBehavior VisibleCharactersBehavior { get; set; }`

Gets or sets whether visibility limits characters before or after shaping, or visual glyphs.

CharsBeforeShaping initially. Unknown numeric values are retained.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="autowraptrimflags"></a>
### AutowrapTrimFlags

`public TextLineBreakFlags AutowrapTrimFlags { get; set; }`

Gets or sets the line-edge whitespace trimming flags.

Start and end trimming initially. Only TrimIndent, TrimStartEdgeSpaces and TrimEndEdgeSpaces are retained.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="labelsettings"></a>
### LabelSettings

`public LabelSettings? LabelSettings { get; set; }`

Gets or sets an optional borrowed font and effects resource.

Null initially; equal identities are silent.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="language"></a>
### Language

`public string Language { get; set; }`

Gets or sets the language used for shaping and casing.

Empty initially. An empty value uses the translation domain's locale override, then the current culture, then the tool locale.

Errors: `ArgumentException` — The language is null or contains NUL.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="paragraphseparator"></a>
### ParagraphSeparator

`public string ParagraphSeparator { get; set; }`

Gets or sets the escaped separator used to split paragraphs.

The two-character escape \n initially; empty keeps one paragraph.

Errors: `ArgumentNullException` — The separator is null.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="ellipsischar"></a>
### EllipsisChar

`public string EllipsisChar { get; set; }`

Gets or sets the ellipsis scalar.

An ellipsis initially. Only the first scalar is retained; empty selects the default ellipsis.

Errors: `ArgumentNullException` — The supplied string is null.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="structuredtextbidioverrideoptions"></a>
### StructuredTextBIDIOverrideOptions

`public string[] StructuredTextBIDIOverrideOptions { get; set; }`

Gets or replaces the typed custom/list-parser options.

An independent empty array initially; List uses exactly one delimiter string.

Errors: `ArgumentNullException` — The array or an option is null.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="tabstops"></a>
### TabStops

`public float[] TabStops { get; set; }`

Gets or replaces repeating tab-stop increments in logical pixels.

An independent empty array initially.

Errors: `ArgumentException` — The array is null, an increment is nonfinite, or its complete cycle is not positive.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="visiblecharacters"></a>
### VisibleCharacters

`public int VisibleCharacters { get; set; }`

Gets or sets the stored visible-character limit.

Minus one initially. Negative limits display all text; the stored ratio follows the supplied count.

Errors: `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="visibleratio"></a>
### VisibleRatio

`public float VisibleRatio { get; set; }`

Gets or sets the visible fraction of characters or glyphs.

One initially. Assignments clamp to zero through one and update VisibleCharacters.

Errors: `ArgumentOutOfRangeException` — The ratio is not finite.; `InvalidOperationException` — An attached mutation is off its owner thread or occurs during scene capture.; `ObjectDisposedException` — The label is disposed.

<a id="gettotalcharactercount"></a>
### GetTotalCharacterCount

`public int GetTotalCharacterCount()`

Returns the number of logical Unicode scalars in the translated text, including whitespace.

The complete source character count.

Errors: `InvalidOperationException` — An attached query is off the owner thread.; `ObjectDisposedException` — The label is disposed.

<a id="getlinecount"></a>
### GetLineCount

`public int GetLineCount()`

Returns the shaped line count.

One while detached; the complete line count while in a tree.

Errors: `InvalidOperationException` — An attached query is off the owner thread or a required theme font is unavailable.; `ObjectDisposedException` — The label or a borrowed resource is disposed.

<a id="getlineheight"></a>
### GetLineHeight

`public int GetLineHeight(int line = -1)`

Returns the height of one line, or the maximum line height when the index is outside the line range.

The pixel height, truncated to an integer.

`line`: Line index, or minus one for the maximum.

Errors: `InvalidOperationException` — An attached query is off the owner thread or a required theme font is unavailable.; `ObjectDisposedException` — The label or a borrowed resource is disposed.

<a id="getvisiblelinecount"></a>
### GetVisibleLineCount

`public int GetVisibleLineCount()`

Returns the number of complete lines fitting in the control after skipping and line limits.

The visible line count.

Errors: `InvalidOperationException` — An attached query is off the owner thread or a required theme font is unavailable.; `ObjectDisposedException` — The label or a borrowed resource is disposed.

<a id="getcharacterbounds"></a>
### GetCharacterBounds

`public Rect2 GetCharacterBounds(int position)`

Returns the local rectangle occupied by a logical character's shaped cluster.

The cluster rectangle, or an empty rectangle outside the laid-out visible lines.

`position`: The zero-based Unicode scalar index.

Errors: `InvalidOperationException` — An attached query is off the owner thread or a required theme font is unavailable.; `ObjectDisposedException` — The label or a borrowed resource is disposed.

<a id="createsceneinstancefactory"></a>
### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()`

Uses the inherited [Control layout](Control.md) and [Node lifecycle](Node.md) contracts.

<a id="dispose"></a>
### Dispose

`protected override void Dispose(bool disposing)`

Uses the inherited [Control layout](Control.md) and [Node lifecycle](Node.md) contracts.

<a id="ongetminimumsize"></a>
### OnGetMinimumSize

`protected override Vector2 OnGetMinimumSize()`

Errors: `InvalidOperationException` — A required font or style is missing, or shaping fails.; `ObjectDisposedException` — A borrowed settings, font or style resource is disposed.
Uses the inherited [Control layout](Control.md) and [Node lifecycle](Node.md) contracts.

<a id="onnotification"></a>
### OnNotification

`protected override void OnNotification(int what)`

Errors: `Exception` — Resource, theme, shaping, drawing or inherited notification callbacks fail.
Uses the inherited [Control layout](Control.md) and [Node lifecycle](Node.md) contracts.

<a id="getpropertydescriptors"></a>
### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Uses the inherited [Control layout](Control.md) and [Node lifecycle](Node.md) contracts.

## Verification and limits

[LabelTests](../../tests/Electron2D.Tests/LabelTests.cs) covers defaults, typed packing, scalar bounds, paragraphs, grapheme wrapping, source alignment rounding, visibility, custom structured contexts, effect order, resource ownership, background/missed callbacks and active/idle allocation checks. [FontTests](../../tests/Electron2D.Tests/FontTests.cs) verifies the shared shaping and glyph-recording paths. Native renderer and platform evidence is recorded separately by the text slice; a managed geometry test does not establish pixel or platform acceptance. Text/locale changes outside prepared cache entries, first shaping/raster work, public array snapshots and custom parser code may allocate. Public low-level TextServer handles and script-language-specific structured parsing remain outside this typed consumer.
