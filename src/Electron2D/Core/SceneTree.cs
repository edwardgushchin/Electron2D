using System.Reflection;

namespace Electron2D;

public class SceneTree : Object
{
    private readonly List<SceneTreeDiagnostic> _diagnostics = new();
    private readonly List<Node> _deleteQueue = new();
    private int _traversalDepth;
    private bool _flushingQueues;

    public SceneTree()
    {
        Root = new Node { Name = "root" };
        AttachSubtree(Root);
    }

    public Node Root { get; }

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

            InvokeUserCallback(node, method, () => InvokeMethod(callableMethod, node, callArguments));
        }
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
        RunTraversal(() => Root.ProcessRecursive(delta));
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

    internal void InvokeUserCallback(Node node, string callback, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            _diagnostics.Add(new SceneTreeDiagnostic(node, callback, exception));
        }
    }

    private void RunTraversal(Action action)
    {
        _traversalDepth++;
        try
        {
            action();
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
            while (DeferredCallQueue.HasPendingCalls || _deleteQueue.Count > 0)
            {
                DeferredCallQueue.Drain();
                FlushDeleteQueue();
            }
        }
        finally
        {
            _flushingQueues = false;
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

internal sealed record SceneTreeDiagnostic(Node Node, string Callback, Exception Exception);
