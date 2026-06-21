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

internal sealed class ToolScriptExecutionHost
{
    public bool SupportsDynamicAssemblyLoad => false;

    public ToolScriptExecutionResult Execute(
        Node script,
        ToolScriptExecutionCallback callback,
        double delta = 0d)
    {
        ArgumentNullException.ThrowIfNull(script);
        if (!double.IsFinite(delta) || delta < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Delta must be finite and non-negative.");
        }

        var callbackName = GetCallbackName(callback);
        var scriptType = script.GetType();
        if (!ScriptObjectMetadataRegistry.TryGetByScriptType(scriptType, out var metadata))
        {
            return ToolScriptExecutionResult.Skipped(
                ToolScriptExecutionStatus.MissingMetadata,
                callbackName,
                scriptType);
        }

        if (!metadata.IsTool)
        {
            return ToolScriptExecutionResult.Skipped(
                ToolScriptExecutionStatus.NotToolScript,
                callbackName,
                scriptType);
        }

        if (!metadata.IsToolExecutionSandboxed)
        {
            return ToolScriptExecutionResult.Skipped(
                ToolScriptExecutionStatus.NotSandboxed,
                callbackName,
                scriptType);
        }

        try
        {
            ExecuteCallback(script, callback, delta);
            return ToolScriptExecutionResult.Success(callbackName, scriptType);
        }
        catch (Exception exception)
        {
            return ToolScriptExecutionResult.ExceptionIsolated(callbackName, scriptType, exception);
        }
    }

    private static void ExecuteCallback(Node script, ToolScriptExecutionCallback callback, double delta)
    {
        switch (callback)
        {
            case ToolScriptExecutionCallback.EnterTree:
                script._EnterTree();
                break;
            case ToolScriptExecutionCallback.Ready:
                script._Ready();
                break;
            case ToolScriptExecutionCallback.Process:
                script._Process(delta);
                break;
            case ToolScriptExecutionCallback.PhysicsProcess:
                script._PhysicsProcess(delta);
                break;
            case ToolScriptExecutionCallback.ExitTree:
                script._ExitTree();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(callback), callback, "Tool script callback is not supported.");
        }
    }

    private static string GetCallbackName(ToolScriptExecutionCallback callback)
    {
        return callback switch
        {
            ToolScriptExecutionCallback.EnterTree => nameof(Node._EnterTree),
            ToolScriptExecutionCallback.Ready => nameof(Node._Ready),
            ToolScriptExecutionCallback.Process => nameof(Node._Process),
            ToolScriptExecutionCallback.PhysicsProcess => nameof(Node._PhysicsProcess),
            ToolScriptExecutionCallback.ExitTree => nameof(Node._ExitTree),
            _ => throw new ArgumentOutOfRangeException(nameof(callback), callback, "Tool script callback is not supported.")
        };
    }
}

internal enum ToolScriptExecutionCallback
{
    EnterTree,
    Ready,
    Process,
    PhysicsProcess,
    ExitTree
}

internal enum ToolScriptExecutionStatus
{
    Executed,
    MissingMetadata,
    NotToolScript,
    NotSandboxed,
    ExceptionIsolated
}

internal sealed record ToolScriptExecutionResult(
    ToolScriptExecutionStatus Status,
    bool Executed,
    string Callback,
    Type ScriptType,
    Exception? Exception)
{
    public static ToolScriptExecutionResult Success(string callback, Type scriptType)
    {
        return new ToolScriptExecutionResult(
            ToolScriptExecutionStatus.Executed,
            Executed: true,
            callback,
            scriptType,
            Exception: null);
    }

    public static ToolScriptExecutionResult Skipped(
        ToolScriptExecutionStatus status,
        string callback,
        Type scriptType)
    {
        return new ToolScriptExecutionResult(
            status,
            Executed: false,
            callback,
            scriptType,
            Exception: null);
    }

    public static ToolScriptExecutionResult ExceptionIsolated(
        string callback,
        Type scriptType,
        Exception exception)
    {
        return new ToolScriptExecutionResult(
            ToolScriptExecutionStatus.ExceptionIsolated,
            Executed: false,
            callback,
            scriptType,
            exception);
    }
}
