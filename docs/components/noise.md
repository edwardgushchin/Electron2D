# Noise component

Last updated: 2026-10-11

## Scope and runtime flow

[Noise](../classes/Noise.md) provides the abstract 1D/2D sampler, scalar/vector dispatch and image conversion. [FastNoiseLite](../classes/FastNoiseLite.md) implements all applicable 2D algorithms/configuration with independent base/warp state. [NoiseTexture](../classes/NoiseTexture.md) turns a borrowed sampler and optional Gradient into a retained texture, including seamless images, color mapping, normal conversion and mipmaps.

A scene texture coalesces settings/source changes into the active SceneTree's deferred queue. The first generation runs synchronously in that callback; later generations compute on WorkerThreadPool. Image reads and renderer captures keep the last completed image while work waits. Completion commits pixels and emits Changed on the owner, outside the texture gate. Superseded input is discarded and current input rescheduled; failures preserve preceding pixels, retire work and surface on the owner. Corrected input can retry. The current browser profile keeps deferred synchronous computation until threaded bootstrap exists.

Without a running scene loop, image reads are the synchronous library-host boundary. Custom Noise image methods keep virtual dispatch and may run on workers; they must coordinate source state and cannot access scene/render objects off their owner. Built-in FastNoiseLite and Gradient inputs are copied into job-owned snapshots. The texture borrows the original resources; jobs retain any file graph until retirement. Recursive reads/disposal reject. Worker callbacks cannot dispose an unfinished generating texture before its logical transition; ordinary teardown drains owned work outside the texture gate.

A null source completes with a null GetImage result and keeps its last rendering allocation. Before any pixels exist, ordinary canvas sampling uses the renderer's diagnostic placeholder. Pixel copies belong to callers. Cold edits/generation allocate; unchanged and queued renderer reads reuse snapshots.

## Image semantics

Grayscale normalization retains float quantization for ordinary ranges and widens overflowing finite spans. Constant normalized values become zero before inversion. Seamless generation uses the float overlap extent and smoothstep, quadrant wrapping and packed byte blending. Copied RGBA quadrants retain source alpha; blended seams are opaque. Large overlap strips are clipped to output dimensions instead of writing outside their buffer. The default seamless path accepts L8 and RGBA8; custom generators may override the virtual method for another readable format. Image/rendering format support remains owned by those components.

Gradient color uses canonical RGBA8 Image truncation. Normal conversion follows mapping; mipmaps follow normal conversion. Non-renormalized byte-channel mip levels round their encoded 2-by-2 average, including odd extents and one-pixel axes. Source dimensions and absence of source mipmaps validate before publication. Invalid samples and formats fail explicitly. Three-dimensional samples/volumes remain outside the product boundary.

## Portable authoring

NoiseTexture and FastNoiseLite register compiled resource factories and store every authored setting through typed descriptors; generated pixels/jobs are transient. Gradient uses its existing schema. [ProceduralNoise](../../examples/ProceduralNoise/Program.cs) writes a resource and scene through the public API, then reloads two independent local instances. Shared texture aliases stay shared within one instance. Editing the first generator refreshes only its own pattern; disposing the template does not invalidate live instances. Loaded roots own their decoded graph while ordinary sources remain borrowed.

## Verification

NoiseTests, NoiseTextureTests and FastNoiseLiteTests cover scalar dispatch, defaults, invalid input, borrowed lifetime, copying and algorithm vectors. [NoiseGenerationTests](../../tests/Electron2D.Tests/NoiseGenerationTests.cs) covers first/deferred timing, worker execution, owner signals, retained queries, stale-input coalescing, failures/recovery, recursion and worker disposal guards, null rendering retention, task retirement, complete settings and fresh-process/local graphs. One thousand warmed pending renderer reads allocate zero managed bytes. Threaded file preparation/publication and loaded-root teardown exercise graph ownership.

[NoiseImageReferenceTests](../../tests/Electron2D.Tests/NoiseImageReferenceTests.cs) matches 122 independently executed pinned image/seamless/gradient/normal/mipmap fixtures plus 20 unchanged C++ RGBA-kernel fixtures. Development-only producers are [image_reference.gd](../../tools/noise/image_reference.gd) and [build_rgba_reference.py](../../tools/noise/build_rgba_reference.py). The latter adapts byte containers, not the template math; its fixture records the pinned header hash. Large-skirt safety tests cover the accepted clipped-write correction separately from unsafe reference inputs.

[NoiseGenerationRenderingTests](../../tests/Electron2D.Tests/NoiseGenerationRenderingTests.cs) reloads assets authored by another process. Linux Wayland GPU and compatibility each passed 48 actual rendered frames. Both worker lanes stayed occupied until frame 40; initial deferred pixels still published, and staged updates retained complete old images/pixels. Following release, the first pattern changed to the authored output while its independent sibling stayed unchanged. Twenty-five warmed pending frame intervals allocated zero managed owner bytes. Captures /tmp/e2d-noise-gpu.png and /tmp/e2d-noise-compatibility.png were inspected. Other platforms, browser threading, editor workflows, native allocation tracing, large-texture throughput and human acceptance remain separate gates.
