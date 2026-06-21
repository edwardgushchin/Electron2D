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
using System.Reflection;
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class CleanRuntimeBaselineTests
{
    [Fact]
    public void RuntimeAssemblyExportsOnlyCurrentPublicBaselineTypes()
    {
        var assembly = Assembly.Load("Electron2D");
        var publicTypeNames = assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "Electron2D.AnimatedSprite2D",
                "Electron2D.Animation",
                "Electron2D.Animation+InterpolationTypeEnum",
                "Electron2D.Animation+LoopModeEnum",
                "Electron2D.Animation+TrackTypeEnum",
                "Electron2D.AnimationLibrary",
                "Electron2D.AnimationPlayer",
                "Electron2D.Area2D",
                "Electron2D.AtlasTexture",
                "Electron2D.Callable",
                "Electron2D.Camera2D",
                "Electron2D.CanvasItem",
                "Electron2D.CanvasLayer",
                "Electron2D.CapsuleShape2D",
                "Electron2D.CharacterBody2D",
                "Electron2D.CharacterBody2D+MotionModeEnum",
                "Electron2D.CharacterBody2D+PlatformOnLeaveEnum",
                "Electron2D.CircleShape2D",
                "Electron2D.Collections.Array",
                "Electron2D.Collections.Dictionary",
                "Electron2D.CollisionObject2D",
                "Electron2D.CollisionShape2D",
                "Electron2D.Color",
                "Electron2D.ConcavePolygonShape2D",
                "Electron2D.ConnectFlags",
                "Electron2D.Control",
                "Electron2D.ConvexPolygonShape2D",
                "Electron2D.Error",
                "Electron2D.ExportAttribute",
                "Electron2D.Font",
                "Electron2D.HorizontalAlignment",
                "Electron2D.InputEvent",
                "Electron2D.InputEventFromWindow",
                "Electron2D.InputEventKey",
                "Electron2D.InputEventMouse",
                "Electron2D.InputEventMouseButton",
                "Electron2D.InputEventMouseMotion",
                "Electron2D.InputEventWithModifiers",
                "Electron2D.Key",
                "Electron2D.KeyLocation",
                "Electron2D.KinematicCollision2D",
                "Electron2D.Label",
                "Electron2D.Material",
                "Electron2D.Mathf",
                "Electron2D.MouseButton",
                "Electron2D.MouseButtonMask",
                "Electron2D.Node",
                "Electron2D.Node2D",
                "Electron2D.NodePath",
                "Electron2D.Object",
                "Electron2D.PackedScene",
                "Electron2D.PhysicsBody2D",
                "Electron2D.PhysicsDirectSpaceState2D",
                "Electron2D.PhysicsMaterial",
                "Electron2D.PhysicsPointQueryParameters2D",
                "Electron2D.PhysicsRayQueryParameters2D",
                "Electron2D.PhysicsServer2D",
                "Electron2D.PhysicsServer2D+ProcessInfo",
                "Electron2D.PhysicsServer2D+ShapeType",
                "Electron2D.PhysicsServer2D+SpaceParameter",
                "Electron2D.PhysicsShapeQueryParameters2D",
                "Electron2D.RandomNumberGenerator",
                "Electron2D.RayCast2D",
                "Electron2D.Rect2",
                "Electron2D.Rect2I",
                "Electron2D.RectangleShape2D",
                "Electron2D.RefCounted",
                "Electron2D.RenderingServer",
                "Electron2D.RenderingServer+RenderingFeature",
                "Electron2D.RenderingServer+RenderingProfile",
                "Electron2D.Resource",
                "Electron2D.ResourceUid",
                "Electron2D.Rid",
                "Electron2D.RigidBody2D",
                "Electron2D.RigidBody2D+CenterOfMassModeEnum",
                "Electron2D.RigidBody2D+FreezeModeEnum",
                "Electron2D.SceneTree",
                "Electron2D.SegmentShape2D",
                "Electron2D.Shader",
                "Electron2D.Shader+Mode",
                "Electron2D.ShaderMaterial",
                "Electron2D.Shape2D",
                "Electron2D.SignalAttribute",
                "Electron2D.Sprite2D",
                "Electron2D.SpriteFrames",
                "Electron2D.SpriteFrames+LoopModeEnum",
                "Electron2D.StaticBody2D",
                "Electron2D.StringName",
                "Electron2D.Texture2D",
                "Electron2D.ToolAttribute",
                "Electron2D.Transform2D",
                "Electron2D.Variant",
                "Electron2D.Variant+Type",
                "Electron2D.Vector2",
                "Electron2D.Vector2I",
                "Electron2D.VerticalAlignment",
                "Electron2D.Viewport",
                "Electron2D.ViewportTexture",
                "Electron2D.World2D"
            },
            publicTypeNames);

        Assert.Null(assembly.GetType("Electron2D.IComponent"));
        Assert.Null(assembly.GetType("Electron2D.SpriteRenderer"));
        Assert.Null(assembly.GetType("Electron2D.SpriteAnimator"));
        Assert.Null(assembly.GetType("Electron2D.Rigidbody"));
        Assert.Null(assembly.GetType("Electron2D.Collider"));
    }
}
