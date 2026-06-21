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
using System.Globalization;
using Box2D.NET;

var warmupTicks = ReadIntArg(args, "--warmup", 60);
var measuredTicks = ReadIntArg(args, "--ticks", 240);
if (warmupTicks < 0)
{
    throw new ArgumentOutOfRangeException(nameof(warmupTicks), warmupTicks, "Warmup tick count must not be negative.");
}

if (measuredTicks <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(measuredTicks), measuredTicks, "Measured tick count must be greater than zero.");
}

var worldDef = B2Types.b2DefaultWorldDef();
worldDef.gravity = new B2Vec2(0f, -9.80665f);

var worldId = B2Worlds.b2CreateWorld(in worldDef);
try
{
    var groundBodyDef = B2Types.b2DefaultBodyDef();
    groundBodyDef.position = new B2Vec2(0f, -1f);
    var groundBodyId = B2Bodies.b2CreateBody(worldId, in groundBodyDef);

    var groundShapeDef = B2Types.b2DefaultShapeDef();
    var groundBox = B2Geometries.b2MakeBox(20f, 0.5f);
    B2Shapes.b2CreatePolygonShape(groundBodyId, in groundShapeDef, in groundBox);

    var dynamicBodyDef = B2Types.b2DefaultBodyDef();
    dynamicBodyDef.type = B2BodyType.b2_dynamicBody;
    dynamicBodyDef.position = new B2Vec2(0f, 4f);
    dynamicBodyDef.enableSleep = true;
    var dynamicBodyId = B2Bodies.b2CreateBody(worldId, in dynamicBodyDef);

    var circleShapeDef = B2Types.b2DefaultShapeDef();
    circleShapeDef.density = 1f;
    circleShapeDef.material.friction = 0.4f;
    var circle = new B2Circle(new B2Vec2(0f, 0f), 0.5f);
    B2Shapes.b2CreateCircleShape(dynamicBodyId, in circleShapeDef, in circle);

    var initialPosition = B2Bodies.b2Body_GetPosition(dynamicBodyId);
    StepWorld(worldId, warmupTicks);

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    StepWorld(worldId, measuredTicks);
    var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    var allocatedBytesPerTick = allocatedBytes / (double)measuredTicks;
    var finalPosition = B2Bodies.b2Body_GetPosition(dynamicBodyId);

    if (finalPosition.Y >= initialPosition.Y)
    {
        throw new InvalidOperationException(
            $"Dynamic body did not move down under gravity. InitialY={initialPosition.Y}, FinalY={finalPosition.Y}");
    }

    Console.WriteLine("Box2D.NET physics candidate smoke passed.");
    Console.WriteLine("PackageVersion=3.1.654");
    Console.WriteLine($"WarmupTicks={warmupTicks.ToString(CultureInfo.InvariantCulture)}");
    Console.WriteLine($"Ticks={measuredTicks.ToString(CultureInfo.InvariantCulture)}");
    Console.WriteLine($"AllocatedBytes={allocatedBytes.ToString(CultureInfo.InvariantCulture)}");
    Console.WriteLine($"AllocatedBytesPerTick={allocatedBytesPerTick.ToString("0.###", CultureInfo.InvariantCulture)}");
    Console.WriteLine($"InitialY={initialPosition.Y.ToString("0.###", CultureInfo.InvariantCulture)}");
    Console.WriteLine($"FinalY={finalPosition.Y.ToString("0.###", CultureInfo.InvariantCulture)}");
}
finally
{
    B2Worlds.b2DestroyWorld(worldId);
}

static void StepWorld(B2WorldId worldId, int ticks)
{
    const float fixedDelta = 1f / 60f;
    const int subStepCount = 4;

    for (var tick = 0; tick < ticks; tick++)
    {
        B2Worlds.b2World_Step(worldId, fixedDelta, subStepCount);
    }
}

static int ReadIntArg(string[] args, string name, int fallback)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
    }

    return fallback;
}
