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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class SdlRendererCompatibilityBackendTests
{
    [Fact]
    public void CompatibilityBackendConvertsReferenceSceneToSdlRendererCommands()
    {
        var tree = CompatibilityReferenceScene.Create();
        tree.ProcessFrame(1.0 / 60.0);
        var renderPlan = new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root);

        var backend = new Electron2D.CompatibilityRenderingBackend();
        var framePlan = backend.CreateFramePlan(renderPlan);

        Assert.Equal("Compatibility", framePlan.BackendName);
        Assert.Equal(Electron2D.RenderingServer.RenderingProfile.Compatibility, framePlan.Profile);
        Assert.Equal(renderPlan.DrawCallCount, framePlan.DrawCallCount);
        Assert.Contains(Electron2D.RenderingServer.RenderingFeature.TileMap, framePlan.Features);
        Assert.DoesNotContain(Electron2D.RenderingServer.RenderingFeature.CustomShaders, framePlan.Features);
        Assert.DoesNotContain(Electron2D.RenderingServer.RenderingFeature.ShaderMaterial, framePlan.Features);

        Assert.Equal(
            new[]
            {
                Electron2D.SdlRendererDrawCommandKind.Texture,
                Electron2D.SdlRendererDrawCommandKind.Line,
                Electron2D.SdlRendererDrawCommandKind.Rect,
                Electron2D.SdlRendererDrawCommandKind.Circle,
                Electron2D.SdlRendererDrawCommandKind.Polygon,
                Electron2D.SdlRendererDrawCommandKind.Texture,
                Electron2D.SdlRendererDrawCommandKind.Text,
                Electron2D.SdlRendererDrawCommandKind.Text
            },
            framePlan.Commands.Select(command => command.Kind).ToArray());

        Assert.Equal(
            new[]
            {
                "SDL_RenderTextureRotated",
                "SDL_RenderLine",
                "SDL_RenderRect",
                "SDL_RenderGeometryCircle",
                "SDL_RenderGeometry",
                "SDL_RenderTexture",
                "SDL_ttf_RenderText",
                "SDL_ttf_RenderText"
            },
            framePlan.Commands.Select(command => command.SdlOperation).ToArray());

        var sprite = framePlan.Commands[0];
        Assert.Equal("sprite", sprite.DebugName);
        Assert.Equal(new Electron2D.Rect2(2f, 1f, 8f, 4f), sprite.SourceRect);
        Assert.Equal(new Electron2D.Rect2(0f, 0f, 8f, 4f), sprite.DestinationRect);
        Assert.Equal(new Electron2D.Vector2(4f, 6f), sprite.Transform.Origin);
        Assert.True(sprite.FlipH);
        Assert.False(sprite.FlipV);

        var circle = framePlan.Commands[3];
        Assert.Equal(new Electron2D.Vector2(5f, 6f), circle.Position);
        Assert.Equal(7f, circle.Radius);
        Assert.True(circle.Filled);

        var polygon = framePlan.Commands[4];
        Assert.Equal(3, polygon.Points.Count);
        Assert.Equal(3, polygon.Colors.Count);
        Assert.Equal(3, polygon.Uvs.Count);
        Assert.True(polygon.UsesTexture);

        var labelText = framePlan.Commands[7];
        Assert.Equal("UI", labelText.Text);
        Assert.Equal(Electron2D.HorizontalAlignment.Center, labelText.Alignment);
        Assert.Equal(40f, labelText.TextWidth);
        Assert.Equal(10, labelText.FontSize);
        Assert.Equal(2, labelText.GlyphCount);
    }

    [Fact]
    public void CompatibilityBackendReportsDocumentedLimitationsAndFeaturePolicy()
    {
        var backend = new Electron2D.CompatibilityRenderingBackend();
        var framePlan = backend.CreateFramePlan(new Electron2D.CanvasItemRenderPlan(Array.Empty<Electron2D.CanvasItemRenderCommand>(), Array.Empty<Electron2D.CanvasItemRenderBatch>()));

        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.Sprites));
        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.Animation));
        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.TileMap));
        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.Ui));
        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.Text));
        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.Primitives));
        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.Camera));
        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.Clipping));
        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.StandardBlendModes));

        Assert.False(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.RenderTargets));
        Assert.False(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.CustomShaders));
        Assert.False(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.ShaderMaterial));
        Assert.False(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.MultiPass));
        Assert.False(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.AdvancedBlending));
        Assert.False(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.PostProcessing));

        Assert.Equal(
            new[]
            {
                "custom-shaders-unsupported",
                "shader-material-unsupported",
                "post-processing-unsupported",
                "render-targets-not-guaranteed",
                "primitive-antialiasing-approximated",
                "pixel-perfect-standard-parity-not-guaranteed"
            },
            backend.Limitations);
        Assert.Equal(backend.Limitations, framePlan.Limitations);
    }
}

