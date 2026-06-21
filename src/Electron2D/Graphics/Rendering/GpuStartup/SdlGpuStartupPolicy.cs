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

internal enum SdlGpuStartupPlatform
{
    Desktop = 0,
    Android = 1
}

internal enum SdlGpuFallbackPolicy
{
    Automatic = 0,
    FailIfUnavailable = 1
}

internal readonly struct SdlGpuStartupOptions
{
    public SdlGpuStartupOptions(
        SdlGpuStartupPlatform platform,
        SdlGpuFallbackPolicy fallbackPolicy,
        SdlGpuWindowInfo window,
        bool debugMode)
    {
        Platform = platform;
        FallbackPolicy = fallbackPolicy;
        Window = window;
        DebugMode = debugMode;
    }

    public SdlGpuStartupPlatform Platform { get; }

    public SdlGpuFallbackPolicy FallbackPolicy { get; }

    public SdlGpuWindowInfo Window { get; }

    public bool DebugMode { get; }
}

internal sealed class SdlGpuStartupResult
{
    public SdlGpuStartupResult(
        string selectedBackendName,
        RenderingServer.RenderingProfile? selectedProfile,
        bool usedFallback,
        SdlGpuDeviceInfo deviceInfo,
        IReadOnlyList<string> reasons,
        SdlGpuSmokeResult smokeResult)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedBackendName);
        ArgumentNullException.ThrowIfNull(reasons);
        ArgumentNullException.ThrowIfNull(smokeResult);

        SelectedBackendName = selectedBackendName;
        SelectedProfile = selectedProfile;
        UsedFallback = usedFallback;
        GpuName = deviceInfo.GpuName;
        DriverName = deviceInfo.DriverName;
        DriverVersion = deviceInfo.DriverVersion;
        DriverInfo = deviceInfo.DriverInfo;
        Reasons = reasons.Where(reason => !string.IsNullOrWhiteSpace(reason)).Distinct(StringComparer.Ordinal).ToArray();
        SmokeResult = smokeResult;
    }

    public string SelectedBackendName { get; }

    public RenderingServer.RenderingProfile? SelectedProfile { get; }

    public bool UsedFallback { get; }

    public string GpuName { get; }

    public string DriverName { get; }

    public string DriverVersion { get; }

    public string DriverInfo { get; }

    public IReadOnlyList<string> Reasons { get; }

    public SdlGpuSmokeResult SmokeResult { get; }

    public string ToLogLine()
    {
        return $"backend={SelectedBackendName}|profile={SelectedProfile?.ToString() ?? "None"}|fallback={UsedFallback}|gpu={GpuName}|driver={DriverName}|driverVersion={DriverVersion}|reasons={string.Join(';', Reasons)}";
    }
}

internal sealed class SdlGpuStartupException : InvalidOperationException
{
    public SdlGpuStartupException(SdlGpuStartupResult result)
        : base(CreateMessage(result))
    {
        Result = result;
    }

    public SdlGpuStartupResult Result { get; }

    private static string CreateMessage(SdlGpuStartupResult result)
    {
        return $"SDL_GPU startup failed. {result.ToLogLine()}";
    }
}

internal sealed class SdlGpuStartupPolicy
{
    private readonly ISdlGpuApi _api;
    private readonly ISdlGpuSmokeTest _smokeTest;

    public SdlGpuStartupPolicy(ISdlGpuApi api, ISdlGpuSmokeTest smokeTest)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(smokeTest);

        _api = api;
        _smokeTest = smokeTest;
    }

    public SdlGpuStartupResult Start(SdlGpuStartupOptions options)
    {
        var createInfo = CreateDeviceInfo(options.Platform, options.DebugMode);
        var gpuBackend = new SdlGpuRenderingBackend(_api, createInfo);
        var smokeResult = SdlGpuSmokeResult.NotRun;
        var reasons = new List<string>();
        var deviceInfo = SdlGpuDeviceInfo.Unknown;

        try
        {
            gpuBackend.Initialize(options.Window);
            deviceInfo = gpuBackend.DeviceInfo;
            smokeResult = _smokeTest.Run(gpuBackend);
            reasons.AddRange(smokeResult.Reasons);

            if (smokeResult.Passed)
            {
                RenderingServer.SetBackend(gpuBackend);
                return new SdlGpuStartupResult(
                    gpuBackend.Name,
                    gpuBackend.Profile,
                    usedFallback: false,
                    deviceInfo,
                    reasons,
                    smokeResult);
            }
        }
        catch (InvalidOperationException exception)
        {
            deviceInfo = gpuBackend.DeviceInfo;
            reasons.Add(exception.Message);
        }

        gpuBackend.Shutdown();

        if (options.FallbackPolicy == SdlGpuFallbackPolicy.Automatic)
        {
            var compatibility = new CompatibilityRenderingBackend();
            RenderingServer.SetBackend(compatibility);
            return new SdlGpuStartupResult(
                compatibility.Name,
                compatibility.Profile,
                usedFallback: true,
                deviceInfo,
                reasons,
                smokeResult);
        }

        var result = new SdlGpuStartupResult(
            "None",
            selectedProfile: null,
            usedFallback: false,
            deviceInfo,
            reasons,
            smokeResult);
        throw new SdlGpuStartupException(result);
    }

    private static SdlGpuDeviceCreateInfo CreateDeviceInfo(SdlGpuStartupPlatform platform, bool debugMode)
    {
        return platform == SdlGpuStartupPlatform.Android
            ? SdlGpuDeviceCreateInfo.AndroidMobile(debugMode)
            : SdlGpuDeviceCreateInfo.Standard(debugMode);
    }
}
