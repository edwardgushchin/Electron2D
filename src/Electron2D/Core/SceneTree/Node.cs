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

/// <summary>
/// Represents the node type.
/// </summary>
///
/// <remarks>
/// This type is part of the Electron2D 0.1.0 Preview public API.
/// </remarks>
///
/// <threadsafety>
/// Instances of this type are not synchronized. Access them from the thread that owns the object unless the member documentation states otherwise.
/// </threadsafety>
///
/// <since>
/// This API is available since Electron2D 0.1.0 Preview.
/// </since>
///
public class Node : Object
{

    /// <summary>
    /// Initializes a new instance of the Node type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public Node()
    {
    }

    private readonly List<Node> _children = new();
    private readonly Dictionary<string, bool> _groups = new(StringComparer.Ordinal);
    private string _name = string.Empty;
    private Node? _parent;
    private Node? _owner;
    private SceneTree? _tree;
    private bool _readyCalled;

    /// <summary>
    /// Gets or sets the name value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current name value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public string Name
    {
        get
        {
            ThrowIfFreed();
            return _name;
        }
        set
        {
            ThrowIfFreed();
            _name = value ?? string.Empty;
            _parent?.MakeChildNameUnique(this);
        }
    }

    /// <summary>
    /// Gets or sets the owner value.
    /// </summary>
    ///
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current owner value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public Node? Owner
    {
        get
        {
            ThrowIfFreed();
            return _owner;
        }
        set
        {
            ThrowIfFreed();
            SetOwner(value);
        }
    }

    /// <summary>
    /// Adds the child value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="child">
    /// The child value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public void AddChild(Node child)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(child);
        child.ThrowIfFreed();

        if (ReferenceEquals(child, this) || child.IsAncestorOf(this))
        {
            throw new InvalidOperationException("A node cannot become a child of itself or its descendant.");
        }

        if (child._parent is not null || child._tree is not null)
        {
            throw new InvalidOperationException("Node already has a parent or is inside a SceneTree.");
        }

        _children.Add(child);
        child._parent = this;
        MakeChildNameUnique(child);

