namespace Electron2D;

/// <summary>Provides a caller-defined text implementation through typed protected callbacks.</summary>
/// <remarks>Public operations retain engine admission. Inherited callbacks execute the native backend.
/// Register and select this object through TextServerManager; release owned identities before disposal.</remarks>
public abstract class TextServerExtension : TextServer
{
    /// <summary>Creates an unregistered implementation with independent native fallback ownership.</summary>
    protected TextServerExtension() { }
    /// <inheritdoc />
    protected override void ValidateDisposal() => ValidateProviderDisposal();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null;
        try { CanvasRasterScale.ForgetProvider(this); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { base.Dispose(disposing); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Text implementation disposal failed.", errors);
    }
}
