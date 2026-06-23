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

internal static class Viewport2DSmoke
{
    public static Viewport2DSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);
        Directory.CreateDirectory(Path.Combine(Path.GetFullPath(workRoot), "Viewport2DSmoke"));

        var tree = new Electron2D.SceneTree();
        var viewport = (Electron2D.Viewport)tree.Root;
        viewport.Size = new Electron2D.Vector2I(800, 600);

        var player = new Electron2D.Node2D
        {
            Name = "Player",
            Position = new Electron2D.Vector2(100f, 100f)
        };
        var enemy = new Electron2D.Node2D
        {
            Name = "Enemy",
            Position = new Electron2D.Vector2(200f, 100f)
        };
        viewport.AddChild(player);
        viewport.AddChild(enemy);

        var staticBody = new Electron2D.StaticBody2D
        {
            Name = "Wall",
            Position = new Electron2D.Vector2(300f, 200f)
        };
        staticBody.AddChild(new Electron2D.CollisionShape2D
        {
            Name = "WallShape",
            Shape = new Electron2D.RectangleShape2D { Size = new Electron2D.Vector2(40f, 20f) }
        });
        viewport.AddChild(staticBody);
        tree.DebugCollisionsHint = true;

        var camera = new Electron2D.Camera2D
        {
            Name = "PreviewCamera",
            Position = new Electron2D.Vector2(200f, 150f),
            Zoom = new Electron2D.Vector2(2f, 2f)
        };
        viewport.AddChild(camera);
        camera.MakeCurrent();

        var editorViewport = new Viewport2D(viewport)
        {
            MoveSnapStep = new Electron2D.Vector2(10f, 10f),
            RotationSnapStep = Electron2D.Mathf.DegToRad(15f),
            ScaleSnapStep = new Electron2D.Vector2(0.5f, 0.5f)
        };
        var selectableBounds = new Electron2D.Rect2(-10f, -10f, 20f, 20f);
        editorViewport.Register(player, selectableBounds);
        editorViewport.Register(enemy, selectableBounds);

        editorViewport.Pan(new Electron2D.Vector2(40f, -20f));
        var worldUnderCursorStable = editorViewport.ZoomAt(new Electron2D.Vector2(200f, 100f), 2f);

        editorViewport.SelectAt(editorViewport.WorldToScreen(player.GlobalPosition), additive: false);
        editorViewport.SelectAt(editorViewport.WorldToScreen(enemy.GlobalPosition), additive: true);

        editorViewport.MoveSelected(new Electron2D.Vector2(12f, 2f));
        var pivot = editorViewport.GetSelectionBounds().GetCenter();
        editorViewport.RotateSelected(Electron2D.Mathf.DegToRad(91f), pivot);
        editorViewport.ScaleSelected(new Electron2D.Vector2(1.9f, 2.1f), pivot);

        var selectionBounds = editorViewport.GetSelectionBounds();
        var collisionOverlays = editorViewport.CaptureCollisionOverlays(tree);
        var cameraPreview = editorViewport.GetCameraPreview(camera);
        var selected = string.Join("|", editorViewport.SelectedNodes.Select(node => node.Name));

        var success = worldUnderCursorStable &&
            selected == "Player|Enemy" &&
            player.GlobalPosition.IsEqualApprox(new Electron2D.Vector2(160f, 0f)) &&
            enemy.GlobalPosition.IsEqualApprox(new Electron2D.Vector2(160f, 200f)) &&
            Electron2D.Mathf.IsEqualApprox(Electron2D.Mathf.RadToDeg(player.GlobalRotation), 90f) &&
            Electron2D.Mathf.IsEqualApprox(Electron2D.Mathf.RadToDeg(enemy.GlobalRotation), 90f) &&
            player.GlobalScale.IsEqualApprox(new Electron2D.Vector2(2f, 2f)) &&
            enemy.GlobalScale.IsEqualApprox(new Electron2D.Vector2(2f, 2f)) &&
            selectionBounds.IsEqualApprox(new Electron2D.Rect2(140f, -20f, 40f, 240f)) &&
            collisionOverlays.Count == 1 &&
            cameraPreview.Bounds.IsEqualApprox(new Electron2D.Rect2(0f, 0f, 400f, 300f));

        if (!success)
        {
            throw new InvalidOperationException(
                $"2D Viewport smoke invariant failed. player={player.GlobalPosition}, enemy={enemy.GlobalPosition}, rotation={Electron2D.Mathf.RadToDeg(player.GlobalRotation)}, scale={player.GlobalScale}, bounds={selectionBounds}, overlays={collisionOverlays.Count}, camera={cameraPreview.Bounds}, stable={worldUnderCursorStable}, selected={selected}.");
        }

        return new Viewport2DSmokeResult(
            editorViewport.PanOffset,
            editorViewport.Zoom,
            selected,
            player.GlobalPosition,
            enemy.GlobalPosition,
            Electron2D.Mathf.RadToDeg(player.GlobalRotation),
            Electron2D.Mathf.RadToDeg(enemy.GlobalRotation),
            player.GlobalScale,
            enemy.GlobalScale,
            selectionBounds,
            collisionOverlays.Count,
            cameraPreview.Bounds,
            worldUnderCursorStable);
    }
}
