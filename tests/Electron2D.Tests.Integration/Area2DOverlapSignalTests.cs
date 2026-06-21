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

[Collection(PhysicsServer2DCollection.Name)]
public sealed class Area2DOverlapSignalTests
{
    [Fact]
    public void Area2DRegistersSignalsAndEmitsBodyEnterThenExit()
    {
        var tree = new Electron2D.SceneTree();
        var area = CreateArea("Sensor", new Electron2D.Vector2(0f, 0f));
        var body = CreateBody("Body", new Electron2D.Vector2(4f, 0f));
        var events = new List<string>();

        area.Connect("body_entered", Electron2D.Callable.From<Electron2D.Node2D>(
            entered => events.Add($"body_entered:{entered.Name}")));
        area.Connect("body_exited", Electron2D.Callable.From<Electron2D.Node2D>(
            exited => events.Add($"body_exited:{exited.Name}")));

        tree.Root.AddChild(area);
        tree.Root.AddChild(body);

        Assert.True(area.HasSignal("body_entered"));
        Assert.True(area.HasSignal("body_exited"));
        Assert.True(area.HasSignal("area_entered"));
        Assert.True(area.HasSignal("area_exited"));

        tree.PhysicsFrame(1d / 60d);

        Assert.Equal(["body_entered:Body"], events);
        Assert.True(area.HasOverlappingBodies());
        Assert.True(area.OverlapsBody(body));
        Assert.Same(body, Assert.Single(area.GetOverlappingBodies()));

        body.Position = new Electron2D.Vector2(100f, 0f);
        tree.PhysicsFrame(1d / 60d);

        Assert.Equal(["body_entered:Body", "body_exited:Body"], events);
        Assert.False(area.HasOverlappingBodies());
        Assert.False(area.OverlapsBody(body));
        Assert.Empty(area.GetOverlappingBodies());
    }

    [Fact]
    public void Area2DEmitsBodySignalsBeforeAreaSignalsInDeterministicOrder()
    {
        var tree = new Electron2D.SceneTree();
        var sensor = CreateArea("Sensor", new Electron2D.Vector2(0f, 0f));
        var body = CreateBody("Body", new Electron2D.Vector2(0f, 0f));
        var otherArea = CreateArea("OtherArea", new Electron2D.Vector2(0f, 0f));
        var events = new List<string>();

        sensor.Connect("body_entered", Electron2D.Callable.From<Electron2D.Node2D>(
            entered => events.Add($"body_entered:{entered.Name}")));
        sensor.Connect("body_exited", Electron2D.Callable.From<Electron2D.Node2D>(
            exited => events.Add($"body_exited:{exited.Name}")));
        sensor.Connect("area_entered", Electron2D.Callable.From<Electron2D.Area2D>(
            entered => events.Add($"area_entered:{entered.Name}")));
        sensor.Connect("area_exited", Electron2D.Callable.From<Electron2D.Area2D>(
            exited => events.Add($"area_exited:{exited.Name}")));

        tree.Root.AddChild(sensor);
        tree.Root.AddChild(body);
        tree.Root.AddChild(otherArea);

        tree.PhysicsFrame(1d / 60d);

        Assert.Equal(["body_entered:Body", "area_entered:OtherArea"], events);
        Assert.Same(body, Assert.Single(sensor.GetOverlappingBodies()));
        Assert.Same(otherArea, Assert.Single(sensor.GetOverlappingAreas()));
        Assert.True(sensor.HasOverlappingAreas());
        Assert.True(sensor.OverlapsArea(otherArea));

        body.Position = new Electron2D.Vector2(100f, 0f);
        otherArea.Position = new Electron2D.Vector2(100f, 0f);
        tree.PhysicsFrame(1d / 60d);

        Assert.Equal(
            [
                "body_entered:Body",
                "area_entered:OtherArea",
                "body_exited:Body",
                "area_exited:OtherArea"
            ],
            events);
        Assert.False(sensor.HasOverlappingAreas());
        Assert.False(sensor.OverlapsArea(otherArea));
    }