internal static class CompatibilityReferenceScene
{
    public static Electron2D.SceneTree Create()
    {
        var texture = new Electron2D.RuntimeTexture2D(16, 8, hasAlpha: true);
        var font = new ReferenceFont();
        var tree = new Electron2D.SceneTree();

        tree.Root.AddChild(new Electron2D.Sprite2D
        {
            Name = "sprite",
            Texture = texture,
            Position = new Electron2D.Vector2(4f, 6f),
            Centered = false,
            RegionEnabled = true,
            RegionRect = new Electron2D.Rect2(2f, 1f, 8f, 4f),
            FlipH = true
        });

        tree.Root.AddChild(new ReferenceDrawNode(texture, font)
        {
            Name = "draw-all",
            Position = new Electron2D.Vector2(10f, 20f)
        });

        var label = new Electron2D.Label
        {
            Name = "ui-label",
            Text = "UI",
            Position = new Electron2D.Vector2(2f, 4f),
            Size = new Electron2D.Vector2(40f, 14f),
            HorizontalAlignment = Electron2D.HorizontalAlignment.Center,
            VerticalAlignment = Electron2D.VerticalAlignment.Center
        };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", 10);
        tree.Root.AddChild(label);

        return tree;
    }

    private sealed class ReferenceDrawNode : Electron2D.Node2D
    {
        private readonly Electron2D.Texture2D texture;
        private readonly Electron2D.Font font;

        public ReferenceDrawNode(Electron2D.Texture2D texture, Electron2D.Font font)
        {
            this.texture = texture;
            this.font = font;
        }

        public override void _Draw()
        {
            DrawLine(Electron2D.Vector2.Zero, new Electron2D.Vector2(4f, 0f), new Electron2D.Color(1f, 0f, 0f), width: 2f, antialiased: true);
            DrawRect(new Electron2D.Rect2(1f, 2f, 3f, 4f), new Electron2D.Color(0f, 1f, 0f), filled: false, width: 3f, antialiased: true);
            DrawCircle(new Electron2D.Vector2(5f, 6f), 7f, new Electron2D.Color(0f, 0f, 1f));
            DrawPolygon(
                new[] { Electron2D.Vector2.Zero, new Electron2D.Vector2(4f, 0f), new Electron2D.Vector2(0f, 4f) },
                new[] { Electron2D.Color.White, new Electron2D.Color(1f, 0f, 0f), new Electron2D.Color(0f, 1f, 0f) },
                new[] { Electron2D.Vector2.Zero, Electron2D.Vector2.Right, Electron2D.Vector2.Down },
                texture);
            DrawTexture(texture, new Electron2D.Vector2(10f, 11f), new Electron2D.Color(0.8f, 0.8f, 1f));
            DrawString(font, new Electron2D.Vector2(12f, 13f), "Hello", Electron2D.HorizontalAlignment.Center, width: 120f, fontSize: 20);
        }
    }

    private sealed class ReferenceFont : Electron2D.Font
    {
    }
}
