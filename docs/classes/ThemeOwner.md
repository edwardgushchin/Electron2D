# ThemeOwner

Last updated: 2026-09-27

**Declaration:** `internal sealed class ThemeOwner : IDisposable` · **Source:** [ThemeOwner.cs](../../src/Scene/Theme/ThemeOwner.cs) · **Component:** [Typed themes](../components/themes.md)

Shared implementation for the public [Control](Control.md) and [Window](Window.md) theme contracts. It is not an additional public API or a scene node. Each supported node creates one helper lazily and supplies its existing read/mutation guards.

## Responsibilities and flow

- Retains the borrowed local Theme, variation name, five typed override/cache stores and native class ancestry.
- Validates lookup keys, selects the eligible local override and variation/type dependency order, searches consecutive Control/Window owner themes, then built-in defaults and category fallback.
- Rejects active dependency-chain cycles with InvalidOperationException. A neutral Node terminates branch inheritance.
- Connects Theme/global context changes to generation-specific deferred actions. Detached resource changes set an atomic invalidation flag, and detached queries bypass value caches so ancestor/global edits remain immediately visible; attached work returns to the scene owner thread. Exit/reentry prevents stale actions from touching a new membership.
- Snapshots live consecutive theme-owner descendants for propagation, continues required callbacks after failures, and bounds nonsettling reentrant propagation to 64 passes.
- Shares one Changed/Disposed subscription per distinct overridden resource using alias reference counts. Own-resource override changes refresh synchronously on the owner thread and defer otherwise.
- Produces typed override property descriptors for scene packing; null descriptor values remove overrides. Its bounded StoredOverride resolver can recreate an absent fresh-target descriptor only for Control/Window, the five reserved prefixes and exact value types under ADR 0023; it never reuses source-owner delegates. Disposing the helper releases subscriptions, maps and caches without disposing external Theme/Texture/StyleBox resources.

Notification dispatch belongs to the public node. It invokes ThemeChanged before clearing caches and then performs the required redraw/minimum/reflow work. Local overrides are checked before the query cache, while an event can still observe a previously cached inherited/resource result. The final invalidation is attempted even if callbacks fail.

## Nested typed store

<a id="store"></a>
`internal sealed class Store<T>` keeps a category's ordinal override dictionary, `(name, type)` resolved-value cache, typed Has/Get/fallback delegates and a resource-category flag. Instantiations are Color, int Constant, int FontSize, nullable Texture and nullable StyleBox. It avoids a Variant/object-valued public or internal item facade while sharing the same owner traversal.

## Invariants, threading and verification

Scene queries and mutations use the owning node's guards. Resource callbacks enqueue work or set an atomic invalidation flag; they do not traverse/mutate attached scene state from a worker. Cached actions, type scratch lists, visited sets and propagation snapshots are reused after preparation; first-use keys and later capacity growth may allocate. Font resources and project Theme loading remain exact separate dependencies.

[ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified. See [ADR 0083](../decisions/rendering.md#adr-0083) and the public owner class pages for supported signatures and observable behavior.

A cache-generation check prevents a virtual Theme getter that replaces owner state from repopulating a cache invalidated during that query. A separate cached override-resource action rechecks bulk suppression at owner-thread delivery, so a queued worker callback cannot publish inside a later active bulk block. Same-owner notification reentry and branch propagation both use bounded settling loops; no recursive notification stack is required.
