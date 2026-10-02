# Electron2D identity design

Last updated: 2026-10-02

Status: **Sprite is the approved Electron2D identity**, selected by the owner on 2026-10-02 from concept 02.8. The Code direction has been removed from active designs, source assets and the delivery package.

The identity represents a typed C# 2D engine under [ADR 0001 and ADR 0004](../decisions/product.md) and the product direction in [ADR 0090](../decisions/agent-native.md#adr-0090). The existing descriptor `Agent-native cross-platform 2D game engine` expresses that direction; it does not establish completed platform delivery or implemented editor or agent tooling. These assets do not change the runtime architecture.

## Editable designs

The local Penpot file is **Electron2D — Logo**. Earlier exploration remains as historical reference; the approved page is the source of truth.

- [Identity · Sprite](http://localhost:9001/#/workspace?team-id=50386cfd-724b-8089-8008-929cf6ab0402&file-id=25be5f50-4fa1-80d7-8008-b98e1f25e65f&page-id=7d6ffda9-e5e3-8008-8008-ba5db39b4086)

The approved page contains six 1440×1000 boards: primary image, logo system, colors and typography, graphics, website and interface, and communication examples. Its library contains 13 color roles, four typography presets and two primary logo components. Localhost links require the local Penpot instance.

## Name and logo geometry

Use the spelling **Electron2D**. The primary lockup has a mark, wordmark and descriptor. The compact lockup omits the descriptor; the stacked lockup places the mark above the wordmark.

Let H be the visible height of the mark. The combined visible height of wordmark and descriptor equals H. The horizontal gap is **0.43H**, and clear space on all sides is **H/4**. Preserve the supplied geometry instead of setting the text again. The vector wordmark is outlined, so it does not depend on installed fonts.

All versions use one master silhouette and the same facial geometry: two eyes, two cheeks and one mouth. Version 1.2 uses a broad head with a flat base and short corner steps. Cheeks are separate internal pixels; every facial detail is surrounded by the silhouette, including in monochrome. Monochrome converts all five details to transparent cutouts; it does not remove or reshape them. Primary and compact lockups share the same outlined wordmark and gap. The primary light, dark and monochrome specimens on the logo-system board use the same H; compact specimens also share a size. Scale a complete supplied asset instead of rebuilding its parts.

| Asset | Minimum screen size | Use |
| --- | --- | --- |
| Primary logo | 360 px wide | Wide headers and communication |
| Compact logo | 160 px wide | Narrow navigation and cards |
| Individual mark | 16 px canvas | Favicons and compact identifiers |

Check the final rendered size; these are design minima rather than a substitute for visual review. At 16 px, a monochrome mark is preferred. Do not stretch, rotate, add a stroke or shadow, move the descriptor, or change the expression of the Sprite logo. For complex images, place the logo on a plain surface. The suffix `dark` means a logo for a dark background, `light` for a light background; SVG backgrounds are transparent.

## Sprite identity

Sprite uses a berry pixel character, warm light backgrounds and dark plum text. Headings and the wordmark use IBM Plex Sans SemiBold. Code samples use JetBrains Mono. The main logo has a fixed expression; an illustrated wink is an additional communication element.

| Role | HEX | Application |
| --- | --- | --- |
| Paper and main background | `#F9F3EE` | Warm light background |
| Surface | `#FFFFFF` | Light cards |
| Ink | `#3D2749` | Primary text |
| Brand and Action | `#A63B75` | Mark, links and buttons |
| Muted | `#7C667A` | Secondary light-theme text |
| Line | `#DAC9D5` | Decorative separators |
| Warm | `#EDA181` | Light-version character details and illustrations |
| Dark | `#241B2C` | Alternate dark background |
| BrandDark | `#F2A6CC` | Mark and accent on dark backgrounds |
| FaceDark | `#3D2749` | Eyes, cheeks and mouth on the pink dark-version mark |

Normal/hover/pressed action fills are `#A63B75` / `#8D3063` / `#772550`, with white labels. Focus uses a 2 px outline with a 2 px offset: berry on light and light pink on dark. Warm and Line are decorative colors, not small-text colors or required input boundaries.

The dark version uses a pink silhouette and plum facial details. Their contrast is **7.01:1**; the former peach-on-pink pair was only 1.11:1. Keep the two eyes, cheeks and mouth distinct. Peach remains part of the light version.

The master mark uses an 8-unit smallest module. Raster output places each module on whole pixels and scales with nearest neighbor. The 24 px PNG uses extra canvas space to preserve that grid; do not trim it. The layout grid is 8 px, with main intervals 16/24/32/48 px and a 64 px outer margin on the 1440 px boards. Cards may use an 8 px radius; individual pixels remain square.

## Typography and accessible application

| Role | Typeface | Screen size and line height |
| --- | --- | --- |
| Headings | IBM Plex Sans SemiBold | 48 / 32 / 24 px sizes |
| Body | IBM Plex Sans Regular | 16 / 24 px |
| Caption | IBM Plex Sans Regular | 12 / 16 px |
| Code | JetBrains Mono Regular | At least 14 px |
| Wordmark | IBM Plex Sans SemiBold | Fixed outlined asset |
| Logo descriptor | IBM Plex Sans Medium | Fixed outlined asset |

Text and essential control boundaries must retain readable contrast. Use at least 4.5:1 for normal text and 3:1 for large text or essential non-text controls, following [WCAG 2.2](https://www.w3.org/TR/WCAG22/). Decorative Line colors do not establish compliant control boundaries.

| Functional pair | Contrast |
| --- | --- |
| Primary text on main background | 12.04:1 |
| Secondary text on main background | 4.73:1 |
| Brand accent on main background | 5.45:1 |
| White button label on normal action fill | 5.99:1 |
| Pink silhouette on dark background | 8.75:1 |
| Plum facial details on pink silhouette | 7.01:1 |

Ratios use relative sRGB luminance for the specified HEX colors. Recalculate for a different background, opacity or state. Links inside prose also have an underline. Errors include explanatory text and an icon. Use a target area of at least 44×44 px and visible keyboard focus. On narrow screens, use one column, 24 px outer margins, 32 px primary headings and a compact logo or mark as space requires. Decoration is reduced before content.

## Assets and communication

Version-controlled sources are under [assets/sprite](assets/sprite): primary, compact, stacked and monochrome logo SVGs; color and monochrome mark SVGs; app icon SVGs; and JSON/CSS tokens. Keep the token file together with the Sprite assets. There is no runtime theme integration in this change.

The delivery package also contains transparent PNG marks at 16, 24, 32, 48, 64, 128, 256 and 512 px, 512 px app icons, PNG logo exports, six board exports, and licensed font files. Numbered `icon-N.png` files use the light-background mark; use `mark-dark.svg` for dark surfaces. The app icon has its own rounded background. Social formats use 1200×630 and 1080×1080 px, with margins of at least 64 px.

Voice is concise, warm and factual. Invitations such as “Соберите первую сцену” are appropriate; API documentation and error messages use precise terms and actionable explanations. State actual supported features in product copy. Website, scene, documentation and social examples are visual mockups, not screenshots of an implemented product.

Fonts: [JetBrains Mono](https://www.jetbrains.com/lp/mono/) and [IBM Plex](https://github.com/IBM/plex/blob/master/LICENSE.txt), both under SIL Open Font License 1.1. License copies are under [assets/licenses](assets/licenses). Preserve them when distributing the font files.

## Verification boundary

The six board exports and final document pages were visually inspected. Penpot validation returned no issues. Source SVG/XML and tokens JSON are validated separately; contrast values are calculated rather than inferred from a screenshot. The accompanying design document includes the boards, expanded application rules and delivery guidance.

This verification covers design, exported graphics and document layout. It does not establish interactive website behavior, runtime rendering, native application acceptance or print-profile accuracy.
