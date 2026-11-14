using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static unsafe partial class WindowsSystemMenuTests
{
    private const string User32 = "user32.dll";
    private const string ComCtl32 = "comctl32.dll";
    private const uint SystemCharacter = 0x0106;
    private const uint EnterMenuLoop = 0x0211;
    private const uint CancelMode = 0x001F;
    private static int _opened;

    internal static void Run(string profile)
    {
        Check(!ProjectSettings.Get(ProjectSettings.EnableAltSpaceMenu), "The system menu shortcut defaults to disabled.");
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (profile == "dummy")
        {
            ProjectSettings.Set(ProjectSettings.EnableAltSpaceMenu, true);
            using var window = new Window();
            window.Ready += _ =>
            {
                Check(SDL3.SDL.GetCurrentVideoDriver() == "dummy", "The dummy display is active.");
                window.Tree!.Quit(12);
            };
            Check(Engine.Run(window) == 12, "A non-Win32 driver has no native system menu work.");
            return;
        }

        ProjectSettings.SetFeatureOverride(ProjectSettings.EnableAltSpaceMenu, "template", true);
        RunWindow(expectMenu: true, changeSetting: true);
        RunWindow(expectMenu: false, changeSetting: false);
        Engine.IsEditorHint = true;
        RunWindow(expectMenu: true, changeSetting: false);
        Engine.IsEditorHint = false;
        Console.WriteLine("Win32 Alt+Space: feature override, retained root/child policy, fresh run, editor hint and native menu loop passed.");
    }

    private static void RunWindow(bool expectMenu, bool changeSetting)
    {
        using var window = new Window { Size = new(240, 160), GUIEmbedSubwindows = false };
        var ran = false;
        window.Ready += _ =>
        {
            Check(SDL3.SDL.GetCurrentVideoDriver() == "windows", "The Win32 display is active.");
            var main = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
            VerifyMenu(main, expectMenu);
            if (changeSetting)
                Check(ProjectSettings.ClearFeatureOverride(ProjectSettings.EnableAltSpaceMenu, "template"), "Clear the effective setting during the run.");
            using var child = new Window { Size = new(180, 120) };
            window.AddChild(child);
            var childHandle = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, child.GetWindowID());
            VerifyMenu(childHandle, expectMenu);
            child.Hide();
            Check(IsWindow(childHandle) == 0, "Hiding the child retires its native window.");
            child.Show();
            VerifyMenu(DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, child.GetWindowID()), expectMenu);
            VerifyMenu(main, expectMenu);
            ran = true;
            window.Tree!.Quit(12);
        };
        Check(Engine.Run(window) == 12 && ran && !DisplayServer.IsAvailable, "Complete the owned native window run.");
    }

    private static void VerifyMenu(nint window, bool expectMenu)
    {
        _opened = 0;
        Check(GetSystemMenu(window, 0) != 0, "The window has a native system menu.");
        Check(SetWindowSubclass(window, &ObserveMenu, 1, 0) != 0, "Observe the native menu loop.");
        using var completed = new ManualResetEventSlim();
        var rescue = Task.Run(() =>
        {
            if (!completed.Wait(TimeSpan.FromSeconds(3))) PostMessage(window, CancelMode, 0, 0);
        });
        try
        {
            SetActiveWindow(window);
            SendMessage(window, SystemCharacter, ' ', 0x20390001);
            Check(_opened == (expectMenu ? 1 : 0), $"Expected native menu activation={expectMenu}, observed {_opened}.");
            SendMessage(window, SystemCharacter, 'X', 0x202D0001);
            Check(_opened == (expectMenu ? 1 : 0), "Unrelated system characters do not open the system menu.");
        }
        finally
        {
            completed.Set();
            rescue.GetAwaiter().GetResult();
            RemoveWindowSubclass(window, &ObserveMenu, 1);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint ObserveMenu(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        if (message == EnterMenuLoop)
        {
            _opened++;
            PostMessage(window, CancelMode, 0, 0);
        }
        return DefSubclassProc(window, message, wParam, lParam);
    }

    [LibraryImport(User32)] private static partial nint GetSystemMenu(nint window, int revert);
    [LibraryImport(User32)] private static partial nint SetActiveWindow(nint window);
    [LibraryImport(User32)] private static partial int IsWindow(nint window);
    [LibraryImport(User32, EntryPoint = "SendMessageW")] private static partial nint SendMessage(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport(User32, EntryPoint = "PostMessageW")] private static partial int PostMessage(nint window, uint message, nuint wParam, nint lParam);
    [LibraryImport(ComCtl32)] private static partial int SetWindowSubclass(nint window, delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> callback, nuint id, nuint data);
    [LibraryImport(ComCtl32)] private static partial int RemoveWindowSubclass(nint window, delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> callback, nuint id);
    [LibraryImport(ComCtl32)] private static partial nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam);
}
