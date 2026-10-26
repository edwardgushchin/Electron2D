using System.Diagnostics;
using Electron2D;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;

internal static partial class AccessibilityFoundationTests
{
    private static void NativeWindowEdges(Window parent)
    {
        var child = new Window { Title = "Independent semantic window", Size = new(64, 48) }; parent.AddChild(child); child.Show();
        Check(child.GetWindowID() > 0, "Independent native child window exists.");
        var parentRoot = parent.GetAccessibilityElement(); var childRoot = child.GetAccessibilityElement();
        Check(parentRoot != childRoot && AccessibilityServer.HasElement(childRoot), "Each native window has its own semantic root.");
        Reject<ArgumentException>(() => AccessibilityServer.UpdateAddChild(parentRoot, AccessibilityServer.CreateElement(child.GetWindowID(), Role.Button)));
        var owned = AccessibilityServer.CreateSubElement(childRoot, Role.Button); AccessibilityServer.UpdateSetFocus(owned);
        parent.RemoveChild(child); child.Dispose();
        Check(!AccessibilityServer.HasElement(childRoot) && !AccessibilityServer.HasElement(owned), "Native child retirement removes roots, owned descendants and unattached elements.");
    }
    internal static void RunLifetimes()
    {
        var folder = Directory.CreateTempSubdirectory("electron2d-accessibility-policy-").FullName;
        try
        {
            using var registry = new ProjectSettingsRegistry(folder, System.IO.Path.Combine(folder, "user"));
            registry.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always); var path = System.IO.Path.Combine(folder, "policy.cfg"); registry.SaveCustom(path);
            using var loaded = new ProjectSettingsRegistry(folder, System.IO.Path.Combine(folder, "loaded-user")); loaded.LoadCustom(path);
            Check(loaded.Get(ProjectSettings.AccessibilitySupport) == AccessibilitySupport.Always, "Typed policy persists with compiled enum metadata, including NativeAOT.");
        }
        finally { Directory.Delete(folder, true); }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Disabled);
        var disabled = new Window { Size = new(120, 80) }; var control = new Button("Disabled policy") { FocusMode = FocusMode.Accessibility }; disabled.AddChild(control);
        Check(Engine.Run((_, _) =>
        {
            Check(!AccessibilityServer.IsSupported() && !control.GetAccessibilityElement().IsValid(), "Disabled policy publishes no semantic service.");
            control.GrabFocus(); Check(!control.HasFocus(), "Accessibility-only focus remains ineligible when output is disabled.");
            Reject<NotSupportedException>(() => AccessibilityServer.CreateElement(0, Role.Button)); disabled.Tree!.Quit();
        }, disabled) == 0, "Disabled application profile completes.");
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Auto);
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D AT-SPI Failure Probe");
        var window = new Window { Size = new(120, 80) }; Process? client = null; Task<string>? output = null, errors = null; RID id = default; var failed = false;
        window.Ready += _ =>
        {
            id = AccessibilityServer.CreateSubElement(AccessibilityServer.GetWindowRoot(window.GetWindowID()), Role.Button);
            AccessibilityServer.UpdateSetName(id, "Failing action");
            AccessibilityServer.UpdateAddAction(id, AAction.Click, () => throw new InvalidOperationException("Probe callback failure"));
            var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(System.IO.Path.GetFullPath("tests/AccessibilityClient/failure.py"));
            client = Process.Start(start)!; output = client.StandardOutput.ReadToEndAsync(); errors = client.StandardError.ReadToEndAsync();
        };
        var started = Stopwatch.GetTimestamp();
        try
        {
            Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Native failure probe timed out."); }, window);
        }
        catch (AggregateException exception) when (exception.Flatten().InnerExceptions.Any(error => error.Message == "Probe callback failure")) { failed = true; }
        finally
        {
            if (client is not null)
            {
                if (!client.WaitForExit(5000)) client.Kill(true);
                Console.Write(output!.GetAwaiter().GetResult()); Console.Write(errors!.GetAwaiter().GetResult());
                Check(client.ExitCode == 0, "External client received the callback's native failure reply."); client.Dispose();
            }
        }
        Check(failed && !AccessibilityServer.HasElement(id) && !AccessibilityServer.IsSupported(), "Callback failure crosses the owner pump safely and teardown retires its identity.");
        RunPrototype();
        Console.WriteLine($"PASS {_checks} disabled-policy/callback-failure/display-reopening contracts.");
    }
}
