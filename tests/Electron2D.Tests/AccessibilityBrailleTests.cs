using System.Diagnostics;
using Electron2D;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
using Path = System.IO.Path;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;

internal static class AccessibilityBrailleTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Braille Probe");
        ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);

        var window = new Window { Size = new(360, 240) };
        RID target = default;
        Process? client = null;
        Task<string>? output = null;
        Task<string>? error = null;
        var complete = false;

        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(),
                "The requested renderer and native accessibility bridge must be active.");
            var root = window.GetAccessibilityElement();
            target = AccessibilityServer.CreateSubElement(root, Role.Button);
            AccessibilityServer.UpdateSetName(target, "Ordinary name");
            AccessibilityServer.UpdateSetRoleDescription(target, "Ordinary role description");

            var controller = AccessibilityServer.CreateSubElement(root, Role.Button);
            AccessibilityServer.UpdateSetName(controller, "Braille controller");
            Action[] steps =
            [
                () => AccessibilityServer.UpdateSetBrailleLabel(target, "⠃⠗ имя"),
                () => AccessibilityServer.UpdateSetBrailleRoleDescription(target, "⠗ роль 🔔"),
                () => AccessibilityServer.UpdateSetBrailleLabel(target, "Contrôle ⠉"),
                () => AccessibilityServer.UpdateSetBrailleRoleDescription(target, "操作 ⠏"),
                () =>
                {
                    AccessibilityServer.UpdateSetBrailleLabel(target, "Contrôle ⠉");
                    AccessibilityServer.UpdateSetBrailleRoleDescription(target, "操作 ⠏");
                },
                () => AccessibilityServer.UpdateSetBrailleLabel(target, ""),
                () =>
                {
                    AccessibilityServer.UpdateSetBrailleLabel(target, "⠃⠗ имя");
                    AccessibilityServer.UpdateSetBrailleRoleDescription(target, "");
                },
                () => AccessibilityServer.UpdateSetBrailleLabel(target, ""),
                () =>
                {
                    AccessibilityServer.UpdateSetBrailleLabel(target, "");
                    AccessibilityServer.UpdateSetBrailleRoleDescription(target, "");
                    Reject<ArgumentNullException>(() => AccessibilityServer.UpdateSetBrailleLabel(target, null!));
                    Reject<ArgumentNullException>(() => AccessibilityServer.UpdateSetBrailleRoleDescription(target, null!));
                    RejectMissingRID(default);
                },
                () =>
                {
                    AccessibilityServer.UpdateSetBrailleLabel(target, "⠃⠗ имя");
                    AccessibilityServer.UpdateSetBrailleRoleDescription(target, "⠗ роль 🔔");
                },
                () =>
                {
                    AccessibilityServer.FreeElement(target);
                    Check(!AccessibilityServer.HasElement(target), "The freed RID must leave the service.");
                    RejectMissingRID(target);
                    complete = true;
                },
            ];

            AccessibilityServer.UpdateAddAction(controller, AAction.Custom, (int index) =>
            {
                steps[index]();
                AccessibilityServer.UpdateSetDescription(controller, $"step-{index}");
            });
            for (var index = 0; index < steps.Length; index++)
            {
                AccessibilityServer.UpdateAddCustomAction(controller, index, $"step-{index}");
            }

            var start = new ProcessStartInfo("python3")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            start.ArgumentList.Add("-B");
            start.ArgumentList.Add(Path.GetFullPath("tests/AccessibilityClient/braille.py"));
            var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot launch the AT-SPI client.");
            client = process;
            output = process.StandardOutput.ReadToEndAsync();
            error = process.StandardError.ReadToEndAsync();
        };

        var started = Stopwatch.GetTimestamp();
        try
        {
            var exit = Engine.Run((_, _) =>
            {
                if (client is { HasExited: true })
                {
                    Console.Write(output!.GetAwaiter().GetResult());
                    Console.Error.Write(error!.GetAwaiter().GetResult());
                    Check(client.ExitCode == 0 && complete, "The independent AT-SPI client must complete all steps.");
                    window.Tree!.Quit();
                }
                if (Stopwatch.GetElapsedTime(started).TotalSeconds > 45)
                {
                    throw new TimeoutException("Braille AT-SPI client timeout.");
                }
            }, window);
            Check(exit == 0, "The braille producer must close successfully.");
        }
        finally
        {
            if (client is { HasExited: false })
            {
                client.Kill(entireProcessTree: true);
            }
            client?.Dispose();
        }
    }

    private static void RejectMissingRID(RID id)
    {
        Reject<KeyNotFoundException>(() => AccessibilityServer.UpdateSetBrailleLabel(id, "unused"));
        Reject<KeyNotFoundException>(() => AccessibilityServer.UpdateSetBrailleRoleDescription(id, "unused"));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
