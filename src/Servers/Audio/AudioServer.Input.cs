namespace Electron2D;

public sealed partial class AudioServer
{
    private AudioInputDevice? _input;
    private string _inputDevice = "Default";
    private bool _manualInput;
    private readonly List<AudioStreamPlaybackMicrophone> _microphones = [];
    private int _inputRate;
    private int _queuedInputReservations;
    internal void PrepareQueuedInput(bool reserve)
    {
        Check(); lock (_gate) { _ = EnsureInput(); if (reserve) { _microphones.EnsureCapacity(checked(_queuedInputReservations + _microphones.Count + 1)); _queuedInputReservations++; } }
    }
    internal void ReleaseQueuedInputReservation() { Check(); lock (_gate) { if (_queuedInputReservations <= 0) throw new InvalidOperationException("Input reservation is absent."); _queuedInputReservations--; } }
    internal AudioInputDevice AcquireQueuedInput(AudioStreamPlaybackMicrophone playback)
    {
        if (!Monitor.IsEntered(_gate)) throw new InvalidOperationException("Scheduled input control requires the audio gate.");
        RequireInputEnabled(); var input = CurrentInput ?? throw new InvalidOperationException("Prepare input on its owner before scheduled activation.");
        var exists = _microphones.Contains(playback); if (!exists && _microphones.Count == _microphones.Capacity) throw new InvalidOperationException("Scheduled input capacity was not prepared.");
        input.SetActive(true); if (!exists) _microphones.Add(playback); return input;
    }
    internal void ReleaseQueuedInput(AudioStreamPlaybackMicrophone playback)
    {
        if (!Monitor.IsEntered(_gate)) throw new InvalidOperationException("Scheduled input control requires the audio gate.");
        ReleaseInputCore(playback);
    }
    internal AudioInputDevice PreparedInput => EnsureInput();
    internal float PreparedInputMixRate => Volatile.Read(ref _inputRate);
    private AudioInputDevice EnsureInput()
    {
        Check(); lock (_gate) { if (_input is null) { var input = new AudioInputDevice(_inputDevice); Volatile.Write(ref _inputRate, input.MixRate); Volatile.Write(ref _input, input); } return _input; }
    }
    /// <summary>Gets the current prepared recording frequency, opening a paused input device when needed.</summary>
    /// <returns>The actual opened input device's frequency in Hz, independent of the output rate.</returns>
    /// <remarks>First preparation requires the audio owner. Once prepared this query is safe on a mix thread.
    /// Preparation does not record; activation requires AudioDriverEnableInput.</remarks>
    /// <exception cref="InvalidOperationException">Preparation is off-owner or the native device is unavailable.</exception>
    /// <exception cref="NotSupportedException">The opened device has unsupported frequency or quantum dimensions.</exception>
    public float GetInputMixRate() => Volatile.Read(ref _input)?.MixRate ?? EnsureInput().MixRate;
    /// <summary>Gets copied recording device names, including the system-default selector.</summary>
    /// <returns>A caller-owned array starting with Default; duplicate native names appear once.</returns>
    /// <exception cref="InvalidOperationException">Access is off-owner or native enumeration fails.</exception>
    public string[] GetInputDeviceList() { Check(); return AudioInputDevice.Devices(); }
    /// <summary>Gets or sets the recording device by its exact enumerated name.</summary>
    /// <value>Default initially. A successful switch resets input history and reader cursors.</value>
    /// <remarks>Switching prepares a replacement before committing; failure retains the previous device and capture.
    /// Microphones follow the new device and frequency. Device selection is an explicit preparation operation.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The named device is absent.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or native preparation fails.</exception>
    /// <exception cref="NotSupportedException">The opened device has unsupported frequency or quantum dimensions.</exception>
    public string InputDevice
    {
        get { Check(); return _inputDevice; }
        set
        {
            Check(); ArgumentNullException.ThrowIfNull(value); lock (_gate)
            {
                if (value == _inputDevice) return;
                var next = new AudioInputDevice(value);
                try { if (_input?.Active == true) next.SetActive(true); }
                catch { next.Dispose(); throw; }
                var previous = _input; _inputDevice = value; Volatile.Write(ref _inputRate, next.MixRate); Volatile.Write(ref _input, next); previous?.Dispose();
            }
        }
    }
    private static void RequireInputEnabled()
    {
        if (!ProjectSettings.Instance.GetWithOverride(ProjectSettings.AudioDriverEnableInput)) throw new InvalidOperationException("Enable audio/driver/enable_input before starting input capture.");
    }
    /// <summary>Starts or pauses the shared recording device explicitly.</summary>
    /// <param name="active">True starts capture; false pauses capture even when microphone playbacks remain active.</param>
    /// <remarks>Repeated requests are idempotent. Activation resets empty input history only when the native device
    /// was stopped. Typed exceptions replace error return codes. Disabling the project setting gates future starts.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner, input is disabled or native activation fails.</exception>
    public void SetInputDeviceActive(bool active)
    {
        Check(); lock (_gate)
        {
            if (active && _manualInput && _input?.Active == true) return;
            if (active) { RequireInputEnabled(); EnsureInput().SetActive(true); }
            else _input?.SetActive(false);
            _manualInput = active;
        }
    }
    /// <summary>Gets the prepared input ring's capacity in stereo frames.</summary>
    /// <returns>Four native recording quanta; zero before input preparation or after engine closure.</returns>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    public int GetInputBufferLengthFrames() { Check(); return _input?.Capacity ?? 0; }
    /// <summary>Gets unread stereo input frames for the server reader.</summary>
    /// <returns>Zero through input capacity. Overflow drops the oldest unread frames.</returns>
    /// <exception cref="InvalidOperationException">Access is off-owner or capture failed.</exception>
    public int GetInputFramesAvailable() { Check(); return _input?.Available ?? 0; }
    /// <summary>Copies and consumes exactly the requested stereo input frames.</summary>
    /// <param name="frames">Nonnegative number of frames; X is left and Y is right.</param>
    /// <returns>A caller-owned array; empty without consumption if insufficient input is available.</returns>
    /// <remarks>The server reader is independent of microphone cursors. Native conversion supplies finite stereo
    /// float PCM clamped to [-1,1]; nonfinite native frames become silence. Reading is permitted while capture is paused.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or capture failed.</exception>
    public Vector2[] GetInputFrames(int frames) { Check(); ArgumentOutOfRangeException.ThrowIfNegative(frames); return _input?.Read(frames) ?? []; }
    internal AudioInputDevice AcquireInput(AudioStreamPlaybackMicrophone playback)
    {
        Check(); lock (_gate) { RequireInputEnabled(); var input = EnsureInput(); var exists = _microphones.Contains(playback); if (!exists) _microphones.EnsureCapacity(checked(_microphones.Count + _queuedInputReservations + 1)); input.SetActive(true); if (!exists) _microphones.Add(playback); return input; }
    }
    internal void ReleaseInput(AudioStreamPlaybackMicrophone playback)
    {
        Check(); lock (_gate) ReleaseInputCore(playback);
    }
    private void ReleaseInputCore(AudioStreamPlaybackMicrophone playback)
    {
        if (!_microphones.Remove(playback)) return;
        if (_microphones.Count == 0 && !_manualInput)
        {
            try { _input?.SetActive(false); }
            catch { var failed = _input; Volatile.Write(ref _input, null); failed?.Dispose(); throw; }
        }
    }
    internal AudioInputDevice? CurrentInput => Volatile.Read(ref _input);
    private void CloseInput()
    {
        lock (_gate)
        {
            foreach (var microphone in _microphones) microphone.CloseInput();
            _microphones.Clear(); _manualInput = false; var input = _input; Volatile.Write(ref _input, null); input?.Dispose();
        }
    }
}
