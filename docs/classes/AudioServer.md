# AudioServer

Last updated: 2026-10-01

**Declaration:** `public sealed class Electron2D.AudioServer` · **Source:** [AudioServer.cs](../../src/Servers/Audio/AudioServer.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Borrowed process-wide service; disposing it throws before logical disposal. The first owner-bound operation claims its configuration thread; passive singleton/rate/speed reads and worker resource mixing do not claim ownership. Later foreign configuration rejects. Native output opens lazily for playback or device queries; runtime bus records remain after native closure. Buses form sends to earlier indices, with unknown/self/later targets falling back to Master. Master stays at index zero. Graph edits prepare replacement submix nodes and redirect existing native sources under the audio mix lock, preserving playback identity, exact cursor, history, pause and polyphony. On failure the configured metadata remains committed, all native output is closed and callers may retry playback. Mute affects a bus and its downstream output; solo retains paths carrying soloed sources, filtering direct unrelated Master sources. Peak meters read actual native post-volume submix samples. Lock/Unlock pair around caller critical sections; native mixing uses that same gate. Native teardown releases every player slot, bus, master and engine even after custom playback cleanup failures. Input, public effects, bus-layout resources, output selection/latency and native sample registration are still absent.

## API summary

| Full signature | Contract |
| --- | --- |
| `public event System.Action? BusLayoutChanged` | Occurs after the bus list or routing graph changes. |
| `public event System.Action<System.Int32, System.String, System.String>? BusRenamed` | Occurs after a bus rename commits. |
| Lifecycle | Arguments are the index, old name and unique new name. |
| `public System.Void AddBus(System.Int32 atPosition = -1)` | Adds a uniquely named bus at an index or appends it. |
| atPosition | Insertion index; minus one appends. Master remains at zero. |
| System.ArgumentOutOfRangeException | The insertion range or bus limit is invalid. |
| `public System.Int32 GetBusChannels(System.Int32 index)` | Gets the number of stereo channel pairs on a bus. Actual native output channels divided by two. |
| index | Live bus index. |
| `public System.Int32 GetBusIndex(System.String busName)` | Gets the index of a bus by exact name. Minus one when absent. |
| busName | Nonnull exact bus name. |
| `public System.String GetBusName(System.Int32 index)` | Gets a bus name. The unique exact name. |
| index | Live bus index. |
| `public System.Single GetBusPeakVolumeLeftDB(System.Int32 index, System.Int32 channel)` | Gets the measured left-channel bus peak in decibels. Measured peak, with silence bounded at minus 200 dB. |
| index | Live bus index. |
| channel | Stereo pair index. |
| `public System.Single GetBusPeakVolumeRightDB(System.Int32 index, System.Int32 channel)` | Gets the measured right-channel bus peak in decibels. Measured peak, with silence bounded at minus 200 dB. |
| index | Live bus index. |
| channel | Stereo pair index. |
| `public System.String GetBusSend(System.Int32 index)` | Gets the requested named send target. The requested target; invalid or later targets resolve to Master during mixing. |
| index | Live bus index. |
| `public System.Single GetBusVolumeDB(System.Int32 index)` | Gets bus gain in decibels. Gain in dB. |
| index | Live bus index. |
| `public System.Single GetBusVolumeLinear(System.Int32 index)` | Gets the linear bus gain. Ten raised to dB/20. |
| index | Live bus index. |
| `public System.String GetDriverName()` | Gets the actual native audio driver. The SDL audio driver after output preparation. |
| `public System.Single GetMixRate()` | Gets the active audio mix frequency. 44100 Hz before output preparation, otherwise actual native mix frequency. |
| `public System.String[] GetOutputDeviceList()` | Lists actual output devices. A caller-owned device-name array. |
| `public Electron2D.AudioServer.SpeakerMode GetSpeakerMode()` | Gets the native output channel arrangement. The current speaker selector. |
| `public System.Double GetTimeSinceLastMix()` | Gets elapsed time since the last actual native mix quantum. Seconds, zero before native output exists. |
| `public System.Double GetTimeToNextMix()` | Gets the estimated time until the next native quantum. Nonnegative seconds based on actual quantum size and mix timestamp. |
| `public System.Boolean IsBusMute(System.Int32 index)` | Gets whether a bus is mute. The current flag. |
| index | Live bus index. |
| `public System.Boolean IsBusSolo(System.Int32 index)` | Gets whether a bus is solo. The current flag. |
| index | Live bus index. |
| `public System.Void Lock()` | Locks configuration for an explicit caller-owned critical section. |
| Lifecycle | Pair with Unlock in finally; this protects the owned bus/playback state, not arbitrary game code. |
| `public System.Void MoveBus(System.Int32 index, System.Int32 toIndex)` | Moves a non-Master bus; minus one moves it to the end. |
| index | Live non-Master source index. |
| toIndex | Destination insertion index; minus one appends. |
| System.ArgumentOutOfRangeException | An index is invalid or identifies Master. |
| `public System.Void RemoveBus(System.Int32 index)` | Removes a non-Master bus, redirecting unresolved sends to Master. |
| index | Live nonzero bus index. |
| System.ArgumentOutOfRangeException | The index is invalid or identifies Master. |
| `public System.Void SetBusMute(System.Int32 index, System.Boolean enable)` | Sets the bus mute flag and updates live native gains. |
| index | Live bus index. |
| enable | New flag. |
| `public System.Void SetBusName(System.Int32 index, System.String name)` | Renames a bus, updating named sends and resolving duplicate names. |
| index | Live non-Master index. |
| name | Requested nonnull name. |
| System.ArgumentOutOfRangeException | The index is invalid or identifies Master. |
| `public System.Void SetBusSend(System.Int32 index, System.String send)` | Sets a non-Master bus send target. |
| index | Live non-Master bus index. |
| send | Nonnull requested target name. |
| `public System.Void SetBusSolo(System.Int32 index, System.Boolean enable)` | Sets the bus solo flag and updates live native gains. |
| index | Live bus index. |
| enable | New flag. |
| `public System.Void SetBusVolumeDB(System.Int32 index, System.Single volumeDB)` | Sets finite bus gain in decibels. |
| index | Live bus index. |
| volumeDB | Finite gain. |
| System.ArgumentOutOfRangeException | The gain is not finite. |
| `public System.Void SetBusVolumeLinear(System.Int32 index, System.Single volumeLinear)` | Sets nonnegative finite linear bus gain. |
| index | Live bus index. |
| volumeLinear | Zero mutes through negative infinity decibels. |
| `public System.Void Unlock()` | Releases one matching configuration lock. |
| System.Threading.SynchronizationLockException | The calling thread owns no matching lock. |
| `protected override System.Void ValidateDisposal()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| Lifecycle | The process-wide service has process lifetime; engine teardown closes its native resources. |
| System.InvalidOperationException | Always thrown for the borrowed singleton. |
| `public System.Int32 BusCount { get; set; }` | Gets or sets the number of bus records, including the required Master. One initially; values must be between one and 255. |
| System.ArgumentOutOfRangeException | The count is outside the valid range. |
| System.InvalidOperationException | Access is off-owner. |
| `public static Electron2D.AudioServer Instance { get;  }` | Gets the process-wide audio service. The borrowed singleton; applications configure it rather than disposing it. |
| `public System.Single PlaybackSpeedScale { get; set; }` | Gets or sets the positive global playback-rate multiplier. One initially; actual player pitch combines this value with its local scale. |
| System.ArgumentOutOfRangeException | The value is nonpositive or nonfinite. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.

[Own reference coverage](../coverage/classes/AudioServer.md) retains missing and Partial members separately.
