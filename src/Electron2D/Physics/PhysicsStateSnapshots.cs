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

internal readonly record struct PhysicsCollisionFilter(uint CollisionLayer, uint CollisionMask);

internal readonly record struct PhysicsMaterialState(float Friction, float Bounce, bool Rough, bool Absorbent)
{
    public static PhysicsMaterialState? From(PhysicsMaterial? material)
    {
        return material is null
            ? null
            : new PhysicsMaterialState(material.Friction, material.Bounce, material.Rough, material.Absorbent);
    }
}

internal readonly record struct PhysicsRigidBody2DState(float GravityScale, bool Sleeping, bool CanSleep);

internal readonly record struct PhysicsBody2DState(
    PhysicsMaterialState? MaterialOverride,
    PhysicsRigidBody2DState? RigidBody);
