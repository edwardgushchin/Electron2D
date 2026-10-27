# NativeCameraFeed

Last updated: 2026-10-27

`internal sealed class NativeCameraFeed : CameraFeed` in [NativeCameraFeed.cs](../../src/Servers/Camera/NativeCameraFeed.cs).

Private SDL source adapter for [camera pixel feeds](../components/camera-feeds.md), under ADR 0115. The retained CameraServer owns discovery, registration, native subsystem references and retirement. Each source retains copied native specs, typed format metadata, its selected output, an active camera handle and reusable working planes. Native IDs and pointers never leave the backend.

Activation chooses a bounded supported spec, requests converted RGBA output for RGB delivery, opens the device and checks permission. Pending permission acquires no surface. Poll acquires at most one ready surface per owner pump, locks it, converts actual colorspace or copies YUYV planes, and publishes only after complete success. Finally releases the surface on conversion/validation/locking failures too. Work storage remains separate from published mutable pixels, so partial conversion cannot damage the preceding frame.

Native RGB uses RGBA8; YUYV supports grayscale, packed RG8 and full-size R8 Y plus half-width/full-height RG8 CbCr. Source format selection and working-storage changes are cold. Warmed conversion/publication reuses arrays and published texture allocation. Deactivation clears the native handle before closing it and retires working planes. Published pixels remain available until source retirement.

The [native boundary tests](../../tests/Electron2D.Tests/CameraFeedNativeTests.cs) exercise production conversion with actual SDL surfaces and synthetic device callbacks, including permission, failure and retirement paths. Physical capture and native allocator/foreign-platform acceptance remain separate.
