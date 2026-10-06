using Electron2D;

internal static class PopupTests
{
    internal static void Run()
    {
        using var root = new SubViewport { Size = new(320, 200), GUIEmbedSubwindows = true };
        var popup = new PopupPanel { Name = "PanelPopup", Size = new(100, 60), Position = new(20, 30) };
        var button = new Button { Name = "Action", Text = "Action" }; popup.AddChild(button); button.Owner = popup; root.AddChild(popup); popup.Owner = root; button.Owner = root;
        using var tree = new SceneTree(root);
        Check(!popup.Visible && popup.IsEmbedded() && popup.Borderless && popup.PopupWindow && popup.Transient && popup.WrapControls && popup.MinimizeDisabled && popup.MaximizeDisabled && popup.Transparent && popup.TransparentBG, "Popup defaults and active embedded identity.");
        var phases = new List<string>(); popup.AboutToPopup += () => phases.Add("about"); popup.VisibilityChanged += () => phases.Add("visible"); popup.FocusEntered += () => phases.Add("focus");
        popup.PopupCentered(new(100, 60)); Check(popup.Visible && popup.Position == new Vector2i(110, 70) && popup.HasFocus() && phases.SequenceEqual(new[] { "about", "focus", "visible" }), "Popup positions, appears and acquires focus.");
        Check(button.Size == (Vector2)popup.Size && root.GetEmbeddedSubwindows().Single() == popup, "PopupPanel arranges public content and root exposes borrowed window.");
        var pressed = 0; button.Pressed += () => pressed++;
        Send(root, new InputEventMouseButton { Position = popup.Position + new Vector2(12, 12), ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
        Send(root, new InputEventMouseButton { Position = popup.Position + new Vector2(12, 12), ButtonIndex = MouseButton.Left, Pressed = false }); Check(pressed == 1, "Root input reaches popup-local GUI.");
        var hides = 0; popup.PopupHide += () => hides++;
        Send(root, new InputEventMouseButton { Position = new(1, 1), ButtonIndex = MouseButton.Left, Pressed = true }); Check(popup.Visible, "Outside click queues hide."); tree.FlushDeferred(); Check(!popup.Visible && hides == 1 && root.GetEmbeddedSubwindows().Length == 0, "Queued hide commits once and removes input/display visibility.");
        popup.Popup(new(new(310, 190), new(100, 60))); Check(popup.Position == new Vector2i(220, 140), "Popup fits the host rectangle.");
        Send(root, new InputEventAction { Action = "ui_cancel", Pressed = true }); tree.FlushDeferred(); Check(!popup.Visible && hides == 2, "Cancel action uses the same deferred hide.");
        popup.Exclusive = true; popup.PopupCentered(); Send(root, new InputEventMouseButton { Position = new(1, 1), ButtonIndex = MouseButton.Left, Pressed = true }); tree.FlushDeferred(); Check(popup.Visible, "Exclusive popup blocks outside dismissal."); popup.Hide();
        Reject<ArgumentOutOfRangeException>(() => popup.PopupCenteredRatio(float.NaN)); Reject<ArgumentOutOfRangeException>(() => popup.PopupCenteredRatio(0)); Reject<InvalidOperationException>(() => root.GUIEmbedSubwindows = false);
        using (var packed = new PackedScene()) { packed.Pack(root); using var copy = (SubViewport)packed.Instantiate(); var reconstructed = (PopupPanel)copy.GetChild(0); Check(!reconstructed.Visible && reconstructed.PopupWindow && reconstructed.Transparent && reconstructed.Position == popup.Position && reconstructed.GetChildCount() == 1, $"Packed popup retains exact factories, configured geometry and public content: visible={reconstructed.Visible}, popup={reconstructed.PopupWindow}, transparent={reconstructed.Transparent}, position={reconstructed.Position}/{popup.Position}, children={reconstructed.GetChildCount()}."); }
        var second = new Popup { Name = "Nested", Size = new(80, 40) }; popup.AddChild(second); popup.PopupCentered(); second.Popup(new(new(30, 30), new(80, 40))); Check(second.HasFocus() && !popup.HasFocus(), "Nested popup is topmost keyboard target."); second.Hide(); Check(popup.HasFocus(), "Hiding top popup restores previous visible window."); popup.Hide();
        popup.PopupCentered(); Action badObserver = () => throw new InvalidOperationException("observer"); popup.PopupHide += badObserver; Reject<AggregateException>(popup.Hide); Check(!popup.Visible, "Observer failure follows committed hidden state."); popup.PopupHide -= badObserver;
        using (var shadowStyle = new StyleBoxFlat { ShadowSize = 4, ShadowOffset = new(2, 0) })
        { popup.AddThemeStyleBoxOverride("panel", shadowStyle); popup.LayoutDirection = LayoutDirection.RTL; popup.Popup(new(new(40, 40), new(100, 60))); Check(popup.Position.X == 34 && button.IsLayoutRTL(), "Window RTL mirrors shadow insets and propagates control direction."); popup.Size = new(130, 80); popup.Hide(); Check(popup.Size == new Vector2i(130, 80), "External resize cancels restoration of the previous shadow-expanded rectangle."); popup.RemoveThemeStyleBoxOverride("panel"); popup.LayoutDirection = LayoutDirection.Inherited; }
        popup.Borderless = false; popup.Popup(new(new(70, 70), new(100, 60)));
        Send(root, new InputEventMouseButton { Position = new(90, 52), ButtonIndex = MouseButton.Left, Pressed = true }); Send(root, new InputEventMouseMotion { Position = new(110, 62), Relative = new(20, 10), ButtonMask = MouseButtonMask.Left }); Send(root, new InputEventMouseButton { Position = new(110, 62), ButtonIndex = MouseButton.Left, Pressed = false }); Check(popup.Position == new Vector2i(90, 80), "Embedded title bar moves its window through captured input.");
        Send(root, new InputEventMouseButton { Position = new(popup.Position.X + popup.Size.X - 18, popup.Position.Y - 18), ButtonIndex = MouseButton.Left, Pressed = true }); tree.FlushDeferred(); Check(!popup.Visible, "Embedded close decoration emits the popup close request."); popup.Borderless = true;
        var textField = new LineEdit { Name = "TextField" }; popup.AddChild(textField); popup.PopupCentered(); textField.GrabFocus(); tree.DispatchCommittedText(popup, "typed"); Check(textField.Text == "typed", "Committed native text resolves the popup's own GUI focus."); popup.RemoveChild(textField); textField.Dispose(); popup.Hide();
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-popup-" + Guid.NewGuid() + ".e2dscene");
        using (var packed = new PackedScene()) { packed.Pack(root); ResourceSaver.Save(packed, path); }
        try { FreshProcess(path); } finally { File.Delete(path); }
        var invalid = new Popup(); using var detachedRoot = new SubViewport(); detachedRoot.AddChild(invalid); using var detachedTree = new SceneTree(detachedRoot); Reject<NotSupportedException>(() => invalid.Popup()); Check(!invalid.Visible, "Missing native child host rejects before visibility mutation.");
        Console.WriteLine("Popup defaults, model, geometry, routed input, deferred hide, focus, observer failure and packing passed.");
    }

    private static void FreshProcess(string path)
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(PopupTests).Assembly.Location);
        start.Environment.Remove("ELECTRON2D_TEST_POPUP"); start.Environment["ELECTRON2D_TEST_POPUP_CHILD"] = path;
        using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh popup scene process."); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh popup scene passed"), "Fresh popup archive: " + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    {
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var root = (SubViewport)loaded.Instantiate(); using var tree = new SceneTree(root); var popup = (PopupPanel)root.GetChild(0); popup.PopupCentered(); Check(popup.IsEmbedded() && popup.Visible && popup.GetChild(0) is Button, "Fresh built-in popup reconstructs and executes."); popup.Hide(); Console.WriteLine("Fresh popup scene passed");
    }
    private static void Send(Viewport viewport, InputEvent input) { using (input) viewport.PushInput(input, true); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
