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
namespace Electron2D.Editor.SceneTreeDock;

internal static class EditorSceneTreeDockSmoke
{
    public static EditorSceneTreeDockSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var projectRoot = Path.Combine(Path.GetFullPath(workRoot), "SceneTreeDockSmoke");
        var scenesRoot = Path.Combine(projectRoot, "scenes");
        var scenePath = Path.Combine(scenesRoot, "main.scene.json");
        Directory.CreateDirectory(scenesRoot);

        File.WriteAllText(scenePath, Electron2D.SceneFileTextSerializer.Serialize(CreateInitialScene()));

        var dock = EditorSceneTreeDock.Load(scenePath);
        var enemyId = dock.AddNode(1, "Electron2D.Node2D", "Enemy");
        dock.RenameNode(enemyId, "EnemySpawner");
        var duplicatePlayerId = dock.DuplicateNode(2);
        dock.DropNode(duplicatePlayerId, enemyId, EditorSceneTreeDropMode.Into);
        dock.DeleteNode(3);

        var undoAvailable = dock.CanUndo;
        dock.Undo();
        var undoRestored = dock.ContainsNodeName("Weapon");
        dock.Redo();
        var redoRemoved = !dock.ContainsNodeName("Weapon");

        dock.Save(scenePath);

        var reloaded = EditorSceneTreeDock.Load(scenePath);
        return new EditorSceneTreeDockSmokeResult(
            scenePath,
            reloaded.Nodes.Count,
            reloaded.CountInvalidOwnerReferences(),
            undoAvailable,
            undoRestored,
            redoRemoved,
            reloaded.Tree.GetRoot()?.GetText(0) ?? string.Empty,
            string.Join("|", reloaded.GetScenePaths()));
    }

    private static Electron2D.SceneFileDocument CreateInitialScene()
    {
        return new Electron2D.SceneFileDocument(
            externalReferences: null,
            internalResources: null,
            nodes:
            [
                new Electron2D.SceneFileNode(1, "Electron2D.Node2D", "Main", parentId: null, ownerId: null),
                new Electron2D.SceneFileNode(2, "Electron2D.Node2D", "Player", parentId: 1, ownerId: 1),
                new Electron2D.SceneFileNode(3, "Electron2D.Node2D", "Weapon", parentId: 2, ownerId: 1),
                new Electron2D.SceneFileNode(4, "Electron2D.CanvasLayer", "UI", parentId: 1, ownerId: 1)
            ]);
    }
}
