using System.Diagnostics;
using System.Globalization;
using Electron2D;
using Electron2D.Examples.PhysicsSandbox;
using Path = System.IO.Path;
using Body = Electron2D.GPUPhysicsBodyStore.BodyHandle;
using State = Electron2D.GPUPhysicsBodyStore.Snapshot;
using SDL = SDL3.SDL;

/// <summary>Developer preview of Smash on the independent device world; no scene-body CPU mirror.</summary>
internal static class GPUPhysicsSmashPreview
{
    internal static void Run()
    {
        var smoke = Environment.GetEnvironmentVariable("ELECTRON2D_GPU_SMASH_SMOKE") == "1";
        var count = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_GPU_SMASH_COUNT") ?? (smoke ? "512" : "9600"), CultureInfo.InvariantCulture);
        if (count is < 64 or > 65536) throw new ArgumentOutOfRangeException(nameof(count), "Use 64–65,536 fragments.");
        var priorRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var priorFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var priorFPS = Engine.MaxFPS;
        var priorBudget = Engine.MaxPhysicsStepsPerFrame;
        using var regular = new FontFile();
        using var bold = new FontFile();
        regular.LoadDynamicFont(Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
        bold.LoadDynamicFont(Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-SemiBold.ttf"));
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            Engine.MaxFPS = 60;
            Engine.MaxPhysicsStepsPerFrame = 1;
            using var window = new PreviewWindow(regular, bold, count, smoke);
            if (Engine.Run(window) != 0) throw new InvalidOperationException("GPU Smash window failed.");
            if (smoke && !window.SmokePassed) throw new InvalidOperationException("GPU Smash smoke did not finish.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, priorRenderer);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, priorFallback);
            Engine.MaxFPS = priorFPS;
            Engine.MaxPhysicsStepsPerFrame = priorBudget;
        }
    }

    private sealed class PreviewWindow : Window
    {
        private const float Scale = PhysicsScene.SmashScale;
        private static readonly Rect2 Field = SandboxWindow.Playfield;
        private readonly Font _font, _bold;
        private readonly List<Resource> _resources = [];
        private readonly Entity _visual, _hud;
        private readonly MultiMesh _mesh;
        private readonly RectangleShape _piece, _block;
        private readonly HSlider[] _sliders = new HSlider[8];
        private readonly Button _play;
        private readonly Texture _thumb, _thumbHover;
        private readonly bool _smoke;
        private GPUPhysicsBodyStore? _world;
        private Body[] _bodies = [];
        private State[] _states = [];
        private Vector2[] _homes = [];
        private float[] _instances = [];
        private bool _paused = true, _step;
        private int _selected = -1, _grab = -1, _ticks, _frames, _moved, _maxMoved, _maxContacts;
        private int _pauseTick;
        private Vector2 _pointer, _dragStart;
        private float _kickBefore;
        private float _pieceSize, _blockMass, _fragmentMass;
        private double _physicsMS, _readMS, _readoutTime;
        private long _readBytes;
        private string _device = "Preparing GPU…";
        internal bool SmokePassed { get; private set; }

        internal PreviewWindow(Font regular, Font bold, int count, bool smoke)
        {
            _font = regular; _bold = bold; _smoke = smoke;
            _thumb = Thumb(PhysicsScene.Ink); _thumbHover = Thumb(PhysicsScene.Pink);
            Name = "GPUPhysicsSmash"; Title = "PhysicsSandbox — Smash · GPU preview";
            Size = MinSize = MaxSize = SandboxWindow.ClientSize; Unresizable = true;
            ProcessEnabled = PhysicsProcessEnabled = InputEnabled = UnhandledInputEnabled = true;
            var clip = new Control { Name = "Playfield", Position = Field.Position, Size = Field.Size, ClipContents = true, MouseFilter = MouseFilter.Ignore };
            _visual = new Entity { Name = "Fragments" }; clip.AddChild(_visual); AddChild(clip);
            var quad = Own(new ArrayMesh());
            quad.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
            {
                Vertices = [new(-.5f, -.5f), new(.5f, -.5f), new(.5f, .5f), new(-.5f, .5f)],
                Indices = [0, 1, 2, 0, 2, 3]
            });
            _mesh = Own(new MultiMesh { Mesh = quad, UseColors = true });
            _piece = Own(new RectangleShape()); _block = Own(new RectangleShape { Size = new(64 * Scale, 64 * Scale) });
            _visual.Draw += DrawWorld;
            _hud = new Entity { Name = "Readouts" }; _hud.Draw += DrawHUD; AddChild(_hud);
            _play = Button("Play", new(338, 64), 106, () => Pause(!_paused));
            Button("Step", new(456, 64), 90, () => { Pause(true); _step = true; });
            Button("Reset", new(558, 64), 94, () => Rebuild(false));
            Button("Launch block", new(24, 712), 266, () => Rebuild(true), primary: true);
            Button("Shockwave", new(306, 712), 266, Shockwave);
            Button("Rebuild wall", new(588, 712), 272, () => Rebuild(false));
            Parameter(0, "Gravity", 0, 1000, 10, 0, 208);
            Parameter(1, "Time scale", .25, 2, .05, 1, 260);
            Parameter(2, "Launch speed", 100, 1600, 10, 600, 338);
            Parameter(3, "Block mass", 1, 50, .5, 12, 390);
            Parameter(4, "Fragments", 64, 65536, 64, count, 468);
            Parameter(5, "Fragment mass", .001, .02, .0005, .0045, 520);
            Parameter(6, "Friction", 0, 1, .05, .05, 572);
            Parameter(7, "Bounce", 0, 1, .05, .1, 624);
            FocusExited += () => _grab = -1;
            Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(PhysicsScene.Paper);
                Rebuild(false);
                _device = _world!.DeviceName;
                Console.WriteLine($"GPU Smash: independent device world, {_bodies.Length - 1} fragments, {_world.Driver}, {_device}");
                if (_smoke) RenderingServer.FramePostDraw += SmokeFrame;
            };
        }

        private T Own<T>(T resource) where T : Resource { _resources.Add(resource); return resource; }
        private float Value(int index) => (float)_sliders[index].Value;
        private float Mass(int index) => index == 0 ? _blockMass : _fragmentMass;

        private void Rebuild(bool launch)
        {
            _grab = _selected = -1; _step = false; _ticks = _moved = 0;
            _world?.Dispose(); _world = null;
            foreach (var shape in _boundaries) shape.Dispose();
            _boundaries.Clear();
            _blockMass = Value(3); _fragmentMass = Value(5);
            var world = new GPUPhysicsBodyStore();
            try
            {
                var count = (int)Value(4);
                _bodies = new Body[count + 1]; _states = new State[count + 1]; _homes = new Vector2[count + 1];
                _instances = new float[(count + 1) * 12]; _mesh.InstanceCount = count + 1;
                var columns = (int)MathF.Ceiling(MathF.Sqrt(count * 1.5f));
                var rows = (count + columns - 1) / columns;
                _pieceSize = MathF.Min(2.4f, 360f / columns * .85f) * Scale;
                _piece.Size = new(_pieceSize, _pieceSize);
                for (var i = 0; i <= count; i++)
                {
                    var position = i == 0 ? new Vector2(160, 268) * Scale :
                        new Vector2(500 * Scale + ((i - 1) % columns - (columns - 1) * .5f) * _pieceSize / .85f,
                            268 * Scale + ((i - 1) / columns - (rows - 1) * .5f) * _pieceSize / .85f);
                    _homes[i] = position;
                    _bodies[i] = world.Add(new(PhysicsServer.BodyMode.Rigid, position, 0,
                        i == 0 && launch ? new(Value(2) * Scale, 0) : Vector2.Zero, 0, Mass(i), LinearDamp: .02f, AngularDamp: i == 0 ? 0 : .05f));
                    world.AddShape(_bodies[i], i == 0 ? _block : _piece, friction: Value(6), bounce: Value(7));
                }
                // Thin invisible boundaries keep fragments in the clipped display area.
                using var horizontal = new RectangleShape { Size = new((Field.Size.X + 40) * Scale, 20 * Scale) };
                using var vertical = new RectangleShape { Size = new(20 * Scale, (Field.Size.Y + 40) * Scale) };
                // The store borrows shape resources, so retain boundary geometry until disposal.
                AddBoundary(world, new(Field.Size.X / 2, -10), horizontal);
                AddBoundary(world, new(Field.Size.X / 2, Field.Size.Y + 10), horizontal);
                AddBoundary(world, new(-10, Field.Size.Y / 2), vertical);
                AddBoundary(world, new(Field.Size.X + 10, Field.Size.Y / 2), vertical);
                // Flush authoring changes before sleeping: adding shapes wakes their bodies.
                world.Step(0, Vector2.Zero);
                if (Value(0) == 0) for (var i = 1; i < _bodies.Length; i++) world.SetSleeping(_bodies[i], true);
                _world = world;
                Publish(); _physicsMS = _readMS = 0; _readBytes = 0; Pause(!launch);
            }
            catch { world.Dispose(); _world = null; throw; }
        }

        private readonly List<Shape> _boundaries = [];
        private void AddBoundary(GPUPhysicsBodyStore world, Vector2 position, RectangleShape template)
        {
            var shape = new RectangleShape { Size = template.Size }; _boundaries.Add(shape);
            var body = world.Add(new(PhysicsServer.BodyMode.Static, position * Scale, 0, default, 0));
            world.AddShape(body, shape, friction: Value(6), bounce: Value(7));
        }

        private void Pause(bool value) { _paused = value; if (value) _grab = -1; _play.Text = value ? "Play" : "Pause"; _hud.QueueRedraw(); }
        private void Shockwave()
        {
            if (_world is null) return;
            for (var i = 1; i < _bodies.Length; i++)
            {
                var direction = (_states[i].Position - new Vector2(500, 268) * Scale).Normalized();
                _world.ApplyImpulse(_bodies[i], direction * (260 * Scale * Mass(i)));
            }
            Pause(false);
        }

        protected override void OnPhysicsProcess(double delta)
        {
            if (_world is null || _paused && !_step) return;
            _step = false;
            if (_grab >= 0)
            {
                ref readonly var state = ref _states[_grab];
                var correction = (_pointer - state.Position) * 20 - new Vector2(state.Velocity.X, state.Velocity.Y);
                _world.ApplyImpulse(_bodies[_grab], correction.LimitLength(1200 * Scale) * (Mass(_grab) * .2f));
            }
            var start = Stopwatch.GetTimestamp();
            _world.Simulate((float)delta * Value(1), new(0, Value(0) * Scale));
            _physicsMS = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            _maxContacts = Math.Max(_maxContacts, _world.ContactPointCount);
            _ticks++; Publish();
        }

        private void Publish()
        {
            var start = Stopwatch.GetTimestamp(); var bytes = _world!.ReadbackBytes;
            _world.Read(_bodies, _states);
            _readBytes = _world.ReadbackBytes - bytes;
            _readMS = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            _moved = 0;
            for (var i = 0; i < _states.Length; i++)
            {
                ref readonly var state = ref _states[i];
                if (!state.Position.IsFinite() || !float.IsFinite(state.Velocity.X) || !float.IsFinite(state.Velocity.Y) || !float.IsFinite(state.Velocity.Z) || !float.IsFinite(state.Pose.Z) || !float.IsFinite(state.Pose.W))
                    throw new InvalidOperationException("GPU Smash produced nonfinite state.");
                var size = i == 0 ? 64 : _pieceSize / Scale / .85f + .5f;
                var color = PhysicsScene.SmashSleepingColor;
                var motionSize = i == 0 ? 64 * Scale : _pieceSize;
                var speed = new Vector2(state.Velocity.X, state.Velocity.Y).Length() + MathF.Abs(state.Velocity.Z) * motionSize * .7071068f;
                if (speed > .01f) color = speed / 60 > motionSize * .25f ? PhysicsScene.Apricot : PhysicsScene.Pink;
                if (i == 0 && speed <= .01f) color = PhysicsScene.Pink;
                var data = _instances.AsSpan(i * 12, 12);
                data[0] = state.Pose.Z * size; data[1] = -state.Pose.W * size; data[2] = 0; data[3] = state.Pose.X / Scale;
                data[4] = state.Pose.W * size; data[5] = state.Pose.Z * size; data[6] = 0; data[7] = state.Pose.Y / Scale;
                data[8] = color.R; data[9] = color.G; data[10] = color.B; data[11] = color.A;
                if (i > 0 && state.Position.DistanceSquaredTo(_homes[i]) > Scale * Scale) _moved++;
            }
            _maxMoved = Math.Max(_maxMoved, _moved);
            _mesh.Buffer = _instances; _mesh.CustomAABB = new(0, 0, Field.Size.X, Field.Size.Y);
            _visual.QueueRedraw();
        }

        protected override void OnProcess(double delta)
        {
            _readoutTime += delta;
            if (_readoutTime >= .15) { _readoutTime = 0; _hud.QueueRedraw(); }
        }
        protected override void OnInput(InputEvent input)
        {
            if (input is InputEventMouseMotion motion) _pointer = (motion.Position - Field.Position) * Scale;
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) _grab = -1;
        }
        protected override void OnUnhandledInput(InputEvent input)
        {
            if (input is InputEventMouseButton { Pressed: true } mouse && Field.HasPoint(mouse.Position))
            {
                _pointer = (mouse.Position - Field.Position) * Scale;
                _selected = Pick(_pointer);
                if (mouse.ButtonIndex == MouseButton.Left) _grab = _selected;
                else if (mouse.ButtonIndex == MouseButton.Right && _selected >= 0)
                {
                    _world!.ApplyImpulse(_bodies[_selected], new Vector2(240, -120) * (Scale * Mass(_selected))); Pause(false);
                }
                _visual.QueueRedraw(); _hud.QueueRedraw(); SetInputAsHandled();
                return;
            }
            if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
            switch (key.PhysicalKeycode != 0 ? key.PhysicalKeycode : key.Keycode)
            {
                case Key.Escape: Tree!.Quit(); break;
                case Key.P: Pause(!_paused); break;
                case Key.Period: Pause(true); _step = true; break;
                case Key.B: Rebuild(true); break;
                case Key.F: case Key.R: Rebuild(false); break;
                case Key.N: Shockwave(); break;
                default: return;
            }
            SetInputAsHandled();
        }
        private int Pick(Vector2 point)
        {
            for (var i = 0; i < _states.Length; i++)
            {
                ref readonly var state = ref _states[i]; var delta = point - state.Position;
                var local = new Vector2(delta.X * state.Pose.Z + delta.Y * state.Pose.W, -delta.X * state.Pose.W + delta.Y * state.Pose.Z);
                var half = i == 0 ? 32 * Scale : _pieceSize / 2;
                if (MathF.Abs(local.X) <= half && MathF.Abs(local.Y) <= half) return i;
            }
            return -1;
        }

