namespace Electron2D;

/// <summary>Encodes non-real-time rendered frames and interleaved signed 32-bit PCM.</summary>
/// <remarks>Register before Engine.Run. The registry borrows writers; their creating thread owns
/// registration and disposal; recording hooks execute on the runtime owner. Most recently registered handlers take precedence.
/// Frame images and audio spans are borrowed only for the callback. Exceptions abort the run,
/// attempt OnWriteEnd and release every owned runtime service. Encoding and readback are cold work.</remarks>
public abstract class MovieWriter : ElectronObject
{
    private static readonly object RegistryGate = new();
    private static readonly List<MovieWriter> Writers = [];
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private int _active, _callback;
    private bool _registered, _owned;
    [ThreadStatic] private static int _registrationDepth;
    internal static void EnsureStartupAllowed()
    { if (_registrationDepth != 0) throw new InvalidOperationException("Runtime startup cannot reenter writer registration."); }

    /// <summary>Creates an unregistered writer on its callback owner thread.</summary>
    protected MovieWriter() { }

    /// <summary>Registers a borrowed writer, giving it precedence over older handlers.</summary>
    /// <param name="writer">A live writer created on this thread.</param>
    /// <remarks>At most eight live custom writers may be registered.
    /// Disposed registrations are reclaimed on the next registration. Duplicate registration rejects.</remarks>
    /// <exception cref="ArgumentNullException">The writer is null.</exception>
    /// <exception cref="ObjectDisposedException">The writer is disposed.</exception>
    /// <exception cref="InvalidOperationException">The runtime is active, the owner differs, registration is duplicate/full, or a hook reenters.</exception>
    /// <exception cref="ArgumentException">A supported extension is empty, duplicated or contains punctuation other than underscore or hyphen.</exception>
    public static void AddWriter(MovieWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer); writer.CheckOwner();
        if (!Engine.Service.IsRuntimeIdle || writer._callback != 0 || _registrationDepth != 0) throw new InvalidOperationException("Register writers before runtime startup and outside writer hooks.");
        writer._callback++; _registrationDepth++;
        string[] extensions;
        try { extensions = writer.OnGetSupportedExtensions() ?? throw new InvalidOperationException("Writer returned null extensions."); }
        finally { writer._callback--; _registrationDepth--; }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in extensions)
        {
            if (string.IsNullOrEmpty(extension) || !seen.Add(extension) || extension.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '_' and not '-'))
                throw new ArgumentException("Writer extensions must be unique bare filename suffixes.", nameof(writer));
        }
        lock (RegistryGate)
        {
            if (!Engine.Service.IsRuntimeIdle) throw new InvalidOperationException("The runtime is starting.");
            Writers.RemoveAll(w => w.IsDisposed);
            if (Writers.Contains(writer) || Writers.Count == 8) throw new InvalidOperationException("Writer is already registered or the registry is full.");
            Writers.Add(writer); writer._registered = true;
        }
    }

    /// <summary>Returns the requested recording rate in Hz.</summary>
    /// <returns>48000 by default; supported rates span 1000 through 200000.</returns>
    protected virtual int OnGetAudioMixRate() => 48000;
    /// <summary>Returns the recording channel arrangement.</summary>
    /// <returns>Stereo by default; all four AudioServer speaker modes are supported.</returns>
    protected virtual AudioServer.SpeakerMode OnGetAudioSpeakerMode() => AudioServer.SpeakerMode.Stereo;
    /// <summary>Lists bare filename extensions during registration.</summary>
    /// <returns>A nonnull list; extension matching remains the responsibility of OnHandlesFile.</returns>
    protected abstract string[] OnGetSupportedExtensions();
    /// <summary>Tests whether this handler accepts the requested output.</summary>
    /// <param name="path">The caller's unchanged movie path.</param><returns>True to select this writer.</returns>
    protected abstract bool OnHandlesFile(string path);
    /// <summary>Prepares output before scene activation.</summary>
    /// <param name="movieSize">Positive rendered pixel dimensions fixed for this recording.</param>
    /// <param name="fps">Positive frames per second.</param><param name="basePath">The requested output path.</param>
    /// <remarks>Throw on failure. OnWriteEnd is attempted even if this hook partially prepares and throws.</remarks>
    protected abstract void OnWriteBegin(Vector2i movieSize, int fps, string basePath);
    /// <summary>Writes one completed rendered frame and its exact duration of audio.</summary>
    /// <param name="frameImage">A borrowed RGB8 frame at the initial recording size.</param>
    /// <param name="audioFrameBlock">Borrowed interleaved PCM in AudioServer channel order; full range is signed Int32.</param>
    /// <remarks>For nonintegral rates, block lengths alternate to preserve the cumulative sample clock without drift.</remarks>
    protected abstract void OnWriteFrame(Image frameImage, ReadOnlySpan<int> audioFrameBlock);
    /// <summary>Finalizes output on normal quit or failure, before native services close.</summary>
    /// <remarks>May follow a partially failed begin. Throwing does not prevent other runtime cleanup.</remarks>
    protected abstract void OnWriteEnd();

    private void CheckOwner()
    {
        ThrowIfDisposed();
        if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Movie writer requires its creating thread.");
    }
    internal static MovieWriter Acquire(string path)
    {
        MovieWriter[] snapshot; lock (RegistryGate) snapshot = Writers.ToArray();
        for (var i = snapshot.Length - 1; i >= 0; i--)
        {
            var writer = snapshot[i]; if (writer.IsDisposed) continue; writer.ThrowIfDisposed();
            writer._callback++;
            bool accepts; try { accepts = writer.OnHandlesFile(path); } finally { writer._callback--; }
            if (!accepts) continue;
            if (writer._active != 0) throw new InvalidOperationException("Writer is already recording.");
            writer._active = 1; return writer;
        }
        if (System.IO.Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)) return new PNGMovieWriter { _active = 1, _owned = true };
        if (System.IO.Path.GetExtension(path).Equals(".avi", StringComparison.OrdinalIgnoreCase)) return new AVIMovieWriter { _active = 1, _owned = true };
        throw new NotSupportedException("No movie writer accepts this path.");
    }
    internal (int Rate, int Channels) GetAudioFormat()
    {
        _callback++;
        try
        {
            var rate = OnGetAudioMixRate(); var mode = OnGetAudioSpeakerMode();
            if (rate is < 1000 or > 200000 || !Enum.IsDefined(mode)) throw new InvalidOperationException("Invalid movie audio format.");
            return (rate, 2 + (int)mode * 2);
        }
        finally { _callback--; }
    }
    internal void Begin(Vector2i size, int fps, string path) { _callback++; try { OnWriteBegin(size, fps, path); } finally { _callback--; } }
    internal void Write(Image image, ReadOnlySpan<int> pcm) { _callback++; try { OnWriteFrame(image, pcm); } finally { _callback--; } }
    internal void End() { _callback++; try { OnWriteEnd(); } finally { _callback--; } }
    internal void Release() { _active = 0; if (_owned) Dispose(); }
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">A registered runtime, hook or recording is active, or disposal occurs off-owner.</exception>
    protected override void ValidateDisposal()
    {
        base.ValidateDisposal(); CheckOwner();
        if (_active != 0 || _callback != 0 || (_registered && !Engine.Service.IsRuntimeIdle)) throw new InvalidOperationException("An active movie writer cannot be disposed.");
    }
}
