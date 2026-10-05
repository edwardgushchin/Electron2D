namespace Electron2D;

public sealed partial class AudioServer
{
    /// <summary>Inserts an enabled borrowed effect resource into a bus's ordered chain.</summary>
    /// <param name="busIndex">Live bus index, including Master.</param>
    /// <param name="effect">Live resource; each stereo pair receives independent instance state.</param>
    /// <param name="atPosition">Insertion index; negative or at/after the end appends.</param>
    /// <remarks>Prepares output and fresh instances for the complete edited chain. Factory failure preserves
    /// the chain; a native graph failure closes output with committed configuration retained for retry.</remarks>
    /// <exception cref="ArgumentNullException">The resource is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/reentrant or preparation fails.</exception>
    public static void AddBusEffect(int busIndex, AudioEffect effect, int atPosition = -1) => Service.AddBusEffectCore(busIndex, effect, atPosition);

    /// <summary>Gets the number of effects in a bus's configured chain.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <returns>The count, including disabled effects.</returns>
    public static int GetBusEffectCount(int busIndex) => Service.GetBusEffectCountCore(busIndex);

    /// <summary>Gets a borrowed configured effect resource.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <returns>The exact resource supplied to AddBusEffect.</returns>
    public static AudioEffect GetBusEffect(int busIndex, int effectIndex) => Service.GetBusEffectCore(busIndex, effectIndex);

    /// <summary>Gets a borrowed live effect instance for an output stereo pair.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <param name="channel">Stereo pair, zero by default.</param>
    /// <returns>Bus-owned state; structural effect edits/output closure invalidate it. Routing edits retain it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid.</exception>
    public static AudioEffectInstance GetBusEffectInstance(int busIndex, int effectIndex, int channel = 0) => Service.GetBusEffectInstanceCore(busIndex, effectIndex, channel);

    /// <summary>Removes an effect and recreates the edited chain's processing state.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <remarks>The removed resource remains caller-owned; old instances are disposed after native detachment.</remarks>
    public static void RemoveBusEffect(int busIndex, int effectIndex) => Service.RemoveBusEffectCore(busIndex, effectIndex);

    /// <summary>Swaps two configured effects and recreates their bus's complete processing state.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">First effect.</param>
    /// <param name="byEffectIndex">Second effect; equal indices do nothing.</param>
    public static void SwapBusEffects(int busIndex, int effectIndex, int byEffectIndex) => Service.SwapBusEffectsCore(busIndex, effectIndex, byEffectIndex);

    /// <summary>Gets an effect's requested enabled flag independently of bus bypass.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <returns>True initially.</returns>
    public static bool IsBusEffectEnabled(int busIndex, int effectIndex) => Service.IsBusEffectEnabledCore(busIndex, effectIndex);

    /// <summary>Enables or disables an effect without recreating its state.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="effectIndex">Live effect index.</param>
    /// <param name="enabled">Requested flag; bypass still suppresses processing.</param>
    public static void SetBusEffectEnabled(int busIndex, int effectIndex, bool enabled) => Service.SetBusEffectEnabledCore(busIndex, effectIndex, enabled);

    /// <summary>Gets whether a bus bypasses its public effects.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <returns>False initially; gain and peak metering remain active.</returns>
    public static bool IsBusBypassingEffects(int busIndex) => Service.IsBusBypassingEffectsCore(busIndex);

    /// <summary>Bypasses or restores a bus's public effects while retaining instance state/enabled flags.</summary>
    /// <param name="busIndex">Live bus index.</param>
    /// <param name="enable">True to bypass.</param>
    public static void SetBusBypassEffects(int busIndex, bool enable) => Service.SetBusBypassEffectsCore(busIndex, enable);

    /// <summary>Gets the current prepared recording frequency, opening a paused input device when needed.</summary>
    /// <returns>The actual opened input device's frequency in Hz, independent of the output rate.</returns>
    /// <remarks>First preparation requires the audio owner. Once prepared this query is safe on a mix thread.
    /// Preparation does not record; activation requires AudioDriverEnableInput.</remarks>
    /// <exception cref="InvalidOperationException">Preparation is off-owner or the native device is unavailable.</exception>
    /// <exception cref="NotSupportedException">The opened device has unsupported frequency or quantum dimensions.</exception>
    public static float GetInputMixRate() => Service.GetInputMixRateCore();

