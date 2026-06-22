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
namespace Electron2D.Editor;

internal sealed class EditorApplication
{
    private const int DefaultViewportWidth = 1280;
    private const int DefaultViewportHeight = 720;

    public EditorStartupResult Start()
    {
        var tree = new Electron2D.SceneTree();
        var viewport = (Electron2D.Viewport)tree.Root;
        viewport.Size = new Electron2D.Vector2I(DefaultViewportWidth, DefaultViewportHeight);

        var shell = CreateShell();
        viewport.AddChild(shell);

        return new EditorStartupResult(
            typeof(Electron2D.Object).Assembly.GetName().Name ?? "Electron2D",
            tree.Root.Name,
            viewport.Size,
            shell.GetType().FullName ?? shell.GetType().Name,
            shell.GetChildCount(),
            Electron2D.RenderingServer.CurrentProfile.ToString());
    }

    private static Electron2D.Panel CreateShell()
    {
        var shell = new Electron2D.Panel
        {
            Name = "EditorRoot",
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 0f,
            OffsetTop = 0f,
            OffsetRight = 0f,
            OffsetBottom = 0f
        };
        var title = new Electron2D.Label
        {
            Name = "EditorTitle",
            Text = "Electron2D Editor",
            Position = new Electron2D.Vector2(16f, 16f),
            Size = new Electron2D.Vector2(360f, 32f)
        };

        shell.AddChild(title);

        return shell;
    }
}
