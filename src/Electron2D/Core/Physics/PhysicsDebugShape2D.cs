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

internal sealed class PhysicsDebugShape2D
{
    public static readonly Color DefaultColor = new(0.2f, 0.6f, 1f, 0.75f);

    private PhysicsDebugShape2D(
        CollisionObject2D owner,
        CollisionShape2D shapeNode,
        Shape2D shape,
        int shapeIndex,
        Rect2 bounds,
        Color color,
        bool disabled)
    {
        Owner = owner;
        ShapeNode = shapeNode;
        Shape = shape;
        ShapeIndex = shapeIndex;
        Bounds = bounds;
        Color = color;
        Disabled = disabled;
    }

    public CollisionObject2D Owner { get; }

    public CollisionShape2D ShapeNode { get; }

    public Shape2D Shape { get; }

    public int ShapeIndex { get; }

    public Rect2 Bounds { get; }

    public Color Color { get; }

    public bool Disabled { get; }

    public static PhysicsDebugShape2D[] Capture(Node root)
    {
        var shapes = new List<PhysicsDebugShape2D>();
        CaptureRecursive(root, shapes);
        return shapes.ToArray();
    }

    private static void CaptureRecursive(Node node, List<PhysicsDebugShape2D> shapes)
    {
        if (node is CollisionObject2D owner)
        {
            var shapeIndex = 0;
            CaptureOwnerShapes(owner, owner, shapes, ref shapeIndex);
        }

        foreach (var child in node.GetChildrenSnapshot())
        {
            CaptureRecursive(child, shapes);
        }
    }

    private static void CaptureOwnerShapes(
        CollisionObject2D owner,
        Node node,
        List<PhysicsDebugShape2D> shapes,
        ref int shapeIndex)
    {
        foreach (var child in node.GetChildrenSnapshot())
        {
            if (child is CollisionObject2D)
            {
                continue;
            }

            if (child is CollisionShape2D { Shape: not null } shapeNode &&
                PhysicsQuery2D.TryGetShapeBounds(shapeNode.Shape, shapeNode.GlobalTransform, out var bounds))
            {
                shapes.Add(new PhysicsDebugShape2D(
                    owner,
                    shapeNode,
                    shapeNode.Shape,
                    shapeIndex,
                    bounds,
                    ResolveDebugColor(shapeNode.DebugColor),
                    shapeNode.Disabled));
                shapeIndex++;
                continue;
            }

            CaptureOwnerShapes(owner, child, shapes, ref shapeIndex);
        }
    }

    private static Color ResolveDebugColor(Color color)
    {
        return color.A <= 0f ? DefaultColor : color;
    }
}
