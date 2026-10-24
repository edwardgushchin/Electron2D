using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private DisplayWindowEvents? _windowEvents;
    private DisplayWindowEvents[] _windowInputSources = [];
    internal bool WindowEventsAvailable => !IsDisposed && !_retiringNativeWindow && !(_rootDisplay ?? this)._retiringNativeWindow;

    /// <summary>Gets the live typed event publisher for one native window.</summary>
    /// <param name="windowID">An active native window ID; zero selects the main window.</param>
    /// <returns>The same borrowed live publisher until it is disposed or the native window retires.</returns>
    /// <remarks>Publisher disposal clears subscriptions without closing the window. A later query creates
    /// a fresh publisher. Hidden windows retain a publisher only while their native identity remains live.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The native window ID is absent.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable, retiring or accessed off-owner.</exception>
    public static DisplayWindowEvents GetWindowEvents(int windowID = 0)
    {
        var context = RequireService().ResolveWindow(windowID); context.ValidateWindowEventAccess();
        return context._windowEvents is { Available: true } live ? live : context._windowEvents = new(context, context.WindowID);
    }

    internal void ValidateWindowEventOwner() => EnsureOwner();
    internal void ValidateWindowEventAccess()
    {
        EnsureOwner();
        if (_retiringNativeWindow || (_rootDisplay ?? this)._retiringNativeWindow)
            throw new InvalidOperationException("The native window is retiring.");
    }
    internal void RefreshWindowInputSources()
    {
        var root = _rootDisplay ?? this;
        var sources = new List<DisplayWindowEvents>();
        if (root._windowEvents is { HasInput: true } main) sources.Add(main);
        foreach (var context in root._childWindows.OrderBy(p => p.Key).Select(p => p.Value))
            if (context._windowEvents is { HasInput: true } source) sources.Add(source);
        Volatile.Write(ref root._windowInputSources, sources.ToArray());
    }
    internal static DisplayWindowEvents[] CaptureWindowInputSources()
    {
        var root = Service;
        if (root is null || root._retiringNativeWindow) return [];
        var sources = Volatile.Read(ref root._windowInputSources);
        if (sources.Length != 0) root.EnsureOwner();
        return sources;
    }
    internal static void EmitWindowInput(DisplayWindowEvents[] sources, InputEvent value)
    {
        var windowID = value is InputEventFromWindow tagged ? tagged.WindowID : (long?)null;
        List<Exception>? failures = null;
        foreach (var source in sources)
        {
            if (!source.Available || windowID is { } id && id != source.WindowID) continue;
            try { source.EmitInput(value); } catch (Exception error) { (failures ??= []).Add(error); }
        }
        if (failures is not null) throw new AggregateException("Native window input callbacks failed.", failures);
    }
    private Exception? RetireWindowEvents()
    {
        var source = _windowEvents; _windowEvents = null;
        try { source?.Dispose(); return null; } catch (Exception error) { return error; }
    }

    private void ForceClosePopup()
    {
        Exception? failure = null;
        try { _windowEvents?.EmitForceClose(); } catch (Exception error) { failure = error; }
        try
        {
            if (!IsDisposed)
            {
                if (SceneWindow is { IsDisposed: false } scene) scene.Hide();
                else Dispose();
            }
        }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void PublishWindow(Action? local, Action? scoped)
    {
        var publisher = _windowEvents;
        Exception? failure = null;
        try { local?.Invoke(); } catch (Exception error) { failure = error; }
        try { if (publisher is { Available: true }) scoped?.Invoke(); }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private void PublishWindow<T>(Action<T>? local, Action<T>? scoped, T value)
    {
        var publisher = _windowEvents;
        Exception? failure = null;
        try { local?.Invoke(value); } catch (Exception error) { failure = error; }
        try { if (publisher is { Available: true }) scoped?.Invoke(value); }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private void PublishWindow<T1, T2>(Action<T1, T2>? local, Action<T1, T2>? scoped, T1 first, T2 second)
    {
        var publisher = _windowEvents;
        Exception? failure = null;
        try { local?.Invoke(first, second); } catch (Exception error) { failure = error; }
        try { if (publisher is { Available: true }) scoped?.Invoke(first, second); }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
