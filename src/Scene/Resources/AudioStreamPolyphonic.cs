namespace Electron2D;

/// <summary>Creates one dynamic mixer with independently controlled child voices.</summary>
/// <remarks>Playbacks capture Polyphony when instantiated. Resources borrow no configured children;
/// transient voices belong to AudioStreamPlaybackPolyphonic and do not enter Resource copies.</remarks>
public sealed class AudioStreamPolyphonic : AudioStream
{
    private int _polyphony = 32;
    /// <summary>Creates a monophonic meta resource with thirty-two child slots.</summary>
    public AudioStreamPolyphonic() { }
    /// <summary>Gets or sets the child capacity captured by subsequent playbacks.</summary>
    /// <value>Thirty-two initially; zero through 128, including an intentionally empty mixer.</value>
    /// <remarks>Existing playbacks retain their capacity. Assignment does not notify Changed.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Capacity is outside zero through 128.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int Polyphony { get { ThrowIfDisposed(); return Volatile.Read(ref _polyphony); } set { ThrowIfDisposed(); if ((uint)value > 128) throw new ArgumentOutOfRangeException(nameof(value)); Volatile.Write(ref _polyphony, value); } }
    /// <inheritdoc />
    protected override AudioStreamPlayback OnInstantiatePlayback() => new AudioStreamPlaybackPolyphonic(this, Polyphony);
    /// <inheritdoc />
    protected override string OnGetStreamName() => nameof(AudioStreamPolyphonic);
    /// <inheritdoc />
    public override bool IsMetaStream() { ThrowIfDisposed(); return true; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioStreamPolyphonic();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) => ((AudioStreamPolyphonic)target).Polyphony = Polyphony;
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([new PropertyDescriptor<AudioStreamPolyphonic, int>(nameof(Polyphony), p => p.Polyphony, (p, v) => p.Polyphony = v, _ => 32, stored: true)]);
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (IsCalling(0) || IsCalling(4)) throw new InvalidOperationException("Child callbacks cannot dispose their polyphonic source."); base.ValidateDisposal(); }
}
