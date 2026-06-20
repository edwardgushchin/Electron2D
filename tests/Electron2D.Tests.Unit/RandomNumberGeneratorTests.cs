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
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class RandomNumberGeneratorTests
{
    [Fact]
    public void SeedReplaysDocumentedPcg32Sequence()
    {
        var rng = new Electron2D.RandomNumberGenerator
        {
            Seed = 42UL
        };

        var values = Enumerable.Range(0, 5)
            .Select(_ => rng.Randi())
            .ToArray();

        Assert.Equal(
            new uint[]
            {
                3270867926U,
                1795671209U,
                1924641435U,
                1143034755U,
                4121910957U
            },
            values);

        var replay = new Electron2D.RandomNumberGenerator
        {
            Seed = rng.Seed
        };

        Assert.Equal(values, Enumerable.Range(0, 5).Select(_ => replay.Randi()).ToArray());
    }

    [Fact]
    public void StateRestoresSequenceContinuation()
    {
        var rng = new Electron2D.RandomNumberGenerator
        {
            Seed = 20260621UL
        };

        var first = rng.Randi();
        var savedState = rng.State;
        var expectedContinuation = rng.Randi();

        _ = rng.Randi();
        _ = rng.Randi();
        rng.State = savedState;

        Assert.NotEqual(0U, first);
        Assert.NotEqual(0UL, savedState);
        Assert.Equal(expectedContinuation, rng.Randi());
    }

    [Fact]
    public void IntegerRangeIsInclusiveAndHandlesReversedBounds()
    {
        var rng = new Electron2D.RandomNumberGenerator
        {
            Seed = 7UL
        };

        Assert.Equal(5, rng.RandiRange(5, 5));

        for (var i = 0; i < 256; i++)
        {
            Assert.InRange(rng.RandiRange(-3, 3), -3, 3);
            Assert.InRange(rng.RandiRange(3, -3), -3, 3);
            Assert.InRange(rng.RandiRange(int.MinValue, int.MaxValue), int.MinValue, int.MaxValue);
        }
    }

    [Fact]
    public void FloatRangeAndNormalDistributionAreDeterministic()
    {
        var first = new Electron2D.RandomNumberGenerator
        {
            Seed = 9001UL
        };

        var second = new Electron2D.RandomNumberGenerator
        {
            Seed = 9001UL
        };

        var firstUnit = first.Randf();
        var secondUnit = second.Randf();
        Assert.Equal(firstUnit, secondUnit);
        Assert.InRange(firstUnit, 0f, 1f);

        var firstRange = first.RandfRange(10f, -2f);
        var secondRange = second.RandfRange(10f, -2f);
        Assert.Equal(firstRange, secondRange);
        Assert.InRange(firstRange, -2f, 10f);

        var firstNormal = first.Randfn(10f, 2f);
        var secondNormal = second.Randfn(10f, 2f);
        Assert.Equal(firstNormal, secondNormal);
        Assert.True(float.IsFinite(firstNormal));

        Assert.Equal(10f, first.Randfn(10f, 0f));
        Assert.Equal(1.25f, first.RandfRange(1.25f, 1.25f));
    }
}
