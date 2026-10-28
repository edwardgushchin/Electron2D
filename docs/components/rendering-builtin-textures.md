# Built-in rendering textures

RenderingServer.GetTestTexture and GetWhiteTexture lazily create ordinary renderer-owned RGB8 textures without mipmaps. The test image is 256 by 256: red falls by the integer half-sum of the coordinates, green/blue carry x/y, and the first two rows/columns of each 32-pixel cell swap the channels into a calibration grid. The white image is 4 by 4 with all channels 255. Both use the existing texture RID registry, copied backing-image queries, retained canvas drawing, shader sampler bindings, proxy redirection and native upload/cache owners.

## Cache and ownership

Calls require the active renderer owner. First use is allocation-capable texture authoring and retains its submission/shutdown guard; warm calls are observations and may execute during canvas recording. Each image keeps one independent cached identity per renderer session. Neither image is a process-wide native handle or the internal 1-by-1 fallback white texture.

The returned RID retains ordinary owned-texture behavior. Updates, size/path metadata and destination replacement remain visible through the same cached identity. If FreeRID or source consumption during TextureReplace retires that identity, the common retirement path clears its cache slot; the next getter creates a fresh identity with the original image. Dependent proxies follow ordinary replacement/redirection and missing-source behavior. Named shader bindings retain ordinary resource lifetime: rebind a sampler when its source RID is consumed or freed. Shutdown releases any cached textures alongside other owned images. A later renderer never reuses their identities. Texture2DGet returns independent images; editing them cannot mutate the cache.

## Verification

Build the Release linux-x64 test DLL and run ELECTRON2D_TEST_BUILTIN_TEXTURES=gpu, compatibility and software (the latter with dummy SDL video and the software render driver). The profile verifies source format/size, grid/gradient/boundary bytes, copied-image isolation, real canvas/proxy pixels, named GPU shader sampling, retained updates, replacement with a cached source, explicit retirement/recreation and two renderer sessions. Cold and warm owner rejection are distinct. The software profile selects its supported Nearest texture filter. After preparation it checks 64 active plus 64 idle full frames for zero owner-managed bytes. Evidence belongs under bin/rendering-builtin-textures/2026-10-29; executing results and foreign/native-heap qualification must be recorded separately.
