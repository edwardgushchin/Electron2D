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

public class Resource : RefCounted
{
    private string _resourceName = string.Empty;
    private string _resourcePath = string.Empty;
    private string _resourceSceneUniqueId = string.Empty;
    private bool _resourceLocalToScene;

    public string ResourceName
    {
        get
        {
            ThrowIfFreed();
            return _resourceName;
        }
        set
        {
            ThrowIfFreed();
            _resourceName = value ?? string.Empty;
        }
    }

    public string ResourcePath
    {
        get
        {
            ThrowIfFreed();
            return _resourcePath;
        }
        protected set
        {
            ThrowIfFreed();
            _resourcePath = value ?? string.Empty;
        }
    }

    public bool ResourceLocalToScene
    {
        get
        {
            ThrowIfFreed();
            return _resourceLocalToScene;
        }
        set
        {
            ThrowIfFreed();
            _resourceLocalToScene = value;
        }
    }

    public string ResourceSceneUniqueId
    {
        get
        {
            ThrowIfFreed();
            return _resourceSceneUniqueId;
        }
        set
        {
            ThrowIfFreed();
            _resourceSceneUniqueId = value ?? string.Empty;
        }
    }

    public void TakeOverPath(string path)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ResourcePath = path;
    }
}
