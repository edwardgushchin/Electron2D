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

namespace Electron2D.Tests.Integration;

public sealed class SdlGpuStartupPolicyTests
{
    [Fact]
    public void AndroidStartupUsesMobileCreateInfoAndSelectsSdlGpuWhenSmokePasses()
    {
        var api = new StartupFakeSdlGpuApi();
        var policy = new Electron2D.SdlGpuStartupPolicy(api, new Electron2D.SdlGpuMobileSmokeTest(api));

        var result = policy.Start(new Electron2D.SdlGpuStartupOptions(
            Electron2D.SdlGpuStartupPlatform.Android,
            Electron2D.SdlGpuFallbackPolicy.Automatic,
            new Electron2D.SdlGpuWindowInfo(1280, 720, 2f, fullscreen: false),
            debugMode: true));

        try
        {
            Assert.NotNull(api.LastCreateInfo);
            Assert.Equal(Electron2D.SdlGpuDeviceProfile.AndroidMobile, api.LastCreateInfo.Value.Profile);
            Assert.True(api.LastCreateInfo.Value.DebugMode);
            Assert.False(api.LastCreateInfo.Value.OptionalFeatures.ClipDistance);
            Assert.False(api.LastCreateInfo.Value.OptionalFeatures.DepthClamping);
            Assert.False(api.LastCreateInfo.Value.OptionalFeatures.IndirectDrawFirstInstance);
            Assert.False(api.LastCreateInfo.Value.OptionalFeatures.Anisotropy);

            Assert.Equal("SDL_GPU", result.SelectedBackendName);
            Assert.Equal(Electron2D.RenderingServer.RenderingProfile.Standard, result.SelectedProfile);
            Assert.False(result.UsedFallback);
            Assert.Equal("Adreno 730", result.GpuName);
            Assert.Equal("vulkan", result.DriverName);
            Assert.Empty(result.Reasons);
            Assert.Equal(
                new[]
                {
                    Electron2D.SdlGpuSmokeStepKind.Texture,
                    Electron2D.SdlGpuSmokeStepKind.Pipeline,
                    Electron2D.SdlGpuSmokeStepKind.CommandBuffer,
                    Electron2D.SdlGpuSmokeStepKind.FirstSubmit
                },
                result.SmokeResult.Steps.Select(step => step.Kind).ToArray());
            Assert.All(result.SmokeResult.Steps, step => Assert.True(step.Succeeded));

            var log = result.ToLogLine();
            Assert.Contains("backend=SDL_GPU", log, StringComparison.Ordinal);
            Assert.Contains("gpu=Adreno 730", log, StringComparison.Ordinal);
            Assert.Contains("driver=vulkan", log, StringComparison.Ordinal);
            Assert.Contains("reasons=", log, StringComparison.Ordinal);
        }
        finally
        {
            Electron2D.RenderingServer.SetBackend(new Electron2D.CompatibilityRenderingBackend());
        }
    }

    [Fact]
    public void AutomaticPolicyFallsBackToCompatibilityAndLogsReasonsWhenSmokeFails()
    {
        var api = new StartupFakeSdlGpuApi
        {
            PipelineSmokeError = "graphics pipeline creation failed on Vulkan driver."
        };
        var policy = new Electron2D.SdlGpuStartupPolicy(api, new Electron2D.SdlGpuMobileSmokeTest(api));

        var result = policy.Start(new Electron2D.SdlGpuStartupOptions(
            Electron2D.SdlGpuStartupPlatform.Android,
            Electron2D.SdlGpuFallbackPolicy.Automatic,
            new Electron2D.SdlGpuWindowInfo(1280, 720, 2f, fullscreen: false),
            debugMode: false));

        try
        {
            Assert.Equal("Compatibility", result.SelectedBackendName);
            Assert.Equal(Electron2D.RenderingServer.RenderingProfile.Compatibility, result.SelectedProfile);
            Assert.True(result.UsedFallback);
            Assert.Equal("Adreno 730", result.GpuName);
            Assert.Equal("vulkan", result.DriverName);
            Assert.Contains("graphics pipeline creation failed", result.Reasons.Single(), StringComparison.Ordinal);
            Assert.Equal(Electron2D.RenderingServer.RenderingProfile.Compatibility, Electron2D.RenderingServer.CurrentProfile);

            var pipeline = Assert.Single(result.SmokeResult.Steps, step => step.Kind == Electron2D.SdlGpuSmokeStepKind.Pipeline);
            Assert.False(pipeline.Succeeded);
            Assert.Equal("graphics pipeline creation failed on Vulkan driver.", pipeline.Reason);

            var commandBuffer = Assert.Single(result.SmokeResult.Steps, step => step.Kind == Electron2D.SdlGpuSmokeStepKind.CommandBuffer);
            var firstSubmit = Assert.Single(result.SmokeResult.Steps, step => step.Kind == Electron2D.SdlGpuSmokeStepKind.FirstSubmit);
            Assert.False(commandBuffer.Succeeded);
            Assert.False(firstSubmit.Succeeded);
            Assert.Contains("previous smoke step failed", commandBuffer.Reason, StringComparison.Ordinal);
            Assert.Contains("previous smoke step failed", firstSubmit.Reason, StringComparison.Ordinal);

            var log = result.ToLogLine();
            Assert.Contains("backend=Compatibility", log, StringComparison.Ordinal);
            Assert.Contains("gpu=Adreno 730", log, StringComparison.Ordinal);
            Assert.Contains("driver=vulkan", log, StringComparison.Ordinal);
            Assert.Contains("graphics pipeline creation failed", log, StringComparison.Ordinal);
        }
        finally
        {
            Electron2D.RenderingServer.SetBackend(new Electron2D.CompatibilityRenderingBackend());
        }
    }

