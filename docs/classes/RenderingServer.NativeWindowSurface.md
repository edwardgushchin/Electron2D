# RenderingServer.NativeWindowSurface

Last updated: 2026-10-19

- Declaration: `private sealed class RenderingServer.NativeWindowSurface`
- Source: [RenderingServer.Windows.cs](../../src/Servers/Rendering/RenderingServer.Windows.cs)
- Owner: [RenderingServer](RenderingServer.md)
- Component: [window runtime](../components/window-runtime.md#independent-native-windows)

## Description

One renderer-session registration for an independent scene Window and its private display context. Its dictionary key is the public native lifetime ID. All access belongs to the scene owner thread. It borrows Window and DisplayServer identities until retirement; it does not initialize SDL or own another scene loop.

## Internal state

| Member | Contract |
| --- | --- |
| `Window` | Borrowed scene viewport identity. |
| `Display` | Private native context; its rendering attachment remains live while registered. |
| `Backend` | Shared main GPU backend, or an owned compatibility backend using the main driver. |
| `Presentation` | GPU child-window claim; null selects compatibility replay. |
| `NeedsSeed` | Compatibility history must be imported before the next frame; true initially and after failed native submission. |

## Lifetime and invariants

Registration acquires the display rendering attachment, then creates an independent compatibility renderer or claims the child on the shared GPU device. Compatibility cold seeding copies current/backbuffer pixels and completed-state flags from authoritative targets, including disabled and feedback sources. Prepared frames replay the frozen dependency postorder without scene callbacks or CPU readback. Retirement removes the registration, drains/releases native presentation or compatibility resources, and releases the display attachment. Renderer shutdown attempts every remaining Window close. Native frame submission rejects lifecycle mutation before retirement begins.

NativeChildWindowTests verifies actual surface images, cross-window sampling, disabled and cyclic history, native queue input/text, lifetime failure/recovery and zero measured warmed managed owner bytes. This does not measure SDL/driver allocations or establish foreign-platform acceptance.
