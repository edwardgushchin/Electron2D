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

namespace Electron2D.Tests.GoldenData;

public sealed class SdlRendererCompatibilityGoldenTests
{
    [Fact]
    public void CompatibilityReferenceSceneMatchesGoldenCommandStream()
    {
        var tree = CreateReferenceScene();
        tree.ProcessFrame(1.0 / 60.0);
        var renderPlan = new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root);
        var framePlan = new Electron2D.CompatibilityRenderingBackend().CreateFramePlan(renderPlan);

        var actual = Electron2D.SdlRendererFramePlanTextSerializer.Serialize(framePlan);

        const string expected =
            "backend=Compatibility|profile=Compatibility|drawCalls=7|commands=8\n" +
            "features=Sprites,Animation,TileMap,Ui,Text,Primitives,Camera,Clipping,StandardBlendModes\n" +
            "limitations=custom-shaders-unsupported;shader-material-unsupported;post-processing-unsupported;render-targets-not-guaranteed;primitive-antialiasing-approximated;pixel-perfect-standard-parity-not-guaranteed;circle-rendered-as-segmented-geometry\n" +
            "0|Texture|op=SDL_RenderTextureRotated|debug=sprite|layer=0|z=0|tree=0|origin=4,6|src=2,1,8,4|dst=0,0,8,4|pos=0,0|points=|colors=|uvs=|modulate=1,1,1,1|width=-1|radius=0|filled=True|aa=False|flipH=True|flipV=False|text=|align=Left|textWidth=-1|fontSize=16|glyphs=0|usesTexture=True\n" +
            "1|Line|op=SDL_RenderLine|debug=draw-all|layer=0|z=0|tree=1|origin=10,20|src=0,0,0,0|dst=0,0,0,0|pos=0,0|points=0,0;4,0|colors=|uvs=|modulate=1,0,0,1|width=2|radius=0|filled=True|aa=True|flipH=False|flipV=False|text=|align=Left|textWidth=-1|fontSize=16|glyphs=0|usesTexture=False\n" +
            "2|Rect|op=SDL_RenderRect|debug=draw-all|layer=0|z=0|tree=2|origin=10,20|src=0,0,0,0|dst=1,2,3,4|pos=0,0|points=|colors=|uvs=|modulate=0,1,0,1|width=3|radius=0|filled=False|aa=True|flipH=False|flipV=False|text=|align=Left|textWidth=-1|fontSize=16|glyphs=0|usesTexture=False\n" +
            "3|Circle|op=SDL_RenderGeometryCircle|debug=draw-all|layer=0|z=0|tree=3|origin=10,20|src=0,0,0,0|dst=0,0,0,0|pos=5,6|points=|colors=|uvs=|modulate=0,0,1,1|width=-1|radius=7|filled=True|aa=False|flipH=False|flipV=False|text=|align=Left|textWidth=-1|fontSize=16|glyphs=0|usesTexture=False\n" +
            "4|Polygon|op=SDL_RenderGeometry|debug=draw-all|layer=0|z=0|tree=4|origin=10,20|src=0,0,0,0|dst=0,0,0,0|pos=0,0|points=0,0;4,0;0,4|colors=1,1,1,1;1,0,0,1;0,1,0,1|uvs=0,0;1,0;0,1|modulate=1,1,1,1|width=-1|radius=0|filled=True|aa=False|flipH=False|flipV=False|text=|align=Left|textWidth=-1|fontSize=16|glyphs=0|usesTexture=True\n" +
            "5|Texture|op=SDL_RenderTexture|debug=draw-all|layer=0|z=0|tree=5|origin=10,20|src=0,0,16,8|dst=10,11,16,8|pos=10,11|points=|colors=|uvs=|modulate=0.8,0.8,1,1|width=-1|radius=0|filled=True|aa=False|flipH=False|flipV=False|text=|align=Left|textWidth=-1|fontSize=16|glyphs=0|usesTexture=True\n" +
            "6|Text|op=SDL_ttf_RenderText|debug=draw-all|layer=0|z=0|tree=6|origin=10,20|src=0,0,0,0|dst=47,-3,50,20|pos=12,13|points=|colors=|uvs=|modulate=1,1,1,1|width=-1|radius=0|filled=True|aa=False|flipH=False|flipV=False|text=Hello|align=Center|textWidth=120|fontSize=20|glyphs=5|usesTexture=False\n" +
            "7|Text|op=SDL_ttf_RenderText|debug=ui-label|layer=0|z=0|tree=7|origin=2,4|src=0,0,0,0|dst=15,2,10,10|pos=0,10|points=|colors=|uvs=|modulate=1,1,1,1|width=-1|radius=0|filled=True|aa=False|flipH=False|flipV=False|text=UI|align=Center|textWidth=40|fontSize=10|glyphs=2|usesTexture=False";

        Assert.Equal(expected, actual);
    }

    private static Electron2D.SceneTree CreateReferenceScene()
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
