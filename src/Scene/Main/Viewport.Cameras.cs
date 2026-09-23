namespace Electron2D;

public abstract partial class Viewport
{
    private Camera? _camera;

    /// <summary>Returns the active camera for this viewport's default canvas.</summary>
    /// <returns>A borrowed Camera, or null when no enabled camera is selected.</returns>
    /// <remarks>Entering enabled cameras claim an unoccupied viewport. Disabling or removing the current camera
    /// selects the first eligible camera in current tree order. A replacement updates on its next tracking call;
    /// removing the last eligible camera restores the identity canvas transform.</remarks>
    /// <exception cref="InvalidOperationException">An attached viewport is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public Camera? GetCamera() { CheckTransformQuery(); return _camera; }

    internal void SetCurrentCamera(Camera camera) => _camera = camera;

    internal void ReleaseCamera(Camera camera)
    {
        if (!ReferenceEquals(_camera, camera)) return;
        _camera = Tree is { } tree ? FindCamera(tree.Root) : null;
        if (_camera is null) CanvasTransform = Transform.Identity;
    }

    private Camera? FindCamera(Node node)
    {
        if (node is Camera camera && camera.CanTrack(this)) return camera;
        for (var i = 0; i < node.ChildCount; i++)
            if (FindCamera(node.GetChild(i)) is { } result) return result;
        return null;
    }
}
