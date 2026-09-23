using Electron2D;

internal static class AnimatedSpriteTests
{
    internal static void Run()
    {
        Resources(); Playback(); Selection(); Packing(); DrawingAndThreads(); Failures();
        Console.WriteLine("SpriteFrames and AnimatedSprite managed checks passed.");
    }

    private static void Resources()
    {
        using var frames = new SpriteFrames();
        using var texture = new ImageTexture();
        var changes = 0; frames.Changed += _ => changes++;
        Check(frames.GetAnimationNames().SequenceEqual(new[] { "default" }) && frames.GetAnimationSpeed("default") == 5 &&
            frames.GetAnimationLoopMode("default") == SpriteFrames.LoopMode.Linear && frames.GetFrameCount("default") == 0, "Library defaults.");
        frames.AddAnimation("z"); frames.AddAnimation("a"); frames.AddAnimation("");
        frames.SetAnimationSpeed("z", 0); frames.SetAnimationLoopMode("z", SpriteFrames.LoopMode.PingPong);
#pragma warning disable CS0618
        Check(!frames.GetAnimationLoop("z"), "Legacy loop query only means linear.");
        frames.SetAnimationLoop("z", true); Check(frames.GetAnimationLoop("z"), "Legacy loop setter.");
        frames.SetAnimationLoop("z", false);
#pragma warning restore CS0618
        frames.DuplicateAnimation("z", "copy"); frames.RenameAnimation("copy", "renamed"); frames.RemoveAnimation("absent");
        Check(changes == 0 && frames.GetAnimationNames().SequenceEqual(new[] { "", "a", "default", "renamed", "z" }), "Name/rate/loop edits stay silent and names are sorted.");
        frames.AddFrame("default", texture, -10); frames.AddFrame("default", null, 2, 0); frames.AddFrame("default", texture, 3, int.MaxValue);
        Check(changes == 3 && frames.GetFrameTexture("default", 0) is null && frames.GetFrameDuration("default", 1) == .01f &&
            frames.GetFrameDuration("default", 2) == 3, "Insertion, append, null and minimum duration.");
        frames.SetFrame("default", 2, texture, 3);
        frames.SetFrame("default", 999, texture, float.NaN);
        Check(changes == 4 && frames.GetFrameTexture("default", 999) is null && frames.GetFrameDuration("default", 999) == 1, "Past-end defaults and same-value notification.");
        Reject<ArgumentOutOfRangeException>(() => frames.GetFrameTexture("default", -1));
        Reject<ArgumentOutOfRangeException>(() => frames.GetFrameDuration("default", -1));
        Reject<ArgumentOutOfRangeException>(() => frames.SetFrame("default", -1, null));
        Reject<ArgumentOutOfRangeException>(() => frames.RemoveFrame("default", 999));
        Reject<ArgumentOutOfRangeException>(() => frames.AddFrame("default", null, float.PositiveInfinity));
        Reject<ArgumentOutOfRangeException>(() => frames.SetAnimationSpeed("default", double.NaN));
        Reject<ArgumentOutOfRangeException>(() => frames.SetAnimationSpeed("default", -1));
        Reject<ArgumentOutOfRangeException>(() => frames.SetAnimationLoopMode("default", (SpriteFrames.LoopMode)3));
        Reject<ArgumentException>(() => frames.AddAnimation("default"));
        Reject<ArgumentException>(() => frames.RenameAnimation("default", "default"));
        Reject<ArgumentException>(() => frames.GetFrameCount("missing"));
        Reject<ArgumentNullException>(() => frames.HasAnimation(null!));
        frames.DuplicateAnimation("default", "independent"); frames.RemoveFrame("independent", 0);
        Check(frames.GetFrameCount("default") == 3 && ReferenceEquals(frames.GetFrameTexture("independent", 0), texture), "Independent frame containers borrow textures.");
        using var shallow = (SpriteFrames)frames.Duplicate();
        using var deep = (SpriteFrames)frames.Duplicate(true);
        using var deepTexture = deep.GetFrameTexture("default", 1)!;
        Check(ReferenceEquals(shallow.GetFrameTexture("default", 1), texture) && !ReferenceEquals(deepTexture, texture) &&
            ReferenceEquals(deepTexture, deep.GetFrameTexture("default", 2)) && ReferenceEquals(deepTexture, deep.GetFrameTexture("independent", 0)), "Resource copies preserve aliases according to depth.");
        shallow.Clear("default"); Check(frames.GetFrameCount("default") == 3, "Shallow copies own frame lists.");
        using var target = new SpriteFrames(); target.CopyFromResource(frames); frames.Clear("default");
        Check(target.GetFrameCount("default") == 3 && ReferenceEquals(target.GetFrameTexture("default", 1), texture), "CopyFromResource copies containers.");
        var before = changes; frames.Clear("default"); Check(changes == before + 1, "Clearing an empty animation still notifies.");
        before = changes; frames.ClearAll(); Check(changes == before && frames.GetAnimationNames().SequenceEqual(new[] { "default" }), "ClearAll silently restores defaults.");
        texture.EmitChanged(); Check(changes == before, "Contained texture events are not forwarded.");
        Action<Resource> fail = _ => throw new ApplicationException(); frames.Changed += fail;
        Reject<ApplicationException>(() => frames.AddFrame("default", texture)); frames.Changed -= fail;
        Check(frames.GetFrameCount("default") == 1, "Resource callback failure retains edit.");
        frames.Dispose(); Check(!texture.IsDisposed, "Library disposal borrows textures."); Reject<ObjectDisposedException>(() => frames.ClearAll());
    }