        private void DrawWorld(CanvasItem canvas)
        {
            canvas.DrawMultiMesh(_mesh);
            if (_selected < 0) return;
            ref readonly var state = ref _states[_selected];
            var size = _selected == 0 ? 64 : _pieceSize / Scale;
            canvas.DrawSetTransform(state.Position / Scale, state.Rotation);
            canvas.DrawRect(new(-size / 2 - 2, -size / 2 - 2, size + 4, size + 4), PhysicsScene.Pink, false, 1.5f);
            canvas.DrawSetTransformMatrix(Transform.Identity);
        }
        private void DrawHUD(CanvasItem canvas)
        {
            Span<char> text = stackalloc char[128];
            int length;
            canvas.DrawString(_bold, new(24, 36), "Electron2D", fontSize: 21, modulate: PhysicsScene.Ink);
            canvas.DrawString(_font, new(174, 35), "PhysicsSandbox", fontSize: 18, modulate: PhysicsScene.Muted);
            canvas.DrawLine(new(24, 48), new(1128, 48), PhysicsScene.Border);
            canvas.DrawString(_bold, new(24, 92), "Smash", fontSize: 24, modulate: PhysicsScene.Ink);
            canvas.DrawString(_font, new(116, 91), "GPU · preview", fontSize: 16, modulate: PhysicsScene.Pink);
            canvas.DrawString(_font, new(24, 140), "Launch a heavy block into a solid wall. Every fragment is a real GPU rigid body.", fontSize: 15, modulate: PhysicsScene.Muted);
            canvas.DrawRect(Field, PhysicsScene.Border, false);
            canvas.DrawRect(new(880, 156, 248, 536), PhysicsScene.Surface);
            canvas.DrawString(_bold, new(896, 188), "WORLD · live", fontSize: 14, modulate: PhysicsScene.Blush);
            canvas.DrawString(_bold, new(896, 318), "IMPACT · apply on launch", fontSize: 14, modulate: PhysicsScene.Pink);
            canvas.DrawString(_bold, new(896, 448), "WALL · apply on rebuild", fontSize: 14, modulate: PhysicsScene.Apricot);
            text.TryWrite(CultureInfo.InvariantCulture, $"{Engine.FramesPerSecond:0} FPS", out length);
            Readout(canvas, new(894, 87), text[..length], 24, PhysicsScene.Ink);
            text.TryWrite(CultureInfo.InvariantCulture, $"Step {_physicsMS:0.00} ms", out length);
            Readout(canvas, new(894, 109), text[..length], 13, PhysicsScene.Muted);
            text.TryWrite(CultureInfo.InvariantCulture, $"Read {_readMS:0.00} ms · {_readBytes / 1024.0:0} KiB", out length);
            Readout(canvas, new(894, 132), text[..length], 13, PhysicsScene.Muted);
            text.TryWrite(CultureInfo.InvariantCulture, $"{_bodies.Length - 1} fragments · {_moved} moved · {_ticks} ticks", out length);
            Readout(canvas, new(24, 678), text[..length], 13, PhysicsScene.Muted);
            canvas.DrawString(_font, new(894, 735), _paused ? "PAUSED" : "RUNNING", fontSize: 15, modulate: PhysicsScene.Blush);
            canvas.DrawString(_font, new(24, 779), "B launch · N shockwave · F rebuild · P pause · . step · drag to grab · right click to kick", fontSize: 13, modulate: PhysicsScene.Muted);
            if (_selected >= 0)
            {
                text.TryWrite(CultureInfo.InvariantCulture, $"{(_selected == 0 ? "Block" : "Fragment")} #{_selected}", out length);
                Readout(canvas, new(894, 760), text[..length], 13, PhysicsScene.Ink);
            }
            else canvas.DrawString(_font, new(894, 760), "Click a body to select", fontSize: 13, modulate: PhysicsScene.Muted);
        }
        private void Readout(CanvasItem canvas, Vector2 position, ReadOnlySpan<char> text, int size, Color color) =>
            PhysicsScene.DrawReadout(canvas, _font, position, text, size, color);

