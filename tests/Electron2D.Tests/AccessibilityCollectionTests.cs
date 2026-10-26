using System.Diagnostics;
using Electron2D;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
using Path = System.IO.Path;

internal static class AccessibilityCollectionTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Native Collection Probe"); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(960, 680) };
        var list = new ItemList { AccessibilityName = "List", Position = new(8, 8), Size = new(280, 260), SelectionMode = ItemList.SelectMode.Multi };
        for (var i = 0; i < 30; i++) list.AddItem("Item " + i); list.Select(1, false); list.Select(2, false); list.SetItemDisabled(3, true); list.SetItemSelectable(4, false); window.AddChild(list);
        var grid = new Tree { AccessibilityName = "Grid", Position = new(300, 8), Size = new(652, 280), Columns = 3, HideRoot = true, ColumnTitlesVisible = true, SelectionMode = Tree.SelectMode.Multi };
        for (var col = 0; col < 3; col++) { grid.SetColumnTitle(col, "Column " + col); grid.SetColumnCustomMinimumWidth(col, 280); }
        var root = grid.CreateItem(); var branch = root.CreateChild(); branch.SetText(0, "Branch"); var folded = branch.CreateChild(); folded.SetText(0, "Folded child"); branch.Collapsed = true;
        var rows = new List<TreeItem>();
        for (var i = 0; i < 14; i++) { var row = root.CreateChild(); row.SetText(0, "Row " + i); row.SetText(1, "A " + i); row.SetText(2, "B " + i); rows.Add(row); }
        rows[2].SetText(0, "Merged row"); rows[2].SetText(1, ""); rows[2].SetText(2, ""); rows[2].SetExpandRight(0, true); rows[5].Visible = false; rows[1].Select(1); rows[13].Select(1); rows[13].SetSelectable(1, false); window.AddChild(grid);
        var destination = new Tree { AccessibilityName = "Destination", Position = new(300, 308), Size = new(652, 220), Columns = 3, HideRoot = true, ColumnTitlesVisible = true };
        var destinationRoot = destination.CreateItem(); var anchor = destinationRoot.CreateChild(); anchor.SetText(0, "Anchor"); window.AddChild(destination);
        var tabs = new TabBar { AccessibilityName = "Tabs", Position = new(8, 548), Size = new(944, 40) };
        for (var i = 0; i < 14; i++) tabs.AddTab("Wide tab " + i); tabs.CurrentTab = 1; tabs.SetTabDisabled(3, true); tabs.SetTabHidden(5, true); window.AddChild(tabs);
        var next = new Button("Next collection phase") { AccessibilityName = "Next collection phase", Position = new(8, 620), Size = new(210, 36) }; window.AddChild(next);
        RID manual = default, retiring = default, retireChild = default, toggle = default;
        var phase = 0; var activation = 0; var listUserEvents = 0; ItemList? clone = null;
        list.ItemActivated += _ => activation++; list.ItemSelected += _ => listUserEvents++; list.MultiSelected += (_, _) => listUserEvents++;
        next.Pressed += () =>
        {
            phase++;
            if (phase == 1)
            {
                Check(list.Current == 9 && list.IsSelected(9) && list.GetSelectedItems().Length == 1 && window.GetGUIFocusOwner() == list, "Native item focus uses the ordinary single/current selection and GUI focus.");
                Check(activation == 1 && listUserEvents == 0, "Activation is delivered; ordinary silent list selection stays silent.");
                list.MoveItem(9, 0); rows[0].MoveBefore(anchor); tabs.MoveTab(1, 8); tabs.DeselectEnabled = true;
                clone = (ItemList)list.Duplicate(); clone.AccessibilityName = "Clone"; clone.Position = new(8, 300); clone.Size = new(280, 220); window.AddChild(clone);
            }
            if (phase == 2) { grid.Columns = 2; list.RemoveItem(0); folded.Dispose(); }
            if (phase == 3)
            {
                Check(!AccessibilityServer.HasElement(retiring) && !AccessibilityServer.HasElement(retireChild), "Selection callbacks can retire their own collection and children.");
                Reject<KeyNotFoundException>(() => AccessibilityServer.UpdateSetListItemSelected(retireChild, true)); list.Select(1); tabs.CurrentTab = 2;
            }
        };
        Process? client = null; Task<string>? output = null, error = null; using var texture = window.GetTexture();
        var complete = false; var post = 0; var frames = 0; var measuring = false; long before = 0, idle = 0, active = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Actual renderer and native bridge are available.");
            var windowID = window.GetAccessibilityElement(); manual = AccessibilityServer.CreateSubElement(windowID, Role.Table); AccessibilityServer.UpdateSetName(manual, "Manual table"); AccessibilityServer.UpdateSetBounds(manual, new(8, 596, 280, 20));
            AccessibilityServer.UpdateSetTableRowCount(manual, 3); AccessibilityServer.UpdateSetTableColumnCount(manual, 3); AccessibilityServer.UpdateSetListItemCount(manual, 3); AccessibilityServer.UpdateSetListOrientation(manual, true); AccessibilityServer.UpdateSetFlag(manual, Flag.Multiselectable, true);
            var rowHeader = AccessibilityServer.CreateSubElement(manual, Role.RowHeader); AccessibilityServer.UpdateSetName(rowHeader, "Manual row 1"); AccessibilityServer.UpdateSetTableRowIndex(rowHeader, 1);
            var colHeader = AccessibilityServer.CreateSubElement(manual, Role.ColumnHeader); AccessibilityServer.UpdateSetName(colHeader, "Manual col 2"); AccessibilityServer.UpdateSetTableColumnIndex(colHeader, 2);
            RID Cell(string name, int row, int col, int rs, int cs, bool selected)
            {
                var cell = AccessibilityServer.CreateSubElement(manual, Role.Cell); AccessibilityServer.UpdateSetName(cell, name); AccessibilityServer.UpdateSetBounds(cell, new(8 + col * 30, 596, 30, 20));
                AccessibilityServer.UpdateSetTableCellPosition(cell, row, col); AccessibilityServer.UpdateSetTableCellSpan(cell, rs, cs); AccessibilityServer.UpdateSetListItemIndex(cell, row); AccessibilityServer.UpdateSetListItemLevel(cell, 0); AccessibilityServer.UpdateSetListItemSelected(cell, selected);
                AccessibilityServer.UpdateAddAction(cell, AAction.Focus, () => AccessibilityServer.UpdateSetListItemSelected(cell, true)); AccessibilityServer.UpdateAddAction(cell, AAction.Blur, () => AccessibilityServer.UpdateSetListItemSelected(cell, false)); return cell;
            }
            toggle = Cell("Spanning cell", 0, 0, 2, 2, true); Cell("Top right", 0, 2, 1, 1, false); Cell("Middle right", 1, 2, 1, 1, false); Cell("Bottom span", 2, 0, 1, 3, false);
            AccessibilityServer.UpdateSetListItemExpanded(toggle, true); AccessibilityServer.UpdateAddAction(toggle, AAction.Collapse, () => AccessibilityServer.UpdateSetListItemExpanded(toggle, false)); AccessibilityServer.UpdateAddAction(toggle, AAction.Expand, () => AccessibilityServer.UpdateSetListItemExpanded(toggle, true));
            var extreme = AccessibilityServer.CreateSubElement(windowID, Role.Table); AccessibilityServer.UpdateSetName(extreme, "Extreme table"); AccessibilityServer.UpdateSetTableRowCount(extreme, int.MaxValue); AccessibilityServer.UpdateSetTableColumnCount(extreme, 1);
            var extremeCell = AccessibilityServer.CreateSubElement(extreme, Role.Cell); AccessibilityServer.UpdateSetTableCellPosition(extremeCell, 0, 0); AccessibilityServer.UpdateSetTableCellSpan(extremeCell, int.MaxValue, 1); AccessibilityServer.UpdateSetListItemSelected(extremeCell, true);
            retiring = AccessibilityServer.CreateSubElement(windowID, Role.ListBox); AccessibilityServer.UpdateSetName(retiring, "Retiring collection"); AccessibilityServer.UpdateSetListItemCount(retiring, 1); retireChild = AccessibilityServer.CreateSubElement(retiring, Role.ListItem); AccessibilityServer.UpdateSetName(retireChild, "Retire child"); AccessibilityServer.UpdateSetListItemSelected(retireChild, false); AccessibilityServer.UpdateAddAction(retireChild, AAction.Focus, () => AccessibilityServer.FreeElement(retiring));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetListItemCount(manual, -1)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetListItemIndex(toggle, -1)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetListItemLevel(toggle, -1));
            Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetTableRowCount(manual, -1)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetTableColumnCount(manual, -1)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetTableCellPosition(toggle, 0, -1)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetTableCellSpan(toggle, 0, 1)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetTableRowIndex(rowHeader, -1)); Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetTableColumnIndex(colHeader, -1));
            Task.Run(() => Reject<InvalidOperationException>(() => AccessibilityServer.UpdateSetListItemSelected(toggle, true))).GetAwaiter().GetResult();
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/collection.py")); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96); if (measuring) before = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 80)
                {
                    var alternate = (post & 1) == 0; list.Select(alternate ? 1 : 2); list.Current = alternate ? 1 : 2;
                    grid.DeselectAll(); rows[alternate ? 1 : 3].Select(1); tabs.CurrentTab = alternate ? 6 : 7;
                    AccessibilityServer.UpdateSetListItemSelected(toggle, alternate);
                }
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (!complete) return; post++;
            if (post == 8 && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } dir) { Directory.CreateDirectory(dir); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(dir, "collection-" + backend + ".png")); }
            if (post == 160) { Check(idle == 0 && active == 0, $"Collection semantic idle={idle}, active={active} managed bytes."); window.Tree!.Quit(); }
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) => { frames++; if (!complete && client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && phase == 3, "Independent collection client completed all phases."); complete = true; } if (Stopwatch.GetElapsedTime(started).TotalSeconds > 90) throw new TimeoutException("Native collection client timed out."); }, window) == 0, "Collection host completes.");
        }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        RunCallbackFailure(); Console.WriteLine($"PASS {_checks} native collection contracts on {backend}; {frames} frames, 64 idle/64 active {idle}/{active} managed bytes.");
    }
    private static void RunCallbackFailure()
    {
        var window = new Window { Size = new(120, 80) }; Process? client = null; Task<string>? output = null, error = null; RID producer = default;
        window.Ready += _ =>
        {
            producer = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.ListBox); AccessibilityServer.UpdateSetName(producer, "Throwing collection"); AccessibilityServer.UpdateSetListItemCount(producer, 1); var item = AccessibilityServer.CreateSubElement(producer, Role.ListItem); AccessibilityServer.UpdateSetListItemSelected(item, false); AccessibilityServer.UpdateAddAction(item, AAction.Focus, () => throw new InvalidOperationException("Injected native collection failure"));
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/collection.py")); start.ArgumentList.Add("--failure"); client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
        };
        var started = Stopwatch.GetTimestamp(); var failed = false;
        try { Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Collection failure client timed out."); }, window); }
        catch (AggregateException errors) { var failures = errors.Flatten().InnerExceptions; failed = failures.Count == 1 && failures[0] is InvalidOperationException { Message: "Injected native collection failure" }; if (!failed) foreach (var failure in failures) Console.WriteLine(failure); }
        finally
        {
            if (client is { HasExited: false } && !client.WaitForExit(5000)) client.Kill(true); if (output is not null) Console.Write(output.GetAwaiter().GetResult()); if (error is not null) Console.Write(error.GetAwaiter().GetResult()); Check(failed && client?.ExitCode == 0 && !AccessibilityServer.HasElement(producer), $"Collection failure replies to the client, reaches its owner and retires the producer (owner={failed}, client={client?.ExitCode}, retained={AccessibilityServer.HasElement(producer)})."); client?.Dispose();
        }
        var fresh = new Window { Size = new(100, 60) }; fresh.Ready += _ => { Check(AccessibilityServer.IsSupported(), "Display reopens after native collection failure."); fresh.Tree!.Quit(); }; Check(Engine.Run(fresh) == 0, "Fresh display closes cleanly.");
    }
}