    [Fact]
    public void FailIfUnavailablePolicyThrowsAndKeepsStructuredStartupResult()
    {
        var api = new StartupFakeSdlGpuApi
        {
            TextureSmokeError = "RGBA8 texture support is unavailable."
        };
        var policy = new Electron2D.SdlGpuStartupPolicy(api, new Electron2D.SdlGpuMobileSmokeTest(api));

        var exception = Assert.Throws<Electron2D.SdlGpuStartupException>(() =>
            policy.Start(new Electron2D.SdlGpuStartupOptions(
                Electron2D.SdlGpuStartupPlatform.Android,
                Electron2D.SdlGpuFallbackPolicy.FailIfUnavailable,
                new Electron2D.SdlGpuWindowInfo(1280, 720, 2f, fullscreen: false),
                debugMode: false)));

        var result = exception.Result;
        Assert.Equal("None", result.SelectedBackendName);
        Assert.Null(result.SelectedProfile);
        Assert.False(result.UsedFallback);
        Assert.Equal("Adreno 730", result.GpuName);
        Assert.Equal("vulkan", result.DriverName);
        Assert.Contains("RGBA8 texture support is unavailable.", result.Reasons);
        Assert.Contains("backend=None", result.ToLogLine(), StringComparison.Ordinal);
        Assert.Contains("RGBA8 texture support is unavailable.", exception.Message, StringComparison.Ordinal);

        Electron2D.RenderingServer.SetBackend(new Electron2D.CompatibilityRenderingBackend());
    }

    private sealed class StartupFakeSdlGpuApi : Electron2D.ISdlGpuApi
    {
        public Electron2D.SdlGpuDeviceCreateInfo? LastCreateInfo { get; private set; }

        public string? CreateDeviceError { get; init; }

        public string? ClaimWindowError { get; init; }

        public string? TextureSmokeError { get; init; }

        public string? PipelineSmokeError { get; init; }

        public string? AcquireCommandBufferError { get; init; }

        public string? SubmitCommandBufferError { get; init; }

        public Electron2D.SdlGpuDeviceHandle CreateDevice(Electron2D.SdlGpuDeviceCreateInfo createInfo, out string? error)
        {
            LastCreateInfo = createInfo;
            error = CreateDeviceError;
            return error is null ? new Electron2D.SdlGpuDeviceHandle(1) : default;
        }

        public bool ClaimWindow(Electron2D.SdlGpuDeviceHandle device, Electron2D.SdlGpuWindowInfo window, out string? error)
        {
            error = ClaimWindowError;
            return error is null;
        }

        public Electron2D.SdlGpuDeviceInfo GetDeviceInfo(Electron2D.SdlGpuDeviceHandle device)
        {
            return new Electron2D.SdlGpuDeviceInfo("Adreno 730", "vulkan", "1.3.250", "Android Vulkan driver 1.3.250");
        }

        public bool ValidateTextureSmoke(Electron2D.SdlGpuDeviceHandle device, out string? error)
        {
            error = TextureSmokeError;
            return error is null;
        }

        public bool ValidatePipelineSmoke(Electron2D.SdlGpuDeviceHandle device, out string? error)
        {
            error = PipelineSmokeError;
            return error is null;
        }

        public Electron2D.SdlGpuCommandBufferHandle AcquireCommandBuffer(Electron2D.SdlGpuDeviceHandle device, out string? error)
        {
            error = AcquireCommandBufferError;
            return error is null ? new Electron2D.SdlGpuCommandBufferHandle(2) : default;
        }

        public bool SubmitCommandBuffer(Electron2D.SdlGpuCommandBufferHandle commandBuffer, out string? error)
        {
            error = SubmitCommandBufferError;
            return error is null;
        }

        public Electron2D.SdlGpuFenceHandle SubmitCommandBufferAndAcquireFence(
            Electron2D.SdlGpuCommandBufferHandle commandBuffer,
            out string? error)
        {
            error = SubmitCommandBufferError;
            return error is null ? new Electron2D.SdlGpuFenceHandle(3) : default;
        }

        public bool WaitForFence(
            Electron2D.SdlGpuDeviceHandle device,
            Electron2D.SdlGpuFenceHandle fence,
            out string? error)
        {
            error = null;
            return true;
        }

        public void ReleaseFence(Electron2D.SdlGpuDeviceHandle device, Electron2D.SdlGpuFenceHandle fence)
        {
        }

        public void DestroyDevice(Electron2D.SdlGpuDeviceHandle device)
        {
        }
    }
}
