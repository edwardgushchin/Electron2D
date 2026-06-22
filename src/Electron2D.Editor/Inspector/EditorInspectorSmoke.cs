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
namespace Electron2D.Editor.Inspector;

internal static class EditorInspectorSmoke
{
    private const string ScriptName = "Electron2D.Editor.Inspector.EditorInspectorSmokeScript";
    private const string StatsResourceType = "Electron2D.Editor.Inspector.EditorInspectorSmokeStatsResource";

    public static EditorInspectorSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        RegisterMetadata();

        var root = Path.GetFullPath(workRoot);
        var scenePath = Path.Combine(root, "scenes", "main.scene.json");
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath)!);
        File.WriteAllText(scenePath, Electron2D.SceneFileTextSerializer.Serialize(CreateInitialScene()));

        var document = Electron2D.SceneFileTextSerializer.Deserialize(File.ReadAllText(scenePath));
        var inspector = new EditorInspector(document, selectedNodeId: 2, CreateDescriptors());
        var initialProperties = inspector.GetProperties();

        inspector.SetProperty("health", Electron2D.SerializedPropertyValue.FromVariant(42));
        inspector.SetProperty("display_name", Electron2D.SerializedPropertyValue.FromVariant("Captain"));
        inspector.SetProperty("active", Electron2D.SerializedPropertyValue.FromVariant(true));
        inspector.SetProperty("mode", Electron2D.SerializedPropertyValue.FromEnum(EditorInspectorSmokeMode.Attack));
        inspector.SetProperty(
            "flags",
            Electron2D.SerializedPropertyValue.FromEnum(EditorInspectorSmokeFlags.Air | EditorInspectorSmokeFlags.Ground));
        inspector.SetProperty(
            "tags",
            Electron2D.SerializedPropertyValue.FromArray(
            [
                Electron2D.SerializedPropertyValue.FromVariant("player"),
                Electron2D.SerializedPropertyValue.FromVariant("captain")
            ]));
        inspector.SetProperty(
            "target_path",
            Electron2D.SerializedPropertyValue.FromVariant(new Electron2D.NodePath("Root/Player/Weapon")));
        inspector.SetProperty("stats", Electron2D.SerializedPropertyValue.InternalResource(1));
        inspector.SetNestedResourceProperty("stats", "max_health", Electron2D.SerializedPropertyValue.FromVariant(250));
        inspector.ResetProperty("display_name");
        var redoName = ReadNodeProperty(inspector.Document, "display_name").VariantValue.AsString();

        inspector.Undo();
        var undoName = ReadNodeProperty(inspector.Document, "display_name").VariantValue.AsString();

        inspector.Redo();
        redoName = ReadNodeProperty(inspector.Document, "display_name").VariantValue.AsString();
        inspector.Save(scenePath);

        var savedText = File.ReadAllText(scenePath);
        var reloaded = Electron2D.SceneFileTextSerializer.Deserialize(savedText);
        var roundTripStable = string.Equals(
            savedText,
            Electron2D.SceneFileTextSerializer.Serialize(reloaded),
            StringComparison.Ordinal);

        var health = ReadNodeProperty(reloaded, "health").VariantValue.AsInt64();
        var displayName = ReadNodeProperty(reloaded, "display_name").VariantValue.AsString();
        var mode = ReadNodeProperty(reloaded, "mode").EnumName;
        var flags = FormatFlags(ReadNodeProperty(reloaded, "flags").EnumName);
        var tags = string.Join(',', ReadNodeProperty(reloaded, "tags").Items.Select(item => item.VariantValue.AsString()));
        var targetPath = ReadNodeProperty(reloaded, "target_path").VariantValue.AsNodePath().ToString();
        var resourceReference = ReadNodeProperty(reloaded, "stats").ReferenceId;
        var nestedMaxHealth = ReadInternalResourceProperty(reloaded, resourceReference, "max_health").VariantValue.AsInt64();

        return new EditorInspectorSmokeResult(
            scenePath,
            initialProperties.Count,
            initialProperties.Count(property => property.Descriptor.IsExported),
            health,
            displayName,
            undoName,
            redoName,
            mode,
            flags,
            tags,
            targetPath,
            resourceReference,
            nestedMaxHealth,
            roundTripStable);
    }

    private static void RegisterMetadata()
    {
        Electron2D.ScriptObjectMetadataRegistry.Register(
            Electron2D.ScriptObjectTypeMetadata.Create<EditorInspectorSmokeScript>(
                ScriptName,
                exports:
                [
                    Electron2D.ScriptExportPropertyMetadata.Create<EditorInspectorSmokeScript, bool>(
                        "active",
                        script => script.Active,
                        (script, value) => script.Active = value),
                    Electron2D.ScriptExportPropertyMetadata.Create<EditorInspectorSmokeScript, string>(
                        "display_name",
                        script => script.DisplayName,
                        (script, value) => script.DisplayName = value),
                    Electron2D.ScriptExportPropertyMetadata.Create<EditorInspectorSmokeScript, EditorInspectorSmokeFlags>(
                        "flags",
                        script => script.Flags,
                        (script, value) => script.Flags = value),
                    Electron2D.ScriptExportPropertyMetadata.Create<EditorInspectorSmokeScript, int>(
                        "health",
                        script => script.Health,
                        (script, value) => script.Health = value),
                    Electron2D.ScriptExportPropertyMetadata.Create<EditorInspectorSmokeScript, EditorInspectorSmokeMode>(
                        "mode",
                        script => script.Mode,
                        (script, value) => script.Mode = value),
                    Electron2D.ScriptExportPropertyMetadata.Create<EditorInspectorSmokeScript, Electron2D.Resource?>(
                        "stats",
                        script => script.Stats,
                        (script, value) => script.Stats = value),
                    Electron2D.ScriptExportPropertyMetadata.Create<EditorInspectorSmokeScript, string[]>(
                        "tags",
                        script => script.Tags,
                        (script, value) => script.Tags = value),
                    Electron2D.ScriptExportPropertyMetadata.Create<EditorInspectorSmokeScript, Electron2D.NodePath>(
                        "target_path",
                        script => script.TargetPath,
                        (script, value) => script.TargetPath = value)
                ],
                signals: []));

        Electron2D.ResourceObjectMetadataRegistry.Register(
            Electron2D.ResourceObjectTypeMetadata.Create<EditorInspectorSmokeStatsResource>(
                StatsResourceType,
                () => new EditorInspectorSmokeStatsResource(),
                [
                    Electron2D.ResourceObjectPropertyMetadata.Create<EditorInspectorSmokeStatsResource, int>(
                        "max_health",
                        resource => resource.MaxHealth,
                        (resource, value) => resource.MaxHealth = value)
                ]));
    }

    private static IReadOnlyList<EditorInspectorPropertyDescriptor> CreateDescriptors()
    {
        var metadata = Electron2D.ScriptObjectMetadataRegistry.GetByScriptName(ScriptName);
        var defaults = new Dictionary<string, (EditorInspectorPropertyKind Kind, Electron2D.SerializedPropertyValue Value)>(StringComparer.Ordinal)
        {
            ["active"] = (EditorInspectorPropertyKind.Primitive, Electron2D.SerializedPropertyValue.FromVariant(false)),
            ["display_name"] = (EditorInspectorPropertyKind.Primitive, Electron2D.SerializedPropertyValue.FromVariant("Player")),
            ["flags"] = (EditorInspectorPropertyKind.Flags, Electron2D.SerializedPropertyValue.FromEnum(EditorInspectorSmokeFlags.None)),
            ["health"] = (EditorInspectorPropertyKind.Primitive, Electron2D.SerializedPropertyValue.FromVariant(10)),
            ["mode"] = (EditorInspectorPropertyKind.Enum, Electron2D.SerializedPropertyValue.FromEnum(EditorInspectorSmokeMode.Idle)),
            ["stats"] = (EditorInspectorPropertyKind.Resource, Electron2D.SerializedPropertyValue.InternalResource(1)),
            ["tags"] = (EditorInspectorPropertyKind.Array, Electron2D.SerializedPropertyValue.FromArray(
            [
                Electron2D.SerializedPropertyValue.FromVariant("player")
            ])),
            ["target_path"] = (EditorInspectorPropertyKind.NodePath, Electron2D.SerializedPropertyValue.FromVariant(new Electron2D.NodePath("Root/Player")))
        };

        return metadata.ExportedProperties.Select(property =>
        {
            var fallback = defaults[property.Name];
            return EditorInspectorPropertyDescriptor.Exported(
                property.Name,
                property.ValueType,
                fallback.Kind,
                fallback.Value);
        }).ToArray();
    }

    private static Electron2D.SceneFileDocument CreateInitialScene()
    {
        return new Electron2D.SceneFileDocument(
            externalReferences: null,
            internalResources:
            [
                new Electron2D.SerializedResourceEntry(
                    1,
                    StatsResourceType,
                    new Dictionary<string, Electron2D.SerializedPropertyValue>(StringComparer.Ordinal)
                    {
                        ["max_health"] = Electron2D.SerializedPropertyValue.FromVariant(100)
                    })
            ],
            nodes:
            [
                new Electron2D.SceneFileNode(1, "Electron2D.Node2D", "Root", parentId: null, ownerId: null),
                new Electron2D.SceneFileNode(
                    2,
                    ScriptName,
                    "Player",
                    parentId: 1,
                    ownerId: 1,
                    properties: CreateDescriptors().ToDictionary(
                        descriptor => descriptor.Name,
                        descriptor => descriptor.DefaultValue,
                        StringComparer.Ordinal))
            ]);
    }

    private static Electron2D.SerializedPropertyValue ReadNodeProperty(
        Electron2D.SceneFileDocument document,
        string propertyName)
    {
        return document.Nodes.Single(node => node.Id == 2).Properties[propertyName];
    }

    private static Electron2D.SerializedPropertyValue ReadInternalResourceProperty(
        Electron2D.SceneFileDocument document,
        int resourceId,
        string propertyName)
    {
        return document.InternalResources.Single(resource => resource.Id == resourceId).Properties[propertyName];
    }

    private static string FormatFlags(string enumName)
    {
        return enumName.Replace(", ", "|", StringComparison.Ordinal);
    }

    private enum EditorInspectorSmokeMode
    {
        Idle = 0,
        Attack = 1
    }

    [Flags]
    private enum EditorInspectorSmokeFlags
    {
        None = 0,
        Air = 1,
        Ground = 2
    }

    private sealed class EditorInspectorSmokeStatsResource : Electron2D.Resource
    {
        public int MaxHealth { get; set; }
    }

    private sealed class EditorInspectorSmokeScript : Electron2D.Node
    {
        [Electron2D.Export]
        public bool Active { get; set; }

        [Electron2D.Export]
        public string DisplayName { get; set; } = string.Empty;

        [Electron2D.Export]
        public EditorInspectorSmokeFlags Flags { get; set; }

        [Electron2D.Export]
        public int Health { get; set; }

        [Electron2D.Export]
        public EditorInspectorSmokeMode Mode { get; set; }

        [Electron2D.Export]
        public Electron2D.Resource? Stats { get; set; }

        [Electron2D.Export]
        public string[] Tags { get; set; } = [];

        [Electron2D.Export]
        public Electron2D.NodePath TargetPath { get; set; }

        public string RuntimeOnly { get; set; } = string.Empty;
    }
}
