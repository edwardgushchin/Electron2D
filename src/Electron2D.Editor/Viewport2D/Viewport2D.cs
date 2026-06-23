/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D.Editor.Viewport2D;

internal sealed class Viewport2D
{
    private readonly Electron2D.Viewport viewport;
    private readonly List<ViewportSelectable2D> selectables = [];
    private readonly HashSet<Electron2D.Node2D> selected = [];

    public Viewport2D(Electron2D.Viewport viewport)
    {
        this.viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
    }

    public Electron2D.Vector2 PanOffset { get; private set; } = Electron2D.Vector2.Zero;

    public float Zoom { get; private set; } = 1f;

    public Electron2D.Vector2 MoveSnapStep { get; set; } = Electron2D.Vector2.Zero;

    public float RotationSnapStep { get; set; }

    public Electron2D.Vector2 ScaleSnapStep { get; set; } = Electron2D.Vector2.Zero;

    public IReadOnlyList<Electron2D.Node2D> SelectedNodes => selected
        .OrderBy(node => node.GetIndex())
        .ToArray();

    public void Register(Electron2D.Node2D node, Electron2D.Rect2 localBounds)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!localBounds.Size.IsFinite() || localBounds.Size.X < 0f || localBounds.Size.Y < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(localBounds), localBounds, "Editor viewport selectable bounds must be finite and non-negative.");
        }

        selectables.RemoveAll(item => ReferenceEquals(item.Node, node));
        selectables.Add(new ViewportSelectable2D(node, localBounds));
    }

    public void Pan(Electron2D.Vector2 screenDelta)
    {
        if (!screenDelta.IsFinite())
        {
            throw new ArgumentOutOfRangeException(nameof(screenDelta), screenDelta, "Editor viewport pan delta must be finite.");
        }

        PanOffset += screenDelta;
    }

    public bool ZoomAt(Electron2D.Vector2 screenPoint, float factor)
    {
        if (!screenPoint.IsFinite())
        {
            throw new ArgumentOutOfRangeException(nameof(screenPoint), screenPoint, "Editor viewport zoom point must be finite.");
        }

        if (!float.IsFinite(factor) || factor <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), factor, "Editor viewport zoom factor must be finite and greater than zero.");
        }

        var before = ScreenToWorld(screenPoint);
        Zoom *= factor;
        PanOffset = screenPoint - (before * Zoom);
        return before.IsEqualApprox(ScreenToWorld(screenPoint));
    }

    public Electron2D.Vector2 ScreenToWorld(Electron2D.Vector2 screenPoint)
    {
        return (screenPoint - PanOffset) / Zoom;
    }

    public Electron2D.Vector2 WorldToScreen(Electron2D.Vector2 worldPoint)
    {
        return (worldPoint * Zoom) + PanOffset;
    }

    public bool SelectAt(Electron2D.Vector2 screenPoint, bool additive)
    {
        var worldPoint = ScreenToWorld(screenPoint);
        var hit = selectables
            .Where(item => GetWorldBounds(item).HasPoint(worldPoint))
            .OrderByDescending(item => item.Node.GetIndex())
            .FirstOrDefault();

        if (!additive)
        {
            selected.Clear();
        }

        if (hit.Node is null)
        {
            return false;
        }

        selected.Add(hit.Node);
        return true;
    }

    public int SelectByRect(Electron2D.Rect2 screenRect, bool additive)
    {
        var worldRect = new Electron2D.Rect2(ScreenToWorld(screenRect.Position), Electron2D.Vector2.Zero)
            .Expand(ScreenToWorld(new Electron2D.Vector2(screenRect.End.X, screenRect.Position.Y)))
            .Expand(ScreenToWorld(screenRect.End))
            .Expand(ScreenToWorld(new Electron2D.Vector2(screenRect.Position.X, screenRect.End.Y)))
            .Abs();

        if (!additive)
        {
            selected.Clear();
        }

        foreach (var item in selectables.Where(item => GetWorldBounds(item).Intersects(worldRect, includeBorders: true)))
        {
            selected.Add(item.Node);
        }

        return selected.Count;
    }

    public void MoveSelected(Electron2D.Vector2 worldDelta)
    {
        var snapped = Snap(worldDelta, MoveSnapStep);
        foreach (var node in selected)
        {
            node.GlobalPosition += snapped;
        }
    }

    public void RotateSelected(float angleRadians, Electron2D.Vector2? pivot = null)
    {
        var snapped = Snap(angleRadians, RotationSnapStep);
        var center = pivot ?? GetSelectionPivot();
        foreach (var node in selected)
        {
            node.GlobalPosition = center + (node.GlobalPosition - center).Rotated(snapped);
            node.GlobalRotation += snapped;
        }
    }

    public void ScaleSelected(Electron2D.Vector2 ratio, Electron2D.Vector2? pivot = null)
    {
        var snapped = Snap(ratio, ScaleSnapStep);
        var center = pivot ?? GetSelectionPivot();
        foreach (var node in selected)
        {
            var offset = node.GlobalPosition - center;
            node.GlobalPosition = center + new Electron2D.Vector2(offset.X * snapped.X, offset.Y * snapped.Y);
            node.GlobalScale *= snapped;
        }
    }

    public Electron2D.Rect2 GetSelectionBounds()
    {
        var bounds = SelectedItems()
            .Select(GetWorldBounds)
            .ToArray();
        if (bounds.Length == 0)
        {
            return new Electron2D.Rect2();
        }

        var result = bounds[0];
        for (var index = 1; index < bounds.Length; index++)
        {
            result = result.Merge(bounds[index]);
        }

        return result.Abs();
    }

    public IReadOnlyList<ViewportCollisionOverlay2D> CaptureCollisionOverlays(Electron2D.SceneTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        return tree.CapturePhysicsDebugShapes()
            .Select(shape => new ViewportCollisionOverlay2D(
                shape.Owner.Name,
                shape.ShapeNode.Name,
                shape.Bounds,
                shape.Color,
                shape.Disabled))
            .ToArray();
    }

    public ViewportCameraPreview2D GetCameraPreview(Electron2D.Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        var zoom = camera.Zoom;
        var size = new Electron2D.Vector2(
            viewport.Size.X / zoom.X,
            viewport.Size.Y / zoom.Y);
        return new ViewportCameraPreview2D(new Electron2D.Rect2(camera.GetTargetPosition() - (size / 2f), size));
    }

    private IReadOnlyList<ViewportSelectable2D> SelectedItems()
    {
        return selectables.Where(item => selected.Contains(item.Node)).ToArray();
    }

    private Electron2D.Vector2 GetSelectionPivot()
    {
        var bounds = GetSelectionBounds();
        if (!bounds.Size.IsFinite())
        {
            return Electron2D.Vector2.Zero;
        }

        return bounds.GetCenter();
    }

    private static Electron2D.Rect2 GetWorldBounds(ViewportSelectable2D selectable)
    {
        return (selectable.Node.GlobalTransform * selectable.LocalBounds).Abs();
    }

    private static Electron2D.Vector2 Snap(Electron2D.Vector2 value, Electron2D.Vector2 step)
    {
        return new Electron2D.Vector2(
            Electron2D.Mathf.Snapped(value.X, step.X),
            Electron2D.Mathf.Snapped(value.Y, step.Y));
    }

    private static float Snap(float value, float step)
    {
        return Electron2D.Mathf.Snapped(value, step);
    }
}
