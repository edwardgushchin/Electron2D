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
    internal float GetInputMixRateCore() => Volatile.Read(ref _input)?.MixRate ?? EnsureInput().MixRate;
    internal string[] GetInputDeviceListCore() { Check(); return AudioInputDevice.Devices(); }
    internal string InputDeviceCore
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
        if (!ProjectSettings.GetWithOverride(ProjectSettings.AudioDriverEnableInput)) throw new InvalidOperationException("Enable audio/driver/enable_input before starting input capture.");
    }
    internal void SetInputDeviceActiveCore(bool active)
    {
        Check(); lock (_gate)
        {
            if (active && _manualInput && _input?.Active == true) return;
            if (active) { RequireInputEnabled(); EnsureInput().SetActive(true); }
            else _input?.SetActive(false);
            _manualInput = active;
        }
    }
    internal int GetInputBufferLengthFramesCore() { Check(); return _input?.Capacity ?? 0; }
    internal int GetInputFramesAvailableCore() { Check(); return _input?.Available ?? 0; }
    internal Vector2[] GetInputFramesCore(int frames) { Check(); ArgumentOutOfRangeException.ThrowIfNegative(frames); return _input?.Read(frames) ?? []; }
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
