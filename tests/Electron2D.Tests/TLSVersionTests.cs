using Electron2D;

internal static class TLSVersionTests
{
    // Fresh-process profiles supply incompatible runtime or transport binaries.
    internal static void Reject(string profile)
    {
        if (profile == "runtime")
        {
            using var transport = new StreamPeerBuffer();
            using var peer = new StreamPeerTLS();
            using var options = TLSOptions.ClientUnsafe();
            MustReject(() => peer.ConnectToStream(transport, "localhost", options));
            if (peer.GetStatus() != TLSStatus.Error || peer.GetStream() is not null || transport.IsDisposed)
                throw new Exception("Rejected TLS startup must retain error state and release its borrowed transport.");
            if (OperatingSystem.IsLinux() && File.ReadAllText("/proc/self/maps").Contains("libcrypto.so.4", StringComparison.Ordinal))
                throw new Exception("An incompatible runtime must reject before loading a second crypto provider.");
        }
        else if (profile == "bridge")
        {
            using var peer = WebRTCPeerConnection.Create();
            MustReject(() => peer.Initialize());
            if (peer.GetConnectionState() != WebRTCPeerConnection.ConnectionState.New)
                throw new Exception("Rejected WebRTC startup must not publish a transport.");
        }
        else throw new ArgumentException("Unknown TLS version rejection profile.");
        Console.WriteLine("TLS version rejection passed: " + profile);
    }

    private static void MustReject(Action action)
    {
        try { action(); }
        catch (PlatformNotSupportedException) { return; }
        throw new Exception("An incompatible native cryptography cohort must reject before transport creation.");
    }
}
