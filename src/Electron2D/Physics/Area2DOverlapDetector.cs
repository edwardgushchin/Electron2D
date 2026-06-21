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

internal static class Area2DOverlapDetector
{
    public static Area2DOverlapSnapshot Capture(Area2D area)
    {
        var root = area.GetTree()?.Root;
        if (root is null || !PhysicsQuery2D.TryGetObjectBounds(area, out var areaBounds))
        {
            return Area2DOverlapSnapshot.Empty;
        }

        var bodies = new List<Node2D>();
        var areas = new List<Area2D>();
        foreach (var candidate in PhysicsQuery2D.CollectCollisionObjects(root))
        {
            if (ReferenceEquals(candidate, area) ||
                !PhysicsQuery2D.CollisionMaskMatches(area.CollisionMask, candidate) ||
                !PhysicsQuery2D.TryGetObjectBounds(candidate, out var candidateBounds) ||
                !areaBounds.Intersects(candidateBounds, includeBorders: true))
            {
                continue;
            }

            if (candidate is Area2D candidateArea)
            {
                if (candidateArea.Monitorable)
                {
                    areas.Add(candidateArea);
                }

                continue;
            }

            if (candidate is PhysicsBody2D)
            {
                bodies.Add(candidate);
            }
        }

        return new Area2DOverlapSnapshot(
            bodies.OrderBy(static body => body.GetInstanceId()).ToArray(),
            areas.OrderBy(static candidateArea => candidateArea.GetInstanceId()).ToArray());
    }
}

internal readonly record struct Area2DOverlapSnapshot(Node2D[] Bodies, Area2D[] Areas)
{
    public static Area2DOverlapSnapshot Empty { get; } = new([], []);
}
