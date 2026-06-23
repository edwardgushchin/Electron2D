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
namespace Electron2D.Editor.ProjectManagement;

internal sealed class ProjectOpenResult
{
    private ProjectOpenResult(
        bool succeeded,
        string projectName,
        string projectPath,
        string projectSettingsPath,
        string mainScenePath,
        Electron2D.Electron2DRendererProfileSetting rendererProfile,
        int recentProjectCount,
        string[] diagnostics)
    {
        Succeeded = succeeded;
        ProjectName = projectName;
        ProjectPath = projectPath;
        ProjectSettingsPath = projectSettingsPath;
        MainScenePath = mainScenePath;
        RendererProfile = rendererProfile;
        RecentProjectCount = recentProjectCount;
        Diagnostics = diagnostics;
    }

    public bool Succeeded { get; }

    public string ProjectName { get; }

    public string ProjectPath { get; }

    public string ProjectSettingsPath { get; }

    public string MainScenePath { get; }

    public Electron2D.Electron2DRendererProfileSetting RendererProfile { get; }

    public int RecentProjectCount { get; }

    public string[] Diagnostics { get; }

    public static ProjectOpenResult Success(
        string projectName,
        string projectPath,
        string projectSettingsPath,
        string mainScenePath,
        Electron2D.Electron2DRendererProfileSetting rendererProfile,
        int recentProjectCount)
    {
        return new ProjectOpenResult(
            true,
            projectName,
            projectPath,
            projectSettingsPath,
            mainScenePath,
            rendererProfile,
            recentProjectCount,
            []);
    }

    public static ProjectOpenResult Failure(string projectPath, string projectSettingsPath, params string[] diagnostics)
    {
        return new ProjectOpenResult(
            false,
            string.Empty,
            projectPath,
            projectSettingsPath,
            string.Empty,
            Electron2D.Electron2DRendererProfileSetting.Automatic,
            0,
            diagnostics);
    }
}
