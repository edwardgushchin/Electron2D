# RichTextLabel

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.RichTextLabel`. **Source:** [source](../../src/Scene/GUI/RichTextLabel.cs). **Component:** [Rich text](../components/rich-text.md).

## Description

Displays a mutable document of shaped styled paragraphs, inline images, horizontal rules, drop caps, lists, tables, links and glyph effects. Text retains untranslated source; BBCodeEnabled governs Text assignments while AppendText always parses its own markup. Manual pushes retain runtime content and do not rewrite Text. Assigning Text replaces manual content. GetParsedText includes drop caps and structural tab markers; total/scalar selection excludes those markers and drop caps, counts newlines and one scalar per image. Root line queries treat a table as one inline object; cell line geometry contributes to table height. Source and stored configuration are independent of runtime selection, frame cache and manual payloads.

## Members

| Declaration | Contract |
| --- | --- |
| [`public RichTextLabel()`](#member-cd5272b2f8d1) | Creates an empty clipped rich-text control with owned scrolling and context-menu children. |
| [`public RichTextLabel(System.String text)`](#member-091ce13e57a5) | Creates plain initial text with the default rich-text policies. |
| [`public event System.Action Finished`](#member-c987a360ddba) | Reports that the current document has completed layout. |
| [`public event System.Action<Electron2D.RichTextMetadata> MetaClicked`](#member-8c71d73ac81b) | Reports the borrowed typed metadata of a clicked span. |
| [`public event System.Action<Electron2D.RichTextMetadata> MetaHoverEnded`](#member-830dc7440637) | Reports departure from a metadata span. |
| [`public event System.Action<Electron2D.RichTextMetadata> MetaHoverStarted`](#member-cc88c3e2a884) | Reports entry into a metadata span. |
| [`public System.Void AddHR(System.Int32 width = 90, System.Int32 height = 2, System.Nullable<Electron2D.Color> color = null, Electron2D.HorizontalAlignment alignment = Center, System.Boolean widthInPercent = true, System.Boolean heightInPercent = false)`](#member-01c8c91ec0cf) | Adds a horizontal rule. |
| [`public System.Void AddImage(Electron2D.Texture image, System.Single width = 0f, System.Single height = 0f, System.Nullable<Electron2D.Color> color = null, Electron2D.InlineAlignment inlineAlign = Center, Electron2D.Rect2 region = default, System.Boolean pad = false, System.String tooltip = "", Electron2D.RichTextLabel.ImageUnit widthUnit = Pixel, Electron2D.RichTextLabel.ImageUnit heightUnit = Pixel, System.String altText = "")`](#member-d011bfb0b561) | Adds an unkeyed borrowed inline image. |
| [`public System.Void AddImage<T>(T key, Electron2D.Texture image, System.Single width = 0f, System.Single height = 0f, System.Nullable<Electron2D.Color> color = null, Electron2D.InlineAlignment inlineAlign = Center, Electron2D.Rect2 region = default, System.Boolean pad = false, System.String tooltip = "", Electron2D.RichTextLabel.ImageUnit widthUnit = Pixel, Electron2D.RichTextLabel.ImageUnit heightUnit = Pixel, System.String altText = "")`](#member-92ec0345179b) | Adds an image with an exact typed update key. |
| [`public System.Void AddText(System.String text)`](#member-996fd6a36f0d) | Adds raw text, splitting LF/CRLF into independently shaped paragraphs. |
| [`public System.Void AppendText(System.String bbcode)`](#member-2315b89cbd7a) | Appends markup; closing tags may only close tags opened in this append call. |
| [`public System.Void Clear()`](#member-4109aac59122) | Clears the runtime stack and document without replacing the source Text property. |
| [`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`](#member-3885f453f3b4) | Inherited typed lifecycle contract. |
| [`public System.Void Deselect()`](#member-3444ceb07c7a) | Clears selection. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-a1a555c1d268) | Inherited typed lifecycle contract. |
| [`public System.Int32 GetCharacterLine(System.Int32 character)`](#member-1172adefca69) | Gets the loaded line containing a scalar position. |
| [`public System.Int32 GetCharacterParagraph(System.Int32 character)`](#member-09f29d5a1e1f) | Gets the root paragraph containing a scalar position. |
| [`public System.Int32 GetContentHeight()`](#member-ab8a96b3d251) | Gets total loaded content height. |
| [`public System.Int32 GetContentWidth()`](#member-e914da6fb217) | Gets total loaded content width. |
| [`public System.Int32 GetLineCount()`](#member-33338465546c) | Gets the number of shaped lines in root paragraphs. |
| [`public System.Int32 GetLineHeight(System.Int32 line)`](#member-a6735bd4c2d5) | Gets one loaded line's pixel height. |
| [`public System.Single GetLineOffset(System.Int32 line)`](#member-8e0b649f6e1b) | Gets a loaded line's document Y offset. |
| [`public Electron2D.Vector2i GetLineRange(System.Int32 line)`](#member-bf6601ef7589) | Gets a loaded line's absolute scalar interval. |
| [`public System.Int32 GetLineWidth(System.Int32 line)`](#member-780b88fe92ef) | Gets one loaded line's width. |
| [`public Electron2D.PopupMenu GetMenu()`](#member-4e44d47dd33e) | Gets the borrowed required context menu. |
| [`public System.Int32 GetParagraphCount()`](#member-3d638cdc224e) | Gets root paragraph count. |
| [`public System.Single GetParagraphOffset(System.Int32 paragraph)`](#member-14d40cf013b3) | Gets one root paragraph's Y offset. |
| [`public System.String GetParsedText()`](#member-dfd71be8f516) | Gets raw parsed text, including drop caps, image spaces and indentation markers. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-f033e8dc1c8b) | Inherited typed lifecycle contract. |
| [`public System.String GetSelectedText()`](#member-95e961284feb) | Gets the selected plain text. |
| [`public System.Int32 GetSelectionFrom()`](#member-6cee10847ef4) | Gets the selected absolute scalar start. |
| [`public System.Single GetSelectionLineOffset()`](#member-f7a359690639) | Gets the selected line's Y offset. |
| [`public System.Int32 GetSelectionTo()`](#member-26aab5dc9b98) | Gets the selected exclusive scalar end. |
| [`public System.Int32 GetTotalCharacterCount()`](#member-b8f482626a74) | Gets scalar document count excluding drop caps and structural tags. |
| [`public Electron2D.VScrollBar GetVScrollBar()`](#member-b13b00abbbf2) | Gets the borrowed required vertical scrollbar. |
| [`public Electron2D.Rect2i GetVisibleContentRect()`](#member-04d343378a10) | Gets the content viewport rectangle excluding padding and the visible scrollbar. |
| [`public System.Int32 GetVisibleLineCount()`](#member-fe7f7f2053f4) | Gets the number of lines intersecting the current visible viewport. |
| [`public System.Int32 GetVisibleParagraphCount()`](#member-d5b8d48ae176) | Gets the number of root paragraphs intersecting the viewport. |
| [`public System.Void InstallEffect(Electron2D.RichTextEffect effect)`](#member-79cd1cbbb431) | Installs one borrowed effect for tag lookup. |
| [`public System.Boolean InvalidateParagraph(System.Int32 paragraph)`](#member-2ddfc328425c) | Invalidates one root paragraph's cached layout. |
| [`public System.Boolean IsFinished()`](#member-1b123d7957e8) | Gets completion of the current asynchronous layout. |
| [`public System.Boolean IsMenuVisible()`](#member-4b5f8c4e4023) | Gets whether the context menu is visible. |
| [`public System.Void MenuOption(Electron2D.RichTextLabel.MenuItems option)`](#member-63385bbdd135) | Executes a built-in rich-text menu action. |
| [`public System.Void Newline()`](#member-c0bab2375a6c) | Starts another independently shaped paragraph. |
| [`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`](#member-8a8e6eb6cfc7) | Inherited typed lifecycle contract. |
| [`protected override Electron2D.CursorShape OnGetCursorShape(Electron2D.Vector2 atPosition)`](#member-a53ac36117b0) | Inherited typed lifecycle contract. |
| [`protected override Electron2D.DragPayload OnGetDragData(Electron2D.Vector2 atPosition)`](#member-2c4321146540) | Inherited typed lifecycle contract. |
| [`protected override Electron2D.Vector2 OnGetMinimumSize()`](#member-493e101a89d6) | Inherited typed lifecycle contract. |
| [`protected override System.String OnGetTooltip(Electron2D.Vector2 atPosition)`](#member-569afd22fc68) | Inherited typed lifecycle contract. |
| [`protected override System.Void OnNotification(System.Int32 what)`](#member-c12bfb3da19d) | Inherited typed lifecycle contract. |
| [`public System.Void ParseBBCode(System.String bbcode)`](#member-efa94baf8ed1) | Replaces runtime content with parsed markup. |
| [`public Electron2D.RichTextEffectEnvironment ParseExpressionsForValues(System.String[] expressions)`](#member-6163ff6530e3) | Parses named scalar and mixed-array effect arguments into a dedicated typed context. |
| [`public System.Void Pop()`](#member-33da26246560) | Restores the last pushed style/frame context. |
| [`public System.Void PopAll()`](#member-2beb8121a98a) | Restores all pushed contexts. |
| [`public System.Void PopContext()`](#member-5d26399d55b3) | Restores through the most recent explicit context. |
| [`public System.Void PushBGColor(Electron2D.Color color)`](#member-927f22d83e55) | Pushes finite bgcolor styling. |
| [`public System.Void PushBold()`](#member-0eb4820ae5e2) | Pushes the themed bold font role. |
| [`public System.Void PushBoldItalics()`](#member-9205a30d666a) | Pushes the themed bolditalics font role. |
| [`public System.Void PushCell()`](#member-d5767c96d8b4) | Pushes the next table cell frame. |
| [`public System.Void PushColor(Electron2D.Color color)`](#member-f6894496594f) | Pushes finite color styling. |
| [`public System.Void PushContext()`](#member-41706d093473) | Marks an independently restorable push context. |
| [`public System.Void PushCustomFX(Electron2D.RichTextEffect effect, Electron2D.RichTextEffectEnvironment env)`](#member-c4aed705de67) | Pushes a borrowed custom effect and its shared typed parameter context. |
| [`public System.Void PushDropcap(System.String text, Electron2D.Font font, System.Int32 size, Electron2D.Rect2 dropcapMargins = default, System.Nullable<Electron2D.Color> color = null, System.Int32 outlineSize = 0, System.Nullable<Electron2D.Color> outlineColor = null)`](#member-ea481841e10d) | Adds the paragraph's independently shaped initial drop cap. |
| [`public System.Void PushFGColor(Electron2D.Color color)`](#member-d1a98c3281c2) | Pushes finite fgcolor styling. |
| [`public System.Void PushFont(Electron2D.Font font, System.Int32 fontSize = 0)`](#member-8e3d2c3afaa8) | Pushes a borrowed font and optional positive font size. |
| [`public System.Void PushFontSize(System.Int32 fontSize)`](#member-a2c5bda3506d) | Pushes a positive font size. |
| [`public System.Void PushHint(System.String description)`](#member-97d732b04636) | Pushes a hover description. |
| [`public System.Void PushIndent(System.Int32 level)`](#member-6a5be5c7c990) | Pushes paragraph indentation. |
| [`public System.Void PushItalics()`](#member-86929b146b06) | Pushes the themed italics font role. |
| [`public System.Void PushLanguage(System.String language)`](#member-601815399c14) | Pushes a shaping language. |
| [`public System.Void PushList(System.Int32 level, Electron2D.RichTextLabel.ListType type, System.Boolean capitalize, System.String bullet = "\u2022")`](#member-0f3399ab5994) | Pushes an ordered or bulleted list. |
| [`public System.Void PushMeta<T>(T data, Electron2D.RichTextLabel.MetaUnderline underlineMode = Always, System.String tooltip = "")`](#member-9f309e85ebe4) | Pushes typed borrowed link metadata. |
| [`public System.Void PushMono()`](#member-0ba3292c35d7) | Pushes the themed mono font role. |
| [`public System.Void PushNormal()`](#member-61dd5f2d6131) | Pushes the themed normal font role. |
| [`public System.Void PushOutlineColor(Electron2D.Color color)`](#member-d53dad5b38cc) | Pushes finite outlinecolor styling. |
| [`public System.Void PushOutlineSize(System.Int32 outlineSize)`](#member-65e10b588ac2) | Pushes a nonnegative glyph outline width. |
| [`public System.Void PushParagraph(Electron2D.HorizontalAlignment alignment, Electron2D.TextDirection baseDirection = Auto, System.String language = "", Electron2D.StructuredTextParser stParser = Default, Electron2D.TextJustificationFlags justificationFlags = Kashida, WordBound, SkipLastLine, DoNotSkipSingleLine, System.Single[] tabStops = null)`](#member-468388b4f846) | Starts an explicitly configured paragraph context. |
| [`public System.Void PushStrikethrough(System.Nullable<Electron2D.Color> color = null)`](#member-2c6f08bc24eb) | Pushes strikethrough styling. |
| [`public System.Void PushTable(System.Int32 columns, Electron2D.InlineAlignment inlineAlign = Top, System.Int32 alignToRow = -1, System.String name = "")`](#member-1fcf17c7fb7a) | Pushes an inline table. |
| [`public System.Void PushUnderline(System.Nullable<Electron2D.Color> color = null)`](#member-bf44302006e4) | Pushes underline styling. |
| [`public System.Void ReloadEffects()`](#member-76c00828f664) | Rebuilds source tags using the current installed effects. |
| [`public System.Boolean RemoveParagraph(System.Int32 paragraph, System.Boolean noInvalidate = false)`](#member-8a42ef680359) | Removes one root paragraph and its children. |
| [`public System.Void ScrollToLine(System.Int32 line)`](#member-a3e026bbde4a) | Scrolls to a shaped line. |
| [`public System.Void ScrollToParagraph(System.Int32 paragraph)`](#member-0b3acd643547) | Scrolls to a root paragraph. |
| [`public System.Void ScrollToSelection()`](#member-4d25d4e9d983) | Scrolls to the first selected scalar. |
| [`public System.Void SelectAll()`](#member-882ddabb2160) | Selects the complete document when selection is enabled. |
| [`public System.Void SetCellBorderColor(Electron2D.Color color)`](#member-ba51f66b1eec) | Sets cell border tint. |
| [`public System.Void SetCellPadding(Electron2D.Rect2 padding)`](#member-ec6a7a227044) | Sets left/top/right/bottom cell padding. |
| [`public System.Void SetCellRowBackgroundColor(Electron2D.Color oddRowBG, Electron2D.Color evenRowBG)`](#member-9750d73d7c42) | Sets alternating cell row backgrounds. |
| [`public System.Void SetCellSizeOverride(Electron2D.Vector2 minSize, Electron2D.Vector2 maxSize)`](#member-80ec5bd91832) | Overrides cell minimum and maximum sizes. |
| [`public System.Void SetTableColumnExpand(System.Int32 column, System.Boolean expand, System.Int32 ratio = 1, System.Boolean shrink = true)`](#member-8dbcefcb8f53) | Configures table expansion weight and shrink participation. |
| [`public System.Void SetTableColumnName(System.Int32 column, System.String name)`](#member-b644fa7c32a6) | Sets a table column's assistive name. |
| [`public System.Void UpdateImage<T>(T key, Electron2D.RichTextLabel.ImageUpdateMask mask, Electron2D.Texture image, System.Single width = 0f, System.Single height = 0f, System.Nullable<Electron2D.Color> color = null, Electron2D.InlineAlignment inlineAlign = Center, Electron2D.Rect2 region = default, System.Boolean pad = false, System.String tooltip = "", Electron2D.RichTextLabel.ImageUnit widthUnit = Pixel, Electron2D.RichTextLabel.ImageUnit heightUnit = Pixel)`](#member-063c89a619ac) | Updates matching image keys under the specified mask. |
| [`protected override System.Void ValidateDisposal()`](#member-c3878cfe19d4) | Inherited typed lifecycle contract. |
| [`public Electron2D.TextAutowrapMode AutowrapMode { get; set; }`](#member-c3f6bdf83d2f) | Gets or sets the paragraph wrapping policy. |
| [`public Electron2D.TextLineBreakFlags AutowrapTrimFlags { get; set; }`](#member-ee2e301230aa) | Gets or sets the whitespace trimming flags at wrapped edges. |
| [`public System.Boolean BBCodeEnabled { get; set; }`](#member-314bdf1569d7) | Gets or sets whether Text assignments interpret markup. |
| [`public System.Boolean ContextMenuEnabled { get; set; }`](#member-b62a08d1082d) | Gets or sets whether right click opens the owned context menu. |
| [`public Electron2D.RichTextEffect[] CustomEffects { get; set; }`](#member-74ee38fa2b6c) | Gets or replaces copied membership of borrowed custom effects. |
| [`public System.Boolean DeselectOnFocusLossEnabled { get; set; }`](#member-69e725d25495) | Gets or sets whether focus loss clears selection. |
| [`public System.Boolean DragAndDropSelectionEnabled { get; set; }`](#member-a6e823b190b0) | Gets or sets whether selected text can be dragged. |
| [`public System.Boolean FitContent { get; set; }`](#member-ccf5a367c07f) | Gets or sets whether shaped content contributes to minimum size. |
| [`public System.Boolean HintUnderlined { get; set; }`](#member-5e73121c3193) | Gets or sets whether hover hints receive underline styling. |
| [`public Electron2D.HorizontalAlignment HorizontalAlignment { get; set; }`](#member-ec3b74b5b648) | Gets or sets the default paragraph alignment. |
| [`public Electron2D.TextJustificationFlags JustificationFlags { get; set; }`](#member-00a53376eecd) | Gets or sets the paragraph fill rules. |
| [`public System.String Language { get; set; }`](#member-d90646bd416e) | Gets or sets the shaping language. |
| [`public System.Boolean MetaUnderlined { get; set; }`](#member-5d1ec78dbb03) | Gets or sets whether link underline policies are applied. |
| [`public System.Int32 ProgressBarDelay { get; set; }`](#member-96b091f2816a) | Gets or sets the nonnegative threaded progress delay in milliseconds. |
| [`public System.Boolean ScrollActive { get; set; }`](#member-28402ecd8957) | Gets or sets whether content can scroll through its owned scrollbar. |
| [`public System.Boolean ScrollFollowing { get; set; }`](#member-9f306fd8423d) | Gets or sets whether layout follows the content bottom. |
| [`public System.Boolean ScrollFollowingVisibleCharacters { get; set; }`](#member-cfa233329c99) | Gets or sets whether scrolling follows the reveal position. |
| [`public System.Boolean SelectionEnabled { get; set; }`](#member-cf7911a7ab36) | Gets or sets whether pointer and keyboard selection is available. |
| [`public System.Boolean ShortcutKeysEnabled { get; set; }`](#member-c300e91697ba) | Gets or sets whether remapped copy and selection actions are handled. |
| [`public Electron2D.StructuredTextParser StructuredTextBIDIOverride { get; set; }`](#member-6c2e3825a106) | Gets or sets the structured text context parser. |
| [`public System.String[] StructuredTextBIDIOverrideOptions { get; set; }`](#member-f7c8463a7e76) | Gets or sets copied structured-text parser options. |
| [`public System.Int32 TabSize { get; set; }`](#member-8e2302f8d45c) | Gets or sets a positive default tab width. |
| [`public System.Single[] TabStops { get; set; }`](#member-74f2ce91db4c) | Gets or sets copied finite nonnegative tab positions. |
| [`public System.String Text { get; set; }`](#member-6335e99c2f23) | Gets or sets untranslated source text; assignment replaces manual stack edits. |
| [`public Electron2D.TextDirection TextDirection { get; set; }`](#member-2b12d66baecf) | Gets or sets the default bidirectional paragraph base. |
| [`public System.Boolean Threaded { get; set; }`](#member-830cddc17113) | Gets or sets whether paragraph shaping runs asynchronously. |
| [`public Electron2D.VerticalAlignment VerticalAlignment { get; set; }`](#member-f3ca5c6a7e8e) | Gets or sets the placement of shorter content in its viewport. |
| [`public System.Int32 VisibleCharacters { get; set; }`](#member-ccc1d7a71ff3) | Gets or sets the scalar/glyph reveal budget. |
| [`public Electron2D.TextVisibleCharactersBehavior VisibleCharactersBehavior { get; set; }`](#member-f763642c23e6) | Gets or sets the scalar or glyph reveal policy. |
| [`public System.Single VisibleRatio { get; set; }`](#member-ba36881de18b) | Gets or sets the reveal fraction. |

## Member descriptions

<a id="member-cd5272b2f8d1"></a>

### RichTextLabel()

`public RichTextLabel()`

Creates an empty clipped rich-text control with owned scrolling and context-menu children.

<a id="member-091ce13e57a5"></a>

### RichTextLabel(System.String)

`public RichTextLabel(System.String text)`

Creates plain initial text with the default rich-text policies.

**Text:** Nonnull source text.

<a id="member-c987a360ddba"></a>

### Finished

`public event System.Action Finished`

Reports that the current document has completed layout.

<a id="member-8c71d73ac81b"></a>

### MetaClicked

`public event System.Action<Electron2D.RichTextMetadata> MetaClicked`

Reports the borrowed typed metadata of a clicked span.

<a id="member-830dc7440637"></a>

### MetaHoverEnded

`public event System.Action<Electron2D.RichTextMetadata> MetaHoverEnded`

Reports departure from a metadata span.

<a id="member-cc88c3e2a884"></a>

### MetaHoverStarted

`public event System.Action<Electron2D.RichTextMetadata> MetaHoverStarted`

Reports entry into a metadata span.

<a id="member-01c8c91ec0cf"></a>

### AddHR(System.Int32, System.Int32, System.Nullable<Electron2D.Color>, Electron2D.HorizontalAlignment, System.Boolean, System.Boolean)

`public System.Void AddHR(System.Int32 width = 90, System.Int32 height = 2, System.Nullable<Electron2D.Color> color = null, Electron2D.HorizontalAlignment alignment = Center, System.Boolean widthInPercent = true, System.Boolean heightInPercent = false)`

Adds a horizontal rule.

**Width:** Nonnegative width.

**Height:** Nonnegative height.

**Color:** Finite tint or white.

**Alignment:** Horizontal position.

**Widthinpercent:** Width is a percentage of content width.

**Heightinpercent:** Height is a percentage of content width.

<a id="member-d011bfb0b561"></a>

### AddImage(Electron2D.Texture, System.Single, System.Single, System.Nullable<Electron2D.Color>, Electron2D.InlineAlignment, Electron2D.Rect2, System.Boolean, System.String, Electron2D.RichTextLabel.ImageUnit, Electron2D.RichTextLabel.ImageUnit, System.String)

`public System.Void AddImage(Electron2D.Texture image, System.Single width = 0f, System.Single height = 0f, System.Nullable<Electron2D.Color> color = null, Electron2D.InlineAlignment inlineAlign = Center, Electron2D.Rect2 region = default, System.Boolean pad = false, System.String tooltip = "", Electron2D.RichTextLabel.ImageUnit widthUnit = Pixel, Electron2D.RichTextLabel.ImageUnit heightUnit = Pixel, System.String altText = "")`

Adds an unkeyed borrowed inline image.

**Image:** Live texture.

**Width:** Nonnegative width.

**Height:** Nonnegative height.

**Color:** Tint or white.

**Inlinealign:** Object alignment.

**Region:** Source region or zero for the texture.

**Pad:** Pads smaller source images.

**Tooltip:** Hover text.

**Widthunit:** Width units.

**Heightunit:** Height units.

**Alttext:** Assistive description.

<a id="member-92ec0345179b"></a>

### AddImage(T, Electron2D.Texture, System.Single, System.Single, System.Nullable<Electron2D.Color>, Electron2D.InlineAlignment, Electron2D.Rect2, System.Boolean, System.String, Electron2D.RichTextLabel.ImageUnit, Electron2D.RichTextLabel.ImageUnit, System.String)

`public System.Void AddImage<T>(T key, Electron2D.Texture image, System.Single width = 0f, System.Single height = 0f, System.Nullable<Electron2D.Color> color = null, Electron2D.InlineAlignment inlineAlign = Center, Electron2D.Rect2 region = default, System.Boolean pad = false, System.String tooltip = "", Electron2D.RichTextLabel.ImageUnit widthUnit = Pixel, Electron2D.RichTextLabel.ImageUnit heightUnit = Pixel, System.String altText = "")`

Adds an image with an exact typed update key.

**T:** Key type.

**Image:** Borrowed texture.

**Key:** Borrowed key.

**Width:** Width.

**Height:** Height.

**Color:** Tint.

**Inlinealign:** Alignment.

**Region:** Source region.

**Pad:** Padding mode.

**Tooltip:** Hover text.

**Widthunit:** Width unit.

**Heightunit:** Height unit.

**Alttext:** Assistive description.

<a id="member-996fd6a36f0d"></a>

### AddText(System.String)

`public System.Void AddText(System.String text)`

Adds raw text, splitting LF/CRLF into independently shaped paragraphs.

**Text:** Nonnull raw text.

<a id="member-2315b89cbd7a"></a>

### AppendText(System.String)

`public System.Void AppendText(System.String bbcode)`

Appends markup; closing tags may only close tags opened in this append call.

**Bbcode:** Nonnull markup.

<a id="member-4109aac59122"></a>

### Clear()

`public System.Void Clear()`

Clears the runtime stack and document without replacing the source Text property.

<a id="member-3885f453f3b4"></a>

### CreateSceneInstanceFactory()

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

<a id="member-3444ceb07c7a"></a>

### Deselect()

`public System.Void Deselect()`

Clears selection.

<a id="member-a1a555c1d268"></a>

### Dispose(System.Boolean)

`protected override System.Void Dispose(System.Boolean disposing)`

<a id="member-1172adefca69"></a>

### GetCharacterLine(System.Int32)

`public System.Int32 GetCharacterLine(System.Int32 character)`

Gets the loaded line containing a scalar position.

**Character:** Nonnegative scalar offset.

**Returns:** Line or -1 if not loaded.

<a id="member-09f29d5a1e1f"></a>

### GetCharacterParagraph(System.Int32)

`public System.Int32 GetCharacterParagraph(System.Int32 character)`

Gets the root paragraph containing a scalar position.

**Character:** Nonnegative scalar offset.

**Returns:** Paragraph or -1.

<a id="member-ab8a96b3d251"></a>

### GetContentHeight()

`public System.Int32 GetContentHeight()`

Gets total loaded content height.

**Returns:** Ceiling pixels.

<a id="member-e914da6fb217"></a>

### GetContentWidth()

`public System.Int32 GetContentWidth()`

Gets total loaded content width.

**Returns:** Ceiling pixels.

<a id="member-33338465546c"></a>

### GetLineCount()

`public System.Int32 GetLineCount()`

Gets the number of shaped lines in root paragraphs.

**Returns:** Loaded root line count. Cell lines contribute to table height.

<a id="member-a6735bd4c2d5"></a>

### GetLineHeight(System.Int32)

`public System.Int32 GetLineHeight(System.Int32 line)`

Gets one loaded line's pixel height.

**Line:** Existing line.

**Returns:** Ceiling height.

<a id="member-8e0b649f6e1b"></a>

### GetLineOffset(System.Int32)

`public System.Single GetLineOffset(System.Int32 line)`

Gets a loaded line's document Y offset.

**Line:** Existing line.

**Returns:** Pixel offset.

<a id="member-bf6601ef7589"></a>

### GetLineRange(System.Int32)

`public Electron2D.Vector2i GetLineRange(System.Int32 line)`

Gets a loaded line's absolute scalar interval.

**Line:** Existing line.

**Returns:** Start and exclusive end.

<a id="member-780b88fe92ef"></a>

### GetLineWidth(System.Int32)

`public System.Int32 GetLineWidth(System.Int32 line)`

Gets one loaded line's width.

**Line:** Existing line.

**Returns:** Ceiling width.

<a id="member-4e44d47dd33e"></a>

### GetMenu()

`public Electron2D.PopupMenu GetMenu()`

Gets the borrowed required context menu.

**Returns:** Owned child.

<a id="member-3d638cdc224e"></a>

### GetParagraphCount()

`public System.Int32 GetParagraphCount()`

Gets root paragraph count.

**Returns:** At least one paragraph.

<a id="member-14d40cf013b3"></a>

### GetParagraphOffset(System.Int32)

`public System.Single GetParagraphOffset(System.Int32 paragraph)`

Gets one root paragraph's Y offset.

**Paragraph:** Existing root paragraph.

**Returns:** Pixel offset.

<a id="member-dfd71be8f516"></a>

### GetParsedText()

`public System.String GetParsedText()`

Gets raw parsed text, including drop caps, image spaces and indentation markers.

**Returns:** Markup-free document text.

<a id="member-f033e8dc1c8b"></a>

### GetPropertyDescriptors()

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

<a id="member-95e961284feb"></a>

### GetSelectedText()

`public System.String GetSelectedText()`

Gets the selected plain text.

**Returns:** Empty without selection.

<a id="member-6cee10847ef4"></a>

### GetSelectionFrom()

`public System.Int32 GetSelectionFrom()`

Gets the selected absolute scalar start.

**Returns:** -1 without selection.

<a id="member-f7a359690639"></a>

### GetSelectionLineOffset()

`public System.Single GetSelectionLineOffset()`

Gets the selected line's Y offset.

**Returns:** Zero without selection.

<a id="member-26aab5dc9b98"></a>

### GetSelectionTo()

`public System.Int32 GetSelectionTo()`

Gets the selected exclusive scalar end.

**Returns:** -1 without selection.

<a id="member-b8f482626a74"></a>

### GetTotalCharacterCount()

`public System.Int32 GetTotalCharacterCount()`

Gets scalar document count excluding drop caps and structural tags.

**Returns:** Text, newline and image count.

<a id="member-b13b00abbbf2"></a>

### GetVScrollBar()

`public Electron2D.VScrollBar GetVScrollBar()`

Gets the borrowed required vertical scrollbar.

**Returns:** Owned child.

<a id="member-04d343378a10"></a>

### GetVisibleContentRect()

`public Electron2D.Rect2i GetVisibleContentRect()`

Gets the content viewport rectangle excluding padding and the visible scrollbar.

**Returns:** Local integer rectangle.

<a id="member-fe7f7f2053f4"></a>

### GetVisibleLineCount()

`public System.Int32 GetVisibleLineCount()`

Gets the number of lines intersecting the current visible viewport.

**Returns:** Zero for a locally hidden label.

<a id="member-d5b8d48ae176"></a>

### GetVisibleParagraphCount()

`public System.Int32 GetVisibleParagraphCount()`

Gets the number of root paragraphs intersecting the viewport.

**Returns:** Zero for a locally hidden label.

<a id="member-79cd1cbbb431"></a>

### InstallEffect(Electron2D.RichTextEffect)

`public System.Void InstallEffect(Electron2D.RichTextEffect effect)`

Installs one borrowed effect for tag lookup.

**Effect:** Live effect resource.

<a id="member-2ddfc328425c"></a>

### InvalidateParagraph(System.Int32)

`public System.Boolean InvalidateParagraph(System.Int32 paragraph)`

Invalidates one root paragraph's cached layout.

**Paragraph:** Root index.

**Returns:** False for an invalid index.

<a id="member-1b123d7957e8"></a>

### IsFinished()

`public System.Boolean IsFinished()`

Gets completion of the current asynchronous layout.

**Returns:** True when the current version is loaded.

<a id="member-4b5f8c4e4023"></a>

### IsMenuVisible()

`public System.Boolean IsMenuVisible()`

Gets whether the context menu is visible.

**Returns:** Popup visibility.

<a id="member-63385bbdd135"></a>

### MenuOption(Electron2D.RichTextLabel.MenuItems)

`public System.Void MenuOption(Electron2D.RichTextLabel.MenuItems option)`

Executes a built-in rich-text menu action.

**Option:** Copy or SelectAll.

<a id="member-c0bab2375a6c"></a>

### Newline()

`public System.Void Newline()`

Starts another independently shaped paragraph.

<a id="member-8a8e6eb6cfc7"></a>

### OnGUIInput(Electron2D.InputEvent)

`protected override System.Void OnGUIInput(Electron2D.InputEvent inputEvent)`

<a id="member-a53ac36117b0"></a>

### OnGetCursorShape(Electron2D.Vector2)

`protected override Electron2D.CursorShape OnGetCursorShape(Electron2D.Vector2 atPosition)`

<a id="member-2c4321146540"></a>

### OnGetDragData(Electron2D.Vector2)

`protected override Electron2D.DragPayload OnGetDragData(Electron2D.Vector2 atPosition)`

<a id="member-493e101a89d6"></a>

### OnGetMinimumSize()

`protected override Electron2D.Vector2 OnGetMinimumSize()`

<a id="member-569afd22fc68"></a>

### OnGetTooltip(Electron2D.Vector2)

`protected override System.String OnGetTooltip(Electron2D.Vector2 atPosition)`

<a id="member-c12bfb3da19d"></a>

### OnNotification(System.Int32)

`protected override System.Void OnNotification(System.Int32 what)`

<a id="member-efa94baf8ed1"></a>

### ParseBBCode(System.String)

`public System.Void ParseBBCode(System.String bbcode)`

Replaces runtime content with parsed markup.

**Bbcode:** Nonnull markup.

<a id="member-6163ff6530e3"></a>

### ParseExpressionsForValues(System.String[])

`public Electron2D.RichTextEffectEnvironment ParseExpressionsForValues(System.String[] expressions)`

Parses named scalar and mixed-array effect arguments into a dedicated typed context.

**Expressions:** Nonnull name=value expressions.

**Returns:** Parsed context; malformed assignment terminates parsing after prior entries.

<a id="member-33da26246560"></a>

### Pop()

`public System.Void Pop()`

Restores the last pushed style/frame context.

<a id="member-2beb8121a98a"></a>

### PopAll()

`public System.Void PopAll()`

Restores all pushed contexts.

<a id="member-5d26399d55b3"></a>

### PopContext()

`public System.Void PopContext()`

Restores through the most recent explicit context.

<a id="member-927f22d83e55"></a>

### PushBGColor(Electron2D.Color)

`public System.Void PushBGColor(Electron2D.Color color)`

Pushes finite bgcolor styling.

**Color:** Finite color.

<a id="member-0eb4820ae5e2"></a>

### PushBold()

`public System.Void PushBold()`

Pushes the themed bold font role.

<a id="member-9205a30d666a"></a>

### PushBoldItalics()

`public System.Void PushBoldItalics()`

Pushes the themed bolditalics font role.

<a id="member-d5767c96d8b4"></a>

### PushCell()

`public System.Void PushCell()`

Pushes the next table cell frame.

<a id="member-f6894496594f"></a>

### PushColor(Electron2D.Color)

`public System.Void PushColor(Electron2D.Color color)`

Pushes finite color styling.

**Color:** Finite color.

<a id="member-41706d093473"></a>

### PushContext()

`public System.Void PushContext()`

Marks an independently restorable push context.

<a id="member-c4aed705de67"></a>

### PushCustomFX(Electron2D.RichTextEffect, Electron2D.RichTextEffectEnvironment)

`public System.Void PushCustomFX(Electron2D.RichTextEffect effect, Electron2D.RichTextEffectEnvironment env)`

Pushes a borrowed custom effect and its shared typed parameter context.

**Effect:** Live resource.

**Env:** Borrowed arguments.

<a id="member-ea481841e10d"></a>

### PushDropcap(System.String, Electron2D.Font, System.Int32, Electron2D.Rect2, System.Nullable<Electron2D.Color>, System.Int32, System.Nullable<Electron2D.Color>)

`public System.Void PushDropcap(System.String text, Electron2D.Font font, System.Int32 size, Electron2D.Rect2 dropcapMargins = default, System.Nullable<Electron2D.Color> color = null, System.Int32 outlineSize = 0, System.Nullable<Electron2D.Color> outlineColor = null)`

Adds the paragraph's independently shaped initial drop cap.

**Text:** Nonnull cap text.

**Font:** Borrowed font.

**Size:** Positive size.

**Dropcapmargins:** Left/top/right/bottom margins.

**Color:** Cap color.

**Outlinesize:** Nonnegative outline width.

**Outlinecolor:** Outline tint.

<a id="member-d1a98c3281c2"></a>

### PushFGColor(Electron2D.Color)

`public System.Void PushFGColor(Electron2D.Color color)`

Pushes finite fgcolor styling.

**Color:** Finite color.

<a id="member-8e3d2c3afaa8"></a>

### PushFont(Electron2D.Font, System.Int32)

`public System.Void PushFont(Electron2D.Font font, System.Int32 fontSize = 0)`

Pushes a borrowed font and optional positive font size.

**Font:** Live font.

**Fontsize:** Zero inherits size.

<a id="member-a2c5bda3506d"></a>

### PushFontSize(System.Int32)

`public System.Void PushFontSize(System.Int32 fontSize)`

Pushes a positive font size.

**Fontsize:** Positive pixel size.

<a id="member-97d732b04636"></a>

### PushHint(System.String)

`public System.Void PushHint(System.String description)`

Pushes a hover description.

**Description:** Nonnull hint.

<a id="member-6a5be5c7c990"></a>

### PushIndent(System.Int32)

`public System.Void PushIndent(System.Int32 level)`

Pushes paragraph indentation.

**Level:** Nonnegative tab levels.

<a id="member-86929b146b06"></a>

### PushItalics()

`public System.Void PushItalics()`

Pushes the themed italics font role.

<a id="member-601815399c14"></a>

### PushLanguage(System.String)

`public System.Void PushLanguage(System.String language)`

Pushes a shaping language.

**Language:** Nonnull language.

<a id="member-0f3399ab5994"></a>

### PushList(System.Int32, Electron2D.RichTextLabel.ListType, System.Boolean, System.String)

`public System.Void PushList(System.Int32 level, Electron2D.RichTextLabel.ListType type, System.Boolean capitalize, System.String bullet = "\u2022")`

Pushes an ordered or bulleted list.

**Level:** Nonnegative indentation.

**Type:** List domain.

**Capitalize:** Uppercase numeric markers.

**Bullet:** Nonnull bullet text.

<a id="member-9f309e85ebe4"></a>

### PushMeta(T, Electron2D.RichTextLabel.MetaUnderline, System.String)

`public System.Void PushMeta<T>(T data, Electron2D.RichTextLabel.MetaUnderline underlineMode = Always, System.String tooltip = "")`

Pushes typed borrowed link metadata.

**T:** Metadata type.

**Data:** Borrowed payload.

**Underlinemode:** Link underline policy.

**Tooltip:** Nonnull optional hover text.

<a id="member-0ba3292c35d7"></a>

### PushMono()

`public System.Void PushMono()`

Pushes the themed mono font role.

<a id="member-61dd5f2d6131"></a>

### PushNormal()

`public System.Void PushNormal()`

Pushes the themed normal font role.

<a id="member-d53dad5b38cc"></a>

### PushOutlineColor(Electron2D.Color)

`public System.Void PushOutlineColor(Electron2D.Color color)`

Pushes finite outlinecolor styling.

**Color:** Finite color.

<a id="member-65e10b588ac2"></a>

### PushOutlineSize(System.Int32)

`public System.Void PushOutlineSize(System.Int32 outlineSize)`

Pushes a nonnegative glyph outline width.

**Outlinesize:** Pixel width.

<a id="member-468388b4f846"></a>

### PushParagraph(Electron2D.HorizontalAlignment, Electron2D.TextDirection, System.String, Electron2D.StructuredTextParser, Electron2D.TextJustificationFlags, System.Single[])

`public System.Void PushParagraph(Electron2D.HorizontalAlignment alignment, Electron2D.TextDirection baseDirection = Auto, System.String language = "", Electron2D.StructuredTextParser stParser = Default, Electron2D.TextJustificationFlags justificationFlags = Kashida, WordBound, SkipLastLine, DoNotSkipSingleLine, System.Single[] tabStops = null)`

Starts an explicitly configured paragraph context.

**Alignment:** Horizontal alignment.

**Basedirection:** Text direction.

**Language:** Nonnull shaping language.

**Stparser:** Structured parser.

**Justificationflags:** Fill flags.

**Tabstops:** Copied tab positions.

<a id="member-2c6f08bc24eb"></a>

### PushStrikethrough(System.Nullable<Electron2D.Color>)

`public System.Void PushStrikethrough(System.Nullable<Electron2D.Color> color = null)`

Pushes strikethrough styling.

**Color:** Transparent or null follows the text color.

<a id="member-1fcf17c7fb7a"></a>

### PushTable(System.Int32, Electron2D.InlineAlignment, System.Int32, System.String)

`public System.Void PushTable(System.Int32 columns, Electron2D.InlineAlignment inlineAlign = Top, System.Int32 alignToRow = -1, System.String name = "")`

Pushes an inline table.

**Columns:** Positive column count.

**Inlinealign:** Inline alignment.

**Aligntorow:** -1 or row alignment index.

**Name:** Nonnull assistive name.

<a id="member-bf44302006e4"></a>

### PushUnderline(System.Nullable<Electron2D.Color>)

`public System.Void PushUnderline(System.Nullable<Electron2D.Color> color = null)`

Pushes underline styling.

**Color:** Transparent or null follows the text color.

<a id="member-76c00828f664"></a>

### ReloadEffects()

`public System.Void ReloadEffects()`

Rebuilds source tags using the current installed effects.

<a id="member-8a42ef680359"></a>

### RemoveParagraph(System.Int32, System.Boolean)

`public System.Boolean RemoveParagraph(System.Int32 paragraph, System.Boolean noInvalidate = false)`

Removes one root paragraph and its children.

**Paragraph:** Root index.

**Noinvalidate:** Defers shaping invalidation until an explicit invalidation.

**Returns:** False for an invalid index.

<a id="member-a3e026bbde4a"></a>

### ScrollToLine(System.Int32)

`public System.Void ScrollToLine(System.Int32 line)`

Scrolls to a shaped line.

**Line:** Existing line.

<a id="member-0b3acd643547"></a>

### ScrollToParagraph(System.Int32)

`public System.Void ScrollToParagraph(System.Int32 paragraph)`

Scrolls to a root paragraph.

**Paragraph:** Existing paragraph.

<a id="member-4d25d4e9d983"></a>

### ScrollToSelection()

`public System.Void ScrollToSelection()`

Scrolls to the first selected scalar.

<a id="member-882ddabb2160"></a>

### SelectAll()

`public System.Void SelectAll()`

Selects the complete document when selection is enabled.

<a id="member-ba51f66b1eec"></a>

### SetCellBorderColor(Electron2D.Color)

`public System.Void SetCellBorderColor(Electron2D.Color color)`

Sets cell border tint.

**Color:** Finite color.

<a id="member-ec6a7a227044"></a>

### SetCellPadding(Electron2D.Rect2)

`public System.Void SetCellPadding(Electron2D.Rect2 padding)`

Sets left/top/right/bottom cell padding.

**Padding:** Finite nonnegative margins.

<a id="member-9750d73d7c42"></a>

### SetCellRowBackgroundColor(Electron2D.Color, Electron2D.Color)

`public System.Void SetCellRowBackgroundColor(Electron2D.Color oddRowBG, Electron2D.Color evenRowBG)`

Sets alternating cell row backgrounds.

**Oddrowbg:** Odd tint.

**Evenrowbg:** Even tint.

<a id="member-80ec5bd91832"></a>

### SetCellSizeOverride(Electron2D.Vector2, Electron2D.Vector2)

`public System.Void SetCellSizeOverride(Electron2D.Vector2 minSize, Electron2D.Vector2 maxSize)`

Overrides cell minimum and maximum sizes.

**Minsize:** Finite nonnegative minimum.

**Maxsize:** Finite nonnegative maximum; zero is unlimited.

<a id="member-8dbcefcb8f53"></a>

### SetTableColumnExpand(System.Int32, System.Boolean, System.Int32, System.Boolean)

`public System.Void SetTableColumnExpand(System.Int32 column, System.Boolean expand, System.Int32 ratio = 1, System.Boolean shrink = true)`

Configures table expansion weight and shrink participation.

**Column:** Existing column.

**Expand:** Expansion.

**Ratio:** Positive weight.

**Shrink:** Shrink participation.

<a id="member-b644fa7c32a6"></a>

### SetTableColumnName(System.Int32, System.String)

`public System.Void SetTableColumnName(System.Int32 column, System.String name)`

Sets a table column's assistive name.

**Column:** Existing column.

**Name:** Nonnull name.

<a id="member-063c89a619ac"></a>

### UpdateImage(T, Electron2D.RichTextLabel.ImageUpdateMask, Electron2D.Texture, System.Single, System.Single, System.Nullable<Electron2D.Color>, Electron2D.InlineAlignment, Electron2D.Rect2, System.Boolean, System.String, Electron2D.RichTextLabel.ImageUnit, Electron2D.RichTextLabel.ImageUnit)

`public System.Void UpdateImage<T>(T key, Electron2D.RichTextLabel.ImageUpdateMask mask, Electron2D.Texture image, System.Single width = 0f, System.Single height = 0f, System.Nullable<Electron2D.Color> color = null, Electron2D.InlineAlignment inlineAlign = Center, Electron2D.Rect2 region = default, System.Boolean pad = false, System.String tooltip = "", Electron2D.RichTextLabel.ImageUnit widthUnit = Pixel, Electron2D.RichTextLabel.ImageUnit heightUnit = Pixel)`

Updates matching image keys under the specified mask.

**T:** Exact key type.

**Key:** Matching key.

**Mask:** Fields to replace.

**Image:** Live replacement texture when the Texture mask is set; otherwise ignored.

**Width:** Width.

**Height:** Height.

**Color:** Tint.

**Inlinealign:** Alignment.

**Region:** Source region.

**Pad:** Padding.

**Tooltip:** Hover text.

**Widthunit:** Width units.

**Heightunit:** Height units.

<a id="member-c3878cfe19d4"></a>

### ValidateDisposal()

`protected override System.Void ValidateDisposal()`

<a id="member-c3f6bdf83d2f"></a>

### AutowrapMode

`public Electron2D.TextAutowrapMode AutowrapMode { get; set; }`

Gets or sets the paragraph wrapping policy.

**Value:** TextAutowrapMode.WordSmart initially.

<a id="member-ee2e301230aa"></a>

### AutowrapTrimFlags

`public Electron2D.TextLineBreakFlags AutowrapTrimFlags { get; set; }`

Gets or sets the whitespace trimming flags at wrapped edges.

**Value:** TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces initially.

<a id="member-314bdf1569d7"></a>

### BBCodeEnabled

`public System.Boolean BBCodeEnabled { get; set; }`

Gets or sets whether Text assignments interpret markup.

**Value:** false initially.

<a id="member-b62a08d1082d"></a>

### ContextMenuEnabled

`public System.Boolean ContextMenuEnabled { get; set; }`

Gets or sets whether right click opens the owned context menu.

**Value:** false initially.

<a id="member-74ee38fa2b6c"></a>

### CustomEffects

`public Electron2D.RichTextEffect[] CustomEffects { get; set; }`

Gets or replaces copied membership of borrowed custom effects.

**Value:** Empty initially; effect resources remain borrowed.

<a id="member-69e725d25495"></a>

### DeselectOnFocusLossEnabled

`public System.Boolean DeselectOnFocusLossEnabled { get; set; }`

Gets or sets whether focus loss clears selection.

**Value:** true initially.

<a id="member-a6e823b190b0"></a>

### DragAndDropSelectionEnabled

`public System.Boolean DragAndDropSelectionEnabled { get; set; }`

Gets or sets whether selected text can be dragged.

**Value:** true initially.

<a id="member-ccf5a367c07f"></a>

### FitContent

`public System.Boolean FitContent { get; set; }`

Gets or sets whether shaped content contributes to minimum size.

**Value:** false initially.

<a id="member-5e73121c3193"></a>

### HintUnderlined

`public System.Boolean HintUnderlined { get; set; }`

Gets or sets whether hover hints receive underline styling.

**Value:** true initially.

<a id="member-ec3b74b5b648"></a>

### HorizontalAlignment

`public Electron2D.HorizontalAlignment HorizontalAlignment { get; set; }`

Gets or sets the default paragraph alignment.

**Value:** HorizontalAlignment.Left initially.

<a id="member-00a53376eecd"></a>

### JustificationFlags

`public Electron2D.TextJustificationFlags JustificationFlags { get; set; }`

Gets or sets the paragraph fill rules.

**Value:** (TextJustificationFlags)163 initially.

<a id="member-d90646bd416e"></a>

### Language

`public System.String Language { get; set; }`

Gets or sets the shaping language.

**Value:** Empty initially.

<a id="member-5d1ec78dbb03"></a>

### MetaUnderlined

`public System.Boolean MetaUnderlined { get; set; }`

Gets or sets whether link underline policies are applied.

**Value:** true initially.

<a id="member-96b091f2816a"></a>

### ProgressBarDelay

`public System.Int32 ProgressBarDelay { get; set; }`

Gets or sets the nonnegative threaded progress delay in milliseconds.

**Value:** 1000 initially.

<a id="member-28402ecd8957"></a>

### ScrollActive

`public System.Boolean ScrollActive { get; set; }`

Gets or sets whether content can scroll through its owned scrollbar.

**Value:** true initially.

<a id="member-9f306fd8423d"></a>

### ScrollFollowing

`public System.Boolean ScrollFollowing { get; set; }`

Gets or sets whether layout follows the content bottom.

**Value:** false initially.

<a id="member-cfa233329c99"></a>

### ScrollFollowingVisibleCharacters

`public System.Boolean ScrollFollowingVisibleCharacters { get; set; }`

Gets or sets whether scrolling follows the reveal position.

**Value:** false initially.

<a id="member-cf7911a7ab36"></a>

### SelectionEnabled

`public System.Boolean SelectionEnabled { get; set; }`

Gets or sets whether pointer and keyboard selection is available.

**Value:** false initially.

<a id="member-c300e91697ba"></a>

### ShortcutKeysEnabled

`public System.Boolean ShortcutKeysEnabled { get; set; }`

Gets or sets whether remapped copy and selection actions are handled.

**Value:** true initially.

<a id="member-6c2e3825a106"></a>

### StructuredTextBIDIOverride

`public Electron2D.StructuredTextParser StructuredTextBIDIOverride { get; set; }`

Gets or sets the structured text context parser.

**Value:** StructuredTextParser.Default initially.

<a id="member-f7c8463a7e76"></a>

### StructuredTextBIDIOverrideOptions

`public System.String[] StructuredTextBIDIOverrideOptions { get; set; }`

Gets or sets copied structured-text parser options.

**Value:** Empty initially.

<a id="member-8e2302f8d45c"></a>

### TabSize

`public System.Int32 TabSize { get; set; }`

Gets or sets a positive default tab width.

**Value:** Four initially.

<a id="member-74f2ce91db4c"></a>

### TabStops

`public System.Single[] TabStops { get; set; }`

Gets or sets copied finite nonnegative tab positions.

**Value:** Empty initially.

<a id="member-6335e99c2f23"></a>

### Text

`public System.String Text { get; set; }`

Gets or sets untranslated source text; assignment replaces manual stack edits.

**Value:** Empty initially.

<a id="member-2b12d66baecf"></a>

### TextDirection

`public Electron2D.TextDirection TextDirection { get; set; }`

Gets or sets the default bidirectional paragraph base.

**Value:** TextDirection.Auto initially.

<a id="member-830cddc17113"></a>

### Threaded

`public System.Boolean Threaded { get; set; }`

Gets or sets whether paragraph shaping runs asynchronously.

**Value:** false initially.

<a id="member-f3ca5c6a7e8e"></a>

### VerticalAlignment

`public Electron2D.VerticalAlignment VerticalAlignment { get; set; }`

Gets or sets the placement of shorter content in its viewport.

**Value:** VerticalAlignment.Top initially.

<a id="member-ccc1d7a71ff3"></a>

### VisibleCharacters

`public System.Int32 VisibleCharacters { get; set; }`

Gets or sets the scalar/glyph reveal budget.

**Value:** -1 shows all.

<a id="member-f763642c23e6"></a>

### VisibleCharactersBehavior

`public Electron2D.TextVisibleCharactersBehavior VisibleCharactersBehavior { get; set; }`

Gets or sets the scalar or glyph reveal policy.

**Value:** TextVisibleCharactersBehavior.CharsBeforeShaping initially.

<a id="member-ba36881de18b"></a>

### VisibleRatio

`public System.Single VisibleRatio { get; set; }`

Gets or sets the reveal fraction.

**Value:** One initially; zero through one reveals a proportional scalar budget.

## Verification and limits

See the rich-text component for actual tests, native artifacts, prepared allocation bounds and exact missing dependencies.

Font tags now create independent FontVariation spans borrowing generic Font resources, including spacing, embolden, collection face, slant, variable coordinates and feature options with their documented aliases. See [Text](../components/text.md) for the native instance and cache contract. Script resource installation remains its own dependency.
