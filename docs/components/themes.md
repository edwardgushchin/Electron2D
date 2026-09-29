# Typed themes and lookup

Last updated: 2026-09-30

## Scope and owned types

This component connects reusable [Theme](../classes/Theme.md) resources and the [ThemeDB](../classes/ThemeDB.md) process service to typed Control/Window lookup, overrides and actual layout/drawing consumers. Internal [ThemeOwner](../classes/ThemeOwner.md) shares cache, subscription and owner traversal behavior without creating a public theme-owner API. [Theme.DataType](../classes/Theme.DataType.md) preserves category identities.

The implemented data categories are Color, Constant, Font, FontSize, Icon and StyleBox. General Theme file loading, remaining GUI default assets and editor skin authoring retain precise separate dependencies under [ADR 0083](../decisions/rendering.md#adr-0083).

## Runtime flow

1. A Theme stores values by category, explicit theme type and item name. Resource queries read this exact type; typed methods preserve null resource entries and category fallback behavior.
2. Control/Window selects a type dependency order from its active variation and native ancestry. Its own overrides participate only for requests matching its default/current type or variation.
3. Lookup visits each nearest-to-outer Control/Window owner theme, checking the type order within each resource. A neutral Node ends branch inheritance. The global default theme follows; a universal category fallback supplies the final Get result, while Has still reports actual matching data.
4. Typed query caches are invalidated by assignment, variation, ancestry, resource content and fallback changes. Resource callbacks use deferred owner-thread delivery; direct override changes notify immediately unless a bulk operation suppresses publication.
5. ThemeChanged and notifications 45/32 refresh real consumers: Panel retained drawing, PanelContainer content layout, and box/grid separation constants. They preserve existing scene owner/capture/lifetime rules.
6. Removal, replacement and disposal release subscriptions and cached resource references according to ownership. Theme resource copying uses the existing Resource graph and exact factory mechanism; nodes borrow external theme/font/style/texture resources.

## Consumers and dependencies

[Panel](../classes/Panel.md) draws the inherited `panel` style without imposing padding on children. [PanelContainer](../classes/PanelContainer.md) combines style minimum and child bounds, draws the same key and fits eligible children inside the resolved content area. Box/HBox/VBox and Grid separation values consume actual typed constant lookup and local overrides, retaining the default gap four.

ThemeDB supplies actual default entries for panel/container, horizontal/vertical slider, Label, button/check and scroll consumers. The slider families share three styles and grabber-state icons, with an axis-specific tick icon; default assets use scale one. Scrollbar orientations share three grabber styles, the existing control focus style and one live zero-size image across all arrow-state slots; their tracks have separate axis margins. ScrollContainer adds an empty panel, a separate expanded focus border and two exact SVG scroll hints. Padding and separation constants absent from the source catalog retain zero fallback rather than becoming invented stored entries. It does not claim a full default asset catalog for absent GUI families. GetProjectTheme is absent until a Theme resource loader and startup project configuration provide real loaded state. The built-in DefaultFont and initial FallbackFont share an owned embedded Open Sans SemiBold resource with deferred native loading. Font slots/defaults, inherited lookup and local overrides borrow actual Font resources; replacements invalidate text consumers through the same scene notification path. The font implementation supplies shaping and glyph drawing under ADR 0046.

## Verification and limits

[ItemList](../classes/ItemList.md) now consumes a registered panel, row-state styles, font colors, spacing constants and the shared vertical scroll hint. ItemListTests checks representative exact built-in defaults and verifies the list uses them in measured and rendered rows.

[ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. [ThemeFontTests](../../tests/Electron2D.Tests/ThemeFontTests.cs) adds all font-category operations, default/item alias subscriptions, deep copying, merge/clear, background bulk suppression, owner lifetime and typed override packing. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified. Existing renderer and platform limits remain in [canvas rendering](canvas-rendering.md) and [ADR 0021](../decisions/product.md#adr-0021).
