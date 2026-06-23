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

namespace Electron2D.Editor.ProjectManagement;

internal static class SdkChecker
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static SdkCheckResult Check()
    {
        try
        {
            using var process = StartDotNetVersionProcess();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit((int)DefaultTimeout.TotalMilliseconds))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }

                return new SdkCheckResult(false, string.Empty, "dotnet --version timed out.");
            }

            var output = outputTask.GetAwaiter().GetResult().Trim();
            var error = errorTask.GetAwaiter().GetResult().Trim();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return new SdkCheckResult(true, output, string.Empty);
            }

            return new SdkCheckResult(
                false,
                string.Empty,
                string.IsNullOrWhiteSpace(error) ? "dotnet --version did not return an SDK version." : error);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            return new SdkCheckResult(false, string.Empty, exception.Message);
        }
    }

    private static Process StartDotNetVersionProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--version");

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet --version.");
    }
}
