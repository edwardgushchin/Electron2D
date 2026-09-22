# DisplayServer.FileDialogMode

Last updated: 2026-09-22

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Servers/Display/DisplayServer.Dialogs.cs`](../../src/Servers/Display/DisplayServer.Dialogs.cs)

**Declaration:** `public enum FileDialogMode` nested in [`DisplayServer`](DisplayServer.md)

## Description

Selects the kind of native file chooser requested by `DisplayServer.FileDialogShow`. The values are stable public identities. The server translates them to SDL chooser operations, copies selected paths from native callback memory, and delivers the typed result on the opening thread during `ProcessEvents`. Native choosers can ignore requested title, location, or filters. The enum itself owns no native state.

## Example

The following snippet assumes an open `DisplayServer display` and a host loop that continues to call `display.ProcessEvents()` until the chooser completes:

```csharp
display.FileDialogShow("Open", "", "", false,
    DisplayServer.FileDialogMode.OpenFile, ["*.png;PNG images"],
    (accepted, paths, selectedFilter) =>
    {
        if (accepted)
            Console.WriteLine(paths[0]);
    });
```

## Enumeration Summary

| Value | Meaning |
| --- | --- |
| [`OpenFile = 0`](#value-openfile) | Selects one existing file. |
| [`OpenFiles = 1`](#value-openfiles) | Selects multiple existing files. |
| [`OpenDirectory = 2`](#value-opendirectory) | Selects one directory. |
| [`OpenAny = 3`](#value-openany) | Requests a file or directory in one chooser; unavailable in the current native backend. |
| [`SaveFile = 4`](#value-savefile) | Selects a destination file that need not already exist. |

## Enumeration Descriptions

<a id="value-openfile"></a>
### `OpenFile = 0`

Opens one-file selection. The callback receives one copied path when accepted. Extension filters are applied where the native host supports them.

<a id="value-openfiles"></a>
### `OpenFiles = 1`

Opens multi-file selection. The callback receives a native-order path snapshot when accepted. Extension filters are applied where supported.

<a id="value-opendirectory"></a>
### `OpenDirectory = 2`

Opens a folder chooser. `FileDialogShow` uses `currentDirectory` as its requested initial location and ignores `filename` and filters in this mode.

<a id="value-openany"></a>
### `OpenAny = 3`

Reserved for selection of either a file or a directory in the same chooser. SDL has no equivalent operation; `FileDialogShow` throws `NotSupportedException` before launching native UI. This value remains part of the stable enum to make unsupported use explicit. A separately approved native chooser capable of selecting either kind on Wayland is the implementation trigger.

<a id="value-savefile"></a>
### `SaveFile = 4`

Opens a save destination chooser. The returned path can name a file that does not exist; creating or overwriting its content is the caller's responsibility.

## Lifecycle, invariants, and errors

The values are immutable. Unknown numeric values throw `ArgumentOutOfRangeException` from `FileDialogShow`; `OpenAny` and MIME-only filters throw `NotSupportedException`. A pending chooser prevents server disposal until its native result has been delivered by a later event pump. Cancellation invokes the callback with `false` and no paths. A native chooser failure also invokes it with `false` and no paths, then the event pump reports the failure in an aggregate exception.

## Threading and interactions

The enum has no thread affinity. `FileDialogShow` and `ProcessEvents` require the opening SDL main thread. The native callback may occur on another thread, but the public callback only runs during owner-thread pumping. See the [complete DisplayServer reference](DisplayServer.md).

## Verification and limitations

The SDL dummy-driver test checks enum IDs, unsupported modes and MIME-only filters, argument validation, and owner-thread restrictions. Real Wayland dialog behavior, filter presentation, and native cancellation remain unverified; other targets are outside the current release gate. On Linux, `showHidden: true` is accepted but ignored. SDL applies extension patterns and ignores an optional trailing MIME section; a MIME-only filter is rejected explicitly. The first native Linux portal FileChooser integration must apply MIME filters and verify selected-filter behavior. See the [coverage inventory](../coverage/classes/DisplayServer.md).

## Relevant decisions

[ADR 0040](../decisions/display.md#adr-0040) and [ADR 0021](../decisions/product.md#adr-0021).
