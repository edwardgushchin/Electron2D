# Electron2D platform packages

Install `Electron2D` for the managed C# runtime, then add `Electron2D.Windows`, `Electron2D.Linux`, `Electron2D.MacOS`, `Electron2D.Android`, `Electron2D.iOS`, `Electron2D.tvOS` or `Electron2D.Web` for your game targets. Each platform package contains its audited native files and selects the matching SDL, image, shader and font dependencies. It contains no managed backend assembly.

For example, a Linux game uses:

```bash
dotnet add package Electron2D --prerelease
dotnet add package Electron2D.Linux --prerelease
```

The platform package version is the minimum supported Electron2D version, including its prerelease suffix. `Electron2D.Linux` version `0.2.0-alpha` therefore requires `Electron2D` version `0.2.0-alpha` or later. NuGet records and enforces that minimum dependency; a version of SDL, FreeType or OpenSSL is not the version of the Electron2D platform package. Use matching product/platform release versions unless a different combination has been verified.

Publish for a specific RID to include only its native assets under `runtimes/<RID>/native`. Platform package installation does not establish target execution or complete game hosting; refer to the repository platform report for current verification limits. Native archives for Apple and Web still follow their SDK's static-link rules.

Native producers keep source receipts, binary SHA-256 values, architecture/loader/export checks and bundled notices. The maintainer source-build mode and CI prepare packages; game developers do not compile the native backends.

The WebP bridge producer in `tools/webp-native` statically links the pinned mature codec for exact/lossy saving, static decoding and first-frame animation reconstruction. Source builds and native receipts require `Electron2DWebP` on desktop/Android and its bridge/codec/demux archives on Apple device/browser profiles. Its engine entry points are `e2d_webp_encode`, `e2d_webp_decode` and `e2d_webp_memory`; older bundles reject before codec use. Utility and SharpYUV allocations use one immutable caller allocator through generated, hash-checked source overrides. Linux execution is verified separately from foreign targets and publication.

Desktop packages also contain the private WebRTC data-channel bridge and dependency license/source-access notices. The bridge statically links pinned libdatachannel/libjuice/usrsctp/plog and exports only its 24 engine functions. Desktop producers link the shared pinned OpenSSL 4.1.0 pair. Linux preserves major-4 SONAMEs and relative `$ORIGIN` lookup for the .NET PAL; the process must select that same provider before cryptography (see docs/components/tls.md#unified-openssl-source-profile). Linux source production requires patchelf. A browser-hosted engine adapter and mobile/static-Apple WebRTC assets require their own integration.

The private Electron2DRender bridge supplies native renderer allocation diagnostics from the pinned SDL core. Matching source and runtime ABI checks precede opaque Vulkan access; other GPU adapters reject explicitly. Its generated declarations retain the existing SDL notice. See [the producer](../render-native/README.md).
