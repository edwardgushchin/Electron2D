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
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed class RuntimeSdlRendererFramePresenter : IRuntimeFramePresenter
{
    private const int CircleSegmentCount = 32;

    private readonly IntPtr window;
    private readonly IntPtr renderer;
    private readonly Dictionary<Texture2D, RuntimeSdlTextureResource> textureCache = new(ReferenceEqualityComparer.Instance);
    private Vector2I presentationSize;
    private int textureUploads;
    private int textureCacheHits;
    private int observedPresentationResizes;
    private bool disposed;

    public RuntimeSdlRendererFramePresenter(IntPtr window, Vector2I presentationSize)
    {
        if (window == IntPtr.Zero)
        {
            throw new ArgumentException("Runtime SDL renderer presenter requires a valid window.", nameof(window));
        }

        this.window = window;
        renderer = SDL.CreateRenderer(window, null!);
        if (renderer == IntPtr.Zero)
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback creation failed: " + SDL.GetError());
        }

        this.presentationSize = presentationSize;
    }

    public RuntimePresentedFrame Present(
        CanvasItemRenderPlan renderPlan,
        Vector2I windowSize,
        Color clearColor,
        bool captureFrame)
    {
        ArgumentNullException.ThrowIfNull(renderPlan);
        ThrowIfDisposed();

        UpdateObservedPresentationSize(windowSize);

        SetDrawBlendMode();
        SetDrawColor(clearColor);
        if (!SDL.RenderClear(renderer))
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback clear failed: " + SDL.GetError());
        }

        var actualDrawCalls = 0;
        for (var index = 0; index < renderPlan.Commands.Count; index++)
        {
            var command = renderPlan.Commands[index];
            actualDrawCalls += DrawCommand(command);
        }

        var screenshot = captureFrame ? CaptureCurrentRenderTarget() : null;
        if (!SDL.RenderPresent(renderer))
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback presentation failed: " + SDL.GetError());
        }

        return new RuntimePresentedFrame(
            new RuntimeFrameDiagnostics(
                RuntimeFramePresenter.RenderSource,
                RuntimeFramePresenter.SdlRendererPresentationBackend,
                UsedFallbackPresenter: true,
                FallbackReason: string.Empty,
                renderPlan.DrawCallCount,
                actualDrawCalls,
                CountTextureSwitches(renderPlan),
                CountPipelineSwitches(renderPlan),
                textureUploads,
                textureCacheHits,
                PresentationResourcesCreated: 1,
                PresentationResourcesRecreated: 0,
                observedPresentationResizes,
                PresentationBackendReconfigurations: 0,
                MaxPresenterManagedBytesPerFrame: 0,
                PresenterMeasuredFrames: 0,
                screenshot?.RgbaPixels.Length ?? 0),
            screenshot);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        foreach (var texture in textureCache.Values)
        {
            SDL.DestroyTexture(texture.Handle);
        }

        textureCache.Clear();
        SDL.DestroyRenderer(renderer);
        disposed = true;
    }

    private RuntimeFrameSnapshot CaptureCurrentRenderTarget()
    {
        var surface = SDL.RenderReadPixels(renderer, null);
        if (surface == IntPtr.Zero)
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback screenshot readback failed: " + SDL.GetError());
        }

        var convertedSurface = IntPtr.Zero;
        try
        {
            convertedSurface = SDL.ConvertSurface(surface, SDL.PixelFormat.ABGR8888);
            if (convertedSurface == IntPtr.Zero)
            {
                throw new InvalidOperationException("Runtime SDL renderer fallback screenshot conversion failed: " + SDL.GetError());
            }

            return CopySurfaceToSnapshot(convertedSurface);
        }
        finally
        {
            if (convertedSurface != IntPtr.Zero)
            {
                SDL.DestroySurface(convertedSurface);
            }

            SDL.DestroySurface(surface);
        }
    }

    private static RuntimeFrameSnapshot CopySurfaceToSnapshot(IntPtr surfaceHandle)
    {
        var surface = Marshal.PtrToStructure<SDL.Surface>(surfaceHandle);
        if (surface.Width <= 0 || surface.Height <= 0 || surface.Pixels == IntPtr.Zero)
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback screenshot returned an invalid surface.");
        }

        var width = surface.Width;
        var height = surface.Height;
        var destinationStride = checked(width * 4);
        var pixels = new byte[checked(destinationStride * height)];
        for (var row = 0; row < height; row++)
        {
            Marshal.Copy(
                IntPtr.Add(surface.Pixels, row * surface.Pitch),
                pixels,
                row * destinationStride,
                destinationStride);
        }

        return new RuntimeFrameSnapshot(width, height, pixels);
    }

    private int DrawCommand(CanvasItemRenderCommand command)
    {
        return command.Kind switch
        {
            CanvasItemRenderCommandKind.Texture => DrawTexture(command),
            CanvasItemRenderCommandKind.Line => DrawLine(command),
            CanvasItemRenderCommandKind.Rect => DrawRect(command),
            CanvasItemRenderCommandKind.Circle => DrawCircle(command),
            CanvasItemRenderCommandKind.Polygon => DrawPolygon(command),
            CanvasItemRenderCommandKind.String => DrawString(command),
            _ => throw new InvalidOperationException("Unsupported canvas item render command kind: " + command.Kind + ".")
        };
    }

    private void UpdateObservedPresentationSize(Vector2I requestedWindowSize)
    {
        var observedSize = requestedWindowSize;
        if (SDL.GetWindowSize(window, out var width, out var height))
        {
            observedSize = new Vector2I(width, height);
        }

        if (observedSize == presentationSize)
        {
            return;
        }

        presentationSize = observedSize;
        observedPresentationResizes++;
    }

    private int DrawTexture(CanvasItemRenderCommand command)
    {
        if (command.Texture is null || !TryGetTextureResource(command.Texture, out var texture))
        {
            throw new UnsupportedTextureResourceException(command.Texture);
        }

        var source = RuntimeTextureResolver.NormalizeSourceRect(command.SourceRect, texture.Width, texture.Height);
        var u0 = source.Position.X / texture.Width;
        var v0 = source.Position.Y / texture.Height;
        var u1 = source.End.X / texture.Width;
        var v1 = source.End.Y / texture.Height;
        if (command.FlipH)
        {
            (u0, u1) = (u1, u0);
        }

        if (command.FlipV)
        {
            (v0, v1) = (v1, v0);
        }

        RenderTexturedQuad(command.Transform, command.DestinationRect, texture.Handle, command.EffectiveModulate, u0, v0, u1, v1);
        return 1;
    }

    private int DrawLine(CanvasItemRenderCommand command)
    {
        if (command.Points.Count < 2)
        {
            return 0;
        }

        SetDrawColor(command.EffectiveModulate);
        var from = Transform(command, command.Points[0]);
        var to = Transform(command, command.Points[1]);
        var width = Math.Max(1f, command.Width <= 0f ? 1f : command.Width);
        if (width > 1f)
        {
            var direction = to - from;
            var length = direction.Length();
            if (length <= 0f)
            {
                return 0;
            }

            var normal = new Vector2(-direction.Y / length, direction.X / length) * (width * 0.5f);
            RenderSolidTriangle(from - normal, to - normal, to + normal, command.EffectiveModulate);
            RenderSolidTriangle(from - normal, to + normal, from + normal, command.EffectiveModulate);
            return 2;
        }

        if (!SDL.RenderLine(renderer, from.X, from.Y, to.X, to.Y))
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback line draw failed: " + SDL.GetError());
        }

        return 1;
    }

    private int DrawRect(CanvasItemRenderCommand command)
    {
        SetDrawColor(command.EffectiveModulate);
        var rect = command.DestinationRect;
        if (command.Filled)
        {
            RenderSolidQuad(command.Transform, rect, command.EffectiveModulate);
            return 1;
        }

        return DrawRectOutline(command.Transform, rect);
    }

    private int DrawCircle(CanvasItemRenderCommand command)
    {
        SetDrawColor(command.EffectiveModulate);
        var center = Transform(command, command.Position);
        var radius = Math.Max(1f, command.Radius);
        var previous = Transform(command, command.Position + new Vector2(radius, 0f));
        var calls = 0;
        for (var segment = 1; segment <= CircleSegmentCount; segment++)
        {
            var angle = MathF.Tau * segment / CircleSegmentCount;
            var next = Transform(command, command.Position + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius));
            RenderSolidTriangle(center, previous, next, command.EffectiveModulate);
            previous = next;
            calls++;
        }

        return calls;
    }

    private int DrawPolygon(CanvasItemRenderCommand command)
    {
        if (command.Points.Count < 3)
        {
            return 0;
        }

        var origin = Transform(command, command.Points[0]);
        var originColor = GetPolygonVertexColor(command, 0);
        var calls = 0;
        for (var index = 1; index < command.Points.Count - 1; index++)
        {
            RenderSolidTriangle(
                origin,
                Transform(command, command.Points[index]),
                Transform(command, command.Points[index + 1]),
                originColor,
                GetPolygonVertexColor(command, index),
                GetPolygonVertexColor(command, index + 1));
            calls++;
        }

        return calls;
    }

    private int DrawString(CanvasItemRenderCommand command)
    {
        if (string.IsNullOrEmpty(command.Text))
        {
            return 0;
        }

        SetDrawColor(command.EffectiveModulate);
        var scale = Math.Max(1, (int)MathF.Round(command.FontSize / 8f, MidpointRounding.AwayFromZero));
        var x = command.Position.X;
        var y = command.Position.Y - (RuntimePixelFont.GlyphHeight * scale);
        var calls = 0;
        for (var charIndex = 0; charIndex < command.Text.Length; charIndex++)
        {
            var glyph = RuntimePixelFont.GetGlyph(char.ToUpperInvariant(command.Text[charIndex]));
            for (var row = 0; row < RuntimePixelFont.GlyphHeight; row++)
            {
                var glyphRow = glyph[row];
                for (var column = 0; column < RuntimePixelFont.GlyphWidth; column++)
                {
                    if (glyphRow[column] != '1')
                    {
                        continue;
                    }

                    RenderSolidQuad(
                        command.Transform,
                        new Rect2(x + (column * scale), y + (row * scale), scale, scale),
                        command.EffectiveModulate);
                    calls++;
                }
            }

            x += (RuntimePixelFont.GlyphWidth + 1) * scale;
        }

        return calls;
    }

    private int DrawRectOutline(Transform2D transform, Rect2 rect)
    {
        var topLeft = transform.Xform(rect.Position);
        var topRight = transform.Xform(new Vector2(rect.End.X, rect.Position.Y));
        var bottomRight = transform.Xform(rect.End);
        var bottomLeft = transform.Xform(new Vector2(rect.Position.X, rect.End.Y));
        if (!SDL.RenderLine(renderer, topLeft.X, topLeft.Y, topRight.X, topRight.Y) ||
            !SDL.RenderLine(renderer, topRight.X, topRight.Y, bottomRight.X, bottomRight.Y) ||
            !SDL.RenderLine(renderer, bottomRight.X, bottomRight.Y, bottomLeft.X, bottomLeft.Y) ||
            !SDL.RenderLine(renderer, bottomLeft.X, bottomLeft.Y, topLeft.X, topLeft.Y))
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback rectangle outline failed: " + SDL.GetError());
        }

        return 4;
    }

    private void RenderSolidQuad(Transform2D transform, Rect2 rect, Color color)
    {
        RenderQuad(transform, rect, IntPtr.Zero, color, 0f, 0f, 0f, 0f);
    }

    private void RenderTexturedQuad(
        Transform2D transform,
        Rect2 rect,
        IntPtr texture,
        Color color,
        float u0,
        float v0,
        float u1,
        float v1)
    {
        RenderQuad(transform, rect, texture, color, u0, v0, u1, v1);
    }

    private void RenderQuad(
        Transform2D transform,
        Rect2 rect,
        IntPtr texture,
        Color color,
        float u0,
        float v0,
        float u1,
        float v1)
    {
        var x0 = rect.Position.X;
        var y0 = rect.Position.Y;
        var x1 = rect.Position.X + Math.Max(1f, rect.Size.X);
        var y1 = rect.Position.Y + Math.Max(1f, rect.Size.Y);
        var topLeft = transform.Xform(new Vector2(x0, y0));
        var topRight = transform.Xform(new Vector2(x1, y0));
        var bottomRight = transform.Xform(new Vector2(x1, y1));
        var bottomLeft = transform.Xform(new Vector2(x0, y1));
        var vertexColor = ToFColor(color);
        Span<SDL.Vertex> vertices = stackalloc SDL.Vertex[6];
        vertices[0] = CreateVertex(topLeft, vertexColor, u0, v0);
        vertices[1] = CreateVertex(topRight, vertexColor, u1, v0);
        vertices[2] = CreateVertex(bottomRight, vertexColor, u1, v1);
        vertices[3] = CreateVertex(topLeft, vertexColor, u0, v0);
        vertices[4] = CreateVertex(bottomRight, vertexColor, u1, v1);
        vertices[5] = CreateVertex(bottomLeft, vertexColor, u0, v1);
        if (!SDL.RenderGeometry(renderer, texture, vertices, vertices.Length, IntPtr.Zero, 0))
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback quad draw failed: " + SDL.GetError());
        }
    }

    private void RenderSolidTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        var vertexColor = ToFColor(color);
        RenderSolidTriangle(a, b, c, vertexColor, vertexColor, vertexColor);
    }

    private void RenderSolidTriangle(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Color colorA,
        Color colorB,
        Color colorC)
    {
        RenderSolidTriangle(a, b, c, ToFColor(colorA), ToFColor(colorB), ToFColor(colorC));
    }

    private void RenderSolidTriangle(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        SDL.FColor colorA,
        SDL.FColor colorB,
        SDL.FColor colorC)
    {
        Span<SDL.Vertex> vertices = stackalloc SDL.Vertex[3];
        vertices[0] = CreateVertex(a, colorA, 0f, 0f);
        vertices[1] = CreateVertex(b, colorB, 0f, 0f);
        vertices[2] = CreateVertex(c, colorC, 0f, 0f);
        if (!SDL.RenderGeometry(renderer, IntPtr.Zero, vertices, 3, IntPtr.Zero, 0))
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback geometry draw failed: " + SDL.GetError());
        }
    }

    private static SDL.Vertex CreateVertex(Vector2 position, SDL.FColor color, float u, float v)
    {
        return new SDL.Vertex
        {
            Position = new SDL.FPoint { X = position.X, Y = position.Y },
            Color = color,
            TexCoord = new SDL.FPoint { X = u, Y = v }
        };
    }

    private bool TryGetTextureResource(Texture2D texture, out RuntimeSdlTextureResource resource)
    {
        var contentVersion = texture.RenderContentVersion;
        if (textureCache.TryGetValue(texture, out resource))
        {
            if (resource.ContentVersion == contentVersion)
            {
                textureCacheHits++;
                return true;
            }

            SDL.DestroyTexture(resource.Handle);
            textureCache.Remove(texture);
        }

        if (!RuntimeTextureResolver.TryCreateTexturePixels(texture, out var width, out var height, out var pixels))
        {
            resource = default;
            return false;
        }

        var handle = SDL.CreateTexture(renderer, SDL.PixelFormat.ABGR8888, SDL.TextureAccess.Static, width, height);
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback texture creation failed: " + SDL.GetError());
        }

        if (!SDL.SetTextureBlendMode(handle, SDL.BlendMode.Blend))
        {
            SDL.DestroyTexture(handle);
            throw new InvalidOperationException("Runtime SDL renderer fallback texture blend setup failed: " + SDL.GetError());
        }

        if (!SDL.UpdateTexture(handle, IntPtr.Zero, pixels, width * 4))
        {
            SDL.DestroyTexture(handle);
            throw new InvalidOperationException("Runtime SDL renderer fallback texture upload failed: " + SDL.GetError());
        }

        resource = new RuntimeSdlTextureResource(handle, width, height, contentVersion);
        textureCache.Add(texture, resource);
        textureUploads++;
        return true;
    }

    private static int CountTextureSwitches(CanvasItemRenderPlan renderPlan)
    {
        var switches = 0;
        Rid? previous = null;
        for (var index = 0; index < renderPlan.Commands.Count; index++)
        {
            var command = renderPlan.Commands[index];
            if (!command.BatchKey.Texture.IsValid())
            {
                previous = null;
                continue;
            }

            if (previous is null || previous.Value != command.BatchKey.Texture)
            {
                switches++;
                previous = command.BatchKey.Texture;
            }
        }

        return switches;
    }

    private static int CountPipelineSwitches(CanvasItemRenderPlan renderPlan)
    {
        var switches = 0;
        bool? previousTextured = null;
        for (var index = 0; index < renderPlan.Batches.Count; index++)
        {
            var batch = renderPlan.Batches[index];
            var textured = batch.Key.Texture.IsValid();
            if (previousTextured is null || previousTextured.Value != textured)
            {
                switches++;
                previousTextured = textured;
            }
        }

        return switches;
    }

    private static SDL.FRect ToSourceRect(Rect2 sourceRect, int textureWidth, int textureHeight)
    {
        var width = sourceRect.Size.X > 0f ? sourceRect.Size.X : textureWidth;
        var height = sourceRect.Size.Y > 0f ? sourceRect.Size.Y : textureHeight;
        return new SDL.FRect
        {
            X = sourceRect.Position.X,
            Y = sourceRect.Position.Y,
            W = width,
            H = height
        };
    }

    private static SDL.FRect ToFRect(Rect2 rect)
    {
        return new SDL.FRect
        {
            X = rect.Position.X,
            Y = rect.Position.Y,
            W = Math.Max(1f, rect.Size.X),
            H = Math.Max(1f, rect.Size.Y)
        };
    }

    private static Rect2 Transform(CanvasItemRenderCommand command, Rect2 rect)
    {
        return command.Transform * rect;
    }

    private static Vector2 Transform(CanvasItemRenderCommand command, Vector2 point)
    {
        return command.Transform.Xform(point);
    }

    private static Color GetPolygonVertexColor(CanvasItemRenderCommand command, int index)
    {
        var vertexColor = index >= 0 && index < command.Colors.Count
            ? command.Colors[index]
            : Color.White;
        return command.EffectiveModulate * vertexColor;
    }

    private void SetDrawColor(Color color)
    {
        if (!SDL.SetRenderDrawColor(
            renderer,
            ToByte(color.R),
            ToByte(color.G),
            ToByte(color.B),
            ToByte(color.A)))
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback draw color setup failed: " + SDL.GetError());
        }
    }

    private void SetDrawBlendMode()
    {
        if (!SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.Blend))
        {
            throw new InvalidOperationException("Runtime SDL renderer fallback blend setup failed: " + SDL.GetError());
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(RuntimeSdlRendererFramePresenter));
        }
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp((int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f, MidpointRounding.AwayFromZero), 0, 255);
    }

    private static SDL.FColor ToFColor(Color color)
    {
        return new SDL.FColor
        {
            R = Math.Clamp(color.R, 0f, 1f),
            G = Math.Clamp(color.G, 0f, 1f),
            B = Math.Clamp(color.B, 0f, 1f),
            A = Math.Clamp(color.A, 0f, 1f)
        };
    }

    private readonly record struct RuntimeSdlTextureResource(IntPtr Handle, int Width, int Height, long ContentVersion);
}
