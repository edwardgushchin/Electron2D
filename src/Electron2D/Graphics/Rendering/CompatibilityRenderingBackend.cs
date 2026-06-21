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
namespace Electron2D;

internal sealed class CompatibilityRenderingBackend : RenderingBackend
{
    private static readonly RenderingServer.RenderingFeature[] CompatibilityFeatures =
    {
        RenderingServer.RenderingFeature.Sprites,
        RenderingServer.RenderingFeature.Animation,
        RenderingServer.RenderingFeature.TileMap,
        RenderingServer.RenderingFeature.Ui,
        RenderingServer.RenderingFeature.Text,
        RenderingServer.RenderingFeature.Primitives,
        RenderingServer.RenderingFeature.Camera,
        RenderingServer.RenderingFeature.Clipping,
        RenderingServer.RenderingFeature.StandardBlendModes
    };

    private static readonly string[] CompatibilityLimitations =
    {
        "custom-shaders-unsupported",
        "shader-material-unsupported",
        "post-processing-unsupported",
        "render-targets-not-guaranteed",
        "primitive-antialiasing-approximated",
        "pixel-perfect-standard-parity-not-guaranteed"
    };

    public CompatibilityRenderingBackend()
        : base(
            "Compatibility",
            RenderingServer.RenderingProfile.Compatibility,
            CompatibilityFeatures)
    {
    }

    internal IReadOnlyList<string> Limitations => CompatibilityLimitations;

    internal SdlRendererFramePlan CreateFramePlan(CanvasItemRenderPlan renderPlan)
    {
        ArgumentNullException.ThrowIfNull(renderPlan);

        var commands = renderPlan.Commands
            .Select(SdlRendererDrawCommand.FromCanvasCommand)
            .ToArray();
        var limitations = BuildLimitations(commands);

        return new SdlRendererFramePlan(
            Name,
            Profile,
            CompatibilityFeatures,
            renderPlan.DrawCallCount,
            commands,
            limitations);
    }

    private static IReadOnlyList<string> BuildLimitations(IReadOnlyList<SdlRendererDrawCommand> commands)
    {
        var limitations = CompatibilityLimitations.ToList();
        if (commands.Any(command => command.Kind == SdlRendererDrawCommandKind.Circle))
        {
            limitations.Add("circle-rendered-as-segmented-geometry");
        }

        return limitations;
    }
}