    private static SpriteFrames Library(int count, double FPS = 1)
    {
        var frames = new SpriteFrames(); frames.SetAnimationSpeed("default", FPS);
        for (var i = 0; i < count; i++) frames.AddFrame("default", null);
        return frames;
    }

    private static void Playback()
    {
        using var frames = Library(3);
        var sprite = new AnimatedSprite { SpriteFrames = frames };
        using var tree = new SceneTree(sprite);
        var events = new List<string>();
        sprite.FrameChanged += () => events.Add("frame" + sprite.Frame);
        sprite.AnimationLooped += () => events.Add("loop" + sprite.Frame);
        sprite.AnimationFinished += () => events.Add("finished");
        sprite.Play(); tree.PhysicsFrame(1); State(sprite, 0, 0, true);
        tree.ProcessFrame(1); State(sprite, 0, 1, true); Check(events.Count == 0, "An exact boundary transitions on the next positive tick.");
        tree.ProcessFrame(.25); State(sprite, 1, .25f, true); Check(events.SequenceEqual(new[] { "frame1" }), "Forward transition.");
        sprite.Pause(); tree.ProcessFrame(1); State(sprite, 1, .25f, false); Check(sprite.GetPlayingSpeed() == 0, "Pause retains progress.");
        sprite.Play(customSpeed: 2); tree.ProcessFrame(.125); State(sprite, 1, .5f, true);
        sprite.SpeedScale = 0; tree.ProcessFrame(1); State(sprite, 1, .5f, true);
        sprite.SpeedScale = 1; sprite.Stop(); State(sprite, 0, 0, false);
        sprite.PlayBackwards(); State(sprite, 2, 1, true); Check(sprite.GetPlayingSpeed() == -1, "Reverse start.");
        tree.ProcessFrame(.25); State(sprite, 2, .75f, true);
        sprite.Frame = 1; State(sprite, 1, 1, true);
        sprite.SetFrameAndProgress(0, 0); events.Clear(); tree.ProcessFrame(.25);
        State(sprite, 2, .75f, true); Check(events.SequenceEqual(new[] { "loop2", "frame2" }), "Reverse wrap ordering.");
        sprite.Stop(); sprite.Play(); events.Clear(); tree.ProcessFrame(100);
        State(sprite, 0, 1, true); Check(events.SequenceEqual(new[] { "frame1", "frame2", "loop0", "frame0" }), "Large delta has bounded work and discards the remainder.");
        frames.SetAnimationLoopMode("default", SpriteFrames.LoopMode.None);
        sprite.SetFrameAndProgress(2, 1); events.Clear(); tree.ProcessFrame(.25);
        State(sprite, 2, 1, false); Check(events.SequenceEqual(new[] { "finished" }), "Nonlooping endpoint pauses before finish without FrameChanged.");
        sprite.Play(); State(sprite, 0, 0, true);
        sprite.PlayBackwards(); sprite.SetFrameAndProgress(0, 0); tree.ProcessFrame(.25); State(sprite, 0, 0, false);
        frames.SetAnimationLoopMode("default", SpriteFrames.LoopMode.Linear);
        sprite.Play(); tree.Paused = true; tree.ProcessFrame(.25); State(sprite, 0, 0, true);
        sprite.ProcessMode = NodeProcessMode.Always; tree.ProcessFrame(.25); State(sprite, 0, .25f, true);
        sprite.ProcessMode = NodeProcessMode.Disabled; tree.ProcessFrame(.25); State(sprite, 0, .25f, true);
        sprite.ProcessMode = NodeProcessMode.Inherit; tree.Paused = false;
        frames.SetAnimationSpeed("default", 0); tree.ProcessFrame(1); State(sprite, 0, .25f, true);
        frames.SetAnimationSpeed("default", 2); frames.SetFrame("default", 0, null, 2);
        sprite.Stop(); sprite.Play(); tree.ProcessFrame(1); State(sprite, 0, 1, true);
        tree.ProcessFrame(.25); State(sprite, 1, .25f, true);
        tree.ProcessFrame(.125); State(sprite, 1, .5f, true);
        // The transition iteration consumes at the previous frame rate; later iterations use the new rate.
        frames.RemoveFrame("default", 2); frames.SetAnimationSpeed("default", 1); frames.SetFrame("default", 0, null);
        frames.SetAnimationLoopMode("default", SpriteFrames.LoopMode.PingPong);
        sprite.Stop(); sprite.Play(); tree.ProcessFrame(1); tree.ProcessFrame(1); events.Clear();
        tree.ProcessFrame(.25); State(sprite, 1, .25f, true);
        Check(sprite.GetPlayingSpeed() == -1 && events.SequenceEqual(new[] { "loop1", "frame1" }), "Ping-pong retains the endpoint and reverses custom speed.");
        tree.ProcessFrame(.5); State(sprite, 0, .75f, true);
        sprite.SetFrameAndProgress(0, 0); tree.ProcessFrame(.25); State(sprite, 0, .75f, true); Check(sprite.GetPlayingSpeed() == 1, "Reverse ping-pong endpoint.");
    }

