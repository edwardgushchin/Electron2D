# RenderHandle

Last updated: 2026-10-11

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
| `internal void ReplaceNative(nint value)` | [Private buffer replacement](#private-buffer-replacement) |
| `protected override bool ReleaseHandle()` | [Release](#release) |

## Member descriptions

### Construction

`internal RenderHandle(nint value, Action<nint> release, SafeHandle? owner = null)`

Rejects zero through CanvasBackend.Failure. Retains the optional parent using DangerousAddRef before adopting value. If retaining the parent fails, invokes release(value) and propagates the error. The release callback must be valid for the handle and must not throw during finalization.

### Release

`protected override bool ReleaseHandle()`

Calls the release callback, then releases the parent reference in finally. SafeHandle supplies idempotent Dispose semantics and prevents premature native parent destruction. Returns true on normal release.

### Private buffer replacement

`internal void ReplaceNative(nint value)`

Exclusively owned physics capacity buffers may adopt a newly created native buffer through the existing wrapper. The original release delegate and one retained parent reference survive growth. The new resource becomes owned before retiring the old one; an old-release exception still leaves the new resource available for ordinary cleanup. A closed wrapper retires the incoming resource and throws. Zero rejects without changing ownership. Callers serialize this operation on the owner and never use it for published resources or borrowed native identities. SDL still allocates the native buffer; this removes managed wrapper/delegate allocations rather than the native resource or its copy/wait costs.

## Verification and limits

Native renderer tests exercise constructor failures, shutdown and repeated startup through resource owners. [Replacement tests](../../tests/Electron2D.Tests/RenderHandleReplacementTests.cs) cover retained-parent release, idempotent disposal, closed incoming cleanup, old-release failure and 64 warmed replacements at zero owner/all-thread managed bytes. The [scene pipeline fixture](../../tests/Electron2D.Tests/PhysicsPipelinePerformance.cs) exercises actual late GPU capacity growth after warmup. Finalizer timing is not a public guarantee.
