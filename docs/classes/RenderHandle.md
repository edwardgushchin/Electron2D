# RenderHandle

Last updated: 2026-09-22

- Declaration: `internal sealed class RenderHandle : SafeHandleZeroOrMinusOneIsInvalid`
- Source: [CanvasBackend.cs](../../src/Servers/Rendering/CanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

Owns one native graphics/reflection handle and optionally retains its parent SafeHandle. Explicit backend disposal is the ordinary lifetime path; SafeHandle finalization is a fallback, not rendering-thread scheduling. The parent remains retained until child native release completes. Internal callers only supply native APIs whose failure value is zero.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
using var surface = new RenderHandle(nativeSurface, SDL3.SDL.DestroySurface);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal RenderHandle(nint value, Action<nint> release, SafeHandle? owner = null)` | [Construction](#construction) |
| `protected override bool ReleaseHandle()` | [Release](#release) |

## Member descriptions

### Construction

`internal RenderHandle(nint value, Action<nint> release, SafeHandle? owner = null)`

Rejects zero through CanvasBackend.Failure. Retains the optional parent using DangerousAddRef before adopting value. If retaining the parent fails, invokes release(value) and propagates the error. The release callback must be valid for the handle and must not throw during finalization.

### Release

`protected override bool ReleaseHandle()`

Calls the release callback, then releases the parent reference in finally. SafeHandle supplies idempotent Dispose semantics and prevents premature native parent destruction. Returns true on normal release.

## Verification and limits

Native renderer tests exercise constructor failures, shutdown and repeated startup through resource owners. SafeHandle reference accounting is relied upon; finalizer timing is not a public guarantee.
