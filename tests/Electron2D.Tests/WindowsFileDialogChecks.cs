using Electron2D;
using System.Diagnostics;
using System.Text;
using IOPath = System.IO.Path;

internal static class WindowsFileDialogChecks
{
    private static int _checks;
    private static string _probe = "", _folder = "", _file = "", _other = "";
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private sealed record Result(bool Accepted, IReadOnlyList<string> Paths, int Filter, IReadOnlyDictionary<string, FileDialogOptionValue> Options);
    internal static void Run(string mode, string probe, string output)
    {
        _probe = probe; _folder = IOPath.GetFullPath(output); Directory.CreateDirectory(_folder);
        _file = IOPath.Combine(_folder, "alpha &,; 日本語.txt"); _other = IOPath.Combine(_folder, "beta #.txt");
        File.WriteAllText(_file, "Owned native chooser fixture."); File.WriteAllText(_other, "Second owned item.");
        var hidden = IOPath.Combine(_folder, "hidden.txt"); if (!File.Exists(hidden)) File.WriteAllText(hidden, "Owned hidden item."); File.SetAttributes(hidden, File.GetAttributes(hidden) | FileAttributes.Hidden);
        Directory.CreateDirectory(IOPath.Combine(_folder, "selected 日本語"));
        if (mode == "unavailable")
        {
            using var display = DisplayServer.Open("Electron2D unsupported chooser fixture", new(320, 240), hidden: true);
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFileExtra), "An unrelated driver does not advertise Windows controls.");
            Reject<NotSupportedException>(() => DisplayServer.FileDialogWithOptionsShow("unsupported", _folder, "", "", false, FileDialogMode.OpenFile, [], [], (_, _, _, _) => throw new Exception("Unsupported adapter cannot callback.")));
        }
        else if (mode is "gpu" or "compatibility") Scene(mode); else Raw();
        Check(!DisplayServer.IsAvailable, "All native dialog owners retire.");
        Console.WriteLine($"Windows file chooser: {_checks} checks passed; mode={mode}.");
    }
    private static void Raw()
    {
        using var display = DisplayServer.Open("Electron2D chooser parent", new(420, 260), hidden: true);
        Check(DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFileExtra) && !DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFileMime), "The Windows backend exposes controls and its extension-only policy.");
        Action<bool, IReadOnlyList<string>, int> unused = (_, _, _) => throw new InvalidOperationException("Invalid launches have no callback.");
        foreach (var bad in new[] { "bad\0title" }) Reject<ArgumentException>(() => DisplayServer.FileDialogShow(bad, _folder, "", false, FileDialogMode.OpenFile, [], unused));
        Reject<ArgumentException>(() => DisplayServer.FileDialogShow("", "bad\0folder", "", false, FileDialogMode.OpenFile, [], unused));
        Reject<ArgumentException>(() => DisplayServer.FileDialogShow("", _folder, "bad\0file", false, FileDialogMode.OpenFile, [], unused));
        foreach (var bad in new[] { "not-an-extension", "*.txt,*", "*.txt;bad\0label" }) Reject<ArgumentException>(() => DisplayServer.FileDialogShow("", _folder, "", false, FileDialogMode.OpenFile, [bad], unused));
        Reject<NotSupportedException>(() => DisplayServer.FileDialogShow("", _folder, "", false, FileDialogMode.OpenFile, [";Text;text/plain"], unused));
        Reject<NotSupportedException>(() => DisplayServer.FileDialogShow("", _folder, "", false, FileDialogMode.OpenAny, [], unused));
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.FileDialogShow("", _folder, "", false, (FileDialogMode)99, [], unused));
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.FileDialogShow("", _folder, "", false, FileDialogMode.OpenFile, [], unused, 99999));
        Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.FileDialogShow("", _folder, "", false, FileDialogMode.OpenFile, [], unused)).GetAwaiter().GetResult());
        Reject<NotSupportedException>(() => DisplayServer.FileDialogWithOptionsShow("", _folder, _folder, "", false, FileDialogMode.OpenFile, [], [], (_, _, _, _) => { }));
        Reject<ArgumentNullException>(() => DisplayServer.FileDialogWithOptionsShow("", _folder, "", "", false, FileDialogMode.OpenFile, [], [null!], (_, _, _, _) => { }));
        var captions = new[] { "Low", "High" }; var choice = new FileDialogOption("Quality", captions, -9); captions[0] = "mutated";
        Check(choice.GetValues()[0] == "Low" && choice.DefaultValueIndex == 0, "Option descriptions copy captions and clamp defaults.");
        var result = Native(display, FileDialogMode.OpenFile, [new("Flag & 日本語", [], 9), choice], "accept", _file, "Flag & 日本語", "0", "Quality", "High");
        Check(result.Accepted && result.Paths.SequenceEqual(new[] { _file }) && result.Filter == 0, "Acceptance returns the exact Unicode/metacharacter path and filter.");
        Check(!result.Options["Flag & 日本語"].Checked && result.Options["Quality"].SelectedIndex == 1, "Real native checkbox and choice edits reach typed results.");
        Reject<NotSupportedException>(() => ((IDictionary<string, FileDialogOptionValue>)result.Options).Clear());
        var retained = result.Options;
        result = Native(display, FileDialogMode.OpenFile, [new("Duplicate", [], 0), new("Duplicate", ["First", "Last"], 99)], "accept", _file);
        Check(result.Options.Count == 1 && !result.Options["Duplicate"].IsCheckbox && result.Options["Duplicate"].SelectedIndex == 1, "Duplicate authored names use the final option and clamped choice default.");
        result = Native(display, FileDialogMode.OpenFile, [new("Flag & 日本語", [], 1), choice], "cancel", "-");
        Check(!result.Accepted && result.Paths.Count == 0 && result.Filter == -1 && result.Options.Count == 0, "Cancellation returns empty raw paths and options.");
        Check(!retained["Flag & 日本語"].Checked && retained["Quality"].SelectedIndex == 1, "A completed immutable map survives subsequent requests.");
        result = Native(display, FileDialogMode.OpenFile, [], "accept", "-", filename: IOPath.GetFileName(_file));
        Check(result.Accepted && result.Paths[0] == _file && result.Options.Count == 0, "Initial folder/filename and empty options execute through the common adapter.");
        result = Native(display, FileDialogMode.OpenFile, [], "item", "hidden.txt");
        Check(result.Accepted && result.Paths[0] == IOPath.Combine(_folder, "hidden.txt"), "showHidden exposes an actual hidden item for native list selection.");
        result = Native(display, FileDialogMode.OpenFile, [], "accept", _file, selectedFilter: 1);
        Check(result.Accepted && result.Paths[0] == _file && result.Filter == 1, "The changed native filter maps to the caller's second index.");
        result = Native(display, FileDialogMode.OpenFiles, [], "accept", $"\"{IOPath.GetFileName(_file)}\" \"{IOPath.GetFileName(_other)}\"");
        Check(result.Accepted && result.Paths.Order().SequenceEqual(new[] { _file, _other }.Order()), "Native multi-selection returns each filesystem item once.");
        result = Native(display, FileDialogMode.OpenDirectory, [], "directory", "selected 日本語");
        Check(result.Accepted && result.Paths[0] == IOPath.Combine(_folder, "selected 日本語") && result.Filter == -1, "Folder mode ignores filters and returns its filesystem path.");
        var save = IOPath.Combine(_folder, "new 日本語 " + Guid.NewGuid().ToString("N"));
        result = Native(display, FileDialogMode.SaveFile, [], "accept", "-", filename: IOPath.GetFileName(save));
        Check(result.Accepted && result.Paths[0] == save + ".txt" && !File.Exists(save + ".txt"), "Save selection applies the default extension without writing the file.");
        result = Native(display, FileDialogMode.OpenFile, [], "accept", _file, basic: true, callbackFailure: true);
        Check(result.Accepted && result.Paths[0] == _file, "Basic native chooser shares ownership and completes even when the client callback fails.");
        var calls = 0;
        DisplayServer.FileDialogWithOptionsShow("invalid-directory", IOPath.Combine(_folder, Guid.NewGuid().ToString("N")), "", "", false, FileDialogMode.OpenFile, [], [choice], (ok, paths, filter, options) => { Check(!ok && paths.Count == 0 && filter == -1 && options.Count == 0, "Native setup failure publishes no partial result."); calls++; });
        Check(Pump(() => calls == 1) == 1 && calls == 1, "Native setup errors report one aggregate after their owner callback.");
    }
    private static Result Native(DisplayServer display, FileDialogMode mode, FileDialogOption[] options, string action, string path, string check = "-", string state = "0", string choice = "-", string caption = "-", string filename = "", bool basic = false, bool callbackFailure = false, int selectedFilter = -1)
    {
        var title = "Electron2D chooser fixture " + Guid.NewGuid().ToString("N"); var calls = 0; Result? result = null; var owner = Environment.CurrentManagedThreadId;
        void Completed(bool ok, IReadOnlyList<string> paths, int filter, IReadOnlyDictionary<string, FileDialogOptionValue> selected)
        { Check(Environment.CurrentManagedThreadId == owner, "Callbacks execute on the display owner."); result = new(ok, paths, filter, selected); calls++; if (callbackFailure) throw new FormatException("Owned chooser observer failure."); }
        string[] filters = mode == FileDialogMode.OpenDirectory ? ["ignored folder filter"] : ["*.txt;Text;text/plain", "*.png,*.jpg;Images"];
        if (basic) DisplayServer.FileDialogShow(title, _folder, filename, true, mode, filters, (ok, paths, filter) => Completed(ok, paths, filter, new Dictionary<string, FileDialogOptionValue>()));
        else DisplayServer.FileDialogWithOptionsShow(title, _folder, "", filename, true, mode, filters, options, Completed);
        Check(calls == 0, "Native results wait for pumping."); Reject<InvalidOperationException>(display.Dispose);
        filters[0] = "mutated after launch"; if (options.Length != 0) options[0] = new("Mutated after launch", []);
        using var actor = Start(title, action, path, check, state, choice, caption, selectedFilter);
        var stdout = actor.StandardOutput.ReadToEndAsync(); var stderr = actor.StandardError.ReadToEndAsync();
        try
        {
            Check(Pump(() => calls == 1) == (callbackFailure ? 1 : 0), "Expected native and observer failure count.");
            Check(actor.WaitForExit(3000) && actor.ExitCode == 0, "SDK action completed: " + (actor.HasExited ? stderr.GetAwaiter().GetResult() : "timeout"));
            Console.Write(stdout.GetAwaiter().GetResult()); Console.WriteLine($"{mode}: accepted={result!.Accepted}, filter={result.Filter}, paths={string.Join('|', result.Paths)}"); DisplayServer.ProcessEvents(); Check(calls == 1, "One terminal callback per native request.");
            return result!;
        }
        catch { if (actor.HasExited) { Console.Write(stdout.GetAwaiter().GetResult()); Console.Error.Write(stderr.GetAwaiter().GetResult()); } throw; }
        finally
        {
            if (calls == 0) { using var cancel = Start(title, "cancel", "-"); Pump(() => calls == 1, 3000); }
            if (!actor.HasExited) actor.Kill(true);
        }
    }
    private static Process Start(string title, string action, string path, string check = "-", string state = "0", string choice = "-", string caption = "-", int selectedFilter = -1)
    {
        var start = new ProcessStartInfo(_probe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        foreach (var arg in new[] { "act", Environment.ProcessId.ToString(), title, action, path, check, state, choice, caption, "selected 日本語", selectedFilter.ToString(System.Globalization.CultureInfo.InvariantCulture) }) start.ArgumentList.Add(arg);
        return Process.Start(start)!;
    }
    private static int Pump(Func<bool> complete, int timeout = 25000)
    {
        var errors = 0; var deadline = Environment.TickCount64 + timeout;
        while (!complete() && Environment.TickCount64 < deadline) { try { DisplayServer.ProcessEvents(); } catch (AggregateException) { errors++; } Thread.Sleep(5); }
        Check(complete(), "Native chooser completes before timeout."); return errors;
    }
    private static void Scene(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var title = "Electron2D scene chooser " + Guid.NewGuid().ToString("N");
        var root = new Window { Size = new(420, 260), Title = "Electron2D chooser scene", GUIEmbedSubwindows = true };
        var parent = root;
        if (backend == "compatibility") { root.GUIEmbedSubwindows = false; parent = new Window { Title = "Electron2D chooser native child", Size = new(420, 260), GUIEmbedSubwindows = true }; root.AddChild(parent); }
        var picker = new FileDialog { Access = FileDialogAccess.FileSystem, UseNativeDialog = true, CurrentDir = _folder, FileMode = FileDialogMode.OpenFile, Title = title, Filters = ["*.txt;Text"] };
        parent.AddChild(picker); picker.AddOption("Flag_checked", [], 1); picker.AddOption("Quality", ["Low", "High"], 0);
        var accepted = 0; var canceled = 0; var actors = new List<Process>();
        picker.FileSelected += path =>
        {
            var selected = picker.GetSelectedOptions(); Check(path == _file && !selected["Flag_checked"].Checked && selected["Quality"].SelectedIndex == 1 && !selected.ContainsKey("Changed"), "Scene commits captured results before FileSelected.");
            accepted++; title = "Electron2D scene cancel " + Guid.NewGuid().ToString("N"); picker.Title = title; picker.PopupFileDialog(); actors.Add(Start(title, "cancel", "-"));
        };
        picker.Canceled += () =>
        {
            var selected = picker.GetSelectedOptions(); Check(selected["Changed"].Checked && selected["Quality"].SelectedIndex == 0, "Scene cancellation retains captured defaults."); canceled++;
            picker.RootSubfolder = _folder; picker.PopupFileDialog(); Check(picker.Visible, "Root confinement uses the custom browser."); picker.Hide(); picker.RootSubfolder = "";
            picker.SetOptionName(0, ""); picker.PopupFileDialog(); Check(picker.Visible, "Empty native labels retain custom controls."); picker.Hide(); root.Tree!.Quit();
        };
        root.Ready += _ =>
        {
            picker.PopupFileDialog(); Check(!picker.Visible && picker.GetSelectedOptions()["Flag_checked"].Checked, "Successful native presentation publishes captured defaults.");
            if (parent != root) { Check(parent.GetWindowID() > DisplayServer.MainWindowId, "The chooser selects a real native child parent."); Reject<InvalidOperationException>(parent.Dispose); Reject<InvalidOperationException>(() => root.RemoveChild(parent)); }
            actors.Add(Start(title, "accept", _file, "Flag_checked", "0", "Quality", "High"));
            Reject<InvalidOperationException>(picker.Dispose); Reject<InvalidOperationException>(picker.PopupFileDialog); Reject<InvalidOperationException>(picker.Show); Reject<InvalidOperationException>(() => picker.PopupCentered()); Reject<InvalidOperationException>(() => picker.Visible = true);
            picker.UseNativeDialog = false; Reject<InvalidOperationException>(picker.Show); picker.UseNativeDialog = true; picker.SetOptionName(0, "Changed");
        };
        var deadline = Environment.TickCount64 + 30000;
        var code = Engine.Run((_, _) => { if (Environment.TickCount64 > deadline) root.Tree!.Quit(91); }, root);
        foreach (var actor in actors) { Check(actor.WaitForExit(3000) && actor.ExitCode == 0, "Scene SDK actor completed: " + actor.StandardError.ReadToEnd()); Console.Write(actor.StandardOutput.ReadToEnd()); actor.Dispose(); }
        Check(code == 0 && accepted == 1 && canceled == 1 && picker.IsDisposed, "Rendered scene completes acceptance, cancellation and deterministic native retirement.");
    }
}
