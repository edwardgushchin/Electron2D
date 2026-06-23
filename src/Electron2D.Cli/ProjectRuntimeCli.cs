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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Electron2D;
using Electron2D.ProjectSystem;

internal static partial class Electron2DCommandLine
{
    private static readonly JsonSerializerOptions ProjectRuntimeJsonOptions = new()
    {
        WriteIndented = true
    };

    private static bool IsProjectRuntimeRun(CliOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.GetOption("--play-script") is not null ||
            options.GetOption("--screenshot") is not null)
        {
            return true;
        }

        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        return options.Values.Count == 0 && ProjectFileLocator.TryResolveProjectFilePath(projectRoot, out _);
    }

    private static int RunProjectRuntime(CliOptions options, TextWriter output, TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var projectSettingsPath = ProjectFileLocator.ResolveProjectFilePath(projectRoot);
        var loadResult = Electron2DSettingsStore.LoadProject(projectSettingsPath);
        if (!loadResult.Succeeded || loadResult.Settings is null)
        {
            var diagnostics = loadResult.Diagnostics
                .Select(diagnostic => CreateCliDiagnostic(diagnostic.Code, diagnostic.Message))
                .ToArray();
            return WriteResult(
                CliResult.Failure(
                    "run",
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Project settings could not be loaded.",
                    diagnostics.Length == 0 ? [CreateCliDiagnostic("E2D-CLI-0002", "Project settings could not be loaded.")] : diagnostics,
                    new JsonObject()),
                output,
                error);
        }

        var settings = loadResult.Settings;
        settings.ApplyToRuntime();

        using var resourceMount = ResourceFileSystem.MountProjectRoot(projectRoot);
        var projectFile = ResolveSingleProjectFile(projectRoot, options);
        var scene = LoadMainScene(projectRoot, settings.MainScene, options);
        var assemblyPath = BuildAndResolveAssembly(projectRoot, projectFile, options);
        var gameAssembly = LoadProjectAssembly(assemblyPath);
        var metadata = ReadProjectMetadata(projectFile);
        var mainNode = CreateSceneRoot(projectRoot, settings, scene, gameAssembly, metadata.RootNamespace, options);
        return RunSceneTree(projectRoot, settings, mainNode, options, output);
    }

    private static string ResolveSingleProjectFile(string projectRoot, CliOptions options)
    {
        var projects = Directory.EnumerateFiles(projectRoot, "*.csproj", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        return projects.Length switch
        {
            1 => projects[0],
            0 => throw new CliCommandException(
                "run",
                options,
                "Project run requires one C# project file in the project root.",
                CreateCliDiagnostic("E2D-CLI-0002", "Project run requires one C# project file in the project root.")),
            _ => throw new CliCommandException(
                "run",
                options,
                "Project run found more than one C# project file in the project root.",
                CreateCliDiagnostic("E2D-CLI-0002", "Project run found more than one C# project file in the project root."))
        };
    }

    private static ProjectRuntimeScene LoadMainScene(string projectRoot, string mainScene, CliOptions options)
    {
        var normalizedScene = ProjectDocumentPaths.NormalizeRelativePath(mainScene);
        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, normalizedScene.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(fullPath))
        {
            throw new CliCommandException(
                "run",
                options,
                $"Project main scene was not found: {normalizedScene}.",
                CreateCliDiagnostic("E2D-CLI-0002", $"Project main scene was not found: {normalizedScene}."));
        }

        using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
        var root = document.RootElement.GetProperty("root");
        return LoadMainSceneFromDocument(normalizedScene, root, options);
    }

    private static ProjectRuntimeScene LoadMainSceneFromText(string scenePath, string sceneText, CliOptions options)
    {
        var normalizedScene = ProjectDocumentPaths.NormalizeRelativePath(scenePath);
        using var document = JsonDocument.Parse(sceneText);
        var root = document.RootElement.GetProperty("root");
        return LoadMainSceneFromDocument(normalizedScene, root, options);
    }

    private static ProjectRuntimeScene LoadMainSceneFromDocument(string normalizedScene, JsonElement root, CliOptions options)
    {
        var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? string.Empty : string.Empty;
        var name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
        var script = root.TryGetProperty("script", out var scriptElement) ? scriptElement.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(script))
        {
            throw new CliCommandException(
                "run",
                options,
                "Project main scene root must declare a script in Preview.",
                CreateCliDiagnostic("E2D-CLI-0002", "Project main scene root must declare a script in Preview."));
        }

        return new ProjectRuntimeScene(
            normalizedScene,
            type,
            name,
            ProjectDocumentPaths.NormalizeRelativePath(script),
            root.Clone(),
            ReadSceneChildren(root));
    }

    private static string BuildAndResolveAssembly(string projectRoot, string projectFile, CliOptions options)
    {
        return BuildAndResolveAssembly(projectRoot, projectFile, options, "Debug");
    }

    private static string BuildAndResolveAssembly(string projectRoot, string projectFile, CliOptions options, string configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);
        var build = RunProcess(projectRoot, "dotnet", ["build", projectFile, "--configuration", configuration, "--nologo"]);
        if (build.ExitCode != 0)
        {
            throw new CliCommandException(
                "run",
                options,
                "Project build failed before runtime launch.",
                CreateCliDiagnostic("E2D-CLI-0002", "Project build failed before runtime launch." + Environment.NewLine + build.Output + build.Error));
        }

        var metadata = ReadProjectMetadata(projectFile);
        foreach (var expectedPath in new[]
        {
            Path.Combine(Path.GetDirectoryName(projectFile) ?? projectRoot, "bin", configuration, metadata.TargetFramework, metadata.AssemblyName + ".dll"),
            Path.Combine(Path.GetDirectoryName(projectFile) ?? projectRoot, ".electron2d", "build", "bin", configuration, metadata.TargetFramework, metadata.AssemblyName + ".dll")
        })
        {
            if (File.Exists(expectedPath))
            {
                return Path.GetFullPath(expectedPath);
            }
        }

        var candidateRoots = new[]
        {
            Path.Combine(projectRoot, "bin"),
            Path.Combine(projectRoot, ".electron2d", "build", "bin")
        };
        var candidate = candidateRoots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, metadata.AssemblyName + ".dll", SearchOption.AllDirectories))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (candidate is not null)
        {
            return Path.GetFullPath(candidate);
        }

        throw new CliCommandException(
            "run",
            options,
            "Project build did not produce a runtime assembly.",
            CreateCliDiagnostic("E2D-CLI-0002", "Project build did not produce a runtime assembly."));
    }

    private static bool TryRunExportedPlayer(string[] args, TextWriter output, TextWriter error, out int exitCode)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var manifestPath = Path.Combine(baseDirectory, "electron2d.pack.json");
        if (!File.Exists(manifestPath))
        {
            exitCode = 0;
            return false;
        }

        var options = CliOptions.Parse("run", args);
        var manifest = ReadExportedPlayerManifest(manifestPath);
        using var resourceMount = ResourceFileSystem.MountPacks(baseDirectory, manifestPath);
        var settingsResult = Electron2DSettingsStore.LoadProjectFromText(
            ResourceFileSystem.ReadAllText("res://" + manifest.ProjectFile),
            "res://" + manifest.ProjectFile);
        if (!settingsResult.Succeeded || settingsResult.Settings is null)
        {
            var message = settingsResult.Diagnostics.Length == 0
                ? "Exported project settings could not be loaded."
                : string.Join(Environment.NewLine, settingsResult.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
            throw new CommandLineException(message);
        }

        var settings = settingsResult.Settings;
        settings.ApplyToRuntime();
        var scene = LoadMainSceneFromText(
            settings.MainScene,
            ResourceFileSystem.ReadAllText("res://" + ProjectDocumentPaths.NormalizeRelativePath(settings.MainScene)),
            options);
        var assemblyPath = ResolveExportedAssemblyPath(baseDirectory, manifest.ProjectAssembly);
        var assembly = LoadProjectAssembly(assemblyPath);
        var mainNode = CreateSceneRoot(baseDirectory, settings, scene, assembly, manifest.RootNamespace, options);
        exitCode = RunSceneTree(baseDirectory, settings, mainNode, options, output, ResolveExportedSavePath(settings.Name));
        return true;
    }

    private static Assembly LoadProjectAssembly(string assemblyPath)
    {
        var resolver = new AssemblyDependencyResolver(assemblyPath);
        Assembly? ResolveDependency(AssemblyLoadContext context, AssemblyName name)
        {
            var resolvedPath = resolver.ResolveAssemblyToPath(name);
            return resolvedPath is null ? null : context.LoadFromAssemblyPath(resolvedPath);
        }

        AssemblyLoadContext.Default.Resolving += ResolveDependency;
        try
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        }
        finally
        {
            AssemblyLoadContext.Default.Resolving -= ResolveDependency;
        }
    }

    private static Node CreateSceneRoot(
        string projectRoot,
        Electron2DProjectSettings settings,
        ProjectRuntimeScene scene,
        Assembly assembly,
        string rootNamespace,
        CliOptions options)
    {
        var scriptTypeName = ResolveScriptTypeName(rootNamespace, scene.ScriptPath);
        var scriptType = assembly.GetType(scriptTypeName, throwOnError: false, ignoreCase: false);
        if (scriptType is null || !typeof(Node).IsAssignableFrom(scriptType))
        {
            throw new CliCommandException(
                "run",
                options,
                $"Scene script '{scriptTypeName}' was not found or does not inherit Node.",
                CreateCliDiagnostic("E2D-CLI-0002", $"Scene script '{scriptTypeName}' was not found or does not inherit Node."));
        }

        var node = (Node?)Activator.CreateInstance(scriptType, nonPublic: true);
        if (node is null)
        {
            throw new CliCommandException(
                "run",
                options,
                $"Scene script '{scriptTypeName}' could not be created.",
                CreateCliDiagnostic("E2D-CLI-0002", $"Scene script '{scriptTypeName}' could not be created."));
        }

        node.Name = string.IsNullOrWhiteSpace(scene.Name) ? settings.Name : scene.Name;
        ApplySceneNodeProperties(node, scene.RootProperties);
        foreach (var child in scene.Children)
        {
            node.AddChild(CreateSceneNode(child, options));
        }

        ApplySceneScriptProperties(node, scene.RootProperties, options);

        var projectRootProperty = scriptType.GetProperty("ProjectRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (projectRootProperty is not null && projectRootProperty.PropertyType == typeof(string))
        {
            projectRootProperty.SetValue(node, projectRoot);
        }

        return node;
    }

    private static IReadOnlyList<ProjectRuntimeSceneNode> ReadSceneChildren(JsonElement element)
    {
        if (!element.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<ProjectRuntimeSceneNode>();
        foreach (var child in children.EnumerateArray())
        {
            result.Add(ReadSceneNode(child));
        }

        return result;
    }

    private static ProjectRuntimeSceneNode ReadSceneNode(JsonElement element)
    {
        var type = element.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? string.Empty : string.Empty;
        var name = element.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
        return new ProjectRuntimeSceneNode(type, name, element.Clone(), ReadSceneChildren(element));
    }

    private static Node CreateSceneNode(ProjectRuntimeSceneNode sceneNode, CliOptions options)
    {
        var node = sceneNode.Type switch
        {
            "Node" => new Node(),
            "Node2D" => new Node2D(),
            "CanvasLayer" => new CanvasLayer(),
            "Control" => new Control(),
            "Panel" => new Panel(),
            "Label" => new Label(),
            "Button" => new Button(),
            "TextureRect" => new TextureRect(),
            "Sprite2D" => new Sprite2D(),
            "TileMapLayer" => new TileMapLayer(),
            "CharacterBody2D" => new CharacterBody2D(),
            "CollisionShape2D" => new CollisionShape2D(),
            "Camera2D" => new Camera2D(),
            "AnimatedSprite2D" => new AnimatedSprite2D(),
            "AnimationPlayer" => new AnimationPlayer(),
            "AudioStreamPlayer" => new AudioStreamPlayer(),
            _ => throw new CliCommandException(
                "run",
                options,
                $"Scene node type '{sceneNode.Type}' is not supported by Preview runtime loading.",
                CreateCliDiagnostic("E2D-CLI-0002", $"Scene node type '{sceneNode.Type}' is not supported by Preview runtime loading."))
        };

        node.Name = string.IsNullOrWhiteSpace(sceneNode.Name) ? sceneNode.Type : sceneNode.Name;
        ApplySceneNodeProperties(node, sceneNode.Properties);
        foreach (var child in sceneNode.Children)
        {
            node.AddChild(CreateSceneNode(child, options));
        }

        return node;
    }

    private static void ApplySceneNodeProperties(Node node, JsonElement properties)
    {
        if (properties.TryGetProperty("processMode", out var processMode) &&
            processMode.ValueKind == JsonValueKind.String &&
            Enum.TryParse<ProcessMode>(processMode.GetString(), ignoreCase: true, out var processModeValue))
        {
            node.ProcessMode = processModeValue;
        }

        if (properties.TryGetProperty("visible", out var visible) &&
            visible.ValueKind is JsonValueKind.True or JsonValueKind.False &&
            node is CanvasItem canvasItem)
        {
            canvasItem.Visible = visible.GetBoolean();
        }

        if (properties.TryGetProperty("zIndex", out var zIndex) &&
            zIndex.ValueKind == JsonValueKind.Number &&
            zIndex.TryGetInt32(out var zIndexValue) &&
            node is CanvasItem zCanvasItem)
        {
            zCanvasItem.ZIndex = zIndexValue;
        }

        if (properties.TryGetProperty("position", out var position) && TryReadVector2(position, out var positionValue))
        {
            if (node is Control control)
            {
                control.Position = positionValue;
            }
            else if (node is Node2D node2D)
            {
                node2D.Position = positionValue;
            }
        }

        if (properties.TryGetProperty("size", out var size) &&
            TryReadVector2(size, out var sizeValue) &&
            node is Control sizedControl)
        {
            sizedControl.Size = sizeValue;
        }

        if (properties.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
        {
            switch (node)
            {
                case Label label:
                    label.Text = text.GetString() ?? string.Empty;
                    break;
                case Button button:
                    button.Text = text.GetString() ?? string.Empty;
                    break;
            }
        }

        if (properties.TryGetProperty("horizontalAlignment", out var horizontalAlignment) &&
            horizontalAlignment.ValueKind == JsonValueKind.String &&
            node is Label labelWithHorizontalAlignment &&
            Enum.TryParse<HorizontalAlignment>(horizontalAlignment.GetString(), ignoreCase: true, out var horizontalAlignmentValue))
        {
            labelWithHorizontalAlignment.HorizontalAlignment = horizontalAlignmentValue;
        }

        if (properties.TryGetProperty("verticalAlignment", out var verticalAlignment) &&
            verticalAlignment.ValueKind == JsonValueKind.String &&
            node is Label labelWithVerticalAlignment &&
            Enum.TryParse<VerticalAlignment>(verticalAlignment.GetString(), ignoreCase: true, out var verticalAlignmentValue))
        {
            labelWithVerticalAlignment.VerticalAlignment = verticalAlignmentValue;
        }

        if (properties.TryGetProperty("mouseFilter", out var mouseFilter) &&
            mouseFilter.ValueKind == JsonValueKind.String &&
            node is Control mouseControl &&
            Enum.TryParse<MouseFilter>(mouseFilter.GetString(), ignoreCase: true, out var mouseFilterValue))
        {
            mouseControl.MouseFilter = mouseFilterValue;
        }

        if (properties.TryGetProperty("themeColors", out var themeColors) &&
            themeColors.ValueKind == JsonValueKind.Object &&
            node is Control themedControl)
        {
            foreach (var themeColor in themeColors.EnumerateObject())
            {
                if (TryReadColor(themeColor.Value, out var themeColorValue))
                {
                    themedControl.AddThemeColorOverride(themeColor.Name, themeColorValue);
                }
            }
        }

        if (node is Control fontControl)
        {
            if (properties.TryGetProperty("font", out var font) &&
                font.ValueKind == JsonValueKind.String)
            {
                fontControl.AddThemeFontOverride("font", ReadFont(font.GetString() ?? string.Empty));
            }

            if (properties.TryGetProperty("fontSize", out var fontSize) &&
                fontSize.ValueKind == JsonValueKind.Number &&
                fontSize.TryGetInt32(out var fontSizeValue))
            {
                fontControl.AddThemeFontSizeOverride("font_size", fontSizeValue);
            }
        }

        if (properties.TryGetProperty("texture", out var texture) &&
            texture.ValueKind == JsonValueKind.String)
        {
            var textureResource = ImageTexture.LoadFromFile(texture.GetString() ?? string.Empty);
            switch (node)
            {
                case TextureRect textureRect:
                    textureRect.Texture = textureResource;
                    break;
                case Sprite2D sprite:
                    sprite.Texture = textureResource;
                    break;
            }
        }

        if (node is CollisionShape2D collisionShape &&
            properties.TryGetProperty("shape", out var shape) &&
            shape.ValueKind == JsonValueKind.Object)
        {
            collisionShape.Shape = ReadShape2D(shape);
        }

        if (node is CharacterBody2D characterBody &&
            properties.TryGetProperty("floorSnapLength", out var floorSnapLength) &&
            floorSnapLength.ValueKind == JsonValueKind.Number &&
            floorSnapLength.TryGetSingle(out var floorSnapLengthValue))
        {
            characterBody.FloorSnapLength = floorSnapLengthValue;
        }

        if (node is TileMapLayer tileMapLayer)
        {
            if (properties.TryGetProperty("tileSet", out var tileSet) && tileSet.ValueKind == JsonValueKind.Object)
            {
                tileMapLayer.TileSet = ReadTileSet(tileSet);
            }

            if (properties.TryGetProperty("cells", out var cells) && cells.ValueKind == JsonValueKind.Array)
            {
                ApplyTileMapCells(tileMapLayer, cells);
            }
        }

        if (node is AnimatedSprite2D animatedSprite)
        {
            if (properties.TryGetProperty("spriteFrames", out var spriteFrames) && spriteFrames.ValueKind == JsonValueKind.Object)
            {
                animatedSprite.SpriteFrames = ReadSpriteFrames(spriteFrames);
            }

            if (properties.TryGetProperty("animation", out var animation) && animation.ValueKind == JsonValueKind.String)
            {
                animatedSprite.Animation = animation.GetString() ?? string.Empty;
            }

            if (properties.TryGetProperty("autoplay", out var autoplay) && autoplay.ValueKind == JsonValueKind.String)
            {
                animatedSprite.Autoplay = autoplay.GetString() ?? string.Empty;
            }
        }

        if (node is AudioStreamPlayer audioStreamPlayer &&
            properties.TryGetProperty("stream", out var stream) &&
            stream.ValueKind == JsonValueKind.Object)
        {
            audioStreamPlayer.Stream = ReadAudioStream(stream);
        }
    }

    private static void ApplySceneScriptProperties(Node node, JsonElement properties, CliOptions options)
    {
        if (!properties.TryGetProperty("scriptProperties", out var scriptProperties) ||
            scriptProperties.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var nodeType = node.GetType();
        foreach (var scriptProperty in scriptProperties.EnumerateObject())
        {
            var property = nodeType.GetProperty(
                scriptProperty.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null &&
                property.SetMethod is not null &&
                property.GetCustomAttribute<ExportAttribute>(inherit: true) is not null)
            {
                property.SetValue(node, ConvertSceneScriptValue(node, scriptProperty.Value, property.PropertyType));
                continue;
            }

            var field = nodeType.GetField(
                scriptProperty.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is not null &&
                field.GetCustomAttribute<ExportAttribute>(inherit: true) is not null)
            {
                field.SetValue(node, ConvertSceneScriptValue(node, scriptProperty.Value, field.FieldType));
                continue;
            }

            throw new CliCommandException(
                "run",
                options,
                $"Scene script property '{scriptProperty.Name}' was not found or is not exported.",
                CreateCliDiagnostic("E2D-CLI-0002", $"Scene script property '{scriptProperty.Name}' was not found or is not exported."));
        }
    }

    private static object ConvertSceneScriptValue(Node owner, JsonElement value, Type targetType)
    {
        if (targetType == typeof(NodePath) && value.ValueKind == JsonValueKind.String)
        {
            return new NodePath(value.GetString() ?? string.Empty);
        }

        if (typeof(Node).IsAssignableFrom(targetType) && value.ValueKind == JsonValueKind.String)
        {
            var path = new NodePath(value.GetString() ?? string.Empty);
            var node = owner.GetNode(path);
            return targetType.IsInstanceOfType(node)
                ? node
                : throw new FormatException($"Scene node path '{path}' does not point to a '{targetType.Name}'.");
        }

        if (targetType == typeof(string) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        if (targetType == typeof(bool) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        if (targetType == typeof(int) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer))
        {
            return integer;
        }

        if (targetType == typeof(float) && value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out var single))
        {
            return single;
        }

        if (targetType == typeof(double) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number;
        }

        throw new FormatException($"Scene script value cannot be converted to '{targetType.Name}'.");
    }

    private static Font ReadFont(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ResourceFileSystem.ReadAllBytes(source);
        var fileName = Path.GetFileNameWithoutExtension(source);
        var format = source.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)
            ? FontSourceFormat.Otf
            : FontSourceFormat.Ttf;
        return FontImportResourceFactory.CreateFont(new FontImportMetadata(
            source,
            ResourceUid.CreateIdForPath(source),
            format,
            string.IsNullOrWhiteSpace(fileName) ? "Scene Font" : fileName,
            "Regular",
            string.IsNullOrWhiteSpace(fileName) ? "Scene Font Regular" : fileName,
            string.IsNullOrWhiteSpace(fileName) ? "SceneFont-Regular" : fileName));
    }

    private static Shape2D ReadShape2D(JsonElement shape)
    {
        var type = shape.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? string.Empty : string.Empty;
        return type switch
        {
            "RectangleShape2D" => new RectangleShape2D
            {
                Size = shape.TryGetProperty("size", out var size) && TryReadVector2(size, out var sizeValue)
                    ? sizeValue
                    : Vector2.Zero
            },
            _ => throw new FormatException($"Scene shape type '{type}' is not supported by Preview runtime loading.")
        };
    }

    private static TileSet ReadTileSet(JsonElement tileSetElement)
    {
        var tileSet = new TileSet();
        if (tileSetElement.TryGetProperty("tileSize", out var tileSize) && TryReadVector2I(tileSize, out var tileSizeValue))
        {
            tileSet.TileSize = tileSizeValue;
        }

        if (!tileSetElement.TryGetProperty("sources", out var sources) || sources.ValueKind != JsonValueKind.Array)
        {
            return tileSet;
        }

        foreach (var sourceElement in sources.EnumerateArray())
        {
            var source = new TileSetAtlasSource();
            if (sourceElement.TryGetProperty("texture", out var texture) && texture.ValueKind == JsonValueKind.String)
            {
                var textureResource = ImageTexture.LoadFromFile(texture.GetString() ?? string.Empty);
                source.Texture = textureResource;
                source.TextureRegionSize = new Vector2I(textureResource.GetWidth(), textureResource.GetHeight());
            }

            if (sourceElement.TryGetProperty("textureRegionSize", out var regionSize) &&
                TryReadVector2I(regionSize, out var regionSizeValue))
            {
                source.TextureRegionSize = regionSizeValue;
            }

            if (sourceElement.TryGetProperty("tiles", out var tiles) && tiles.ValueKind == JsonValueKind.Array)
            {
                foreach (var tile in tiles.EnumerateArray())
                {
                    var atlas = tile.TryGetProperty("atlas", out var atlasValue) && TryReadVector2I(atlasValue, out var atlasCoords)
                        ? atlasCoords
                        : Vector2I.Zero;
                    source.CreateTile(atlas);
                    if (tile.TryGetProperty("collision", out var collision) && collision.ValueKind == JsonValueKind.Array)
                    {
                        ApplyTileCollision(source.GetTileData(atlas)!, collision);
                    }
                }
            }

            var sourceId = sourceElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out var idValue)
                ? idValue
                : -1;
            tileSet.AddSource(source, sourceId);
        }

        return tileSet;
    }

    private static void ApplyTileCollision(TileData tileData, JsonElement collision)
    {
        var polygons = collision.EnumerateArray().ToArray();
        tileData.SetCollisionPolygonsCount(0, polygons.Length);
        for (var index = 0; index < polygons.Length; index++)
        {
            var polygon = polygons[index];
            if (!polygon.TryGetProperty("points", out var points) || points.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            tileData.SetCollisionPolygonPoints(0, index, points.EnumerateArray()
                .Where(point => TryReadVector2(point, out _))
                .Select(point =>
                {
                    TryReadVector2(point, out var value);
                    return value;
                })
                .ToArray());

            if (polygon.TryGetProperty("oneWay", out var oneWay) && oneWay.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                tileData.SetCollisionPolygonOneWay(0, index, oneWay.GetBoolean());
            }

            if (polygon.TryGetProperty("oneWayMargin", out var oneWayMargin) &&
                oneWayMargin.ValueKind == JsonValueKind.Number &&
                oneWayMargin.TryGetSingle(out var margin))
            {
                tileData.SetCollisionPolygonOneWayMargin(0, index, margin);
            }
        }
    }

    private static void ApplyTileMapCells(TileMapLayer tileMapLayer, JsonElement cells)
    {
        foreach (var cell in cells.EnumerateArray())
        {
            if (!cell.TryGetProperty("coords", out var coords) || !TryReadVector2I(coords, out var coordsValue))
            {
                continue;
            }

            var sourceId = cell.TryGetProperty("sourceId", out var sourceIdElement) &&
                sourceIdElement.ValueKind == JsonValueKind.Number &&
                sourceIdElement.TryGetInt32(out var sourceIdValue)
                    ? sourceIdValue
                    : -1;
            var atlas = cell.TryGetProperty("atlas", out var atlasElement) && TryReadVector2I(atlasElement, out var atlasValue)
                ? atlasValue
                : Vector2I.Zero;
            tileMapLayer.SetCell(coordsValue, sourceId, atlas);
        }
    }

    private static SpriteFrames ReadSpriteFrames(JsonElement spriteFramesElement)
    {
        var spriteFrames = new SpriteFrames();
        if (!spriteFramesElement.TryGetProperty("animations", out var animations) ||
            animations.ValueKind != JsonValueKind.Array)
        {
            return spriteFrames;
        }

        foreach (var animation in animations.EnumerateArray())
        {
            var name = animation.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!spriteFrames.HasAnimation(name))
            {
                spriteFrames.AddAnimation(name);
            }

            if (animation.TryGetProperty("speed", out var speed) &&
                speed.ValueKind == JsonValueKind.Number &&
                speed.TryGetSingle(out var speedValue))
            {
                spriteFrames.SetAnimationSpeed(name, speedValue);
            }

            if (!animation.TryGetProperty("frames", out var frames) || frames.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var frame in frames.EnumerateArray())
            {
                if (frame.ValueKind == JsonValueKind.String)
                {
                    spriteFrames.AddFrame(name, ImageTexture.LoadFromFile(frame.GetString() ?? string.Empty));
                }
            }
        }

        return spriteFrames;
    }

    private static AudioStream ReadAudioStream(JsonElement stream)
    {
        var source = stream.TryGetProperty("source", out var sourceElement) ? sourceElement.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new FormatException("Scene audio stream must declare a source resource path.");
        }

        ResourceFileSystem.ReadAllBytes(source);
        var length = stream.TryGetProperty("length", out var lengthElement) &&
            lengthElement.ValueKind == JsonValueKind.Number &&
            lengthElement.TryGetSingle(out var lengthValue)
                ? lengthValue
                : 0f;
        var sampleRate = stream.TryGetProperty("sampleRate", out var sampleRateElement) &&
            sampleRateElement.ValueKind == JsonValueKind.Number &&
            sampleRateElement.TryGetInt32(out var sampleRateValue)
                ? sampleRateValue
                : 44100;
        var channelCount = stream.TryGetProperty("channelCount", out var channelElement) &&
            channelElement.ValueKind == JsonValueKind.Number &&
            channelElement.TryGetInt32(out var channelValue)
                ? channelValue
                : 2;
        var bitsPerSample = stream.TryGetProperty("bitsPerSample", out var bitsElement) &&
            bitsElement.ValueKind == JsonValueKind.Number &&
            bitsElement.TryGetInt32(out var bitsValue)
                ? bitsValue
                : 16;
        var format = source.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
            ? AudioSourceFormat.Wav
            : AudioSourceFormat.OggVorbis;
        var metadata = new AudioImportMetadata(
            source,
            ResourceUid.CreateIdForPath(source),
            format,
            AudioImportMode.Static,
            sampleRate,
            channelCount,
            bitsPerSample,
            Math.Max(0, (long)(sampleRate * length)),
            length);
        return AudioImportResourceFactory.CreateAudioStream(metadata);
    }

    private static bool TryReadVector2(JsonElement element, out Vector2 value)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var items = element.EnumerateArray().ToArray();
            if (items.Length == 2 &&
                items[0].TryGetSingle(out var x) &&
                items[1].TryGetSingle(out var y))
            {
                value = new Vector2(x, y);
                return true;
            }
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("x", out var xElement) &&
            element.TryGetProperty("y", out var yElement) &&
            xElement.TryGetSingle(out var objectX) &&
            yElement.TryGetSingle(out var objectY))
        {
            value = new Vector2(objectX, objectY);
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryReadVector2I(JsonElement element, out Vector2I value)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var items = element.EnumerateArray().ToArray();
            if (items.Length == 2 &&
                items[0].TryGetInt32(out var x) &&
                items[1].TryGetInt32(out var y))
            {
                value = new Vector2I(x, y);
                return true;
            }
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("x", out var xElement) &&
            element.TryGetProperty("y", out var yElement) &&
            xElement.TryGetInt32(out var objectX) &&
            yElement.TryGetInt32(out var objectY))
        {
            value = new Vector2I(objectX, objectY);
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryReadColor(JsonElement element, out Color value)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var items = element.EnumerateArray().ToArray();
            if ((items.Length == 3 || items.Length == 4) &&
                items[0].TryGetSingle(out var red) &&
                items[1].TryGetSingle(out var green) &&
                items[2].TryGetSingle(out var blue))
            {
                var alpha = items.Length == 4 && items[3].TryGetSingle(out var alphaValue) ? alphaValue : 1f;
                value = new Color(red, green, blue, alpha);
                return true;
            }
        }

        value = default;
        return false;
    }

    private static int RunSceneTree(
        string projectRoot,
        Electron2DProjectSettings settings,
        Node mainNode,
        CliOptions options,
        TextWriter output,
        string? savePathOverride = null)
    {
        var tree = new SceneTree();
        tree.Root.AddChild(mainNode);

        var savePath = savePathOverride ?? ResolveSavePath(projectRoot, settings.Name);
        var fixedDelta = 1d / Math.Max(1, settings.PhysicsTicksPerSecond);
        var playableResult = TryRunPlayableScript(mainNode, options.GetOption("--play-script"), savePath, fixedDelta);
        var screenshotPath = options.GetOption("--screenshot");
        var boundedRun = playableResult is not null || !string.IsNullOrWhiteSpace(screenshotPath);
        var runResult = RuntimeHost.Run(
            tree,
            new RuntimeHostOptions
            {
                WindowTitle = settings.Name,
                WindowSize = settings.Display.WindowSize,
                FrameLimit = boundedRun ? 4 : 0,
                FixedDelta = fixedDelta,
                ScreenshotPath = screenshotPath,
                QuitOnEscape = false,
                ClearColor = Color.Black
            });

        var lines = BuildProjectRuntimeLines(settings, playableResult, runResult);
        if (options.Format == CliOutputFormat.Json)
        {
            output.WriteLine(BuildProjectRuntimeJson(options, projectRoot, settings, lines).ToJsonString(ProjectRuntimeJsonOptions));
        }
        else
        {
            foreach (var (key, value) in lines)
            {
                output.WriteLine($"{key}={value}");
            }
        }

        return runResult.Succeeded && (playableResult?.Succeeded ?? true) ? 0 : 1;
    }

    private static string ResolveScriptTypeName(string rootNamespace, string scriptPath)
    {
        var withoutExtension = Path.ChangeExtension(scriptPath.Replace('\\', '/'), null) ?? scriptPath;
        var segments = withoutExtension
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => segment.Replace('-', '_'))
            .ToArray();
        return string.Join('.', new[] { rootNamespace }.Concat(segments));
    }

    private static string ResolveSavePath(string projectRoot, string projectName)
    {
        var environmentVariable = GetSaveEnvironmentVariable(projectName);
        var configured = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return Path.Combine(projectRoot, ".electron2d", "user", GetSaveFileName(projectName));
    }

    private static string ResolveExportedSavePath(string projectName)
    {
        var environmentVariable = GetSaveEnvironmentVariable(projectName);
        var configured = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var root = string.IsNullOrWhiteSpace(localAppData) ? Path.GetTempPath() : localAppData;
        return Path.Combine(root, "Electron2D", "games", SanitizeFileName(projectName), GetSaveFileName(projectName));
    }

    private static string GetSaveEnvironmentVariable(string projectName)
    {
        return projectName.Contains("UiHeavy", StringComparison.OrdinalIgnoreCase)
            ? "ELECTRON2D_UI_HEAVY_REFERENCE_SAVE"
            : projectName.Contains("ReferencePlatformer", StringComparison.OrdinalIgnoreCase)
                ? "ELECTRON2D_REFERENCE_PLATFORMER_SAVE"
                : "ELECTRON2D_RUNTIME_SAVE";
    }

    private static string GetSaveFileName(string projectName)
    {
        return projectName.Contains("UiHeavy", StringComparison.OrdinalIgnoreCase)
            ? "ui-heavy-progress.json"
            : projectName.Contains("ReferencePlatformer", StringComparison.OrdinalIgnoreCase)
                ? "reference-platformer-progress.json"
                : "runtime-progress.json";
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(character => invalid.Contains(character) ? '_' : character).ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "Project" : sanitized;
    }

    private static ExportedPlayerManifest ReadExportedPlayerManifest(string manifestPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        var projectFile = root.TryGetProperty("projectFile", out var projectFileElement) ? projectFileElement.GetString() ?? string.Empty : string.Empty;
        var projectAssembly = root.TryGetProperty("projectAssembly", out var assemblyElement) ? assemblyElement.GetString() ?? string.Empty : string.Empty;
        var rootNamespace = root.TryGetProperty("rootNamespace", out var namespaceElement) ? namespaceElement.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(projectFile))
        {
            projectFile = ProjectFileLocator.LegacyProjectFileName;
        }

        if (string.IsNullOrWhiteSpace(projectAssembly) || string.IsNullOrWhiteSpace(rootNamespace))
        {
            throw new FormatException("Exported player manifest must contain projectAssembly and rootNamespace.");
        }

        return new ExportedPlayerManifest(ProjectDocumentPaths.NormalizeRelativePath(projectFile), projectAssembly, rootNamespace);
    }

    private static string ResolveExportedAssemblyPath(string baseDirectory, string projectAssembly)
    {
        var fullBaseDirectory = Path.GetFullPath(baseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        var assemblyPath = Path.GetFullPath(Path.Combine(baseDirectory, projectAssembly));
        if (!assemblyPath.StartsWith(fullBaseDirectory, StringComparison.OrdinalIgnoreCase) || !File.Exists(assemblyPath))
        {
            throw new CommandLineException($"Exported project assembly was not found: {projectAssembly}.");
        }

        return assemblyPath;
    }

    private static ProjectRuntimePlayableResult? TryRunPlayableScript(Node node, string? script, string savePath, double delta)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return null;
        }

        var method = node.GetType().GetMethod(
            "RunPlayableScript",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(IReadOnlyList<string>), typeof(string), typeof(double)],
            modifiers: null);
        object? result;
        var commands = script.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (method is not null)
        {
            result = method.Invoke(node, [commands, savePath, delta]);
        }
        else
        {
            method = node.GetType().GetMethod(
                "RunPlayableScript",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                [typeof(IReadOnlyList<string>), typeof(string)],
                modifiers: null);
            if (method is null)
            {
                throw new CommandLineException("Project script does not provide RunPlayableScript for Preview acceptance.");
            }

            result = method.Invoke(node, [commands, savePath]);
        }

        if (result is null)
        {
            throw new CommandLineException("Project script playable result was empty.");
        }

        var values = ExtractPlayableValues(result);
        var succeeded = values.TryGetValue("Playable", out var playable) &&
            string.Equals(playable, bool.TrueString, StringComparison.Ordinal);
        return new ProjectRuntimePlayableResult(succeeded, values);
    }

    private static SortedDictionary<string, string> ExtractPlayableValues(object result)
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Mode"] = "playable"
        };
        foreach (var property in result.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var name = property.Name == "CheckpointId" ? "Checkpoint" : property.Name;
            values[name] = FormatRuntimeValue(property.GetValue(result));
        }

        var formatPosition = result.GetType().GetMethod("FormatPlayerPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (formatPosition is not null)
        {
            values["PlayerPosition"] = FormatRuntimeValue(formatPosition.Invoke(result, []));
        }

        return values;
    }

    private static SortedDictionary<string, string> BuildProjectRuntimeLines(
        Electron2DProjectSettings settings,
        ProjectRuntimePlayableResult? playableResult,
        RuntimeHostResult runResult)
    {
        var lines = playableResult is null
            ? new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["Mode"] = "run",
                ["Playable"] = bool.FalseString
            }
            : new SortedDictionary<string, string>(playableResult.Values, StringComparer.Ordinal);

        lines["Project"] = settings.Name;
        lines["WindowCreated"] = runResult.WindowCreated.ToString(CultureInfo.InvariantCulture);
        lines["WindowShown"] = runResult.WindowShown.ToString(CultureInfo.InvariantCulture);
        lines["FramePresented"] = runResult.FramePresented.ToString(CultureInfo.InvariantCulture);
        lines["InputEventsDispatched"] = runResult.InputEventsDispatched.ToString(CultureInfo.InvariantCulture);
        lines["FrameCount"] = runResult.FrameCount.ToString(CultureInfo.InvariantCulture);
        lines["DrawCommands"] = runResult.DrawCommands.ToString(CultureInfo.InvariantCulture);
        lines["ScreenshotPath"] = runResult.ScreenshotPath ?? string.Empty;
        lines["ScreenshotSaved"] = runResult.ScreenshotSaved.ToString(CultureInfo.InvariantCulture);
        lines["RuntimeSucceeded"] = runResult.Succeeded.ToString(CultureInfo.InvariantCulture);
        return lines;
    }

    private static JsonObject BuildProjectRuntimeJson(
        CliOptions options,
        string projectRoot,
        Electron2DProjectSettings settings,
        SortedDictionary<string, string> lines)
    {
        return new JsonObject
        {
            ["schemaVersion"] = 1,
            ["command"] = "run",
            ["projectRoot"] = projectRoot,
            ["project"] = settings.Name,
            ["format"] = options.Format.ToString(),
            ["data"] = new JsonObject(lines.Select(pair => KeyValuePair.Create<string, JsonNode?>(pair.Key, pair.Value)).ToArray())
        };
    }

    private static string FormatRuntimeValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            bool boolean => boolean.ToString(CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static ProjectRuntimeProjectMetadata ReadProjectMetadata(string projectFile)
    {
        var document = XDocument.Load(projectFile);
        var propertyGroups = document.Root?.Elements("PropertyGroup") ?? [];
        var assemblyName = propertyGroups
            .Elements("AssemblyName")
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => value.Length > 0) ?? Path.GetFileNameWithoutExtension(projectFile);
        var rootNamespace = propertyGroups
            .Elements("RootNamespace")
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => value.Length > 0) ?? assemblyName;
        var targetFramework = propertyGroups
            .Elements("TargetFramework")
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => value.Length > 0) ??
            propertyGroups
                .Elements("TargetFrameworks")
                .Select(element => element.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty)
                .FirstOrDefault(value => value.Length > 0) ??
            "net10.0";
        return new ProjectRuntimeProjectMetadata(assemblyName, rootNamespace, targetFramework);
    }

    private static ProjectRuntimeProcessResult RunProcess(string workingDirectory, string fileName, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ??
            throw new CommandLineException($"Process could not start: {fileName}.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProjectRuntimeProcessResult(process.ExitCode, output, error);
    }

    private sealed record ExportedPlayerManifest(string ProjectFile, string ProjectAssembly, string RootNamespace);

    private sealed record ProjectRuntimeScene(
        string ScenePath,
        string Type,
        string Name,
        string ScriptPath,
        JsonElement RootProperties,
        IReadOnlyList<ProjectRuntimeSceneNode> Children);

    private sealed record ProjectRuntimeSceneNode(
        string Type,
        string Name,
        JsonElement Properties,
        IReadOnlyList<ProjectRuntimeSceneNode> Children);

    private sealed record ProjectRuntimeProjectMetadata(string AssemblyName, string RootNamespace, string TargetFramework);

    private sealed record ProjectRuntimeProcessResult(int ExitCode, string Output, string Error);

    private sealed record ProjectRuntimePlayableResult(bool Succeeded, SortedDictionary<string, string> Values);
}
