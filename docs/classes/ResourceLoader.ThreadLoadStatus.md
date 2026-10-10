# ResourceLoader.ThreadLoadStatus

Last updated: 2026-10-10

Public nested enum in [ResourceLoader.Threaded.cs](../../src/Core/IO/ResourceLoader.Threaded.cs), compiled into Electron2D.dll.

| Value | Integer | Meaning |
| --- | --- | --- |
| InvalidResource | 0 | No request exists, or its final matching result was collected. |
| InProgress | 1 | Background preparation or owner cache publication is pending. |
| Failed | 2 | Load, cancellation, ownership or publication failed; get rethrows and releases its request. |
| Loaded | 3 | Cache publication finished and the retained resource is available for collection. |

GetStatus can also return monotonic float progress through an out parameter. Absent requests report zero; terminal states report one. Completion does not consume a request. See [ResourceLoader](ResourceLoader.md) and [threaded loading](../components/threaded-resource-loading.md).
