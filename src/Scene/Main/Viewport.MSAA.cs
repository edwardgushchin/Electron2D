namespace Electron2D;

public abstract partial class Viewport
{
    /// <summary>Selects the number of coverage samples used for canvas geometry.</summary>
    public enum MSAA
    {
        /// <summary>Uses one coverage sample without multisample antialiasing.</summary>
        Disabled = 0,
        /// <summary>Uses two coverage samples per pixel.</summary>
        Samples2X = 1,
        /// <summary>Uses four coverage samples per pixel.</summary>
        Samples4X = 2,
        /// <summary>Uses eight coverage samples per pixel.</summary>
        Samples8X = 3,
        /// <summary>Sentinel; not a valid selection.</summary>
        Max = 4
    }
    private MSAA _msaa;
    private bool _msaaAssigned;
    internal void ApplyProjectMSAA() { if (!_msaaAssigned) _msaa = ProjectSettings.GetWithOverride(ProjectSettings.MSAA2D); }
    /// <summary>Gets or sets multisample antialiasing for this viewport's canvas geometry.</summary>
    /// <value>Disabled initially. The root window samples ProjectSettings.MSAA2D when Engine.Run starts unless explicitly assigned.</value>
    /// <remarks>Changes recreate native targets on the next submitted frame, preserving texture RID identity.
    /// Valid assignments republish a renderer-only override. GPU rendering and native support for the requested
    /// sample count are required; compatibility rendering rejects enabled MSAA. MSAA smooths geometry coverage without filtering texture texels.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is undefined or Max.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public MSAA MSAA2D
    {
        get { CheckTransformQuery(); return _msaa; }
        set { EnsureMutable(); if ((uint)value >= (uint)MSAA.Max) throw new ArgumentOutOfRangeException(nameof(value)); _msaa = value; _msaaAssigned = true; RenderingOwner?.PublishViewportMSAA(this); }
    }
    private static readonly PropertyDescriptor[] MSAAProperties =
    [new PropertyDescriptor<Viewport, MSAA>(nameof(MSAA2D), n => n.MSAA2D, (n, v) => n.MSAA2D = v, _ => MSAA.Disabled, (_, v) => (uint)v < (uint)MSAA.Max, stored: true)];
}
