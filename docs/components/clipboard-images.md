# Clipboard image ingestion

Last updated: 2026-10-27

`DisplayServer.ClipboardGetImage()` reads the ordinary clipboard through SDL MIME
data and returns an independent caller-owned Image. No supported offer returns
null. It reads copied image content, never a file-manager path or URI list. The
active display and its opening thread are required. Primary selection is separate.
On Wayland the native offer is delivered to the keyboard-focused client.

| Priority | MIME | Existing decoder |
| --- | --- | --- |
| 1 | image/png | LoadPNGFromBuffer |
| 2 | image/jpeg | LoadJPGFromBuffer |
| 3 | image/webp | LoadWebPFromBuffer |
| 4 | image/svg+xml | LoadSVGFromBuffer |
| 5 | image/bmp | LoadBMPFromBuffer |
| 6 | image/x-tga, image/x-targa | LoadTGAFromBuffer |
| 7 | image/ktx | LoadKTXFromBuffer |
| 8 | image/x-exr | LoadEXRFromBuffer |

The first offered supported format is authoritative. Its malformed data raises
InvalidDataException; failed native retrieval raises IOException. It does not
silently choose another format. Empty native data may be reported by SDL as a
retrieval failure. The common Image budget limits encoded input to 64 MiB before
engine copying/decoding, with existing decoder dimension/layout limits. SDL owns
transport buffering before the size is returned. Its returned buffer is freed in
finally, including decode/size failures. Failed temporary Images also retire.

`ClipboardHasImage()` runs this same cold read/decoding, disposes its temporary
Image and returns false for absent, invalid or unretrievable image data. It is a
validity predicate, not merely a MIME advertisement. It allocates and can perform
native I/O; do not poll it in a frame hot loop. A selection may change between calls.
Neither result aliases native clipboard storage or another returned image. Image
alpha, stored layout/mips and precision follow the [existing decoders](images.md).
An ImageTexture created from the result survives clipboard source retirement.

## Verification and reproduction

The public-only `tests/ClipboardImages.Consumer` references the public runtime and
native consumer targets. Its author mode writes PNG, JPEG, WebP, SVG, BMP, TGA,
KTX and HDR EXR fixtures. The Linux `run_native.py` driver uses installed Mutter,
PyGObject/Gio and a private D-Bus/Wayland runtime; its RemoteDesktop clipboard
producer offers synthetic MIME data independently of the application process.
The user's desktop clipboard is not used. A text marker proves receipt of each
actual offer before testing. Transfer lengths and hashes are recorded. A native
transfer serial cancelled by deliberate source retirement is recorded separately;
this exact cancellation is accepted only after retirement. Other producer errors
fail the cohort.

Build the consumer in Release, run its executable with `author <proof>/images`,
then run `dbus-run-session -- /usr/bin/python3 -B tests/ClipboardImages.Consumer/run_native.py compatibility clr <proof>`.
Replace compatibility with gpu for the independent GPU path. Publish with strict
full-trim NativeAOT into `tests/ClipboardImages.Consumer/bin/aot`, then replace clr
with aot. An optional fifth argument selects one scenario. The driver creates and
retires its compositor/clients; it never installs packages or changes host services.

The fourteen scenarios cover every supported MIME role, no-image file lists,
malformed/empty data, malformed preferred PNG plus valid JPEG, and 64 MiB+1 data.
Valid cases check exact dimensions, expected colors/alpha, independent copies,
repeated predicate object-census balance, wrong-thread and unavailable lifecycle
calls, source-offer revocation and real native canvas pixel readback. GPU hands off to
`RenderLoopEnabled=false` plus `ForceDraw(false)` after ingestion, exercising the
existing native offscreen canvas/readback without further swapchain presentation. HDR EXR keeps
a source red value above one; the Compatibility preview explicitly converts only
its independent preview image to RGBA8, while GPU uploads its original precision.
This preserves the renderer's existing HDR capability boundary.

The executable gate is Linux x64/Wayland with the exercised native drivers and
explicitly disabled VSync in the consumer. GPU window-presentation runs on private
Mutter 51/NVIDIA, with enabled or disabled VSync, intermittently waited in `drm_syncobj_array_wait_timeout` after
successful decode and completed clipboard transfers; debugger attachment was
rejected by ptrace_scope. Those timed-out presentation runs are unaccepted and
remain an unclassified target synchronization gate; these runs do not establish
GPU window-presentation acceptance. The default
VSync setting is unchanged by this implementation. Native
heap peaks/system-call costs, X11, Windows native bitmap formats, Apple pasteboard,
mobile/browser clipboard permissions and user acceptance retain their own target
qualification. Generic SDL MIME input is implemented; this does not establish every
foreign clipboard producer's native-format conversion. See [ADR 0040](../decisions/display.md#adr-0040).

The pinned [Wayland clipboard decoder](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/platform/linuxbsd/wayland/display_server_wayland.cpp)
defines format order. SDL documents [native buffer ownership](https://wiki.libsdl.org/SDL3/SDL_GetClipboardData)
and [main-thread MIME queries](https://wiki.libsdl.org/SDL3/SDL_HasClipboardData).
