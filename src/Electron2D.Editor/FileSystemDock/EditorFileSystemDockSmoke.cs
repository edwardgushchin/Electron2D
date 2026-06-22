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
namespace Electron2D.Editor.FileSystemDock;

internal static class EditorFileSystemDockSmoke
{
    private const long TextureUid = 123456789L;

    public static EditorFileSystemDockSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var projectRoot = Path.GetFullPath(workRoot);
        Directory.CreateDirectory(projectRoot);

        var liveImportStates = new Dictionary<string, string>(StringComparer.Ordinal);
        var dock = new EditorFileSystemDock(
            projectRoot,
            relativePath => liveImportStates.TryGetValue(relativePath, out var state) ? state : null);
        dock.CreateFolder(string.Empty, "assets");
        dock.CreateFolder(string.Empty, "scenes");
        WriteResource(projectRoot, "assets/player.e2res", TextureUid, "Electron2D.Texture2D");
        var scenePath = WriteScene(projectRoot, "scenes/main.scene.json");

        var initialItemCount = dock.Browse().Count;
        dock.CreateFolder("assets", "characters");
        var folderCreated = Directory.Exists(Path.Combine(projectRoot, "assets", "characters"));

        dock.Reimport();
        var uidBefore = dock.GetResourceReference("res://assets/player.e2res").UidText;
        var renamedResourcePath = dock.Rename("assets/player.e2res", "player_texture.e2res");
        var movedResourcePath = dock.Move("assets/player_texture.e2res", "assets/characters");
        var movedFilePath = Path.Combine(projectRoot, "assets", "characters", "player_texture.e2res");
        var movedFileExists = File.Exists(movedFilePath);

        dock.Reimport();
        var movedReference = dock.GetResourceReference(movedResourcePath);
        var uidAfter = movedReference.UidText;
        var draggedNodeId = dock.DragResourceIntoScene(scenePath, movedResourcePath, parentNodeId: 1);
        var searchResults = string.Join(
            '|',
            dock.Search("player_texture")
                .Where(item => item.Kind == EditorFileSystemItemKind.File)
                .Select(item => item.ResourcePath));

        File.WriteAllText(Path.Combine(projectRoot, "assets", "broken.e2res"), "{ this is not valid json");
        dock.Reimport();
        var errors = dock.GetImportErrors();
        var importError = errors.SingleOrDefault(error => error.ResourcePath == "res://assets/broken.e2res");
        File.WriteAllBytes(Path.Combine(projectRoot, "assets", "pending.png"), [137, 80, 78, 71, 13, 10, 26, 10]);
        liveImportStates["assets/pending.png"] = "Importing";
        var liveImportStatusVisible = dock.Browse()
            .Single(item => item.RelativePath == "assets/pending.png")
            .ImportStatus == "Importing";

        var sceneText = File.ReadAllText(scenePath);
        var scene = Electron2D.SceneFileTextSerializer.Deserialize(sceneText);
        var roundTripStable = string.Equals(
            sceneText,
            Electron2D.SceneFileTextSerializer.Serialize(scene),
            StringComparison.Ordinal);
        var externalReference = scene.ExternalReferences.Single(reference => reference.Uid == movedReference.Uid);
        var draggedNode = scene.Nodes.Single(node => node.Id == draggedNodeId);

        return new EditorFileSystemDockSmokeResult(
            scenePath,
            initialItemCount,
            folderCreated,
            movedFileExists,
            renamedResourcePath,
            movedResourcePath,
            uidBefore,
            uidAfter,
            string.Equals(uidBefore, uidAfter, StringComparison.Ordinal),
            externalReference.Path,
            externalReference.UidText,
            draggedNode.Type,
            searchResults,
            errors.Count,
            importError?.ResourcePath ?? string.Empty,
            importError is not null && !string.IsNullOrWhiteSpace(importError.Message),
            liveImportStatusVisible,
            roundTripStable);
    }

    private static string WriteScene(string projectRoot, string relativePath)
    {
        var scenePath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath)!);
        var scene = new Electron2D.SceneFileDocument(
            externalReferences: null,
            internalResources: null,
            nodes:
            [
                new Electron2D.SceneFileNode(1, "Electron2D.Node2D", "Main", parentId: null, ownerId: null)
            ]);
        File.WriteAllText(scenePath, Electron2D.SceneFileTextSerializer.Serialize(scene));
        return scenePath;
    }

    private static void WriteResource(string projectRoot, string relativePath, long uid, string type)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var document = new Electron2D.ResourceFileDocument(
            uid,
            type,
            "res://" + relativePath.Replace('\\', '/'),
            externalReferences: null,
            internalResources: null,
            properties: new Dictionary<string, Electron2D.Variant>(StringComparer.Ordinal)
            {
                ["resource_name"] = Path.GetFileNameWithoutExtension(relativePath)
            });
        File.WriteAllText(path, Electron2D.ResourceFileTextSerializer.Serialize(document));
    }
}
