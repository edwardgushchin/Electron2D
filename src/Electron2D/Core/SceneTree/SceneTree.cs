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
using System.Reflection;

namespace Electron2D;

public class SceneTree : Object
{
    private readonly List<SceneTreeDiagnostic> _diagnostics = new();
    private readonly Queue<DeferredCallQueue.DeferredCall> _deferredCalls = new();
    private readonly List<Node> _deleteQueue = new();
    private int _traversalDepth;
    private bool _flushingQueues;
    private PackedScene? _pendingScene;

    public SceneTree()
    {
        Root = new Viewport { Name = "root" };
        AttachSubtree(Root);
    }

    public Node Root { get; }

    public Node? CurrentScene { get; private set; }

    public Node? GetFirstNodeInGroup(string group)
    {
        return GetNodesInGroup(group).FirstOrDefault();
    }

    public int GetNodeCountInGroup(string group)
    {
        return GetNodesInGroup(group).Length;
    }

    public Node[] GetNodesInGroup(string group)
    {
        var groupName = Node.ValidateGroupName(group);
        var nodes = new List<Node>();
        Root.CollectNodesInGroup(groupName, nodes);
        return nodes.ToArray();
    }

    public bool HasGroup(string name)
    {
        return GetNodeCountInGroup(name) > 0;
    }

    public void CallGroup(string group, string method, params object?[] args)
    {
        var groupName = Node.ValidateGroupName(group);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        var callArguments = args ?? Array.Empty<object?>();

        foreach (var node in GetNodesInGroup(groupName))
        {
            var callableMethod = FindCallableMethod(node, method, callArguments);
            if (callableMethod is null)
            {
                continue;
            }

            InvokeUserCallback(
                node,
                method,
                () => InvokeMethod(callableMethod, node, callArguments),
                RuntimeUserCodeFailureKind.GroupCall);
        }
    }

    public Error ChangeSceneToPacked(PackedScene packedScene)
    {
        if (packedScene is null || !packedScene.CanInstantiate())
        {
            return Error.InvalidParameter;
        }

        if (CurrentScene is not null)
        {
            var currentScene = CurrentScene;
            CurrentScene = null;

            if (currentScene.GetParent() is not null)
            {
                Root.RemoveChild(currentScene);
            }

            QueueDelete(currentScene);
        }

        _pendingScene = packedScene;
        return Error.Ok;
    }

    internal IReadOnlyList<SceneTreeDiagnostic> Diagnostics => _diagnostics;

    internal void AttachSubtree(Node node)
    {
        RunTraversal(() =>
        {
            node.EnterTreeRecursive(this);
            node.ReadyRecursive();
        });
    }

    internal void ProcessFrame(double delta)
    {
        RunTraversal(() =>
        {
            Root.ProcessRecursive(delta);
            Root.DrawRecursive();
        });
    }

    internal void PhysicsFrame(double delta)
    {
        RunTraversal(() => Root.PhysicsProcessRecursive(delta));
    }

    internal void DispatchInput(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        RunTraversal(() => Root.InputRecursive(inputEvent));
    }

    internal void QueueDelete(Node node)
    {
        if (!_deleteQueue.Contains(node))
        {
            _deleteQueue.Add(node);
        }
    }

    internal void QueueDeferredCall(Callable callable, object?[] args)
    {
        _deferredCalls.Enqueue(new DeferredCallQueue.DeferredCall(callable, args));
    }

    internal void InvokeUserCallback(
        Node node,
        string callback,
        Action action,
        RuntimeUserCodeFailureKind kind = RuntimeUserCodeFailureKind.LifecycleCallback)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            ReportUserCodeException(node, callback, exception, kind);
        }
    }

    internal void ReportUserCodeException(
        Node? node,
        string callback,
        Exception exception,
        RuntimeUserCodeFailureKind kind)
    {
        _diagnostics.Add(new SceneTreeDiagnostic(node, callback, kind, exception));
    }

    private void RunTraversal(Action action)
    {
        _traversalDepth++;
        try
        {
            using (DeferredCallQueue.EnterTree(this))
            {
                action();
            }
        }
        finally
        {
            _traversalDepth--;
            if (_traversalDepth == 0)
            {
                FlushQueues();
            }
        }
    }

    private void FlushQueues()
    {
        if (_flushingQueues)
        {
            return;
        }

        _flushingQueues = true;
        try
        {
            using (DeferredCallQueue.EnterTree(this))
            {
                while (_deferredCalls.Count > 0 ||
                    DeferredCallQueue.HasGlobalPendingCalls ||
                    _deleteQueue.Count > 0 ||
                    _pendingScene is not null)
                {
                    DrainDeferredCalls();
                    DeferredCallQueue.DrainGlobal(this);
                    FlushDeleteQueue();
                    FlushPendingSceneChange();
                }
            }
        }
        finally
        {
            _flushingQueues = false;
        }
    }

    private void DrainDeferredCalls()
    {
        while (_deferredCalls.Count > 0)
        {
            var call = _deferredCalls.Dequeue();
            DeferredCallQueue.Execute(this, call);
        }
    }

    private void FlushDeleteQueue()
    {
        while (_deleteQueue.Count > 0)
        {
            var queuedNodes = _deleteQueue.ToArray();
            _deleteQueue.Clear();

            foreach (var node in queuedNodes)
            {
                if (Object.IsInstanceValid(node))
                {
                    node.Free();
                }
            }
        }
    }

    private void FlushPendingSceneChange()
    {
        var packedScene = _pendingScene;
        if (packedScene is null)
        {
            return;
        }

        _pendingScene = null;
        var nextScene = packedScene.Instantiate();
        if (nextScene is null)
        {
            return;
        }

        Root.AddChild(nextScene);
        CurrentScene = nextScene;
    }

    private static MethodInfo? FindCallableMethod(Node node, string method, object?[] args)
    {
        return node
            .GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(candidate =>
                candidate.Name == method &&
                ParametersMatch(candidate.GetParameters(), args));
    }

    private static bool ParametersMatch(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length != args.Length)
        {
            return false;
        }

        for (var index = 0; index < parameters.Length; index++)
        {
            var argument = args[index];
            var parameterType = parameters[index].ParameterType;
            if (argument is null)
            {
                if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
                {
                    return false;
                }

                continue;
            }

            if (!parameterType.IsInstanceOfType(argument))
            {
                return false;
            }
        }

        return true;
    }

    private static void InvokeMethod(MethodInfo method, Node node, object?[] args)
    {
        try
        {
            method.Invoke(node, args);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }
}

internal sealed record SceneTreeDiagnostic(
    Node? Node,
    string Callback,
    RuntimeUserCodeFailureKind Kind,
    Exception Exception)
{
    public string Message => Exception.Message;

    public string StackTrace => Exception.StackTrace ?? string.Empty;
}
