using System.Diagnostics;
using Electron2D;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using ActionKind = Electron2D.AccessibilityServer.AccessibilityAction;
using Path = System.IO.Path;

internal static partial class AccessibilityRelationTests
{
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Semantic Relations Probe");
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var group = new ButtonGroup(); using var otherGroup = new ButtonGroup();
        var window = new Window { Size = new(420, 300) };
        var caption = new Label { Name = "GainLabel", Text = "Gain", Position = new(12, 8), Size = new(120, 24) };
        var alternate = new Label { Name = "Alternate", Text = "Alternative gain", Position = new(200, 8), Size = new(180, 24) };
        var help = new Label { Name = "Help", Text = "Choose the audio gain", Position = new(12, 38), Size = new(380, 24) };
        var errorLabel = new Label { Name = "Error", Text = "Gain must not exceed 80", Position = new(12, 110), Size = new(380, 24), Visible = false };
        var slider = new HSlider { Name = "GainInput", Value = 20, Position = new(12, 70), Size = new(240, 28), AccessibilityLabeledByNodes = ["../GainLabel", "../GainLabel", "", "../Missing"], AccessibilityDescribedByNodes = ["../Help"], AccessibilityControlsNodes = ["../Meter"], AccessibilityFlowToNodes = ["../Next"] };
        var meter = new ProgressBar { Name = "Meter", AccessibilityName = "Gain meter", ShowPercentage = false, Position = new(268, 70), Size = new(136, 28) };
        var radioA = new Button("Stereo") { Name = "RadioA", ToggleMode = true, ButtonGroup = group, ButtonPressed = true, Position = new(12, 150), Size = new(100, 30) };
        var radioB = new Button("Mono") { Name = "RadioB", ToggleMode = true, ButtonGroup = group, Position = new(122, 150), Size = new(100, 30) };
        var chooser = new Button("Choose A or B") { Name = "Chooser", AccessibilityName = "Chooser", FocusMode = FocusMode.All, Position = new(230, 150), Size = new(170, 30) };
        var form = new Control { AccessibilityName = "Form group", Position = new(280, 200), Size = new(120, 32) };
        var unnamed = new Button("") { Name = "NamedByRelation", AccessibilityLabeledByNodes = ["../GainLabel"], Position = new(142, 232), Size = new(110, 32) };
        var next = new Button("Next phase") { Name = "Next", Position = new(12, 232), Size = new(120, 32) };
        foreach (var node in new Control[] { caption, alternate, help, errorLabel, slider, meter, radioA, radioB, chooser, form, unnamed, next }) window.AddChild(node);
        var phase = 0; var frames = 0; var post = 0; var complete = false; var measuring = false;
        long startBytes = 0, idle = 0, active = 0; RID sliderID = default, chooserID = default, first = default, second = default, link = default, manualA = default, manualB = default;
        slider.ValueChanged += value =>
        {
            meter.Value = value; errorLabel.Visible = value > 80;
            AccessibilityServer.UpdateSetErrorMessage(slider.GetAccessibilityElement(), value > 80 ? errorLabel.GetAccessibilityElement() : default);
        };
        next.Pressed += () =>
        {
            phase++;
            switch (phase)
            {
                case 1: slider.AccessibilityLabeledByNodes = ["../Alternate"]; unnamed.AccessibilityLabeledByNodes = ["../GainLabel", "../Alternate"]; break;
                case 2: window.RemoveChild(caption); caption.Dispose(); alternate.Name = "Renamed"; break;
                case 3:
                    alternate.Name = "Alternate"; caption = new Label { Name = "GainLabel", Text = "Restored gain", Position = new(12, 8), Size = new(160, 24) }; window.AddChild(caption); break;
                case 4: unnamed.AccessibilityLabeledByNodes = ["../Alternate", "../GainLabel"]; chooser.Text = "Option B selected"; AccessibilityServer.UpdateSetActiveDescendant(chooserID, second); break;
                case 5: chooser.Text = "Choose A or B"; AccessibilityServer.UpdateAddChild(window.GetAccessibilityElement(), second); break;
                case 6: AccessibilityServer.FreeElement(second); break;
                case 7: radioB.ToggleMode = false; break;
                case 8: radioB.ToggleMode = true; break;
                case 9: radioB.ButtonGroup = otherGroup; AccessibilityServer.UpdateSetMemberOf(sliderID, default); break;
                case 10: radioB.ButtonGroup = group; AccessibilityServer.UpdateSetMemberOf(sliderID, form.GetAccessibilityElement()); break;
            }
        };
        using var texture = window.GetTexture(); Process? client = null; Task<string>? output = null, errors = null;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "Actual renderer and native bridge.");
            sliderID = slider.GetAccessibilityElement(); chooserID = chooser.GetAccessibilityElement();
            first = AccessibilityServer.CreateSubElement(chooserID, Role.StaticText); AccessibilityServer.UpdateSetName(first, "Option A"); AccessibilityServer.UpdateSetBounds(first, new(230, 150, 70, 30));
            second = AccessibilityServer.CreateSubElement(chooserID, Role.StaticText); AccessibilityServer.UpdateSetName(second, "Option B"); AccessibilityServer.UpdateSetBounds(second, new(310, 150, 70, 30));
            AccessibilityServer.UpdateSetActiveDescendant(chooserID, first);
            AccessibilityServer.UpdateAddRelatedLabeledBy(sliderID, caption.GetAccessibilityElement());
            AccessibilityServer.UpdateAddRelatedLabeledBy(sliderID, caption.GetAccessibilityElement());
            AccessibilityServer.UpdateAddRelatedDetails(sliderID, help.GetAccessibilityElement());
            AccessibilityServer.UpdateSetMemberOf(sliderID, form.GetAccessibilityElement());
            link = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Link); AccessibilityServer.UpdateSetName(link, "Internal option link"); AccessibilityServer.UpdateSetInPageLinkTarget(link, second);
            manualA = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.RadioButton); AccessibilityServer.UpdateSetName(manualA, "Manual radio A");
            manualB = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.RadioButton); AccessibilityServer.UpdateSetName(manualB, "Manual radio B");
            AccessibilityServer.UpdateAddRelatedRadioGroup(manualA, manualB); AccessibilityServer.UpdateAddRelatedRadioGroup(manualB, manualA);
            AccessibilityServer.UpdateAddRelatedControls(manualA, meter.GetAccessibilityElement());
            AccessibilityServer.UpdateAddRelatedDescribedBy(manualA, help.GetAccessibilityElement());
            AccessibilityServer.UpdateAddRelatedFlowTo(manualA, manualB); AccessibilityServer.UpdateAddRelatedFlowTo(manualB, manualA);
            Reject<KeyNotFoundException>(() => AccessibilityServer.UpdateAddRelatedControls(default, first));
            Reject<KeyNotFoundException>(() => AccessibilityServer.UpdateAddRelatedDetails(sliderID, default));
            Reject<ArgumentException>(() => AccessibilityServer.UpdateSetActiveDescendant(chooserID, chooserID));
            Reject<ArgumentException>(() => AccessibilityServer.UpdateSetActiveDescendant(chooserID, sliderID));
            Task.Run(() => Reject<InvalidOperationException>(() => AccessibilityServer.UpdateAddRelatedControls(sliderID, first))).GetAwaiter().GetResult();
            var foreign = new Window { Size = new(80, 60) }; window.AddChild(foreign); foreign.Show();
            Reject<ArgumentException>(() => AccessibilityServer.UpdateAddRelatedLabeledBy(sliderID, foreign.GetAccessibilityElement()));
            window.RemoveChild(foreign); foreign.Dispose();
            var owner = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Container);
            var survivor = AccessibilityServer.CreateElement(window.GetWindowID(), Role.StaticText); AccessibilityServer.UpdateSetName(survivor, "Detached survivor");
            AccessibilityServer.UpdateAddChild(owner, survivor); AccessibilityServer.UpdateSetActiveDescendant(owner, survivor); AccessibilityServer.UpdateSetFocus(owner);
            AccessibilityServer.FreeElement(owner); AccessibilityServer.UpdateAddChild(window.GetAccessibilityElement(), survivor);
            var cycle = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Container);
            AccessibilityServer.UpdateAddRelatedControls(cycle, cycle); AccessibilityServer.UpdateAddRelatedFlowTo(cycle, cycle); AccessibilityServer.FreeElement(cycle);
            Check(!AccessibilityServer.HasElement(cycle) && AccessibilityServer.HasElement(survivor), "Cyclic references retire and independent semantic children survive.");
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/relations.py"));
            client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); errors = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96);
                if (measuring) startBytes = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 80)
                {
                    AccessibilityServer.UpdateAddRelatedRadioGroup(manualA, manualB);
                    AccessibilityServer.UpdateSetActiveDescendant(chooserID, (post & 1) == 0 ? first : default);
                    radioA.ButtonPressed = (post & 1) == 0;
                }
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - startBytes; if (post < 80) idle += bytes; else active += bytes; }
            AccessibilityServer.SetWindowFocused(window.GetWindowID(), true);
            if (!complete) return;
            post++;
            if (post == 8 && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder)
            { Directory.CreateDirectory(folder); using var image = texture.GetImage()!; image.SavePNG(Path.Combine(folder, "relations-" + backend + ".png")); }
            if (post == 160) { Check(idle == 0 && active == 0, $"Prepared relations idle={idle}, active={active} managed owner bytes."); window.Tree!.Quit(); }
        }
        var started = Stopwatch.GetTimestamp();
        try
        {
            Check(Engine.Run((_, _) =>
            {
                frames++;
                if (!complete && client?.HasExited == true)
                {
                    Console.Write(output!.GetAwaiter().GetResult()); Console.Write(errors!.GetAwaiter().GetResult());
                    Check(client.ExitCode == 0 && phase == 10 && slider.Value == 40 && meter.Value == 40, "External client completed composed relations, validation and group changes."); complete = true;
                }
                if (Stopwatch.GetElapsedTime(started).TotalSeconds > 45) throw new TimeoutException("Relation native client timed out.");
            }, window) == 0, "Native relation host completes.");
        }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw;
            if (client is { HasExited: false }) client.Kill(true); client?.Dispose();
        }
        Check(!AccessibilityServer.HasElement(first) && !AccessibilityServer.HasElement(link) && !AccessibilityServer.HasElement(sliderID), "Display teardown retires graph, anchors, active targets and source identities.");
        Console.WriteLine($"PASS {_checks} relation contracts on {backend}; {frames} frames, 64 idle/64 active, {idle}/{active} managed bytes.");
    }
}
