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

/// <summary>
/// Represents the callable value type.
/// </summary>
///
/// <remarks>
/// This type is part of the Electron2D 0.1.0 Preview public API.
/// </remarks>
///
/// <threadsafety>
/// Instances of this type are not synchronized. Access them from the thread that owns the object unless the member documentation states otherwise.
/// </threadsafety>
///
/// <since>
/// This API is available since Electron2D 0.1.0 Preview.
/// </since>
///
public readonly struct Callable : IEquatable<Callable>
{
    private readonly Object? _target;
    private readonly string? _method;
    private readonly Delegate? _delegate;

    /// <summary>
    /// Initializes a new instance of the Callable type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="target">
    /// The target value.
    /// </param>
    ///
    /// <param name="method">
    /// The method value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
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

    /// <summary>
    /// Executes the from operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="action">
    /// The action value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public static Callable From(Action action)
    {
        return new Callable(action);
    }

    /// <summary>
    /// Executes the from operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <typeparam name="T">
    /// The t type.
    /// </typeparam>
    ///
    /// <param name="action">
    /// The action value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public static Callable From<T>(Action<T> action)
    {
        return new Callable(action);
    }

    /// <summary>
    /// Checks whether null is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public bool IsNull()
    {
        return _target is null && _method is null && _delegate is null;
    }

    /// <summary>
    /// Gets the object value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current object value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public Object? GetObject()
    {
        return _target;
    }

    /// <summary>
    /// Gets the method value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current method value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public string GetMethod()
    {
        return _method ?? _delegate?.Method.Name ?? string.Empty;
    }

    /// <summary>
    /// Executes the call operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="args">
    /// The args value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public object? Call(params object?[] args)
    {
        TryCall(args ?? Array.Empty<object?>(), out var result);
        return result;
    }

    /// <summary>
    /// Executes the call deferred operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="args">
    /// The args value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public void CallDeferred(params object?[] args)
    {
        if (IsNull())
        {
            return;
        }

        DeferredCallQueue.Enqueue(this, args ?? Array.Empty<object?>());
    }

    /// <summary>
    /// Executes the equals operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="other">
    /// The other value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public bool Equals(Callable other)
    {
        if (_delegate is not null || other._delegate is not null)
        {
            return Equals(_delegate, other._delegate);
        }

        return ReferenceEquals(_target, other._target) &&
            string.Equals(_method, other._method, StringComparison.Ordinal);
    }

    /// <summary>
    /// Executes the equals operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="obj">
    /// The obj value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operation.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public override bool Equals(object? obj)
    {
        return obj is Callable other && Equals(other);
    }

    /// <summary>
    /// Gets the hash code value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current hash code value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public override int GetHashCode()
    {
        return HashCode.Combine(_target, _method, _delegate);
    }

    /// <summary>
    /// Applies the <c>==</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
    public static bool operator ==(Callable left, Callable right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Applies the <c>!=</c> operator.
    /// </summary>
    ///
    /// <remarks>
    /// This operator returns a value derived from the supplied operands.
    /// </remarks>
    ///
    /// <param name="left">
    /// The left value.
    /// </param>
    ///
    /// <param name="right">
    /// The right value.
    /// </param>
    ///
    /// <returns>
    /// The result of the operator.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Callable" />
    ///
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
