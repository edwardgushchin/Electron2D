namespace Electron2D;

public abstract partial class Viewport
{
    private Camera? _camera;
    private readonly HashSet<Parallax> _parallaxes = [];
    private readonly HashSet<ParallaxBackground> _parallaxBackgrounds = [];

    /// <summary>Returns the active camera for this viewport's default canvas.</summary>
    /// <returns>A borrowed Camera, or null when no enabled camera is selected.</returns>
    /// <remarks>Entering enabled cameras claim an unoccupied viewport. Disabling or removing the current camera
    /// selects the first eligible camera in current tree order. A replacement updates on its next tracking call;
    /// removing the last eligible camera restores the identity canvas transform.</remarks>
    /// <exception cref="InvalidOperationException">An attached viewport is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public Camera? GetCamera() { CheckTransformQuery(); return _camera; }

    internal void SetCurrentCamera(Camera camera) { _camera = camera; ResetCanvasInterpolationSnapshot(); }

    internal void RegisterParallax(Parallax parallax) => _parallaxes.Add(parallax);
    internal void UnregisterParallax(Parallax parallax) => _parallaxes.Remove(parallax);
    internal void RegisterParallaxBackground(ParallaxBackground background) => _parallaxBackgrounds.Add(background);
    internal void UnregisterParallaxBackground(ParallaxBackground background) => _parallaxBackgrounds.Remove(background);

    internal void NotifyParallaxCameraMoved(Vector2 adjustedScreenPosition, Transform canvas, Vector2 screenOffset)
    {
        if (_parallaxes.Count == 0 && _parallaxBackgrounds.Count == 0) return;
        List<Exception>? errors = null;
        foreach (var parallax in _parallaxes.ToArray())
            if (!parallax.IsDisposed && ReferenceEquals(parallax.GetViewport(), this))
                try { parallax.CameraMoved(adjustedScreenPosition, SnapTransformsToPixel); }
                catch (Exception error) { CollectException(ref errors, error); }
        foreach (var background in _parallaxBackgrounds.ToArray())
            if (!background.IsDisposed && ReferenceEquals(background.CanvasViewport, this))
                try { background.CameraMoved(canvas, screenOffset); }
                catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("One or more parallax camera updates failed.", errors);
    }

    internal void ReleaseCamera(Camera camera)
    {
        if (!ReferenceEquals(_camera, camera)) return;
        _camera = Tree is { } tree ? FindCamera(tree.Root) : null;
        if (_camera is null) CanvasTransform = Transform.Identity;
    }

    private Camera? FindCamera(Node node)
    {
        if (node is Camera camera && camera.CanTrack(this)) return camera;
        for (var i = 0; i < node.GetChildCount(includeInternal: true); i++)
            if (FindCamera(node.GetChild(i, includeInternal: true)) is { } result) return result;
        return null;
    }
}
