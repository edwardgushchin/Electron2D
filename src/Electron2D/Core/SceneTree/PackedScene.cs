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
namespace Electron2D;

public class PackedScene : Resource
{
    private List<NodeSnapshot> _nodes = new();

    public Error Pack(Node path)
    {
        if (path is null)
        {
            return Error.InvalidParameter;
        }

        if (!Object.IsInstanceValid(path))
        {
            return Error.InvalidParameter;
        }

        var nodes = new List<NodeSnapshot>();
        var indices = new Dictionary<Node, int>();
        CaptureNode(path, parentIndex: null, ownerIndex: null, nodes, indices);
        _nodes = nodes;
        return Error.Ok;
    }

    public bool CanInstantiate()
    {
        return _nodes.Count > 0;
    }

    public Node? Instantiate()
    {
        if (!CanInstantiate())
        {
            return null;
        }

        var clones = new Node[_nodes.Count];
        for (var index = 0; index < _nodes.Count; index++)
        {
            var snapshot = _nodes[index];
            if (Activator.CreateInstance(snapshot.NodeType, nonPublic: true) is not Node node)
            {
                return null;
            }

            node.Name = snapshot.Name;
            foreach (var group in snapshot.PersistentGroups)
            {
                node.AddToGroup(group, persistent: true);
            }

            clones[index] = node;
        }

        for (var index = 0; index < _nodes.Count; index++)
        {
            var parentIndex = _nodes[index].ParentIndex;
            if (parentIndex is not null)
            {
                clones[parentIndex.Value].AddChild(clones[index]);
            }
        }

        for (var index = 0; index < _nodes.Count; index++)
        {
            var ownerIndex = _nodes[index].OwnerIndex;
            if (ownerIndex is not null)
            {
                clones[index].Owner = clones[ownerIndex.Value];
            }
        }

        return clones[0];
    }

    private static void CaptureNode(
        Node node,
        int? parentIndex,
        int? ownerIndex,
        List<NodeSnapshot> nodes,
        Dictionary<Node, int> indices)
    {
        var nodeIndex = nodes.Count;
        indices.Add(node, nodeIndex);
        nodes.Add(new NodeSnapshot(
            node.GetType(),
            node.Name,
            parentIndex,
            ownerIndex,
            GetPersistentGroups(node)));

        foreach (var child in node.GetChildrenSnapshot())
        {
            var owner = child.Owner;
            if (owner is null || !indices.TryGetValue(owner, out var childOwnerIndex))
            {
                continue;
            }

            CaptureNode(child, nodeIndex, childOwnerIndex, nodes, indices);
        }
    }

    private static string[] GetPersistentGroups(Node node)
    {
        return node
            .GetGroups()
            .Where(node.IsGroupPersistent)
            .ToArray();
    }

    private sealed record NodeSnapshot(
        Type NodeType,
        string Name,
        int? ParentIndex,
        int? OwnerIndex,
        string[] PersistentGroups);
}