        private StyleBoxFlat Style(Color color, bool accent = false)
        {
            var style = Own(new StyleBoxFlat { BGColor = color, BorderColor = accent ? PhysicsScene.Pink : PhysicsScene.Border });
            style.SetCornerRadiusAll(8); style.SetBorderWidthAll(accent ? 2 : 1); return style;
        }
        private Button Button(string text, Vector2 position, float width, Action action, bool primary = false)
        {
            var button = new Button(text) { Name = "Button" + GetChildCount(), Position = position, Size = new(width, 44) };
            button.AddThemeFontOverride("font", _font); button.AddThemeFontSizeOverride("font_size", 15);
            button.AddThemeColorOverride("font_color", PhysicsScene.Ink); button.AddThemeColorOverride("font_hover_color", PhysicsScene.Ink);
            button.AddThemeColorOverride("font_pressed_color", PhysicsScene.Ink); button.AddThemeColorOverride("font_focus_color", PhysicsScene.Ink);
            button.AddThemeStyleBoxOverride("normal", Style(primary ? PhysicsScene.Berry : PhysicsScene.Surface, primary));
            button.AddThemeStyleBoxOverride("hover", Style(PhysicsScene.Hover)); button.AddThemeStyleBoxOverride("pressed", Style(PhysicsScene.Pressed, true));
            button.AddThemeStyleBoxOverride("focus", Style(Colors.Transparent, true));
            button.Pressed += () => { action(); button.ReleaseFocus(); };
            AddChild(button); return button;
        }
        private Texture Thumb(Color color)
        {
            using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
            for (var y = 0; y < 16; y++) for (var x = 0; x < 16; x++)
                    image.SetPixel(x, y, new(color.R, color.G, color.B, Math.Clamp(7.5f - new Vector2(x - 7.5f, y - 7.5f).Length(), 0, 1)));
            return Own(ImageTexture.CreateFromImage(image));
        }
        private void Parameter(int index, string name, double min, double max, double step, double value, float y)
        {
            var label = new Label(name) { Name = "Label" + index, Position = new(896, y), MouseFilter = MouseFilter.Ignore };
            label.AddThemeFontOverride("font", _font); label.AddThemeFontSizeOverride("font_size", 13); label.AddThemeColorOverride("font_color", PhysicsScene.Ink);
            AddChild(label);
            var slider = _sliders[index] = new HSlider { Name = "Parameter" + index, Position = new(896, y + 19), Size = new(216, 28), MinValue = min, MaxValue = max, Step = step, Value = value };
            var track = Style(PhysicsScene.Border); track.SetBorderWidthAll(0); track.SetCornerRadiusAll(2); track.ContentMarginTop = track.ContentMarginBottom = 2;
            slider.AddThemeStyleBoxOverride("slider", track);
            var fill = Style(index < 2 ? PhysicsScene.Blush : index < 4 ? PhysicsScene.Pink : PhysicsScene.Apricot);
            fill.SetBorderWidthAll(0); fill.SetCornerRadiusAll(2); fill.ContentMarginTop = fill.ContentMarginBottom = 2;
            slider.AddThemeStyleBoxOverride("grabber_area", fill); slider.AddThemeStyleBoxOverride("grabber_area_highlight", fill);
            slider.AddThemeIconOverride("grabber", _thumb); slider.AddThemeIconOverride("grabber_highlight", _thumbHover);
            slider.AddThemeConstantOverride("center_grabber", 0); slider.AddThemeConstantOverride("grabber_offset", 0);
            slider.ValueChanged += _ => _hud.QueueRedraw();
            slider.DragEnded += _ => slider.ReleaseFocus(); AddChild(slider);
            var readout = new Entity { Name = "ParameterValue" + index };
            readout.Draw += c =>
            {
                Span<char> buffer = stackalloc char[40];
                var format = index is 0 or 2 or 4 ? "0" : index == 5 ? "0.0000" : "0.##";
                slider.Value.TryFormat(buffer, out var length, format, CultureInfo.InvariantCulture);
                var unit = index switch { 0 => " u/s²", 1 => "×", 2 => " u/s", 3 or 5 => " kg", _ => "" };
                unit.AsSpan().CopyTo(buffer[length..]); length += unit.Length;
                PhysicsScene.DrawReadout(c, _font, new(1010, y + 12), buffer[..length], 12, PhysicsScene.Blush);
            };
            slider.ValueChanged += _ => readout.QueueRedraw(); AddChild(readout);
        }

