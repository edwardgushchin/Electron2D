# Electron2D identity design

Last updated: 2026-10-05

Status: **Sprite is the approved Electron2D identity**, selected by the owner on 2026-10-02 from concept 02.8. The Code direction has been removed from active designs, source assets and the delivery package.

The identity presents Electron2D as an **agent-native, cross-platform 2D game engine**, following [ADR 0090](../decisions/agent-native.md#adr-0090) and the product scope in [ADR 0004](../decisions/product.md#adr-0004). This is the primary positioning expressed by the descriptor `Agent-native cross-platform 2D game engine`. C# and typed APIs are implementation details under [ADR 0001](../decisions/product.md#adr-0001). The descriptor does not establish completed platform delivery or implemented editor or agent tooling. These assets do not change the runtime architecture.

## Editable designs

The local Penpot file is **Electron2D — Logo**. The approved page is the source of truth.

- [Identity · Sprite](http://localhost:9001/#/workspace?team-id=50386cfd-724b-8089-8008-929cf6ab0402&file-id=25be5f50-4fa1-80d7-8008-b98e1f25e65f&page-id=7d6ffda9-e5e3-8008-8008-ba5db39b4086)

The page contains seven 1440×1000 boards, including the editor startup composition. Its logo library contains Primary light, Primary dark and Mark shaded. The monochrome specimens were removed by the owner on 2026-10-05. Localhost links require the local Penpot instance.

## Name and logo geometry

Use the spelling **Electron2D**. The primary lockup has a mark, wordmark and descriptor. The compact lockup omits the descriptor; the stacked lockup places the mark above the wordmark.

The current shaded character has symmetric hollow ears, internal highlights, large eyes with light pupils, separate blush pixels and an open mouth. The same character colors and geometry serve both background variants. Light/dark lockups change the lettering to fit their background. There is no monochrome version or external white outline.

Primary and compact SVGs preserve the current Penpot composition, including visible text alignment. The stacked communication lockup preserves the editor board's character, two open pixel sparkles, wordmark and descriptor, with a transparent cropped canvas. Clear space remains H/4; the nominal mark/name gap is about 0.43H. Preserve the supplied exported composition rather than recalculating it from text rectangles.

Penpot text remains editable in IBM Plex Sans SemiBold (600) and Regular (400). Repository communication SVGs contain glyph outlines generated from those fonts with their shaping and export positions, so image consumers need no installed fonts. Letter paths are not drawn manually. The editor instead renders real Label nodes with bundled fonts; see [editor startup](../components/editor-startup.md).

| Asset | Minimum screen size | Use |
| --- | --- | --- |
| Primary logo | 360 px wide | Wide headers and communication |
| Compact logo | 160 px wide | Narrow navigation and cards |
| Individual mark | 16 px canvas | Favicons and compact identifiers |

Check the final rendered size; these are design minima rather than a substitute for visual review. Do not stretch, rotate, add a stroke or shadow, move the descriptor, or change the expression of the Sprite logo. For complex images, place the logo on a plain surface. The suffix `dark` means a logo for a dark background, `light` for a light background; SVG backgrounds are transparent.

## Sprite identity

Sprite uses a berry pixel character, warm light backgrounds and dark plum text. Headings and the wordmark use IBM Plex Sans SemiBold. Code samples use JetBrains Mono. The main logo has a fixed expression; an illustrated wink is an additional communication element.

| Role | HEX | Application |
| --- | --- | --- |
| Paper and main background | `#F9F3EE` | Warm light background |
| Surface | `#FFFFFF` | Light cards |
| Ink | `#3D2749` | Primary text |
| Brand and Action | `#A63B75` | Light-background wordmark accent, links and buttons |
| Muted | `#7C667A` | Secondary light-theme text |
| Line | `#DAC9D5` | Decorative separators |
| Warm | `#EDA181` | Communication illustrations |
| Dark | `#241B2C` | Alternate dark background |
| BrandDark | `#F2A6CC` | Dark-background wordmark accent and sparkles |
| FaceDark | `#3D2749` | Eyes and mouth |

Normal/hover/pressed action fills are `#A63B75` / `#8D3063` / `#772550`, with white labels. Focus uses a 2 px outline with a 2 px offset: berry on light and light pink on dark. Warm and Line are decorative colors, not small-text colors or required input boundaries.

The shaded character palette is shared by both versions:

| Role | HEX |
| --- | --- |
| Outer edge | `#D65D96` |
| Face | `#FFADCF` |
| Highlights | `#FFE4EE` |
| Inner ears and mouth detail | `#E872A6` |
| Lower face shading | `#F18BB8` |
| Bottom shadow | `#B94780` |
| Eyes and mouth | `#3D2749` |
| Eye pupils | `#FFF9F3` |
| Blush | `#E86C9F` |

The JSON and CSS tokens include this palette alongside existing interface roles. Pixel shapes remain square. Icons use nearest-neighbor scaling from the canonical mark. Check the final small size because highlights can disappear during reduction. The layout grid remains 8 px with 16/24/32/48 px intervals and 64 px outer margins on the boards.

## Typography and accessible application

| Role | Typeface | Screen size and line height |
| --- | --- | --- |
| Headings | IBM Plex Sans SemiBold | 48 / 32 / 24 px sizes |
| Body | IBM Plex Sans Regular | 16 / 24 px |
| Caption | IBM Plex Sans Regular | 12 / 16 px |
| Code | JetBrains Mono Regular | At least 14 px |
| Wordmark | IBM Plex Sans SemiBold | Fixed outlined asset |
| Logo descriptor | IBM Plex Sans Regular | Editable source, outlined communication asset |

Text and essential control boundaries must retain readable contrast. Use at least 4.5:1 for normal text and 3:1 for large text or essential non-text controls, following [WCAG 2.2](https://www.w3.org/TR/WCAG22/). Decorative Line colors do not establish compliant control boundaries.

| Functional pair | Contrast |
| --- | --- |
| Primary text on main background | 12.04:1 |
| Secondary text on main background | 4.73:1 |
| Brand accent on main background | 5.45:1 |
| White button label on normal action fill | 5.99:1 |
| Pink accent on dark background | 8.75:1 |
| Plum eyes on shaded face (#FFADCF) | 7.65:1 |

Ratios use relative sRGB luminance for the specified HEX colors. Recalculate for a different background, opacity or state. Links inside prose also have an underline. Errors include explanatory text and an icon. Use a target area of at least 44×44 px and visible keyboard focus. On narrow screens, use one column, 24 px outer margins, 32 px primary headings and a compact logo or mark as space requires. Decoration is reduced before content.

## Assets and communication

Version-controlled sources are under [assets/sprite](assets/sprite): primary, compact and stacked logo SVGs, light/dark copies of the same colored mark, the app icon, open pixel sparkle and JSON/CSS tokens. The four monochrome SVGs are removed. Token delivery does not integrate a runtime theme.

The [1280×640 GitHub social preview](assets/sprite/github-social-preview.png) and its [editable SVG source](assets/sprite/github-social-preview.svg) use the refreshed primary logo. Committing this PNG does not configure the remote GitHub social preview; that requires uploading it in repository settings.

Desktop identity uses app-icon.svg without text: Linux registers a PNG rasterization with an absolute file path, Windows embeds the refreshed [ICO](../../editor/Assets/Electron2D.ico), and macOS packages the refreshed [ICNS](../../editor/Assets/Electron2D.icns). The rounded [app-icon.svg](assets/sprite/app-icon.svg) is the desktop application identity. Raster containers are generated from this SVG at each icon size; editor build/publish needs no graphics-conversion tool.

The five README editions share the refreshed primary and compact SVGs through their existing light/dark picture sources. Their logo alternative text includes the current descriptor and the primary image reserves a 640×148 slot. Their prose, language navigation and dynamic status badges retain their existing meaning.

The eight README section icons were approved by the owner on 2026-10-05. Their editable source is the [README icon page in Penpot](http://localhost:9001/#/workspace?team-id=50386cfd-724b-8089-8008-929cf6ab0402&file-id=25be5f50-4fa1-80d7-8008-b98e1f25e65f&page-id=b500948f-af59-8076-8008-be692d9942c6). SVG exports from the canonical 24×24 designs are `readme-about.svg`, `readme-features.svg`, `readme-quick-start.svg`, `readme-platforms.svg`, `readme-development.svg`, `readme-documentation.svg`, `readme-contributing.svg` and `readme-license.svg` under `assets/sprite`. They use the shaded Sprite palette, square pixels and transparent backgrounds; the same colored icons serve both themes. Their viewboxes crop transparent margins to the visible artwork, so a 24 px heading icon occupies the same visible size as an ordinary emoji. SVG crisp-edge rendering preserves the pixel edges; heading images use text-top alignment to avoid dropping below the visible letters; the smaller invitation sparkle retains middle alignment. Render them at 24 px in headings and the sparkle at 16 px beside the GitHub star invitation. The open sparkle also replaces the emoji beside the GitHub star invitation. These icons are decorative and use empty alternative text because the adjacent text carries the meaning. Heading text and existing navigation anchors are preserved in all five editions.

Voice is concise, warm and factual. Invitations such as “Соберите первую сцену” are appropriate; API documentation and error messages use precise terms and actionable explanations. State actual supported features in product copy. Website, scene, documentation and social examples are visual mockups, not screenshots of an implemented product.

Fonts: [JetBrains Mono](https://www.jetbrains.com/lp/mono/) and [IBM Plex](https://github.com/IBM/plex/blob/master/LICENSE.txt), both under SIL Open Font License 1.1. License copies are under [assets/licenses](assets/licenses). Preserve them when distributing the font files.

## Verification boundary

The current Penpot file validates with zero issues. Native reference exports were compared with repository SVG rasterization, including symmetric ears, typography, alignment and transparent backgrounds. SVG/XML, tokens, icon contents and local README references are checked separately. The editor's real rendering checks and platform limits are documented in [editor startup](../components/editor-startup.md).

Graphic verification does not establish interactive website behavior, other desktop host acceptance or print-profile accuracy.

## README badges

The five README editions use dynamic Shields badges for the remote .NET target, license, latest release, main commit, and aggregate Build/Tests checks across all 18 RIDs. One CI workflow prepares native packages once for both matrices. The two check-run badges select the exact `Build` and `Tests` names on `main`; both link to that workflow. Localized labels and alt text are retained. At 28 px height, each image preserves its natural aspect ratio. The duplicate draft README and the former local static status SVGs are removed.

Remote status is separate from local verification. The single-workflow configuration is prepared locally and requires its first GitHub execution. The preceding two-workflow run at `9688465b` failed macOS FreeType compilation because a generated libpng header was not on the include path; its fix is included in the candidate. Dynamic check-run badges can report no result before their checks exist, and published output is cache-delayed. Local builds/tests are not a CI-success claim. [Platform verification](../platform-verification.md#automated-rid-checks) records the exact contract and remaining native/hardware gates.

Local HTML asset/anchor checks and the workflow configuration are verified. Final GitHub rendering, the first remote test run and owner acceptance remain separate. The earlier local SVG inspection applies only to the retired static artwork, not to the final remote badge pixels.
