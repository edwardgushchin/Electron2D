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
using SDL3;

namespace Electron2D;

internal static class RuntimeShaderCrossService
{
    private static readonly object SyncRoot = new();
    private static readonly RuntimeShaderCrossLifetime ProductionLifetime = new(RuntimeSdlShaderCrossApi.Instance);
    private static RuntimeShaderCrossLifetime activeLifetime = ProductionLifetime;

    public static RuntimeShaderCrossLease Acquire()
    {
        lock (SyncRoot)
        {
            return activeLifetime.Acquire();
        }
    }

    internal static void ShutdownOnRenderThread()
    {
        lock (SyncRoot)
        {
            activeLifetime.ShutdownOnRenderThread();
        }
    }

    internal static IDisposable UseApiForTests(IRuntimeShaderCrossApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        lock (SyncRoot)
        {
            if (activeLifetime.ActiveLeases > 0)
            {
                throw new InvalidOperationException("Runtime shader compiler test API cannot be replaced while initialized.");
            }

            var previous = activeLifetime;
            var testLifetime = new RuntimeShaderCrossLifetime(api);
            activeLifetime = testLifetime;
            return new RuntimeShaderCrossApiScope(previous, testLifetime);
        }
    }

    internal static void Release(RuntimeShaderCrossLifetime lifetime)
    {
        lock (SyncRoot)
        {
            lifetime.Release();
        }
    }

    private sealed class RuntimeShaderCrossApiScope(
        RuntimeShaderCrossLifetime previous,
        RuntimeShaderCrossLifetime testLifetime) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (!ReferenceEquals(activeLifetime, testLifetime))
                {
                    throw new InvalidOperationException("Runtime shader compiler test API scopes must be disposed in reverse creation order.");
                }

                testLifetime.ShutdownForTestScope();
                activeLifetime = previous;
                disposed = true;
            }
        }
    }
}

internal sealed class RuntimeShaderCrossLifetime(IRuntimeShaderCrossApi shaderCrossApi)
{
    private bool initialized;
    private bool shutdownCompleted;
    private int renderThreadId;
    private int activeLeases;

    public int ActiveLeases => activeLeases;

    public RuntimeShaderCrossLease Acquire()
    {
        if (shutdownCompleted)
        {
            throw new GpuPresenterUnavailableException("Runtime shader compiler was already shut down for this process.");
        }

        if (initialized)
        {
            if (renderThreadId != Environment.CurrentManagedThreadId)
            {
                throw new GpuPresenterUnavailableException("Runtime shader compiler must be used from the render-owning thread.");
            }

            activeLeases++;
            return new RuntimeShaderCrossLease(this);
        }

        if (!shaderCrossApi.Init())
        {
            throw new GpuPresenterUnavailableException("Runtime shader compiler initialization failed: " + shaderCrossApi.GetError());
        }

        initialized = true;
        renderThreadId = Environment.CurrentManagedThreadId;
        activeLeases = 1;
        return new RuntimeShaderCrossLease(this);
    }

    public void Release()
    {
        if (activeLeases > 0)
        {
            activeLeases--;
        }
    }

    public void ShutdownOnRenderThread()
    {
        if (!initialized)
        {
            shutdownCompleted = true;
            return;
        }

        if (renderThreadId != Environment.CurrentManagedThreadId)
        {
            throw new InvalidOperationException("Runtime shader compiler shutdown must run on the render-owning thread.");
        }

        if (activeLeases > 0)
        {
            throw new InvalidOperationException("Runtime shader compiler shutdown cannot run while a presenter lease is active.");
        }

        shaderCrossApi.Quit();
        initialized = false;
        shutdownCompleted = true;
        renderThreadId = 0;
    }

    public void ShutdownForTestScope()
    {
        if (activeLeases > 0)
        {
            throw new InvalidOperationException("Runtime shader compiler test API scope cannot be disposed while a presenter lease is active.");
        }

        if (initialized)
        {
            shaderCrossApi.Quit();
            initialized = false;
            renderThreadId = 0;
        }

        activeLeases = 0;
    }
}

internal static class RuntimeApplicationServices
{
    internal static void ShutdownOnRenderThread()
    {
        RuntimeShaderCrossService.ShutdownOnRenderThread();
    }
}

internal interface IRuntimeShaderCrossApi
{
    bool Init();

    void Quit();

    string GetError();
}

internal sealed class RuntimeSdlShaderCrossApi : IRuntimeShaderCrossApi
{
    public static readonly RuntimeSdlShaderCrossApi Instance = new();

    private RuntimeSdlShaderCrossApi()
    {
    }

    public bool Init()
    {
        return ShaderCross.Init();
    }

    public void Quit()
    {
        ShaderCross.Quit();
    }

    public string GetError()
    {
        return SDL.GetError();
    }
}

internal sealed class RuntimeShaderCrossLease : IDisposable
{
    private readonly RuntimeShaderCrossLifetime lifetime;
    private bool disposed;

    internal RuntimeShaderCrossLease(RuntimeShaderCrossLifetime lifetime)
    {
        this.lifetime = lifetime;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        RuntimeShaderCrossService.Release(lifetime);
    }
}
