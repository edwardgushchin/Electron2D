# AudioServer

Last updated: 2026-10-03

**Declaration:** `public sealed partial class Electron2D.AudioServer` · **Source:** [AudioServer.cs](../../src/Servers/Audio/AudioServer.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Borrowed process-wide service; disposing it throws before logical disposal. The first owner-bound operation claims its configuration thread; passive singleton/rate/speed reads and worker resource mixing do not claim ownership. Later foreign configuration rejects. Native output opens lazily for playback or device queries; runtime bus records remain after native closure. Buses form sends to earlier indices, with unknown/self/later targets falling back to Master. Master stays at index zero. Graph edits prepare replacement submix nodes and redirect existing native sources under the audio mix lock, preserving playback identity, exact cursor, history, pause and polyphony. On failure the configured metadata remains committed, all native output is closed and callers may retry playback. Mute affects a bus and its downstream output; solo retains paths carrying soloed sources, filtering direct unrelated Master sources. Peak meters read actual native post-volume submix samples. Lock/Unlock pair around caller critical sections; native mixing uses that same gate. Native teardown releases every player slot, bus, master and engine even after custom playback cleanup failures. Ordered public effects now execute before bus gain and final peak metering. Sample registration is cold, transactional and weak-keyed; explicit re-registration captures edits for subsequent voices while active voices retain their snapshot. Engine closure clears registrations. Bus-layout resources and output selection/latency remain separate dependencies.

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

## Recording API summary

| Full signature | Contract |
| --- | --- |
| `public float GetInputMixRate()` | Prepared actual recording frequency; lazy paused preparation. |
| `public string[] GetInputDeviceList()` | Copied exact native names plus Default. |
| `public string InputDevice { get; set; }` | Default initially; prepare-before-commit device switch. |
| `public void SetInputDeviceActive(bool active)` | Explicit start/global pause, typed errors. |
| `public int GetInputBufferLengthFrames()` | Four native quanta, zero before preparation/after engine closure. |
| `public int GetInputFramesAvailable()` | Unread server frames, zero through capacity. |
| `public Vector2[] GetInputFrames(int frames)` | Whole-count copied stereo read or empty without consumption. |

## Recording member descriptions

### GetInputMixRate

Returns Hz from the actual opened recording device, independently of output. Lazy preparation opens a paused SDL3 stream and requires the audio owner; once prepared the read is passive and usable on audio workers. It does not resume recording, and OS privacy/device errors can still prevent preparation. Device selection changes the prepared frequency; engine closure releases the device. Internal generator mixing only reads the last prepared frequency and never lazily opens input on its callback thread. A new Input playback/start explicitly prepares again after closure.

### GetInputDeviceList

Owner-bound enumeration returns a caller-owned array beginning with Default; native duplicate names appear once. It does not activate recording. Enumeration balances its temporary SDL audio reference and reports failures with InvalidOperationException.

### InputDevice

Gets/sets the exact name on the audio owner. Null throws ArgumentNullException and an unavailable name throws ArgumentException. Default follows the system recording selector. Same-name assignment is a no-op. Replacement opens/prepares a new paused stream and resumes it only if capture was active; failure leaves the old name/device/capture intact. Success publishes the new stream/rate, clears server read history and resets microphone cursors/history at their next mix before releasing the old native stream. Input generator queues keep their original capacity and observe the new rate. Each native quantum is bounded to 1..262144 frames; unsupported dimensions throw NotSupportedException before allocating the ring.

### SetInputDeviceActive

Owner-bound idempotent explicit activation. True requires [AudioDriverEnableInput](ProjectSettings.md#audiodriverenableinput), starts or retains capture and installs a manual request. False pauses the shared native device even with active microphone playbacks and releases the manual request; microphones stay active, consume pending input and then produce silence. A later fresh microphone Start or explicit true can resume it. Automatic microphone Stop retains recording for other microphones or a manual request, and pauses only the final automatic request otherwise. Disabling the project setting gates future starts; it does not asynchronously revoke capture. Failed activation throws InvalidOperationException without publishing a request; restarting from paused resets ring/readers and callback failure. Pausing preserves unread data.

### GetInputBufferLengthFrames

Owner-bound capacity query. Returns four opened native recording quanta as stereo frame count; zero when input is unprepared/engine-closed. Queries do not prepare or activate a device. Capacity is fixed per device preparation and independent of output quantum/rate.

### GetInputFramesAvailable

Owner-bound unread server frame count, never exceeding capacity. Overflow discards oldest unread history and exact full-buffer wraps remain readable. The server cursor is independent of microphone cursors. Native callback failure throws InvalidOperationException; stop/start clears it.

### GetInputFrames

Nonnegative count; negative throws ArgumentOutOfRangeException. Exactly enough input returns a caller-owned Vector2 array and advances only the server cursor; an insufficient request returns empty without consumption. Zero/unprepared input returns empty. X is left, Y is right, signed finite float PCM clamped to [-1,1]; native nonfinite frames become silence. Reading can occur while recording is paused. Allocation is explicit copied export, separate from allocation-free prepared capture/mixing. Off-owner access or callback failure throws InvalidOperationException.

### Recording lifecycle example

Partial owner-thread snippet; retrieve frames on later scene frames while capture proceeds.

```csharp
var audio = AudioServer.Instance;
ProjectSettings.Instance.Set(ProjectSettings.AudioDriverEnableInput, true);
audio.SetInputDeviceActive(true);
// On a later scene callback:
Vector2[] recorded = audio.GetInputFrames(audio.GetInputFramesAvailable());
audio.SetInputDeviceActive(false);
```

Engine shutdown releases both input/output, stops standalone microphone playbacks and clears requests. Last output-player detachment closes output while retaining independent input. Capture callbacks use their own ring gate and never enter AudioServer.Lock; caller mix locking does not protect input history. Public copy/availability operations synchronize input internally. See [recording verification](../components/audio-playback.md#recording-input).

If the final automatic native pause fails, its microphone request is still released and the failed input stream is destroyed before reporting the error. Disposed playbacks cannot retain dead capture requests; a later Start prepares a fresh device. Explicit manual pause failures retain the manual request for retry.

## Native sample methods

| Full signature | Contract |
| --- | --- |
| `public bool IsStreamRegisteredAsSample(AudioStream stream)` | Queries the borrowed live resource identity on the owner. |
| `public void RegisterStreamAsSample(AudioStream stream)` | Transactionally generates/replaces a cold sample snapshot; unsupported resources and callback reentry reject. |

## Method Descriptions

### IsStreamRegisteredAsSample

Queries the borrowed live resource identity on the owner. See [native sample playback](../components/audio-playback.md#native-sample-playback) for ownership, preparation, source edits, boundaries and driver limits. Disposed inputs reject.

### RegisterStreamAsSample

Transactionally generates/replaces a cold sample snapshot; unsupported resources and callback reentry reject. See [native sample playback](../components/audio-playback.md#native-sample-playback) for ownership, preparation, source edits, boundaries and driver limits. Disposed inputs reject.

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.

[Own reference coverage](../coverage/classes/AudioServer.md) retains missing and Partial members separately.


<a id="effects"></a>
## Bus effects

[AudioEffect](AudioEffect.md) resources are borrowed; buses own one [AudioEffectInstance](AudioEffectInstance.md) for each output stereo pair. Add prepares native output and the entire edited chain. Factory failure preserves configured entries and old instance identity. Native graph failure retains committed configuration and closes output for retry. Structural edits dispose old instances after voices detach; custom cleanup exceptions are collected after all cleanup attempts. Graph routing changes preserve effect identities, capture history and tail activity. Configuration and Lock/Unlock from effect factories, processing and disposal reject reentrancy.

| Signature | Contract |
| --- | --- |
| `public void AddBusEffect(int busIndex, AudioEffect effect, int atPosition = -1)` | Inserts an enabled borrowed resource. |
| `public int GetBusEffectCount(int busIndex)` | Includes disabled entries; does not prepare output. |
| `public AudioEffect GetBusEffect(int busIndex, int effectIndex)` | Exact borrowed configured resource. |
| `public AudioEffectInstance GetBusEffectInstance(int busIndex, int effectIndex, int channel = 0)` | Borrowed live stereo-pair state; prepares output when needed. |
| `public void RemoveBusEffect(int busIndex, int effectIndex)` | Removes resource reference and recreates edited chain. |
| `public void SwapBusEffects(int busIndex, int effectIndex, int byEffectIndex)` | Swaps order and recreates edited chain. |
| `public bool IsBusEffectEnabled(int busIndex, int effectIndex)` | Requested flag independent of bypass. |
| `public void SetBusEffectEnabled(int busIndex, int effectIndex, bool enabled)` | Toggles native processing without resetting state. |
| `public bool IsBusBypassingEffects(int busIndex)` | Requested bypass flag. |
| `public void SetBusBypassEffects(int busIndex, bool enable)` | Suppresses all public effects; retains gain/metering. |

<a id="addbuseffect"></a>
### AddBusEffect

Live bus index includes Master. Null/disposed resources reject before mutation. Negative or at/after-end positions append; other indices insert. Every newly configured entry defaults enabled. A successful edit creates fresh state for every entry on that bus and invalidates previous borrowed instances. Capture's resource-owned ring remains fixed in size and clears through its factories.

<a id="getbuseffectcount"></a>
<a id="getbuseffect"></a>
### GetBusEffectCount and GetBusEffect

Read owner-bound configuration. Count includes disabled effects. GetBusEffect returns the exact supplied resource, retained even across engine closure; caller disposal is observable on later processing rather than silently changing ownership. Invalid bus/effect indices throw ArgumentOutOfRangeException.

<a id="getbuseffectinstance"></a>
### GetBusEffectInstance

Returns bus-owned state for channel zero by default, where channel indexes stereo pairs, not individual native channels. Invalid pair indices throw ArgumentOutOfRangeException. It prepares native output and instances after closure; structural effect edits, bus removal and output closure dispose older handles. Calling Process or Dispose on an attached handle rejects. Pair identity remains stable through enable, bypass and ordinary routing edits.

<a id="removebuseffect"></a>
<a id="swapbuseffects"></a>
### RemoveBusEffect and SwapBusEffects

Validate all indices before editing. Equal swap indices do nothing. Otherwise recreate every entry on the edited bus and dispose its old state after native detachment. Removed resources are never disposed by the bus. Cleanup-hook failures do not undo committed removal/order or leave native references attached.

<a id="isbuseffectenabled"></a>
<a id="setbuseffectenabled"></a>
### IsBusEffectEnabled and SetBusEffectEnabled

Requested flag defaults true and is independent of bypass. Disabled effects pass PCM through; hooks and capture do not run, counters/history are retained. Repeated flags do nothing. Toggle operations serialize with actual mixing and keep instance identity.

<a id="isbusbypassingeffects"></a>
<a id="setbusbypasseffects"></a>
### IsBusBypassingEffects and SetBusBypassEffects

Bypass defaults false. It passes PCM around all public effects without changing enabled flags, ownership or processing state. Final gain, mute/solo, sends and peak metering remain active. Restoring bypass resumes enabled effects. Hooks requesting silence run before bus mute/gain when enabled.

## Effect runtime and verification

Public processing precedes final bus gain because the native submix's ordinary volume occurs before its native effects. An engine-owned final gain FAPO preserves the required order, finite-output validation and post-volume meter. Active stereo pairs retain silent blocks for tails; prepared AudioBusesChannelDisableThresholdDB/Time control expiry by actual mixed frames. Capture opts into inactive silence. An exception in processing/silence hooks or borrowed-resource disposal clears that failed effect block and latches silence until structural recreation. SceneTree reports collected errors once on its owner while still dispatching ordinary frame callbacks. Gain failures similarly silence output; changing gain clears that failure state.

AudioEffectTests checks native ordered PCM, raw capture versus final output, mute/bypass/enable, direct pair projection, activity/tails, factory/reentrancy/disposal failures, graph identity, closure/reopen and warmed managed/custom allocator limits. Public Window hosts and cross-platform/physical limits are described in the [component](../components/audio-playback.md#bus-effects).


Source OnMix callbacks share the effect-processing configuration guard during both native output and owner-thread final transition preparation. They cannot mutate/rebuild the graph or call Lock/Unlock. Passive output-rate/speed reads remain available; see [stream transitions](../components/audio-playback.md#stream-transitions).

Interactive parents now prepare child controls on the audio owner, including paused microphone input and request capacity, then schedule selected child Start/Stop under the shared audio gate. Public microphone controls/disposal retain owner checks; preparation alone does not record. Mixed interactive/randomizer/synchronized graphs share cycle/owner validation. See [interactive streams](../components/audio-playback.md#interactive-streams) for timing, lifecycle, native evidence and limits.
