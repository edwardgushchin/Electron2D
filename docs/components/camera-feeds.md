# Camera pixel feeds

Last updated: 2026-10-27

[ADR 0115](../decisions/camera-feeds.md#adr-0115) owns the camera source, registry and
texture contract. This component connects native camera frames or caller-produced
images to the existing ordinary Texture/canvas pipeline.

## Sources, ownership and ordering

[CameraServer](../classes/CameraServer.md) is a permanent static service with a retained
Engine singleton. Monitoring is initially false. Enabling it initializes SDL's camera
subsystem; Process scans immediately and then at most once per second. Discovery
returns inactive [CameraFeed](../classes/CameraFeed.md) instances and typed
[formats](../classes/CameraFeedFormat.md), without requesting captured images. Source
activation separately opens the device. Pending permission yields no frames; denial
closes the active handle and reports the failure. Monitoring shutdown removes and
disposes every native source before releasing its subsystem reference. Engine teardown on the camera owner
runs that cleanup even after scene callbacks fail. Native engine and camera host
operations must share that owner; a separately owned standalone camera host keeps
its own explicit shutdown responsibility.

A caller may instead construct/subclass CameraFeed, register it, activate it and
submit copied Image planes. Registration retains its exact identity without owning
its disposal or activity. Remove a custom feed before disposing it. Native feeds are
service-owned and retire on removal. Registry events observe committed state;
recursive registry mutation, monitoring changes and pumps are rejected during
added/removed delivery. Feed activation/settings/hooks require its creating thread and
reject reentry. Image submission may run on producer threads, with a 64 MiB budget
for original and sampled image storage together. Two-plane submission validates both
copies before replacing either plane. Inactive sources ignore valid submissions.
Ordinary supported texture formats and mipmap data remain available; the producer
and consumer own the YCbCr channel interpretation.

Publication precedes notification. Process coalesces pending format/frame events,
delivers format first and frame second on the owner, and attempts every subscriber
and every remaining feed after failures. Image authoring from a callback schedules
another notification for a later pump. Event callbacks may query formats and stop/change the source; disposal and registry
changes cannot reenter that feed's event emission. Callbacks run outside pixel locks.
The display event pump invokes Process for an already owned camera service;
standalone/headless hosts use the public Process boundary explicitly.

## Native frames and texture views

[NativeCameraFeed](../classes/NativeCameraFeed.md) releases acquired SDL surfaces in
finally blocks. Conversion writes to prepared working planes; only a successful
complete frame copies into the separately retained published storage. Failure keeps
the previous frame and sends no frame event. Native RGB conversion uses the surface's
actual colorspace/properties and produces RGBA8. YUYV additionally supports grayscale,
a packed RG8 copy and separate full-size R8 Y plus half-width/full-height RG8 CbCr.
Unsupported non-RGB source selections return false. Native format selection requires
an inactive source. Dimensions and all sampled storage are bounded before allocation.
Prepared acquisition/conversion/publication reuse managed storage; discovery,
configuration, format changes, authored-image copies and image exports are cold work.

[CameraTexture](../classes/CameraTexture.md) borrows its selected source/plane and owns
a stable ordinary rendering RID. Frame and source changes invalidate its sampled
pixels; both canvas backends consume those updates. Logical size remains the full
frame size for the chroma plane too, while GetImage preserves actual plane dimensions.
An unknown/unregistered source has zero size and no image. GetImage returns an
independent coherent copy. FeedTransform is normalized image-coordinate metadata;
Texture drawing does not apply it implicitly. HasAlpha is false.

CameraFeedID and WhichFeed participate in ordinary resource duplication, scene/file
factories and worker preparation. They select process-local identities, so a saved
ID may be absent in a later process. CameraIsActive is a live convenience switch and
is never stored; loading a view cannot start a camera. A duplicate borrows the same
source and owns a separate view identity. Custom sources and native device handles
are not serialized.

```csharp
using var source = new CameraFeed();
CameraServer.AddFeed(source);
try
{
    using var texture = new CameraTexture { CameraFeedID = source.GetID() };
    source.FeedIsActive = true;
    using var image = Image.CreateEmpty(64, 32, false, Image.Format.Rgba8);
    image.Fill(Colors.Red);
    source.SetRGBImage(image);
    CameraServer.Process();
    // texture is ready for the ordinary CanvasItem.DrawTexture family.
}
finally { CameraServer.RemoveFeed(source); }
```

## Dependencies and limits

The implementation reuses the already private SDL binding, Image/TexturePixels,
retained rendering registry, typed resource descriptors/factories and event lifetime
machinery. No native camera handle is a public RID. The external-image data selector,
SetExternal and raw backend texture-ID getter retain the exact portable ownership
and native import prerequisite shared with ExternalTexture under ADRs 0021/0028.
They are absent rather than inert. CameraFeed and its data-type enum therefore retain Partial aggregate rows;
its implemented pixel family and those three blocked rows are recorded separately.

## Verification

The public-only [consumer](../../tests/CameraFeeds.Consumer/CameraFeedContracts.cs)
checks typed hooks, defaults, copied/coherent planes, producer-thread publication,
owner/reentry/lifetime guards, callback failure, source retirement, resource duplication,
worker file construction, fresh-process load and rendered color changes.
[Native boundary tests](../../tests/Electron2D.Tests/CameraFeedNativeTests.cs) supply
synthetic device identities and actual SDL surfaces to the production camera path,
checking pointer-table format metadata, real colorspace conversion, all four outputs,
permission and conversion-failure cleanup, and prepared allocations. They acquire no
personal camera images. Native hardware capture, permission prompts, other platforms,
native allocator measurements and owner acceptance remain separate gates.
Actual local receipts are retained in `bin/camera-feeds/2026-10-27/`.
