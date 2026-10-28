namespace Electron2D;

/// <summary>Describes copied native sample coverage, fragment frequency and alpha configuration.</summary>
public sealed class RDPipelineMultisampleState : ElectronObject
{
    /// <summary>Creates the default configuration.</summary>
    public RDPipelineMultisampleState() { }
    private RenderingDevice.TextureSamples _sampleCount = RenderingDevice.TextureSamples.Samples1;
    /// <summary>Selects the exact raster sample count.</summary>
    /// <value>Initially RenderingDevice.TextureSamples.Samples1; copied when the device consumes this configuration.</value>
    public RenderingDevice.TextureSamples SampleCount { get { ThrowIfDisposed(); return _sampleCount; } set { ThrowIfDisposed(); _sampleCount = value; } }
    private bool _enableAlphaToCoverage = false;
    /// <summary>Converts fragment alpha into sample coverage.</summary>
    /// <value>Initially false; copied when the device consumes this configuration.</value>
    public bool EnableAlphaToCoverage { get { ThrowIfDisposed(); return _enableAlphaToCoverage; } set { ThrowIfDisposed(); _enableAlphaToCoverage = value; } }
    private bool _enableAlphaToOne;
    /// <summary>Replaces fragment color alpha with one after alpha-to-coverage evaluation.</summary>
    /// <value>False initially. Copied at pipeline creation; requires the native alpha-to-one feature.</value>
    public bool EnableAlphaToOne { get { ThrowIfDisposed(); return _enableAlphaToOne; } set { ThrowIfDisposed(); _enableAlphaToOne = value; } }
    private bool _enableSampleShading;
    /// <summary>Enables a minimum fraction of distinct fragment sample invocations.</summary>
    /// <value>False initially. Requires the native sample-rate shading feature.</value>
    public bool EnableSampleShading { get { ThrowIfDisposed(); return _enableSampleShading; } set { ThrowIfDisposed(); _enableSampleShading = value; } }
    private float _minSampleShading;
    /// <summary>Selects the minimum fraction of raster samples shaded separately when sample shading is enabled.</summary>
    /// <value>Zero initially; finite values from zero through one are accepted and copied.</value>
    /// <remarks>Sample-index/position shader inputs independently force full sample shading.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The fraction is nonfinite or outside zero through one.</exception>
    public float MinSampleShading { get { ThrowIfDisposed(); return _minSampleShading; } set { ThrowIfDisposed(); if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); _minSampleShading = value; } }
    private uint[] _sampleMasks = [];
    /// <summary>Selects enabled raster samples through 32-bit mask words.</summary>
    /// <value>Empty initially, enabling all samples. Reads and writes copy; a nonempty array has one entry per raster sample.</value>
    /// <remarks>Bit b of word w selects sample 32*w+b. Only the first rounded-up group of words is consumed; trailing entries and bits beyond the raster sample count are ignored.</remarks>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    public uint[] SampleMasks { get { ThrowIfDisposed(); return _sampleMasks.ToArray(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _sampleMasks = value.ToArray(); } }
    internal ReadOnlySpan<uint> BorrowMasks { get { ThrowIfDisposed(); return _sampleMasks; } }
    /// <summary>Projects inherited metadata followed by the executing configuration fields.</summary>
    /// <returns>Typed configuration descriptors with their default values.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDPipelineMultisampleState, RenderingDevice.TextureSamples>(nameof(SampleCount), p => p.SampleCount, (p, v) => p.SampleCount = v, _ => RenderingDevice.TextureSamples.Samples1),
        new PropertyDescriptor<RDPipelineMultisampleState, bool>(nameof(EnableAlphaToOne), p => p.EnableAlphaToOne, (p, v) => p.EnableAlphaToOne = v, _ => false),
        new PropertyDescriptor<RDPipelineMultisampleState, bool>(nameof(EnableSampleShading), p => p.EnableSampleShading, (p, v) => p.EnableSampleShading = v, _ => false),
        new PropertyDescriptor<RDPipelineMultisampleState, float>(nameof(MinSampleShading), p => p.MinSampleShading, (p, v) => p.MinSampleShading = v, _ => 0),
        new PropertyDescriptor<RDPipelineMultisampleState, uint[]>(nameof(SampleMasks), p => p.SampleMasks, (p, v) => p.SampleMasks = v, _ => []),
        new PropertyDescriptor<RDPipelineMultisampleState, bool>(nameof(EnableAlphaToCoverage), p => p.EnableAlphaToCoverage, (p, v) => p.EnableAlphaToCoverage = v, _ => false),
    ]);
}
