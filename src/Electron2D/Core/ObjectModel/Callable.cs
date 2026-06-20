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

public readonly struct Callable : IEquatable<Callable>
{
    private readonly Object? _target;
    private readonly string? _method;
    private readonly Delegate? _delegate;

    public Callable(Object target, string method)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        _target = target;
        _method = method;
        _delegate = null;
    }

    private Callable(Delegate callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _target = null;
        _method = null;
        _delegate = callback;
    }

    public static Callable From(Action action)
    {
        return new Callable(action);
    }

    public static Callable From<T>(Action<T> action)
    {
        return new Callable(action);
    }

    public bool IsNull()
    {
        return _target is null && _method is null && _delegate is null;
    }

    public Object? GetObject()
    {
        return _target;
    }

    public string GetMethod()
    {
        return _method ?? _delegate?.Method.Name ?? string.Empty;
    }

    public object? Call(params object?[] args)
    {
        TryCall(args ?? Array.Empty<object?>(), out var result);
        return result;
    }

    public void CallDeferred(params object?[] args)
    {
        if (IsNull())
        {
            return;
        }

        DeferredCallQueue.Enqueue(this, args ?? Array.Empty<object?>());
    }

    public bool Equals(Callable other)
    {
        if (_delegate is not null || other._delegate is not null)
        {
            return Equals(_delegate, other._delegate);
        }

        return ReferenceEquals(_target, other._target) &&
            string.Equals(_method, other._method, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is Callable other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_target, _method, _delegate);
    }

    public static bool operator ==(Callable left, Callable right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Callable left, Callable right)
    {
        return !left.Equals(right);
    }

    internal Error TryCall(object?[] args, out object? result)
    {
        return TryCall(args, out result, out _);
    }

    internal Error TryCall(object?[] args, out object? result, out Exception? exception)
    {
        result = null;
        exception = null;

        if (IsNull())
        {
            return Error.InvalidParameter;
        }

        if (_delegate is not null)
        {
            return TryCallDelegate(args, out result, out exception);
        }

        if (_target is null || !Object.IsInstanceValid(_target) || _method is null)
        {
            return Error.Failed;
        }

        var method = FindCallableMethod(_target.GetType(), _method, args);
        if (method is null)
        {
            return Error.Failed;
        }

        try
        {
            result = method.Invoke(_target, args);
            return Error.Ok;
        }
        catch (TargetInvocationException targetException) when (targetException.InnerException is not null)
        {
            exception = targetException.InnerException;
            return Error.Failed;
        }
        catch (Exception invokeException)
        {
            exception = invokeException;
            return Error.Failed;
        }
    }

    private Error TryCallDelegate(object?[] args, out object? result, out Exception? exception)
    {
        result = null;
        exception = null;

        try
        {
            result = _delegate?.DynamicInvoke(args);
            return Error.Ok;
        }
        catch (ArgumentException argumentException)
        {
            exception = argumentException;
            return Error.Failed;
        }
        catch (TargetInvocationException targetException) when (targetException.InnerException is not null)
        {
            exception = targetException.InnerException;
            return Error.Failed;
        }
        catch (Exception invokeException)
        {
            exception = invokeException;
            return Error.Failed;
        }
    }

    private static MethodInfo? FindCallableMethod(Type type, string method, object?[] args)
    {
        return type
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
}
