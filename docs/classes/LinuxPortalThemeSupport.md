# LinuxPortalThemeSupport

Last updated: 2026-09-22

**Source:** `src/Servers/Display/LinuxPortalThemeSupport.cs`

**Declaration:** `internal static class LinuxPortalThemeSupport`

**Inherits:** —

**Inherited By:** —

Internal Linux Wayland/X11 capability probe for the desktop Settings portal.

## Description

`DisplayServer` calls this probe once while opening on Linux Wayland or X11. It makes one bounded session D-Bus request for the Settings interface version and reports support for version one or newer. It does not read the color-scheme value or subscribe to notifications. `DisplayServer` stores the result; SDL supplies the current theme value and change event. The probe has no public or protected API.

## Example

Inside the display server's native initialization path:

```csharp
bool themeQueriesAvailable = LinuxPortalThemeSupport.Query();
```

This internal helper is not callable by game code.

## Method summary

| Signature | Contract |
| --- | --- |
| `internal static bool Query()` | Returns whether the Settings portal supports appearance queries. |

## Method descriptions

<a id="method-query"></a>
### `internal static bool Query()`

Calls `org.freedesktop.DBus.Properties.Get` for the `org.freedesktop.portal.Settings` `version` property with a 250 ms reply timeout. Returns `true` only for a version-one-or-newer unsigned result. Missing native D-Bus symbols, session bus, portal, reply, or compatible value produce `false`. Native messages and the borrowed bus connection are released on every path.

## Lifecycle, invariants, and errors

The probe runs synchronously on the display opening thread. It owns no persistent connection, event subscription, or cached state. It does not throw for unavailable D-Bus libraries or entries. A later portal availability change is visible only after reopening `DisplayServer`.

## Dependencies and interactions

Uses the system `libdbus-1.so.3` via native interop on Linux Wayland and X11. It adds no managed package and launches no external command. [`DisplayServer`](DisplayServer.md) is its only caller.

## Verification and limitations

Native Wayland and X11 smokes passed with this host's Settings portal version two and with an invalid session-bus address and expected unsupported result. An isolated Wayland session bus supplied version zero with a dark preference and version one with an unset preference: the probe returned false and true respectively. A malformed reply, live portal restart, and other Linux distributions have not been exercised.

## Relevant decisions

[ADR 0043](../decisions/display.md#adr-0043).
