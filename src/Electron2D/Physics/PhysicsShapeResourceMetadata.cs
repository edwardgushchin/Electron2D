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

internal static class PhysicsShapeResourceMetadata
{
    public static void Register()
    {
        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create(
                typeof(RectangleShape2D).FullName!,
                () => new RectangleShape2D(),
                [
                    ResourceObjectPropertyMetadata.Create<RectangleShape2D, Vector2>(
                        "size",
                        resource => resource.Size,
                        (resource, value) => resource.Size = value)
                ]));

        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create(
                typeof(CircleShape2D).FullName!,
                () => new CircleShape2D(),
                [
                    ResourceObjectPropertyMetadata.Create<CircleShape2D, float>(
                        "radius",
                        resource => resource.Radius,
                        (resource, value) => resource.Radius = value)
                ]));

        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create(
                typeof(CapsuleShape2D).FullName!,
                () => new CapsuleShape2D(0.5f, 20f),
                [
                    ResourceObjectPropertyMetadata.Create<CapsuleShape2D, float>(
                        "height",
                        resource => resource.Height,
                        (resource, value) => resource.Height = value),
                    ResourceObjectPropertyMetadata.Create<CapsuleShape2D, float>(
                        "radius",
                        resource => resource.Radius,
                        (resource, value) => resource.Radius = value)
                ]));

        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create(
                typeof(SegmentShape2D).FullName!,
                () => new SegmentShape2D(),
                [
                    ResourceObjectPropertyMetadata.Create<SegmentShape2D, Vector2>(
                        "a",
                        resource => resource.A,
                        (resource, value) => resource.A = value),
                    ResourceObjectPropertyMetadata.Create<SegmentShape2D, Vector2>(
                        "b",
                        resource => resource.B,
                        (resource, value) => resource.B = value)
                ]));

        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create(
                typeof(ConvexPolygonShape2D).FullName!,
                () => new ConvexPolygonShape2D(),
                [
                    ResourceObjectPropertyMetadata.CreateArray<ConvexPolygonShape2D, Vector2>(
                        "points",
                        resource => resource.Points,
                        (resource, value) => resource.Points = value)
                ]));

        ResourceObjectMetadataRegistry.Register(
            ResourceObjectTypeMetadata.Create(
                typeof(ConcavePolygonShape2D).FullName!,
                () => new ConcavePolygonShape2D(),
                [
                    ResourceObjectPropertyMetadata.CreateArray<ConcavePolygonShape2D, Vector2>(
                        "segments",
                        resource => resource.Segments,
                        (resource, value) => resource.Segments = value)
                ]));
    }
}
