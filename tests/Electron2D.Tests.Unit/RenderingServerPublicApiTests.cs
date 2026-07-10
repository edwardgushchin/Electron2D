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

public sealed class RenderingServerPublicApiTests
{
    [Fact]
    public void RenderingServerIsPublicApiSurface()
    {
        var assembly = typeof(Electron2D.Node).Assembly;
        var renderingServer = assembly.GetType("Electron2D.RenderingServer", throwOnError: true)!;
        var exportedTypeNames = assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .ToArray();

        Assert.True(renderingServer.IsPublic);
        Assert.Contains("Electron2D.RenderingServer", exportedTypeNames);
        Assert.Contains("Electron2D.RenderingServer+RenderingFeature", exportedTypeNames);
        Assert.Contains("Electron2D.RenderingServer+RenderingProfile", exportedTypeNames);
    }

    [Fact]
    public void RenderingServerDoesNotExportConcreteBackendTypes()
    {
        var assembly = typeof(Electron2D.Node).Assembly;
        var exportedTypeNames = assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .ToArray();

        Assert.DoesNotContain("Electron2D.IRenderingBackend", exportedTypeNames);
        Assert.DoesNotContain("Electron2D.StandardRenderingBackend", exportedTypeNames);
        Assert.DoesNotContain("Electron2D.CompatibilityRenderingBackend", exportedTypeNames);
    }

    [Fact]
    public void PublicNodeSurfaceDoesNotExposeConcreteRenderingBackends()
    {
        var backendNames = new[]
        {
            "IRenderingBackend",
            "StandardRenderingBackend",
            "CompatibilityRenderingBackend"
        };

        var nodePublicMembers = typeof(Electron2D.Node)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Where(member => member.DeclaringType == typeof(Electron2D.Node));

        foreach (var member in nodePublicMembers)
        {
            Assert.DoesNotContain(backendNames, backendName => member.ToString()?.Contains(backendName, StringComparison.Ordinal) == true);
        }
    }
}
