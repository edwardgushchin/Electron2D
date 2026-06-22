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
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ApiManifestTests
{
    [Fact]
    public void ApiManifestDescribesCompiledPublicRuntimeSurface()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "data", "api", "electron2d-api-manifest.json");

        Assert.True(File.Exists(manifestPath), $"API manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var rootElement = document.RootElement;

        Assert.Equal(1, rootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("0.1.0-preview", rootElement.GetProperty("manifestVersion").GetString());
        Assert.Equal("Electron2D 0.1.0 2D", rootElement.GetProperty("profileName").GetString());
        Assert.Equal("4.7-stable", rootElement.GetProperty("godotBaseline").GetString());

        var generatedFrom = rootElement.GetProperty("generatedFrom");
        Assert.EndsWith("src/Electron2D/bin/Debug/net10.0/Electron2D.dll", generatedFrom.GetProperty("compiledAssembly").GetString(), StringComparison.Ordinal);
        Assert.EndsWith(".github/wiki/API-Compatibility.md", generatedFrom.GetProperty("compatibilityPage").GetString(), StringComparison.Ordinal);
        Assert.EndsWith("Electron2D.xml", generatedFrom.GetProperty("xmlDocumentation").GetString(), StringComparison.Ordinal);

        var manifestTypes = rootElement.GetProperty("types")
            .EnumerateArray()
            .Select(type => type.GetProperty("fullName").GetString())
            .ToArray();
        var runtimeTypes = typeof(Electron2D.Object).Assembly.GetExportedTypes()
            .Where(type => type.Assembly == typeof(Electron2D.Object).Assembly)
            .Select(DisplayName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(runtimeTypes, manifestTypes);
    }

    [Fact]
    public void ApiManifestCarriesStableIdentifiersAndProfileStatus()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "data", "api", "electron2d-api-manifest.json");

        Assert.True(File.Exists(manifestPath), $"API manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var rootElement = document.RootElement;

        var summary = rootElement.GetProperty("strictParitySummary");
        Assert.Equal(0, summary.GetProperty("missingTypes").GetInt32());
        Assert.Equal(0, summary.GetProperty("missingMembers").GetInt32());
        Assert.Equal(0, summary.GetProperty("signatureMismatches").GetInt32());
        Assert.Equal(0, summary.GetProperty("inheritanceMismatches").GetInt32());
        Assert.Equal(0, summary.GetProperty("defaultMismatches").GetInt32());
        Assert.Equal(0, summary.GetProperty("unexpectedChanges").GetInt32());

        var node = FindType(rootElement, "Electron2D.Node");
        Assert.Equal("electron2d://api/type/Electron2D.Node", node.GetProperty("id").GetString());
        Assert.Equal("Electron2D.Object", node.GetProperty("baseType").GetString());
        Assert.Contains(node.GetProperty("members").EnumerateArray(), member =>
            member.GetProperty("kind").GetString() == "Property" &&
            member.GetProperty("name").GetString() == "Name" &&
            member.GetProperty("id").GetString()!.StartsWith("electron2d://api/member/Electron2D.Node/Property/", StringComparison.Ordinal));
        Assert.Contains(node.GetProperty("members").EnumerateArray(), member =>
            member.GetProperty("kind").GetString() == "Method" &&
            member.GetProperty("name").GetString() == "AddChild" &&
            member.GetProperty("id").GetString()!.StartsWith("electron2d://api/member/Electron2D.Node/Method/", StringComparison.Ordinal));

        var control = FindType(rootElement, "Electron2D.Control");
        var profile = control.GetProperty("profile");
        Assert.Equal("supported", profile.GetProperty("status").GetString());
        Assert.Equal("parity_verified", profile.GetProperty("parity").GetString());
        Assert.False(profile.GetProperty("outOfProfile").GetBoolean());
    }

    private static JsonElement FindType(JsonElement rootElement, string fullName)
    {
        foreach (var type in rootElement.GetProperty("types").EnumerateArray())
        {
            if (type.GetProperty("fullName").GetString() == fullName)
            {
                return type;
            }
        }

        throw new InvalidOperationException($"API manifest does not contain type {fullName}.");
    }

    private static string DisplayName(Type type) => (type.FullName ?? type.Name).Replace('+', '.');

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
