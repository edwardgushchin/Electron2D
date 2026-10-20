# Loading images

Last updated: 2026-10-20

## Scope and choice

RenderingServer.SetBootImageWithStretch immediately displays a borrowed Image over a supplied background in the main native window. It executes on the existing GPU and Compatibility backends, independently of scene viewport sizing and the automatic render-loop switch.

This slice supplies the current method, SplashStretchMode and all six values: eight applicable declarations. It was selected over shared device texture views (three declarations needing native subresource/format aliases and retained view lifetime) and MissingNode (five declarations needing persistent scene signal endpoints, typed recording and missing-type factory/reference restoration). Existing image sampling and presentation make the loading workflow executable now. The pinned set_boot_image method is deprecated in favor of set_boot_image_with_stretch and remains excluded without an alias.

## Public workflow

```csharp
using var image = Image.CreateEmpty(64, 32, false, Image.Format.Rgba8);
image.Fill(Colors.Blue);
using var window = new Window { Size = new(640, 360) };
window.Ready += _ => RenderingServer.SetBootImageWithStretch(
    image, Colors.Black, RenderingServer.SplashStretchMode.Keep);
Engine.Run(window);
```

The existing native service is already available during readiness. Call outside scene rendering on its owner thread, for example before preparing additional loading work. This is explicit runtime presentation; no automatic project startup-image setting or additional loader is introduced. It does not pump native events or run scene work while the caller is occupied.

## Geometry, sampling and lifetime

The [six stretch modes](../classes/RenderingServer.SplashStretchMode.md) use native client pixels. Disabled retains image size and floors the centered origin. Keep selects client height in landscape and client width in portrait or square windows, preserving image aspect even when this crops. KeepWidth/KeepHeight select their named axis; Cover fills both axes with cropping; Ignore scales axes independently. Geometry overflow clips through the existing target/presentation path.

UseFilter defaults to true and selects actual linear interpolation. False selects nearest. The borrowed image is copied for the call and remains caller-owned; later source edits do not alter completed output. Transparent image samples blend over the background through the existing canvas source-alpha rule. Opaque native windows use opaque background alpha. Main-window Transparent configuration is passed at native creation; Compatibility preserves its alpha, strict GPU startup rejects and permitted fallback selects Compatibility. Live alpha changes reject without changing configured state. Image formats and filtering retain their existing native capability checks; the software SDL triangle driver rejects linear filtering and unsigned integer canvas images require their existing shader/filter adapter.

The operation creates temporary ordinary color targets and an image texture, submits and presents, then releases their native ownership before returning. The backend's normal presentation history retains readable final pixels. Pending particle simulation requests and ordinary scene cache-retirement bookkeeping are not consumed. Scene viewport images, node state, default scene clear color, rendering time, process/physics/drawn-frame counters and queued completed-frame requests are preserved. No FramePreDraw/FramePostDraw events run. Pending scene changes remain pending; the next normal submission replaces the splash. RenderLoopEnabled=false does not prevent this explicit operation.

Null or empty images preserve output. A disposed image, invalid enum, nonfinite background, foreign owner, absent service or reentrant rendering call fails explicitly. Nonpositive native client dimensions skip drawing. Unsupported image/filter/target capability and native allocation/submission errors propagate; temporary ownership and phase flags unwind, allowing a later valid presentation. The operation is cold image upload and native allocation work, not an allocation-free game-loop producer.

## Verification and limits

BootImageTests selects managed, gpu or compatibility through ELECTRON2D_TEST_BOOT_IMAGE. Native checks use fixed golden destination rectangles for wide/tall images in landscape, portrait and square clients, odd original-size centering, actual sampled pixels, default linear versus nearest filtering, copied opaque/transparent alpha data, strict GPU transparency cleanup/fallback/rollback, invalid/empty/owner/phase guards, temporary texture-cache retirement, first output before any scene target, pending particle-request isolation and later consumption, normal-scene/callback isolation, unsupported-format failure recovery and subsequent scene restoration. After explicit warmup, 32 normal scene frames after loading allocate zero managed owner bytes. Native readback observes presented pixels separately from unchanged scene viewport pixels.

Local renderer checks use software Vulkan/GL under Linux Wayland/X11. Cold boot allocations are allowed and native allocator totals are not measured. Physical GPU, foreign platforms, HDR native swapchains, minimized-window presentation and human acceptance remain separate checks; ordinary current color targets are used independently of a scene viewport's HDR/MSAA policy. No full-engine stability claim follows from these focused checks.

## Decisions

[ADR 0028](../decisions/rendering.md#adr-0028) owns native rendering, [ADR 0095](../decisions/singleton-services.md#adr-0095) owns session access, and [ADR 0040](../decisions/display.md#adr-0040) owns creation-time native window flags, and [ADR 0004](../decisions/product.md#adr-0004) excludes the deprecated predecessor. The existing scene, resource and native-window lifetime remains authoritative.
