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

public sealed class SignalCallableTests
{
    [Fact]
    public void MultipleSubscribersAreInvokedInConnectionOrder()
    {
        var emitter = new Electron2D.Object();
        var calls = new List<string>();
        var receiver = new SignalReceiver(calls);
        emitter.AddUserSignal("hit");

        var first = Electron2D.Callable.From<string>(value => calls.Add($"first:{value}"));
        var second = new Electron2D.Callable(receiver, nameof(SignalReceiver.Record));

        Assert.Equal(Electron2D.Error.Ok, emitter.Connect("hit", first));
        Assert.Equal(Electron2D.Error.Ok, emitter.Connect("hit", second));
        Assert.True(emitter.IsConnected("hit", first));

        Assert.Equal(Electron2D.Error.Ok, emitter.EmitSignal("hit", "tick"));

        Assert.Equal(new[] { "first:tick", "receiver:tick" }, calls);
    }

    [Fact]
    public void UntypedCallableReceivesNoArgumentSignal()
    {
        var emitter = new Electron2D.Object();
        var calls = new List<string>();
        emitter.AddUserSignal("done");

        Assert.Equal(Electron2D.Error.Ok, emitter.Connect("done", Electron2D.Callable.From(() => calls.Add("done"))));

        Assert.Equal(Electron2D.Error.Ok, emitter.EmitSignal("done"));

        Assert.Equal(new[] { "done" }, calls);
    }

    [Fact]
    public void DisconnectDuringEmissionUsesStableSnapshot()
    {
        var emitter = new Electron2D.Object();
        var calls = new List<string>();
        emitter.AddUserSignal("pulse");
        Electron2D.Callable second = default;
        var first = Electron2D.Callable.From(() =>
        {
            calls.Add("first");
            emitter.Disconnect("pulse", second);
        });
        second = Electron2D.Callable.From(() => calls.Add("second"));
        emitter.Connect("pulse", first);
        emitter.Connect("pulse", second);

        Assert.Equal(Electron2D.Error.Ok, emitter.EmitSignal("pulse"));
        Assert.Equal(Electron2D.Error.Ok, emitter.EmitSignal("pulse"));

        Assert.Equal(new[] { "first", "second", "first" }, calls);
    }

    [Fact]
    public void SignalErrorsAreReportedWithoutStoppingOtherCallbacks()
    {
        var emitter = new Electron2D.Object();
        var calls = new List<string>();
        emitter.AddUserSignal("boom");
        var throwing = Electron2D.Callable.From(() => throw new InvalidOperationException("boom"));
        var succeeding = Electron2D.Callable.From(() => calls.Add("after"));

        Assert.Equal(Electron2D.Error.Unavailable, emitter.Connect("missing", succeeding));
        Assert.Equal(Electron2D.Error.Ok, emitter.Connect("boom", throwing));
        Assert.Equal(Electron2D.Error.Ok, emitter.Connect("boom", succeeding));
        Assert.Equal(Electron2D.Error.AlreadyExists, emitter.Connect("boom", succeeding));

        Assert.Equal(Electron2D.Error.Failed, emitter.EmitSignal("boom"));
        Assert.Equal(Electron2D.Error.Unavailable, emitter.EmitSignal("missing"));

        Assert.Equal(new[] { "after" }, calls);
    }

    [Fact]
    public void MismatchedCallableArgumentsReportFailure()
    {
        var emitter = new Electron2D.Object();
        var calls = new List<string>();
        emitter.AddUserSignal("typed");
        emitter.Connect("typed", Electron2D.Callable.From<int>(value => calls.Add(value.ToString())));

        Assert.Equal(Electron2D.Error.Failed, emitter.EmitSignal("typed", "not-an-int"));

        Assert.Empty(calls);
    }

    [Fact]
    public void SignalApiDoesNotExposeDotNetEvents()
    {
        Assert.Empty(typeof(Electron2D.Object).GetEvents());
    }

    private sealed class SignalReceiver : Electron2D.Object
    {
        private readonly List<string> _calls;

        public SignalReceiver(List<string> calls)
        {
            _calls = calls;
        }

        public void Record(string value)
        {
            _calls.Add($"receiver:{value}");
        }
    }
}
