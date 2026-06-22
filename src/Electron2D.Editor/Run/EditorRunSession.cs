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

namespace Electron2D.Editor.Run;

internal sealed class EditorRunSession : IDisposable
{
    private readonly Process process;
    private readonly EditorOutputConsole sharedOutputConsole;
    private readonly Task<string> standardOutputTask;
    private readonly Task<string> standardErrorTask;
    private bool outputCaptured;

    public EditorRunSession(Process process, EditorRunTarget target, EditorOutputConsole sharedOutputConsole)
    {
        this.process = process ?? throw new ArgumentNullException(nameof(process));
        this.sharedOutputConsole = sharedOutputConsole ?? throw new ArgumentNullException(nameof(sharedOutputConsole));
        Target = target;
        OutputConsole = new EditorOutputConsole();
        standardOutputTask = process.StandardOutput.ReadToEndAsync();
        standardErrorTask = process.StandardError.ReadToEndAsync();
    }

    public EditorRunTarget Target { get; }

    public EditorOutputConsole OutputConsole { get; }

    public bool IsRunning => !process.HasExited;

    public int? ExitCode => process.HasExited ? process.ExitCode : null;

    public bool StopRequested { get; private set; }

    public bool WasRunningWhenStopRequested { get; private set; }

    public bool StopObserved { get; private set; }

    public async Task<int> WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await CaptureOutputAsync().ConfigureAwait(false);
        return process.ExitCode;
    }

    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        StopRequested = true;
        WasRunningWhenStopRequested = !process.HasExited;
        if (WasRunningWhenStopRequested)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        StopObserved = true;
        await CaptureOutputAsync().ConfigureAwait(false);
        return WasRunningWhenStopRequested;
    }

    public void Dispose()
    {
        process.Dispose();
    }

    private async Task CaptureOutputAsync()
    {
        if (outputCaptured)
        {
            return;
        }

        var output = await standardOutputTask.ConfigureAwait(false);
        var error = await standardErrorTask.ConfigureAwait(false);
        OutputConsole.AppendText(output);
        OutputConsole.AppendText(error);
        sharedOutputConsole.AppendText(output);
        sharedOutputConsole.AppendText(error);
        outputCaptured = true;
    }
}
