using Android.App;
using Android.Content.PM;
using Android.Util;
using Electron2D;
using Org.Libsdl.App;

namespace Electron2DAndroidProbe;

[Activity(Label = "Electron2D probe", MainLauncher = true, Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize |
        ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
public sealed class MainActivity : SDLActivity
{
    protected override string[] GetLibraries() => ["SDL3"];

    protected override void Main()
    {
        var scenario = Intent?.GetStringExtra("scenario") ?? "compatibility";
        Log.Info("Electron2DProbe", $"START {scenario}");
        try
        {
            if (scenario == "physics") VerifyPhysics();
            else if (scenario == "gles_shader") Gles2Probe.Run();
            else VerifyCanvas(scenario);
            Log.Info("Electron2DProbe", $"DONE {scenario}");
        }
        catch (Exception error)
        {
            Log.Error("Electron2DProbe", $"FAIL {scenario}: {error}");
        }
    }

    private static void VerifyCanvas(string scenario)
    {
        if (scenario is not ("compatibility" or "gpu" or "shader" or "auto"))
            throw new ArgumentOutOfRangeException(nameof(scenario));
        ProjectSettings.Set(ProjectSettings.RenderingMethod,
            scenario == "compatibility" ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, scenario == "auto");
        if (scenario != "compatibility")
        {
            var drivers = SDL3.SDL.GetNumGPUDrivers();
            Log.Info("Electron2DProbe", $"GPU_DRIVERS {drivers} FORMATS {ShaderCompiler.GetFormats()}");
            for (var i = 0; i < drivers; i++)
                Log.Info("Electron2DProbe", $"GPU_DRIVER {SDL3.SDL.GetGPUDriver(i)}");
            SDL3.SDL.SetLogPriority(SDL3.SDL.LogCategory.GPU, SDL3.SDL.LogPriority.Verbose);
            Log.Info("Electron2DProbe", $"GPU_SUPPORTS_SPIRV {SDL3.SDL.GPUSupportsShaderFormats(SDL3.SDL.GPUShaderFormat.SPIRV, "vulkan")}");
        }
        using var shader = scenario == "shader" ? LoadShader() : null;
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(640, 360) };
        var node = new ProbeNode { ProcessEnabled = true, Material = material };
        window.AddChild(node);
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!;
            Log.Info("Electron2DProbe", $"DRIVER {scenario} {RenderingServer.GetCurrentRenderingDriverName()}");
            RenderingServer.FramePostDraw += () =>
            {
                if (node.Sampled) return;
                using var frame = server.Readback();
                var pixel = frame.GetPixel(4, 4);
                Log.Info("Electron2DProbe", $"PIXEL {scenario} {pixel.R:F3},{pixel.G:F3},{pixel.B:F3}");
                if (pixel.R < 0.8f || pixel.G > 0.2f || pixel.B > 0.2f)
                    throw new InvalidOperationException("The red canvas pixel was not rendered.");
                node.Sampled = true;
            };
        };
        Engine.MaxFPS = 60;
        var code = Engine.Run(window);
        if (code != 0 || !node.Sampled) throw new InvalidOperationException("Canvas frame or clean exit was missing.");
    }

    private static Shader LoadShader()
    {
        using var input = typeof(MainActivity).Assembly.GetManifestResourceStream("Probe.CanvasHLSL.spv")!;
        using var memory = new MemoryStream();
        input.CopyTo(memory);
        return Shader.CreateFromSPIRV(memory.ToArray());
    }

    private static void VerifyPhysics()
    {
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var boxShape = new RectangleShape { Size = new(20, 20) };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody();
        body.AddChild(new CollisionShape { Shape = boxShape });
        root.AddChild(floor);
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Log.Info("Electron2DProbe", $"PHYSICS {body.GlobalPosition.Y:F3} {body.LinearVelocity.Y:F3}");
        if (body.GlobalPosition.Y is <= 75 or >= 85 || MathF.Abs(body.LinearVelocity.Y) >= 2)
            throw new InvalidOperationException("Body did not rest on the floor.");
    }

    private sealed class ProbeNode : Entity
    {
        private int _frames;
        internal bool Sampled;
        protected override void OnDraw() => DrawRect(new(0, 0, 640, 360), Colors.Red);
        protected override void OnProcess(double delta)
        {
            if (++_frames == 10) Tree!.Quit();
        }
    }
}