    /// <summary>Gets copied recording device names, including the system-default selector.</summary>
    /// <returns>A caller-owned array starting with Default; duplicate native names appear once.</returns>
    /// <exception cref="InvalidOperationException">Access is off-owner or native enumeration fails.</exception>
    public static string[] GetInputDeviceList() => Service.GetInputDeviceListCore();

    /// <summary>Gets or sets the recording device by its exact enumerated name.</summary>
    /// <value>Default initially. A successful switch resets input history and reader cursors.</value>
    /// <remarks>Switching prepares a replacement before committing; failure retains the previous device and capture.
    /// Microphones follow the new device and frequency. Device selection is an explicit preparation operation.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The named device is absent.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or native preparation fails.</exception>
    /// <exception cref="NotSupportedException">The opened device has unsupported frequency or quantum dimensions.</exception>
    public static string InputDevice
    {
        get => Service.InputDeviceCore;
        set => Service.InputDeviceCore = value;
    }

    /// <summary>Starts or pauses the shared recording device explicitly.</summary>
    /// <param name="active">True starts capture; false pauses capture even when microphone playbacks remain active.</param>
    /// <remarks>Repeated requests are idempotent. Activation resets empty input history only when the native device
    /// was stopped. Typed exceptions replace error return codes. Disabling the project setting gates future starts.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner, input is disabled or native activation fails.</exception>
    public static void SetInputDeviceActive(bool active) => Service.SetInputDeviceActiveCore(active);

    /// <summary>Gets the prepared input ring's capacity in stereo frames.</summary>
    /// <returns>Four native recording quanta; zero before input preparation or after engine closure.</returns>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static int GetInputBufferLengthFrames() => Service.GetInputBufferLengthFramesCore();

    /// <summary>Gets unread stereo input frames for the server reader.</summary>
    /// <returns>Zero through input capacity. Overflow drops the oldest unread frames.</returns>
    /// <exception cref="InvalidOperationException">Access is off-owner or capture failed.</exception>
    public static int GetInputFramesAvailable() => Service.GetInputFramesAvailableCore();

    /// <summary>Copies and consumes exactly the requested stereo input frames.</summary>
    /// <param name="frames">Nonnegative number of frames; X is left and Y is right.</param>
    /// <returns>A caller-owned array; empty without consumption if insufficient input is available.</returns>
    /// <remarks>The server reader is independent of microphone cursors. Native conversion supplies finite stereo
    /// float PCM clamped to [-1,1]; nonfinite native frames become silence. Reading is permitted while capture is paused.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or capture failed.</exception>
    public static Vector2[] GetInputFrames(int frames) => Service.GetInputFramesCore(frames);

    /// <summary>Gets or sets the playback device by its exact enumerated name.</summary>
    /// <value>Default initially, following the system default playback route.</value>
    /// <remarks>Selection prepares a new native output stream while retaining the mix format, source
    /// playback, bus graph and effect histories. Mixing pauses during the switch. Invalid selection
    /// preserves the prior stream and requested name. The preference survives output closure.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The exact name is unavailable.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/reentrant or native output preparation fails.</exception>
    public static string OutputDevice
    {
        get => Service.OutputDeviceCore;
        set => Service.OutputDeviceCore = value;
    }

    /// <summary>Gets the currently reported native output buffering delay in seconds.</summary>
    /// <returns>The opened SDL device chunk duration plus currently queued source PCM duration.</returns>
    /// <remarks>Prepares output on first use. The live snapshot is refreshed by output callbacks, so
    /// repeated queries allocate no measured managed memory and do not join a driver callback. It describes
    /// driver buffering rather than additional operating-system, transport or physical converter delay.
    /// Snapshot publication and reads remain atomic on 32-bit hosts.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner/reentrant or output buffering is unavailable.</exception>
    public static double GetOutputLatency() => Service.GetOutputLatencyCore();

    /// <summary>Gets whether a live stream has a prepared sample snapshot in this engine session.</summary>
    /// <param name="stream">Borrowed stream identity.</param>
    /// <returns>True after successful registration; engine closure clears registration.</returns>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static bool IsStreamRegisteredAsSample(AudioStream stream) => Service.IsStreamRegisteredAsSampleCore(stream);

