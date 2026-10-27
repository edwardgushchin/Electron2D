# CameraServer

Last updated: 2026-10-27

`public sealed class Electron2D.CameraServer`

Inherits: `Electron2D.ElectronObject`. Part of [camera pixel feeds](../components/camera-feeds.md), under [ADR 0115](../decisions/camera-feeds.md#adr-0115).

Permanent static process service backed by a retained Engine singleton. Monitoring and registration bind to their first owner thread; read-only registry queries remain synchronized. Monitoring is initially false, never activates a source, and scans immediately on Process then at most once per second. Registered custom sources are borrowed; native sources are owned. Registry callbacks observe committed changes and cannot recursively mutate the registry or pump. Process attempts remaining feeds after failures. Disabling monitoring and Engine shutdown close native devices and detach their views. No public Instance accessor exists.

## Example

The source/format comments below identify any required caller context.

```csharp
CameraServer.MonitoringFeeds = true;
try
{
    CameraServer.Process();
    foreach (var feed in CameraServer.Feeds())
        Console.WriteLine(feed.GetName()); // Discovery does not activate capture.
}
finally { CameraServer.MonitoringFeeds = false; }
```

## API reference

| Declaration | Contract |
| --- | --- |
| [`public static event System.Action<System.Int32> CameraFeedAdded`](#member-eb15c010c56a) | Reports a newly registered process-local feed ID. |
| [`public static event System.Action<System.Int32> CameraFeedRemoved`](#member-66574c6417a4) | Reports a removed process-local feed ID. |
| [`public static event System.Action CameraFeedsUpdated`](#member-7464fcd9cd78) | Reports completion of a native device scan, including an empty result. |
| [`public static System.Void AddFeed(Electron2D.CameraFeed feed)`](#member-3564358c5684) | Adds one caller-owned feed, retaining its identity until removal. |
| [`public static Electron2D.CameraFeed[] Feeds()`](#member-15890aaf7068) | Returns registered feeds in insertion order as a copied array. |
| [`public static Electron2D.CameraFeed GetFeed(System.Int32 index)`](#member-6c9485c9950f) | Returns one registered feed by index. |
| [`public static System.Int32 GetFeedCount()`](#member-989c690d2acc) | Returns the registered source count. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-ef59b2f58375) | Extends the inherited typed property metadata with this type's current configuration. |
| [`public static System.Void Process()`](#member-084d16fbc150) | Discovers changes, acquires available frames and delivers coalesced source events on the owner. |
| [`public static System.Void RemoveFeed(Electron2D.CameraFeed feed)`](#member-702a04d84436) | Removes a registered feed; native feeds also close and retire their device. |
| [`protected override System.Void ValidateDisposal()`](#member-54278d7dd549) | Enforces the documented ownership, callback and permanent-service lifetime rules. |
| [`protected override System.Void ValidateMutation()`](#member-6517b3887942) | Enforces the owning service/source mutation contract before inherited mutation. |
| [`public static System.Boolean MonitoringFeeds { get; set; }`](#member-47a0dafcb8cb) | Gets or changes native-device discovery; disabling it retires all service-owned feeds. |

## Member descriptions

<a id="member-eb15c010c56a"></a>
### `public static event System.Action<System.Int32> CameraFeedAdded`

Reports a newly registered process-local feed ID.

<a id="member-66574c6417a4"></a>
### `public static event System.Action<System.Int32> CameraFeedRemoved`

Reports a removed process-local feed ID.

<a id="member-7464fcd9cd78"></a>
### `public static event System.Action CameraFeedsUpdated`

Reports completion of a native device scan, including an empty result.

<a id="member-3564358c5684"></a>
### `public static System.Void AddFeed(Electron2D.CameraFeed feed)`

Adds one caller-owned feed, retaining its identity until removal.

Repeated registration of the same feed has no effect. Added events observe the committed registry.

Parameters:

- `feed`: Live feed created on the service owner thread.

<a id="member-15890aaf7068"></a>
### `public static Electron2D.CameraFeed[] Feeds()`

Returns registered feeds in insertion order as a copied array.

Returns: Borrowed source identities.

<a id="member-6c9485c9950f"></a>
### `public static Electron2D.CameraFeed GetFeed(System.Int32 index)`

Returns one registered feed by index.

Returns: A borrowed live source.

Parameters:

- `index`: Zero-based insertion-order index.

<a id="member-989c690d2acc"></a>
### `public static System.Int32 GetFeedCount()`

Returns the registered source count.

Returns: Native and custom feeds together.

<a id="member-ef59b2f58375"></a>
### `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Extends the inherited typed property metadata with this type's current configuration.

<a id="member-084d16fbc150"></a>
### `public static System.Void Process()`

Discovers changes, acquires available frames and delivers coalesced source events on the owner.

The native display pump calls this automatically. Standalone hosts call it explicitly. Callback failures are collected after other feeds are attempted. Recursive pumps are rejected.

<a id="member-702a04d84436"></a>
### `public static System.Void RemoveFeed(Electron2D.CameraFeed feed)`

Removes a registered feed; native feeds also close and retire their device.

A custom feed's activity and ownership remain with its caller. Removed events observe the committed registry.

Parameters:

- `feed`: Currently registered identity.

<a id="member-54278d7dd549"></a>
### `protected override System.Void ValidateDisposal()`

Enforces the documented ownership, callback and permanent-service lifetime rules.

<a id="member-6517b3887942"></a>
### `protected override System.Void ValidateMutation()`

Enforces the owning service/source mutation contract before inherited mutation.

<a id="member-47a0dafcb8cb"></a>
### `public static System.Boolean MonitoringFeeds { get; set; }`

Gets or changes native-device discovery; disabling it retires all service-owned feeds.

Value: False initially. Camera activation is separate and remains opt-in.

Errors:

- `T:System.IO.IOException`: The native camera subsystem cannot initialize.

## Verification and limits

The public camera consumer and native boundary tests are linked from the [component verification](../components/camera-feeds.md#verification). Compiled API/XML, native surface conversion and rendered pixels are distinct from physical camera capture, foreign platforms, native allocator and owner acceptance.
