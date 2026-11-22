using Android.Content;
using Android.OS.Storage;
using Android.Util;
using Electron2D;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Electron2DAndroidTests;

internal static class DriveMenuChecks
{
    internal static void Run(Context context, string run)
    {
        using var appDirectory = context.GetExternalFilesDir(null)
            ?? throw new InvalidOperationException("The primary app directory is unavailable.");
        var root = new SubViewport { Size = new(1100, 720), GUIEmbedSubwindows = true };
        var dialog = new FileDialog
        {
            Access = FileDialogAccess.FileSystem,
            CurrentDir = appDirectory.AbsolutePath,
            FileMode = FileDialogMode.OpenDirectory,
            GUIEmbedSubwindows = true
        };
        root.AddChild(dialog);
        using var tree = new SceneTree(root);
        tree.ProcessFrame(.01);
        dialog.PopupFileDialog();
        tree.ProcessFrame(.01);
        var button = (MenuButton)dialog.GetVBox().GetNode("Toolbar/Drives");
        var popup = button.GetPopup();
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Require(!button.Visible && popup.ItemCount == 0, "Drive paths are unavailable before API 30.");
            Log.Info("Electron2DTests", $"DRIVE_MENU {run} unsupported API menu hidden");
            return;
        }

        Require(button.Visible, "The unrestricted filesystem browser shows Drives.");
        dialog.Hide();
        dialog.RootSubfolder = appDirectory.AbsolutePath;
        Require(!button.Visible, "A rooted browser hides Drives.");
        dialog.RootSubfolder = "";
        Directory.CreateDirectory(ProjectSettings.UserDataRoot);
        dialog.Access = FileDialogAccess.UserData;
        Require(!button.Visible, "The user data browser hides Drives.");
        dialog.Access = FileDialogAccess.Resources;
        Require(!button.Visible, "The resource browser hides Drives.");
        dialog.Access = FileDialogAccess.FileSystem;
        dialog.CurrentDir = appDirectory.AbsolutePath;
        dialog.PopupFileDialog();
        tree.ProcessFrame(.01);
        CheckVolumeChanges(context, run, dialog, button);
    }

    [SupportedOSPlatform("android30.0")]
    private static void CheckVolumeChanges(Context context, string run, FileDialog dialog, MenuButton button)
    {
        var storage = (StorageManager)context.GetSystemService(Context.StorageService)!;
        var popup = button.GetPopup();
        var initialDirectory = dialog.CurrentDir;
        var initialRoots = OpenMenu(button);
        Require(initialRoots.Length == 1, "Start the drive-menu test with only the primary volume.");
        popup.Hide();

        Log.Info("Electron2DTests", $"DRIVE_MENU {run} mount requested");
        WaitForDisk(storage, mounted: true);
        var mountedRoots = OpenMenu(button);
        Require(mountedRoots.Length == 2 && mountedRoots.Contains(initialRoots[0]),
            "Reopening the same menu must add the mounted disk.");
        var addedRoot = mountedRoots.Single(path => path != initialRoots[0]);
        Activate(popup, Array.IndexOf(mountedRoots, addedRoot));
        Require(dialog.CurrentDir == addedRoot, "Selecting the mounted row must use its displayed path.");
        dialog.CurrentDir = initialDirectory;

        var openRoots = OpenMenu(button);
        var staleIndex = Array.IndexOf(openRoots, addedRoot);
        Require(staleIndex >= 0, "The additional volume must still be visible before unmounting.");
        Log.Info("Electron2DTests", $"DRIVE_MENU {run} unmount requested");
        WaitForDisk(storage, mounted: false);
        Require(popup.GetItemText(staleIndex) == addedRoot, "The open menu retains the displayed path.");
        Activate(popup, staleIndex);
        Require(dialog.CurrentDir == initialDirectory, "Selecting an unmounted row must preserve the current directory.");
        var error = dialog.GetNode<AcceptDialog>("_file_error");
        Require(error.Visible && error.DialogText.Contains(addedRoot, StringComparison.Ordinal),
            "The navigation error must identify the displayed path of the unmounted volume.");
        error.Hide();

        var remainingRoots = OpenMenu(button);
        Require(remainingRoots.SequenceEqual(initialRoots), "Reopening must remove the unmounted volume.");
        Activate(popup, 0);
        Require(dialog.CurrentDir == remainingRoots[0], "The remaining row navigates to its displayed path.");
        Log.Info("Electron2DTests", $"DRIVE_MENU {run} 1 -> 2 -> 1; navigation and stale row verified");
    }

    private static string[] OpenMenu(MenuButton button)
    {
        button.ShowPopup();
        var popup = button.GetPopup();
        Require(popup.Visible, "The drive popup must open in the embedded scene.");
        return Enumerable.Range(0, popup.ItemCount).Select(popup.GetItemText).ToArray();
    }

    private static void Activate(Electron2D.PopupMenu popup, int index)
    {
        popup.SetItemAccelerator(index, Key.F8);
        using var key = new InputEventKey { Keycode = Key.F8, Pressed = true };
        Require(popup.ActivateItemByEvent(key), "The drive row must handle activation.");
    }

    [SupportedOSPlatform("android30.0")]
    private static void WaitForDisk(StorageManager storage, bool mounted)
    {
        var wait = Stopwatch.StartNew();
        while (wait.Elapsed < TimeSpan.FromSeconds(90))
        {
            if (storage.StorageVolumes.Any(volume => !volume.IsPrimary &&
                volume.State == (mounted ? "mounted" : "unmounted")))
            {
                return;
            }
            Thread.Sleep(200);
        }
        throw new TimeoutException($"The virtual disk did not reach the requested mount state ({mounted}).");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
