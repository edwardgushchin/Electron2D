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
using System.Text;

namespace Electron2D.Editor.Scripting;

internal sealed class ScriptWorkspace
{
    private readonly string projectRoot;
    private readonly string rootNamespace;

    public ScriptWorkspace(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        this.projectRoot = Path.GetFullPath(projectRoot);
        Directory.CreateDirectory(this.projectRoot);
        rootNamespace = ReadRootNamespace(this.projectRoot);
    }

    public ScriptCreationResult CreateScript(string className)
    {
        var normalizedClassName = ToCSharpIdentifier(className, "Script");
        var namespaceName = rootNamespace + ".Scripts";
        var relativePath = Path.Combine("Scripts", normalizedClassName + ".cs");
        var scriptPath = ResolveProjectPath(relativePath);
        if (File.Exists(scriptPath))
        {
            throw new InvalidOperationException($"Script file already exists: {relativePath}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);
        File.WriteAllText(scriptPath, CreateScriptTemplate(namespaceName, normalizedClassName));
        return new ScriptCreationResult(
            scriptPath,
            relativePath.Replace(Path.DirectorySeparatorChar, '/'),
            namespaceName,
            normalizedClassName,
            namespaceName + "." + normalizedClassName);
    }

    public CodeDocument OpenScript(string relativePath)
    {
        return CodeDocument.Open(ResolveProjectPath(relativePath));
    }

    public void AttachScriptToNode(string scenePath, int nodeId, string scriptFullTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptFullTypeName);

        var fullScenePath = ResolveProjectPath(Path.GetRelativePath(projectRoot, Path.GetFullPath(scenePath)));
        var document = Electron2D.SceneFileTextSerializer.Deserialize(File.ReadAllText(fullScenePath));
        var found = false;
        var nodes = document.Nodes.Select(node =>
        {
            if (node.Id != nodeId)
            {
                return node;
            }

            found = true;
            return new Electron2D.SceneFileNode(
                node.Id,
                scriptFullTypeName,
                node.Name,
                node.ParentId,
                node.OwnerId,
                node.PersistentGroups,
                node.Properties);
        }).ToArray();

        if (!found)
        {
            throw new InvalidOperationException($"Scene node id {nodeId} was not found.");
        }

        File.WriteAllText(fullScenePath, Electron2D.SceneFileTextSerializer.Serialize(new Electron2D.SceneFileDocument(
            document.ExternalReferences,
            document.InternalResources,
            nodes)));
    }

    private string ResolveProjectPath(string relativePath)
    {
        var candidate = Path.GetFullPath(Path.Combine(
            projectRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!Electron2D.ResourceImportPath.IsSameOrChildOf(projectRoot, candidate))
        {
            throw new ArgumentException("Script workflow path must stay inside the project root.", nameof(relativePath));
        }

        return candidate;
    }

    private static string ReadRootNamespace(string projectRoot)
    {
        var settingsPath = Path.Combine(projectRoot, "project.e2d.json");
        if (File.Exists(settingsPath))
        {
            var loadResult = Electron2D.Electron2DSettingsStore.LoadProject(settingsPath);
            if (loadResult.Succeeded && loadResult.Settings is not null)
            {
                return ToCSharpIdentifier(loadResult.Settings.Name, "Electron2DGame");
            }
        }

        return ToCSharpIdentifier(Path.GetFileName(projectRoot), "Electron2DGame");
    }

    private static string CreateScriptTemplate(string namespaceName, string className)
    {
        return $$"""
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

            namespace {{namespaceName}};

            public sealed class {{className}} : Node
            {
                public override void _Ready()
                {
                }
            }
            """;
    }

    private static string ToCSharpIdentifier(string value, string fallback)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '.')
            {
                builder.Append('_');
            }
        }

        if (builder.Length == 0)
        {
            builder.Append(fallback);
        }

        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }
}
