using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;

internal static class EditorHintTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    internal static void Managed()
    {
        Check(!Engine.IsEditorHint, "A fresh game process defaults to runtime mode.");
        Task.Run(() => Engine.IsEditorHint = true).GetAwaiter().GetResult(); Check(Engine.IsEditorHint, "Host configuration is visible across threads.");
        var descriptor = (PropertyDescriptor<Engine, bool>)Engine.GetSingleton("Engine").GetPropertyList().Single(p => p.Name == nameof(Engine.IsEditorHint));
        Check(!descriptor.IsReadOnly && !descriptor.IsStored, "Host hint is typed transient configuration.");
        descriptor.SetValue((Engine)Engine.GetSingleton("Engine"), false); Check(!Engine.IsEditorHint, "Retained service descriptors share the static host hint.");
        foreach (var editor in new[] { true, false })
        {
            Engine.IsEditorHint = editor;
            using var frames = new SpriteFrames(); frames.AddFrame("default", null); frames.AddFrame("default", null);
            using var a = new Animation { Length = .2 }; using var b = new Animation { Length = .2 }; using var library = new AnimationLibrary(); library.AddAnimation("a", a); library.AddAnimation("b", b);
            var root = new Node(); var sprite = new AnimatedSprite { SpriteFrames = frames, Autoplay = "default" }; var player = new AnimationPlayer { Autoplay = "a" }; player.AddAnimationLibrary("", library); root.AddChild(sprite); root.AddChild(player);
            using var tree = new SceneTree(root);
            Check(sprite.IsPlaying() == !editor && player.IsPlaying() == !editor, "Ready-time sprite and animation autoplay respect the host role.");
            Check(sprite.Autoplay == "default" && player.Autoplay == "a", "Suppression preserves authored startup options.");
            if (editor) { tree.ProcessFrame(.1); Check(sprite.Frame == 0 && player.CurrentAnimationPosition == 0, "Authoring does not advance skipped playback."); }
            sprite.Play(); player.Play("a"); tree.ProcessFrame(.1); Check(sprite.IsPlaying() && player.CurrentAnimationPosition > 0, "Explicit authoring preview executes.");
            player.Stop(); player.AnimationSetNext("a", "b"); player.Play("a"); player.Advance(.2); Check(editor ? !player.IsPlaying() : player.CurrentAnimation == "b", "Editor preview suppresses configured automatic next animations.");
            player.Play("a"); player.Queue("b"); player.Advance(.2); Check(player.CurrentAnimation == "b", "An explicit queue is honored in either host role.");
            Engine.IsEditorHint = !editor; Check(sprite.IsPlaying(), "Changing the hint does not stop existing explicit playback.");
        }
        Engine.IsEditorHint = true;
        using (var detached = new Electron2D.Timer { Autostart = true })
        {
            var rejected = false; try { detached.Notify(Node.NotificationReady); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Detached ready delivery retains the timer's explicit attachment validation.");
        }
        using (var tree = new SceneTree(new Node()))
        {
            var edited = new Node(); tree.Root.AddChild(edited); tree.EditedSceneRoot = edited;
            var authored = new Electron2D.Timer { Autostart = true }; var ui = new Electron2D.Timer { Autostart = true };
            edited.AddChild(authored); tree.Root.AddChild(ui);
            Check(authored.IsStopped() && authored.Autostart && !ui.IsStopped() && !ui.Autostart, "Only edited-scene timer autostart is suppressed; editor UI timers still run.");
            authored.Start(); Check(!authored.IsStopped(), "Explicit timer preview remains available.");
            Engine.IsEditorHint = false; var runtime = new Electron2D.Timer { Autostart = true }; edited.AddChild(runtime);
            Check(!runtime.IsStopped() && !runtime.Autostart, "Edited-scene metadata alone does not change runtime timer startup.");
        }
        Engine.IsEditorHint = false;
        Console.WriteLine($"PASS {_checks} managed editor hint, authoring startup and explicit-preview contracts.");
    }
    internal static void Native(string backend)
    {
        Managed();
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        var folder = Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") ?? Path.Combine(Path.GetTempPath(), "electron2d-editor-hint"); Directory.CreateDirectory(folder);
        var videoFile = Path.GetFullPath("tests/Electron2D.Tests/Fixtures/Video/theora-silent.ogv");
        using var stream = Tone(); using var videoSource = new VideoStreamTheora { File = videoFile };
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red); using var red = ImageTexture.CreateFromImage(image); image.Fill(Colors.Green); using var green = ImageTexture.CreateFromImage(image);
        using var frames = new SpriteFrames(); frames.AddFrame("default", red); frames.AddFrame("default", green); frames.SetAnimationSpeed("default", 5);
        foreach (var editor in new[] { true, false })
            foreach (var transport in new[] { AudioServer.PlaybackType.Stream, AudioServer.PlaybackType.Sample })
            {
                Engine.IsEditorHint = editor;
                using var animation = new Animation { Length = 10 }; using var library = new AnimationLibrary(); library.AddAnimation("a", animation);
                var window = new Window { Size = new(420, 240) };
                var player = new AudioStreamPlayer { Stream = stream, Autoplay = true, PlaybackType = transport };
                var emitter = new AudioStreamEmitter { Stream = stream, Autoplay = true, PlaybackType = transport };
                var sprite = new AnimatedSprite { SpriteFrames = frames, Autoplay = "default", Position = new(40, 40), Scale = new(3, 3) };
                var animations = new AnimationPlayer { Autoplay = "a" }; animations.AddAnimationLibrary("", library);
                var video = new VideoStreamPlayer { Stream = videoSource, Autoplay = true, Loop = true, Position = new(110, 16), Size = new(160, 120), Expand = true };
                foreach (var node in new Node[] { player, emitter, sprite, animations, video }) window.AddChild(node);
                var bus = AudioServer.BusCount; AudioServer.AddBus(); AudioServer.SetBusName(bus, "EditorHintCapture"); using var capture = new AudioEffectCapture { BufferLength = 1 }; AudioServer.AddBusEffect(bus, capture); player.Bus = "EditorHintCapture"; emitter.Bus = "EditorHintCapture";
                var count = 0; long startBytes = 0, idle = 0, active = 0; var measured = false; var requested = false; var started = Stopwatch.GetTimestamp();
                using var texture = window.GetTexture();
                window.Ready += _ =>
                {
                    RenderingServer.SetDefaultClearColor(Colors.Black);
                    Check(Engine.IsEditorHint == editor && RenderingServer.GetCurrentRenderingMethod() == backend, "Rendered host has the selected role and actual backend.");
                    Check(player.IsPlaying() == !editor && video.IsPlaying() == !editor && sprite.IsPlaying() == !editor && animations.IsPlaying() == !editor, "Automatic native startup follows the authoring role.");
                    window.Tree!.ProcessFrameStarted += _ =>
                    {
                        measured = count >= 32 && count < 96 || count >= 112 && count < 176; if (measured) startBytes = GC.GetAllocatedBytesForCurrentThread();
                        if (count >= 96 && count < 176) Engine.IsEditorHint = (count & 1) == 0;
                    };
                    RenderingServer.FramePostDraw += AfterDraw;
                };
                void AfterDraw()
                {
                    if (measured) { var bytes = GC.GetAllocatedBytesForCurrentThread() - startBytes; if (count < 96) idle += bytes; else active += bytes; }
                    count++;
                    if (count == 8)
                    {
                        Check(emitter.IsPlaying() == !editor, "Spatial autoplay is checked on its real first physics step.");
                        Check(player.Autoplay && emitter.Autoplay && video.Autoplay && sprite.Autoplay == "default" && animations.Autoplay == "a", "Authoring retains all startup data.");
                        if (editor) Check(!player.HasStreamPlayback() && !emitter.HasStreamPlayback() && video.StreamPosition == 0 && sprite.Frame == 0, "Skipped startup creates no audio voice or moving source.");
                        player.Play(); emitter.Play(); sprite.Play(); animations.Play("a"); video.Play(); requested = true;
                    }
                    if (count == 24)
                    {
                        Check(requested && player.IsPlaying() && emitter.IsPlaying() && sprite.IsPlaying() && animations.IsPlaying() && video.IsPlaying(), "Explicit audio, spatial, sprite, animation and video preview execute.");
                        var pcm = capture.GetBuffer(capture.GetFramesAvailable()); if (!pcm.Any(v => MathF.Abs(v.X) > .001f || MathF.Abs(v.Y) > .001f) || video.StreamPosition <= 0) { count--; return; }
                        Check(pcm.Length > 0, "Actual native audio reaches the public bus capture.");
                        Check(video.StreamPosition > 0, "Native decoder time advances during explicit preview.");
                        using var output = texture.GetImage()!; var pixel = output.GetPixel(115, 22); Check(pixel.R + pixel.G + pixel.B > .1f, "Actual video pixels differ from the black clear target."); output.SavePNG(Path.Combine(folder, $"hint-{backend}-{editor}-{transport}.png"));

                    }
                    if (count == 176)
                    {
                        Check(idle == 0 && active == 0, $"Prepared hint reads/writes and rendered playback allocate {idle}/{active} managed owner bytes.");
                        window.Tree!.Quit();
                    }
                }
                try { Check(Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 35) throw new TimeoutException("Editor-hint native fixture timeout."); }, window) == 0, "Native host closes."); }
                finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; Engine.IsEditorHint = false; AudioServer.RemoveBus(bus); }
                Check(window.IsDisposed, "Native ownership is released.");
                Console.WriteLine($"PASS native {backend}, editor={editor}, transport={transport}, 64 idle/64 active {idle}/{active} managed bytes.");
            }
        Failure(); Console.WriteLine($"PASS {_checks} editor role/native playback/lifetime contracts on {backend}.");
    }
    private static AudioStreamWAV Tone()
    {
        var data = new byte[48000 * 2]; for (var i = 0; i < 48000; i++) BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(Math.Sin(2 * Math.PI * 440 * i / 48000) * 8000));
        return new AudioStreamWAV { Data = data, SampleFormat = AudioStreamWAV.Format.PCM16, MixRate = 48000, Loop = AudioLoopMode.Forward, LoopEnd = 47999 };
    }
    private sealed class FailingRoot : Node
    { protected override void OnReady() => throw new InvalidOperationException("Injected authoring activation failure"); }
    private static void Failure()
    {
        Engine.IsEditorHint = true; var root = new FailingRoot();
        try { using var tree = new SceneTree(root); throw new InvalidOperationException("Expected activation failure."); }
        catch (AggregateException errors) { Check(errors.Flatten().InnerExceptions.Any(e => e.Message == "Injected authoring activation failure"), "Authoring lifecycle failures propagate normally."); }
        finally { root.Dispose(); Engine.IsEditorHint = false; }
        using var fresh = new SceneTree(new Node()); Check(!Engine.IsEditorHint, "Host-controlled role can be reset after failed authoring activation.");
    }
}