        private void SmokeFrame()
        {
            _frames++;
            if (_frames == 1)
            {
                Capture("-initial"); Check(_selected == -1 && _ticks == 0, "Initial pause/selection");
                for (var i = 1; i < _states.Length; i++) Check(_states[i].Sleeping, "Initial sleeping wall");
                NativeClick(new(150, 734));
            }
            if (_frames == 40) Capture("-impact");
            if (_frames == 50) { Check(_maxContacts > 0 && _maxMoved > 0, "Real GPU impact propagation"); NativeKey(SDL.Scancode.P); }
            if (_frames == 54) _pauseTick = _ticks;
            if (_frames == 58) { Check(_ticks == _pauseTick, "Pause"); NativeKey(SDL.Scancode.Period); }
            if (_frames == 63) { Check(_ticks == _pauseTick + 1, "Single step"); NativeClick(Field.Position + _states[0].Position / Scale); }
            if (_frames == 66) { Check(_selected == 0, "Select block"); NativeClick(Field.Position + new Vector2(12, 12)); }
            if (_frames == 69) { Check(_selected == -1, "Clear selection"); NativeKey(SDL.Scancode.F); }
            if (_frames == 73) { Check(_ticks == 0 && _moved == 0 && _paused, "Rebuild"); NativeKey(SDL.Scancode.N); }
            if (_frames == 96) { Check(_moved > 0, "Shockwave"); _sliders[4].Value = 1024; NativeClick(new(720, 734)); }
            if (_frames == 101) { Check(_bodies.Length == 1025 && _paused, "Changed population"); NativeKey(SDL.Scancode.B); }
            if (_frames == 107)
            {
                _dragStart = _states[0].Position;
                NativeButton(Field.Position + _dragStart / Scale, true);
            }
            if (_frames == 110)
            {
                Check(_grab == 0, "Grab starts on selected body");
                NativeMotion(Field.Position + _dragStart / Scale + new Vector2(-32, -48));
            }
            if (_frames == 117)
            {
                Check(_states[0].Position.Y < _dragStart.Y - Scale, "Dragging applies GPU impulses");
                Capture("-grab"); NativeButton(Field.Position + _pointer / Scale, false);
            }
            if (_frames == 121)
            {
                _kickBefore = _states[0].Velocity.X;
                var point = Field.Position + _states[0].Position / Scale;
                NativeButton(point, true, 3); NativeButton(point, false, 3);
            }
            if (_frames == 124) Check(_states[0].Velocity.X > _kickBefore + 80 * Scale, "Right click kick");
            if (_frames == 150)
            {
                Capture("");
                SmokePassed = true;
                Console.WriteLine($"GPU Smash native smoke passed: impact, pause/step, selection/clear, drag, kick, rebuild, shockwave, population. Peak contacts={_maxContacts}, moved={_maxMoved}; GPU={_world!.Driver}.");
                Tree!.Quit();
            }
        }
        private static void Capture(string suffix)
        {
            var path = Environment.GetEnvironmentVariable("ELECTRON2D_GPU_SMASH_CAPTURE");
            if (path is null) return;
            path = Path.GetFullPath(path);
            var target = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + suffix + ".png");
            using var image = RenderingServer.Service!.Readback(); image.SavePNG(target);
        }
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("GPU Smash: " + message); }
        private static uint NativeWindow() => SDL.GetWindowID(SDL.GetWindows(out _)![0]);
        private static void NativeClick(Vector2 point) { NativeMotion(point); NativeButton(point, true); NativeButton(point, false); }
        private static void NativeMotion(Vector2 point)
        {
            var e = new SDL.Event { Motion = new() { Type = SDL.EventType.MouseMotion, WindowID = NativeWindow(), X = point.X, Y = point.Y } };
            Check(SDL.PushEvent(ref e), "Mouse motion");
        }
        private static void NativeButton(Vector2 point, bool down, byte button = 1)
        {
            var e = new SDL.Event
            {
                Button = new()
                {
                    Type = down ? SDL.EventType.MouseButtonDown : SDL.EventType.MouseButtonUp,
                    WindowID = NativeWindow(),
                    Button = button,
                    Down = down,
                    X = point.X,
                    Y = point.Y
                }
            };
            Check(SDL.PushEvent(ref e), "Mouse button");
        }
        private static void NativeKey(SDL.Scancode code)
        {
            var e = new SDL.Event { Key = new() { Type = SDL.EventType.KeyDown, WindowID = NativeWindow(), Down = true, Scancode = code, Key = SDL.GetKeyFromScancode(code, SDL.Keymod.None, true) } };
            Check(SDL.PushEvent(ref e), "Key down"); e.Key.Type = SDL.EventType.KeyUp; e.Key.Down = false; Check(SDL.PushEvent(ref e), "Key up");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { _world?.Dispose(); _world = null; }
            base.Dispose(disposing);
            if (disposing) { foreach (var shape in _boundaries) shape.Dispose(); foreach (var resource in _resources) resource.Dispose(); }
        }
    }
}
