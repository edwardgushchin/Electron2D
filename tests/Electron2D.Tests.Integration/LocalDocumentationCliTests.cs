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
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class LocalDocumentationCliTests
{
    [Fact]
    public void CliProjectIsPartOfSolutionAndNamedE2D()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Cli", "Electron2D.Cli.csproj");

        Assert.True(File.Exists(projectPath), "Electron2D.Cli project file must exist.");
        Assert.Contains("Electron2D.Cli", File.ReadAllText(Path.Combine(root, "src", "Electron2D.sln")));

        var project = XDocument.Load(projectPath);
        var assemblyName = project.Descendants("AssemblyName").SingleOrDefault()?.Value;
        var outputType = project.Descendants("OutputType").SingleOrDefault()?.Value;

        Assert.Equal("e2d", assemblyName);
        Assert.Equal("Exe", outputType);
    }

    [Fact]
    public async Task DocsTypeAndMemberCommandsReturnManifestBackedJson()
    {
        var type = await RunCliJsonAsync("docs", "type", "CharacterBody2D", "--format", "json");
        var typeEntry = type.RootElement.GetProperty("type");

        Assert.Equal("Electron2D.CharacterBody2D", typeEntry.GetProperty("fullName").GetString());
        Assert.Equal("electron2d://api/type/Electron2D.CharacterBody2D", typeEntry.GetProperty("id").GetString());
        Assert.Equal("data/api/electron2d-api-manifest.json", type.RootElement.GetProperty("sourcePath").GetString());
        Assert.True(typeEntry.GetProperty("members").GetArrayLength() > 0);

        var member = await RunCliJsonAsync("docs", "member", "CharacterBody2D.MoveAndSlide", "--format", "json");
        var memberEntry = member.RootElement.GetProperty("member");

        Assert.Equal("Electron2D.CharacterBody2D", memberEntry.GetProperty("declaringType").GetString());
        Assert.Equal("MoveAndSlide", memberEntry.GetProperty("name").GetString());
        Assert.Equal("Method", memberEntry.GetProperty("kind").GetString());
        Assert.Contains("MoveAndSlide", memberEntry.GetProperty("signature").GetString(), StringComparison.Ordinal);
        Assert.Equal("data/api/electron2d-api-manifest.json", member.RootElement.GetProperty("sourcePath").GetString());
    }

    [Fact]
    public async Task DocsSearchAndExampleCommandsUseLocalDocumentationIndex()
    {
        var search = await RunCliJsonAsync("docs", "search", "move and slide", "--format", "json");
        var results = search.RootElement.GetProperty("results").EnumerateArray().ToArray();

        Assert.Contains(results, result =>
            result.GetProperty("kind").GetString() == "api-member" &&
            result.GetProperty("title").GetString() == "CharacterBody2D.MoveAndSlide" &&
            result.GetProperty("sourcePath").GetString() == "data/api/electron2d-api-manifest.json");

        var example = await RunCliJsonAsync("docs", "example", "platformer movement", "--format", "json");
        var exampleEntry = example.RootElement.GetProperty("example");
        var apiIds = exampleEntry.GetProperty("apiIds").EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Equal("example:platformer-movement", exampleEntry.GetProperty("id").GetString());
        Assert.Contains("CharacterBody2D", exampleEntry.GetProperty("code").GetString(), StringComparison.Ordinal);
        Assert.Contains("electron2d://api/type/Electron2D.CharacterBody2D", apiIds);
        Assert.Equal("data/documentation/electron2d-doc-examples.json", exampleEntry.GetProperty("sourcePath").GetString());
    }

    private static async Task<JsonDocument> RunCliJsonAsync(params string[] arguments)
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Cli", "Electron2D.Cli.csproj");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"e2d command failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

        return JsonDocument.Parse(output);
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
