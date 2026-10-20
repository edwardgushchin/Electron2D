using Electron2D;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Path = System.IO.Path;

internal static class ScriptRPCTests
{
    private static readonly RPCOptions Any = new(RPCMode.AnyPeer, true);
    private static ScriptRPCActor CreateActor() => new();
    internal static RPCMethod<T, int> Method<T>(uint id, Action<T, int> action) where T : Node => new(id, action, static _ => 4,
        static (v, b) => { BinaryPrimitives.WriteInt32LittleEndian(b, v); return 4; },
        static b => b.Length == 4 ? BinaryPrimitives.ReadInt32LittleEndian(b) : throw new InvalidDataException());
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run(string phase)
    {
        Register();
        if (phase == "child")
        {
            var path = Environment.GetEnvironmentVariable("ELECTRON2D_SCRIPT_RPC_SCENE")!;
            Network(path, false); Network(path, true); Console.WriteLine("Fresh compiled RPC scene passed"); return;
        }
        Local();
        var folder = Path.Combine(Path.GetTempPath(), "e2d-script-rpc-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "actor.e2dscene");
            using (var source = ResourceLoader.Load<Script>(ScriptRPCActor.SourcePath))
            using (var actor = source.New<ScriptRPCActor>())
            using (var scene = new PackedScene())
            { actor.Name = "Actor"; scene.Pack(actor); ResourceSaver.Save(scene, path, SaverFlags.Compress | SaverFlags.SaveBigEndian); }
            if (phase == "native") Native(path); else Fresh(path);
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine("Compiled Script RPC declarations, inheritance, overrides, source/storage and execution passed.");
    }
    private static void Register()
    {
        Reject<ArgumentException>(() => Script.RegisterNodeWithRPC("ScriptRPC.Actor", ScriptRPCActor.SourcePath, CreateActor, new RPCRegistration[65537]));
        Reject<ArgumentException>(() => Script.RegisterNodeWithRPC("ScriptRPC.Actor", ScriptRPCActor.SourcePath, CreateActor, [default]));
        var wrong = Method<ScriptActor>(9101, static (_, _) => { });
        Reject<ArgumentException>(() => Script.RegisterNodeWithRPC("ScriptRPC.Actor", ScriptRPCActor.SourcePath, CreateActor, [new(wrong, Any)]));
        Reject<ArgumentException>(() => Script.RegisterNodeWithRPC("ScriptRPC.Actor", ScriptRPCActor.SourcePath, CreateActor, [new(ScriptRPCActor.Add, Any), new(ScriptRPCActor.Add, Any)]));
        Reject<ArgumentException>(() => Script.RegisterAbstractWithRPC<ScriptRPCActor>("ScriptRPC.Actor", ScriptRPCActor.SourcePath, []));
        var input = new[] { new RPCRegistration(ScriptRPCActor.Add, Any), new RPCRegistration(ScriptRPCActor.Disabled, default) };
        Script.RegisterNodeWithRPC("ScriptRPC.Actor", ScriptRPCActor.SourcePath, CreateActor, input); input[0] = default;
        using var script = ResourceLoader.Load<Script>(ScriptRPCActor.SourcePath);
        Check(ScriptRPCActor.Created == 0 && script.GetRPCConfig().Length == 2, "Copied declarations and metadata need no hidden instance");
        using var early = new ScriptRPCActor(); early.RPCConfig(ScriptRPCBaseActor.Power);
        Script.RegisterAbstractWithRPC<ScriptRPCBaseActor>("ScriptRPC.Base", ScriptRPCBaseActor.SourcePath, [new(ScriptRPCBaseActor.BaseScore, Any), new(ScriptRPCBaseActor.Power, Any)]);
        Check(script.GetRPCConfig().Length == 3 && early.GetNodeRPCConfig().Length == 2, "Late base registration is visible while prior instance removal remains local");
        var declarations = script.GetRPCConfig(); Check(ReferenceEquals(declarations[0].Method, ScriptRPCActor.Add) && ReferenceEquals(declarations[2].Method, ScriptRPCBaseActor.Power), "Derived ID shadows base and preserves declaration order");
        declarations[0] = default; Check(script.GetRPCConfig()[0].Method != null, "Metadata snapshots do not mutate class declarations");
    }
    private static void Local()
    {
        using var script = ResourceLoader.Load<Script>(ScriptRPCActor.SourcePath);
        using var basis = ResourceLoader.Load<Script>(ScriptRPCBaseActor.SourcePath);
        using var unbound = new Script(); Check(unbound.GetRPCConfig().Length == 0 && basis.GetRPCConfig().Length == 2, "Unbound and abstract class metadata");
        ScriptTests.Register(); using var resource = ResourceLoader.Load<Script>(ScriptPaintEffect.SourcePath); Check(resource.GetRPCConfig().Length == 0, "Resource scripts have no Node RPC declarations");
        using var duplicate = (Script)script.Duplicate(); using var fromSource = duplicate.New<ScriptRPCActor>();
        Check(fromSource.GetNodeRPCConfig().Length == 3, "Script construction and duplication retain compiled declarations");
        using var nodeCopy = fromSource.Duplicate(); using var stripped = fromSource.Duplicate(Node.DuplicateFlags.Default & ~Node.DuplicateFlags.Scripts);
        Check(nodeCopy is ScriptRPCActor && nodeCopy.GetNodeRPCConfig().Length == 3 && stripped.GetNodeRPCConfig().Length == 0, "Duplication preserves defaults only for the actual compiled class");
        var snapshot = fromSource.GetNodeRPCConfig(); snapshot[0] = default; Check(fromSource.GetNodeRPCConfig()[0].Method != null, "Node configuration snapshots are independent");
        using var tree = new SceneTree(fromSource);
        fromSource.RPC(ScriptRPCActor.Add, 2); fromSource.RPC(ScriptRPCBaseActor.Power, 3); Check(fromSource.Score == 2 && fromSource.PowerValue == 3 && fromSource.Sender == 1, "Actual derived/base typed invocation");
        Reject<InvalidOperationException>(() => fromSource.RPC(ScriptRPCActor.Disabled, 1));
        Reject<ArgumentException>(() => fromSource.RPCConfig(ScriptRPCBaseActor.BaseScore, Any));
        fromSource.RPCConfig(ScriptRPCActor.Add); Reject<InvalidOperationException>(() => fromSource.RPC(ScriptRPCActor.Add, 1));
        Check(fromSource.GetNodeRPCConfig().Length == 2 && script.GetRPCConfig().Length == 3, "Per-node tombstone suppresses default without changing Script");
        fromSource.RPCConfig(ScriptRPCActor.Add, new RPCOptions(RPCMode.AnyPeer, false)); fromSource.RPC(ScriptRPCActor.Add, 1); Check(fromSource.Score == 2, "Instance policy overrides class call-local");
        fromSource.RPCConfig(ScriptRPCActor.Add, Any); fromSource.RPC(ScriptRPCActor.Add, 1); Check(fromSource.Score == 3, "Explicit default re-enable");
        fromSource.RPCConfig(ScriptRPCActor.Add); fromSource.RPCConfig(ScriptRPCBaseActor.BaseScore, Any); fromSource.RPC(ScriptRPCBaseActor.BaseScore, 1); Check(fromSource.Score == 103, "Removed ID may be deliberately replaced");
        Reject<InvalidOperationException>(() => Task.Run(() => fromSource.GetNodeRPCConfig()).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => fromSource.RPCConfig(ScriptRPCBaseActor.Power)).GetAwaiter().GetResult());
        script.SourceCode += "\n// edited text"; Check(script.GetRPCConfig().Length == 3, "Source edit leaves compiled declarations intact");
        script.Dispose(); Reject<ObjectDisposedException>(() => script.GetRPCConfig());
        var pair = Captured(); pair.Node.Dispose(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Check(!pair.Payload.IsAlive, "Disposed retained Node releases per-instance RPC captures"); GC.KeepAlive(pair.Node);
        using var dead = new ScriptRPCActor(); dead.Dispose(); Reject<ObjectDisposedException>(() => dead.GetNodeRPCConfig()); Reject<ObjectDisposedException>(() => dead.RPCConfig(ScriptRPCActor.Add));
    }
    private sealed class Payload { internal int Value; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Node Node, WeakReference Payload) Captured()
    {
        var payload = new Payload(); var node = new Node(); var token = Method<Node>(9200, (_, value) => payload.Value = value); node.RPCConfig(token, Any); return (node, new WeakReference(payload));
    }
    private static void Fresh(string path)
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(ScriptRPCTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_SCRIPT_RPC_PHASE"] = "child"; start.Environment["ELECTRON2D_SCRIPT_RPC_SCENE"] = path;
        using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("fresh RPC scene"); }
        var text = output.GetAwaiter().GetResult(); Check(child.ExitCode == 0 && text.Contains("Fresh compiled RPC scene passed"), text + error.GetAwaiter().GetResult()); Console.Write(text);
    }
    private static ScriptRPCActor Load(string path) { using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); return (ScriptRPCActor)scene.Instantiate(); }
    private static int Port(bool udp)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, udp ? SocketType.Dgram : SocketType.Stream, udp ? ProtocolType.Udp : ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
    private static void Wait(Action poll, Func<bool> ready)
    {
        var deadline = Environment.TickCount64 + 10000; while (!ready()) { poll(); if (Environment.TickCount64 > deadline) throw new TimeoutException("compiled RPC transport"); Thread.Yield(); }
    }
    private static void Network(string path, bool enet)
    {
        using MultiplayerPeer host = enet ? new ENetMultiplayerPeer() : new WebSocketMultiplayerPeer(); using MultiplayerPeer client = enet ? new ENetMultiplayerPeer() : new WebSocketMultiplayerPeer(); var port = Port(enet);
        if (enet) { ((ENetMultiplayerPeer)host).CreateServer(port); ((ENetMultiplayerPeer)client).CreateClient("127.0.0.1", port); }
        else { ((WebSocketMultiplayerPeer)host).CreateServer(port, "127.0.0.1"); ((WebSocketMultiplayerPeer)client).CreateClient("ws://127.0.0.1:" + port); }
        using var hs = new SceneMultiplayer { MultiplayerPeer = host }; using var cs = new SceneMultiplayer { MultiplayerPeer = client };
        var root = new Node { Name = "Root" }; var hn = Load(path); root.AddChild(hn); var branch = new Node { Name = "Client" }; var cn = Load(path); branch.AddChild(cn); root.AddChild(branch);
        using var tree = new SceneTree(root); tree.SetMultiplayer(hs); tree.SetMultiplayer(cs, branch.GetPath()); void Poll() => tree.ProcessFrame(0);
        Wait(Poll, () => hs.GetPeers().Length == 1 && cs.GetPeers().Length == 1); var id = client.GetUniqueID();
        hn.RPCID(id, ScriptRPCBaseActor.Power, 7); Wait(Poll, () => cn.PowerValue == 7);
        cn.RPCID(1, ScriptRPCActor.Add, 3); Wait(Poll, () => hn.Score == 3); Check(hn.Sender == id, "Fresh file receiver uses derived declaration and original sender");
        hn.RPCConfig(ScriptRPCActor.Add); cn.RPCID(1, ScriptRPCActor.Add, 1); var rejected = false;
        Wait(() => { try { Poll(); } catch (AggregateException) { rejected = true; } }, () => rejected); Check(hn.Score == 3, "Remote invocation honors removed class default");
        hn.RPCConfig(ScriptRPCActor.Add, new RPCOptions(RPCMode.Authority)); cn.RPCID(1, ScriptRPCActor.Add, 1); rejected = false;
        Wait(() => { try { Poll(); } catch (AggregateException) { rejected = true; } }, () => rejected); Check(hn.Score == 3, "Remote authority policy overrides declared AnyPeer");
        hn.RPCConfig(ScriptRPCActor.Add, Any); hn.Fail = true; cn.RPCID(1, ScriptRPCActor.Add, 1); rejected = false;
        Wait(() => { try { Poll(); } catch (AggregateException) { rejected = true; } }, () => rejected); hn.Fail = false; Check(hs.GetRemoteSenderID() == 0, "Failed compiled callback restores sender scope");
        for (var i = 0; i < 32; i++) { var expected = cn.PowerValue + 1; hn.RPCID(id, ScriptRPCBaseActor.Power, 1); Wait(Poll, () => cn.PowerValue == expected); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { var expected = cn.PowerValue + 1; hn.RPCID(id, ScriptRPCBaseActor.Power, 1); var deadline = Environment.TickCount64 + 10000; while (cn.PowerValue != expected) { Poll(); if (Environment.TickCount64 > deadline) throw new TimeoutException(); } }
        var active = GC.GetAllocatedBytesForCurrentThread() - before; before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Poll(); var idle = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(active == 0 && idle == 0, "Prepared class-default RPC active/idle bytes " + active + "/" + idle); Console.WriteLine("64 compiled-default " + (enet ? "ENet" : "WS") + " RPC/frame active + idle intervals: " + active + "/" + idle + " managed owner bytes.");
        tree.SetMultiplayer(null, branch.GetPath()); tree.SetMultiplayer(null);
    }
    private static void Native(string path)
    {
        var renderer = Environment.GetEnvironmentVariable("ELECTRON2D_SCRIPT_RENDERER") ?? "gpu"; var old = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
        try
        {
            using var host = new WebSocketMultiplayerPeer(); using var client = new WebSocketMultiplayerPeer(); var port = Port(false); host.CreateServer(port, "127.0.0.1"); client.CreateClient("ws://127.0.0.1:" + port);
            using var hs = new SceneMultiplayer { MultiplayerPeer = host }; using var cs = new SceneMultiplayer { MultiplayerPeer = client };
            var window = new Window { Size = new(240, 140) }; var hn = Load(path); window.AddChild(hn); var branch = new Node { Name = "Client" }; var cn = Load(path); branch.AddChild(cn); window.AddChild(branch);
            var tint = new ColorRect { Position = new(20, 20), Size = new(160, 80), Color = new(1, 0, 0) }; window.AddChild(tint);
            var sent = false; var drawn = false; var frames = 0; var deadline = Environment.TickCount64 + 15000;
            window.Ready += _ =>
            {
                window.Tree!.SetMultiplayer(hs); window.Tree.SetMultiplayer(cs, branch.GetPath());
                RenderingServer.FramePreDraw += () => { if (cn.Score == 9) tint.Color = new(0, 1, 1); };
                RenderingServer.FramePostDraw += () =>
                {
                    frames++; if (!sent && hs.GetPeers().Length == 1 && cs.GetPeers().Length == 1) { sent = true; hn.RPCID(client.GetUniqueID(), ScriptRPCActor.Add, 9); }
                    if (cn.Score == 9 && tint.Color.G == 1)
                    {
                        using var image = RenderingServer.Service!.Readback(); var pixel = image.GetPixel(60, 60); Check(pixel.G > .9f && pixel.B > .9f && pixel.R < .1f, "Fresh compiled RPC changes actual rendered pixels");
                        if (Environment.GetEnvironmentVariable("ELECTRON2D_SCRIPT_CAPTURE") is { } capture) image.SavePNG(capture);
                        drawn = true; window.Tree!.SetMultiplayer(null, branch.GetPath()); window.Tree.SetMultiplayer(null); window.Tree.Quit();
                    }
                    if (Environment.TickCount64 > deadline) throw new TimeoutException("rendered compiled RPC");
                };
            };
            Check(Engine.Run(window) == 0 && sent && drawn && frames > 1, "Native window/transport/compiled source RPC/render workflow"); Console.WriteLine("Native compiled source/file-scene/RPC pixel acceptance: " + renderer);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, old); }
    }
}
