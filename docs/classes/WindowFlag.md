# WindowFlag

Last updated: 2026-10-02

**Source:** [`src/Core/PublicEnumDomains.cs`](../../src/Core/PublicEnumDomains.cs)
**Declaration:** `public enum WindowFlag` in `Electron2D`
**Inherits:** `System.Enum`
**Inherited By:** —

## Description

Identifies a native window policy; values are indices, not a bit mask. Used by [Window](Window.md) and [DisplayServer](DisplayServer.md). Values identify policies, not bit positions; `Max` is not selectable.

## Values

| Value | Meaning |
| --- | --- |
| `ResizeDisabled = 0` | Whether dragging the window border is prevented from resizing it. |
| `Borderless = 1` | Whether the window has no native border and title bar. |
| `AlwaysOnTop = 2` | Whether the window stays above ordinary windows. |
| `Transparent = 3` | Whether the window background can be transparent; requires transparent window creation and a renderer. |
| `NoFocus = 4` | Whether the window is prevented from receiving keyboard focus. |
| `Popup = 5` | Whether the window is a transient menu popup; requires multiple-window ownership. |
| `ExtendToTitle = 6` | Whether content extends under the native title bar; requires platform title-bar integration. |
| `MousePassthrough = 7` | Whether mouse input passes to an underlying application window; requires native hit-test integration. |
| `SharpCorners = 8` | Whether native rounded window corners are suppressed; requires platform window-style integration. |
| `ExcludeFromCapture = 9` | Whether ordinary screen capture excludes the window; requires platform capture-policy integration. |
| `PopupWmHint = 10` | Whether the window manager treats the window as a popup; requires multiple-window ownership. |
| `MinimizeDisabled = 11` | Whether native minimization controls are disabled; requires platform window-style integration. |
| `MaximizeDisabled = 12` | Whether native maximization controls are disabled; requires platform window-style integration. |
| `Max = 13` | The number of defined policy identifiers; not a window policy. |

## Lifecycle and validation

This is an immutable value type. Each consuming API validates values it can select before changing state. Its numeric identities are fixed by ADR 0051.

## Verification and limitations

The managed Electron2D suite checks the consumers and numeric values. Native display behavior remains subject to the platform limits documented by its owning APIs.

## Relevant decision

[ADR 0051](../decisions/product.md#adr-0051).
