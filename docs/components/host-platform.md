# Host identity and runtime features

Last updated: 2026-10-21

## Scope and ownership

[`OS`](../classes/OS.md) owns process-wide platform, CPU, operating-system locale and sandbox observations. Its retained service caches immutable identity metadata and reuses the environment gate for live locale/sandbox reads. [`ProjectSettingsRegistry`](../classes/ProjectSettingsRegistry.md) uses the same host feature vocabulary. [`RenderingServer`](../classes/RenderingServer.md), CanvasBackend and GPUCanvasBackend own immutable session compression observations; NativeSystemFonts supplies the optional host-font capability. No new service identity or public Instance accessor is introduced.

## Runtime flow

GetName reports Windows/Linux/macOS/Android/iOS/tvOS/Web. Logical CPU count comes from the platform; it can differ from process scheduling capacity. CPU brand data is cached from CPUID where available, otherwise Linux metadata may supply it. Distribution/version queries parse bounded optional Linux metadata with exact keys; empty values retain fallback behavior. GetVersion falls back to the kernel build text on Linux. Missing optional input does not imply a fabricated hardware model.

GetLocale reads current LANG on Linux/FreeBSD, strips the encoding suffix and uses en when the variable is absent. Other hosts use the native preferred locale pointer array, which is freed deterministically. GetLocaleLanguage returns the first language component. These cold queries leave application Culture/catalogs unchanged. Linux sandbox checks use Flatpak/container markers and all of SNAP/SNAP_NAME/SNAP_REVISION; one or two unrelated variables are insufficient. Other sandbox profiles throw until their actual adapters exist.

Host/build/architecture tags are shared with typed settings, including pc/mobile, x86/arm families, pointer bitness, single precision, template configuration and thread profile. Query matching follows the existing case-insensitive typed feature policy. Custom tags are live and runtime-managed; saving settings persists their typed override values, not an implicit activation. OS.HasFeature adds live movie, system-font and renderer observations. Project default overrides use host/custom tags; explicit supplied feature lists preserve their own selection context.

GPU renderer startup captures complete s3tc/rgtc/bptc family support for ordinary native uploads. Compatibility reports no such native upload family and uses existing decoded image input. OS.HasFeature returns false for renderer tags before startup/after shutdown. RenderingServer.HasOSFeature requires an active service, reads immutable data on any thread and returns false for unknown names. A capability query never opens a renderer. Optional system-font discovery can initialize its existing provider on first use.

## Invariants and errors

Host metadata and capability preparation are cold. Prepared immutable identity and feature queries reuse data; live environment queries may allocate. Null feature names throw ArgumentNullException; unknown/empty/malformed names return false. Native service shutdown removes its capability snapshot. Environment edits remain process-local and synchronized through OS. No shell subprocess is used to read host identity. Optional file reads have fixed byte budgets; parser results never execute shell syntax.

## Verification and limits

`tests/HostPlatform.Consumer` uses only public runtime API. Managed mode checks Linux metadata, host/custom feature selection, persistence boundaries, LANG isolation and Snap combinations. `gpu OUTPUT` and `compatibility OUTPUT` select BC1 or RGBA input, render red pixels and save a native framebuffer. Root `ELECTRON2D_TEST_HOST_PLATFORM=1` links the same checks and adds exact-key/quote/malformed metadata fixtures. JIT, strict NativeAOT, both Linux display protocols and both renderer backends are separate observations. Warm queries measure owner-managed bytes only. Complete foreign version/model/sandbox and simulator/browser feature adapters, physical GPU, native allocation, editor and human acceptance remain open.

## Decisions

[ADR0113](../decisions/host-platform.md#adr-0113), [ADR0019](../decisions/core-data-io.md#adr-0019), [ADR0021](../decisions/product.md#adr-0021), [ADR0028](../decisions/rendering.md#adr-0028), [ADR0095](../decisions/singleton-services.md#adr-0095).
