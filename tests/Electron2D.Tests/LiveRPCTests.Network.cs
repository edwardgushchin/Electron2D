using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Electron2D;
using Path = System.IO.Path;

internal sealed partial class LiveRPCTests
{
    private static int Port(bool udp)
    { using var socket = new Socket(AddressFamily.InterNetwork, udp ? SocketType.Dgram : SocketType.Stream, udp ? ProtocolType.Udp : ProtocolType.Tcp); socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); return ((IPEndPoint)socket.LocalEndPoint!).Port; }
    private static void Wait(Action poll, Func<bool> ready)
    { var deadline = Environment.TickCount64 + 20000; while (!ready()) { poll(); if (Environment.TickCount64 >= deadline) throw new TimeoutException("Live RPC protocol deadline."); Thread.Yield(); } }
    private static RigidBody Body(Script source)
    { var body = source.New<RigidBody>(); body.Name = "Body"; body.GravityScale = 0; body.CanSleep = false; body.LinearDampMode = RigidBody.DampMode.Replace; body.LinearDamp = 0; return body; }
    private static void NetworkGuards(Script forward, Script reverse)
    {
        using var host = new ENetMultiplayerPeer(); using var client = new ENetMultiplayerPeer(); var port = Port(true); host.CreateServer(port); client.CreateClient("127.0.0.1", port);
        using var hs = new SceneMultiplayer { MultiplayerPeer = host }; using var cs = new SceneMultiplayer { MultiplayerPeer = client };
        using var hw = new World(PhysicsServer.Backend.CPU); using var cw = new World(PhysicsServer.Backend.CPU);
        var hr = new SubViewport { Name = "Root", World = hw }; var cr = new SubViewport { Name = "Root", World = cw };
        var receiver = Body(forward); var sender = Body(forward); hr.AddChild(receiver); cr.AddChild(sender);
        using var ht = new SceneTree(hr); using var ct = new SceneTree(cr); ht.SetMultiplayer(hs); ct.SetMultiplayer(cs);
        void Poll() { ht.ProcessFrame(0); ct.ProcessFrame(0); }
        Wait(Poll, () => hs.GetPeers().Length == 1 && cs.GetPeers().Length == 1);
        SizeHook = () => { SizeHook = null; sender.SetScript(reverse); };
        Reject<InvalidOperationException>(() => sender.RPCID(1, LiveRPCForward.Drive, 1));
        for (var i = 0; i < 8; i++) Poll(); Check(StateFor(receiver).Calls == 0 && StateFor(sender).Mode == -1, "Encoder source replacement rejects before sending or local invocation.");
        sender.SetScript(forward); var original = new IOException("size callback"); SizeHook = () => throw original;
        try { Expect(original, () => sender.RPCID(1, LiveRPCForward.Drive, 1)); } finally { SizeHook = null; }
        var old = StateFor(receiver); DecodeHook = () => { DecodeHook = null; receiver.SetScript(null); receiver.SetScript(forward); };
        sender.RPCID(1, LiveRPCForward.Drive, 1); Rejected();
        Check(old.Releases == 1 && StateFor(receiver).Calls == 0 && receiver.GetNodeRPCConfig()[0].Method == LiveRPCForward.Drive, "Decoder retire/rebind to the identical token is rejected by binding revision.");
        DecodeHook = () => { DecodeHook = null; receiver.RPCConfig(LiveRPCForward.Drive, new(RPCMode.Authority)); };
        sender.RPCID(1, LiveRPCForward.Drive, 1); Rejected(); Check(StateFor(receiver).Calls == 0, "Decoder policy changes are revalidated before the handler."); receiver.RPCConfig(LiveRPCForward.Drive, Any);
        DecodeHook = () => { DecodeHook = null; receiver.Name = "Changed"; };
        sender.RPCID(1, LiveRPCForward.Drive, 1); Rejected(); Check(StateFor(receiver).Calls == 0, "Decoder route changes cannot invoke a stale cached receiver route."); receiver.Name = "Body";
        SwitchAfterDrive = reverse; sender.RPCID(1, LiveRPCForward.Drive, 2); sender.RPCID(1, LiveRPCForward.Drive, 3);
        Wait(Poll, () => StateFor(receiver).Mode == -1 && StateFor(receiver).Calls == 1);
        Check(StateFor(receiver).Value == -3 && hs.GetRemoteSenderID() == 0, "Queued wire IDs use the new handler at delivery; sender scope is restored after failures and replacement.");
        receiver.SetScript(forward); var retiring = StateFor(receiver);
        DecodeHook = () => { DecodeHook = null; receiver.Dispose(); }; sender.RPCID(1, LiveRPCForward.Drive, 1); Rejected();
        Check(receiver.IsDisposed && retiring.Calls == 0 && retiring.Releases == 1, "Decoder disposal retires the source and cannot invoke its disposed receiver.");
        void Rejected()
        { var rejected = false; Wait(() => { try { Poll(); } catch (AggregateException) { rejected = true; } }, () => rejected); Check(hs.GetRemoteSenderID() == 0, "Failed codec dispatch restores remote sender scope."); }
    }
    private static Process WorkerStart(string role, int port, bool enet, PhysicsServer.Backend backend, bool separate, string scenePath)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { Assembly.GetExecutingAssembly().Location, "--live-rpc-worker", role, port.ToString(System.Globalization.CultureInfo.InvariantCulture), enet ? "enet" : "ws", backend == PhysicsServer.Backend.GPU ? "gpu" : "cpu", separate ? "1" : "0", scenePath }) start.ArgumentList.Add(arg);
        foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("ELECTRON2D_TEST_", StringComparison.Ordinal)).ToArray()) start.Environment.Remove(key);
        start.Environment["SDL_VIDEODRIVER"] = backend == PhysicsServer.Backend.GPU ? "wayland" : "dummy";
        start.Environment["VK_DRIVER_FILES"] = start.Environment["VK_ICD_FILENAMES"] = backend == PhysicsServer.Backend.GPU ? "/usr/share/vulkan/icd.d/nvidia_icd.json" : "/nonexistent/e2d-no-gpu.json";
        start.Environment["DOTNET_TieredCompilation"] = "0"; return Process.Start(start)!;
    }
    private static void Cohort(PhysicsServer.Backend clientBackend, bool separate, bool enet, string scenePath)
    {
        var port = Port(enet); using var server = WorkerStart("server", port, enet, PhysicsServer.Backend.CPU, separate, scenePath); var serverErrors = server.StandardError.ReadToEndAsync();
        Process? client = null;
        try
        {
            var prelude = "";
            for (var line = 0; ; line++)
            {
                var ready = server.StandardOutput.ReadLineAsync(); Check(line < 16 && ready.Wait(30000) && ready.Result is not null, "CPU server did not become ready: " + prelude);
                if (ready.Result == "LIVE_RPC_READY") break;
                prelude += ready.Result + "\n";
            }
            client = WorkerStart("client", port, enet, clientBackend, separate, scenePath); var co = client.StandardOutput.ReadToEndAsync(); var ce = client.StandardError.ReadToEndAsync(); var so = server.StandardOutput.ReadToEndAsync();
            if (!client.WaitForExit(60000)) throw new TimeoutException("Live RPC client.");
            if (!server.WaitForExit(30000)) throw new TimeoutException("Live RPC CPU server.");
            Check(client.ExitCode == 0 && server.ExitCode == 0 && co.Result.Contains("LIVE_RPC_CLIENT_PASS", StringComparison.Ordinal) && so.Result.Contains("LIVE_RPC_SERVER_PASS", StringComparison.Ordinal), "Separate live RPC process failure: " + co.Result + ce.Result + so.Result + serverErrors.Result);
            Console.WriteLine($"Separate live RPC CPU server/{clientBackend} client, {(enet ? "ENet" : "WS")}, separate={separate}: passed.");
        }
        finally { if (client is not null) { if (!client.HasExited) { client.Kill(true); client.WaitForExit(5000); } client.Dispose(); } if (!server.HasExited) { server.Kill(true); server.WaitForExit(5000); } }
    }
    internal static void Worker(string role, int port, bool enet, PhysicsServer.Backend backend, bool separate, string scenePath)
    {
        Register(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        using var forward = ResourceLoader.Load<Script>(LiveRPCForward.SourcePath); using var reverse = ResourceLoader.Load<Script>(LiveRPCReverse.SourcePath);
        using MultiplayerPeer peer = enet ? new ENetMultiplayerPeer() : new WebSocketMultiplayerPeer();
        if (role == "server") { if (enet) ((ENetMultiplayerPeer)peer).CreateServer(port); else ((WebSocketMultiplayerPeer)peer).CreateServer(port, "127.0.0.1"); }
        else { if (enet) ((ENetMultiplayerPeer)peer).CreateClient("127.0.0.1", port); else ((WebSocketMultiplayerPeer)peer).CreateClient("ws://127.0.0.1:" + port); }
        using var api = new SceneMultiplayer { MultiplayerPeer = peer }; using var world = new World(backend);
        using var packed = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.IgnoreDeep); var body = (RigidBody)packed.Instantiate();
        Check(body.GetType() == typeof(RigidBody) && StateFor(body).Mode == 1 && body.GetNodeRPCConfig().Length == 3 && body.Children.OfType<CollisionShape>().Count() == 1, "Fresh-process scene reconstruction restores live RPC defaults without runtime overrides.");
        body.Position = Vector2.Zero; body.LinearVelocity = Vector2.Zero; var root = new SubViewport { Name = "Root", World = world }; root.AddChild(body);
        using var tree = new SceneTree(root); tree.SetMultiplayer(api); var rid = body.GetRID(); var id = body.InstanceID; var rejected = false;
        void Poll() { try { tree.ProcessFrame(0); } catch (AggregateException) { if (role != "server") throw; rejected = true; } }
        void Ack(int value) { var token = StateFor(body).Mode == 1 ? LiveRPCForward.Ack : LiveRPCReverse.Ack; body.RPCID(api.GetPeers()[0], token, value); }
        void Physics(float expected)
        { var before = body.Position.X; tree.PhysicsFrame(1d / 60); Check(body.LinearVelocity.X == expected && (expected > 0 ? body.Position.X > before : expected < 0 ? body.Position.X < before : body.Position.X == before) && body.GetRID() == rid && body.InstanceID == id, "Separate-process RPC drives selected physics without replacing the owner."); }
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Separate process selected its requested solver without fallback.");
        if (role == "server")
        {
            Check(backend == PhysicsServer.Backend.CPU && !File.ReadAllText("/proc/self/maps").Contains("libnvidia", StringComparison.OrdinalIgnoreCase), "Headless CPU server requires no GPU driver.");
            body.RPCConfig(LiveRPCForward.Drive, new(RPCMode.AnyPeer, false, TransferMode.Reliable, 1));
            Console.WriteLine("LIVE_RPC_READY"); Console.Out.Flush(); Wait(Poll, () => api.GetPeers().Length == 1);
            Wait(Poll, () => StateFor(body).Calls == 1); Physics(3); Ack(11);
            var old = StateFor(body); SwitchAfterDrive = reverse; Wait(Poll, () => StateFor(body).Mode == -1 && StateFor(body).Calls == 1);
            Check(old.Releases == 1 && StateFor(body).Value == -2 && body.GetNodeRPCConfig().Single(r => r.Method.ID == 9911).Options.Channel == 1, "CPU server swaps queued handlers and retains instance policy by ID."); Physics(-2); Ack(22);
            Wait(Poll, () => StateFor(body).Calls == 2); Physics(-4);
            body.RPCConfig(LiveRPCReverse.Drive); body.SetScript(forward); Ack(33); Wait(Poll, () => rejected);
            Check(StateFor(body).Calls == 0 && body.GetNodeRPCConfig().All(r => r.Method.ID != 9911) && api.GetRemoteSenderID() == 0, "Source replacement retains suppression and rejects the remote method."); Physics(0); Ack(44);
            Wait(Poll, () => StateFor(body).Extra == 99); Ack(55); Wait(Poll, () => api.GetPeers().Length == 0);
            Check(!File.ReadAllText("/proc/self/maps").Contains("libnvidia", StringComparison.OrdinalIgnoreCase), "CPU server remained GPU-free through all physical/network operations.");
            Console.WriteLine("LIVE_RPC_SERVER_PASS");
        }
        else
        {
            Wait(Poll, () => api.GetPeers().Length == 1); body.RPCID(1, LiveRPCForward.Drive, 3); Wait(Poll, () => StateFor(body).Ack == 11);
            body.RPCID(1, LiveRPCForward.Drive, 1); body.RPCID(1, LiveRPCForward.Drive, 2); Wait(Poll, () => StateFor(body).Ack == 22);
            var old = StateFor(body); body.SetScript(reverse); Check(old.Releases == 1, "Client swaps independent source state.");
            body.RPC(LiveRPCReverse.Drive, 4); Physics(-4); Wait(Poll, () => StateFor(body).Ack == 33);
            body.RPCID(1, LiveRPCReverse.Drive, 5); Wait(Poll, () => StateFor(body).Ack == 44);
            body.RPCID(1, LiveRPCBase.Extra, 99); Wait(Poll, () => StateFor(body).Ack == 55);
            peer.DisconnectPeer(1); Wait(Poll, () => api.GetPeers().Length == 0); peer.Close(); Console.WriteLine("LIVE_RPC_CLIENT_PASS");
        }
        tree.SetMultiplayer(null);
    }
}
