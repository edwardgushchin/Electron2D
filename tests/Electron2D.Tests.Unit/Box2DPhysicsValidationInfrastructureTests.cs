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

public sealed class Box2DPhysicsValidationInfrastructureTests
{
    [Fact]
    public void RepositoryContainsBox2DPhysicsCandidateValidationGate()
    {
        var root = FindRepositoryRoot();

        var projectPath = Path.Combine(
            root,
            "tests",
            "Electron2D.Tests.PhysicsBox2DSmoke",
            "Electron2D.Tests.PhysicsBox2DSmoke.csproj");
        var programPath = Path.Combine(root, "tests", "Electron2D.Tests.PhysicsBox2DSmoke", "Program.cs");
        var verifierPath = Path.Combine(root, "tools", "Verify-Box2DPhysicsCandidate.ps1");
        var specPath = Path.Combine(root, "docs", "specifications", "physics", "box2d-net-validation.md");
        var docPath = Path.Combine(root, "docs", "documentation", "physics", "box2d-net-validation.md");
        var workflowPath = Path.Combine(root, ".github", "workflows", "ci.yml");

        Assert.True(File.Exists(projectPath), $"Missing Box2D smoke project: {projectPath}");
        Assert.True(File.Exists(programPath), $"Missing Box2D smoke entry point: {programPath}");
        Assert.True(File.Exists(verifierPath), $"Missing Box2D validation verifier: {verifierPath}");
        Assert.True(File.Exists(specPath), $"Missing Box2D validation specification: {specPath}");
        Assert.True(File.Exists(docPath), $"Missing Box2D validation documentation: {docPath}");

        var project = File.ReadAllText(projectPath);
        Assert.Contains("PackageReference Include=\"Box2D.NET\" Version=\"3.1.654\"", project, StringComparison.Ordinal);
        Assert.Contains("<IsAotCompatible>true</IsAotCompatible>", project, StringComparison.Ordinal);
        Assert.DoesNotContain("<ProjectReference Include=\"..\\..\\src\\Electron2D\\Electron2D.csproj\"", project, StringComparison.OrdinalIgnoreCase);

        var runtimeProject = File.ReadAllText(Path.Combine(root, "src", "Electron2D", "Electron2D.csproj"));
        Assert.DoesNotContain("Box2D.NET", runtimeProject, StringComparison.OrdinalIgnoreCase);

        var program = File.ReadAllText(programPath);
        Assert.Contains("B2Worlds.b2World_Step", program, StringComparison.Ordinal);
        Assert.Contains("GC.GetAllocatedBytesForCurrentThread", program, StringComparison.Ordinal);
        Assert.Contains("AllocatedBytesPerTick", program, StringComparison.Ordinal);

        var verifier = File.ReadAllText(verifierPath);
        Assert.Contains("Box2D.NET", verifier, StringComparison.Ordinal);
        Assert.Contains("$NativeAot", verifier, StringComparison.Ordinal);
        Assert.Contains("PublishAot=true", verifier, StringComparison.Ordinal);
        Assert.Contains("AllocatedBytesPerTick", verifier, StringComparison.Ordinal);

        var combinedDocumentation = File.ReadAllText(specPath) + "\n" + File.ReadAllText(docPath);
        foreach (var required in new[]
        {
            "Windows x64",
            "Linux x64",
            "macOS",
            "Android arm64",
            "iOS arm64",
            "NativeAOT",
            "Release/AOT",
            "mobile",
            "gap",
            "allocations per tick",
            "Box2D.NET 3.1.654"
        })
        {
            Assert.Contains(required, combinedDocumentation, StringComparison.OrdinalIgnoreCase);
        }

        var workflow = File.ReadAllText(workflowPath);
        Assert.Contains("Verify-Box2DPhysicsCandidate.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("-NativeAot", workflow, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
