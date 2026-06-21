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
namespace Electron2D;

internal enum SdlGpuSmokeStepKind
{
    Texture = 0,
    Pipeline = 1,
    CommandBuffer = 2,
    FirstSubmit = 3
}

internal readonly struct SdlGpuSmokeStepResult
{
    public SdlGpuSmokeStepResult(SdlGpuSmokeStepKind kind, bool succeeded, string reason = "")
    {
        Kind = kind;
        Succeeded = succeeded;
        Reason = reason ?? string.Empty;
    }

    public SdlGpuSmokeStepKind Kind { get; }

    public bool Succeeded { get; }

    public string Reason { get; }
}

internal sealed class SdlGpuSmokeResult
{
    public static readonly SdlGpuSmokeResult NotRun = new(Array.Empty<SdlGpuSmokeStepResult>());

    public SdlGpuSmokeResult(IReadOnlyList<SdlGpuSmokeStepResult> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        Steps = steps.ToArray();
    }

    public IReadOnlyList<SdlGpuSmokeStepResult> Steps { get; }

    public bool Passed => Steps.Count > 0 && Steps.All(step => step.Succeeded);

    public IReadOnlyList<string> Reasons => Steps
        .Where(step => !step.Succeeded &&
            !string.IsNullOrWhiteSpace(step.Reason) &&
            !string.Equals(step.Reason, "previous smoke step failed.", StringComparison.Ordinal))
        .Select(step => step.Reason)
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}

internal interface ISdlGpuSmokeTest
{
    SdlGpuSmokeResult Run(SdlGpuRenderingBackend backend);
}

internal sealed class SdlGpuMobileSmokeTest : ISdlGpuSmokeTest
{
    private const string PreviousStepFailedReason = "previous smoke step failed.";
    private readonly ISdlGpuApi _api;

    public SdlGpuMobileSmokeTest(ISdlGpuApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        _api = api;
    }

    public SdlGpuSmokeResult Run(SdlGpuRenderingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        var steps = new List<SdlGpuSmokeStepResult>();
        if (!RunApiStep(
            steps,
            SdlGpuSmokeStepKind.Texture,
            () => _api.ValidateTextureSmoke(backend.Device, out var error) ? null : error ?? "texture smoke failed."))
        {
            AddSkippedSteps(steps, SdlGpuSmokeStepKind.Pipeline, SdlGpuSmokeStepKind.CommandBuffer, SdlGpuSmokeStepKind.FirstSubmit);
            return new SdlGpuSmokeResult(steps);
        }

        if (!RunApiStep(
            steps,
            SdlGpuSmokeStepKind.Pipeline,
            () => _api.ValidatePipelineSmoke(backend.Device, out var error) ? null : error ?? "pipeline smoke failed."))
        {
            AddSkippedSteps(steps, SdlGpuSmokeStepKind.CommandBuffer, SdlGpuSmokeStepKind.FirstSubmit);
            return new SdlGpuSmokeResult(steps);
        }

        SdlGpuFrame frame;
        try
        {
            frame = backend.BeginFrame();
            steps.Add(new SdlGpuSmokeStepResult(SdlGpuSmokeStepKind.CommandBuffer, succeeded: true));
        }
        catch (InvalidOperationException exception)
        {
            steps.Add(new SdlGpuSmokeStepResult(SdlGpuSmokeStepKind.CommandBuffer, succeeded: false, exception.Message));
            AddSkippedSteps(steps, SdlGpuSmokeStepKind.FirstSubmit);
            return new SdlGpuSmokeResult(steps);
        }

        try
        {
            backend.EndFrame(frame);
            steps.Add(new SdlGpuSmokeStepResult(SdlGpuSmokeStepKind.FirstSubmit, succeeded: true));
        }
        catch (InvalidOperationException exception)
        {
            steps.Add(new SdlGpuSmokeStepResult(SdlGpuSmokeStepKind.FirstSubmit, succeeded: false, exception.Message));
        }

        return new SdlGpuSmokeResult(steps);
    }

    private static bool RunApiStep(
        ICollection<SdlGpuSmokeStepResult> steps,
        SdlGpuSmokeStepKind kind,
        Func<string?> run)
    {
        var reason = run();
        if (reason is null)
        {
            steps.Add(new SdlGpuSmokeStepResult(kind, succeeded: true));
            return true;
        }

        steps.Add(new SdlGpuSmokeStepResult(kind, succeeded: false, reason));
        return false;
    }

    private static void AddSkippedSteps(ICollection<SdlGpuSmokeStepResult> steps, params SdlGpuSmokeStepKind[] skipped)
    {
        foreach (var kind in skipped)
        {
            steps.Add(new SdlGpuSmokeStepResult(kind, succeeded: false, PreviousStepFailedReason));
        }
    }
}
