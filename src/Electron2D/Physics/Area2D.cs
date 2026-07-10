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
/// Provides an Electron2D 2D area node for overlap and influence queries.
/// </summary>
///
/// <remarks>
/// The 0.1-preview baseline creates and frees an area RID, stores area
/// properties and synchronizes transforms. Overlap signals and query results are
/// added by later physics tasks.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate nodes on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
public class Area2D : CollisionObject2D
{
    private const string BodyEnteredSignal = "body_entered";
    private const string BodyExitedSignal = "body_exited";
    private const string AreaEnteredSignal = "area_entered";
    private const string AreaExitedSignal = "area_exited";

    private readonly HashSet<Node2D> overlappingBodies = new();
    private readonly HashSet<Area2D> overlappingAreas = new();

    /// <summary>
    /// Creates an area and registers its Electron2D built-in overlap signals.
    /// </summary>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Area2D" />
    ///
    public Area2D()
    {
        AddUserSignal(BodyEnteredSignal);
        AddUserSignal(BodyExitedSignal);
        AddUserSignal(AreaEnteredSignal);
        AddUserSignal(AreaExitedSignal);
    }

    /// <summary>
    /// Gets or sets whether this area monitors overlapping bodies and areas.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current monitoring value.
    /// </value>
    ///
    /// <seealso cref="Area2D" />
    ///
    public bool Monitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets whether other areas can monitor this area.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current monitorable value.
    /// </value>
    ///
    /// <seealso cref="Area2D" />
    ///
    public bool Monitorable { get; set; } = true;

    /// <summary>
    /// Gets or sets the processing priority for this area.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current priority value.
    /// </value>
    ///
    /// <seealso cref="Area2D" />
    ///
    public int Priority { get; set; }

    /// <summary>
    /// Returns a snapshot of physics bodies currently overlapping this area.
    /// </summary>
    /// <returns>
    /// A new array containing valid overlapping body nodes ordered by instance id.
    /// </returns>
    ///
    /// <remarks>
    /// The list updates during <see cref="SceneTree" /> physics frames. Nodes
    /// that were freed after a deferred deletion are removed before the snapshot
    /// is returned.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="Area2D" />
    ///
    public Node2D[] GetOverlappingBodies()
    {
        ThrowIfFreed();
        PruneInvalidOverlaps();
        return overlappingBodies.OrderBy(static body => body.GetInstanceId()).ToArray();
    }

    /// <summary>
    /// Returns a snapshot of areas currently overlapping this area.
    /// </summary>
    /// <returns>
    /// A new array containing valid overlapping areas ordered by instance id.
    /// </returns>
    ///
    /// <remarks>
    /// Areas with <see cref="Monitorable" /> set to <c>false</c> are not
    /// included in another area's snapshot.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <seealso cref="Area2D" />
    ///
    public Area2D[] GetOverlappingAreas()
    {
        ThrowIfFreed();
        PruneInvalidOverlaps();
        return overlappingAreas.OrderBy(static area => area.GetInstanceId()).ToArray();
    }

    /// <summary>
    /// Checks whether this area currently overlaps at least one body.
    /// </summary>
    /// <returns><c>true</c> when at least one valid body overlaps this area; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Area2D" />
    ///
    public bool HasOverlappingBodies()
    {
        ThrowIfFreed();
        PruneInvalidOverlaps();
        return overlappingBodies.Count > 0;
    }

    /// <summary>
    /// Checks whether this area currently overlaps at least one other area.
    /// </summary>
    /// <returns><c>true</c> when at least one valid area overlaps this area; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Area2D" />
    ///
    public bool HasOverlappingAreas()
    {
        ThrowIfFreed();
        PruneInvalidOverlaps();
        return overlappingAreas.Count > 0;
    }

    /// <summary>
    /// Checks whether a specific body currently overlaps this area.
    /// </summary>
    /// <param name="body">The body node to check.</param>
    /// <returns><c>true</c> when <paramref name="body" /> is in the current body overlap list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="body" /> is <c>null</c>.</exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Area2D" />
    ///
    public bool OverlapsBody(Node2D body)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(body);
        PruneInvalidOverlaps();
        return overlappingBodies.Contains(body);
    }

    /// <summary>
    /// Checks whether a specific area currently overlaps this area.
    /// </summary>
    /// <param name="area">The area node to check.</param>
    /// <returns><c>true</c> when <paramref name="area" /> is in the current area overlap list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="area" /> is <c>null</c>.</exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Area2D" />
    ///
    public bool OverlapsArea(Area2D area)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(area);
        PruneInvalidOverlaps();
        return overlappingAreas.Contains(area);
    }

    protected override Rid CreatePhysicsRid()
    {
        return PhysicsServer2D.AreaCreate();
    }

    protected override void SynchronizePhysicsState(Rid rid)
    {
        base.SynchronizePhysicsState(rid);
        UpdateOverlaps();
    }

    protected override void OnFree()
    {
        overlappingBodies.Clear();
        overlappingAreas.Clear();
        base.OnFree();
    }

    private void UpdateOverlaps()
    {
        if (!Monitoring || !IsInsideTree())
        {
            overlappingBodies.Clear();
            overlappingAreas.Clear();
            return;
        }

        PruneInvalidOverlaps();
        var snapshot = Area2DOverlapDetector.Capture(this);
        EmitOverlapChanges(overlappingBodies, snapshot.Bodies, BodyEnteredSignal, BodyExitedSignal);
        EmitOverlapChanges(overlappingAreas, snapshot.Areas, AreaEnteredSignal, AreaExitedSignal);
    }

    private void PruneInvalidOverlaps()
    {
        overlappingBodies.RemoveWhere(static body => !IsValidOverlapNode(body));
        overlappingAreas.RemoveWhere(static area => !IsValidOverlapNode(area));
    }

    private void EmitOverlapChanges<TNode>(
        HashSet<TNode> previous,
        IReadOnlyCollection<TNode> current,
        string enteredSignal,
        string exitedSignal)
        where TNode : Node2D
    {
        var entered = current
            .Where(node => !previous.Contains(node))
            .OrderBy(static node => node.GetInstanceId())
            .ToArray();
        var exited = previous
            .Where(node => !current.Contains(node))
            .OrderBy(static node => node.GetInstanceId())
            .ToArray();

        previous.Clear();
        foreach (var node in current)
        {
            if (IsValidOverlapNode(node))
            {
                previous.Add(node);
            }
        }

        foreach (var node in entered)
        {
            if (IsValidOverlapNode(node))
            {
                EmitSignal(enteredSignal, node);
            }
        }

        foreach (var node in exited)
        {
            if (IsValidOverlapNode(node))
            {
                EmitSignal(exitedSignal, node);
            }
        }
    }

    private static bool IsValidOverlapNode(Node2D node)
    {
        return ElectronObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
    }
}
