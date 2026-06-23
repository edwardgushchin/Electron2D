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
using Electron2D.Editor.ProjectManagement;
using Electron2D.Editor.Shell;

namespace Electron2D.Editor.SpecializedEditors;

internal static class SpecializedEditorsSmoke
{
    private const string ProjectName = "SpecializedEditorsSmoke";
    private const string SpriteFramesResourcePath = "res://resources/player_frames.e2res";
    private const string TileSetResourcePath = "res://resources/terrain_tileset.e2res";
    private const string AnimationResourcePath = "res://resources/player_motion.e2res";
    private const string SceneResourcePath = "res://scenes/specialized-editors.scene.json";

    public static SpecializedEditorsSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        Directory.CreateDirectory(fullWorkRoot);
        var runRoot = Path.Combine(fullWorkRoot, "run-" + Guid.NewGuid().ToString("N"));
        var visualRoot = Path.Combine(fullWorkRoot, "visual");
        Directory.CreateDirectory(runRoot);
        Directory.CreateDirectory(visualRoot);

        var repositoryRoot = FindRepositoryRoot();
        var templateRoot = Path.Combine(repositoryRoot, "data", "templates", "electron2d-empty");
        var manager = new ProjectManager(templateRoot);
        var creation = manager.CreateProject(new ProjectCreateOptions(
            ProjectName,
            Path.Combine(runRoot, "projects"),
            Electron2DRendererProfileSetting.Compatibility));

