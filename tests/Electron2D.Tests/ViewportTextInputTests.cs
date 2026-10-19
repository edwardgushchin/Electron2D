using Electron2D;

internal static partial class ViewportTextInputTests
{
    private sealed class Root : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 480, 320); }

    internal static void RunManaged()
    {
        Line(); Document(new TextEdit()); Document(new CodeEdit()); FailuresAndHistory(); Routing();
        Console.WriteLine("Focused viewport text replacement, editor signals/history and routing passed.");
    }

    private static void Line()
    {
        var root = new Root(); var line = new LineEdit { Text = "old", Size = new(160, 30) };
        root.AddChild(line); using var tree = new SceneTree(root); line.GrabFocus();
        var changed = 0; var commits = 0; string? rejected = null;
        line.TextChanged += _ => changed++; line.TextInput += _ => commits++; line.TextChangeRejected += text => rejected = text;
        line.CaretColumn = 2; line.SelectAll(); line.MaxLength = 3;
        root.PushTextInput("A😀BC");
        Check(line.Text == "A😀B" && rejected == "C" && line.CaretColumn == 0 && !line.HasSelection(), "Line replacement applies scalar length and resets selection/caret.");
        Check(changed == 0 && commits == 0, "Replacement does not emit native text or user change events.");
        line.MenuOption(TextMenuAction.Undo); Check(line.Text == "", "Line replacement resets old history and retains its empty initial undo state.");
        line.MenuOption(TextMenuAction.Redo); Check(line.Text == "A😀B", "Line replacement can redo the accepted text."); changed = 0;
        line.Editable = false; root.GUIDisableInput = true; tree.Paused = true;
        root.PushTextInput(""); Check(line.Text == "" && changed == 0, "Programmatic replacement ignores read-only, paused and GUI-input policy.");
        root.ReleaseGUIFocus(); root.PushTextInput("ignored"); Check(line.Text == "", "No focus is a no-op.");
        Reject<ArgumentNullException>(() => root.PushTextInput(null!));
        Check(Task.Run(() => { try { root.PushTextInput("worker"); return false; } catch (InvalidOperationException) { return true; } }).Result, "Off-owner replacement fails before mutation.");
    }

    private static void Document(TextEdit editor)
    {
        var root = new Root(); editor.Size = new(200, 90); root.AddChild(editor);
        using var tree = new SceneTree(root); editor.Text = "old\ntext"; editor.ClearUndoHistory(); tree.FlushDeferred(); editor.GrabFocus();
        editor.SetCaretColumn(2); editor.AddCaret(1, 1); editor.SelectAll();
        var changed = 0; var sets = 0; var commits = 0;
        editor.TextChanged += () => changed++; editor.TextSet += () => { sets++; Check(editor.GetCaretCount() == 1 && editor.GetCaretColumn() == 0 && !editor.HasSelection(), "TextSet observes committed caret state."); };
        editor.TextInput += _ => commits++;
        root.PushTextInput("A😀\r\nnew\n");
        Check(editor.Text == "A😀\nnew\n" && sets == 1 && changed == 0 && commits == 0, "Multiline replacement normalizes text and emits only synchronous TextSet.");
        tree.FlushDeferred(); Check(changed == 0, "Replacement does not defer a TextChanged event.");
        editor.Undo(); Check(editor.Text == "old\ntext" && !editor.HasUndo(), "Multiline replacement is one undo operation preserving previous content.");
        editor.Redo(); Check(editor.Text == "A😀\nnew\n", "Replacement redo restores the document."); tree.FlushDeferred(); changed = 0;
        editor.AddCaret(1, 1); editor.SelectAll(); root.PushTextInput(editor.Text);
        Check(sets == 2 && editor.GetCaretCount() == 1 && !editor.HasSelection() && editor.GetCaretLine() == 0 && editor.GetCaretColumn() == 0, "Equal replacement still resets carets and emits TextSet.");
        tree.FlushDeferred(); Check(changed == 0, "Equal replacement is silent for user-change observers.");
        editor.Editable = false; root.GUIDisableInput = true; tree.Paused = true;
        root.PushTextInput(""); Check(editor.Text == "" && sets == 3, "Read-only document remains programmatically replaceable while paused.");
    }

    private static void Routing()
    {
        using (var detached = new SubViewport()) detached.PushTextInput("no focus");
        var disposed = new SubViewport(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.PushTextInput("x"));
        var root = new Root { GUIEmbedSubwindows = true };
        var rootLine = new LineEdit { Text = "root", Size = new(120, 30) };
        var plain = new Button { Text = "button", Size = new(120, 30) };
        var embedded = new Window { Size = new(150, 80), Visible = false, GUIEmbedSubwindows = true };
        var nested = new Window { Size = new(120, 60), Visible = false };
        var childLine = new LineEdit { Text = "child", Size = new(100, 30) };
        var nestedLine = new LineEdit { Text = "nested", Size = new(100, 30) };
        var container = new SubViewportContainer { Size = new(150, 90) };
        var sub = new SubViewport { Size = new(150, 90) }; var subLine = new LineEdit { Text = "sub", Size = new(100, 30) };
        root.AddChild(rootLine); root.AddChild(plain); root.AddChild(embedded); embedded.AddChild(childLine); embedded.AddChild(nested); nested.AddChild(nestedLine);
        root.AddChild(container); container.AddChild(sub); sub.AddChild(subLine);
        using var tree = new SceneTree(root);
        rootLine.GrabFocus(); root.PushTextInput("main"); Check(rootLine.Text == "main", "Root focus owner is replaced.");
        plain.GrabFocus(); root.PushTextInput("ignored"); Check(plain.Text == "button", "A text-bearing button lacks the editor replacement contract.");
        subLine.GrabFocus(); root.PushTextInput("ignored"); Check(subLine.Text == "sub", "Container focus does not invent text forwarding.");
        sub.PushTextInput("direct"); Check(subLine.Text == "direct", "Direct SubViewport replacement uses its own focus.");
        embedded.Show(); embedded.GrabFocus(); childLine.GrabFocus(); root.PushTextInput("embedded");
        Check(childLine.Text == "embedded" && rootLine.Text == "main", "Focused embedded windows take priority over the host focus owner.");
        nested.Show(); nested.GrabFocus(); nestedLine.GrabFocus(); root.PushTextInput("deep");
        Check(nestedLine.Text == "deep" && childLine.Text == "embedded", "Nested embedded focus forwards recursively.");
        nested.Hide(); embedded.Hide(); rootLine.GrabFocus(); root.PushTextInput("back"); Check(rootLine.Text == "back", "Hiding child focus restores host routing.");
        subLine.Visible = false; sub.PushTextInput("ignored"); Check(subLine.Text == "direct", "Hidden controls lose focus before replacement.");
    }

    private static void FailuresAndHistory()
    {
        var root = new Root(); var editor = new TextEdit { Text = "base", Size = new(180, 80) };
        var line = new LineEdit { MaxLength = 2, Size = new(120, 30) };
        root.AddChild(editor); root.AddChild(line); using var tree = new SceneTree(root); tree.FlushDeferred();
        editor.GrabFocus(); editor.ClearUndoHistory(); var changed = 0; editor.TextChanged += () => changed++;
        editor.InsertTextAtCaret("user"); root.PushTextInput("replacement"); tree.FlushDeferred();
        Check(changed == 1, "Replacing text does not erase a previously queued user change.");
        Action fail = () => throw new ApplicationException("expected replacement observer"); editor.TextSet += fail;
        Reject<AggregateException>(() => root.PushTextInput("committed")); editor.TextSet -= fail;
        Check(editor.Text == "committed" && editor.GetCaretColumn() == 0, "A failed observer sees committed replacement.");
        editor.Undo(); Check(editor.Text == "replacement", "Observer failure retains replacement history.");
        editor.ClearUndoHistory(); editor.BeginComplexOperation(); root.PushTextInput("grouped"); editor.InsertText("!", 0, 0); editor.EndComplexOperation();
        editor.Undo(); Check(editor.Text == "replacement" && !editor.HasUndo(), "Replacement preserves an enclosing undo group.");
        editor.Clear(); Check(editor.Text == "" && editor.HasUndo(), "Editable Clear retains an undo step."); editor.Undo();
        editor.Editable = false; editor.Clear(); Check(editor.Text == "" && !editor.HasUndo(), "Read-only Clear retires old history.");
        line.GrabFocus(); Action<string> reject = _ => throw new ApplicationException("expected length rejection"); line.TextChangeRejected += reject;
        Reject<ApplicationException>(() => root.PushTextInput("😀AB")); line.TextChangeRejected -= reject;
        Check(line.Text == "😀A" && line.CaretColumn == 0 && line.HasUndo(), "Length-observer failure still finalizes accepted text, caret and history.");
        var limit = ProjectSettings.Get(ProjectSettings.TextEditUndoStackMaxSize);
        try
        {
            ProjectSettings.Set(ProjectSettings.TextEditUndoStackMaxSize, 0); using var noHistory = new TextEdit { Text = "first" };
            noHistory.Text = "second"; Check(!noHistory.HasUndo(), "Disabled history remains disabled for replacement.");
        }
        finally { ProjectSettings.Set(ProjectSettings.TextEditUndoStackMaxSize, limit); }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
