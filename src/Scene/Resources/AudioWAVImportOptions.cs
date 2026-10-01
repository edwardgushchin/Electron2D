namespace Electron2D;

/// <summary>Provides typed WAV import edits, format selection and optional explicit loop points.</summary>
public sealed class AudioWAVImportOptions
{
    /// <summary>Creates import edits with source-width PCM and detected loop metadata.</summary>
    public AudioWAVImportOptions() { }
    /// <summary>Gets whether to normalize the largest absolute sample to unity.</summary>
    /// <value>False by default.</value>
    public bool Normalize { get; init; }
    /// <summary>Gets whether to trim silence and apply the source's 500-frame tail fade when not looping.</summary>
    /// <value>False by default.</value>
    public bool Trim { get; init; }
    /// <summary>Gets whether to force eight-bit internal PCM when compression is disabled.</summary>
    /// <value>False by default.</value>
    public bool Force8Bit { get; init; }
    /// <summary>Gets whether to average stereo input into one channel.</summary>
    /// <value>False by default.</value>
    public bool ForceMono { get; init; }
    /// <summary>Gets whether to limit the imported sample rate.</summary>
    /// <value>False by default.</value>
    public bool LimitRate { get; init; }
    /// <summary>Gets the requested maximum frequency in Hz.</summary>
    /// <value>Zero by default; must be positive when LimitRate is true.</value>
    public int MaxRate { get; init; }
    /// <summary>Gets the import loop policy: null detects RIFF metadata; Disabled discards it.</summary>
    /// <value>Null by default; detects source loop metadata.</value>
    public AudioStreamWAV.LoopMode? Loop { get; init; }
    /// <summary>Gets the explicit loop-begin frame; negative values count back from the imported end.</summary>
    /// <value>Zero by default; interpreted only with an explicit enabled Loop.</value>
    public int LoopBegin { get; init; }
    /// <summary>Gets the explicit loop-end frame; negative values count back from the imported end.</summary>
    /// <value>Zero by default; interpreted only with an explicit enabled Loop.</value>
    public int LoopEnd { get; init; }
    /// <summary>Gets the internal compressed representation, or null for ordinary source-width PCM.</summary>
    /// <value>Only IMAADPCM/QOA select compression.</value>
    public AudioStreamWAV.Format? Compression { get; init; }
}