        var userSettingsPath = Path.Combine(runRoot, "user", "user.e2settings.json");
        var openResult = manager.OpenProject(creation.ProjectPath, userSettingsPath);
        if (!openResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, openResult.Diagnostics));
        }

        var resourceDirectory = Path.Combine(creation.ProjectPath, "resources");
        var scenesDirectory = Path.Combine(creation.ProjectPath, "scenes");
        Directory.CreateDirectory(resourceDirectory);
        Directory.CreateDirectory(scenesDirectory);

        var spriteFramesPath = ToProjectFilePath(creation.ProjectPath, SpriteFramesResourcePath);
        var tileSetPath = ToProjectFilePath(creation.ProjectPath, TileSetResourcePath);
        var animationPath = ToProjectFilePath(creation.ProjectPath, AnimationResourcePath);
        var scenePath = ToProjectFilePath(creation.ProjectPath, SceneResourcePath);

        var spriteFramesText = WriteSerializedResource(spriteFramesPath, CreateSpriteFramesDocument());
        var tileSet = CreateTileSet();
        var tileSetText = WriteSerializedResource(tileSetPath, ResourceObjectSerializer.Capture(tileSet, TileSetResourcePath));
        var animationText = WriteSerializedResource(animationPath, CreateAnimationDocument());

        var tileMapLayer = CreateTileMapLayer(tileSet);
        var usedRect = tileMapLayer.GetUsedRect();
        var sceneText = WriteSceneFile(scenePath, CreateSceneDocument(usedRect));

        var settings = LoadProjectSettings(creation.ProjectSettingsPath);
        settings.MainScene = SceneResourcePath["res://".Length..];
        Electron2DSettingsStore.SaveProject(creation.ProjectSettingsPath, settings);

        var spriteFramesRoundTrip = RoundTripsSerializedResource(spriteFramesPath, spriteFramesText);
        var tileMapRoundTrip = RoundTripsSerializedResource(tileSetPath, tileSetText) &&
            FormatRect(usedRect) == "0,0,3,2";
        var animationTimelineRoundTrip = RoundTripsSerializedResource(animationPath, animationText);
        var sceneRoundTrip = RoundTripsScene(scenePath, sceneText);

        var spriteAnimations = FormatSpriteAnimations(SerializedResourceTextSerializer.Deserialize(File.ReadAllText(spriteFramesPath)));
        var animationTracks = FormatAnimationTracks(SerializedResourceTextSerializer.Deserialize(File.ReadAllText(animationPath)));
        var tileMapUsedRect = FormatRect(usedRect);

        var snapshot = new SpecializedEditorsVisualSnapshot(
            creation.ProjectPath,
            spriteFramesPath,
            tileSetPath,
            animationPath,
            scenePath,
            spriteAnimations,
            tileMapUsedRect,
            animationTracks);
        var visual = SpecializedEditorsVisualHarness.WriteArtifacts(snapshot, visualRoot);
        var regions = SpecializedEditorsVisualHarness.CreateVisualRegions(snapshot);
        var pointerInteractionObserved = SpecializedEditorsVisualHarness.DispatchPalettePointer(regions);
        var keyboardInteractionObserved = SpecializedEditorsVisualHarness.DispatchKeyboardSave();
        var window = WindowHost.PresentStaticCanvas(visual.Canvas, smokeFrameCount: 4);
        var screenshotReviewed = visual.ScreenshotReviewed &&
            spriteFramesRoundTrip &&
            tileMapRoundTrip &&
            animationTimelineRoundTrip &&
            sceneRoundTrip &&
            window.WindowCreated &&
            window.WindowShown &&
            window.FramePresented &&
            pointerInteractionObserved &&
            keyboardInteractionObserved &&
            visual.TextOverflowCount == 0 &&
            visual.ForbiddenUiMatchCount == 0;

        SpecializedEditorsVisualHarness.UpdateWindowAnalysis(
            visual.AnalysisPath,
            window,
            pointerInteractionObserved,
            keyboardInteractionObserved,
            screenshotReviewed);

        return new SpecializedEditorsSmokeResult(
            creation.ProjectPath,
            creation.ProjectSettingsPath,
            spriteFramesPath,
            tileSetPath,
            animationPath,
            scenePath,
            spriteAnimations,
            tileMapUsedRect,
            animationTracks,
            spriteFramesRoundTrip,
            tileMapRoundTrip,
            animationTimelineRoundTrip,
            sceneRoundTrip,
            window.WindowCreated,
            window.WindowShown,
            window.FramePresented,
            window.EventPumpObserved,
            pointerInteractionObserved,
            keyboardInteractionObserved,
            visual.TextOverflowCount,
            visual.ClickableControlCount,
            visual.ForbiddenUiMatchCount,
            screenshotReviewed,
            visual.ScreenshotPath,
            visual.AnalysisPath);
    }

    private static SerializedResourceDocument CreateSpriteFramesDocument()
    {
        return new SerializedResourceDocument(
            ResourceUid.CreateIdForPath(SpriteFramesResourcePath),
            typeof(SpriteFrames).FullName!,
            SpriteFramesResourcePath,
            externalReferences:
            [
                new ResourceFileExternalReference(1, ResourceUid.CreateIdForPath("res://textures/player_idle.png"), "res://textures/player_idle.png", typeof(Texture2D).FullName!),
                new ResourceFileExternalReference(2, ResourceUid.CreateIdForPath("res://textures/player_run.png"), "res://textures/player_run.png", typeof(Texture2D).FullName!)
            ],
            properties: new Dictionary<string, SerializedPropertyValue>(StringComparer.Ordinal)
            {
                ["animations"] = Array(
                [
                    Object(
                        Property("fps", Value(6f)),
                        Property("frames", Array(
                        [
                            Object(
                                Property("duration", Value(1f)),
                                Property("texture", SerializedPropertyValue.ExternalResource(1)))
                        ])),
                        Property("loop_mode", EnumValue(SpriteFrames.LoopModeEnum.None)),
                        Property("name", Value("idle"))),
                    Object(
                        Property("fps", Value(12f)),
                        Property("frames", Array(
                        [
                            Object(
                                Property("duration", Value(0.5f)),
                                Property("texture", SerializedPropertyValue.ExternalResource(2))),
                            Object(
                                Property("duration", Value(0.75f)),
                                Property("texture", SerializedPropertyValue.ExternalResource(1)))
                        ])),
                        Property("loop_mode", EnumValue(SpriteFrames.LoopModeEnum.Linear)),
                        Property("name", Value("run")))
                ])
            });
    }

    private static TileSet CreateTileSet()
    {
        var tileSet = new TileSet
        {
            TileSize = new Vector2I(16, 16)
        };
        var source = new TileSetAtlasSource
        {
            TextureRegionSize = new Vector2I(16, 16)
        };

        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                source.CreateTile(new Vector2I(x, y));
            }
        }

        tileSet.AddSource(source, atlasSourceIdOverride: 1);
        return tileSet;
    }

    private static TileMapLayer CreateTileMapLayer(TileSet tileSet)
    {
        var layer = new TileMapLayer
        {
            TileSet = tileSet
        };

        layer.SetCell(new Vector2I(0, 0), sourceId: 1, atlasCoords: new Vector2I(0, 0));
        layer.SetCell(new Vector2I(1, 0), sourceId: 1, atlasCoords: new Vector2I(1, 0));
        layer.SetCell(new Vector2I(2, 1), sourceId: 1, atlasCoords: new Vector2I(2, 1));
        return layer;
    }

    private static SerializedResourceDocument CreateAnimationDocument()
    {
        return new SerializedResourceDocument(
            ResourceUid.CreateIdForPath(AnimationResourcePath),
            typeof(Animation).FullName!,
            AnimationResourcePath,
            properties: new Dictionary<string, SerializedPropertyValue>(StringComparer.Ordinal)
            {
                ["length"] = Value(1.25d),
                ["loop_mode"] = EnumValue(Animation.LoopModeEnum.Linear),
                ["tracks"] = Array(
                [
                    Object(
                        Property("enabled", Value(true)),
                        Property("interpolation", EnumValue(Animation.InterpolationTypeEnum.Linear)),
                        Property("keys", Array(
                        [
                            Object(
                                Property("time", Value(0d)),
                                Property("value", Value(0f))),
                            Object(
                                Property("time", Value(1.25d)),
                                Property("value", Value(96f)))
                        ])),
                        Property("path", Value("Player:position:x")),
                        Property("track", Value("position")),
                        Property("type", EnumValue(Animation.TrackTypeEnum.Value))),
                    Object(
                        Property("enabled", Value(true)),
                        Property("keys", Array(
                        [
                            Object(
                                Property("args", Array(System.Array.Empty<SerializedPropertyValue>())),
                                Property("method", Value("OnStep")),
                                Property("time", Value(0.75d)))
                        ])),
                        Property("path", Value("Player")),
                        Property("track", Value("call")),
                        Property("type", EnumValue(Animation.TrackTypeEnum.Method)))
                ])
            });
    }

    private static SceneFileDocument CreateSceneDocument(Rect2I usedRect)
    {
        return new SceneFileDocument(
            externalReferences:
            [
                new ResourceFileExternalReference(1, ResourceUid.CreateIdForPath(SpriteFramesResourcePath), SpriteFramesResourcePath, typeof(SpriteFrames).FullName!),
                new ResourceFileExternalReference(2, ResourceUid.CreateIdForPath(TileSetResourcePath), TileSetResourcePath, typeof(TileSet).FullName!),
                new ResourceFileExternalReference(3, ResourceUid.CreateIdForPath(AnimationResourcePath), AnimationResourcePath, typeof(Animation).FullName!)
            ],
            internalResources: [],
            nodes:
            [
                new SceneFileNode(
                    id: 1,
                    type: typeof(Node2D).FullName!,
                    name: "Root",
                    parentId: null,
                    ownerId: null),
                new SceneFileNode(
                    id: 2,
                    type: typeof(AnimatedSprite2D).FullName!,
                    name: "Player",
                    parentId: 1,
                    ownerId: 1,
                    properties: new Dictionary<string, SerializedPropertyValue>(StringComparer.Ordinal)
                    {
                        ["animation"] = Value("run"),
                        ["autoplay"] = Value("run"),
                        ["position"] = Value(new Vector2(32f, 48f)),
                        ["sprite_frames"] = SerializedPropertyValue.ExternalResource(1)
                    }),
                new SceneFileNode(
                    id: 3,
                    type: typeof(TileMapLayer).FullName!,
                    name: "Ground",
                    parentId: 1,
                    ownerId: 1,
                    properties: new Dictionary<string, SerializedPropertyValue>(StringComparer.Ordinal)
                    {
                        ["cells"] = Array(
                        [
                            TileCell(new Vector2I(0, 0), new Vector2I(0, 0)),
                            TileCell(new Vector2I(1, 0), new Vector2I(1, 0)),
                            TileCell(new Vector2I(2, 1), new Vector2I(2, 1))
                        ]),
                        ["tile_set"] = SerializedPropertyValue.ExternalResource(2),
                        ["used_rect"] = Value(FormatRect(usedRect))
                    }),
                new SceneFileNode(
                    id: 4,
                    type: typeof(AnimationPlayer).FullName!,
                    name: "AnimationPlayer",
                    parentId: 1,
                    ownerId: 1,
                    properties: new Dictionary<string, SerializedPropertyValue>(StringComparer.Ordinal)
                    {
                        ["animation"] = SerializedPropertyValue.ExternalResource(3),
                        ["assigned_animation"] = Value("move"),
                        ["root_node"] = Value(new NodePath(".."))
                    })
            ]);
    }

    private static SerializedPropertyValue TileCell(Vector2I coords, Vector2I atlasCoords)
    {
        return Object(
            Property("alternative_tile", Value(0)),
            Property("atlas_coords", Value(atlasCoords)),
            Property("coords", Value(coords)),
            Property("source_id", Value(1)));
    }

    private static Electron2DProjectSettings LoadProjectSettings(string projectSettingsPath)
    {
        var result = Electron2DSettingsStore.LoadProject(projectSettingsPath);
        if (!result.Succeeded || result.Settings is null)
        {
            throw new InvalidOperationException(string.Join(
                Environment.NewLine,
                result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        }

        return result.Settings;
    }

    private static string WriteSerializedResource(string path, SerializedResourceDocument document)
    {
        var text = SerializedResourceTextSerializer.Serialize(document);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, text);
        return text;
    }

    private static string WriteSceneFile(string path, SceneFileDocument document)
    {
        var text = SceneFileTextSerializer.Serialize(document);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, text);
        return text;
    }

    private static bool RoundTripsSerializedResource(string path, string expected)
    {
        var loaded = SerializedResourceTextSerializer.Deserialize(File.ReadAllText(path));
        return string.Equals(SerializedResourceTextSerializer.Serialize(loaded), expected, StringComparison.Ordinal);
    }

    private static bool RoundTripsScene(string path, string expected)
    {
        var loaded = SceneFileTextSerializer.Deserialize(File.ReadAllText(path));
        return string.Equals(SceneFileTextSerializer.Serialize(loaded), expected, StringComparison.Ordinal);
    }

    private static string FormatSpriteAnimations(SerializedResourceDocument document)
    {
        return string.Join(
            '|',
            ReadArray(document.Properties["animations"], "SpriteFrames animations")
                .Select(animation => ReadString(ReadObject(animation, "SpriteFrames animation"), "name"))
                .Order(StringComparer.Ordinal));
    }

    private static string FormatAnimationTracks(SerializedResourceDocument document)
    {
        return string.Join(
            '|',
            ReadArray(document.Properties["tracks"], "Animation tracks")
                .Select(track => ReadString(ReadObject(track, "Animation track"), "track")));
    }

    private static string ToProjectFilePath(string projectPath, string resourcePath)
    {
        var relative = resourcePath.StartsWith("res://", StringComparison.Ordinal)
            ? resourcePath["res://".Length..]
            : resourcePath;
        return Path.Combine(projectPath, relative.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string FormatRect(Rect2I rect)
    {
        return string.Join(',', rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y);
    }

    private static SerializedPropertyDictionaryEntry Property(string name, SerializedPropertyValue value)
    {
        return new SerializedPropertyDictionaryEntry(Value(name), value);
    }

    private static SerializedPropertyValue Object(params SerializedPropertyDictionaryEntry[] properties)
    {
        return SerializedPropertyValue.FromDictionary(properties);
    }

    private static SerializedPropertyValue Array(IEnumerable<SerializedPropertyValue> values)
    {
        return SerializedPropertyValue.FromArray(values);
    }

    private static SerializedPropertyValue Value(string value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(bool value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(int value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(float value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(double value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(Vector2 value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(Vector2I value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue Value(NodePath value)
    {
        return SerializedPropertyValue.FromVariant(value);
    }

    private static SerializedPropertyValue EnumValue(Enum value)
    {
        return SerializedPropertyValue.FromEnum(value);
    }

    private static IReadOnlyList<SerializedPropertyValue> ReadArray(SerializedPropertyValue value, string context)
    {
        if (value.Kind != SerializedPropertyValueKind.Array)
        {
            throw new FormatException($"{context} must be an array.");
        }

        return value.Items;
    }

    private static IReadOnlyDictionary<string, SerializedPropertyValue> ReadObject(SerializedPropertyValue value, string context)
    {
        if (value.Kind != SerializedPropertyValueKind.Dictionary)
        {
            throw new FormatException($"{context} must be a dictionary.");
        }

        return value.DictionaryEntries.ToDictionary(
            entry => entry.Key.VariantValue.AsString(),
            entry => entry.Value,
            StringComparer.Ordinal);
    }

    private static string ReadString(IReadOnlyDictionary<string, SerializedPropertyValue> properties, string name)
    {
        return properties.TryGetValue(name, out var value)
            ? value.VariantValue.AsString()
            : throw new FormatException($"Serialized property '{name}' is missing.");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
