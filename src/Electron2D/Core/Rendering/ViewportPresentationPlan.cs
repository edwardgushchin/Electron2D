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

internal sealed class ViewportPresentationPlan
{
    public ViewportPresentationPlan(
        Vector2I logicalWindowSize,
        Vector2I framebufferSize,
        Vector2I renderTargetSize,
        Rect2I viewportRect,
        Vector2 canvasScale,
        Vector2 canvasOffset,
        Transform2D canvasTransform)
    {
        LogicalWindowSize = logicalWindowSize;
        FramebufferSize = framebufferSize;
        RenderTargetSize = renderTargetSize;
        ViewportRect = viewportRect;
        CanvasScale = canvasScale;
        CanvasOffset = canvasOffset;
        CanvasTransform = canvasTransform;
    }

    public Vector2I LogicalWindowSize { get; }

    public Vector2I FramebufferSize { get; }

    public Vector2I RenderTargetSize { get; }

    public Rect2I ViewportRect { get; }

    public Vector2 CanvasScale { get; }

    public Vector2 CanvasOffset { get; }

    public Transform2D CanvasTransform { get; }
}
