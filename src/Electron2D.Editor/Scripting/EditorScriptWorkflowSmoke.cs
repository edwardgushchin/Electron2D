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
using System.Xml.Linq;

using Electron2D.Editor.ProjectManagement;

namespace Electron2D.Editor.Scripting;

internal static class EditorScriptWorkflowSmoke
{
    private const string ProjectName = "ScriptWorkflowSmoke";
    private const string ExpectedMessage = "edited from embedded editor";

    public static EditorScriptWorkflowSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var repoRoot = FindRepositoryRoot();
        var templateRoot = Path.Combine(repoRoot, "templates", "electron2d-empty");
        var manager = new EditorProjectManager(templateRoot);
        var creation = manager.CreateProject(new EditorProjectCreateOptions(
            ProjectName,
            Path.GetFullPath(workRoot),
            Electron2D.Electron2DRendererProfileSetting.Compatibility));
        var projectFile = Path.Combine(creation.ProjectPath, ProjectName + ".csproj");
        RewriteProjectToUseRuntimeProjectReference(projectFile, Path.Combine(repoRoot, "src", "Electron2D", "Electron2D.csproj"));
        WriteScene(creation.MainScenePath);

        var workspace = new EditorScriptWorkspace(creation.ProjectPath);
        var script = workspace.CreateScript("PlayerController");
        var document = workspace.OpenScript(script.RelativePath);
        var openedScript = document.Text.Contains("class PlayerController", StringComparison.Ordinal);

        document.ReplaceText(CreateInvalidScript(script.NamespaceName, script.ClassName));
        var dirtyBeforeSave = document.IsDirty;
        document.Save();
        var dirtyAfterSave = document.IsDirty;

        workspace.AttachScriptToNode(creation.MainScenePath, nodeId: 2, script.FullTypeName);
        WriteProgram(Path.Combine(creation.ProjectPath, "Program.cs"), script.NamespaceName, script.ClassName);

        var runner = new EditorProjectBuildRunner();
        var failedBuild = runner.Build(projectFile);
        var compilerErrors = failedBuild.Diagnostics
            .Where(diagnostic => diagnostic.Severity == EditorProjectDiagnosticSeverity.Error)
            .ToArray();
        var firstError = compilerErrors.FirstOrDefault();

        document.ReplaceText(CreateValidScript(script.NamespaceName, script.ClassName));
        document.Save();
        var fixedBuild = runner.Build(projectFile);
        var run = fixedBuild.Succeeded
            ? runner.RunAfterBuild(projectFile)
            : new EditorProjectRunResult(-1, string.Empty);

        var sceneText = File.ReadAllText(creation.MainScenePath);
        var scene = Electron2D.SceneFileTextSerializer.Deserialize(sceneText);
        var sceneRoundTripStable = string.Equals(
            sceneText,
            Electron2D.SceneFileTextSerializer.Serialize(scene),
            StringComparison.Ordinal);
        var attachedNodeType = scene.Nodes.Single(node => node.Id == 2).Type;

        return new EditorScriptWorkflowSmokeResult(
            creation.ProjectPath,
            creation.MainScenePath,
            script.ScriptPath,
            File.Exists(script.ScriptPath),
            openedScript,
            dirtyBeforeSave,
            dirtyAfterSave,
            attachedNodeType,
            compilerErrors.Length,
            firstError?.Code ?? string.Empty,
            firstError?.Line ?? 0,
            firstError?.Column ?? 0,
            fixedBuild.Succeeded,
            run.ExitCode,
            run.Output.Contains(ExpectedMessage, StringComparison.Ordinal),
            sceneRoundTripStable,
            fixedBuild.Succeeded && run.ExitCode == 0 && run.Output.Contains(ExpectedMessage, StringComparison.Ordinal));
    }

    private static void RewriteProjectToUseRuntimeProjectReference(string projectFile, string runtimeProjectFile)
    {
        var document = XDocument.Load(projectFile);
        var itemGroups = document.Root?.Elements("ItemGroup").ToArray() ?? [];
        foreach (var packageReference in itemGroups
            .SelectMany(group => group.Elements("PackageReference"))
            .Where(reference => string.Equals((string?)reference.Attribute("Include"), "Electron2D", StringComparison.Ordinal))
            .ToArray())
        {
            packageReference.Remove();
        }

        var itemGroup = itemGroups.FirstOrDefault(group => group.Elements("ProjectReference").Any()) ??
            new XElement("ItemGroup");
        if (itemGroup.Parent is null)
        {
            document.Root?.Add(itemGroup);
        }

        itemGroup.Add(new XElement("ProjectReference", new XAttribute("Include", Path.GetFullPath(runtimeProjectFile))));
        document.Save(projectFile);
    }

    private static void WriteScene(string scenePath)
    {
        var document = new Electron2D.SceneFileDocument(
            externalReferences: null,
            internalResources: null,
            nodes:
            [
                new Electron2D.SceneFileNode(1, "Electron2D.Node2D", "Main", parentId: null, ownerId: null),
                new Electron2D.SceneFileNode(2, "Electron2D.Node2D", "Player", parentId: 1, ownerId: 1)
            ]);
        File.WriteAllText(scenePath, Electron2D.SceneFileTextSerializer.Serialize(document));
    }

    private static void WriteProgram(string programPath, string namespaceName, string className)
    {
        File.WriteAllText(programPath, $$"""
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
            using Electron2D;
            using {{namespaceName}};

            var tree = new SceneTree();
            var script = new {{className}} { Name = "Player" };
            tree.Root.AddChild(script);

            Console.WriteLine($"EditorScriptMessage={script.Message}");
            return script.Message == "{{ExpectedMessage}}" ? 0 : 1;
            """);
    }

    private static string CreateInvalidScript(string namespaceName, string className)
    {
        return $$"""
            using Electron2D;

            namespace {{namespaceName}};

            public sealed class {{className}} : Node
            {
                public string Message => "broken"
            }
            """;
    }

    private static string CreateValidScript(string namespaceName, string className)
    {
        return $$"""
            using Electron2D;

            namespace {{namespaceName}};

            public sealed class {{className}} : Node
            {
                public string Message => "{{ExpectedMessage}}";
            }
            """;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "templates", "electron2d-empty")) &&
                File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        while (workingDirectory is not null)
        {
            if (Directory.Exists(Path.Combine(workingDirectory.FullName, "templates", "electron2d-empty")) &&
                File.Exists(Path.Combine(workingDirectory.FullName, "src", "Electron2D.sln")))
            {
                return workingDirectory.FullName;
            }

            workingDirectory = workingDirectory.Parent;
        }

        throw new InvalidOperationException("Electron2D repository root was not found.");
    }
}
