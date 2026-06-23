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

internal sealed class SceneTreeDock
{
    private readonly SceneUndoRedoStack undoRedo = new();
    private readonly Dictionary<int, Electron2D.TreeItem> treeItemsByNodeId = new();
    private Electron2D.SceneFileDocument document;

    public SceneTreeDock(Electron2D.SceneFileDocument document)
    {
        this.document = NormalizeAndValidate(document);
        Tree = new Electron2D.Tree
        {
            Name = "SceneTreeDock",
            HideRoot = false,
            SelectMode = Electron2D.Tree.SelectModeEnum.Row
        };
        SynchronizeTree();
    }

    public Electron2D.Tree Tree { get; }

    public bool CanUndo => undoRedo.CanUndo;

    public bool CanRedo => undoRedo.CanRedo;

    public IReadOnlyList<Electron2D.SceneFileNode> Nodes => document.Nodes;

    public static SceneTreeDock Load(string scenePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        return new SceneTreeDock(Electron2D.SceneFileTextSerializer.Deserialize(File.ReadAllText(scenePath)));
    }

    public void Save(string scenePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(scenePath)) ?? ".");
        File.WriteAllText(scenePath, Electron2D.SceneFileTextSerializer.Serialize(document));
    }

    public int AddNode(int parentId, string type, string name)
    {
        var newId = NextNodeId();
        ApplyMutation("Add Node", current =>
        {
            var parent = GetNode(current, parentId);
            var rootId = GetRoot(current).Id;
            var node = new Electron2D.SceneFileNode(
                newId,
                type,
                MakeUniqueChildName(current, parent.Id, name),
                parent.Id,
                rootId);

            return Rebuild(current, current.Nodes.Concat([node]));
        });

        return newId;
    }

    public void RenameNode(int nodeId, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        ApplyMutation("Rename Node", current =>
        {
            var node = GetNode(current, nodeId);
            var parentId = node.ParentId;
            var normalizedName = parentId is null ? newName.Trim() : MakeUniqueChildName(current, parentId.Value, newName, nodeId);
            return ReplaceNode(current, CloneNode(node, name: normalizedName));
        });
    }

    public int DuplicateNode(int nodeId)
    {
        var duplicateRootId = NextNodeId();
        ApplyMutation("Duplicate Node", current =>
        {
            var source = GetNode(current, nodeId);
            if (source.ParentId is null)
            {
                throw new InvalidOperationException("Scene root cannot be duplicated by the Scene Tree dock.");
            }

            var subtree = GetSubtree(current, source.Id).OrderBy(node => GetDepth(current, node.Id)).ToArray();
            var idMap = new Dictionary<int, int>();
            var nextId = duplicateRootId;
            foreach (var node in subtree)
            {
                idMap[node.Id] = nextId++;
            }

            var rootId = GetRoot(current).Id;
            var duplicateNodes = new List<Electron2D.SceneFileNode>();
            foreach (var node in subtree)
            {
                var newParentId = node.Id == source.Id
                    ? source.ParentId
                    : idMap[node.ParentId ?? throw new InvalidOperationException("Subtree node must have a parent.")];
                var newOwnerId = node.OwnerId is not null && idMap.TryGetValue(node.OwnerId.Value, out var mappedOwner)
                    ? mappedOwner
                    : rootId;
                var newName = node.Id == source.Id
                    ? MakeUniqueChildName(current, source.ParentId!.Value, node.Name + " Copy")
                    : node.Name + " Copy";
                duplicateNodes.Add(CloneNode(
                    node,
                    id: idMap[node.Id],
                    name: newName,
                    parentId: newParentId,
                    ownerId: newOwnerId));
            }

            return Rebuild(current, current.Nodes.Concat(duplicateNodes));
        });

        return duplicateRootId;
    }

    public void DeleteNode(int nodeId)
    {
        ApplyMutation("Delete Node", current =>
        {
            var node = GetNode(current, nodeId);
            if (node.ParentId is null)
            {
                throw new InvalidOperationException("Scene root cannot be deleted.");
            }

            var removed = GetSubtree(current, node.Id).Select(item => item.Id).ToHashSet();
            return Rebuild(current, current.Nodes.Where(item => !removed.Contains(item.Id)));
        });
    }

    public void DropNode(int nodeId, int targetNodeId, SceneTreeDropMode mode)
    {
        var target = GetNode(document, targetNodeId);
        var newParentId = mode switch
        {
            SceneTreeDropMode.Into => target.Id,
            SceneTreeDropMode.Before or SceneTreeDropMode.After => target.ParentId
                ?? throw new InvalidOperationException("Cannot drop a node before or after the scene root."),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported Scene Tree dock drop mode.")
        };

        ReparentNode(nodeId, newParentId);
    }

    public void ReparentNode(int nodeId, int newParentId)
    {
        ApplyMutation("Reparent Node", current =>
        {
            var node = GetNode(current, nodeId);
            var parent = GetNode(current, newParentId);
            if (node.ParentId is null)
            {
                throw new InvalidOperationException("Scene root cannot be reparented.");
            }

            if (node.Id == parent.Id || IsAncestor(current, node.Id, parent.Id))
            {
                throw new InvalidOperationException("Scene Tree dock cannot create parent cycles.");
            }

            return ReplaceNode(current, CloneNode(node, parentId: parent.Id));
        });
    }

    public void Undo()
    {
        SetDocument(undoRedo.Undo());
    }

    public void Redo()
    {
        SetDocument(undoRedo.Redo());
    }

    public bool ContainsNodeName(string name)
    {
        return document.Nodes.Any(node => node.Name.Equals(name, StringComparison.Ordinal));
    }

    public int CountInvalidOwnerReferences()
    {
        return CountInvalidOwnerReferences(document);
    }

    public IReadOnlyList<string> GetScenePaths()
    {
        var root = GetRoot(document);
        var paths = new List<string>();
        CollectScenePaths(root.Id, root.Name, paths);
        return paths;
    }

    private void ApplyMutation(string name, Func<Electron2D.SceneFileDocument, Electron2D.SceneFileDocument> transform)
    {
        var before = document;
        var after = NormalizeAndValidate(transform(before));
        undoRedo.Push(name, before, after);
        SetDocument(after);
    }

    private void SetDocument(Electron2D.SceneFileDocument nextDocument)
    {
        document = NormalizeAndValidate(nextDocument);
        SynchronizeTree();
    }

    private void SynchronizeTree()
    {
        Tree.Clear();
        treeItemsByNodeId.Clear();
        var root = GetRoot(document);
        AddTreeItem(root, parentItem: null);
    }

    private void AddTreeItem(Electron2D.SceneFileNode node, Electron2D.TreeItem? parentItem)
    {
        var item = Tree.CreateItem(parentItem);
        item.SetText(0, node.Name);
        treeItemsByNodeId[node.Id] = item;

        foreach (var child in document.Nodes.Where(child => child.ParentId == node.Id).OrderBy(child => child.Id))
        {
            AddTreeItem(child, item);
        }
    }

    private void CollectScenePaths(int nodeId, string currentPath, List<string> paths)
    {
        foreach (var child in document.Nodes.Where(node => node.ParentId == nodeId).OrderBy(node => node.Id))
        {
            var childPath = currentPath + "/" + child.Name;
            paths.Add(childPath);
            CollectScenePaths(child.Id, childPath, paths);
        }
    }

    private int NextNodeId()
    {
        return document.Nodes.Max(node => node.Id) + 1;
    }

    private static Electron2D.SceneFileDocument NormalizeAndValidate(Electron2D.SceneFileDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var root = GetRoot(document);
        var ids = document.Nodes.Select(node => node.Id).ToHashSet();
        foreach (var node in document.Nodes)
        {
            if (node.ParentId is not null && !ids.Contains(node.ParentId.Value))
            {
                throw new FormatException($"Scene node '{node.Name}' references missing parent id {node.ParentId.Value}.");
            }
        }

        var normalized = document.Nodes.Select(node =>
        {
            if (node.Id == root.Id)
            {
                return new Electron2D.SceneFileNode(
                    node.Id,
                    node.Type,
                    node.Name,
                    parentId: null,
                    ownerId: null,
                    node.PersistentGroups,
                    node.Properties);
            }

            return IsValidOwner(document, node.Id, node.OwnerId)
                ? node
                : CloneNode(node, ownerId: root.Id);
        });

        return new Electron2D.SceneFileDocument(document.ExternalReferences, document.InternalResources, normalized);
    }

    private static Electron2D.SceneFileDocument Rebuild(
        Electron2D.SceneFileDocument current,
        IEnumerable<Electron2D.SceneFileNode> nodes)
    {
        return NormalizeAndValidate(new Electron2D.SceneFileDocument(current.ExternalReferences, current.InternalResources, nodes));
    }

    private static Electron2D.SceneFileDocument ReplaceNode(
        Electron2D.SceneFileDocument current,
        Electron2D.SceneFileNode replacement)
    {
        return Rebuild(current, current.Nodes.Select(node => node.Id == replacement.Id ? replacement : node));
    }

    private static Electron2D.SceneFileNode CloneNode(
        Electron2D.SceneFileNode node,
        int? id = null,
        string? type = null,
        string? name = null,
        int? parentId = null,
        int? ownerId = null)
    {
        return new Electron2D.SceneFileNode(
            id ?? node.Id,
            type ?? node.Type,
            name ?? node.Name,
            parentId ?? node.ParentId,
            ownerId ?? node.OwnerId,
            node.PersistentGroups,
            node.Properties);
    }

    private static Electron2D.SceneFileNode GetRoot(Electron2D.SceneFileDocument document)
    {
        var roots = document.Nodes.Where(node => node.ParentId is null).ToArray();
        return roots.Length == 1
            ? roots[0]
            : throw new FormatException("Scene file must contain exactly one root node.");
    }

    private static Electron2D.SceneFileNode GetNode(Electron2D.SceneFileDocument document, int nodeId)
    {
        return document.Nodes.SingleOrDefault(node => node.Id == nodeId)
            ?? throw new InvalidOperationException($"Scene node id {nodeId} was not found.");
    }

    private static IReadOnlyList<Electron2D.SceneFileNode> GetSubtree(Electron2D.SceneFileDocument document, int rootNodeId)
    {
        var result = new List<Electron2D.SceneFileNode>();
        Collect(rootNodeId);
        return result;

        void Collect(int nodeId)
        {
            var node = GetNode(document, nodeId);
            result.Add(node);
            foreach (var child in document.Nodes.Where(item => item.ParentId == nodeId).OrderBy(item => item.Id))
            {
                Collect(child.Id);
            }
        }
    }

    private static int GetDepth(Electron2D.SceneFileDocument document, int nodeId)
    {
        var depth = 0;
        var current = GetNode(document, nodeId);
        while (current.ParentId is not null)
        {
            depth++;
            current = GetNode(document, current.ParentId.Value);
        }

        return depth;
    }

    private static bool IsAncestor(Electron2D.SceneFileDocument document, int ancestorId, int nodeId)
    {
        var current = GetNode(document, nodeId);
        while (current.ParentId is not null)
        {
            if (current.ParentId.Value == ancestorId)
            {
                return true;
            }

            current = GetNode(document, current.ParentId.Value);
        }

        return false;
    }

    private static bool IsValidOwner(Electron2D.SceneFileDocument document, int nodeId, int? ownerId)
    {
        return ownerId is not null &&
            document.Nodes.Any(node => node.Id == ownerId.Value) &&
            IsAncestor(document, ownerId.Value, nodeId);
    }

    private static int CountInvalidOwnerReferences(Electron2D.SceneFileDocument document)
    {
        return document.Nodes.Count(node => node.ParentId is not null && !IsValidOwner(document, node.Id, node.OwnerId));
    }

    private static string MakeUniqueChildName(
        Electron2D.SceneFileDocument document,
        int parentId,
        string name,
        int? ignoredNodeId = null)
    {
        var baseName = string.IsNullOrWhiteSpace(name) ? "Node" : name.Trim();
        var siblingNames = document.Nodes
            .Where(node => node.ParentId == parentId && node.Id != ignoredNodeId)
            .Select(node => node.Name)
            .ToHashSet(StringComparer.Ordinal);
        if (!siblingNames.Contains(baseName))
        {
            return baseName;
        }

        for (var index = 2; ; index++)
        {
            var candidate = baseName + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!siblingNames.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
