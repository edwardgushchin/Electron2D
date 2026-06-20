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

internal static class DeferredCallQueue
{
    private static readonly Queue<DeferredCall> s_globalCalls = new();

    [ThreadStatic]
    private static SceneTree? t_currentTree;

    public static bool HasGlobalPendingCalls
    {
        get
        {
            lock (s_globalCalls)
            {
                return s_globalCalls.Count > 0;
            }
        }
    }

    public static void Enqueue(Callable callable, object?[] args)
    {
        var tree = (callable.GetObject() as Node)?.GetTree() ?? t_currentTree;
        if (tree is not null)
        {
            tree.QueueDeferredCall(callable, args);
            return;
        }

        lock (s_globalCalls)
        {
            s_globalCalls.Enqueue(new DeferredCall(callable, args));
        }
    }

    public static IDisposable EnterTree(SceneTree tree)
    {
        var previousTree = t_currentTree;
        t_currentTree = tree;
        return new TreeScope(previousTree);
    }

    public static void DrainGlobal(SceneTree tree)
    {
        while (true)
        {
            DeferredCall call;
            lock (s_globalCalls)
            {
                if (s_globalCalls.Count == 0)
                {
                    return;
                }

                call = s_globalCalls.Dequeue();
            }

            Execute(tree, call);
        }
    }

    public static void Execute(SceneTree tree, DeferredCall call)
    {
        var result = call.Callable.TryCall(call.Arguments, out _, out var exception);
        if (result != Error.Ok && exception is not null)
        {
            tree.ReportUserCodeException(
                call.Callable.GetObject() as Node,
                call.Callable.GetMethod(),
                exception,
                RuntimeUserCodeFailureKind.DeferredCall);
        }
    }

    internal readonly record struct DeferredCall(Callable Callable, object?[] Arguments);

    private sealed class TreeScope : IDisposable
    {
        private readonly SceneTree? _previousTree;
        private bool _disposed;

        public TreeScope(SceneTree? previousTree)
        {
            _previousTree = previousTree;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            t_currentTree = _previousTree;
            _disposed = true;
        }
    }
}
