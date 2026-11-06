# Native window capture exclusion

Last updated: 2026-11-07

[Window.ExcludeFromCapture](../classes/Window.md#exclude-from-capture) and
`WindowFlag.ExcludeFromCapture` select one native window's ordinary OS capture
policy. [DisplayServer](../classes/DisplayServer.md) exposes observed policy
through `WindowGetFlag` and applies it through `WindowSetFlag`, including child IDs.
The implementation is `src/Servers/Display/DisplayServer.Capture.cs`.

Windows 10 version 2004 and later with DWM composition use
[`SetWindowDisplayAffinity`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity)
with `WDA_EXCLUDEFROMCAPTURE` (`0x11`) or `WDA_NONE` (`0`). The capability query
checks the active Windows driver, OS version and composition; native failures
raise `IOException` with the Win32 error. Queries read the actual HWND policy.
Calls retain the existing owner, active-service and native-ID guards. Native
window ownership and teardown stay with SDL; no capture session is owned by the
runtime. System DLL imports search System32.

The scene property uses the existing flag storage and stored property descriptor.
Detached requests, duplication and PackedScene reconstruction retain the value;
native activation applies enabled requests, and hide/show reapplies them to the
new child HWND. A failed active native request leaves authored state unchanged.
Direct DisplayServer writes change native state without changing scene authoring.
Embedded windows retain the authored request without excluding their containing
native window. Popup subclasses use their own native HWND.

Unsupported native drivers and Windows hosts without the prerequisite reject
both direct reads and writes and advertise no capability. An enabled detached
request fails native activation there. macOS capture/sharing integration remains
unimplemented; this Windows slice does not complete portable capture exclusion.
Offscreen textures and engine readback remain available. OS capture policy is
not a content-security or DRM guarantee.

## Verification

`WindowCaptureChecks` is shared by the root runner and
`tests/WindowCapture.Consumer`. `tools/window-capture.cpp` is an independent
Windows SDK consumer of Windows Graphics Capture, D3D11 staging pixels and the
actual tested HWND. Capturable controls must contain red rendered pixels;
excluded controls must refuse access or produce no frame within the bounded wait.
Other errors fail the check. The helper without arguments creates its own GDI
window and verifies capture, exclusion and restoration independently of the engine.

Build the helper in a Visual Studio x64 developer shell with Windows SDK 26100:

```powershell
cl /nologo /EHsc /O2 /MT /std:c++20 /utf-8 tools/window-capture.cpp /Fe:capture-probe.exe /link windowsapp.lib d3d11.lib dxgi.lib user32.lib gdi32.lib
dotnet run --project tests/WindowCapture.Consumer -c Release -- gpu C:/absolute/path/capture-probe.exe
```

Use `compatibility` for the second renderer and `unavailable` on a native host
without capture exclusion. The root runner selects the same checks with
`ELECTRON2D_TEST_WINDOW_CAPTURE`. Actual host/profile results and remaining gates
are recorded in [platform verification](../platform-verification.md).
