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
using System.Threading;

namespace Electron2D;

public class Object
{
    private static long s_nextInstanceId;

    private readonly Dictionary<string, List<SignalConnection>> _signalConnections = new(StringComparer.Ordinal);
    private readonly HashSet<string> _signals = new(StringComparer.Ordinal);
    private readonly ulong _instanceId = (ulong)Interlocked.Increment(ref s_nextInstanceId);
    private bool _freed;
    private bool _freeing;
    private bool _queuedForDeletion;

    public ulong GetInstanceId()
    {
        return _instanceId;
    }

    public void Free()
    {
        if (_freed || _freeing)
        {
            return;
        }

        _freeing = true;
        try
        {
            OnFree();
        }
        finally
        {
            _freed = true;
            _freeing = false;
            _queuedForDeletion = false;
        }
    }

    public static bool IsInstanceValid(Object? instance)
    {
        return instance is not null && !instance._freed;
    }

    public bool IsQueuedForDeletion()
    {
        return _queuedForDeletion;
    }

    public void AddUserSignal(string signal)
    {
        ThrowIfFreed();
        _signals.Add(ValidateSignalName(signal));
    }

    public bool HasSignal(string signal)
    {
        ThrowIfFreed();
        return _signals.Contains(ValidateSignalName(signal));
    }

    public Error Connect(string signal, Callable callable, ConnectFlags flags = ConnectFlags.None)
    {
        ThrowIfFreed();
        var signalName = ValidateSignalName(signal);
        if (!_signals.Contains(signalName))
        {
            return Error.Unavailable;
        }

        if (callable.IsNull())
        {
            return Error.InvalidParameter;
        }

        if (!_signalConnections.TryGetValue(signalName, out var connections))
        {
            connections = new List<SignalConnection>();
            _signalConnections.Add(signalName, connections);
        }

        var existing = connections.FirstOrDefault(connection => connection.Callable == callable);
        if (existing is not null)
        {
            if (flags.HasFlag(ConnectFlags.ReferenceCounted))
            {
                existing.ReferenceCount++;
                return Error.Ok;
            }

            return Error.AlreadyExists;
        }

        connections.Add(new SignalConnection(callable, flags));
        return Error.Ok;
    }

    public void Disconnect(string signal, Callable callable)
    {
        ThrowIfFreed();
        var signalName = ValidateSignalName(signal);
        if (!_signalConnections.TryGetValue(signalName, out var connections))
        {
            return;
        }

        var connection = connections.FirstOrDefault(item => item.Callable == callable);
        if (connection is null)
        {
            return;
        }

        if (connection.ReferenceCount > 1)
        {
            connection.ReferenceCount--;
            return;
        }

        connections.Remove(connection);
        if (connections.Count == 0)
        {
            _signalConnections.Remove(signalName);
        }
    }

    public bool IsConnected(string signal, Callable callable)
    {
        ThrowIfFreed();
        var signalName = ValidateSignalName(signal);
        return _signalConnections.TryGetValue(signalName, out var connections) &&
            connections.Any(connection => connection.Callable == callable);
    }

    public Error EmitSignal(string signal, params object?[] args)
    {
        ThrowIfFreed();
        var signalName = ValidateSignalName(signal);
        if (!_signals.Contains(signalName))
        {
            return Error.Unavailable;
        }

        if (!_signalConnections.TryGetValue(signalName, out var connections))
        {
            return Error.Ok;
        }

        var result = Error.Ok;
        var signalArguments = args ?? Array.Empty<object?>();
        foreach (var connection in connections.ToArray())
        {
            if (connection.Callable.TryCall(signalArguments, out _, out var exception) != Error.Ok)
            {
                result = Error.Failed;
                if (exception is not null)
                {
                    ReportSignalDiagnostic(signalName, connection.Callable, exception);
                }
            }
        }

        return result;
    }

    public object? CallDeferred(string method, params object?[] args)
    {
        ThrowIfFreed();
        new Callable(this, method).CallDeferred(args ?? Array.Empty<object?>());
        return null;
    }

    /// <summary>
    /// Translates a message through the process-wide translation server.
    /// </summary>
    ///
    /// <param name="message">The source message key to translate.</param>
    /// <param name="context">The optional message context.</param>
    /// <returns>
    /// The translated message, or <paramref name="message" /> when no
    /// registered translation can resolve it.
    /// </returns>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message" /> or <paramref name="context" />
    /// is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread as long as registered
    /// translation resources are not mutated concurrently.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TranslationServer" />
    /// <seealso cref="Translation" />
    public string Tr(string message, string context = "")
    {
        ThrowIfFreed();
        return TranslationServer.Translate(message, context);
    }

    public override string ToString()
    {
        return $"{GetType().Name}:{_instanceId}";
    }

    protected void ThrowIfFreed()
    {
        if (_freed)
        {
            throw new InvalidOperationException($"{GetType().Name} instance was freed.");
        }
    }

    protected bool MarkQueuedForDeletion()
    {
        if (_queuedForDeletion)
        {
            return false;
        }

        _queuedForDeletion = true;
        return true;
    }

    protected void ClearQueuedForDeletion()
    {
        _queuedForDeletion = false;
    }

    protected virtual void OnFree()
    {
        _signals.Clear();
        _signalConnections.Clear();
    }

    private static string ValidateSignalName(string signal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signal);
        return signal;
    }

    private void ReportSignalDiagnostic(string signal, Callable callable, Exception exception)
    {
        var targetNode = callable.GetObject() as Node;
        var targetTree = targetNode?.GetTree();
        if (targetNode is not null && targetTree is not null)
        {
            targetTree.ReportUserCodeException(
                targetNode,
                signal,
                exception,
                RuntimeUserCodeFailureKind.SignalEmission);
            return;
        }

        if (this is Node emitterNode && emitterNode.GetTree() is { } emitterTree)
        {
            emitterTree.ReportUserCodeException(
                emitterNode,
                signal,
                exception,
                RuntimeUserCodeFailureKind.SignalEmission);
        }
    }

    private sealed class SignalConnection
    {
        public SignalConnection(Callable callable, ConnectFlags flags)
        {
            Callable = callable;
            Flags = flags;
        }

        public Callable Callable { get; }

        public ConnectFlags Flags { get; }

        public int ReferenceCount { get; set; } = 1;
    }
}
