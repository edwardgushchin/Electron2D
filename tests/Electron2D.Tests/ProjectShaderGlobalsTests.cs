using System.Diagnostics;
using Electron2D;
using Kind = Electron2D.RenderingServer.GlobalShaderParameterType;

internal static class ProjectShaderGlobalsTests
{
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunManaged()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "project-shader-globals-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
            using var texture = ImageTexture.CreateFromImage(image); using var array = new TextureArray(); array.CreateFromImages([image]);
            ResourceSaver.Save(texture, System.IO.Path.Combine(folder, "map.e2dres")); ResourceSaver.Save(array, System.IO.Path.Combine(folder, "array.e2dres"));
            var seed = ShaderGlobalParametersTests.Fixture(texture, array);
            using var settings = new ProjectSettingsRegistry(folder, folder + "-user");
            var gain = Register(settings, seed, true); var path = System.IO.Path.Combine(folder, "project.e2d"); settings.SaveCustom(path);
            var prepared = ShaderGlobalParameters.PrepareProject(settings);
            try
            {
                Verify(seed, prepared); var loaded = prepared.GetTexture("map", false)!; Check(!ReferenceEquals(loaded, texture), "Configured images are independently owned, rather than borrowed originals.");
                settings.Set(gain, new() { Type = Kind.Float, Value = .25f }); Check(prepared.Get<float>("gain") == .5f, "Prepared session does not track later project edits.");
                settings.SetFeatureOverride(gain, "project_global_test", new() { Type = Kind.Float, Value = .75f }); settings.AddCustomFeature("project_global_test");
                var next = ShaderGlobalParameters.PrepareProject(settings);
                try { Check(next.Get<float>("gain") == .75f && prepared.Get<float>("gain") == .5f, "Preparation samples active features independently."); }
                finally { next.RetireProjectImages(); }
            }
            finally { prepared.RetireProjectImages(); }
            Check(!texture.IsDisposed && !array.IsDisposed, "Preparation never disposes source resources.");
            using (var restored = new ProjectSettingsRegistry(folder, folder + "-restored"))
            {
                restored.LoadCustom(path); Register(restored, seed, false); var loaded = ShaderGlobalParameters.PrepareProject(restored);
                try { Verify(seed, loaded); } finally { loaded.RetireProjectImages(); }
            }
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(ProjectShaderGlobalsTests).Assembly.Location);
            start.ArgumentList.Add("--project-shader-globals-read"); start.ArgumentList.Add(folder);
            using var child = Process.Start(start)!; Check(child.WaitForExit(30000) && child.ExitCode == 0, "Fresh project restoration.");
            VerifyFailures(folder);
        }
        finally { Directory.Delete(folder, true); }
        Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Authoring and preparation require no native host.");
        Console.WriteLine("Project shader globals: 25 roles, saved/fresh process, features, snapshots and failure cleanup passed.");
    }
    internal static void RunArchive(string folder)
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image); using var array = new TextureArray(); array.CreateFromImages([image]);
        var seed = ShaderGlobalParametersTests.Fixture(texture, array);
        using var settings = new ProjectSettingsRegistry(folder, folder + "-child"); settings.LoadCustom(System.IO.Path.Combine(folder, "project.e2d")); Register(settings, seed, false);
        var loaded = ShaderGlobalParameters.PrepareProject(settings); try { Verify(seed, loaded); } finally { loaded.RetireProjectImages(); }
    }
    private static ProjectSetting<GlobalShaderParameterSettings<float>> Register(ProjectSettingsRegistry settings, ShaderGlobalParameters seed, bool write, List<Action>? remove = null, string imagePrefix = "res://")
    {
        ProjectSetting<GlobalShaderParameterSettings<T>> Number<T>(string name, Kind kind, T value) where T : notnull
        {
            var fallback = typeof(T) == typeof(float[]) ? (T)(object)new float[kind == Kind.Mat3 ? 9 : 16] : typeof(T) == typeof(string) ? (T)(object)"" : default!;
            var setting = new ProjectSetting<GlobalShaderParameterSettings<T>>("shader_globals/" + name, new() { Type = kind, Value = fallback });
            settings.Register(setting); remove?.Add(() => settings.Unregister(setting));
            if (write) settings.Set(setting, new() { Type = kind, Value = value }); return setting;
        }
        ProjectSetting<GlobalShaderParameterSettings<float>>? gain = null;
        foreach (var name in seed.Names())
        {
            var kind = seed.GetType(name);
            switch (kind)
            {
                case Kind.Bool: Number(name, kind, seed.Get<bool>(name)); break;
                case Kind.BVec2 or Kind.BVec3 or Kind.BVec4 or Kind.Int: Number(name, kind, seed.Get<int>(name)); break;
                case Kind.UInt: Number(name, kind, seed.Get<uint>(name)); break;
                case Kind.Float: gain = Number(name, kind, seed.Get<float>(name)); break;
                case Kind.IVec2 or Kind.UVec2: Number(name, kind, seed.Get<Vector2i>(name)); break;
                case Kind.IVec3 or Kind.UVec3: Number(name, kind, seed.Get<Vector3i>(name)); break;
                case Kind.IVec4 or Kind.UVec4: Number(name, kind, seed.Get<Vector4i>(name)); break;
                case Kind.Rect2i: Number(name, kind, seed.Get<Rect2i>(name)); break;
                case Kind.Vec2: Number(name, kind, seed.Get<Vector2>(name)); break;
                case Kind.Vec3: Number(name, kind, seed.Get<Vector3>(name)); break;
                case Kind.Vec4: Number(name, kind, seed.Get<Vector4>(name)); break;
                case Kind.Color: Number(name, kind, seed.Get<Color>(name)); break;
                case Kind.Rect2: Number(name, kind, seed.Get<Rect2>(name)); break;
                case Kind.Mat2 or Kind.Transform2D: Number(name, kind, seed.Get<Transform>(name)); break;
                case Kind.Mat3 or Kind.Mat4: Number(name, kind, seed.GetMatrix(name)); break;
                case Kind.Sampler2D: Number(name, kind, imagePrefix + "map.e2dres"); break;
                case Kind.Sampler2DArray: Number(name, kind, imagePrefix + "array.e2dres"); break;
                default: throw new InvalidOperationException("Fixture role.");
            }
        }
        return gain!;
    }
    private static void Verify(ShaderGlobalParameters seed, ShaderGlobalParameters actual)
    {
        Check(actual.Names().SequenceEqual(seed.Names()), "All configured names restore.");
        foreach (var name in seed.Names())
        {
            var kind = seed.GetType(name); Check(actual.GetType(name) == kind, "Logical role " + name);
            void Same<T>() where T : unmanaged => Check(EqualityComparer<T>.Default.Equals(actual.Get<T>(name), seed.Get<T>(name)), "Numeric value " + name);
            switch (kind)
            {
                case Kind.Bool: Same<bool>(); break;
                case Kind.BVec2 or Kind.BVec3 or Kind.BVec4 or Kind.Int: Same<int>(); break;
                case Kind.UInt: Same<uint>(); break;
                case Kind.Float: Same<float>(); break;
                case Kind.IVec2 or Kind.UVec2: Same<Vector2i>(); break;
                case Kind.IVec3 or Kind.UVec3: Same<Vector3i>(); break;
                case Kind.IVec4 or Kind.UVec4: Same<Vector4i>(); break;
                case Kind.Rect2i: Same<Rect2i>(); break;
                case Kind.Vec2: Same<Vector2>(); break;
                case Kind.Vec3: Same<Vector3>(); break;
                case Kind.Vec4: Same<Vector4>(); break;
                case Kind.Color: Same<Color>(); break;
                case Kind.Rect2: Same<Rect2>(); break;
                case Kind.Mat2 or Kind.Transform2D: Same<Transform>(); break;
                case Kind.Mat3 or Kind.Mat4: Check(actual.GetMatrix(name).SequenceEqual(seed.GetMatrix(name)), "Copied matrix " + name); break;
                case Kind.Sampler2D: Check(actual.GetTexture(name, false) is Texture { IsDisposed: false }, "Loaded ordinary image."); break;
                case Kind.Sampler2DArray: Check(actual.GetTexture(name, true) is TextureLayered { IsDisposed: false }, "Loaded image array."); break;
            }
        }
    }
    private static void VerifyFailures(string folder)
    {
        using var settings = new ProjectSettingsRegistry(folder, folder + "-bad");
        var data = new ProjectSetting<GlobalShaderParameterSettings<float>>("shader_globals/gain", new() { Type = Kind.Float, Value = .5f }); settings.Register(data);
        settings.Set(data, new() { Version = 2, Type = Kind.Float, Value = 1 }); Reject<InvalidDataException>(() => ShaderGlobalParameters.PrepareProject(settings));
        settings.Set(data, new() { Type = Kind.UInt, Value = 1 }); Reject<ArgumentException>(() => ShaderGlobalParameters.PrepareProject(settings));
        settings.Set(data, new() { Type = Kind.Max, Value = 1 }); Reject<ArgumentException>(() => ShaderGlobalParameters.PrepareProject(settings));
        settings.Set(data, new() { Type = Kind.Float, Value = 1 });
        var bad = new ProjectSetting<int>("shader_globals/wrong", 1); settings.Register(bad); Reject<InvalidDataException>(() => ShaderGlobalParameters.PrepareProject(settings)); settings.Unregister(bad);
        using var loader = new TrackingLoader(); ResourceLoader.AddResourceFormatLoader(loader, atFront: true);
        try
        {
            var first = new ProjectSetting<GlobalShaderParameterSettings<string>>("shader_globals/first", new() { Type = Kind.Sampler2D, Value = "res://one.globaltest" }); settings.Register(first);
            var alias = new ProjectSetting<GlobalShaderParameterSettings<string>>("shader_globals/alias", new() { Type = Kind.Sampler2D, Value = "res://one.globaltest" }); settings.Register(alias);
            var second = new ProjectSetting<GlobalShaderParameterSettings<string>>("shader_globals/second", new() { Type = Kind.Sampler2D, Value = "res://two.globaltest" }); settings.Register(second);
            var success = ShaderGlobalParameters.PrepareProject(settings);
            Check(loader.Loaded.Count == 2 && ReferenceEquals(success.GetTexture("first", false), success.GetTexture("alias", false)), "One owned image per normalized path with aliases.");
            success.GetTexture("first", false)!.Disposed += _ => throw new InvalidOperationException("cleanup consumer");
            Reject<AggregateException>(success.RetireProjectImages); Check(loader.Loaded.All(image => image.IsDisposed), "A disposal callback cannot strand other owned images.");
            var matrix = new ProjectSetting<GlobalShaderParameterSettings<float[]>>("shader_globals/late", new() { Type = Kind.Mat3, Value = [1, 2] }); settings.Register(matrix);
            Reject<ArgumentException>(() => ShaderGlobalParameters.PrepareProject(settings)); Check(loader.Loaded.Count == 4 && loader.Loaded.All(image => image.IsDisposed), "Late invalid matrix retires previously loaded images.");
            settings.Set(matrix, new() { Type = Kind.Mat3, Value = null! }); Reject<ArgumentException>(() => ShaderGlobalParameters.PrepareProject(settings)); Check(loader.Loaded.All(image => image.IsDisposed), "Null matrices retire staged images too.");
            settings.Unregister(matrix);
            settings.Set(second, new() { Type = Kind.Sampler2D, Value = null! }); Reject<InvalidDataException>(() => ShaderGlobalParameters.PrepareProject(settings));
            settings.Set(second, new() { Type = Kind.Sampler2DArray, Value = "res://one.globaltest" }); Reject<ArgumentException>(() => ShaderGlobalParameters.PrepareProject(settings));
            Check(loader.Loaded.All(image => image.IsDisposed), "Conflicting image role cannot leak the shared resource.");
            settings.Set(second, new() { Type = Kind.Sampler2D, Value = "" }); var empty = ShaderGlobalParameters.PrepareProject(settings);
            Check(empty.GetTexture("second", false) is null, "Empty configured image remains explicitly unassigned."); empty.RetireProjectImages();
        }
        finally { ResourceLoader.RemoveResourceFormatLoader(loader); }
        settings.Set(data, new() { Type = Kind.Float, Value = 1 });
        var wrongJSON = "[shader_globals]\ngain={\"Version\":1,\"Type\":13,\"Value\":1,\"unknown\":true}\n"; using var file = new ConfigFile(); file.Parse(wrongJSON);
        var path = System.IO.Path.Combine(folder, "bad.cfg"); File.WriteAllText(path, wrongJSON); Reject<InvalidDataException>(() => settings.LoadCustom(path));
        Check(settings.Get(data).Value == 1, "Rejected unknown schema fields preserve registered values.");
    }
    internal static void RunNative(string profile)
    {
        var compatibility = profile == "compatibility"; var oldMethod = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "project-global-native-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); var remove = new List<Action>();
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var source = ImageTexture.CreateFromImage(image); using var array = new TextureArray(); array.CreateFromImages([image]);
        ResourceSaver.Save(source, System.IO.Path.Combine(folder, "map.e2dres")); ResourceSaver.Save(array, System.IO.Path.Combine(folder, "array.e2dres"));
        using var shader = Shader.CreateFromSPIRV(ShaderGlobalParametersTests.Module(profile == "hlsl" ? "HLSL" : "GLSL", "vert"), ShaderGlobalParametersTests.Module(profile == "hlsl" ? "HLSL" : "GLSL", "frag"));
        using var material = new ShaderMaterial { Shader = shader };
        var seed = ShaderGlobalParametersTests.Fixture(source, array); Resource? loadedImage = null, loadedArray = null;
        try
        {
            var gain = Register(ProjectSettings.Service, seed, true, remove, folder + System.IO.Path.DirectorySeparatorChar);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            for (var run = 0; run < (compatibility ? 1 : 2); run++)
            {
                using var window = new Window { Size = new(96, 48) }; var sceneOverride = new ShaderGlobalsOverride();
                if (!compatibility && run == 0) sceneOverride.SetShaderParameter<float>("gain", Kind.Float, .75f);
                window.AddChild(sceneOverride); window.AddChild(new Box { Position = new(8, 8), Material = compatibility ? null : material });
                var frames = 0; var currentRun = run;
                window.Ready += _ =>
                {
                    Check(RenderingServer.GetCurrentRenderingMethod() == (compatibility ? "compatibility" : "gpu"), "Effective project renderer.");
                    Check(RenderingServer.GlobalShaderParameterGetList().Length == 25, "All project globals exist before Ready.");
                    Check(RenderingServer.GlobalShaderParameterGet<float>("gain") == (currentRun == 0 ? .5f : .25f), "Startup snapshots the current project value before scene callbacks.");
                    loadedImage = RenderingServer.GlobalShaderParameterGetTexture("map"); loadedArray = RenderingServer.GlobalShaderParameterGetLayeredTexture("layers");
                    Check(loadedImage is { IsDisposed: false } && loadedArray is { IsDisposed: false }, "Project images load before scene readiness.");
                    RenderingServer.SetDefaultClearColor(Colors.Black);
                    if (currentRun == 0) ProjectSettings.Set(gain, new() { Type = Kind.Float, Value = .25f });
                    RenderingServer.FramePostDraw += Drawn;
                };
                void Drawn()
                {
                    using var pixels = window.GetTexture().GetImage()!;
                    var value = currentRun == 0 ? frames == 0 ? .75f : .5f : .25f;
                    var color = pixels.GetPixel(compatibility ? 12 : 8 + 24 + (int)(16 * value) + 4, 12);
                    if (compatibility) Check(color.R > .99f && color.G > .99f && color.B > .99f, "Compatibility keeps ordinary rendering with configured registry data.");
                    else Check(MathF.Abs(color.R - value * value) < .01f && MathF.Abs(color.G - value * value * .5f) < .01f && MathF.Abs(color.B - value * value * .25f) < .01f, "All project global roles feed actual vertex and fragment output: " + color);
                    frames++;
                    if (frames == 1 && currentRun == 0 && !compatibility) sceneOverride.SetShaderParameter<float>("gain", Kind.Float, null);
                    else { RenderingServer.FramePostDraw -= Drawn; window.Tree!.Quit(); }
                }
                Check(Engine.Run(window) == 0 && frames >= 1, "Configured window completed.");
                Check(loadedImage is { IsDisposed: true } && loadedArray is { IsDisposed: true }, "Renderer shutdown retires its configured resources.");
            }
            var late = new ProjectSetting<GlobalShaderParameterSettings<float[]>>("shader_globals/late_failure", new() { Type = Kind.Mat4, Value = [1] });
            ProjectSettings.Register(late);
            var reachedReady = false; var failed = new Window { Size = new(32, 32) }; failed.Ready += _ => reachedReady = true;
            try
            {
                Reject<ArgumentException>(() => Engine.Run(failed));
                Check(failed.IsDisposed && !reachedReady && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Invalid prepared data aborts before Ready and closes the native session.");
            }
            finally { ProjectSettings.Unregister(late); }
            var recovered = new Window { Size = new(32, 32) }; recovered.Ready += _ => { Check(RenderingServer.GlobalShaderParameterGetList().Length == 25, "Recovery publishes only the complete valid registry."); recovered.Tree!.Quit(); };
            Check(Engine.Run(recovered) == 0, "A failed project preparation does not retain the engine reservation.");
            Check(!source.IsDisposed && !array.IsDisposed && !RenderingServer.IsAvailable, "Original resources and session lifetime remain independent.");
        }
        finally
        {
            foreach (var action in remove) action(); ProjectSettings.Set(ProjectSettings.RenderingMethod, oldMethod); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback); Directory.Delete(folder, true);
        }
        Console.WriteLine("Project shader globals " + profile + ": Ready ordering, scene overrides, real pixels, restart and owned image retirement passed.");
    }
    private sealed class Box : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 16, 16), Colors.White); }
    private sealed class TrackingLoader : ResourceFormatLoader
    {
        internal readonly List<ImageTexture> Loaded = [];
        public override string[] GetRecognizedExtensions() => ["globaltest"];
        public override bool HandlesType(Type type) => type == typeof(Texture);
        public override Type? GetResourceType(string path) => typeof(ImageTexture);
        public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
        { using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); var texture = ImageTexture.CreateFromImage(image); Loaded.Add(texture); return texture; }
        public override bool Exists(string path) => true;
    }
}
