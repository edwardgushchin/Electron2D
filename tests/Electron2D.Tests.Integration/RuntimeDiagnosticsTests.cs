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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class RuntimeDiagnosticsTests
{
    [Fact]
    public void DeferredCallExceptionIsCapturedAndQueueContinues()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var node = new DeferredDiagnosticsNode(events);
        tree.Root.AddChild(node);
        events.Clear();

        tree.ProcessFrame(0.0d);

        Assert.Equal(
            new[]
            {
                "process",
                "deferred:throw",
                "deferred:after"
            },
            events);

        var diagnostic = Assert.Single(tree.Diagnostics);
        Assert.Same(node, diagnostic.Node);
        Assert.Equal(nameof(DeferredDiagnosticsNode.ThrowDeferred), diagnostic.Callback);
        Assert.Equal(Electron2D.RuntimeUserCodeFailureKind.DeferredCall, diagnostic.Kind);
        Assert.IsType<InvalidOperationException>(diagnostic.Exception);
        Assert.Equal("deferred boom", diagnostic.Message);
        Assert.Contains(nameof(DeferredDiagnosticsNode.ThrowDeferred), diagnostic.StackTrace);
    }

    [Fact]
    public void SignalCallbackExceptionIsCapturedAndEmissionContinues()
    {
        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var emitter = new Electron2D.Node { Name = "Emitter" };
        var target = new ThrowingSignalTarget(events);
        emitter.AddUserSignal("finished");
        Assert.Equal(
            Electron2D.Error.Ok,
            emitter.Connect("finished", new Electron2D.Callable(target, nameof(ThrowingSignalTarget.OnFinished))));
        Assert.Equal(
            Electron2D.Error.Ok,
            emitter.Connect("finished", Electron2D.Callable.From(() => events.Add("signal:after"))));
        tree.Root.AddChild(emitter);
        tree.Root.AddChild(target);
        events.Clear();

        var result = emitter.EmitSignal("finished");

        Assert.Equal(Electron2D.Error.Failed, result);
        Assert.Equal(new[] { "signal:throw", "signal:after" }, events);

        var diagnostic = Assert.Single(tree.Diagnostics);
        Assert.Same(target, diagnostic.Node);
        Assert.Equal("finished", diagnostic.Callback);
        Assert.Equal(Electron2D.RuntimeUserCodeFailureKind.SignalEmission, diagnostic.Kind);
        Assert.IsType<InvalidOperationException>(diagnostic.Exception);
        Assert.Equal("signal boom", diagnostic.Message);
        Assert.Contains(nameof(ThrowingSignalTarget.OnFinished), diagnostic.StackTrace);
    }

    private sealed class DeferredDiagnosticsNode : Electron2D.Node
    {
        private readonly List<string> _events;

        public DeferredDiagnosticsNode(List<string> events)
        {
            Name = "DeferredDiagnostics";
            _events = events;
        }

        public override void _Process(double delta)
        {
            _events.Add("process");
            CallDeferred(nameof(ThrowDeferred));
            CallDeferred(nameof(RecordDeferred));
        }

        public void ThrowDeferred()
        {
            _events.Add("deferred:throw");
            throw new InvalidOperationException("deferred boom");
        }

        public void RecordDeferred()
        {
            _events.Add("deferred:after");
        }
    }

    private sealed class ThrowingSignalTarget : Electron2D.Node
    {
        private readonly List<string> _events;

        public ThrowingSignalTarget(List<string> events)
        {
            Name = "SignalTarget";
            _events = events;
        }

        public void OnFinished()
        {
            _events.Add("signal:throw");
            throw new InvalidOperationException("signal boom");
        }
    }
}
