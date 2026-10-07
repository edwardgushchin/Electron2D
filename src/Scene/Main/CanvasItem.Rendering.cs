namespace Electron2D;

public abstract partial class CanvasItem
{
    internal RenderingCanvasItemState? ServerState;
    internal CanvasItem? RenderParent => ServerState is { ParentAssigned: true } state ? RenderingCanvasItemRegistry.ResolveOrNull(state.Parent) : GetParentItem();
    internal Transform RenderLocal(float fraction) => ServerState?.Transform ?? GetInterpolatedVisualTransform(fraction);
    internal Transform RenderGlobal(float fraction) => RenderParent is { } parent ? parent.RenderGlobal(fraction) * RenderLocal(fraction) : RenderLocal(fraction);
    internal Color RenderModulate => ServerState?.Modulate ?? _modulate;
    internal Color RenderSelfModulate => ServerState?.SelfModulate ?? _selfModulate;
    internal Color RenderInheritedModulate => RenderParent is { } parent ? parent.RenderInheritedModulate * RenderModulate : RenderModulate;
    internal bool RenderVisible => (ServerState?.Visible ?? _visible) && (RenderParent?.RenderVisible ?? (ServerState is { ParentAssigned: true } || !IsInsideTree || _parentVisible));
    internal bool RenderBehind => ServerState?.Behind ?? ShowBehindParent;
    internal bool RenderYSort => ServerState?.YSort ?? YSortEnabled;
    internal int RenderZ => (ServerState?.ZRelative ?? ZAsRelative) ? Mathf.Clamp((RenderParent?.RenderZ ?? 0) + (ServerState?.Z ?? ZIndex), MinimumZIndex, MaximumZIndex) : ServerState?.Z ?? ZIndex;
    internal TextureFilter RenderFilter => (ServerState?.Filter ?? _textureFilter) is var value && value != TextureFilter.ParentNode ? value : RenderParent?.RenderFilter ?? TextureFilter.ParentNode;
    internal TextureRepeat RenderRepeat => (ServerState?.Repeat ?? _textureRepeat) is var value && value != TextureRepeat.ParentNode ? value : RenderParent?.RenderRepeat ?? TextureRepeat.ParentNode;
    internal RenderingCanvasRuntime? RenderCanvas()
    {
        var root = this; for (var depth = 0; depth <= 1024; depth++)
        {
            if (root.ServerState is { ParentAssigned: true } state && RenderingCanvasRegistry.ResolveOrNull(state.Parent) is { } canvas) return canvas;
            if (root.RenderParent is not { } parent) { if (root.ServerState is { ParentAssigned: true }) return null; return root._canvasLayer is { } layer ? layer.CanvasRuntime : root.GetViewport() is { } viewport ? viewport.DefaultCanvasRuntime : null; }
            root = parent;
        }
        throw new InvalidOperationException("Canvas parent depth exceeds 1024.");
    }
    internal bool RenderLive(SceneTree tree) => !IsDisposed && (ServerState is { Owned: true } || ReferenceEquals(Tree, tree) && IsInsideTree || ServerState is { ParentAssigned: true }) && RenderVisible;
    internal Rect2? RenderClip => ServerState?.Clip == true ? RenderBounds() : ServerState?.Clip == false ? null : CanvasClipRect ?? (this is Control { ClipContents: true } control ? new(Vector2.Zero, control.Size) : null);
    internal Rect2 RenderBounds()
    {
        if (ServerState?.CustomRect is { } rect) return rect;
        var commands = ServerState?.Commands ?? this; var found = false; var bounds = default(Rect2); var drawing = Transform.Identity;
        if (commands._canvasCommands is not null) foreach (var command in commands._canvasCommands)
            {
                if (command.SetTransform) { drawing = command.Transform; continue; }
                if (command.AnimationSlice is not null || command.ClipIgnore is not null) continue;
                Rect2? value = command.Mesh?.GetDrawBounds() ?? command.MultiMesh?.Bounds();
                if (value is null && command.Stroke is { } stroke) value = stroke.GetLocalBounds();
                if (value is null && command.Polygon is { } polygon) value = polygon.GetLocalBounds();
                if (value is null) value = command.Line ? new Rect2(command.A, Vector2.Zero).Expand(command.B).Grow(Math.Max(0, command.Width / 2)) : new Rect2(command.A, command.B).Abs();
                if (value is { } area) { area = drawing * area; bounds = found ? bounds.Merge(area) : area; found = true; }
            }
        return bounds;
    }
    internal ServerDrawingScope StartServerDrawing() => new(this);
    internal readonly struct ServerDrawingScope : IDisposable
    {
        private readonly CanvasItem _item;
        private readonly CanvasItem? _previous;
        private readonly bool _drawing;
        internal ServerDrawingScope(CanvasItem item) { _item = item; _previous = _currentDrawingItem; _drawing = item._drawing; _currentDrawingItem = item; item._drawing = true; }
        public void Dispose() { _item._drawing = _drawing; _currentDrawingItem = _previous; }
    }
    internal bool MayIgnoreClip
    {
        get { var commands = ServerState?.Commands?._canvasCommands ?? _canvasCommands; if (commands is not null) foreach (var command in commands) if (command.ClipIgnore == true) return true; return false; }
    }
    internal uint RenderVisibilityLayer => ServerState?.VisibilityLayer ?? _visibilityLayer;
    internal void RecordServerClipIgnore(bool ignore) => (_canvasCommands ??= []).Add(new(false, default, default, Colors.White, 0, false, Transform.Identity, ClipIgnore: ignore));
    internal void RecordServerStroke(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased, bool connected)
    {
        EnsureDrawing();
        if (points.Length < 2 || points.Length > 1_048_576 || !connected && points.Length % 2 != 0 || colors.Length != 0 && colors.Length != 1 && colors.Length != points.Length && (connected || colors.Length != points.Length / 2)) throw new ArgumentException("Invalid stroke point/color count.");
        if (!float.IsFinite(width)) throw new ArgumentException("Stroke width must be finite.");
        foreach (var point in points) if (!point.IsFinite()) throw new ArgumentException("Stroke points must be finite.");
        foreach (var color in colors) ValidateCanvasColor(color);
        var stroke = NextStroke(); stroke.Set(points, colors, width, antialiased, connected, perVertexColors: !connected && colors.Length == points.Length); CommitStroke(stroke);
    }
    internal void RecordServerTriangles(ReadOnlySpan<int> indices, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> bones, ReadOnlySpan<float> weights, Texture? texture, int count)
    {
        EnsureDrawing();
        if (points.Length == 0 || points.Length > 1_048_576 || colors.Length != 0 && colors.Length != 1 && colors.Length != points.Length || uvs.Length != 0 && uvs.Length != points.Length || bones.Length != 0 && bones.Length != checked(points.Length * 4) || weights.Length != 0 && weights.Length != checked(points.Length * 4)) throw new ArgumentException("Invalid triangle channel count.");
        var used = count < 0 ? indices.Length / 3 * 3 : checked(count * 3); if (used > indices.Length) throw new ArgumentOutOfRangeException(nameof(count));
        foreach (var point in points) if (!point.IsFinite()) throw new ArgumentException("Triangle points must be finite.");
        foreach (var color in colors) ValidateCanvasColor(color);
        foreach (var uv in uvs) if (!uv.IsFinite()) throw new ArgumentException("Triangle UVs must be finite.");
        foreach (var bone in bones) if ((uint)bone > 65535) throw new ArgumentOutOfRangeException(nameof(bones));
        foreach (var weight in weights) if (!float.IsFinite(weight)) throw new ArgumentException("Triangle weights must be finite.");
        foreach (var index in indices[..used]) if ((uint)index >= (uint)points.Length) throw new ArgumentOutOfRangeException(nameof(indices));
        if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
        _polygons ??= []; if (_polygonCount == _polygons.Count) _polygons.Add(new());
        var polygon = _polygons[_polygonCount]; polygon.SetIndexedTriangles(indices[..used], points, colors, uvs, bones, weights, indices.IsEmpty ? points.Length / 3 * 3 : used);
        (_canvasCommands ??= []).Add(new(false, default, default, Colors.White, 0, false, Transform.Identity, texture, Polygon: polygon)); _polygonCount++;
    }
    internal void AppendServerMesh(Mesh mesh, Texture? texture, Transform transform, Color modulate)
    { var previous = _currentDrawingItem; var drawing = _drawing; _currentDrawingItem = this; _drawing = true; try { DrawMesh(mesh, texture, transform, modulate); } finally { _drawing = drawing; _currentDrawingItem = previous; } }
    internal void AppendServerMultiMesh(MultiMesh mesh, Texture? texture)
    { var previous = _currentDrawingItem; var drawing = _drawing; _currentDrawingItem = this; _drawing = true; try { DrawMultiMesh(mesh, texture); } finally { _drawing = drawing; _currentDrawingItem = previous; } }
    internal void AppendServerNinePatch(Texture texture, Rect2 rect, Rect2 source, CanvasNinePatch patch, Color color)
    { var previous = _currentDrawingItem; var drawing = _drawing; _currentDrawingItem = this; _drawing = true; try { RecordNinePatch(texture, rect, source, patch, color); } finally { _drawing = drawing; _currentDrawingItem = previous; } }
    internal void ClearServerCommands() { _canvasCommands?.Clear(); if (_meshes is not null) foreach (var mesh in _meshes) mesh.Clear(); if (_multiMeshes is not null) foreach (var mesh in _multiMeshes) mesh.Clear(); _meshCount = _multiMeshCount = _polygonCount = _strokeCount = 0; }
    internal CanvasItem ServerCommandTarget(RenderingCanvasItemState state)
    {
        if (state.Owned || _drawing) return this;
        if (state.Commands is not null) return state.Commands;
        var painter = new RenderingServerCanvasItem(); if (_canvasCommands is not null) painter._canvasCommands = new(_canvasCommands); return state.Commands = painter;
    }
}

internal sealed class RenderingServerCanvasItem : Entity { }
