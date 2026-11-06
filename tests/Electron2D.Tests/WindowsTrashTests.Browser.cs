using Electron2D;
using IOPath = System.IO.Path;

internal static partial class WindowsTrashTests
{
    private static void ActionKey(Viewport root, string action)
    { using var down = new InputEventAction { Action = action, Pressed = true }; using var up = new InputEventAction { Action = action }; root.PushInput(down, true); root.PushInput(up, true); }
    private static void RequestTrash(Viewport root, FileDialog dialog)
    { dialog.GetOKButton().GrabFocus(); using var key = new InputEventKey { Keycode = Key.Delete, Pressed = true }; root.PushInput(key, true); }
    private static void Browser()
    {
        string good = FileAt("browser/01-good.txt", "browser bytes"), bad = FileAt("browser/02-locked.txt", "locked browser bytes");
        Attempted.Add(good); Attempted.Add(bad);
        var root = new SubViewport { Size = new(1100, 720), GUIEmbedSubwindows = true };
        var dialog = new FileDialog { Access = FileDialogAccess.FileSystem, CurrentDir = IOPath.GetDirectoryName(good)!, FileMode = FileDialogMode.OpenFiles }; root.AddChild(dialog);
        using var tree = new SceneTree(root); tree.ProcessFrame(.01); dialog.PopupFileDialog(); tree.ProcessFrame(.01);
        var list = (ItemList)dialog.GetVBox().GetNode("Browser/Files");
        var first = Enumerable.Range(0, list.ItemCount).First(i => list.GetItemText(i) == "01-good.txt"); list.Select(first);
        RequestTrash(root, dialog); var confirmation = (ConfirmationDialog)dialog.GetNode("_trash"); Check(confirmation.Visible && File.Exists(good), "The browser requires confirmation before native recycling.");
        ActionKey(root, "ui_close_dialog"); tree.ProcessFrame(.01); Check(File.Exists(good) && Oracle("inspect", good).Length == 0, "Canceled browser trash preserves the original and creates no native entry.");
        var second = Enumerable.Range(0, list.ItemCount).First(i => list.GetItemText(i) == "02-locked.txt"); list.Select(first); list.Select(second, single: false);
        using (var locked = File.Open(bad, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None))
        {
            RequestTrash(root, dialog); confirmation.GetOKButton().GrabFocus(); ActionKey(root, "ui_accept"); tree.ProcessFrame(.01);
            Check(!File.Exists(good) && File.Exists(bad), "Confirmed multi-selection recycles the first item and preserves a later sharing failure.");
            Check(!Enumerable.Range(0, list.ItemCount).Any(i => list.GetItemText(i) == "01-good.txt") && Enumerable.Range(0, list.ItemCount).Any(i => list.GetItemText(i) == "02-locked.txt"), "The browser refreshes committed changes after a later failure.");
            Check(((AcceptDialog)dialog.GetNode("_file_error")).Visible, "A failed native operation reaches the existing error dialog.");
        }
        Restore(good, Oracle("inspect", good).Single()); Check(File.ReadAllText(good) == "browser bytes" && File.ReadAllText(bad) == "locked browser bytes", "Both browser entries retain their data after native restoration and failure.");
        dialog.Hide();
    }
}
