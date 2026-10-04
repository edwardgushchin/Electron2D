namespace Electron2D;

public sealed partial class Input
{
    private int _defaultCursorShape;

    internal CursorShape DefaultCursorShape => (CursorShape)Volatile.Read(ref _defaultCursorShape);

    internal MouseMode MouseModeCore
    {
        get => RequireDisplay().MouseGetModeCore();
        set => RequireDisplay().MouseSetModeCore(value);
    }

    internal CursorShape GetCurrentCursorShapeCore() => RequireDisplay().CursorGetShapeCore();

    internal void SetDefaultCursorShapeCore(CursorShape shape = CursorShape.Arrow)
    {
        RequireDisplay().CursorSetShapeCore(shape);
        Volatile.Write(ref _defaultCursorShape, (int)shape);
        if (Engine.MainLoop is SceneTree tree)
            tree.RefreshGUICursor();
    }

    internal void SetCustomMouseCursorCore(Resource? image, CursorShape shape = CursorShape.Arrow, Vector2 hotspot = default) =>
    RequireDisplay().CursorSetCustomImageCore(image, shape, hotspot);

    internal void WarpMouseCore(Vector2 position)
    {
        if (!position.IsFinite() || (double)position.X < int.MinValue || (double)position.X > int.MaxValue ||
            (double)position.Y < int.MinValue || (double)position.Y > int.MaxValue)
            throw new ArgumentException("Pointer coordinates must be finite and within native integer limits.", nameof(position));
        RequireDisplay().WarpMouseCore(new Vector2i((int)position.X, (int)position.Y));
    }

    private static DisplayServer RequireDisplay() => DisplayServer.Service ??
        throw new InvalidOperationException("Pointer control requires an active display server.");
}