    private static void Selection()
    {
        using var frames = Library(2); frames.AddAnimation("other"); frames.AddFrame("other", null); frames.AddFrame("other", null);
        using var sprite = new AnimatedSprite();
        Check(sprite.Animation == "default" && sprite.Autoplay == "" && sprite.SpriteFrames is null && sprite.Centered &&
            sprite.Offset == Vector2.Zero && !sprite.FlipH && !sprite.FlipV && sprite.SpeedScale == 1, "Node defaults.");
        sprite.Frame = 99; State(sprite, 0, 0, false);
        Reject<InvalidOperationException>(() => sprite.Animation = "other"); Check(sprite.Animation == "", "No-library selection clears the committed name.");
        sprite.SpriteFrames = frames; Check(sprite.Animation == "default", "Fallback uses insertion order.");
        var events = new List<string>(); sprite.FrameChanged += () => events.Add("frame"); sprite.AnimationChanged += () => events.Add("animation");
        sprite.Play("other", -1, true); Check(events.SequenceEqual(new[] { "frame", "animation" }), "Play selection order.");
        events.Clear(); sprite.Animation = "default"; Check(events.SequenceEqual(new[] { "animation" }), "Selection resets using current direction without equal-index FrameChanged.");
        State(sprite, 1, 1, true); sprite.Frame = 999; Check(events.Last() == "frame", "Requested index compared before clamping.");
        sprite.SetFrameAndProgress(-3, 2); State(sprite, 0, 2, true);
        sprite.FrameProgress = -.5f; State(sprite, 0, -.5f, true);
        frames.AddAnimation("empty"); sprite.Play("empty"); Check(sprite.Animation == "default" && sprite.IsPlaying(), "Playing an empty animation is a no-op.");
        sprite.Animation = "empty"; State(sprite, 0, 0, false);
        Reject<ArgumentException>(() => sprite.Animation = "missing"); Check(sprite.Animation == "missing", "Invalid selection is retained after stopping.");
        sprite.SetFrameAndProgress(12, .5f); State(sprite, 12, .5f, false);
        sprite.SpriteFrames = null; State(sprite, 12, .5f, false);
        using var ordered = new SpriteFrames(); ordered.RemoveAnimation("default"); ordered.AddAnimation("z"); ordered.AddAnimation("a");
        sprite.Autoplay = "invalid"; sprite.SpriteFrames = ordered; Check(sprite.Animation == "z" && sprite.Autoplay == "", "First insertion rather than sorted name; invalid autoplay clears.");
        ordered.RenameAnimation("z", "last"); sprite.SpriteFrames = null; sprite.SpriteFrames = ordered; Check(sprite.Animation == "a", "Renamed animation moves to end.");
        ordered.RemoveAnimation("a"); ordered.RemoveAnimation("last"); sprite.SpriteFrames = null; sprite.SpriteFrames = ordered; Check(sprite.Animation == "", "Empty library selection.");
        var root = new Node(); var automatic = new AnimatedSprite { SpriteFrames = frames, Autoplay = "default" }; root.AddChild(automatic);
        using var tree = new SceneTree(root); Check(automatic.IsPlaying(), "Ready autoplay.");
        tree.ProcessFrame(.25); root.RemoveChild(automatic); tree.ProcessFrame(1); State(automatic, 0, .25f, true);
        automatic.Pause(); root.AddChild(automatic); Check(!automatic.IsPlaying(), "Ready happens once.");
        root.RemoveChild(automatic); automatic.RequestReady(); root.AddChild(automatic); Check(automatic.IsPlaying(), "RequestReady permits autoplay again.");
    }

