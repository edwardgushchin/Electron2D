namespace Electron2D;

public sealed partial class DisplayServer
{
    private bool _nativeTextInputActive;
    private bool UsesWindowsInput => OperatingSystem.IsWindows() && _videoDriver == "windows";
    private bool CanUseNativeInput() => UsesWindowsInput || CanUseGTKInput();

    internal void DialogInputTextCore(string title, string description, string existingText, Action<string> callback)
    {
        EnsureOwner();
        OS.ValidateNativeMessage(title, nameof(title));
        OS.ValidateNativeMessage(description, nameof(description));
        OS.ValidateNativeMessage(existingText, nameof(existingText));
        ArgumentNullException.ThrowIfNull(callback);
        var windows = UsesWindowsInput;
        if (_nativeTextInputActive || _nativeMenuTracking || (windows && HasPendingNativeDialogs))
            throw new InvalidOperationException("A native modal presentation is already active.");
        if (!windows && !CanUseGTKInput())
            throw new NotSupportedException("Native text input requires a Windows or matching GTK 3.22 desktop backend.");

        _nativeTextInputActive = true;
        Interlocked.Increment(ref _pendingNativeDialogs);
        Interlocked.Increment(ref _pendingOwnedNativeDialogs);
        string? result;
        try
        {
            result = windows
                ? WindowsTextInput.Show(WindowGetNativeHandleCore(HandleType.WindowHandle), title, description, existingText)
                : ShowGTKInput(GetWindow(MainWindowId), title, description, existingText);
        }
        finally
        {
            Interlocked.Decrement(ref _pendingOwnedNativeDialogs);
            Interlocked.Decrement(ref _pendingNativeDialogs);
            _nativeTextInputActive = false;
        }
        if (result != null)
            callback(result);
    }
}
