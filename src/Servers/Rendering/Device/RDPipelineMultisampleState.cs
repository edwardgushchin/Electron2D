namespace Electron2D;

/// <summary>Describes copied native sample count and alpha-to-coverage configuration.</summary>
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
    /// <summary>Projects inherited metadata followed by the executing configuration fields.</summary>
    /// <returns>Typed configuration descriptors with their default values.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDPipelineMultisampleState, RenderingDevice.TextureSamples>(nameof(SampleCount), p => p.SampleCount, (p, v) => p.SampleCount = v, _ => RenderingDevice.TextureSamples.Samples1),
        new PropertyDescriptor<RDPipelineMultisampleState, bool>(nameof(EnableAlphaToCoverage), p => p.EnableAlphaToCoverage, (p, v) => p.EnableAlphaToCoverage = v, _ => false),
    ]);
}
