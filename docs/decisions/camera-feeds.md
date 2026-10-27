# Camera frame delivery

Last updated: 2026-10-27

<a id="adr-0115"></a>
## ADR 0115: Owned camera devices and borrowed pixel feeds

- Status: Accepted
- Scope: CameraServer, CameraFeed and CameraTexture; native external-image import remains separate

CameraServer is a permanent static service backed by a retained object under ADR 0095.
Its owner thread manages discovery, registration and native device lifetime. Monitoring
is opt-in and does not activate a camera. SDL's existing camera subsystem supplies
native device discovery, formats, permission state and acquired surfaces; no backend
handle escapes. Disabling monitoring retires its native feeds and closes all their
devices. Caller-created feeds remain borrowed and must be removed before disposal.
Native feeds are service-owned. CameraServer.Process is the explicit standalone host
boundary; the display event pump also calls it. Engine shutdown on the camera owner closes monitoring; separately owned standalone
hosts retain their explicit shutdown responsibility.

CameraFeed is an extensible ElectronObject with synchronous typed activation,
deactivation, format-query and format-selection hooks. CameraFeedFormat replaces the
format dictionaries with dimensions, a source-format name and rational frame rate.
CameraFeedOutput replaces the defined output string parameter: RGB, Grayscale,
Separate and Copy. Native RGB conversion preserves source colorspace through SDL.
YUYV additionally supports grayscale, a full-size Y plane plus half-width CbCr plane,
and a packed two-channel copy. Other source formats reject non-RGB selections rather
than silently changing their meaning. Native preparation and format changes are cold;
acquisition, conversion and prepared texture publication reuse storage.

Feed settings, activation, hooks and disposal require the creating thread. Source
hooks reject recursive operations; event callbacks may query formats and change
settings or stop capture after the native surface has been released. Activation failure keeps the feed inactive; deactivation commits the inactive
state and attempts cleanup even when a hook throws. Image setters accept copied,
bounded nonempty supported texture images on producer threads. Inactive feeds ignore valid
submissions. Keep the supported texture formats and mipmap data. Producers and consumers own
YCbCr channel interpretation; native YUYV conversion supplies R8 Y and RG8 CbCr. Frame publication atomically replaces both planes.
Native frame delivery reuses prepared mutable storage on the owner. Format and frame
events are coalesced and delivered by Process on the owner after publication, in that
order. A failing callback cannot suppress the other event or another feed. Callbacks
may author a later frame; the next Process delivers it. Disposal and registry mutation
are forbidden during the same feed's event emission. Native surfaces are released on
all exits. No callback executes while the pixel-state lock is held.

CameraTexture is a normal Texture view with its own stable rendering identity. It
borrows the selected registered feed and emits Changed for frame, format, source and
registry changes, so retained canvas commands observe new pixels on both backends.
Its logical dimensions remain the full frame dimensions for either plane. The
source's FeedTransform remains queryable metadata for normalized image coordinates;
ordinary Texture drawing does not implicitly apply it. HasAlpha remains false.
CameraIsActive forwards the live feed switch; it is runtime-only, preventing resource
loading or scene capture from starting a device. CameraFeedID and WhichFeed are stored
configuration; absent process-local IDs produce an empty texture until registered.
Duplicates retain the view configuration and borrow the same feed; current pixels
export as independent Images. CameraTexture participates in ordinary resource/file
factories. Custom feeds and native devices are not serialized.

Keep External feed data, SetExternal and backend texture-ID access blocked on the
portable external-image ownership/import decision shared with ExternalTexture under
ADRs 0021/0028. Integer native texture handles cannot represent both current renderers.
Do not expose inert enum values or activation switches for this missing path.

Verify copied planes, coherent replacement, failures/reentry, owner delivery, registry
retirement, storage and actual 2D rendered updates. Linux Wayland is the native gate
under ADR 0021. Synthetic producers, backend-boundary tests and device enumeration do
not establish physical camera capture, permission prompts or foreign-platform execution.