    private static void Packing()
    {
        using var frames = Library(2); using var texture = new ImageTexture();
        frames.SetFrame("default", 0, texture); frames.SetFrame("default", 1, texture);
        using var sprite = new AnimatedSprite
        {
            SpriteFrames = frames,
            Frame = 1,
            FrameProgress = .75f,
            SpeedScale = -2,
            Centered = false,
            FlipH = true,
            FlipV = true,
            Offset = new(2, 3),
            Position = new(4, 5),
            Autoplay = "default"
        };
        using var scene = new PackedScene(); scene.Pack(sprite); using var copy = (AnimatedSprite)scene.Instantiate();
        Check(copy.GetType() == typeof(AnimatedSprite) && typeof(AnimatedSprite).BaseType == typeof(Entity) &&
            ReferenceEquals(copy.SpriteFrames, frames) && copy.Frame == 1 && copy.FrameProgress == .75f && copy.SpeedScale == -2 &&
            !copy.Centered && copy.FlipH && copy.FlipV && copy.Offset == sprite.Offset && copy.Position == sprite.Position && !copy.IsPlaying(), "Stored state and exact node factory.");
        sprite.Play(); Check(!sprite.GetPropertyList().Single(p => p.Name == "Frame").IsStored, "Playing frame is transient.");
        scene.Pack(sprite); using var runningCopy = (AnimatedSprite)scene.Instantiate(); Check(runningCopy.Frame == 0 && !runningCopy.IsPlaying(), "Packing does not store running frame or playback flag.");
        sprite.Pause(); frames.ResourceLocalToScene = true; scene.Pack(sprite);
        using var local = (AnimatedSprite)scene.Instantiate(); var localFrames = local.SpriteFrames!; var localTexture = localFrames.GetFrameTexture("default", 0)!;
        Check(!ReferenceEquals(localFrames, frames) && !ReferenceEquals(localTexture, texture) && ReferenceEquals(localTexture, localFrames.GetFrameTexture("default", 1)), "Scene-local graph retains texture aliases.");
        local.Dispose(); Check(localFrames.IsDisposed && localTexture.IsDisposed && !frames.IsDisposed && !texture.IsDisposed, "Scene root owns only local copies.");
    }

