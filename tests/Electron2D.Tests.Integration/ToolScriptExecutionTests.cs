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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ToolScriptExecutionTests
{
    [Fact]
    public void HostExecutesOnlyRegisteredSandboxedToolScripts()
    {
        Register<ToolExecutionScript>("ToolExecutionScript", isTool: true);
        Register<RuntimeOnlyScript>("RuntimeOnlyScript", isTool: false);

        var host = new Electron2D.ToolScriptExecutionHost();
        var toolScript = new ToolExecutionScript();
        var runtimeScript = new RuntimeOnlyScript();
        var missingMetadataScript = new MissingMetadataScript();

        var ready = host.Execute(toolScript, Electron2D.ToolScriptExecutionCallback.Ready);
        var process = host.Execute(toolScript, Electron2D.ToolScriptExecutionCallback.Process, 0.25d);
        var runtimeOnly = host.Execute(runtimeScript, Electron2D.ToolScriptExecutionCallback.Ready);
        var missing = host.Execute(missingMetadataScript, Electron2D.ToolScriptExecutionCallback.Ready);

        Assert.Equal(Electron2D.ToolScriptExecutionStatus.Executed, ready.Status);
        Assert.Equal(Electron2D.ToolScriptExecutionStatus.Executed, process.Status);
        Assert.True(ready.Executed);
        Assert.True(process.Executed);
        Assert.Equal(nameof(Electron2D.Node._Ready), ready.Callback);
        Assert.Equal(nameof(Electron2D.Node._Process), process.Callback);
        Assert.Equal(1, toolScript.ReadyCalls);
        Assert.Equal(1, toolScript.ProcessCalls);
        Assert.Equal(0.25d, toolScript.LastDelta);

        Assert.Equal(Electron2D.ToolScriptExecutionStatus.NotToolScript, runtimeOnly.Status);
        Assert.False(runtimeOnly.Executed);
        Assert.Equal(0, runtimeScript.ReadyCalls);

        Assert.Equal(Electron2D.ToolScriptExecutionStatus.MissingMetadata, missing.Status);
        Assert.False(missing.Executed);
        Assert.Equal(0, missingMetadataScript.ReadyCalls);
    }

    [Fact]
    public void HostIsolatesCallbackExceptionsAndCanContinue()
    {
        Register<ThrowingToolScript>("ThrowingToolScript", isTool: true);

        var host = new Electron2D.ToolScriptExecutionHost();
        var script = new ThrowingToolScript { ThrowOnReady = true };

        var failed = host.Execute(script, Electron2D.ToolScriptExecutionCallback.Ready);
        script.ThrowOnReady = false;
        var recovered = host.Execute(script, Electron2D.ToolScriptExecutionCallback.Process, 0.5d);

        Assert.Equal(Electron2D.ToolScriptExecutionStatus.ExceptionIsolated, failed.Status);
        Assert.False(failed.Executed);
        Assert.Equal(nameof(Electron2D.Node._Ready), failed.Callback);
        Assert.IsType<InvalidOperationException>(failed.Exception);
        Assert.Equal("tool boom", failed.Exception!.Message);
        Assert.Equal(1, script.ReadyCalls);

        Assert.Equal(Electron2D.ToolScriptExecutionStatus.Executed, recovered.Status);
        Assert.True(recovered.Executed);
        Assert.Null(recovered.Exception);
        Assert.Equal(1, script.ProcessCalls);
        Assert.Equal(0.5d, script.LastDelta);
    }

    [Fact]
    public void HostDoesNotExposeDynamicAssemblyLoading()
    {
        var host = new Electron2D.ToolScriptExecutionHost();

        Assert.False(host.SupportsDynamicAssemblyLoad);

        var publicMethods = typeof(Electron2D.ToolScriptExecutionHost)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var parameterTypes = publicMethods
            .SelectMany(method => method.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.DoesNotContain(typeof(Assembly), parameterTypes);
        Assert.DoesNotContain(typeof(byte[]), parameterTypes);
        Assert.DoesNotContain(parameterTypes, type => type.FullName == "System.Runtime.Loader.AssemblyLoadContext");
        Assert.DoesNotContain(
            publicMethods,
            method => method.GetParameters().Any(parameter =>
                parameter.ParameterType == typeof(string) &&
                (parameter.Name?.Contains("path", StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    private static void Register<TScript>(string scriptName, bool isTool)
        where TScript : Electron2D.Node
    {
        Electron2D.ScriptObjectMetadataRegistry.Register(
            Electron2D.ScriptObjectTypeMetadata.Create<TScript>(
                scriptName,
                Array.Empty<Electron2D.ScriptExportPropertyMetadata>(),
                Array.Empty<Electron2D.ScriptSignalMetadata>(),
                isTool));
    }

    [Tool]
    private sealed class ToolExecutionScript : Electron2D.Node
    {
        public int ReadyCalls { get; private set; }

        public int ProcessCalls { get; private set; }

        public double LastDelta { get; private set; }

        public override void _Ready()
        {
            ReadyCalls++;
        }

        public override void _Process(double delta)
        {
            ProcessCalls++;
            LastDelta = delta;
        }
    }

    private sealed class RuntimeOnlyScript : Electron2D.Node
    {
        public int ReadyCalls { get; private set; }

        public override void _Ready()
        {
            ReadyCalls++;
        }
    }

    [Tool]
    private sealed class MissingMetadataScript : Electron2D.Node
    {
        public int ReadyCalls { get; private set; }

        public override void _Ready()
        {
            ReadyCalls++;
        }
    }

    [Tool]
    private sealed class ThrowingToolScript : Electron2D.Node
    {
        public bool ThrowOnReady { get; set; }

        public int ReadyCalls { get; private set; }

        public int ProcessCalls { get; private set; }

        public double LastDelta { get; private set; }

        public override void _Ready()
        {
            ReadyCalls++;
            if (ThrowOnReady)
            {
                throw new InvalidOperationException("tool boom");
            }
        }

        public override void _Process(double delta)
        {
            ProcessCalls++;
            LastDelta = delta;
        }
    }
}
