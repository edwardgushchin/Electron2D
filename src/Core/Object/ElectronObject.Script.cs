namespace Electron2D;

public partial class ElectronObject
{
    private Script? _constructionScript;
    private ScriptRegistration? _scriptBinding;
    private int _scriptLifetimeOperation;
    [ThreadStatic] private static ElectronObject? _scriptCallbackOwner;
    internal Action<int, bool>? BoundScriptNotification;
    internal bool HasLiveScriptBinding => _scriptBinding is not null;

    /// <summary>Installs, replaces or detaches a registered live typed script binding on this existing owner.</summary>
    /// <param name="script">Borrowed live-binding asset, or null to detach.</param>
    /// <remarks>Prevalidation retains the prior binding on failure. After retirement starts, a failed clear/bind leaves
    /// the owner unbound, attempts partial-installation cleanup and reports the committed source change before throwing.
    /// PropertyListChanged precedes ScriptChanged. Observers see committed state and may start another transition.
    /// Reentrant binding/disposal is rejected. Functions execute through the owner's existing typed callbacks.
    /// A fixed compiled CLR source class cannot be replaced by this operation.</remarks>
    /// <exception cref="ArgumentException">The binding's owner role is incompatible.</exception>
    /// <exception cref="NotSupportedException">The source is not a live binding or this owner has a fixed CLR source.</exception>
    /// <exception cref="InvalidOperationException">A lifetime transition is already active, or the installed schema is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This owner or the new asset is disposed.</exception>
    /// <exception cref="Exception">Binding, cleanup or a source-change observer fails.</exception>
    public void SetScript(Script? script)
    {
        ThrowIfDisposed(); ValidateMutation();
        List<Exception>? errors = null;
        using (BeginScriptLifetimeOperation())
        {
            ThrowIfDisposed();
            if (ReferenceEquals(_constructionScript, script)) return;
            var registration = script?.GetBindingRegistration(requireLive: true);
            if (script is not null && registration is null) throw new NotSupportedException("The source does not declare a live typed binding.");
            if (registration is not null && this is Resource) throw new NotSupportedException("Live Resource binding requires its separate copy/cache lifetime contract.");
            if (registration is not null && !registration.InstanceType.IsInstanceOfType(this)) throw new ArgumentException("The binding requires another owner role.", nameof(script));
            if (_constructionScript is not null && _scriptBinding is null) throw new NotSupportedException("Replacing a fixed CLR source class requires a restart.");
            var retirement = RetireScriptBinding();
            if (retirement is not null) (errors ??= []).Add(retirement);
            if (errors is null && registration is not null)
            {
                _scriptBinding = registration; Volatile.Write(ref _constructionScript, script);
                try { InvokeScriptBind(registration); Script.ValidateSchema(registration, this); }
                catch (Exception error)
                {
                    (errors ??= []).Add(error);
                    var cleanup = RetireScriptBinding(); if (cleanup is not null) errors.Add(cleanup);
                }
            }
        }
        try { NotifyConstructionScriptChanged(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        ThrowScriptErrors(errors);
    }

    internal void InitializeScriptBinding()
    {
        if (_scriptBinding is not null || _constructionScript?.GetBindingRegistration(requireLive: false) is not { } registration) return;
        using var operation = BeginScriptLifetimeOperation();
        _scriptBinding = registration;
        try { InvokeScriptBind(registration); Script.ValidateSchema(registration, this); }
        catch (Exception error)
        {
            var cleanup = RetireScriptBinding();
            if (cleanup is not null) throw new AggregateException(error, cleanup);
            throw;
        }
    }

    private Exception? RetireScriptBinding()
    {
        var old = _scriptBinding; _scriptBinding = null;
        if (old is null) return null;
        Exception? error = null;
        try
        {
            try { old.Clear!(this); } catch (Exception failure) { error = failure; }
            var native = this is Node node ? node.RetireScriptCallbacks() : null;
            return error is null ? native : native is null ? error : new AggregateException(error, native);
        }
        finally { Volatile.Write(ref _constructionScript, null); }
    }

    private void InvokeScriptBind(ScriptRegistration registration)
    {
        var previous = _scriptCallbackOwner; _scriptCallbackOwner = this;
        try { registration.Bind!(this); }
        finally { _scriptCallbackOwner = previous; }
    }
    internal void EnsureScriptCallbackInstallation()
    {
        ThrowIfDisposed();
        if (!ReferenceEquals(_scriptCallbackOwner, this) || _scriptBinding is null || _scriptLifetimeOperation != 1)
            throw new InvalidOperationException("Script callbacks install only in this owner's synchronous Bind callback.");
    }

    private static void ThrowScriptErrors(List<Exception>? errors)
    {
        if (errors is { Count: > 1 }) throw new AggregateException(errors);
        if (errors is { Count: 1 }) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
    }

    private ScriptLifetimeOperation BeginScriptLifetimeOperation()
    {
        if (Interlocked.CompareExchange(ref _scriptLifetimeOperation, 1, 0) != 0) throw new InvalidOperationException("A script/owner lifetime transition is already active.");
        return new(this);
    }
    private bool TryBeginScriptDisposal(out ScriptLifetimeOperation operation)
    {
        var previous = Interlocked.CompareExchange(ref _scriptLifetimeOperation, 2, 0);
        if (previous == 1) throw new InvalidOperationException("A script binding transition is already active.");
        operation = previous == 0 ? new(this) : default;
        return previous == 0;
    }
    private readonly struct ScriptLifetimeOperation(ElectronObject? owner) : IDisposable
    { public void Dispose() { if (owner is not null) Volatile.Write(ref owner._scriptLifetimeOperation, 0); } }

    /// <summary>Gets the borrowed source asset selected by compiled construction or live typed binding.</summary>
    /// <returns>The exact source asset, or null when no association is installed.</returns>
    /// <remarks>An initial association is available during the derived constructor; live binding follows construction.
    /// Source text changes do not replace the association.
    /// Disposing the borrowed asset does not stop compiled callbacks; the returned identity may then be disposed.
    /// Copies and version-nine scene/resource archives retain the association through their resource ownership policy.
    /// Earlier archives retain null when no association was recorded. SetScript changes only live typed bindings.
    /// Prepared reads require no metadata discovery, native device, callback or managed allocation.</remarks>
    /// <exception cref="ObjectDisposedException">This owner is disposing on another thread or has finished disposing.</exception>
    public Script? GetScript() { ThrowIfDisposed(); return Volatile.Read(ref _constructionScript); }

    internal (Script Asset, Type Type)? ConstructionScript =>
        _constructionScript is { } script ? (script, GetType()) : null;

    internal bool RemapConstructionScript(Func<Resource, Resource> remap)
    {
        if (_constructionScript is { } script)
            return ReplaceConstructionScript(remap(script) as Script ?? throw new InvalidOperationException("A compiled source reference must remain a Script."));
        return false;
    }

    internal bool CopyConstructionScript(ElectronObject source) => ReplaceConstructionScript(source._constructionScript);

    private bool ReplaceConstructionScript(Script? source)
    {
        if (ReferenceEquals(_constructionScript, source)) return false;
        Volatile.Write(ref _constructionScript, source);
        return true;
    }

    internal void NotifyConstructionScriptChanged()
    {
        NotifyPropertyListChanged();
        if (!IsDisposed) NotifyScriptChanged();
    }
}
