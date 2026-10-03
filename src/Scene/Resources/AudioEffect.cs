using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Describes an audio bus effect and creates independent stereo processing state.</summary>
/// <remarks>Buses borrow resources and own one instance per output stereo pair. Factories and graph edits are
/// preparation operations; repeated processing must use prepared storage and synchronize custom mutable state.</remarks>
public abstract class AudioEffect : Resource
{
    /// <summary>Initializes an effect resource.</summary>
    protected AudioEffect() { }
    /// <summary>Creates a caller-owned stereo effect instance borrowing this resource.</summary>
    /// <returns>A live, independent instance.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">The factory returns null, disposed or bus-owned state.</exception>
    public AudioEffectInstance Instantiate()
    {
        ThrowIfDisposed(); var instance = OnInstantiate();
        if (instance is null || instance.IsDisposed || instance.IsAttached) throw new InvalidOperationException("An effect must create a live independent instance.");
        if (IsDisposed) { instance.Dispose(); ThrowIfDisposed(); }
        return instance;
    }
    /// <summary>Creates independent stereo processing state.</summary>
    /// <returns>A new caller-owned instance; factories must not reenter audio configuration.</returns>
    protected abstract AudioEffectInstance OnInstantiate();
}

/// <summary>Processes stereo floating-point PCM for an audio effect.</summary>
/// <remarks>Standalone instances are caller-owned. Instances obtained from AudioServer are borrowed and may
/// not be processed or disposed by callers. Structural effect edits and output closure invalidate them.
/// Hooks run on the audio thread, support aliased input/output, and must avoid allocation and configuration.</remarks>
public abstract class AudioEffectInstance : ElectronObject
{
    private readonly object _gate = new();
    private Vector2[] _overlapScratch = [];
    private bool _processing, _attached;
    internal bool IsAttached { get { lock (_gate) return _attached; } }
    /// <summary>Initializes independent processing state.</summary>
    protected AudioEffectInstance() { }
    /// <summary>Processes a complete standalone stereo block into caller-provided storage.</summary>
    /// <param name="source">Finite stereo input; it may alias destination.</param>
    /// <param name="destination">Output with exactly the same number of frames; overwritten by the hook.</param>
    /// <remarks>Partial overlap prepares a reusable input copy; its first call or capacity growth may allocate.
    /// Exact in-place and disjoint blocks need no copy.</remarks>
    /// <exception cref="ArgumentException">Lengths differ or PCM is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The instance is bus-owned or processing is reentrant.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    public void Process(ReadOnlySpan<Vector2> source, Span<Vector2> destination) => ProcessCore(source, destination, native: false);
    internal void ProcessNative(ReadOnlySpan<Vector2> source, Span<Vector2> destination) => ProcessCore(source, destination, native: true);
    private void ProcessCore(ReadOnlySpan<Vector2> source, Span<Vector2> destination, bool native)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_processing || _attached && !native) throw new InvalidOperationException("Effect processing is reentrant or bus-owned.");
            if (source.Length != destination.Length) throw new ArgumentException("Effect input and output lengths must match.");
            foreach (var frame in source) if (!float.IsFinite(frame.X) || !float.IsFinite(frame.Y)) throw new ArgumentException("Effect input must be finite.", nameof(source));
            if (source.Overlaps(destination) && !Unsafe.AreSame(ref MemoryMarshal.GetReference(source), ref MemoryMarshal.GetReference(destination)))
            {
                if (_overlapScratch.Length < source.Length) _overlapScratch = new Vector2[source.Length];
                source.CopyTo(_overlapScratch); source = _overlapScratch.AsSpan(0, source.Length);
            }
            _processing = true;
            AudioServer.EnterAudioProcessing();
            try
            {
                OnProcess(source, destination);
                foreach (var frame in destination) if (!float.IsFinite(frame.X) || !float.IsFinite(frame.Y)) throw new ArgumentException("Effect output must be finite.", nameof(destination));
            }
            finally { AudioServer.ExitAudioProcessing(); _processing = false; }
        }
    }
    /// <summary>Transforms a complete stereo block, including aliased input and output.</summary>
    /// <param name="source">Finite stereo input.</param>
    /// <param name="destination">Same-length output; write every frame.</param>
    protected abstract void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination);
    /// <summary>Gets whether this instance requests processing of silent bus blocks.</summary>
    /// <returns>False unless the concrete hook opts in.</returns>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    public bool ProcessSilence()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_processing) throw new InvalidOperationException("Effect processing is reentrant.");
            _processing = true; AudioServer.EnterAudioProcessing(); try { return OnProcessSilence(); } finally { AudioServer.ExitAudioProcessing(); _processing = false; }
        }
    }
    /// <summary>Selects processing while the bus input is silent.</summary>
    /// <returns>False by default; true for capture and effects with silent-input output.</returns>
    protected virtual bool OnProcessSilence() => false;
    internal void Attach(int pair)
    {
        lock (_gate) { ThrowIfDisposed(); if (_attached || _processing) throw new InvalidOperationException("An effect instance is already in use."); _attached = true; }
        OnAttached(pair);
    }
    internal virtual void OnAttached(int pair) { }
    internal void Detach() { lock (_gate) _attached = false; }
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The instance is bus-owned or processing.</exception>
    protected override void ValidateDisposal() { lock (_gate) { if (_attached || _processing) throw new InvalidOperationException("An active or bus-owned effect instance cannot be disposed."); base.ValidateDisposal(); } }
}