    /// <summary>Registers or replaces an immutable finite PCM snapshot for native sample playback.</summary>
    /// <param name="stream">Borrowed sample-capable stream.</param>
    /// <remarks>Preparation is cold and transactional. Existing voices retain their old snapshot; explicit
    /// registration captures later edits for subsequent voices. The weak-key cache does not retain unused resources.</remarks>
    /// <exception cref="NotSupportedException">The stream cannot be sampled.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or sampling reenters registration.</exception>
    public static void RegisterStreamAsSample(AudioStream stream) => Service.RegisterStreamAsSampleCore(stream);

    /// <summary>Gets or sets the number of bus records, including the required Master.</summary>
    /// <value>One initially; values must be between one and 255.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside the valid range.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    public static int BusCount
    {
        get => Service.BusCountCore;
        set => Service.BusCountCore = value;
    }

    /// <summary>Gets or sets the positive global playback-rate multiplier.</summary>
    /// <value>One initially; actual player pitch combines this value with its local scale.</value>
    /// <remarks>Passive reads are atomic on every host; configuration writes require the audio owner.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonpositive or nonfinite.</exception>
    /// <exception cref="NotSupportedException">Prepared native sample effective pitch is unsupported; the old configuration is restored.</exception>
    public static float PlaybackSpeedScale
    {
        get => Service.PlaybackSpeedScaleCore;
        set => Service.PlaybackSpeedScaleCore = value;
    }

    /// <summary>Occurs after the bus list or routing graph changes.</summary>
    public static event Action? BusLayoutChanged
    {
        add => Service.BusLayoutChangedCore += value;
        remove => Service.BusLayoutChangedCore -= value;
    }

    /// <summary>Occurs after a bus rename commits.</summary>
    /// <remarks>Arguments are the index, old name and unique new name.</remarks>
    public static event Action<int, string, string>? BusRenamed
    {
        add => Service.BusRenamedCore += value;
        remove => Service.BusRenamedCore -= value;
    }

    /// <summary>Adds a uniquely named bus at an index or appends it.</summary>
    /// <param name="atPosition">Insertion index; minus one appends. Master remains at zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">The insertion range or bus limit is invalid.</exception>
    public static void AddBus(int atPosition = -1) => Service.AddBusCore(atPosition);

    /// <summary>Removes a non-Master bus, redirecting unresolved sends to Master.</summary>
    /// <param name="index">Live nonzero bus index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid or identifies Master.</exception>
    public static void RemoveBus(int index) => Service.RemoveBusCore(index);

    /// <summary>Moves a non-Master bus; minus one moves it to the end.</summary>
    /// <param name="index">Live non-Master source index.</param>
    /// <param name="toIndex">Destination insertion index; minus one appends.</param>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or identifies Master.</exception>
    public static void MoveBus(int index, int toIndex) => Service.MoveBusCore(index, toIndex);

    /// <summary>Gets the index of a bus by exact name.</summary>
    /// <param name="busName">Nonnull exact bus name.</param>
    /// <returns>Minus one when absent.</returns>
    public static int GetBusIndex(string busName) => Service.GetBusIndexCore(busName);

    /// <summary>Gets a bus name.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>The unique exact name.</returns>
    public static string GetBusName(int index) => Service.GetBusNameCore(index);

    /// <summary>Renames a bus, updating named sends and resolving duplicate names.</summary>
    /// <param name="index">Live non-Master index.</param>
    /// <param name="name">Requested nonnull name.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid or identifies Master.</exception>
    public static void SetBusName(int index, string name) => Service.SetBusNameCore(index, name);

    /// <summary>Gets the requested named send target.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>The requested target; invalid or later targets resolve to Master during mixing.</returns>
    public static string GetBusSend(int index) => Service.GetBusSendCore(index);

    /// <summary>Sets a non-Master bus send target.</summary>
    /// <param name="index">Live non-Master bus index.</param>
    /// <param name="send">Nonnull requested target name.</param>
    public static void SetBusSend(int index, string send) => Service.SetBusSendCore(index, send);

    /// <summary>Gets bus gain in decibels.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>Gain in dB.</returns>
    public static float GetBusVolumeDB(int index) => Service.GetBusVolumeDBCore(index);

