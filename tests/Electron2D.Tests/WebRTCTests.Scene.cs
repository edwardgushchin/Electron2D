using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
internal static partial class WebRTCTests
{
    private static readonly RPCMethod<RTCPlayer, int> UpdateScore = new(411, static (node, score) => { node.Score += score; node.Sender = node.Multiplayer.GetRemoteSenderID(); }, static _ => 4, static (value, bytes) => { BinaryPrimitives.WriteInt32LittleEndian(bytes, value); return 4; }, static bytes => bytes.Length == 4 ? BinaryPrimitives.ReadInt32LittleEndian(bytes) : throw new InvalidDataException());
    private sealed class RTCPlayer : Node
    {
        internal int Score, Sender;
        internal RTCPlayer() { RPCConfig(UpdateScore, new RPCOptions(RPCMode.AnyPeer)); }
    }
    private static void Scene()
    {
        using var a = WebRTCPeerConnection.Create(); using var b = WebRTCPeerConnection.Create(); using var server = new WebRTCMultiplayerPeer(); using var client = new WebRTCMultiplayerPeer();
        server.CreateServer(); client.CreateClient(7); server.AddPeer(a, 7); client.AddPeer(b, 1); Link(a, b);
        using var host = new SceneMultiplayer { MultiplayerPeer = server }; using var remote = new SceneMultiplayer { MultiplayerPeer = client };
        var root = new Node { Name = "Root" }; var hostPlayer = new RTCPlayer { Name = "Player" }; root.AddChild(hostPlayer); var branch = new Node { Name = "Client" }; var remotePlayer = new RTCPlayer { Name = "Player" }; branch.AddChild(remotePlayer); root.AddChild(branch);
        using var tree = new SceneTree(root); tree.SetMultiplayer(host); tree.SetMultiplayer(remote, branch.GetPath()); a.CreateOffer();
        void Wait(Func<bool> ready) { var timer = Stopwatch.StartNew(); while (!ready()) { tree.ProcessFrame(0); if (timer.Elapsed.TotalSeconds > 15) throw new TimeoutException("WebRTC scene RPC"); Thread.Sleep(1); } }
        Wait(() => host.GetPeers().Length == 1 && remote.GetPeers().Length == 1);
        remotePlayer.RPCID(1, UpdateScore, 23); Wait(() => hostPlayer.Score == 23); Check(hostPlayer.Sender == 7 && host.GetRemoteSenderID() == 0, "SceneTree WebRTC RPC retains caller identity and restores sender scope.");
        hostPlayer.RPCID(7, UpdateScore, 17); Wait(() => remotePlayer.Score == 17); Check(remotePlayer.Sender == 1, "Server RPC reaches client scene node.");
        var received = false; remote.PeerPacket += (id, bytes) => { Check(id == 1 && bytes.SequenceEqual("scene"u8), "Scene raw bytes retain source."); received = true; }; host.SendBytes("scene"u8, 7); Wait(() => received);
        Console.WriteLine("WebRTC SceneTree automatic polling, bidirectional typed RPC and scene raw packets passed.");
    }
}