        if (_tree is not null)
        {
            _tree.AttachSubtree(child);
        }
    }

    /// <summary>
    /// Removes the child value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="child">
    /// The child value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public void RemoveChild(Node child)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(child);
        child.ThrowIfFreed();

        DetachChild(child, clearInvalidOwners: true);
    }

    /// <summary>
    /// Gets the parent value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current parent value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public Node? GetParent()
    {
        ThrowIfFreed();
        return _parent;
    }

    /// <summary>
    /// Gets the child value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="index">
    /// The index value.
    /// </param>
    ///
    /// <returns>
    /// The current child value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public Node? GetChild(int index)
    {
        ThrowIfFreed();
        var childIndex = NormalizeChildIndex(index);
        if (childIndex < 0 || childIndex >= _children.Count)
        {
            return null;
        }

        return _children[childIndex];
    }

    /// <summary>
    /// Gets the child count value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current child count value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public int GetChildCount()
    {
        ThrowIfFreed();
        return _children.Count;
    }

    /// <summary>
    /// Gets the index value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current index value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public int GetIndex()
    {
        ThrowIfFreed();
        return _parent is null ? -1 : _parent._children.IndexOf(this);
    }

    /// <summary>
    /// Checks whether ancestor of is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="node">
    /// The node value.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public bool IsAncestorOf(Node node)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(node);
        node.ThrowIfFreed();

        var current = node._parent;
        while (current is not null)
        {
            if (ReferenceEquals(current, this))
            {
                return true;
            }

            current = current._parent;
        }

        return false;
    }

    /// <summary>
    /// Executes the move child operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="childNode">
    /// The child node value.
    /// </param>
    ///
    /// <param name="toIndex">
    /// The to index value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public void MoveChild(Node childNode, int toIndex)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(childNode);
        childNode.ThrowIfFreed();

        var currentIndex = _children.IndexOf(childNode);
        if (currentIndex < 0)
        {
            throw new InvalidOperationException("Node is not a child of this node.");
        }

        var childIndex = NormalizeChildIndex(toIndex);
        if (childIndex < 0 || childIndex >= _children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(toIndex));
        }

        if (currentIndex == childIndex)
        {
            return;
        }

        _children.RemoveAt(currentIndex);
        _children.Insert(childIndex, childNode);
    }

    /// <summary>
    /// Executes the reparent operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="newParent">
    /// The new parent value.
    /// </param>
    ///
    /// <param name="keepGlobalTransform">
    /// The keep global transform value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public void Reparent(Node newParent, bool keepGlobalTransform = true)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(newParent);
        newParent.ThrowIfFreed();
        var globalTransform = keepGlobalTransform && this is Node2D node2D
            ? node2D.GlobalTransform
            : (Transform2D?)null;

        if (_parent is null)
        {
            throw new InvalidOperationException("Node must already have a parent before Reparent can be used.");
        }

        if (ReferenceEquals(newParent, this) || IsAncestorOf(newParent))
        {
            throw new InvalidOperationException("A node cannot be reparented to itself or its descendant.");
        }

        var oldParent = _parent;
        oldParent.DetachChild(this, clearInvalidOwners: false);
        newParent.AddChild(this);
        if (globalTransform is not null && this is Node2D reparentedNode2D)
        {
            reparentedNode2D.GlobalTransform = globalTransform.Value;
        }

        ClearInvalidOwnersRecursive();
    }

    /// <summary>
    /// Executes the queue free operation.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public void QueueFree()
    {
        ThrowIfFreed();

        if (_parent is null && _tree is not null)
        {
            throw new InvalidOperationException("The SceneTree root node cannot be queued for deletion.");
        }

        if (!MarkQueuedForDeletion())
        {
            return;
        }

        if (_tree is not null)
        {
            _tree.QueueDelete(this);
            return;
        }

        Free();
    }

    /// <summary>
    /// Adds the to group value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="group">
    /// The group value.
    /// </param>
    ///
    /// <param name="persistent">
    /// The persistent value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public void AddToGroup(string group, bool persistent = false)
    {
        ThrowIfFreed();
        var groupName = ValidateGroupName(group);

        if (_groups.TryGetValue(groupName, out var existingPersistent))
        {
            _groups[groupName] = existingPersistent || persistent;
            return;
        }

        _groups.Add(groupName, persistent);
    }

    /// <summary>
    /// Removes the from group value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="group">
    /// The group value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public void RemoveFromGroup(string group)
    {
        ThrowIfFreed();
        var groupName = ValidateGroupName(group);
        _groups.Remove(groupName);
    }

    /// <summary>
    /// Checks whether in group is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="group">
    /// The group value.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public bool IsInGroup(string group)
    {
        ThrowIfFreed();
        var groupName = ValidateGroupName(group);
        return _groups.ContainsKey(groupName);
    }

    /// <summary>
    /// Gets the groups value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current groups value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public string[] GetGroups()
    {
        ThrowIfFreed();
        return _groups.Keys.OrderBy(group => group, StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// Checks whether inside tree is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public bool IsInsideTree()
    {
        ThrowIfFreed();
        return _tree is not null;
    }

    /// <summary>
    /// Gets the tree value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current tree value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public SceneTree? GetTree()
    {
        ThrowIfFreed();
        return _tree;
    }

    /// <summary>
    /// Gets the nearest viewport that contains this node.
    /// </summary>
    ///
    /// <returns>
    /// The nearest ancestor <see cref="Viewport"/>, or the root viewport of the
    /// current <see cref="SceneTree"/> when this node is inside a tree without
    /// a nearer viewport; otherwise, <c>null</c>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Use this method from input callbacks when user code needs viewport-local
    /// services, such as <see cref="Viewport.SetInputAsHandled"/>.
    /// </para>
    /// <para>
    /// The 0.1.0 Preview scene tree creates a <see cref="Viewport"/> as its
    /// root node. Future subviewport nodes can make the nearest ancestor rule
    /// observable for nested viewports.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread that
    /// owns this node.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Viewport"/>
    /// <seealso cref="Viewport.SetInputAsHandled"/>
    public Viewport? GetViewport()
    {
        ThrowIfFreed();

        for (Node? current = this; current is not null; current = current._parent)
        {
            if (current is Viewport viewport)
            {
                return viewport;
            }
        }

        return _tree?.Root as Viewport;
    }

    /// <summary>
    /// Creates a tween bound to this node.
    /// </summary>
    ///
    /// <returns>
    /// A valid <see cref="Tween"/> registered in the current
    /// <see cref="SceneTree"/> and bound to this node.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The node must already be inside a scene tree. The returned tween is
    /// processed by that tree and pauses automatic advancement while this node
    /// is outside the tree or no longer valid.
    /// </para>
    /// <para>
    /// Use <see cref="SceneTree.CreateTween"/> when the tween should not be
    /// bound to a specific node lifetime.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="InvalidOperationException">
    /// Thrown when this node is not currently inside a <see cref="SceneTree"/>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread that
    /// owns this node.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SceneTree.CreateTween"/>
    /// <seealso cref="Tween.BindNode"/>
    public Tween CreateTween()
    {
        ThrowIfFreed();
        if (_tree is null)
        {
            throw new InvalidOperationException("Node must be inside a SceneTree before CreateTween can be used.");
        }

        return _tree.CreateTween().BindNode(this);
    }

    /// <summary>
    /// Gets the node value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="path">
    /// The path value.
    /// </param>
    ///
    /// <returns>
    /// The current node value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public Node GetNode(NodePath path)
    {
        var node = GetNodeOrNull(path);
        if (node is null)
        {
            throw new InvalidOperationException($"Node path '{path}' was not found.");
        }

        return node;
    }

    /// <summary>
    /// Gets the node or null value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="path">
    /// The path value.
    /// </param>
    ///
    /// <returns>
    /// The current node or null value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public Node? GetNodeOrNull(NodePath path)
    {
        ThrowIfFreed();
        if (path.IsEmpty())
        {
            return null;
        }

        var current = path.IsAbsolute() ? _tree?.Root : this;
        if (current is null)
        {
            return null;
        }

        var names = path.GetNodeNames();
        for (var index = 0; index < names.Length; index++)
        {
            var name = names[index];
            if (path.IsAbsolute() && index == 0 && string.Equals(name, current._name, StringComparison.Ordinal))
            {
                continue;
            }

            if (name == ".")
            {
                continue;
            }

            if (name == "..")
            {
                current = current._parent;
                if (current is null)
                {
                    return null;
                }

                continue;
            }

            current = current.GetDirectChildByName(name);
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Called when this node enters a scene tree.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public virtual void _EnterTree()
    {
    }

    /// <summary>
    /// Called when this node and its children are ready.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public virtual void _Ready()
    {
    }

    /// <summary>
    /// Called during an idle frame update.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="delta">
    /// The elapsed time in seconds.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public virtual void _Process(double delta)
    {
    }

    /// <summary>
    /// Called during a fixed physics update.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="delta">
    /// The elapsed time in seconds.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public virtual void _PhysicsProcess(double delta)
    {
    }

    /// <summary>
    /// Called when an input event is delivered to this node.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="inputEvent">
    /// The input event value.
    /// </param>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public virtual void _Input(InputEvent inputEvent)
    {
    }

    /// <summary>
    /// Called when this node exits a scene tree.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node" />
    ///
    public virtual void _ExitTree()
    {
    }

    internal void EnterTreeRecursive(SceneTree tree)
    {
        _tree = tree;
        if (this is ISceneTreeLifecycleHandler lifecycleHandler)
        {
            lifecycleHandler.OnEnterTree();
        }

        tree.InvokeUserCallback(this, nameof(_EnterTree), _EnterTree);

        foreach (var child in _children.ToArray())
        {
            child.EnterTreeRecursive(tree);
        }
    }

    internal void ReadyRecursive()
    {
        if (!IsInstanceValid(this) || _tree is null)
        {
            return;
        }

        foreach (var child in _children.ToArray())
        {
            child.ReadyRecursive();
        }

        if (_readyCalled)
        {
            return;
        }

        _readyCalled = true;
        _tree?.InvokeUserCallback(this, nameof(_Ready), _Ready);
    }

    internal void ProcessRecursive(double delta)
    {
        if (!IsInstanceValid(this) || _tree is null)
        {
            return;
        }

        _tree?.InvokeUserCallback(this, nameof(_Process), () => _Process(delta));
        if (IsInstanceValid(this) &&
            _tree is not null &&
            !IsQueuedForDeletion() &&
            this is ISceneTreeLifecycleHandler lifecycleHandler)
        {
            _tree.InvokeUserCallback(
                this,
                "process lifecycle handler",
                () => lifecycleHandler.OnProcess(delta));
        }

        foreach (var child in _children.ToArray())
        {
            child.ProcessRecursive(delta);
        }
    }

    internal void DrawRecursive()
    {
        if (!IsInstanceValid(this) || _tree is null)
        {
            return;
        }

        if (this is CanvasItem canvasItem)
        {
            canvasItem.DrawIfNeeded();
        }

        foreach (var child in _children.ToArray())
        {
            child.DrawRecursive();
        }
    }

    internal void PhysicsProcessRecursive(double delta)
    {
        if (!IsInstanceValid(this) || _tree is null || IsQueuedForDeletion())
        {
            return;
        }

        _tree?.InvokeUserCallback(this, nameof(_PhysicsProcess), () => _PhysicsProcess(delta));
        if (IsInstanceValid(this) &&
            _tree is not null &&
            !IsQueuedForDeletion() &&
            this is ISceneTreeLifecycleHandler lifecycleHandler)
        {
            lifecycleHandler.OnPhysicsProcess(delta);
        }

        foreach (var child in _children.ToArray())
        {
            child.PhysicsProcessRecursive(delta);
        }
    }

    internal bool InputRecursive(InputEvent inputEvent, Viewport viewport)
    {
        if (!IsInstanceValid(this) || _tree is null)
        {
            return viewport.IsInputHandled;
        }

        _tree?.InvokeUserCallback(this, nameof(_Input), () => _Input(inputEvent));
        if (viewport.IsInputHandled)
        {
            return true;
        }

        foreach (var child in _children.ToArray())
        {
            if (child.InputRecursive(inputEvent, viewport))
            {
                return true;
            }
        }

        return viewport.IsInputHandled;
    }

    internal void ExitTreeRecursive()
    {
        var tree = _tree;
        if (tree is null)
        {
            return;
        }

        foreach (var child in _children.ToArray())
        {
            child.ExitTreeRecursive();
        }

        tree?.InvokeUserCallback(this, nameof(_ExitTree), _ExitTree);
        if (IsInstanceValid(this) && this is ISceneTreeLifecycleHandler lifecycleHandler)
        {
            lifecycleHandler.OnExitTree();
        }

        _tree = null;
    }

    protected override void OnFree()
    {
        ExitTreeRecursive();

        if (_parent is not null)
        {
            _parent._children.Remove(this);
            _parent = null;
        }

        foreach (var child in _children.ToArray())
        {
            child.Free();
        }

        _children.Clear();
        _groups.Clear();
        _owner = null;
        ClearQueuedForDeletion();
        base.OnFree();
    }

    private void DetachChild(Node child, bool clearInvalidOwners)
    {
        if (!_children.Remove(child))
        {
            throw new InvalidOperationException("Node is not a child of this node.");
        }

        if (child._tree is not null)
        {
            child.ExitTreeRecursive();
        }

        child._parent = null;

        if (clearInvalidOwners)
        {
            child.ClearInvalidOwnersRecursive();
        }
    }

    private void SetOwner(Node? owner)
    {
        if (owner is not null)
        {
            owner.ThrowIfFreed();
            if (ReferenceEquals(owner, this) || !owner.IsAncestorOf(this))
            {
                throw new InvalidOperationException("Owner must be an ancestor of this node.");
            }
        }

        _owner = owner;
    }

    private void ClearInvalidOwnersRecursive()
    {
        if (_owner is not null && !_owner.IsAncestorOf(this))
        {
            _owner = null;
        }

        foreach (var child in _children)
        {
            child.ClearInvalidOwnersRecursive();
        }
    }

    private int NormalizeChildIndex(int index)
    {
        return index < 0 ? _children.Count + index : index;
    }

    private void MakeChildNameUnique(Node child)
    {
        var baseName = string.IsNullOrWhiteSpace(child._name) ? child.GetType().Name : child._name;
        var candidate = baseName;
        var suffix = 2;

        while (_children.Any(node => !ReferenceEquals(node, child) && node._name == candidate))
        {
            candidate = $"{baseName}{suffix}";
            suffix++;
        }

        child._name = candidate;
    }

    internal static string ValidateGroupName(string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        return group;
    }

    internal bool IsGroupPersistent(string group)
    {
        ThrowIfFreed();
        var groupName = ValidateGroupName(group);
        return _groups.TryGetValue(groupName, out var persistent) && persistent;
    }

    internal Node[] GetChildrenSnapshot()
    {
        ThrowIfFreed();
        return _children.ToArray();
    }

    internal void CollectNodesInGroup(string group, List<Node> nodes)
    {
        if (_groups.ContainsKey(group))
        {
            nodes.Add(this);
        }

        foreach (var child in _children)
        {
            child.CollectNodesInGroup(group, nodes);
        }
    }

    private Node? GetDirectChildByName(string name)
    {
        foreach (var child in _children)
        {
            if (string.Equals(child._name, name, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}