    /// <summary>Gets the linear bus gain.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>Ten raised to dB/20.</returns>
    public static float GetBusVolumeLinear(int index) => Service.GetBusVolumeLinearCore(index);

    /// <summary>Sets finite bus gain in decibels.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="volumeDB">Finite gain.</param>
    /// <exception cref="ArgumentOutOfRangeException">The gain is not finite.</exception>
    public static void SetBusVolumeDB(int index, float volumeDB) => Service.SetBusVolumeDBCore(index, volumeDB);

    /// <summary>Sets nonnegative finite linear bus gain.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="volumeLinear">Zero mutes through negative infinity decibels.</param>
    public static void SetBusVolumeLinear(int index, float volumeLinear) => Service.SetBusVolumeLinearCore(index, volumeLinear);

    /// <summary>Gets whether a bus is mute.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>The current flag.</returns>
    public static bool IsBusMute(int index) => Service.IsBusMuteCore(index);

    /// <summary>Sets the bus mute flag and updates live native gains.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="enable">New flag.</param>
    public static void SetBusMute(int index, bool enable) => Service.SetBusMuteCore(index, enable);

    /// <summary>Gets whether a bus is solo.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>The current flag.</returns>
    public static bool IsBusSolo(int index) => Service.IsBusSoloCore(index);

    /// <summary>Sets the bus solo flag and updates live native gains.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="enable">New flag.</param>
    public static void SetBusSolo(int index, bool enable) => Service.SetBusSoloCore(index, enable);

    /// <summary>Gets the number of stereo channel pairs on a bus.</summary>
    /// <param name="index">Live bus index.</param>
    /// <returns>Actual native output channels divided by two.</returns>
    public static int GetBusChannels(int index) => Service.GetBusChannelsCore(index);

    /// <summary>Gets the measured left-channel bus peak in decibels.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="channel">Stereo pair index.</param>
    /// <returns>Measured peak, with silence bounded at minus 200 dB.</returns>
    public static float GetBusPeakVolumeLeftDB(int index, int channel) => Service.GetBusPeakVolumeLeftDBCore(index, channel);

    /// <summary>Gets the measured right-channel bus peak in decibels.</summary>
    /// <param name="index">Live bus index.</param>
    /// <param name="channel">Stereo pair index.</param>
    /// <returns>Measured peak, with silence bounded at minus 200 dB.</returns>
    public static float GetBusPeakVolumeRightDB(int index, int channel) => Service.GetBusPeakVolumeRightDBCore(index, channel);

    /// <summary>Gets the active audio mix frequency.</summary>
    /// <returns>44100 Hz before output preparation, otherwise actual native mix frequency.</returns>
    public static float GetMixRate() => Service.GetMixRateCore();

    /// <summary>Gets the actual native audio driver.</summary>
    /// <returns>The SDL audio driver after output preparation.</returns>
    public static string GetDriverName() => Service.GetDriverNameCore();

    /// <summary>Lists actual output devices.</summary>
    /// <returns>A caller-owned device-name array.</returns>
    public static string[] GetOutputDeviceList() => Service.GetOutputDeviceListCore();

    /// <summary>Gets the native output channel arrangement.</summary>
    /// <returns>The current speaker selector.</returns>
    public static SpeakerMode GetSpeakerMode() => Service.GetSpeakerModeCore();

    /// <summary>Gets elapsed time since the last actual native mix quantum.</summary>
    /// <returns>Seconds, zero before native output exists.</returns>
    public static double GetTimeSinceLastMix() => Service.GetTimeSinceLastMixCore();

    /// <summary>Gets the estimated time until the next native quantum.</summary>
    /// <returns>Nonnegative seconds based on actual quantum size and mix timestamp.</returns>
    public static double GetTimeToNextMix() => Service.GetTimeToNextMixCore();

    /// <summary>Locks configuration for an explicit caller-owned critical section.</summary>
    /// <remarks>Pair with Unlock in finally; this protects the owned bus/playback state, not arbitrary game code.</remarks>
    public static void Lock() => Service.LockCore();

    /// <summary>Releases one matching configuration lock.</summary>
    /// <exception cref="SynchronizationLockException">The calling thread owns no matching lock.</exception>
    public static void Unlock() => Service.UnlockCore();

}
