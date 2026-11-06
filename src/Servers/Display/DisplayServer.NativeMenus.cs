namespace Electron2D;

public sealed partial class DisplayServer
{
    private NativeMenu? _nativeMenus;
    private bool _nativeMenuTracking;
    internal bool SupportsNativeMenus => OperatingSystem.IsWindows() && _videoDriver == "windows";
    internal NativeMenu GetNativeMenus() { EnsureOwner(); if (_retiringNativeWindow) throw new InvalidOperationException("The native display is retiring."); return _nativeMenus ??= NativeMenu.CreateOwner(this); }
    internal void EnsureNativeMenuOwner() => EnsureOwner();
    internal nint BeginNativeMenu(DisplayServer owner)
    {
        EnsureOwner();
        owner.EnsureOwner();
        if (_nativeMenuTracking || _nativeTextInputActive || HasPendingNativeDialogs || owner.HasPendingNativeDialogs) throw new InvalidOperationException("A native modal presentation is already active.");
        var window = owner.WindowGetNativeHandleCore(HandleType.WindowHandle);
        if (owner != this) { owner._nativeMenuTracking = true; Interlocked.Increment(ref owner._pendingOwnedNativeDialogs); }
        _nativeMenuTracking = true; Interlocked.Increment(ref _pendingNativeDialogs); Interlocked.Increment(ref _pendingOwnedNativeDialogs); return window;
    }
    internal void EndNativeMenu(DisplayServer owner)
    { if (owner != this) { owner._nativeMenuTracking = false; Interlocked.Decrement(ref owner._pendingOwnedNativeDialogs); } _nativeMenuTracking = false; Interlocked.Decrement(ref _pendingOwnedNativeDialogs); Interlocked.Decrement(ref _pendingNativeDialogs); }
}