    [Fact]
    public void Area2DHonorsMonitoringMonitorableAndCollisionFilters()
    {
        var tree = new Electron2D.SceneTree();
        var sensor = CreateArea("Sensor", new Electron2D.Vector2(0f, 0f));
        var body = CreateBody("Body", new Electron2D.Vector2(0f, 0f));
        var otherArea = CreateArea("OtherArea", new Electron2D.Vector2(0f, 0f));
        var events = new List<string>();

        sensor.CollisionMask = 0b0010u;
        body.CollisionLayer = 0b0100u;
        otherArea.CollisionLayer = 0b0010u;
        otherArea.Monitorable = false;

        sensor.Connect("body_entered", Electron2D.Callable.From<Electron2D.Node2D>(
            entered => events.Add($"body_entered:{entered.Name}")));
        sensor.Connect("area_entered", Electron2D.Callable.From<Electron2D.Area2D>(
            entered => events.Add($"area_entered:{entered.Name}")));

        tree.Root.AddChild(sensor);
        tree.Root.AddChild(body);
        tree.Root.AddChild(otherArea);

        tree.PhysicsFrame(1d / 60d);

        Assert.Empty(events);
        Assert.Empty(sensor.GetOverlappingBodies());
        Assert.Empty(sensor.GetOverlappingAreas());

        body.CollisionLayer = 0b0010u;
        otherArea.Monitorable = true;
        tree.PhysicsFrame(1d / 60d);

        Assert.Equal(["body_entered:Body", "area_entered:OtherArea"], events);

        sensor.Monitoring = false;
        tree.PhysicsFrame(1d / 60d);

        Assert.False(sensor.HasOverlappingBodies());
        Assert.False(sensor.HasOverlappingAreas());
    }

    [Fact]
    public void QueueFreeInsideOverlapSignalIsDeferredUntilPhysicsFrameEnds()
    {
        var tree = new Electron2D.SceneTree();
        var sensor = CreateArea("Sensor", new Electron2D.Vector2(0f, 0f));
        var body = CreateBody("Body", new Electron2D.Vector2(0f, 0f));
        var events = new List<string>();

        sensor.Connect("body_entered", Electron2D.Callable.From<Electron2D.Node2D>(
            entered =>
            {
                events.Add($"body_entered:{entered.Name}");
                entered.QueueFree();
                events.Add($"valid_inside_signal:{Electron2D.Object.IsInstanceValid(entered)}");
            }));
        sensor.Connect("body_exited", Electron2D.Callable.From<Electron2D.Node2D>(
            exited => events.Add($"body_exited:{exited.Name}")));

        tree.Root.AddChild(sensor);
        tree.Root.AddChild(body);

        tree.PhysicsFrame(1d / 60d);

        Assert.Equal(["body_entered:Body", "valid_inside_signal:True"], events);
        Assert.False(Electron2D.Object.IsInstanceValid(body));

        tree.PhysicsFrame(1d / 60d);

        Assert.Equal(["body_entered:Body", "valid_inside_signal:True"], events);
        Assert.Empty(sensor.GetOverlappingBodies());
    }

    private static Electron2D.Area2D CreateArea(string name, Electron2D.Vector2 position)
    {
        var area = new Electron2D.Area2D
        {
            Name = name,
            Position = position
        };
        area.AddChild(CreateShape());
        return area;
    }

    private static Electron2D.StaticBody2D CreateBody(string name, Electron2D.Vector2 position)
    {
        var body = new Electron2D.StaticBody2D
        {
            Name = name,
            Position = position
        };
        body.AddChild(CreateShape());
        return body;
    }

    private static Electron2D.CollisionShape2D CreateShape()
    {
        return new Electron2D.CollisionShape2D
        {
            Shape = new Electron2D.RectangleShape2D { Size = new Electron2D.Vector2(20f, 20f) }
        };
    }
}
