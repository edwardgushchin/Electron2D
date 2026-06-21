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
using Electron2D;
using System.Reflection;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ScriptMetadataTests
{
    [Fact]
    public void ExportedPropertiesRoundTripThroughScriptMetadata()
    {
        RegisterMetadata();
        var source = new MetadataScript
        {
            Health = 12,
            Title = "Hero",
            SpawnPoint = new Electron2D.Vector2(16f, 32f),
            RuntimeOnly = "not serialized"
        };

        var exported = Electron2D.ScriptObjectSerializer.CaptureExportedProperties(source);

        Assert.Equal(new[] { "health", "spawn_point", "title" }, exported.Keys);

        var target = new MetadataScript
        {
            RuntimeOnly = "kept local"
        };
        Electron2D.ScriptObjectSerializer.RestoreExportedProperties(target, exported);

        Assert.Equal(12, target.Health);
        Assert.Equal("Hero", target.Title);
        Assert.Equal(new Electron2D.Vector2(16f, 32f), target.SpawnPoint);
        Assert.Equal("kept local", target.RuntimeOnly);
    }

    [Fact]
    public void ScriptSignalsRegisteredFromMetadataAreCallable()
    {
        RegisterMetadata();
        var script = new MetadataScript();
        var calls = new List<int>();

        Electron2D.ScriptObjectMetadataRegistry.ApplySignals(script);

        Assert.True(script.HasSignal("health_changed"));
        Assert.Equal(
            Electron2D.Error.Ok,
            script.Connect("health_changed", Electron2D.Callable.From<int>(value => calls.Add(value))));

        Assert.Equal(Electron2D.Error.Ok, script.EmitSignal("health_changed", 7));

        Assert.Equal(new[] { 7 }, calls);
    }

    [Fact]
    public void ToolScriptMetadataIsExperimentalAndSandboxed()
    {
        RegisterMetadata();

        var metadata = Electron2D.ScriptObjectMetadataRegistry.GetByScriptType(typeof(MetadataScript));

        Assert.True(metadata.IsTool);
        Assert.True(metadata.IsToolExperimental);
        Assert.True(metadata.IsToolExecutionSandboxed);
    }

    [Fact]
    public void PublicAttributesExposeOnlyMarkerSurface()
    {
        AssertAttributeUsage<Electron2D.ExportAttribute>(
            AttributeTargets.Property | AttributeTargets.Field,
            allowMultiple: false,
            inherited: true);
        AssertAttributeUsage<Electron2D.SignalAttribute>(
            AttributeTargets.Delegate,
            allowMultiple: false,
            inherited: false);
        AssertAttributeUsage<Electron2D.ToolAttribute>(
            AttributeTargets.Class,
            allowMultiple: false,
            inherited: true);
    }

    private static void RegisterMetadata()
    {
        Electron2D.ScriptObjectMetadataRegistry.Register(
            Electron2D.ScriptObjectTypeMetadata.Create<MetadataScript>(
                "MetadataScript",
                exports:
                [
                    Electron2D.ScriptExportPropertyMetadata.Create<MetadataScript, int>(
                        "health",
                        script => script.Health,
                        (script, value) => script.Health = value),
                    Electron2D.ScriptExportPropertyMetadata.Create<MetadataScript, string>(
                        "title",
                        script => script.Title,
                        (script, value) => script.Title = value),
                    Electron2D.ScriptExportPropertyMetadata.Create<MetadataScript, Electron2D.Vector2>(
                        "spawn_point",
                        script => script.SpawnPoint,
                        (script, value) => script.SpawnPoint = value)
                ],
                signals:
                [
                    Electron2D.ScriptSignalMetadata.Create<MetadataScript, MetadataScript.HealthChangedEventHandler>(
                        "health_changed")
                ],
                isTool: true));
    }

    private static void AssertAttributeUsage<TAttribute>(
        AttributeTargets expectedTargets,
        bool allowMultiple,
        bool inherited)
        where TAttribute : Attribute
    {
        var usage = typeof(TAttribute).GetCustomAttribute<AttributeUsageAttribute>();
        Assert.NotNull(usage);
        Assert.Equal(expectedTargets, usage.ValidOn);
        Assert.Equal(allowMultiple, usage.AllowMultiple);
        Assert.Equal(inherited, usage.Inherited);

        Assert.Empty(typeof(TAttribute).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Empty(typeof(TAttribute).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    [Tool]
    private sealed class MetadataScript : Electron2D.Node
    {
        [Signal]
        public delegate void HealthChangedEventHandler(int health);

        [Export]
        public int Health { get; set; }

        [Export]
        public string Title { get; set; } = string.Empty;

        [Export]
        public Electron2D.Vector2 SpawnPoint { get; set; }

        public string RuntimeOnly { get; set; } = string.Empty;
    }
}
