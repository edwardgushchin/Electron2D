namespace Electron2D;

public sealed partial class AudioServer
{
    private string _outputDevice = "Default";
    internal static double OutputLatencySnapshot => Singleton.IsValueCreated ? Volatile.Read(ref Singleton.Value._native)?.BufferedOutputLatency ?? 0 : 0;
    internal string OutputDeviceCore
    {
        get { Check(); return _outputDevice; }
        set { Check(); ArgumentNullException.ThrowIfNull(value); if (_outputDevice == value) return; EnsureNative(); _native!.SetOutput(value); _outputDevice = value; }
    }
    internal double GetOutputLatencyCore() { Check(); EnsureNative(); return _native!.BufferedOutputLatency; }
}
