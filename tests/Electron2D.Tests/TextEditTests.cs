using Electron2D;
using Path = System.IO.Path;
internal static class TextEditTests
{
    private sealed class Root : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 480, 320); }
    internal static void Run()
    {
        Buffer(); Highlighting(); Routing(); Persistence(); Console.WriteLine("TextEdit document, multicaret, history, Unicode, highlighting, routing and scene storage passed.");
    }
    private static void Buffer()
    {
        using var t = new TextEdit { Size = new(220, 100) }; Check(t.Text == "" && t.GetLineCount() == 1 && t.GetCaretCount() == 1, "defaults"); t.Text = "A😀e\u0301\r\nnext\n"; Check(t.GetLine(0) == "A😀e\u0301" && t.GetLineCount() == 3, "normalized logical lines");
        t.SetCaretColumn(2); t.InsertTextAtCaret("中\nx", 0); Check(t.Text == "A😀中\nxe\u0301\nnext\n" && t.GetCaretLine() == 1 && t.GetCaretColumn() == 1, "scalar multiline insertion"); t.Undo(); Check(t.Text == "A😀e\u0301\nnext\n" && t.GetCaretColumn() == 2, "undo caret snapshot"); t.Redo(); Check(t.GetCaretLine() == 1 && t.GetCaretColumn() == 1, "redo snapshot");
        t.Text = "ab\ncd"; t.SetCaretColumn(1); t.AddCaret(1, 1); t.InsertTextAtCaret("X"); Check(t.Text == "aXb\ncXd" && t.GetCaretCount() == 2, "multicaret reverse insert"); t.Undo(); Check(t.Text == "ab\ncd" && t.GetCaretCount() == 2, "one undo for all carets");
        t.Select(0, 1, 1, 1); Check(t.GetSelectedText(0) == "b\nc", "multiline selection"); t.RemoveSecondaryCarets(); t.InsertTextAtCaret("Q"); Check(t.Text == "aQd", "selection replacement"); t.Undo(); Check(t.GetSelectedText(0) == "b\nc", "undo selection");
        t.Text = "abc"; t.BeginComplexOperation(); t.InsertText("X", 0, 0); t.InsertText("Y", 0, 4); t.EndComplexOperation(); Check(t.Text == "XabcY", "nested group"); t.Undo(); Check(t.Text == "abc" && !t.HasUndo() && t.HasRedo(), "group undo"); t.InsertText("Z", 0, 3); Check(!t.HasRedo(), "redo invalidated"); t.TagSavedVersion(); var saved = t.GetSavedVersion(); t.Undo(); t.InsertText("different", 0, 0); Check(t.GetVersion() != saved, "branched history version does not alias saved state"); t.ClearUndoHistory(); Check(t.GetSavedVersion() == 0, "clear resets saved tag");
        t.Text = "one someone ONE\nlast one"; Check(t.Search("one", TextEdit.SearchFlags.WholeWords, 0, 4) == new Vector2i(12, 0), "whole word case search"); Check(t.Search("one", TextEdit.SearchFlags.MatchCase, 1, 8) == new Vector2i(0, 0), "wrapped search");
        t.Text = "e\u0301😀"; Check(t.GetNextCompositeCharacterColumn(0, 0) == 2 && t.GetPreviousCompositeCharacterColumn(0, 2) == 0, "graphemes"); t.SetCaretColumn(2); t.BackspaceDeletesCompositeCharacterEnabled = true; t.Backspace(); Check(t.Text == "😀", "composite backspace");
        t.Text = "\t  hello"; Check(t.GetFirstNonWhitespaceColumn(0) == 3 && t.GetIndentLevel(0) == 6, "indentation");
        t.Text = "one\ntwo"; t.AddGutter(); t.SetGutterWidth(0, 24); t.SetLineGutterText(1, 0, "2"); t.SetGutterOverwritable(0, true); t.MergeGutters(1, 0); t.SetLineGutterText(1, 0, "changed"); Check(t.GetLineGutterText(0, 0) == "2", "independent merged cells"); t.SetLineGutterMetadata(0, 0, 42); Check(t.GetLineGutterMetadata<int>(0, 0) == 42, "typed metadata"); t.ClearUndoHistory(); t.InsertLineAt(0, "new"); Check(t.GetLineGutterMetadata<int>(1, 0) == 42, "gutter follows inserted line"); t.Undo(); Check(t.GetLineGutterMetadata<int>(0, 0) == 42, "undo restores gutter payload"); Reject<KeyNotFoundException>(() => t.GetLineGutterMetadata<string>(0, 0));
        t.WrapMode = TextEdit.LineWrappingMode.Boundary; t.Text = "very long line with several words that wrap"; Check(t.GetLineWrapCount(0) > 0 && t.GetTotalVisibleLineCount() > 1, "measured wrapping");
        Reject<ArgumentOutOfRangeException>(() => t.GetLine(-1)); Reject<ArgumentOutOfRangeException>(() => t.SetTabSize(0)); Reject<ArgumentOutOfRangeException>(() => t.CaretBlinkInterval = double.NaN); Reject<ArgumentOutOfRangeException>(() => t.GetSelectedText(-2)); Reject<InvalidOperationException>(() => t.GetVScrollBar().Dispose());
        t.Editable = false; var before = t.Text; t.Backspace(); t.MenuOption(TextMenuAction.Clear); Check(t.Text == before, "readonly commands"); t.Editable = true; t.TextChanged += Throw; Reject<AggregateException>(() => t.InsertTextAtCaret("X")); t.TextChanged -= Throw; Check(t.Text.Contains('X') && t.HasUndo(), "observer failure preserves history"); t.Undo(); Check(t.Text == before, "failed observer undo");
    }
    private static void Throw() => throw new InvalidOperationException("observer");
    private static void Highlighting()
    {
        using var h = new CodeHighlighter { NumberColor = Colors.Red, SymbolColor = Colors.Blue, FunctionColor = Colors.Yellow, MemberVariableColor = Colors.Green }; h.AddKeywordColor("var", Colors.Cyan); h.AddColorRegion("/*", "*/", Colors.Magenta); h.AddColorRegion("\"", "\"", Colors.Green, true);
        using var t = new TextEdit { Text = "var x=42; /* begin\nend */ call(1);\n\"quoted\" name.member", SyntaxHighlighter = h }; Check(h.GetTextEdit() == t, "binding"); var a = h.GetLineSyntaxHighlighting(0); Check(a[0] == Colors.Cyan && a.Values.Contains(Colors.Red) && a.Values.Contains(Colors.Magenta), "keyword number region"); Check(h.GetLineSyntaxHighlighting(1)[0] == Colors.Magenta, "multiline continuation"); Check(h.GetLineSyntaxHighlighting(2).Values.Contains(Colors.Green), "quoted member");
        t.SetLine(0, "plain"); Check(h.GetLineSyntaxHighlighting(1)[0] != Colors.Magenta, "cache invalidation"); t.AddThemeColorOverride("font_color", Colors.Orange); Check(h.GetLineSyntaxHighlighting(0)[0] == Colors.Orange, "theme invalidates normal syntax color"); t.Editable = false; Check(h.GetLineSyntaxHighlighting(0)[0] == t.GetThemeColor("font_readonly_color"), "readonly syntax color"); t.Editable = true; var copy = (CodeHighlighter)h.Duplicate(); using (copy) { copy.ClearKeywordColors(); Check(h.HasKeywordColor("var") && !copy.HasKeywordColor("var"), "duplicate containers"); }
        using var other = new TextEdit(); Reject<InvalidOperationException>(() => other.SyntaxHighlighter = h); t.SyntaxHighlighter = null; other.SyntaxHighlighter = h; Check(h.GetTextEdit() == other, "rebind"); other.SyntaxHighlighter = null; h.Dispose(); Check(t.SyntaxHighlighter == null, "borrowed lifetime");
    }
    private static void Routing()
    {
        var root = new Root(); var t = new TextEdit { Size = new(220, 100), DrawSpaces = true }; t.GetLineHeight(); root.AddChild(t); using var tree = new SceneTree(root); tree.FlushDeferred(); t.GrabFocus(); tree.DispatchCommittedText(root, "A😀\nBC"); Check(t.Text == "A😀\nBC", "native committed text route"); KeyEvent(root, Key.Left, true); Check(t.GetSelectedText() == "C", "shift selection"); KeyEvent(root, Key.Backspace); Check(t.Text == "A😀\nB", "selection delete"); KeyEvent(root, Key.Enter); Check(t.Text == "A😀\nB\n", "newline action");
        tree.DispatchIMEComposition(root, "中", new(0, 1)); Check(t.HasIMEText() && t.GetLineWithIME(2) == "中" && t.GetLine(2) == "", "IME preedit"); tree.DispatchCommittedText(root, "中"); Check(!t.HasIMEText() && t.Text == "A😀\nB\n中", "IME commit"); t.Text = "ab\ncd"; t.SetCaretColumn(1); t.AddCaret(1, 1); tree.DispatchIMEComposition(root, "中", new(0, 1)); Check(t.GetLineWithIME(0) == "a中b" && t.GetLineWithIME(1) == "c中d", "multicaret preedit"); t.ApplyIME(); Check(t.Text == "a中b\nc中d", "multicaret composition apply"); t.Undo(); Check(t.Text == "ab\ncd", "composition group undo"); t.RemoveSecondaryCarets(); t.Text = "A😀\nB\n中"; t.Editable = false; tree.DispatchCommittedText(root, "ignored"); Check(t.GetLine(2) == "中", "readonly input");
    }
    private static void Persistence()
    {
        using var h = new CodeHighlighter(); h.AddKeywordColor("let", Colors.Red); using var t = new TextEdit { Text = "let one\ntwo", WrapMode = TextEdit.LineWrappingMode.Boundary, SyntaxHighlighter = h }; using var packed = new PackedScene(); packed.Pack(t); using var copy = (TextEdit)packed.Instantiate(); Check(copy.Text == t.Text && copy.WrapMode == t.WrapMode && copy.SyntaxHighlighter is CodeHighlighter c && c.HasKeywordColor("let"), "scene highlighter copied independently");
        var path = Path.Combine(Path.GetTempPath(), "electron2d-text-" + Guid.NewGuid() + ".e2dscene"); try { ResourceSaver.Save(packed, path); var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TextEditTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_TEXT_EDIT"); start.Environment.Remove("ELECTRON2D_TEST_TEXT_EDIT_HOST"); start.Environment["ELECTRON2D_TEST_TEXT_EDIT_CHILD"] = path; using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("fresh text scene"); } Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh multiline scene passed"), "fresh process: " + error.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path) { using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var t = (TextEdit)scene.Instantiate(); Check(t.Text == "let one\ntwo" && t.SyntaxHighlighter is CodeHighlighter h && h.GetKeywordColor("let") == Colors.Red && h.GetLineSyntaxHighlighting(0)[0] == Colors.Red, "fresh stored highlighter"); t.InsertTextAtCaret("new"); Check(t.HasUndo(), "fresh editing"); Console.WriteLine("Fresh multiline scene passed"); }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_TEXT_RENDERER") ?? "gpu"; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try { Pixels(); NativeInput(); Warm(); } finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
        Console.WriteLine("TextEdit native pixels and prepared rendering passed (" + backend + ").");
    }
    private static void Pixels()
    {
        using var h = new CodeHighlighter { NumberColor = Colors.Green, SymbolColor = Colors.White, FunctionColor = Colors.Cyan }; h.AddKeywordColor("var", Colors.Red); var window = new Window { Size = new(360, 240) }; var t = new TextEdit { Name = "editor", Position = new(8, 8), Size = new(320, 220), Text = "var count = 42;\ncall(count)\nAאב e\u0301 😀\nwrapped words wrapped words wrapped words wrapped words\nline five\nline six\nline seven\nline eight\nline nine\nline ten", SyntaxHighlighter = h, WrapMode = TextEdit.LineWrappingMode.Boundary, MinimapDraw = true, HighlightCurrentLine = true, DrawSpaces = true }; t.AddGutter(); t.SetGutterWidth(0, 28); for (var i = 0; i < t.GetLineCount(); i++) t.SetLineGutterText(i, 0, (i + 1).ToString()); window.AddChild(t); var phase = 0;
        window.Ready += _ =>
        {
            t.GrabFocus(); RenderingServer.FramePostDraw += () =>
        {
            using var image = RenderingServer.Service!.Readback(); var red = 0; var green = 0; var ink = 0; for (var y = 8; y < 228; y++) for (var x = 8; x < 320; x++) { var c = image.GetPixel(x, y); if (c.R > .6 && c.G < .3) red++; if (c.G > .6 && c.R < .3) green++; if (c.R > .7 && c.G > .7) ink++; }
            if (phase == 0) { Check(red > 0 && green > 0 && ink > 0, "syntax colors and gutter pixels"); if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXT_CAPTURE") is { } path) image.SavePNG(path); t.Select(0, 0, 1, 4); }
            else if (phase == 1) { Check(t.HasSelection(), "rendered selection"); t.SetCaretLine(9); t.SetCaretColumn(8); Check(t.ScrollVertical > 0, "scrollbar integration"); }
            else if (phase == 2) { t.Text = ""; t.PlaceholderText = "Multiline text editor"; }
            else { Check(ink > 0, "placeholder pixels"); window.Tree!.Quit(); }
            phase++;
        };
        }; Check(Engine.Run(window) == 0 && phase == 4, "render lifecycle");
    }
    private sealed class NativeDriver(TextEdit field) : Node
    {
        private readonly List<nint> _pointers = []; private uint _id; private string _clipboard = ""; internal int Frame;
        protected override void OnReady() { ProcessEnabled = true; _clipboard = DisplayServer.ClipboardGet(); field.GrabFocus(); _id = SDL3.SDL.GetWindowID(SDL3.SDL.GetWindows(out var count)![0]); PushText("native😀\nsecond"); }
        private void PushText(string text) { var ptr = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8(text); _pointers.Add(ptr); var input = new SDL3.SDL.Event { Text = new SDL3.SDL.TextInputEvent { Type = SDL3.SDL.EventType.TextInput, WindowID = _id, Text = ptr } }; Check(SDL3.SDL.PushEvent(ref input), "SDL committed producer"); }
        protected override void OnProcess(double delta)
        {
            if (Frame++ == 0) { Check(field.Text == "native😀\nsecond", "SDL commit route"); var handle = SDL3.SDL.GetWindows(out var count)![0]; Check(SDL3.SDL.TextInputActive(handle), "native IME active"); var ptr = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8("中"); _pointers.Add(ptr); var input = new SDL3.SDL.Event { Edit = new SDL3.SDL.TextEditingEvent { Type = SDL3.SDL.EventType.TextEditing, WindowID = _id, Text = ptr, Start = 0, Length = 1 } }; Check(SDL3.SDL.PushEvent(ref input), "SDL preedit producer"); }
            else if (Frame == 2) { Check(field.HasIMEText() && field.GetLineWithIME(1) == "second中", "SDL preedit route"); PushText("中"); }
            else if (Frame == 3) { Check(field.Text == "native😀\nsecond中" && !field.HasIMEText(), "SDL composition commit"); field.SelectAll(); field.Copy(); Check(DisplayServer.ClipboardGet() == field.Text, "native clipboard copy"); field.Clear(); field.Paste(); Check(field.Text == "native😀\nsecond中", "native clipboard paste"); field.RemoveSecondaryCarets(); field.Deselect(); field.Copy(); field.SetCaretColumn(1); field.Paste(); Check(field.GetLineCount() == 3 && field.GetLine(1) == "second中", "linewise paste"); field.Text = "ab\ncd"; field.SetCaretColumn(1); field.AddCaret(1, 1); DisplayServer.ClipboardSet("X\nY"); field.Paste(); Check(field.Text == "aXb\ncYd", "one clipboard line per caret"); }
            else { field.Editable = false; var text = field.Text; field.Cut(); Check(field.Text == text, "readonly clipboard cut"); Tree!.Quit(); }
        }
        protected override void Dispose(bool disposing) { if (disposing) { foreach (var ptr in _pointers) System.Runtime.InteropServices.Marshal.FreeCoTaskMem(ptr); if (DisplayServer.Service != null) DisplayServer.ClipboardSet(_clipboard); } base.Dispose(disposing); }
    }
    private static void NativeInput() { var window = new Window { Size = new(240, 160) }; var field = new TextEdit { Size = new(220, 140) }; var driver = new NativeDriver(field); window.AddChild(field); window.AddChild(driver); Check(Engine.Run(window) == 0 && driver.Frame == 4, "SDL multiline input lifecycle"); }
    private static void Warm()
    {
        using var h = new CodeHighlighter { NumberColor = Colors.Green }; h.AddKeywordColor("var", Colors.Red); var window = new Window { Size = new(320, 180) }; var t = new TextEdit { Size = new(300, 160), Text = "var count = 42;\nAאב e\u0301\nother text", SyntaxHighlighter = h, MinimapDraw = true }; window.AddChild(t); var frame = 0; long before = 0, total = 0;
        window.Ready += _ => { t.GrabFocus(); RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread(); RenderingServer.FramePostDraw += () => { var now = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) total += now - before; t.Select(0, 0, 0, frame % 2 == 0 ? 3 : 9); if (frame >= 64) total += GC.GetAllocatedBytesForCurrentThread() - now; if (++frame == 128) window.Tree!.Quit(); }; }; Check(Engine.Run(window) == 0 && total == 0, "64 prepared frames allocation: " + total); Console.WriteLine("64 prepared TextEdit selection/render frames: " + total + " managed bytes.");
    }
    private static void KeyEvent(Viewport root, Key key, bool shift = false) { using var input = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true, ShiftPressed = shift }; root.PushInput(input); }
    private static void Check(bool value, string message) { if (!value) throw new Exception("TextEdit: " + message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
}
