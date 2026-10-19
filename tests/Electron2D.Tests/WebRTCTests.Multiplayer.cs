using System.Diagnostics;
using Electron2D;
internal static partial class WebRTCTests
{
    private static void Multiplayer()
    {
        using var a = WebRTCPeerConnection.Create(); using var b = WebRTCPeerConnection.Create(); using var server = new WebRTCMultiplayerPeer(); using var client = new WebRTCMultiplayerPeer();
        server.CreateServer([TransferMode.Reliable]); client.CreateClient(7, [TransferMode.Reliable]); server.AddPeer(a, 7); client.AddPeer(b, 1); Link(a, b);
        var serverConnected = 0; var clientConnected = 0; server.PeerConnected += id => { Check(id == 7 && server.HasPeer(7) && server.GetPeer(7)!.Connected, "Server event sees committed membership."); serverConnected++; }; client.PeerConnected += id => clientConnected++;
        a.CreateOffer(); var timer = Stopwatch.StartNew(); while (serverConnected == 0 || clientConnected == 0) { if (timer.Elapsed.TotalSeconds > 15) throw new TimeoutException("Multiplayer negotiation"); server.Poll(); client.Poll(); Thread.Sleep(1); }
        Check(server.GetConnectionStatus() == MultiplayerConnectionStatus.Connected && client.GetConnectionStatus() == MultiplayerConnectionStatus.Connected && server.IsServer() && !client.IsServer() && server.IsServerRelaySupported() && client.IsServerRelaySupported(), "Server/client multiplayer state and relay capability.");
        var info = server.GetPeer(7)!; Check(ReferenceEquals(info.Connection, a) && info.Channels.Count == 4 && info.Channels[0].GetID() == 1 && info.Channels[1].GetMaxPacketLifeTime() == 1 && info.Channels[2].GetMaxPacketLifeTime() == 1 && !info.Channels[2].IsOrdered() && info.Channels[3].GetID() == 4, "Reserved and configured channel identities/reliability.");
        var copy = server.GetPeers(); Check(copy.Count == 1 && copy[7].Connected && server.GetMaxPacketSize() == 1200, "Typed membership snapshot and outgoing capacity.");
        Span<byte> payload = stackalloc byte[48]; Span<byte> received = stackalloc byte[48]; payload.Fill(47); server.SetTargetPeer(7);
        foreach (var mode in new[] { TransferMode.Reliable, TransferMode.UnreliableOrdered, TransferMode.Unreliable })
        {
            server.TransferMode = mode; server.PutPacket(payload); WaitMultiplayer(server, client); Check(client.GetPacketPeer() == 1 && client.GetPacketChannel() == 0 && client.GetPacketMode() == mode, "Actual reserved-channel packet metadata."); client.GetPacket(received); Check(received.SequenceEqual(payload), "Reserved-channel packet bytes.");
        }
        server.TransferChannel = 1; server.TransferMode = TransferMode.Unreliable; server.PutPacket(payload); WaitMultiplayer(server, client); Check(client.GetPacketChannel() == 1 && client.GetPacketMode() == TransferMode.Reliable, "Custom channel owns delivery mode independent of requested default.");
        Reject<ArgumentException>(() => client.GetPacket(new byte[1])); Check(client.GetPacketChannel() == 1 && client.GetPacketMode() == TransferMode.Reliable, "Failed small read preserves selected metadata."); client.GetPacket(received);
        for (var i = 0; i < 32; i++) { server.PutPacket(payload); WaitMultiplayer(server, client); client.GetPacket(received); }
        var start = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) { server.PutPacket(payload); WaitMultiplayer(server, client); client.GetPacket(received); }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - start; Check(bytes == 0, "Prepared multiplayer owner allocations: " + bytes);
        server.TransferChannel = int.MaxValue; Reject<ArgumentOutOfRangeException>(() => server.PutPacket([1])); server.TransferChannel = 0; Reject<ArgumentException>(() => server.PutPacket(new byte[1201]));
        server.RemovePeer(7); Check(!server.HasPeer(7) && !a.IsDisposed && info.Channels.All(c => c.IsDisposed), "Removing membership retires owned channels and preserves caller connection.");
        Console.WriteLine("WebRTC multiplayer reserved/custom modes, typed peers, routing, metadata, small-buffer retry and 32 prepared frames=0 managed owner B passed.");
    }
    private static void WaitMultiplayer(WebRTCMultiplayerPeer a, WebRTCMultiplayerPeer b)
    { var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 5; while (b.GetAvailablePacketCount() == 0) { a.Poll(); b.Poll(); if (Stopwatch.GetTimestamp() > deadline) throw new TimeoutException("Multiplayer packet"); Thread.Sleep(1); } }
}
