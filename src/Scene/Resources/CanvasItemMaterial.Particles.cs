namespace Electron2D;

public sealed partial class CanvasItemMaterial
{
    private bool _particlesAnimation, _particlesAnimLoop;
    private int _particlesAnimHFrames = 1, _particlesAnimVFrames = 1;
    /// <summary>Gets or sets sprite-sheet sampling for particle commands.</summary>
    /// <value>False initially; ordinary nonparticle canvas commands are unaffected.</value>
    /// <remarks>Prepared particle drawing reads this setting at replay, including while simulation is paused.</remarks>
    public bool ParticlesAnimation { get { lock (_gate) { ThrowIfDisposed(); return _particlesAnimation; } } set { lock (_gate) { ThrowIfDisposed(); if (_particlesAnimation == value) return; _particlesAnimation = value; } EmitChanged(); } }
    /// <summary>Gets or sets the horizontal sprite-sheet frame count.</summary>
    /// <value>One initially; valid range 1..1024.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside the bounded range.</exception>
    public int ParticlesAnimHFrames { get { lock (_gate) { ThrowIfDisposed(); return _particlesAnimHFrames; } } set { FrameCount(value); lock (_gate) { ThrowIfDisposed(); if (_particlesAnimHFrames == value) return; _particlesAnimHFrames = value; } EmitChanged(); } }
    /// <summary>Gets or sets the vertical sprite-sheet frame count.</summary>
    /// <value>One initially; valid range 1..1024.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside the bounded range.</exception>
    public int ParticlesAnimVFrames { get { lock (_gate) { ThrowIfDisposed(); return _particlesAnimVFrames; } } set { FrameCount(value); lock (_gate) { ThrowIfDisposed(); if (_particlesAnimVFrames == value) return; _particlesAnimVFrames = value; } EmitChanged(); } }
    /// <summary>Gets or sets wrapping of normalized particle animation phase.</summary>
    /// <value>False initially; disabled wrapping clamps the first and last frames.</value>
    public bool ParticlesAnimLoop { get { lock (_gate) { ThrowIfDisposed(); return _particlesAnimLoop; } } set { lock (_gate) { ThrowIfDisposed(); if (_particlesAnimLoop == value) return; _particlesAnimLoop = value; } EmitChanged(); } }
    private static void FrameCount(int value) { if (value < 1 || value > 1024) throw new ArgumentOutOfRangeException(nameof(value)); }
    internal (bool Enabled, int Horizontal, int Vertical, bool Loop) GetParticlesAnimation() { lock (_gate) { ThrowIfDisposed(); return (_particlesAnimation, _particlesAnimHFrames, _particlesAnimVFrames, _particlesAnimLoop); } }
    private static readonly PropertyDescriptor[] ParticleProperties =
    [
        new PropertyDescriptor<CanvasItemMaterial, bool>(nameof(ParticlesAnimation), p => p.ParticlesAnimation, (p, v) => p.ParticlesAnimation = v, _ => false, stored: true),
        new PropertyDescriptor<CanvasItemMaterial, int>(nameof(ParticlesAnimHFrames), p => p.ParticlesAnimHFrames, (p, v) => p.ParticlesAnimHFrames = v, _ => 1, stored: true),
        new PropertyDescriptor<CanvasItemMaterial, int>(nameof(ParticlesAnimVFrames), p => p.ParticlesAnimVFrames, (p, v) => p.ParticlesAnimVFrames = v, _ => 1, stored: true),
        new PropertyDescriptor<CanvasItemMaterial, bool>(nameof(ParticlesAnimLoop), p => p.ParticlesAnimLoop, (p, v) => p.ParticlesAnimLoop = v, _ => false, stored: true)
    ];
}
