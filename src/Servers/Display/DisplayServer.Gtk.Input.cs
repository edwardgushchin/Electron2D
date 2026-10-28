using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private bool _nativeTextInputActive;
    private bool CanUseGTKInput()
    {
        if (!OperatingSystem.IsLinux() || _videoDriver is not ("wayland" or "x11")) return false;
        try
        {
            if (GTKCheckVersion(3, 22, 0) != 0 || GTKInitCheck(0, 0) == 0) return false;
            var display = GDKDisplayGetDefault();
            return display != 0 && GTypeCheckInstanceIsA(display, _videoDriver == "wayland" ? GDKWaylandDisplayGetType() : GDKX11DisplayGetType()) != 0;
        }
        catch (DllNotFoundException) { return false; }
        catch (EntryPointNotFoundException) { return false; }
    }

    internal void DialogInputTextCore(string title, string description, string existingText, Action<string> callback)
    {
        EnsureOwner(); OS.ValidateNativeMessage(title, nameof(title)); OS.ValidateNativeMessage(description, nameof(description)); OS.ValidateNativeMessage(existingText, nameof(existingText)); ArgumentNullException.ThrowIfNull(callback);
        if (_nativeTextInputActive) throw new InvalidOperationException("A native text dialog is already active.");
        if (!CanUseGTKInput()) throw new NotSupportedException("Native text input requires a matching GTK 3.22 desktop backend.");
        var parent = GetWindow(MainWindowId); string? result;
        _nativeTextInputActive = true; Interlocked.Increment(ref _pendingNativeDialogs); Interlocked.Increment(ref _pendingOwnedNativeDialogs);
        try { result = ShowGTKInput(parent, title, description, existingText); }
        finally { Interlocked.Decrement(ref _pendingOwnedNativeDialogs); Interlocked.Decrement(ref _pendingNativeDialogs); _nativeTextInputActive = false; }
        if (result != null) callback(result);
    }

    private string? ShowGTKInput(nint parent, string title, string description, string existingText)
    {
        var dialog = GTKDialogNew(); if (dialog == 0) throw new InvalidOperationException("The native text dialog could not be created.");
        GObjectRef(dialog); nint foreign = 0;
        try
        {
            GTKWindowSetTitle(dialog, title); GTKWindowSetModal(dialog, 1); GTKWindowSetDefaultSize(dialog, 400, -1); GTKWindowSetResizable(dialog, 0);
            var box = GTKDialogGetContentArea(dialog); GTKContainerSetBorderWidth(box, 12); GTKBoxSetSpacing(box, 8);
            var label = GTKLabelNew(description); GTKLabelSetLineWrap(label, 1); GTKLabelSetXAlign(label, 0); GTKContainerAdd(box, label);
            var entry = GTKEntryNew(); GTKContainerAdd(box, entry); GTKLabelSetMnemonicWidget(label, entry); GTKEntrySetText(entry, existingText);
            if (ReadGTKEntry(entry) != existingText) throw new ArgumentException("The native entry cannot represent the initial text without truncation or replacement.", nameof(existingText));
            GTKEntrySetActivatesDefault(entry, 1);
            GTKDialogAddButton(dialog, Marshal.PtrToStringUTF8(GDGettext("gtk30", "_Cancel")) ?? "_Cancel", -6);
            GTKDialogAddButton(dialog, Marshal.PtrToStringUTF8(GDGettext("gtk30", "_OK")) ?? "_OK", -5); GTKDialogSetDefaultResponse(dialog, -5);
            GTKWidgetRealize(dialog); var native = GTKWidgetGetWindow(dialog); var properties = SDL.GetWindowProperties(parent);
            if (_videoDriver == "wayland")
            {
                var exported = SDL.GetStringProperty(properties, SDL.Props.WindowWaylandXDGToplevelExportHandleString, "");
                if (exported.Length != 0) _ = GDKWaylandWindowSetTransientForExported(native, exported);
            }
            else
            {
                var xid = SDL.GetNumberProperty(properties, SDL.Props.WindowX11WindowNumber, 0);
                if (xid != 0) { foreign = GDKX11WindowForeignNewForDisplay(GDKDisplayGetDefault(), checked((nuint)xid)); if (foreign != 0) GDKWindowSetTransientFor(native, foreign); }
            }
            GTKWidgetShowAll(dialog); GTKWidgetGrabFocus(entry); GTKEditableSelectRegion(entry, 0, -1);
            return GTKDialogRun(dialog) == -5 ? ReadGTKEntry(entry) : null;
        }
        finally { GTKWidgetDestroy(dialog); GObjectUnref(dialog); if (foreign != 0) GObjectUnref(foreign); }
    }
    private static string ReadGTKEntry(nint entry) => Marshal.PtrToStringUTF8(GTKEntryGetText(entry)) ?? throw new InvalidOperationException("The native entry returned no text.");

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_check_version")] private static partial nint GTKCheckVersion(uint major, uint minor, uint micro);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_dialog_new")] private static partial nint GTKDialogNew();
    [LibraryImport("libgobject-2.0.so.0", EntryPoint = "g_object_ref")] private static partial nint GObjectRef(nint value);
    [LibraryImport("libgobject-2.0.so.0", EntryPoint = "g_type_check_instance_is_a")] private static partial int GTypeCheckInstanceIsA(nint value, nuint type);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_window_set_title", StringMarshalling = StringMarshalling.Utf8)] private static partial void GTKWindowSetTitle(nint window, string title);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_window_set_modal")] private static partial void GTKWindowSetModal(nint window, int modal);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_window_set_default_size")] private static partial void GTKWindowSetDefaultSize(nint window, int width, int height);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_window_set_resizable")] private static partial void GTKWindowSetResizable(nint window, int resizable);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_dialog_get_content_area")] private static partial nint GTKDialogGetContentArea(nint dialog);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_container_set_border_width")] private static partial void GTKContainerSetBorderWidth(nint container, uint border);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_box_set_spacing")] private static partial void GTKBoxSetSpacing(nint box, int spacing);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_label_new", StringMarshalling = StringMarshalling.Utf8)] private static partial nint GTKLabelNew(string text);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_label_set_line_wrap")] private static partial void GTKLabelSetLineWrap(nint label, int wrap);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_label_set_xalign")] private static partial void GTKLabelSetXAlign(nint label, float alignment);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_container_add")] private static partial void GTKContainerAdd(nint container, nint child);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_label_set_mnemonic_widget")] private static partial void GTKLabelSetMnemonicWidget(nint label, nint widget);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_entry_new")] private static partial nint GTKEntryNew();
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_entry_set_text", StringMarshalling = StringMarshalling.Utf8)] private static partial void GTKEntrySetText(nint entry, string text);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_entry_get_text")] private static partial nint GTKEntryGetText(nint entry);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_entry_set_activates_default")] private static partial void GTKEntrySetActivatesDefault(nint entry, int activates);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_dialog_add_button", StringMarshalling = StringMarshalling.Utf8)] private static partial nint GTKDialogAddButton(nint dialog, string text, int response);
    [LibraryImport("libglib-2.0.so.0", EntryPoint = "g_dgettext", StringMarshalling = StringMarshalling.Utf8)] private static partial nint GDGettext(string domain, string text);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_dialog_set_default_response")] private static partial void GTKDialogSetDefaultResponse(nint dialog, int response);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_widget_realize")] private static partial void GTKWidgetRealize(nint widget);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_widget_get_window")] private static partial nint GTKWidgetGetWindow(nint widget);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_widget_show_all")] private static partial void GTKWidgetShowAll(nint widget);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_widget_grab_focus")] private static partial void GTKWidgetGrabFocus(nint widget);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_editable_select_region")] private static partial void GTKEditableSelectRegion(nint editable, int start, int end);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_dialog_run")] private static partial int GTKDialogRun(nint dialog);
    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_widget_destroy")] private static partial void GTKWidgetDestroy(nint widget);
    [LibraryImport("libgdk-3.so.0", EntryPoint = "gdk_display_get_default")] private static partial nint GDKDisplayGetDefault();
    [LibraryImport("libgdk-3.so.0", EntryPoint = "gdk_wayland_display_get_type")] private static partial nuint GDKWaylandDisplayGetType();
    [LibraryImport("libgdk-3.so.0", EntryPoint = "gdk_x11_display_get_type")] private static partial nuint GDKX11DisplayGetType();
    [LibraryImport("libgdk-3.so.0", EntryPoint = "gdk_wayland_window_set_transient_for_exported", StringMarshalling = StringMarshalling.Utf8)] private static partial int GDKWaylandWindowSetTransientForExported(nint window, string parent);
    [LibraryImport("libgdk-3.so.0", EntryPoint = "gdk_x11_window_foreign_new_for_display")] private static partial nint GDKX11WindowForeignNewForDisplay(nint display, nuint window);
    [LibraryImport("libgdk-3.so.0", EntryPoint = "gdk_window_set_transient_for")] private static partial void GDKWindowSetTransientFor(nint window, nint parent);
}
