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
using System.Text;

namespace Electron2D;

internal static class SdlRendererFramePlanTextSerializer
{
    public static string Serialize(SdlRendererFramePlan framePlan)
    {
        ArgumentNullException.ThrowIfNull(framePlan);

        var builder = new StringBuilder();
        builder
            .Append("backend=").Append(framePlan.BackendName)
            .Append("|profile=").Append(framePlan.Profile)
            .Append("|drawCalls=").Append(MathFormatting.Format(framePlan.DrawCallCount))
            .Append("|commands=").Append(MathFormatting.Format(framePlan.Commands.Count))
            .Append('\n');

        builder
            .Append("features=")
            .AppendJoin(',', framePlan.Features)
            .Append('\n');

        builder
            .Append("limitations=")
            .AppendJoin(';', framePlan.Limitations);

        for (var index = 0; index < framePlan.Commands.Count; index++)
        {
            var command = framePlan.Commands[index];
            builder
                .Append('\n')
                .Append(MathFormatting.Format(index))
                .Append('|').Append(command.Kind)
                .Append("|op=").Append(command.SdlOperation)
                .Append("|debug=").Append(Escape(command.DebugName))
                .Append("|layer=").Append(MathFormatting.Format(command.Layer))
                .Append("|z=").Append(MathFormatting.Format(command.ZIndex))
                .Append("|tree=").Append(command.TreeOrder)
                .Append("|origin=").Append(Format(command.Transform.Origin))
                .Append("|src=").Append(Format(command.SourceRect))
                .Append("|dst=").Append(Format(command.DestinationRect))
                .Append("|pos=").Append(Format(command.Position))
                .Append("|points=").Append(FormatPoints(command.Points))
                .Append("|colors=").Append(FormatColors(command.Colors))
                .Append("|uvs=").Append(FormatPoints(command.Uvs))
                .Append("|modulate=").Append(Format(command.Modulate))
                .Append("|width=").Append(MathFormatting.Format(command.Width))
                .Append("|radius=").Append(MathFormatting.Format(command.Radius))
                .Append("|filled=").Append(command.Filled)
                .Append("|aa=").Append(command.Antialiased)
                .Append("|flipH=").Append(command.FlipH)
                .Append("|flipV=").Append(command.FlipV)
                .Append("|text=").Append(Escape(command.Text))
                .Append("|align=").Append(command.Alignment)
                .Append("|textWidth=").Append(MathFormatting.Format(command.TextWidth))
                .Append("|fontSize=").Append(MathFormatting.Format(command.FontSize))
                .Append("|glyphs=").Append(MathFormatting.Format(command.GlyphCount))
                .Append("|usesTexture=").Append(command.UsesTexture);
        }

        return builder.ToString();
    }

    private static string Format(Vector2 value)
    {
        return $"{MathFormatting.Format(value.X)},{MathFormatting.Format(value.Y)}";
    }

    private static string Format(Rect2 value)
    {
        return $"{Format(value.Position)},{Format(value.Size)}";
    }

    private static string Format(Color value)
    {
        return $"{MathFormatting.Format(value.R)},{MathFormatting.Format(value.G)},{MathFormatting.Format(value.B)},{MathFormatting.Format(value.A)}";
    }

    private static string FormatPoints(IReadOnlyList<Vector2> values)
    {
        return string.Join(';', values.Select(Format));
    }

    private static string FormatColors(IReadOnlyList<Color> values)
    {
        return string.Join(';', values.Select(Format));
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal);
    }
}
