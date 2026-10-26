using System.Diagnostics;
using System.Globalization;
using Electron2D;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;

internal static partial class AccessibilityFoundationTests
{
    internal static void RunPrototype() => Run("compatibility", automatic: false);
    internal static void RunNative(string backend) => Run(backend, automatic: true);
    private static void Run(string backend, bool automatic)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D AT-SPI Foundation Probe");
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, automatic ? AccessibilitySupport.Always : AccessibilitySupport.Auto);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var previousCulture = TranslationServer.Culture;
        TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
        using var catalog = new Translation { Locale = "en" };
        catalog.AddMessage("Activate key", "Activate probe"); catalog.AddMessage("Description key", "Activates the native probe");
        var domain = TranslationServer.GetOrAddDomain("accessibility-native"); domain.AddTranslation(catalog);
        var window = new Window { Title = "Accessibility foundation", Size = new(240, 120) };
        var label = new Label { Text = "Waiting for native action", Position = new(8, 8), Size = new(224, 32) };
        window.AddChild(label);
        var button = new CustomButton { Text = "Go", TranslationDomain = "accessibility-native", AccessibilityName = "Activate key", AccessibilityDescription = "Description key", Position = new(8, 48), Size = new(120, 32), FocusMode = FocusMode.Accessibility, CompositeFocus = automatic };
        if (automatic) window.AddChild(button);
        using var texture = window.GetTexture();
        Container? landmark = null;
        Process? client = null; Task<string>? output = null, errors = null;
        var clicks = 0; var focuses = 0; var frames = 0; var postClient = 0; long allocationStart = 0, idleBytes = 0, activeBytes = 0;
        var complete = false; var measuring = false; RID root = default, action = default, retired = default;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Actual requested backend is active.");
            Check(AccessibilityServer.IsSupported(), "A real native accessibility bridge is established.");
            if (automatic)
            {
                button.Pressed += () =>
                {
                    clicks++; label.Text = "Native action received";
                    if (clicks == 2)
                    {
                        retired = button.GetAccessibilityElement(); window.RemoveChild(button);
                        var group = new Control { AccessibilityName = "Moved group", Size = window.Size }; window.AddChild(group); group.AddChild(button);
                        landmark = new VBoxContainer { AccessibilityRegion = true, AccessibilityName = "Landmark probe", Size = new(20, 20) }; group.AddChild(landmark);
                        var matrix = AccessibilityServer.CreateSubElement(group.GetAccessibilityElement(), Role.Container);
                        AccessibilityServer.UpdateSetName(matrix, "Role matrix");
                        foreach (var role in Enum.GetValues<Role>())
                        {
                            var item = AccessibilityServer.CreateSubElement(matrix, role); AccessibilityServer.UpdateSetName(item, "role:" + (int)role);
                            foreach (var flag in Enum.GetValues<AccessibilityServer.AccessibilityFlags>()) AccessibilityServer.UpdateSetFlag(item, flag, true);
                            AccessibilityServer.UpdateSetChecked(item, true);
                        }
                    }
                    if (clicks == 3) { button.Disabled = true; button.Hide(); landmark!.AccessibilityRegion = false; }
                };
                button.FocusEntered += () => focuses++;
                ServerEdges(window.GetWindowID(), button.GetAccessibilityElement());
            }
            else
            {
                root = AccessibilityServer.GetWindowRoot(window.GetWindowID());
                var text = AccessibilityServer.CreateSubElement(root, Role.StaticText); AccessibilityServer.UpdateSetName(text, label.Text); AccessibilityServer.UpdateSetBounds(text, new(8, 8, 224, 32));
                action = AccessibilityServer.CreateSubElement(root, Role.Button); AccessibilityServer.UpdateSetName(action, "Activate probe"); AccessibilityServer.UpdateSetBounds(action, new(8, 48, 120, 32));
                AccessibilityServer.UpdateAddAction(action, AAction.Click, () =>
                {
                    clicks++; label.Text = "Native action received"; AccessibilityServer.UpdateSetName(text, label.Text);
                    if (clicks == 1) AccessibilityServer.UpdateSetTransform(action, new Transform(0, new Vector2(4, 0)));
                    if (clicks == 2) AccessibilityServer.UpdateSetFlag(action, AccessibilityServer.AccessibilityFlags.TouchPassthrough, true);
                    if (clicks == 3)
                    {
                        AccessibilityServer.UpdateSetFlag(action, AccessibilityServer.AccessibilityFlags.TouchPassthrough, false);
                        AccessibilityServer.UpdateSetBounds(action, new(300, 48, 20, 32)); AccessibilityServer.UpdateSetFlag(root, AccessibilityServer.AccessibilityFlags.ClipsChildren, true);
                    }
                    if (clicks == 4) AccessibilityServer.UpdateSetFlag(root, AccessibilityServer.AccessibilityFlags.ClipsChildren, false);
                    if (clicks == 5)
                    {
                        AccessibilityServer.SetWindowRect(window.GetWindowID(), new(3e38f, 150, 256, 156), new(3e38f, 170, 240, 120));
                        AccessibilityServer.UpdateSetBounds(action, new(3e38f, 48, 20, 32));
                    }
                    if (clicks == 6)
                    {
                        var parent = AccessibilityServer.CreateElement(window.GetWindowID(), Role.Container);
                        AccessibilityServer.UpdateSetName(parent, "Deferred subtree");
                        var child = AccessibilityServer.CreateSubElement(parent, Role.Button); AccessibilityServer.UpdateSetName(child, "Deferred child");
                        AccessibilityServer.UpdateAddChild(root, parent);
                    }
                });
                AccessibilityServer.SetWindowRect(window.GetWindowID(), new(100, 150, 256, 156), new(108, 170, 240, 120));
                AccessibilityServer.UpdateAddAction(action, AAction.Focus, () => { focuses++; AccessibilityServer.SetWindowFocused(window.GetWindowID(), true); AccessibilityServer.UpdateSetFocus(action); });
            }
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(System.IO.Path.GetFullPath("tests/AccessibilityClient/probe.py"));
            if (automatic) start.ArgumentList.Add("--automatic");
            client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); errors = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && postClient >= 16 && (postClient < 80 || postClient >= 96);
                if (measuring) allocationStart = GC.GetAllocatedBytesForCurrentThread();
                if (complete && postClient >= 80 && automatic) button.ButtonPressed = (postClient & 1) == 0;
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring)
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
                if (postClient < 80) idleBytes += bytes; else activeBytes += bytes;
            }
            if (!complete) return;
            postClient++;
            if (postClient == 8 && automatic)
            {
                var folder = Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR");
                if (folder is not null) { Directory.CreateDirectory(folder); using var image = texture.GetImage()!; Check(image.Width == 240 && image.Height == 120, "Real rendered capture dimensions."); image.SavePNG(System.IO.Path.Combine(folder, "accessibility-" + backend + ".png")); }
            }
            if (postClient != 160) return;
            Check(idleBytes == 0 && activeBytes == 0, $"Warmed accessibility idle={idleBytes}, active={activeBytes} managed bytes.");
            window.Tree!.Quit();
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) =>
            {
                frames++;
                if (!complete && client?.HasExited == true)
                {
                    var text = output!.GetAwaiter().GetResult(); var error = errors!.GetAwaiter().GetResult(); Console.Write(text); Console.Write(error);
                    Check(client.ExitCode == 0 && clicks == (automatic ? 3 : 6) && focuses == (automatic ? 2 : 1), $"Native client={client.ExitCode}, clicks={clicks}, focuses={focuses}.");
                    if (automatic)
                    {
                        root = window.GetAccessibilityElement(); action = button.GetAccessibilityElement();
                        Check(button.AccessibilityName == "Activate key" && button.AccessibilityDescription == "Description key", "Translated native metadata retains its authored source text.");
                        Check(root.IsValid() && action.IsValid() && action != retired && !AccessibilityServer.HasElement(retired), "Reparenting retires the former borrowed semantic identity.");
                        Check(button.Invalidations == 1 && button.Updates >= 2, "Automatic invalidation and metadata update notifications reached the producer.");
                        button.Disabled = false; button.Show(); button.ToggleMode = true;
                        button.QueueAccessibilityUpdate(); button.QueueAccessibilityUpdate();
                        NativeWindowEdges(window);
                    }
                    complete = true;
                }
                if (Stopwatch.GetElapsedTime(started).TotalSeconds > 30) throw new TimeoutException("Native accessibility execution timed out.");
            }, window) == 0, "Native accessibility host completed.");
        }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw;
            if (client is { HasExited: false }) client.Kill(true); client?.Dispose();
            if (!automatic) button.Dispose();
            domain.RemoveTranslation(catalog); TranslationServer.Culture = previousCulture;
        }
        Check(!AccessibilityServer.HasElement(root) && !AccessibilityServer.HasElement(action), "Display teardown retires every borrowed semantic identity.");
        Console.WriteLine($"PASS {_checks} accessibility {(automatic ? backend : "manual prototype")} contracts: {frames} frames; 64 idle + 64 active frames, {idleBytes}/{activeBytes} managed bytes.");
    }
}
