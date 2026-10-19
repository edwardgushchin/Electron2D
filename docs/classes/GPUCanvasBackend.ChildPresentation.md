# GPUCanvasBackend.ChildPresentation

Last updated: 2026-10-19

- Declaration: `internal sealed class GPUCanvasBackend.ChildPresentation : IDisposable`
- Source: [GPUCanvasBackend.Windows.cs](../../src/Servers/Rendering/GPUCanvasBackend.Windows.cs)
- Owner: [GPUCanvasBackend](GPUCanvasBackend.md)
- Component: [window runtime](../components/window-runtime.md#independent-native-windows)

## Description

Retains one borrowed native child-window SafeHandle and claims that window on the existing canvas GPU device. It owns a private presentation target pair and effective VSync policy; ordinary viewport targets and device resources remain backend-owned. Construction retains the SafeHandle before native claim, and failed claim/configuration releases completed ownership. Native handles remain internal.

## Internal members

| Member | Contract |
| --- | --- |
| `ChildPresentation(GPUCanvasBackend backend, SafeHandle window)` | Retains the native window wrapper; the caller still owns its original reference. |
| `Claim(DisplayServer.VSyncMode mode)` | Claims the window, applies native policy/fallback and registers with the backend. |
| `SetVSync(DisplayServer.VSyncMode mode)` | Changes this surface's effective presentation mode, preserving native failure. |
| `Window` | Borrowed native window pointer, valid only before disposal. |
| `Target` | Owned presentation target pair, lazily sized to client pixels. |
| `VSync` | Effective native presentation policy after fallback. |
| `Dispose()` | Removes registration, disposes presentation storage, releases the GPU claim and retained window reference once. |

## Lifetime and verification

The RenderingServer owner thread drains GPU work before ordinary retirement; backend shutdown waits before releasing leftover claims. This object never owns a second GPU device or runs scene callbacks. Native child-window tests exercise actual GPU presentation, repeated hide/show and renderer shutdown on Linux X11/Wayland. Native allocations, other platforms and physical/compositor acceptance remain separate.
