namespace Electron2D;

public sealed partial class AudioServer
{
    private string _outputDevice = "Default";
    /// <summary>Gets or sets the playback device by its exact enumerated name.</summary>
    /// <value>Default initially, following the system default playback route.</value>
    /// <remarks>Selection prepares a new native output stream while retaining the mix format, source
    /// playback, bus graph and effect histories. Mixing pauses during the switch. Invalid selection
    /// preserves the prior stream and requested name. The preference survives output closure.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The exact name is unavailable.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/reentrant or native output preparation fails.</exception>
    public string OutputDevice
    {
        get { Check(); return _outputDevice; }
        set { Check(); ArgumentNullException.ThrowIfNull(value); if (_outputDevice == value) return; EnsureNative(); _native!.SetOutput(value); _outputDevice = value; }
    }
    /// <summary>Gets the currently reported native output buffering delay in seconds.</summary>
    /// <returns>The opened SDL device chunk duration plus currently queued source PCM duration.</returns>
    /// <remarks>Prepares output on first use. The live snapshot is refreshed by output callbacks, so
    /// repeated queries allocate no measured managed memory and do not join a driver callback. It describes
    /// driver buffering rather than additional operating-system, transport or physical converter delay.</remarks>
    /// <exception cref="InvalidOperationException">Access is off-owner/reentrant or output buffering is unavailable.</exception>
    public double GetOutputLatency() { Check(); EnsureNative(); return _native!.BufferedOutputLatency; }
}
