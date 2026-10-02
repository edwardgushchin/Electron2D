# WindowMode

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum WindowMode` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Identifies a native window's presentation mode. Used by [Window](Window.md) and [DisplayServer](DisplayServer.md). The window manager may adapt or deny a requested state.

## Values

| Value | Meaning |
| --- | --- |
| `Windowed = 0` | A floating window with its configured decorations. |
| `Minimized = 1` | A window minimized by the window manager. |
| `Maximized = 2` | A window expanded to its screen's work area. |
| `Fullscreen = 3` | A borderless window covering its screen. |
| `ExclusiveFullscreen = 4` | A fullscreen window requesting a dedicated video mode where supported. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. Native display behavior remains subject to the platform limits documented by its owning APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
