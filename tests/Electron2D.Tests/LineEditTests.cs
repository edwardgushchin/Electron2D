using Electron2D;
using SDL = SDL3.SDL;

internal static class LineEditTests
{
    private sealed class Root : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 320, 200); }
    internal static void Run()
    {
        using var field = new LineEdit(); var changed = 0; var rejected = "";
        field.TextChanged += _ => changed++; field.TextChangeRejected += text => rejected = text;
        Check(field.FocusMode == FocusMode.All && field.MouseDefaultCursorShape == CursorShape.IBeam && field.Editable && field.SelectingEnabled && field.Text == "" && field.GetSelectionFromColumn() == -1, "Defaults.");
        field.Text = "A😀e\u0301Z"; field.CaretColumn = 2; field.InsertTextAtCaret("中"); Check(field.Text == "A😀中e\u0301Z" && field.CaretColumn == 3 && changed == 0, "Scalar insertion and quiet programmatic assignments.");
        Check(field.GetNextCompositeCharacterColumn(3) == 5 && field.GetPreviousCompositeCharacterColumn(5) == 3, "Combining-mark boundaries.");
        field.Select(1, 3); Check(field.GetSelectedText() == "😀中", "Scalar selection."); field.Select(4, 2); Check(field.GetSelectedText() == "😀中", "Reversed selection preserves previous interval.");
        field.SelectingEnabled = false; Check(!field.HasSelection(), "Disabling selection clears it."); field.SelectingEnabled = true;
        field.MaxLength = 3; Check(field.Text == "A😀中" && rejected == "e\u0301Z" && field.CaretColumn == 0, "Length truncation and reset.");
        field.CaretColumn = 3; field.InsertTextAtCaret("xy"); Check(rejected == "xy" && field.Text == "A😀中", "Rejected insertion.");
        field.MaxLength = 0; field.Text = "e\u0301"; field.CaretColumn = 2; field.BackspaceDeletesCompositeCharacterEnabled = true; field.DeleteCharAtCaret(); Check(field.Text == "" && changed == 1 && field.HasUndo(), "Composite backspace.");
        field.Text = "ab"; field.DeleteText(0, 1); Check(field.Text == "b" && changed == 1, "Detached deletion has no queued signal."); field.Clear(); Check(changed == 2 && !field.HasUndo(), "Clear resets history and signals once."); field.Clear(); Check(changed == 2, "Empty clear is silent.");
        Reject<ArgumentOutOfRangeException>(() => field.MaxLength = -1); Reject<ArgumentOutOfRangeException>(() => field.DeleteText(1, 0)); Reject<ArgumentOutOfRangeException>(() => field.CaretBlinkInterval = double.NaN);
        field.Secret = true; field.Text = "e\u0301"; Check(field.GetNextCompositeCharacterColumn(0) == 1, "Secret navigation uses displayed mask clusters."); field.Text = ""; field.PlaceholderText = "hint"; Check(field.GetNextCompositeCharacterColumn(0) == 0, "Placeholder does not extend source columns."); field.Secret = false;
        field.SecretCharacter = "😀xx"; Check(field.SecretCharacter == "😀", "First scalar mask.");
        field.StructuredTextBIDIOverrideOptions = ["/"]; var options = field.StructuredTextBIDIOverrideOptions; options[0] = "bad"; Check(field.StructuredTextBIDIOverrideOptions[0] == "/", "Defensive options.");
        field.Text = "stored"; field.PlaceholderText = "hint"; field.Secret = true; field.CaretColumn = 2;
        using var packed = new PackedScene(); packed.Pack(field); using var copy = (LineEdit)packed.Instantiate(); Check(copy.Text == "stored" && copy.PlaceholderText == "hint" && copy.Secret && copy.CaretColumn == 2, "Typed scene storage.");
        Commands(); Routing(); Geometry(); OversampledClip(); Drag(); Console.WriteLine("LineEdit scalar/grapheme editing, rejection, selection, history, IME, actions, storage and shaped caret passed.");
    }
    private static void Commands()
    {
        using var field = new LineEdit(); var changed = 0; field.TextChanged += _ => changed++;
        field.MenuOption(LineEditMenuAction.InsertLRM); Check(field.Text == "\u200e" && changed == 1, "Unicode command inserts and notifies.");
        field.MenuOption(LineEditMenuAction.Undo); Check(field.Text == "" && field.HasRedo(), "Programmatic undo command."); field.MenuOption(LineEditMenuAction.Redo); Check(field.Text == "\u200e", "Programmatic redo command.");
        field.MenuOption(LineEditMenuAction.DirectionRTL); field.MenuOption(LineEditMenuAction.DisplayUCC); Check(field.TextDirection == TextDirection.RTL && field.DrawControlChars, "Direction/display commands execute.");
        field.Editable = false; field.MenuOption(LineEditMenuAction.Clear); Check(field.Text == "\u200e", "Read-only menu mutation suppressed.");
        Reject<NotSupportedException>(() => field.MenuOption(LineEditMenuAction.EmojiAndSymbols));
    }
    private static void Routing()
    {
        var root = new Root(); var field = new LineEdit { Name = "field", Size = new(120, 32) }; root.AddChild(field); using var tree = new SceneTree(root); tree.FlushDeferred();
        var changes = 0; field.TextChanged += _ => changes++; field.GrabFocus(); Check(field.IsEditing(), "Focus enters editing.");
        tree.DispatchCommittedText(root, "A😀\nB"); Check(field.Text == "A😀B" && changes == 1, "Committed strings edit one line.");
        KeyEvent(root, Key.Left, shift: true); Check(field.GetSelectedText() == "B", "Shift extends selection."); KeyEvent(root, Key.Backspace); Check(field.Text == "A😀" && changes == 2, "Selection backspace.");
        KeyEvent(root, Key.Z, command: true); Check(field.Text == "A😀B" && field.HasRedo(), "Undo action."); KeyEvent(root, Key.Z, command: true, shift: true); Check(field.Text == "A😀", "Redo action.");
        field.CaretColumn = 2; tree.DispatchIMEComposition(root, "中", new(0, 1)); Check(field.HasIMEText() && field.Text == "A😀", "Composition remains uncommitted.");
        tree.DispatchCommittedText(root, "中"); Check(field.Text == "A😀中" && !field.HasIMEText(), "Commit replaces preedit without duplication.");
        var before = changes; field.DeleteText(0, 1); field.DeleteText(0, 1); Check(changes == before, "Attached deletion defers."); tree.FlushDeferred(); Check(changes == before + 1 && field.Text == "中", "Deletion notifications coalesce.");
        field.Editable = false; tree.DispatchCommittedText(root, "x"); Check(field.Text == "中" && !field.IsEditing(), "Read-only ignores native commit."); field.Editable = true;
        field.SelectAll(); field.SelectAllOnFocus = true; var submits = 0; field.TextSubmitted += _ => submits++; KeyEvent(root, Key.Enter); Check(submits == 1 && !field.IsEditing() && !field.HasSelection(), "Submit ends edit and deselects.");
        field.Edit(); field.KeepEditingOnTextSubmit = true; KeyEvent(root, Key.KeypadEnter); Check(submits == 2 && field.IsEditing(), "Keypad submit and keep-editing policy."); field.KeepEditingOnTextSubmit = false; field.Text = "abc"; field.CaretColumn = 3; KeyEvent(root, Key.Home); Check(field.CaretColumn == 0, "Home action."); KeyEvent(root, Key.End); Check(field.CaretColumn == 3, "End action.");
        field.TextChanged += ThrowChanged; Reject<AggregateException>(() => tree.DispatchCommittedText(root, "x")); Check(field.Text == "abcx" && field.HasUndo(), "Throwing observer leaves committed history."); field.TextChanged -= ThrowChanged;
        field.Text = "ab"; field.SelectAll(); field.TextChanged += ThrowChanged; Reject<AggregateException>(() => tree.DispatchIMEComposition(root, "中", new(0, 1))); Check(field.Text == "" && field.HasIMEText(), "Composition commits before deletion observer failure."); field.TextChanged -= ThrowChanged; field.CancelIME(); field.Text = "abcx";
        field.MaxLength = 4; field.TextChangeRejected += ThrowChanged; Reject<AggregateException>(() => tree.DispatchCommittedText(root, "y")); Check(field.Text == "abcx", "Throwing rejection leaves consistent state.");
    }
    private static void ThrowChanged(string text) => throw new InvalidOperationException("observer");
    private static void Drag()
    {
        var root = new Root(); var source = new LineEdit { Name = "source", Size = new(120, 32), Text = "abcd" }; var target = new LineEdit { Name = "target", Position = new(140, 0), Size = new(120, 32) }; root.AddChild(source); root.AddChild(target); using var tree = new SceneTree(root); tree.FlushDeferred();
        source.Select(0, 2); Mouse(root, new(7, 12), true); Motion(root, new(160, 12)); Mouse(root, new(160, 12), false);
        Check(source.Text == "cd" && target.Text == "ab" && source.IsDragSuccessful(), $"Drag moves selected text between fields: {source.Text}/{target.Text}/{source.IsDragSuccessful()}/{root.IsGUIDragging()}/{source.HasSelection()}.");
        source.Text = "abcd"; source.Select(0, 2); Mouse(root, new(7, 12), true); Motion(root, new(90, 12)); Mouse(root, new(90, 12), false);
        Check(source.Text == "cdab" && source.GetSelectedText() == "ab", "Self drag moves selection once.");
        source.Secret = true; source.SelectAll(); Mouse(root, new(7, 12), true); Motion(root, new(160, 12)); Mouse(root, new(160, 12), false); Check(target.Text == "ab", "Secret text does not produce drag payload.");
        source.Secret = false; source.DragAndDropSelectionEnabled = false; Check(source.CanDropData(new(10, 10), new DragPayload<string>("x")), "Selection-drag switch does not suppress incoming string drops.");
    }
    private static void Mouse(Viewport root, Vector2 point, bool pressed) { using var input = new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = pressed }; root.PushInput(input, true); }
    private static void Motion(Viewport root, Vector2 point) { using var input = new InputEventMouseMotion { Position = point, GlobalPosition = point, ButtonMask = MouseButtonMask.Left, Relative = point - new Vector2(7, 12) }; root.PushInput(input, true); }
    private sealed class GlyphProbe(Texture texture) : Control
    {
        protected override void OnDraw() => TextLayout.DrawGlyph(this, new(texture, Vector2.Zero, new(2, 2)), Vector2.Zero, Colors.White, clipRect: new Rect2(1, 0, 1, 2));
    }
    private static void OversampledClip()
    {
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image); using var painter = new GlyphProbe(texture);
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); painter.PrepareCanvas(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 6 && vertices.Min(v => v.UV.X) == .5f && vertices.Max(v => v.UV.X) == 1 && vertices.Max(v => v.UV.Y) == 1, "Logical glyph clipping scales UVs to physical oversampled pixels.");
    }
    private static void Geometry()
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans }; var layout = new TextLayout();
        layout.Build(font, new("office Aאב B", 16, 0, HorizontalAlignment.Left, 1, 0, 0, TextDirection.LTR, TextOrientation.Horizontal), new(Overrun: 0, ApplyAlignment: false));
        var words = new TextLayout(); words.Build(font, new("abc def", 16, 0, HorizontalAlignment.Left, 1, 0, 0, TextDirection.LTR, TextOrientation.Horizontal), new(Overrun: 0));
        Check(words.PreviousWord(4) == 0 && words.NextWord(3) == 7, "Word movement skips separator boundaries.");
        var ranges = new List<Vector2>(); layout.SelectionRanges(7, 9, ranges); Check(ranges.Count > 0 && ranges.All(r => r.Y >= r.X), "BiDi selection rectangles.");
        for (var i = 0; i <= 11; i++) { var caret = layout.Carets(i); Check(caret.Leading.HasValue || caret.Trailing.HasValue, "Every scalar boundary has a shaped caret."); }
        layout.Build(font, new("e\u0301😀", 16, 0, HorizontalAlignment.Left, 1, 0, 0, TextDirection.LTR, TextOrientation.Horizontal), new(Overrun: 0));
        Check(layout.NextGrapheme(0) == 2 && layout.PreviousGrapheme(3) == 2, "Scalar versus grapheme indices.");
        layout.Build(font, new("A\u200eB", 16, 0, HorizontalAlignment.Left, 1, 0, 0, TextDirection.LTR, TextOrientation.Horizontal), new(Overrun: 0, PreserveControl: true));
        var controlWidth = layout.Size.X; layout.Build(font, layout.Key, new(Overrun: 0, PreserveControl: false)); Check(controlWidth > layout.Size.X, "Preserved nonprinting glyph has a visible advance.");
        layout.Build(font, new("e\u0301😀", 16, 0, HorizontalAlignment.Left, 1, 0, 0, TextDirection.LTR, TextOrientation.Horizontal), new(Overrun: 0));
        var left = layout.CaretX(0, TextDirection.LTR); var right = layout.CaretX(2, TextDirection.LTR); Check(layout.HitColumn((left + right) / 2, false) != 1, "Pointer hit avoids combining interior.");
    }
    private static void KeyEvent(Viewport root, Key key, bool command = false, bool shift = false) { using var input = new InputEventKey { Keycode = key, Pressed = true, CommandOrControlAutoremap = command, ShiftPressed = shift }; root.PushInput(input, true); }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_LINE_RENDERER") ?? "gpu"; var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try { Pixels(); NativeInput(); Warm(); } finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
        Console.WriteLine("LineEdit native render and committed/preedit input passed (" + backend + ").");
    }
    private static void Pixels()
    {
        var window = new Window { Size = new(200, 120) }; var field = new LineEdit { Name = "field", Position = new(8, 8), Size = new(120, 32), Text = "Aאב e\u0301", CaretForceDisplayed = true }; window.AddChild(field);
        field.AddThemeColorOverride("selection_color", Colors.Red); field.Select(0, 1); var phase = 0;
        window.Ready += _ => RenderingServer.FramePostDraw += () =>
        {
            using var image = RenderingServer.Service!.Readback(); var red = 0; var ink = 0;
            for (var y = 12; y < 36; y++) for (var x = 12; x < 124; x++) { var pixel = image.GetPixel(x, y); if (pixel.R > .8 && pixel.G < .15) red++; if (pixel.R > .7 && pixel.G > .7) ink++; }
            if (phase == 0) { if (Environment.GetEnvironmentVariable("ELECTRON2D_LINE_CAPTURE") is { } capture) image.SavePNG(capture); Check(red > 10 && ink > 0, "Rendered selection and glyphs."); field.Select(3, 5); field.CaretColumn = 5; field.ClearButtonEnabled = true; }
            else if (phase == 1) { Check(ink > 0, "BiDi field and clear icon render."); field.Secret = true; field.Text = "abcdefghijklmnopqrstuvwxyz"; field.CaretColumn = 26; }
            else if (phase == 2) { Check(field.GetScrollOffset() < 0, "Long masked line scrolls caret into view."); Check(image.GetPixel(135, 20).IsEqualApprox(image.GetPixel(180, 20)), "Text clips at content edge."); field.Clear(); field.PlaceholderText = "hint"; }
            else { Check(ink > 0, "Placeholder renders."); window.Tree!.Quit(); }
            phase++;
        };
        Check(Engine.Run(window) == 0 && phase == 4, "Native render lifecycle.");
    }
    private static void Warm()
    {
        var window = new Window { Size = new(160, 80) }; var field = new LineEdit { Name = "field", Size = new(140, 32), Text = "office Aאב", ClearButtonEnabled = true }; window.AddChild(field); var frame = 0; long before = 0, total = 0;
        window.Ready += _ =>
        {
            field.GrabFocus();
            RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePostDraw += () =>
            {
                var now = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 32) total += now - before;
                field.CaretColumn = frame % 2 == 0 ? 3 : 7; field.Select(2, frame % 2 == 0 ? 4 : 8);
                if (frame >= 32) total += GC.GetAllocatedBytesForCurrentThread() - now;
                if (++frame == 96) window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && total == 0, "Warm caret/selection render bytes: " + total); Console.WriteLine("64 warm LineEdit caret/selection render intervals: " + total + " managed bytes.");
    }
    private sealed class Driver(LineEdit field) : Node
    {
        private uint _id; internal int Frames; private readonly List<nint> _pointers = []; private string? _priorClipboard;
        protected override void OnReady()
        {
            ProcessEnabled = true; _priorClipboard = DisplayServer.ClipboardGet(); field.GrabFocus(); _id = SDL.GetWindowID(SDL.GetWindows(out var count)![0]); PushText("native😀");
        }
        private void PushText(string text)
        {
            var pointer = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8(text);
            _pointers.Add(pointer);
            { var input = new SDL.Event { Text = new SDL.TextInputEvent { Type = SDL.EventType.TextInput, WindowID = _id, Text = pointer } }; Check(SDL.PushEvent(ref input), "Queue native commit."); }

        }
        protected override void Dispose(bool disposing) { if (disposing) { foreach (var pointer in _pointers) System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pointer); if (_priorClipboard is not null && DisplayServer.Service is { } display) DisplayServer.ClipboardSet(_priorClipboard); } base.Dispose(disposing); }
        protected override void OnProcess(double delta)
        {
            if (Frames++ == 0)
            {
                Check(field.Text == "native😀", "Native producer committed string.");
                var handle = SDL.GetWindows(out var count)![0]; Check(SDL.GetTextInputArea(handle, out var area, out var cursor) && SDL.TextInputActive(handle), "Active native IME area.");
                var height = field.GetThemeFont("font")!.GetHeight(field.GetThemeFontSize("font_size")); var expected = field.GetViewport()!.GetScreenTransform() * field.GetGlobalTransformWithCanvas() * new Vector2(0, (field.Size.Y + height) / 2);
                Check(area.Y == (int)expected.Y && area.X >= 8, "Embedded IME area uses containing-window coordinates.");
                var pointer = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8("中");
                _pointers.Add(pointer);
                { var input = new SDL.Event { Edit = new SDL.TextEditingEvent { Type = SDL.EventType.TextEditing, WindowID = _id, Text = pointer, Start = 0, Length = 1 } }; Check(SDL.PushEvent(ref input), "Queue preedit."); }

            }
            else if (Frames == 2) { Check(field.HasIMEText() && field.Text == "native😀", "Native preedit."); PushText("中"); }
            else if (Frames == 3) { Check(field.Text == "native😀中" && !field.HasIMEText(), "Native preedit commit."); field.SelectAll(); field.MenuOption(LineEditMenuAction.Copy); Check(DisplayServer.ClipboardGet() == "native😀中", "Native copy command."); field.Clear(); field.MenuOption(LineEditMenuAction.Paste); Check(field.Text == "native😀中", "Native paste command."); }
            else { field.Secret = true; field.SelectAll(); DisplayServer.ClipboardSet("marker"); field.MenuOption(LineEditMenuAction.Copy); field.MenuOption(LineEditMenuAction.Cut); Check(DisplayServer.ClipboardGet() == "marker" && field.Text == "native😀中", "Secret copy/cut suppression."); Tree!.Quit(); }
        }
    }
    private static void NativeInput() { var window = new Window { Size = new(160, 80) }; var field = new LineEdit { Name = "field", Size = new(70, 28) }; var container = new SubViewportContainer { Name = "container", Position = new(8, 8), Size = new(144, 64), Stretch = true, StretchShrink = 2 }; var view = new SubViewport { Name = "view" }; view.AddChild(field); container.AddChild(view); var driver = new Driver(field); window.AddChild(container); window.AddChild(driver); Check(Engine.Run(window) == 0 && driver.Frames == 4, "Native input lifecycle."); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
