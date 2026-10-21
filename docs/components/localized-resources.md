# Localized resource files

Last updated: 2026-10-21

## Scope and owned types

ResourceLoader selects locale-specific backing files and refreshes associated live resources. The public immutable [`ResourceLocaleRemap`](../classes/ResourceLocaleRemap.md) record projects the ordered `internationalization/locale/translation_remaps` project value through [`ProjectSettings.LocaleResourceRemaps`](../classes/ProjectSettings.md#localeresourceremaps). Internal [`ResourceLocaleMap`](../classes/ResourceLocaleMap.md) snapshots feed the existing ResourceLoadGraph. TranslationServer owns culture/scoring and refresh orchestration; ResourceLoader owns file work and weak associations.

## Selection and configuration

The permanent setting defaults to an empty copied array and allows at most 4096 nonnull records. Paths/locales are bounded and reject controls, malformed UTF-16 and subresource suffixes; source UID keys are rejected because caller UIDs resolve before matching. Immutable records and copied arrays support effective feature overrides and compiled JSON save/load under trimming/AOT.

Sources match exact logical paths. Highest positive CompareLocales score wins, first exact or last tied nonexact. Resolve the selected target UID, then use the original if the selected file is absent. A present malformed file fails during decode. Another language uses original data. Catalog fallback and domain locale overrides do not select files; candidates do not chain. Edits alone have no file side effect and affect subsequent loads or translation refresh.

## Logical identity and integration

Load/Exists/dependency inspection and rewrite select the backing file. Cache keys, ResourcePath, original archive UID and bundled subresource prefixes retain the logical source; GetResourceUID continues reading the original. A selected variant's UID never rebinds that source. Custom format hooks receive physical path and logical originalPath separately. Relative external archive dependencies resolve beside the physical selected file; discovery returns resolved fallback paths and rewrite accepts full resolved or literal relative tokens while preserving relative encoding and registered UID precedence.

Existing file access handles native/virtual paths and mounted packs. Public consumer checks execute PNG, bundled AtlasTexture/ImageTexture archives, relative external archives, ZIP images, WAV, FontFile and ShaderInclude data through actual format loaders. PackedScene saves external logical resource paths so a fresh process can choose locale-specific content at startup.

## Refresh, ownership and errors

After committing Culture/options and releasing the catalog lock, TranslationServer refreshes resources before current-MainLoop notification. Different-name Culture changes, every PseudolocalizationEnabled assignment, explicit ReloadPseudolocalization and project localization startup use this path. Equal-name Culture assignment retains the supplied CultureInfo without another refresh. Scene owner rejection precedes resource mutation; independent library trees keep explicit notification.

Weak associations include mapped Ignore/IgnoreDeep results and registered cached sources. A resource loaded with no mapping and never registered is not retroactively discoverable. Each active target prepares an independent IgnoreDeep graph and receives a compatible CopyFromResource, preserving its identity. Removal restores original data once and marks the weak association inactive; re-adding a map reactivates known instances. Inactive associations do not read files. Disposal/path changes prune entries; weak keys do not retain callers or their resources.

Bad preparation leaves prior target data intact. Copy callbacks can fail after data commitment; later resources and the application notification are still attempted, with aggregate errors reported. Completed copies and culture/options are not rolled back. Reentrant changes stop the obsolete pass and settle against the latest map/culture. Nesting and settling have 32-change guards; pathological observer loops fail explicitly. Actual refresh is cold, blocking, allocation-capable work. A prepared empty map without active associations retains zero owner-managed bytes; native allocation is not claimed.

## Threaded loading

A request captures map and culture at acceptance. Incompatible duplicate mapped cohorts are rejected. Culture or relevant map changes before collection reject stale mapped preparation before cache/UID mutation, release prepared resources and require a fresh request. An unrelated setting edit preserves the map identity. After publication, results enter weak live refresh. Reentrant culture changes during publication trigger a current refresh; the existing graph owns dependency transfer and cleanup.

## Verification

`tests/ResourceLocales.Consumer` is an independent public-API executable; its linked checks also run with `ELECTRON2D_TEST_RESOURCE_LOCALES=1` in the main test host. Default/explicit-root mode authors files and checks selection, cache/Ignore identity, one-change delivery, same-name Culture identity, malformed/missing files, observer failures, nested refresh, removal/reactivation, disposal during notification, weak retirement, archive UID/paths, delayed custom-loader stale culture/configuration and format families. `read ROOT` loads persisted settings in a fresh process. `author ROOT` saves a typed project and external-resource PackedScene; `gpu ROOT OUTPUT` and `compatibility ROOT OUTPUT` restore that project, assert data-before-notification and capture green/blue/original-red/green framebuffers.

Linux checks use software lavapipe/llvmpipe on Wayland and X11 with process driver maps, plus strict NativeAOT consumers. Existing resource archive/base/threaded, locale/catalog/project-settings checks cover the shared contracts. Hardware, foreign platforms, native allocation, performance under real game load, editor and human visual acceptance remain separate gates. Focused checks do not claim full-suite release readiness.

## Decisions

[ADR 0007](../decisions/localization.md#adr-0007) owns typed localization and ordering; [ADR 0013](../decisions/resources.md#adr-0013) owns resource identity and graph publication; [ADR 0014](../decisions/resources.md#adr-0014) distinguishes cold preparation from hot intervals; [ADR 0019](../decisions/core-data-io.md#adr-0019) owns typed persistence; [ADR 0090](../decisions/agent-native.md#adr-0090) separates saved authoring, headless checks and actual rendered output.
