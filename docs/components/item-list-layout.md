# ItemList layout

Last updated: 2026-10-25

[ItemList](../classes/ItemList.md) measures text and icons before arranging rows and preparing their visible text. This connects automatic minimum sizes, fixed/equal column widths, wrapping, both owned scrollbars, RTL drawing and current-item visibility through the existing font/canvas backends. It adds no public declaration or backend dependency. Full keyboard/signal ordering and mutating selection callbacks remain a separate Partial family.

## Measurement and placement

Each retained item owns separate measurement and visible TextLayout buffers. Natural measurement is independent of the current viewport width. A positive FixedColumnWidth constrains measurement; outer horizontal separation is added afterward. Without that constraint, a long label retains its natural width even when the visible text is trimmed. AutoWidth/AutoHeight publish content and panel/bar extents through Control's ordinary deferred minimum-size path. They do not create a second sizing system or force an immediate detached resize.

Top-icon captions use the configured line-break flags and MaxTextLines. Each configured line reserves the theme line_separation, including for short text. Left-icon items retain one visible text line. The icon allocation follows region size, transposition, valid two-axis FixedIconSize and then finite IconScale; aspect fitting remains inside that allocation. Zero allocations remain empty. Negative scales retain signed icon quads and source reversal through the ordinary texture-drawing contract. Nonfinite derived measurements and extents reject.

Rows take their tallest item; SameColumnWidth uses the widest natural column, while fixed columns retain their content width plus separation. MaxColumns zero permits an unlimited row, subject to wrap fitting. An incomplete final row does not gain a fictitious final-column expansion. GetItemRect remains in panel-offset content coordinates; scrolling and RTL drawing do not mutate those authored rectangles. Expanded hit/background rectangles do not move the icon/text content within a column.

Visible text is prepared after fitting with the actual content allocation, preserving overrun, language, direction and translation policy. A zero allocation uses a positive trim width rather than accidentally selecting the text service's unconstrained-width role. RTL mirrors icons, captions and cursor rectangles with the panel's asymmetric margins; left-icon captions align to their right-hand allocation.

## Scroll and invalidation

Vertical and horizontal overflow consume each other's actual viewport page. Wrap fitting accounts for the vertical bar; nonwrapping content retains its horizontal extent. RTL keeps the signed negative horizontal range. CenterOnCurrent centers either selected axis immediately. EnsureCurrentIsVisible queues both-axis adjustment for drawing, including RTL. Enlargement clears obsolete ranges and scroll values. Hint modes and tiled/stretched hint textures use the existing draw path.

Failed measurement stays dirty for retry. A virtual measurement cannot recursively query an incomplete layout or silently publish geometry after changing its inputs. Prepared geometry is available to scrollbar clamp observers before range publication invokes them. Broader mutating signal reentry remains outside this completed family.

Borrowed icon changes/disposal may arrive from a worker. Callbacks only set an atomic pending flag and invalidate retained canvas recording. The scene owner removes disposed icon edges, updates subscriptions and rebuilds measurement; GetItemIcon observes a disposed source as absent. Layout operations retain owner/capture/disposal checks. Replacing or destroying a list never destroys borrowed textures.

The natural sizing contract follows pinned `scene/gui/item_list.cpp` and `doc/classes/ItemList.xml` at `ed1daf0bf001b61586d9930840f2f1394092c079`; attribution uses [the existing MIT adaptation notice](../../licence/CanvasStyleGeometry-LICENSE.txt). Portable font metrics, separate retained visible shaping, finite/reentry guards and coherent cross-axis pages are engine-owned. There is no claim of identical default-font pixels or identical internal vertex/layout ordering.

## Verification

[ItemListLayoutTests](../../tests/Electron2D.Tests/ItemListLayoutTests.cs) starts with the observed regression: a long label measured32 pixels rather than the public font contract's220. Its960 icon-grid combinations cover maximum columns, equal/fixed widths, fixed icon allocations, negative/zero/fractional/positive scale, transposition, both icon modes and LTR/RTL. Text tests cover natural width, capped wrapping and reserved spacing. Further checks cover coupled pages, signed centering, empty/enlarged models, failed/reentrant/mutating measurement, finite overflow, worker icon changes/disposal and typed file reconstruction. Existing ItemList model/input/theme/packing tests now use the public SubViewport role.

The [public-only consumer](../../tests/ItemList.Consumer/Program.cs) runs these same contracts. Its `author PATH` and `read PATH` modes verify typed scene files in separate processes. Passing `gpu` or `compatibility` runs [native layout checks](../../tests/Electron2D.Tests/ItemListLayoutRenderingTests.cs) through the ordinary Engine.Run loop: automatic caption growth, LTR/RTL icons and exact hit regions, negative scale, RTL caption alignment, queued two-axis visibility, and stretched/tiled hint pixels. After warmup it measures64 complete active frames while changing text/icon column widths, including deferred minimum-size work and native replay. The fixture does not reenter SceneTree flushing from lifecycle callbacks.

Qualification uses Linux Wayland/X11 software Vulkan and OpenGL compatibility, ordinary CLR and strict NativeAOT with full trimming and warnings as errors. Exact commands, process maps, readbacks, source hashes and raw outcomes are retained under ignored `bin/item-list-layout/2026-10-25/`. Managed owner-byte measurements do not establish native heap totals, physical GPU performance, foreign-platform execution or owner visual acceptance.
