namespace Electron2D;

public abstract partial class TextServer
{
    [ThreadStatic] private static int _nativeBackendDepth;
    private readonly object _implementationGate = new();
    private int _activeImplementationCalls, _activeConsumers, _unregisterThread;
    internal static bool IsNativeBackendCall => _nativeBackendDepth != 0;
    internal bool UsesCallbackConsumers => this is TextServerExtension and not (TextServerAdvanced or TextServerFallback);
    private ImplementationCall EnterImplementation(RID source = default, bool? font = null, bool mutation = false)
    {
        if (this is TextServerDummy)
        {
            lock (_implementationGate)
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                if (_unregisterThread != 0 && _unregisterThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("The text implementation is unregistering.");
                _activeImplementationCalls++; return new(this, null, default, null, null, false, false);
            }
        }
        TextLine? line = null; TextParagraph? paragraph = null; var nativeAlias = false; var nativeBuffer = false;
        if (font is not null && TextBuffers.TryResolve(source, out var buffer))
        {
            nativeBuffer = true;
            if (font == false) { line = buffer.Owner as TextLine; paragraph = buffer.Owner as TextParagraph; }
            nativeAlias = this is TextServerExtension && (paragraph is not null || line is not null && (!ReferenceEquals(line.ServerInterfaceOwner, this) || !line.ServerNativeBackend));
        }
        lock (_implementationGate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (_unregisterThread != 0 && _unregisterThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("The text implementation is unregistering.");
            ExternalEntry? entry = null;
            if (source != default && _external.TryGetValue(source, out entry))
            {
                if (font is { } expected && entry.Font != expected) throw new ArgumentException("The external identity has another resource role.", nameof(source));
                CheckExternalOwner(entry);
                if (entry.RetireWhenIdle && !Font.RetainsCallbackSource(this, source)) throw new Font.SourceChangedDuringRead();
                if (entry.Retiring || entry.Projection is { IsDisposalCommitted: true } || mutation && entry.Readers != 0) throw new InvalidOperationException("An active external source cannot mutate or retire.");
            }
            else if (source != default && ExternalOwners.TryGetValue(source, out var other) && other.TryGetTarget(out var owner) && !ReferenceEquals(owner, this))
                throw new InvalidOperationException("The external source belongs to another text implementation.");
            if (font is { } expectedRole && entry is null)
            {
                var nativeFontSource = HasNativeFont(source) || FontFile.HasSourceIdentity(source);
                if (expectedRole ? nativeBuffer : nativeFontSource) throw new ArgumentException("The identity has another text resource role.", nameof(source));
                if (expectedRole ? !nativeFontSource : !nativeBuffer) throw new InvalidOperationException("The text resource identity is absent or retired.");
            }
            if (mutation) { line?.AcquireServerMutation(); paragraph?.AcquireServerMutation(); }
            else { line?.AcquireServerRead(); paragraph?.AcquireServerRead(); }
            var nativeFont = font == true ? new NativeFontRead(source) : default;
            if (entry is not null) entry.Readers++;
            _activeImplementationCalls++; return new(this, entry, nativeFont, line, paragraph, mutation, nativeAlias);
        }
    }
    private readonly struct ImplementationCall(TextServer owner, ExternalEntry? source, NativeFontRead nativeFont, TextLine? line, TextParagraph? paragraph, bool mutation, bool nativeAlias) : IDisposable
    {
        internal TextServer Dispatcher => nativeAlias ? TextServerManager.NativeInterface : owner;
        internal void Dispose(Exception? failure)
        {
            try { Dispose(); }
            catch (Exception cleanup) { if (failure is not null) throw new AggregateException(failure, cleanup); throw; }
        }
        public void Dispose()
        {
            nativeFont.Dispose();
            ProviderFont? changed = null; RID retire = default;
            lock (owner._implementationGate)
            {
                if (mutation) { line?.ReleaseServerMutation(); paragraph?.ReleaseServerMutation(); }
                else { line?.ReleaseServerRead(); paragraph?.ReleaseServerRead(); }
                if (source is not null) { source.Readers--; if (mutation) { source.Generation++; changed = source.Projection; } if (source.Readers == 0 && source.RetireWhenIdle && !source.Retiring) retire = source.Identity; }
                owner._activeImplementationCalls--;
            }
            List<Exception>? errors = null;
            try { if (changed is { IsDisposed: false }) changed.InvalidateFont(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            try { if (retire != default) owner.DisposeResourceFont(retire); } catch (Exception error) { Node.CollectException(ref errors, error); }
            if (errors is { Count: 1 }) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
            Node.ThrowCollected("Text implementation admission cleanup failed.", errors);
        }
    }
    private NativeBackendCall EnterNativeBackend(RID shaped = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (shaped != default && TextBuffers.TryResolve(shaped, out var buffer) &&
            (buffer.Owner is TextParagraph || buffer.Owner is TextLine line && !line.ServerNativeBackend)) return default;
        _nativeBackendDepth++; return new(true);
    }
    private readonly struct NativeBackendCall(bool active) : IDisposable
    { public void Dispose() { if (active) _nativeBackendDepth--; } }
    internal void AcquireConsumerAdmission()
    { lock (_implementationGate) { EnsureRegistryAvailable(); _activeConsumers++; } }
    internal void ReleaseConsumerAdmission()
    { lock (_implementationGate) _activeConsumers--; }
    internal void ValidateProviderDisposal()
    {
        if (TextServerManager.IsRetained(this)) throw new InvalidOperationException("A selected or registered text implementation cannot be disposed.");
        lock (_implementationGate)
            if (_unregisterThread != 0 || _activeImplementationCalls != 0 || _activeConsumers != 0 || _external.Count != 0) throw new InvalidOperationException("An active text implementation cannot be disposed.");
        lock (_fontsGate) if (_fonts.Count != 0) throw new InvalidOperationException("Release owned fonts before disposing the implementation.");
        lock (_buffersGate) if (_owned.Count != 0) throw new InvalidOperationException("Release owned text buffers before disposing the implementation.");
    }
    internal void EnsureRegistryAvailable()
    {
        lock (_implementationGate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (_unregisterThread != 0) throw new InvalidOperationException("The text implementation is unregistering.");
        }
    }
    internal void BeginUnregister()
    {
        lock (_implementationGate)
        {
            EnsureRegistryAvailable();
            if (_activeImplementationCalls != 0 || _activeConsumers != 0) throw new InvalidOperationException("An active text implementation cannot unregister.");
            _unregisterThread = Environment.CurrentManagedThreadId;
        }
    }
    internal void CleanupBeforeUnregister()
    {
        using var call = EnterImplementation(); List<Exception>? errors = null;
        try { CanvasRasterScale.ForgetProvider(this); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { OnCleanup(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Text implementation cleanup failed.", errors);
    }
    internal void EndUnregister() { lock (_implementationGate) _unregisterThread = 0; }
    /// <summary>Releases implementation caches immediately before registry removal.</summary>
    /// <remarks>Runs once for each committed removal, outside registry and implementation locks.
    /// The interface remains registered during this callback. Consumer admission and registry retention reject until removal completes.
    /// Caller-owned identities retain their explicit lifetime. The native backend has no additional implementation cache to release.</remarks>
    protected virtual void OnCleanup() { }
    private protected override void ValidateDisposalTransition()
    { base.ValidateDisposalTransition(); if (this is TextServerExtension) ValidateProviderDisposal(); }
}