    private static void DrawingAndThreads()
    {
        using var frames = Library(2); using var texture = new RegionTexture(); frames.SetFrame("default", 0, texture);
        var sprite = new CountedSprite { SpriteFrames = frames, Centered = false, FlipH = true, FlipV = true };
        var window = new TestViewport { SnapTransformsToPixel = true }; window.AddChild(sprite); using var tree = new SceneTree(window);
        sprite.Offset = new(.6f, -.6f); sprite.PrepareCanvas(); sprite.PrepareCanvas();
        Check(sprite.Draws == 1 && texture.Destination == new Rect(1, -1, -2, -2) && texture.Source == new Rect(0, 0, 2, 2) && texture.Clip, "Virtual frame draw, flips and local pixel snap.");
        sprite.Frame = 1; sprite.PrepareCanvas(); var drawCount = sprite.Draws;
        var owner = Environment.CurrentManagedThreadId; sprite.PropertyListChanged += _ => Check(Environment.CurrentManagedThreadId == owner, "Scene notifications stay on owner thread.");
        Task.Run(() => frames.RemoveFrame("default", 1)).GetAwaiter().GetResult();
        Check(sprite.Frame == 0, "Worker resource shrink is reconciled before owner read."); sprite.PrepareCanvas(); Check(sprite.Draws == drawCount + 1, "Worker edit invalidates retained commands.");
        Reject<InvalidOperationException>(() => Task.Run(() => sprite.Frame = 0).GetAwaiter().GetResult());
        texture.EmitChanged(); sprite.PrepareCanvas(); Check(sprite.Draws == drawCount + 1, "Contained texture geometry changes require explicit redraw.");
        var rectChanges = 0; sprite.ItemRectChanged += _ => rectChanges++;
        sprite.Centered = true; sprite.Offset = Vector2.Zero; sprite.FlipH = false; sprite.Frame = 0;
        Check(rectChanges == 2, "Only centered/offset changes report rectangle change.");
        sprite.Play(); tree.ProcessFrame(.01); for (var i = 0; i < 50; i++) sprite.Notify(Node.NotificationInternalProcess);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) sprite.Notify(Node.NotificationInternalProcess);
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Stable internal playback notification has no allocation.");
        sprite.Dispose(); frames.EmitChanged(); Check(!frames.IsDisposed && !texture.IsDisposed, "Node disposal disconnects borrowed resources.");
    }

    private static void Failures()
    {
        using var frames = Library(2); var sprite = new AnimatedSprite { SpriteFrames = frames }; var root = new Node(); root.AddChild(sprite); using var tree = new SceneTree(root);
        Reject<ArgumentOutOfRangeException>(() => sprite.SpeedScale = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => sprite.FrameProgress = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => sprite.Play(customSpeed: float.NaN));
        Reject<ArgumentException>(() => sprite.Play("absent"));
        Reject<ArgumentException>(() => sprite.Offset = new(float.NaN, 0));
        Action fail = () => throw new ApplicationException("frame callback");
        sprite.SetFrameAndProgress(1, 1); sprite.FrameChanged += fail;
        Reject<ApplicationException>(() => sprite.Play()); sprite.FrameChanged -= fail;
        tree.ProcessFrame(.25); State(sprite, 0, .25f, true);
        sprite.Frame = 1; sprite.FrameChanged += fail; Reject<ApplicationException>(sprite.Stop); sprite.FrameChanged -= fail;
        tree.ProcessFrame(.25); State(sprite, 0, 0, false);
        var reenter = true;
        Action<ElectronObject> resume = _ => { if (reenter) { reenter = false; sprite.Play(); } };
        sprite.PropertyListChanged += resume; sprite.Pause(); sprite.PropertyListChanged -= resume;
        tree.ProcessFrame(.25); State(sprite, 0, .25f, true);
        frames.SetFrame("default", 0, null, .01f); frames.SetAnimationSpeed("default", double.MaxValue);
        Reject<AggregateException>(() => tree.ProcessFrame(.25));
        frames.SetAnimationSpeed("default", 1); frames.SetFrame("default", 0, null);
        sprite.FrameChanged += () => sprite.Dispose(); sprite.SetFrameAndProgress(0, 1); tree.ProcessFrame(.25);
        Check(sprite.IsDisposed && !frames.IsDisposed, "Disposal in frame callback exits processing safely.");
    }

    private sealed class TestViewport : Viewport
    {
        public override Rect GetVisibleRect() => new(0, 0, 100, 100);
    }
    private sealed class CountedSprite : AnimatedSprite
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; base.OnDraw(); }
    }
    private sealed class RegionTexture : Texture
    {
        internal Rect Destination, Source; internal bool Clip;
        public override int Width => 2;
        public override int Height => 2;
        public override void DrawRectRegion(CanvasItem canvasItem, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
        { Destination = rect; Source = sourceRect; Clip = clipUV; canvasItem.DrawRect(rect, Colors.Red); }
    }
    private static void State(AnimatedSprite sprite, int frame, float progress, bool playing) =>
        Check(sprite.Frame == frame && Math.Abs(sprite.FrameProgress - progress) < .00001f && sprite.IsPlaying() == playing,
            $"Expected {frame}/{progress}/{playing}, got {sprite.Frame}/{sprite.FrameProgress}/{sprite.IsPlaying()}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
