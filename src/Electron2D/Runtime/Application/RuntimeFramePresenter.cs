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
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal interface IRuntimeFramePresenter : IDisposable
{
    bool PresentationSyncObserved => false;

    double LastSubmitTimeSeconds => 0d;

    double LastPresentTimeSeconds => 0d;

    RuntimePresentedFrame Present(
        CanvasItemRenderPlan renderPlan,
        Vector2I windowSize,
        Color clearColor,
        bool captureFrame);
}

internal readonly record struct RuntimePresentationSettings(bool SyncRequested, int TargetFrameRate)
{
    public static RuntimePresentationSettings Default { get; } = new(SyncRequested: true, TargetFrameRate: 60);
}

internal interface IRuntimeGpuPresenterApi
{
    bool SetSwapchainParameters(
        IntPtr device,
        IntPtr window,
        SDL.GPUSwapchainComposition composition,
        SDL.GPUPresentMode presentMode);

    bool WaitAndAcquireSwapchainTexture(
        IntPtr commandBuffer,
        IntPtr window,
        out IntPtr texture,
        out uint width,
        out uint height);

    bool CancelCommandBuffer(IntPtr commandBuffer);

    void BeforePresentationResources();

    SDL.GPUTextureFormat GetSwapchainTextureFormat(IntPtr device, IntPtr window);

    IntPtr CreateGraphicsPipeline(IntPtr device, in SDL.GPUGraphicsPipelineCreateInfo createInfo);

    IntPtr CreateSampler(IntPtr device, in SDL.GPUSamplerCreateInfo createInfo);

    IntPtr CreateBuffer(IntPtr device, in SDL.GPUBufferCreateInfo createInfo);

    IntPtr CreateTexture(IntPtr device, in SDL.GPUTextureCreateInfo createInfo);

    IntPtr CreateTransferBuffer(IntPtr device, in SDL.GPUTransferBufferCreateInfo createInfo);

    IntPtr MapTransferBuffer(IntPtr device, IntPtr transferBuffer, bool cycle);

    void UnmapTransferBuffer(IntPtr device, IntPtr transferBuffer);

    IntPtr BeginCopyPass(IntPtr commandBuffer);

    void EndCopyPass(IntPtr copyPass);

    void UploadToBuffer(
        IntPtr copyPass,
        in SDL.GPUTransferBufferLocation source,
        in SDL.GPUBufferRegion destination,
        bool cycle);

    void UploadToTexture(
        IntPtr copyPass,
        in SDL.GPUTextureTransferInfo source,
        in SDL.GPUTextureRegion destination,
        bool cycle);

    void DownloadFromTexture(
        IntPtr copyPass,
        in SDL.GPUTextureRegion source,
        in SDL.GPUTextureTransferInfo destination);

    IntPtr BeginRenderPass(IntPtr commandBuffer, SDL.GPUColorTargetInfo[] colorTargets);

    void EndRenderPass(IntPtr renderPass);

    void BindVertexBuffers(IntPtr renderPass, uint firstSlot, SDL.GPUBufferBinding[] bindings);

    void BindGraphicsPipeline(IntPtr renderPass, IntPtr pipeline);

    void BindFragmentSamplers(IntPtr renderPass, uint firstSlot, SDL.GPUTextureSamplerBinding[] bindings);

    void DrawPrimitives(IntPtr renderPass, uint vertexCount, uint numInstances, uint firstVertex, uint firstInstance);

    bool GetWindowSize(IntPtr window, out int width, out int height);

    string GetError();

    void ReleaseGraphicsPipeline(IntPtr device, IntPtr pipeline);

    void ReleaseSampler(IntPtr device, IntPtr sampler);

    void ReleaseBuffer(IntPtr device, IntPtr buffer);

    void ReleaseTexture(IntPtr device, IntPtr texture);

    void ReleaseTransferBuffer(IntPtr device, IntPtr transferBuffer);

    void ReleaseWindowFromDevice(IntPtr device, IntPtr window);
}

internal sealed class RuntimeSdlGpuPresenterApi : IRuntimeGpuPresenterApi
{
    public static readonly RuntimeSdlGpuPresenterApi Instance = new();

    private RuntimeSdlGpuPresenterApi()
    {
    }

    public bool SetSwapchainParameters(
        IntPtr device,
        IntPtr window,
        SDL.GPUSwapchainComposition composition,
        SDL.GPUPresentMode presentMode)
    {
        return SDL.SetGPUSwapchainParameters(device, window, composition, presentMode);
    }

    public bool WaitAndAcquireSwapchainTexture(
        IntPtr commandBuffer,
        IntPtr window,
        out IntPtr texture,
        out uint width,
        out uint height)
    {
        return SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer, window, out texture, out width, out height);
    }

    public bool CancelCommandBuffer(IntPtr commandBuffer)
    {
        return SDL.CancelGPUCommandBuffer(commandBuffer);
    }

    public void BeforePresentationResources()
    {
    }

    public SDL.GPUTextureFormat GetSwapchainTextureFormat(IntPtr device, IntPtr window)
    {
        return SDL.GetGPUSwapchainTextureFormat(device, window);
    }

    public IntPtr CreateGraphicsPipeline(IntPtr device, in SDL.GPUGraphicsPipelineCreateInfo createInfo)
    {
        return SDL.CreateGPUGraphicsPipeline(device, in createInfo);
    }

    public IntPtr CreateSampler(IntPtr device, in SDL.GPUSamplerCreateInfo createInfo)
    {
        return SDL.CreateGPUSampler(device, in createInfo);
    }

    public IntPtr CreateBuffer(IntPtr device, in SDL.GPUBufferCreateInfo createInfo)
    {
        return SDL.CreateGPUBuffer(device, in createInfo);
    }

    public IntPtr CreateTexture(IntPtr device, in SDL.GPUTextureCreateInfo createInfo)
    {
        return SDL.CreateGPUTexture(device, in createInfo);
    }

    public IntPtr CreateTransferBuffer(IntPtr device, in SDL.GPUTransferBufferCreateInfo createInfo)
    {
        return SDL.CreateGPUTransferBuffer(device, in createInfo);
    }

    public IntPtr MapTransferBuffer(IntPtr device, IntPtr transferBuffer, bool cycle)
    {
        return SDL.MapGPUTransferBuffer(device, transferBuffer, cycle);
    }

    public void UnmapTransferBuffer(IntPtr device, IntPtr transferBuffer)
    {
        SDL.UnmapGPUTransferBuffer(device, transferBuffer);
    }

    public IntPtr BeginCopyPass(IntPtr commandBuffer)
    {
        return SDL.BeginGPUCopyPass(commandBuffer);
    }

    public void EndCopyPass(IntPtr copyPass)
    {
        SDL.EndGPUCopyPass(copyPass);
    }

    public void UploadToBuffer(
        IntPtr copyPass,
        in SDL.GPUTransferBufferLocation source,
        in SDL.GPUBufferRegion destination,
        bool cycle)
    {
        SDL.UploadToGPUBuffer(copyPass, in source, in destination, cycle);
    }

    public void UploadToTexture(
        IntPtr copyPass,
        in SDL.GPUTextureTransferInfo source,
        in SDL.GPUTextureRegion destination,
        bool cycle)
    {
        SDL.UploadToGPUTexture(copyPass, in source, in destination, cycle);
    }

    public void DownloadFromTexture(
        IntPtr copyPass,
        in SDL.GPUTextureRegion source,
        in SDL.GPUTextureTransferInfo destination)
    {
        SDL.DownloadFromGPUTexture(copyPass, in source, in destination);
    }

    public IntPtr BeginRenderPass(IntPtr commandBuffer, SDL.GPUColorTargetInfo[] colorTargets)
    {
        return SDL.BeginGPURenderPass(commandBuffer, in colorTargets, numColorTargets: 1, IntPtr.Zero);
    }

    public void EndRenderPass(IntPtr renderPass)
    {
        SDL.EndGPURenderPass(renderPass);
    }

    public void BindVertexBuffers(IntPtr renderPass, uint firstSlot, SDL.GPUBufferBinding[] bindings)
    {
        SDL.BindGPUVertexBuffers(renderPass, firstSlot, bindings, numBindings: 1);
    }

    public void BindGraphicsPipeline(IntPtr renderPass, IntPtr pipeline)
    {
        SDL.BindGPUGraphicsPipeline(renderPass, pipeline);
    }

    public void BindFragmentSamplers(IntPtr renderPass, uint firstSlot, SDL.GPUTextureSamplerBinding[] bindings)
    {
        SDL.BindGPUFragmentSamplers(renderPass, firstSlot, bindings, numBindings: 1);
    }

    public void DrawPrimitives(IntPtr renderPass, uint vertexCount, uint numInstances, uint firstVertex, uint firstInstance)
    {
        SDL.DrawGPUPrimitives(renderPass, vertexCount, numInstances, firstVertex, firstInstance);
    }

    public bool GetWindowSize(IntPtr window, out int width, out int height)
    {
        return SDL.GetWindowSize(window, out width, out height);
    }

    public string GetError()
    {
        return SDL.GetError();
    }

    public void ReleaseGraphicsPipeline(IntPtr device, IntPtr pipeline)
    {
        SDL.ReleaseGPUGraphicsPipeline(device, pipeline);
    }

    public void ReleaseSampler(IntPtr device, IntPtr sampler)
    {
        SDL.ReleaseGPUSampler(device, sampler);
    }

    public void ReleaseBuffer(IntPtr device, IntPtr buffer)
    {
        SDL.ReleaseGPUBuffer(device, buffer);
    }

    public void ReleaseTexture(IntPtr device, IntPtr texture)
    {
        SDL.ReleaseGPUTexture(device, texture);
    }

    public void ReleaseTransferBuffer(IntPtr device, IntPtr transferBuffer)
    {
        SDL.ReleaseGPUTransferBuffer(device, transferBuffer);
    }

    public void ReleaseWindowFromDevice(IntPtr device, IntPtr window)
    {
        SDL.ReleaseWindowFromGPUDevice(device, window);
    }
}

internal sealed class RuntimeFramePresenter : IRuntimeFramePresenter
{
    internal const string RenderSource = "RenderingServer";
    internal const string GpuPresentationBackend = "SDL_GPU";
    internal const string SdlRendererPresentationBackend = "SDL_Renderer";
    private const int AllocationWarmupFrames = 120;

    private readonly Func<IntPtr, Vector2I, RuntimePresentationSettings, IRuntimeFramePresenter> gpuFactory;
    private readonly Func<IntPtr, Vector2I, RuntimePresentationSettings, IRuntimeFramePresenter> fallbackFactory;
    private readonly IntPtr window;
    private readonly Vector2I presentationSize;
    private readonly RuntimePresentationSettings presentationSettings;
    private IRuntimeFramePresenter activePresenter;
    private bool usedFallbackPresenter;
    private string fallbackReason = string.Empty;
    private bool disposed;

    public RuntimeFramePresenter(IntPtr window, Vector2I presentationSize)
        : this(window, presentationSize, RuntimePresentationSettings.Default)
    {
    }

    public RuntimeFramePresenter(
        IntPtr window,
        Vector2I presentationSize,
        RuntimePresentationSettings presentationSettings)
        : this(
            window,
            presentationSize,
            presentationSettings,
            static (nativeWindow, size, settings) => new RuntimeGpuFramePresenter(nativeWindow, size, settings),
            static (nativeWindow, size, settings) => new RuntimeSdlRendererFramePresenter(nativeWindow, size, settings))
    {
    }

    internal RuntimeFramePresenter(
        IntPtr window,
        Vector2I presentationSize,
        Func<IntPtr, Vector2I, IRuntimeFramePresenter> gpuFactory,
        Func<IntPtr, Vector2I, IRuntimeFramePresenter> fallbackFactory)
        : this(
            window,
            presentationSize,
            RuntimePresentationSettings.Default,
            (nativeWindow, size, _) => gpuFactory(nativeWindow, size),
            (nativeWindow, size, _) => fallbackFactory(nativeWindow, size))
    {
    }

    internal RuntimeFramePresenter(
        IntPtr window,
        Vector2I presentationSize,
        RuntimePresentationSettings presentationSettings,
        Func<IntPtr, Vector2I, RuntimePresentationSettings, IRuntimeFramePresenter> gpuFactory,
        Func<IntPtr, Vector2I, RuntimePresentationSettings, IRuntimeFramePresenter> fallbackFactory)
    {
        if (window == IntPtr.Zero)
        {
            throw new ArgumentException("Runtime frame presenter requires a valid window.", nameof(window));
        }

        ArgumentNullException.ThrowIfNull(gpuFactory);
        ArgumentNullException.ThrowIfNull(fallbackFactory);

        this.window = window;
        this.presentationSize = presentationSize;
        this.presentationSettings = presentationSettings;
        this.gpuFactory = gpuFactory;
        this.fallbackFactory = fallbackFactory;
        activePresenter = CreatePrimaryOrFallback();
    }

    public bool PresentationSyncObserved => activePresenter.PresentationSyncObserved;

    public double LastSubmitTimeSeconds => activePresenter.LastSubmitTimeSeconds;

    public double LastPresentTimeSeconds => activePresenter.LastPresentTimeSeconds;

    public RuntimePresentedFrame Present(
        CanvasItemRenderPlan renderPlan,
        Vector2I windowSize,
        Color clearColor,
        bool captureFrame)
    {
        ThrowIfDisposed();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            return ApplyFrameDiagnostics(
                activePresenter.Present(renderPlan, windowSize, clearColor, captureFrame),
                allocatedBefore,
                captureFrame);
        }
        catch (GpuPresenterUnavailableException exception) when (!usedFallbackPresenter)
        {
            SwitchToFallback(exception);
            return ApplyFrameDiagnostics(
                activePresenter.Present(renderPlan, windowSize, clearColor, captureFrame),
                allocatedBefore,
                captureFrame);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        activePresenter.Dispose();
        disposed = true;
    }

    private IRuntimeFramePresenter CreatePrimaryOrFallback()
    {
        try
        {
            return gpuFactory(window, presentationSize, presentationSettings);
        }
        catch (GpuPresenterUnavailableException exception)
        {
            usedFallbackPresenter = true;
            fallbackReason = exception.Message;
            return fallbackFactory(window, presentationSize, presentationSettings);
        }
    }

    private void SwitchToFallback(GpuPresenterUnavailableException exception)
    {
        activePresenter.Dispose();
        usedFallbackPresenter = true;
        fallbackReason = exception.Message;
        activePresenter = fallbackFactory(window, presentationSize, presentationSettings);
    }

    private int presentedFrames;
    private int presenterMeasuredFrames;
    private long maxPresenterManagedBytesPerFrame;
    private long capturePresenterManagedBytesAllocated;

    private RuntimePresentedFrame ApplyFrameDiagnostics(
        RuntimePresentedFrame frame,
        long allocatedBefore,
        bool captureFrame)
    {
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var shouldReportAllocations = captureFrame || presentedFrames >= AllocationWarmupFrames;
        if (shouldReportAllocations)
        {
            var measuredBytes = Math.Max(0, allocatedBytes);
            if (!captureFrame)
            {
                presenterMeasuredFrames++;
                maxPresenterManagedBytesPerFrame = Math.Max(maxPresenterManagedBytesPerFrame, measuredBytes);
            }
            else
            {
                capturePresenterManagedBytesAllocated += measuredBytes;
            }
        }

        presentedFrames++;

        return ApplyFallbackDiagnostics(frame with
        {
            Diagnostics = frame.Diagnostics with
            {
                MaxPresenterManagedBytesPerFrame = maxPresenterManagedBytesPerFrame,
                PresenterMeasuredFrames = presenterMeasuredFrames,
                CapturePresenterManagedBytesAllocated = capturePresenterManagedBytesAllocated
            }
        });
    }

    private RuntimePresentedFrame ApplyFallbackDiagnostics(RuntimePresentedFrame frame)
    {
        return frame with
        {
            Diagnostics = frame.Diagnostics with
            {
                UsedFallbackPresenter = usedFallbackPresenter,
                FallbackReason = usedFallbackPresenter ? fallbackReason : string.Empty
            }
        };
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(RuntimeFramePresenter));
        }
    }
}

internal sealed class RuntimeGpuFramePresenter : IRuntimeFramePresenter
{
    private const int FloatsPerVertex = 8;
    private const int BytesPerFloat = sizeof(float);
    private const int BytesPerVertex = FloatsPerVertex * BytesPerFloat;
    private const int InitialVertexCapacity = 65_536;
    private const int CircleSegmentCount = 32;
    private const SDL.GPUTextureFormat ScreenshotTextureFormat = SDL.GPUTextureFormat.R8G8B8A8Unorm;

    private const string VertexShaderSource = """
        struct VSInput
        {
            float2 Position : TEXCOORD0;
            float2 TexCoord : TEXCOORD1;
            float4 Color : TEXCOORD2;
        };

        struct VSOutput
        {
            float2 TexCoord : TEXCOORD0;
            float4 Color : TEXCOORD1;
            float4 Position : SV_Position;
        };

        VSOutput VSMain(VSInput input)
        {
            VSOutput output;
            output.Position = float4(input.Position.xy, 0.0f, 1.0f);
            output.TexCoord = input.TexCoord;
            output.Color = input.Color;
            return output;
        }
        """;

    private const string SolidFragmentShaderSource = """
        struct PSInput
        {
            float2 TexCoord : TEXCOORD0;
            float4 Color : TEXCOORD1;
            float4 Position : SV_Position;
        };

        float4 PSMain(PSInput input) : SV_Target
        {
            return input.Color + float4(input.TexCoord.x * 0.000001f, input.TexCoord.y * 0.000001f, 0.0f, 0.0f);
        }
        """;

    private const string TexturedFragmentShaderSource = """
        Texture2D<float4> Texture0 : register(t0, space2);
        SamplerState Sampler0 : register(s0, space2);

        struct PSInput
        {
            float2 TexCoord : TEXCOORD0;
            float4 Color : TEXCOORD1;
            float4 Position : SV_Position;
        };

        float4 PSMain(PSInput input) : SV_Target
        {
            return Texture0.Sample(Sampler0, input.TexCoord) * input.Color;
        }
        """;

    private readonly IntPtr window;
    private readonly SdlGpuRenderingBackend backend;
    private readonly IRuntimeGpuPresenterApi gpuApi;
    private readonly Dictionary<Texture2D, RuntimeTextureResource> textureCache = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Texture2D, RuntimeTextureResource> stagedTextureCache = new(ReferenceEqualityComparer.Instance);
    private readonly List<RuntimeDrawBatch> drawBatches = new();
    private readonly List<PendingTextureUpload> pendingTextureUploads = new();
    private readonly List<IntPtr> submittedTransferBuffers = new();
    private readonly SDL.GPUColorTargetInfo[] colorTargets = new SDL.GPUColorTargetInfo[1];
    private readonly SDL.GPUBufferBinding[] vertexBindings = new SDL.GPUBufferBinding[1];
    private readonly SDL.GPUTextureSamplerBinding[] textureBindings = new SDL.GPUTextureSamplerBinding[1];
    private Vector2I presentationSize;
    private IntPtr solidPipeline;
    private IntPtr texturedPipeline;
    private IntPtr screenshotSolidPipeline;
    private IntPtr screenshotTexturedPipeline;
    private IntPtr screenshotTexture;
    private IntPtr screenshotTransferBuffer;
    private IntPtr vertexBuffer;
    private IntPtr vertexTransferBuffer;
    private IntPtr sampler;
    private Vector2I screenshotTextureSize;
    private float[] vertexScratch = new float[InitialVertexCapacity * FloatsPerVertex];
    private int vertexCapacity = InitialVertexCapacity;
    private int vertexCount;
    private int textureUploads;
    private int textureCacheHits;
    private int pendingTextureCacheHits;
    private int presentationResourcesCreated;
    private int observedPresentationResizes;
    private int screenshotResourcesRecreated;
    private RuntimeShaderCrossLease? shaderCrossLease;
    private bool windowClaimed;
    private bool disposed;

    public RuntimeGpuFramePresenter(IntPtr window, Vector2I presentationSize)
        : this(window, presentationSize, RuntimePresentationSettings.Default)
    {
    }

    public RuntimeGpuFramePresenter(
        IntPtr window,
        Vector2I presentationSize,
        RuntimePresentationSettings presentationSettings)
        : this(window, presentationSize, presentationSettings, RuntimeSdlGpuPresenterApi.Instance)
    {
    }

    private RuntimeGpuFramePresenter(
        IntPtr window,
        Vector2I presentationSize,
        RuntimePresentationSettings presentationSettings,
        IRuntimeGpuPresenterApi gpuApi)
    {
        if (window == IntPtr.Zero)
        {
            throw new ArgumentException("Runtime GPU frame presenter requires a valid window.", nameof(window));
        }

        ArgumentNullException.ThrowIfNull(gpuApi);

        this.window = window;
        this.gpuApi = gpuApi;
        this.presentationSize = presentationSize;
        backend = new SdlGpuRenderingBackend(new SdlGpuApi(), debugMode: false);

        try
        {
            backend.Initialize(new SdlGpuWindowInfo(
                presentationSize.X,
                presentationSize.Y,
                dpiScale: 1f,
                fullscreen: false,
                nativeWindowHandle: window));
            windowClaimed = true;

            var presentMode = presentationSettings.SyncRequested
                ? SDL.GPUPresentMode.VSync
                : SDL.GPUPresentMode.Immediate;
            if (!gpuApi.SetSwapchainParameters(
                backend.Device.Value,
                window,
                SDL.GPUSwapchainComposition.SDR,
                presentMode))
            {
                throw new GpuPresenterUnavailableException("Runtime GPU swapchain setup failed: " + gpuApi.GetError());
            }

            PresentationSyncObserved = presentMode == SDL.GPUPresentMode.VSync;
            shaderCrossLease = RuntimeShaderCrossService.Acquire();
        }
        catch (GpuPresenterUnavailableException)
        {
            Dispose();
            throw;
        }
        catch (InvalidOperationException exception)
        {
            Dispose();
            throw new GpuPresenterUnavailableException("Runtime GPU presenter initialization failed: " + exception.Message, exception);
        }
    }

    internal RuntimeGpuFramePresenter(
        IntPtr window,
        Vector2I presentationSize,
        SdlGpuRenderingBackend backend,
        IRuntimeGpuPresenterApi gpuApi,
        bool presentationResourcesReady)
        : this(
            window,
            presentationSize,
            backend,
            gpuApi,
            presentationResourcesReady,
            RuntimePresentationSettings.Default)
    {
    }

    internal RuntimeGpuFramePresenter(
        IntPtr window,
        Vector2I presentationSize,
        SdlGpuRenderingBackend backend,
        IRuntimeGpuPresenterApi gpuApi,
        bool presentationResourcesReady,
        RuntimePresentationSettings presentationSettings)
    {
        if (window == IntPtr.Zero)
        {
            throw new ArgumentException("Runtime GPU frame presenter requires a valid window.", nameof(window));
        }

        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(gpuApi);

        this.window = window;
        this.gpuApi = gpuApi;
        this.presentationSize = presentationSize;
        this.backend = backend;
        var presentMode = presentationSettings.SyncRequested
            ? SDL.GPUPresentMode.VSync
            : SDL.GPUPresentMode.Immediate;
        if (!gpuApi.SetSwapchainParameters(
            backend.Device.Value,
            window,
            SDL.GPUSwapchainComposition.SDR,
            presentMode))
        {
            throw new GpuPresenterUnavailableException("Runtime GPU swapchain setup failed: " + gpuApi.GetError());
        }

        PresentationSyncObserved = presentMode == SDL.GPUPresentMode.VSync;
        if (presentationResourcesReady)
        {
            solidPipeline = new IntPtr(101);
            texturedPipeline = new IntPtr(102);
            screenshotSolidPipeline = new IntPtr(103);
            screenshotTexturedPipeline = new IntPtr(104);
            sampler = new IntPtr(105);
            vertexBuffer = new IntPtr(106);
            vertexTransferBuffer = new IntPtr(107);
            vertexCapacity = InitialVertexCapacity;
            presentationResourcesCreated = 1;
        }
    }

    public bool PresentationSyncObserved { get; private set; }

    public double LastSubmitTimeSeconds { get; private set; }

    public double LastPresentTimeSeconds { get; private set; }

    public RuntimePresentedFrame Present(
        CanvasItemRenderPlan renderPlan,
        Vector2I windowSize,
        Color clearColor,
        bool captureFrame)
    {
        ArgumentNullException.ThrowIfNull(renderPlan);
        ThrowIfDisposed();
        UpdateObservedPresentationSize(windowSize);

        var frameStartTimestamp = Stopwatch.GetTimestamp();
        var presentTicks = 0L;
        var frame = backend.BeginFrame();
        var frameSubmitted = false;
        var terminalPathStarted = false;
        var swapchainTextureAcquired = false;
        RuntimeFrameSnapshot? screenshot = null;
        try
        {
            var acquireStartTimestamp = Stopwatch.GetTimestamp();
            if (!gpuApi.WaitAndAcquireSwapchainTexture(
                frame.CommandBuffer.Value,
                window,
                out var swapchainTexture,
                out var swapchainWidth,
                out var swapchainHeight))
            {
                throw new GpuPresenterUnavailableException("Runtime GPU swapchain acquisition failed: " + gpuApi.GetError());
            }

            presentTicks += Stopwatch.GetTimestamp() - acquireStartTimestamp;
            swapchainTextureAcquired = true;
            if (swapchainTexture == IntPtr.Zero)
            {
                terminalPathStarted = true;
                backend.EndFrame(frame);
                frameSubmitted = true;
                SetFrameTiming(frameStartTimestamp, presentTicks);
                return new RuntimePresentedFrame(CreateDiagnostics(renderPlan, capturePresenterManagedBytesAllocated: 0, actualDrawCalls: 0), null);
            }

            EnsurePresentationResources();
            var targetSize = new Vector2I(checked((int)swapchainWidth), checked((int)swapchainHeight));
            BuildFrameGeometry(renderPlan, targetSize);
            UploadPendingResources(frame.CommandBuffer);
            RenderTarget(
                frame.CommandBuffer,
                swapchainTexture,
                clearColor,
                solidPipeline,
                texturedPipeline,
                cycle: false);

            SdlGpuFenceHandle fence = default;
            if (captureFrame)
            {
                EnsureScreenshotResources(targetSize);
                RenderTarget(
                    frame.CommandBuffer,
                    screenshotTexture,
                    clearColor,
                    screenshotSolidPipeline,
                    screenshotTexturedPipeline,
                    cycle: true);
                DownloadScreenshotTexture(frame.CommandBuffer, targetSize);
                terminalPathStarted = true;
                fence = backend.EndFrameAndAcquireFence(frame);
                frameSubmitted = true;
                CommitStagedTextureUploads();
                var screenshotWaitStartTimestamp = Stopwatch.GetTimestamp();
                screenshot = ReadScreenshotAfterFence(fence, targetSize);
                presentTicks += Stopwatch.GetTimestamp() - screenshotWaitStartTimestamp;
            }
            else
            {
                terminalPathStarted = true;
                backend.EndFrame(frame);
                frameSubmitted = true;
                CommitStagedTextureUploads();
            }

        }
        catch
        {
            try
            {
                RuntimeGpuCommandBufferFinalizer.CompleteAfterFailure(
                    frame,
                    swapchainTextureAcquired,
                    terminalPathStarted || frameSubmitted,
                    backend.EndFrame,
                    commandBuffer => gpuApi.CancelCommandBuffer(commandBuffer.Value));
            }
            catch
            {
            }
            finally
            {
                ReleasePendingTextureUploads(releaseTextures: false);
                RollbackStagedTextureUploads();
            }

            throw;
        }
        finally
        {
            ReleaseSubmittedTransferBuffers();
        }

        SetFrameTiming(frameStartTimestamp, presentTicks);
        return new RuntimePresentedFrame(
            CreateDiagnostics(
                renderPlan,
                screenshot?.RgbaPixels.Length ?? 0,
                drawBatches.Count * (captureFrame ? 2 : 1)),
            screenshot);
    }

    private void SetFrameTiming(long frameStartTimestamp, long presentTicks)
    {
        var totalSeconds = Stopwatch.GetElapsedTime(frameStartTimestamp, Stopwatch.GetTimestamp()).TotalSeconds;
        var presentSeconds = Stopwatch.GetElapsedTime(0, presentTicks).TotalSeconds;
        LastPresentTimeSeconds = Math.Max(0d, presentSeconds);
        LastSubmitTimeSeconds = Math.Max(0d, totalSeconds - LastPresentTimeSeconds);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        var device = backend.Device.Value;
        ReleasePendingTextureUploads(releaseTextures: false);
        RollbackStagedTextureUploads();
        foreach (var texture in textureCache.Values)
        {
            if (device != IntPtr.Zero && texture.Handle != IntPtr.Zero)
            {
                gpuApi.ReleaseTexture(device, texture.Handle);
            }
        }

        textureCache.Clear();
        stagedTextureCache.Clear();
        ReleaseSubmittedTransferBuffers();
        ReleaseGpuHandle(device, sampler, gpuApi.ReleaseSampler);
        ReleaseGpuHandle(device, solidPipeline, gpuApi.ReleaseGraphicsPipeline);
        ReleaseGpuHandle(device, texturedPipeline, gpuApi.ReleaseGraphicsPipeline);
        ReleaseGpuHandle(device, screenshotSolidPipeline, gpuApi.ReleaseGraphicsPipeline);
        ReleaseGpuHandle(device, screenshotTexturedPipeline, gpuApi.ReleaseGraphicsPipeline);
        ReleaseGpuHandle(device, screenshotTexture, gpuApi.ReleaseTexture);
        ReleaseGpuHandle(device, screenshotTransferBuffer, gpuApi.ReleaseTransferBuffer);
        ReleaseGpuHandle(device, vertexBuffer, gpuApi.ReleaseBuffer);
        ReleaseGpuHandle(device, vertexTransferBuffer, gpuApi.ReleaseTransferBuffer);

        if (windowClaimed && device != IntPtr.Zero)
        {
            gpuApi.ReleaseWindowFromDevice(device, window);
        }

        backend.Shutdown();
        shaderCrossLease?.Dispose();
        shaderCrossLease = null;
    }

    private void CreatePresentationResources()
    {
        var device = backend.Device.Value;
        var swapchainFormat = gpuApi.GetSwapchainTextureFormat(device, window);
        using var vertexShader = CompileGraphicsShader(VertexShaderSource, "VSMain", ShaderCross.ShaderStage.Vertex);
        using var solidFragmentShader = CompileGraphicsShader(SolidFragmentShaderSource, "PSMain", ShaderCross.ShaderStage.Fragment);
        using var texturedFragmentShader = CompileGraphicsShader(TexturedFragmentShaderSource, "PSMain", ShaderCross.ShaderStage.Fragment);

        solidPipeline = CreatePipeline(vertexShader, solidFragmentShader, swapchainFormat, "solid");
        texturedPipeline = CreatePipeline(vertexShader, texturedFragmentShader, swapchainFormat, "textured");
        screenshotSolidPipeline = CreatePipeline(vertexShader, solidFragmentShader, ScreenshotTextureFormat, "screenshot solid");
        screenshotTexturedPipeline = CreatePipeline(vertexShader, texturedFragmentShader, ScreenshotTextureFormat, "screenshot textured");
        sampler = CreateSampler();
        CreateVertexResources(InitialVertexCapacity);
        presentationResourcesCreated++;
    }

    private void EnsurePresentationResources()
    {
        if (solidPipeline != IntPtr.Zero)
        {
            return;
        }

        try
        {
            gpuApi.BeforePresentationResources();
            CreatePresentationResources();
        }
        catch (GpuPresenterUnavailableException)
        {
            throw;
        }
        catch (InvalidOperationException exception)
        {
            throw new GpuPresenterUnavailableException(
                "Runtime GPU presentation resources are unavailable: " + exception.Message,
                exception);
        }
    }

    private IntPtr CreatePipeline(
        RuntimeGpuShader vertexShader,
        RuntimeGpuShader fragmentShader,
        SDL.GPUTextureFormat targetFormat,
        string name)
    {
        var vertexBufferDescriptions = AllocateStructArray(new[]
        {
            new SDL.GPUVertexBufferDescription
            {
                Slot = 0,
                Pitch = BytesPerVertex,
                InputRate = SDL.GPUVertexInputRate.Vertex,
                InstanceStepRate = 0
            }
        });
        var vertexAttributes = AllocateStructArray(new[]
        {
            new SDL.GPUVertexAttribute
            {
                Location = 0,
                BufferSlot = 0,
                Format = SDL.GPUVertexElementFormat.Float2,
                Offset = 0
            },
            new SDL.GPUVertexAttribute
            {
                Location = 1,
                BufferSlot = 0,
                Format = SDL.GPUVertexElementFormat.Float2,
                Offset = 2 * BytesPerFloat
            },
            new SDL.GPUVertexAttribute
            {
                Location = 2,
                BufferSlot = 0,
                Format = SDL.GPUVertexElementFormat.Float4,
                Offset = 4 * BytesPerFloat
            }
        });
        var colorTargetDescriptions = AllocateStructArray(new[]
        {
            new SDL.GPUColorTargetDescription
            {
                Format = targetFormat,
                BlendState = new SDL.GPUColorTargetBlendState
                {
                    SrcColorBlendFactor = SDL.GPUBlendFactor.SrcAlpha,
                    DstColorBlendFactor = SDL.GPUBlendFactor.OneMinusSrcAlpha,
                    ColorBlendOp = SDL.GPUBlendOp.Add,
                    SrcAlphaBlendFactor = SDL.GPUBlendFactor.One,
                    DstAlphaBlendFactor = SDL.GPUBlendFactor.OneMinusSrcAlpha,
                    AlphaBlendOp = SDL.GPUBlendOp.Add,
                    ColorWriteMask = SDL.GPUColorComponentFlags.R
                        | SDL.GPUColorComponentFlags.G
                        | SDL.GPUColorComponentFlags.B
                        | SDL.GPUColorComponentFlags.A,
                    EnableBlend = true
                }
            }
        });

        try
        {
            var createInfo = new SDL.GPUGraphicsPipelineCreateInfo
            {
                VertexShader = vertexShader.Handle,
                FragmentShader = fragmentShader.Handle,
                VertexInputState = new SDL.GPUVertexInputState
                {
                    VertexBufferDescriptions = vertexBufferDescriptions,
                    NumVertexBuffers = 1,
                    VertexAttributes = vertexAttributes,
                    NumVertexAttributes = 3
                },
                PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
                RasterizerState = new SDL.GPURasterizerState
                {
                    FillMode = SDL.GPUFillMode.Fill,
                    CullMode = SDL.GPUCullMode.None,
                    FrontFace = SDL.GPUFrontFace.CounterClockwise,
                    EnableDepthClip = true
                },
                MultisampleState = new SDL.GPUMultisampleState
                {
                    SampleCount = SDL.GPUSampleCount.SampleCount1,
                    SampleMask = uint.MaxValue
                },
                DepthStencilState = new SDL.GPUDepthStencilState
                {
                    CompareOp = SDL.GPUCompareOp.Always,
                    BackStencilState = CreateDisabledStencilState(),
                    FrontStencilState = CreateDisabledStencilState(),
                    CompareMask = byte.MaxValue,
                    WriteMask = byte.MaxValue,
                    EnableDepthTest = false,
                    EnableDepthWrite = false,
                    EnableStencilTest = false
                },
                TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions = colorTargetDescriptions,
                    NumColorTargets = 1,
                    HasDepthStencilTarget = false
                }
            };

            var pipeline = gpuApi.CreateGraphicsPipeline(backend.Device.Value, in createInfo);
            if (pipeline == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Runtime GPU graphics pipeline creation failed for {name} targetFormat={targetFormat} " +
                    $"vertexFormat={vertexShader.Format} fragmentFormat={fragmentShader.Format} " +
                    $"vertexInputs=[{vertexShader.InputSummary}] vertexOutputs=[{vertexShader.OutputSummary}] " +
                    $"fragmentInputs=[{fragmentShader.InputSummary}] fragmentOutputs=[{fragmentShader.OutputSummary}]: " +
                    gpuApi.GetError());
            }

            return pipeline;
        }
        finally
        {
            Marshal.FreeHGlobal(vertexBufferDescriptions);
            Marshal.FreeHGlobal(vertexAttributes);
            Marshal.FreeHGlobal(colorTargetDescriptions);
        }
    }

    private static SDL.GPUStencilOpState CreateDisabledStencilState()
    {
        return new SDL.GPUStencilOpState
        {
            FailOp = SDL.GPUStencilOp.Keep,
            PassOp = SDL.GPUStencilOp.Keep,
            DepthFailOp = SDL.GPUStencilOp.Keep,
            CompareOp = SDL.GPUCompareOp.Always
        };
    }

    private IntPtr CreateSampler()
    {
        var createInfo = new SDL.GPUSamplerCreateInfo
        {
            MinFilter = SDL.GPUFilter.Nearest,
            MagFilter = SDL.GPUFilter.Nearest,
            MipmapMode = SDL.GPUSamplerMipmapMode.Nearest,
            AddressModeU = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeW = SDL.GPUSamplerAddressMode.ClampToEdge,
            MinLod = 0f,
            MaxLod = 1f
        };
        var result = gpuApi.CreateSampler(backend.Device.Value, in createInfo);
        if (result == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU sampler creation failed: " + gpuApi.GetError());
        }

        return result;
    }

    private void CreateVertexResources(int capacity)
    {
        var device = backend.Device.Value;
        var byteSize = checked((uint)(capacity * BytesPerVertex));
        var bufferInfo = new SDL.GPUBufferCreateInfo
        {
            Usage = SDL.GPUBufferUsageFlags.Vertex,
            Size = byteSize
        };
        vertexBuffer = gpuApi.CreateBuffer(device, in bufferInfo);
        if (vertexBuffer == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU vertex buffer creation failed: " + gpuApi.GetError());
        }

        var transferInfo = new SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL.GPUTransferBufferUsage.Upload,
            Size = byteSize
        };
        vertexTransferBuffer = gpuApi.CreateTransferBuffer(device, in transferInfo);
        if (vertexTransferBuffer == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU vertex transfer buffer creation failed: " + gpuApi.GetError());
        }

        vertexCapacity = capacity;
    }

    private RuntimeGpuShader CompileGraphicsShader(string source, string entryPoint, ShaderCross.ShaderStage stage)
    {
        var info = new ShaderCross.HLSLInfo
        {
            ManagedSource = source,
            ManagedEntrypoint = entryPoint,
            ManagedIncludeDir = null,
            Defines = IntPtr.Zero,
            ShaderStage = stage,
            Props = 0
        };

        var spirv = ShaderCross.CompileSPIRVFromHLSL(ref info, out var spirvSize);
        if (spirv == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU shader SPIR-V compilation failed: " + SDL.GetError());
        }

        var metadataPointer = IntPtr.Zero;
        var code = IntPtr.Zero;
        try
        {
            metadataPointer = ShaderCross.ReflectGraphicsSPIRV(spirv, spirvSize, props: 0);
            if (metadataPointer == IntPtr.Zero)
            {
                throw new GpuPresenterUnavailableException("Runtime GPU shader reflection failed: " + SDL.GetError());
            }

            var metadata = Marshal.PtrToStructure<ShaderCross.GraphicsShaderMetadata>(metadataPointer);
            var shaderFormat = SelectShaderFormat(backend.Device.Value);
            var codeSize = spirvSize;
            if (shaderFormat == SDL.GPUShaderFormat.SPIRV)
            {
                code = spirv;
            }
            else if (shaderFormat == SDL.GPUShaderFormat.DXIL)
            {
                code = ShaderCross.CompileDXILFromHLSL(in info, out codeSize);
            }
            else if (shaderFormat == SDL.GPUShaderFormat.DXBC)
            {
                code = ShaderCross.CompileDXBCFromHLSL(in info, out codeSize);
            }
            else if (shaderFormat == SDL.GPUShaderFormat.MSL)
            {
                var spirvInfo = new ShaderCross.SPIRVInfo
                {
                    ByteCode = spirv,
                    ByteCodeSize = spirvSize,
                    ManagedEntrypoint = entryPoint,
                    ShaderStage = stage,
                    Props = 0
                };
                code = ShaderCross.TranspileMSLFromSPIRV(in spirvInfo);
                codeSize = code == IntPtr.Zero ? UIntPtr.Zero : GetNullTerminatedByteLength(code);
            }
            else
            {
                throw new GpuPresenterUnavailableException("Runtime GPU shader format selection failed.");
            }

            if (code == IntPtr.Zero || codeSize == UIntPtr.Zero)
            {
                throw new GpuPresenterUnavailableException("Runtime GPU shader bytecode compilation failed: " + SDL.GetError());
            }

            var createInfo = new SDL.GPUShaderCreateInfo
            {
                Code = code,
                CodeSize = codeSize,
                Entrypoint = entryPoint,
                Format = shaderFormat,
                Stage = ToGpuShaderStage(stage),
                NumSamplers = metadata.ResourceInfo.NumSamplers,
                NumStorageTextures = metadata.ResourceInfo.NumStorageTextures,
                NumStorageBuffers = metadata.ResourceInfo.NumStorageBuffers,
                NumUniformBuffers = metadata.ResourceInfo.NumUniformBuffers
            };
            var shader = SDL.CreateGPUShader(backend.Device.Value, in createInfo);
            if (shader == IntPtr.Zero)
            {
                throw new GpuPresenterUnavailableException("Runtime GPU shader creation failed: " + SDL.GetError());
            }

            return new RuntimeGpuShader(
                backend.Device.Value,
                shader,
                shaderFormat,
                FormatIoSummary(metadata.Inputs, metadata.NumInputs),
                FormatIoSummary(metadata.Outputs, metadata.NumOutputs));
        }
        finally
        {
            if (metadataPointer != IntPtr.Zero)
            {
                SDL.Free(metadataPointer);
            }

            if (code != IntPtr.Zero && code != spirv)
            {
                SDL.Free(code);
            }

            SDL.Free(spirv);
        }
    }

    private static SDL.GPUShaderFormat SelectShaderFormat(IntPtr device)
    {
        var formats = SDL.GetGPUShaderFormats(device);
        if ((formats & SDL.GPUShaderFormat.DXBC) != 0)
        {
            return SDL.GPUShaderFormat.DXBC;
        }

        if ((formats & SDL.GPUShaderFormat.DXIL) != 0)
        {
            return SDL.GPUShaderFormat.DXIL;
        }

        if ((formats & SDL.GPUShaderFormat.SPIRV) != 0)
        {
            return SDL.GPUShaderFormat.SPIRV;
        }

        if ((formats & SDL.GPUShaderFormat.MSL) != 0)
        {
            return SDL.GPUShaderFormat.MSL;
        }

        return SDL.GPUShaderFormat.Invalid;
    }

    private static SDL.GPUShaderStage ToGpuShaderStage(ShaderCross.ShaderStage stage)
    {
        return stage == ShaderCross.ShaderStage.Fragment
            ? SDL.GPUShaderStage.Fragment
            : SDL.GPUShaderStage.Vertex;
    }

    private void BuildFrameGeometry(CanvasItemRenderPlan renderPlan, Vector2I targetSize)
    {
        vertexCount = 0;
        drawBatches.Clear();
        ReleasePendingTextureUploads(releaseTextures: false);
        RollbackStagedTextureUploads();

        for (var index = 0; index < renderPlan.Batches.Count; index++)
        {
            var batch = renderPlan.Batches[index];
            BuildRenderBatch(renderPlan.Commands, batch, targetSize);
        }
    }

    private void BuildRenderBatch(
        IReadOnlyList<CanvasItemRenderCommand> commands,
        CanvasItemRenderBatch batch,
        Vector2I targetSize)
    {
        var firstVertex = vertexCount;
        var pipelineKind = RuntimePipelineKind.Solid;
        RuntimeTextureResource? textureResource = null;
        for (var index = batch.StartIndex; index < batch.StartIndex + batch.Count; index++)
        {
            var command = commands[index];
            if (command.Kind == CanvasItemRenderCommandKind.Texture)
            {
                if (command.Texture is null || !TryGetTextureResource(command.Texture, out var resource))
                {
                    throw new UnsupportedTextureResourceException(command.Texture);
                }

                pipelineKind = RuntimePipelineKind.Textured;
                textureResource = resource;
                AppendTexture(command, resource, targetSize);
                continue;
            }

            AppendSolid(command, targetSize);
        }

        var count = vertexCount - firstVertex;
        if (count > 0)
        {
            drawBatches.Add(new RuntimeDrawBatch(firstVertex, count, pipelineKind, textureResource));
        }
    }

    private void AppendSolid(CanvasItemRenderCommand command, Vector2I targetSize)
    {
        switch (command.Kind)
        {
            case CanvasItemRenderCommandKind.Line:
                AppendLine(command, targetSize);
                break;
            case CanvasItemRenderCommandKind.Rect:
                AppendRect(command, targetSize);
                break;
            case CanvasItemRenderCommandKind.Circle:
                AppendCircle(command, targetSize);
                break;
            case CanvasItemRenderCommandKind.Polygon:
                AppendPolygon(command, targetSize);
                break;
            case CanvasItemRenderCommandKind.String:
                AppendStringFallback(command, targetSize);
                break;
            case CanvasItemRenderCommandKind.Texture:
                throw new InvalidOperationException("Texture render commands must be handled by the textured presenter path.");
            default:
                throw new InvalidOperationException("Unsupported canvas item render command kind: " + command.Kind + ".");
        }
    }

    private void AppendTexture(CanvasItemRenderCommand command, RuntimeTextureResource texture, Vector2I targetSize)
    {
        var source = NormalizeSourceRect(command.SourceRect, texture.Width, texture.Height);
        var u0 = source.Position.X / texture.Width;
        var v0 = source.Position.Y / texture.Height;
        var u1 = source.End.X / texture.Width;
        var v1 = source.End.Y / texture.Height;
        if (command.FlipH)
        {
            (u0, u1) = (u1, u0);
        }

        if (command.FlipV)
        {
            (v0, v1) = (v1, v0);
        }

        AppendQuad(command.Transform, command.DestinationRect, command.EffectiveModulate, targetSize, u0, v0, u1, v1);
    }

    private void AppendLine(CanvasItemRenderCommand command, Vector2I targetSize)
    {
        if (command.Points.Count < 2)
        {
            return;
        }

        var from = Transform(command, command.Points[0]);
        var to = Transform(command, command.Points[1]);
        var direction = to - from;
        var length = direction.Length();
        if (length <= 0f)
        {
            return;
        }

        var halfWidth = Math.Max(1f, command.Width <= 0f ? 1f : command.Width) * 0.5f;
        var normal = new Vector2(-direction.Y / length, direction.X / length) * halfWidth;
        AppendTriangle(from - normal, to - normal, to + normal, command.EffectiveModulate, targetSize);
        AppendTriangle(from - normal, to + normal, from + normal, command.EffectiveModulate, targetSize);
    }

    private void AppendRect(CanvasItemRenderCommand command, Vector2I targetSize)
    {
        var rect = command.DestinationRect;
        if (command.Filled)
        {
            AppendQuad(command.Transform, rect, command.EffectiveModulate, targetSize);
            return;
        }

        var topLeft = command.Transform.Xform(rect.Position);
        var topRight = command.Transform.Xform(new Vector2(rect.End.X, rect.Position.Y));
        var bottomRight = command.Transform.Xform(rect.End);
        var bottomLeft = command.Transform.Xform(new Vector2(rect.Position.X, rect.End.Y));
        AppendLine(topLeft, topRight, command.EffectiveModulate, targetSize);
        AppendLine(topRight, bottomRight, command.EffectiveModulate, targetSize);
        AppendLine(bottomRight, bottomLeft, command.EffectiveModulate, targetSize);
        AppendLine(bottomLeft, topLeft, command.EffectiveModulate, targetSize);
    }

    private void AppendCircle(CanvasItemRenderCommand command, Vector2I targetSize)
    {
        var center = Transform(command, command.Position);
        var radius = Math.Max(1f, command.Radius);
        var previous = Transform(command, command.Position + new Vector2(radius, 0f));
        for (var segment = 1; segment <= CircleSegmentCount; segment++)
        {
            var angle = MathF.Tau * segment / CircleSegmentCount;
            var next = Transform(command, command.Position + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius));
            AppendTriangle(center, previous, next, command.EffectiveModulate, targetSize);
            previous = next;
        }
    }

    private void AppendPolygon(CanvasItemRenderCommand command, Vector2I targetSize)
    {
        if (command.Points.Count < 3)
        {
            return;
        }

        var origin = Transform(command, command.Points[0]);
        var originColor = GetPolygonVertexColor(command, 0);
        for (var index = 1; index < command.Points.Count - 1; index++)
        {
            AppendTriangle(
                origin,
                Transform(command, command.Points[index]),
                Transform(command, command.Points[index + 1]),
                originColor,
                GetPolygonVertexColor(command, index),
                GetPolygonVertexColor(command, index + 1),
                targetSize);
        }
    }

    private void AppendStringFallback(CanvasItemRenderCommand command, Vector2I targetSize)
    {
        if (string.IsNullOrEmpty(command.Text))
        {
            return;
        }

        var scale = Math.Max(1, (int)MathF.Round(command.FontSize / 8f, MidpointRounding.AwayFromZero));
        var x = command.Position.X;
        var y = command.Position.Y - (RuntimePixelFont.GlyphHeight * scale);
        for (var charIndex = 0; charIndex < command.Text.Length; charIndex++)
        {
            var glyph = RuntimePixelFont.GetGlyph(char.ToUpperInvariant(command.Text[charIndex]));
            for (var row = 0; row < RuntimePixelFont.GlyphHeight; row++)
            {
                var glyphRow = glyph[row];
                for (var column = 0; column < RuntimePixelFont.GlyphWidth; column++)
                {
                    if (glyphRow[column] != '1')
                    {
                        continue;
                    }

                    AppendQuad(
                        command.Transform,
                        new Rect2(x + (column * scale), y + (row * scale), scale, scale),
                        command.EffectiveModulate,
                        targetSize);
                }
            }

            x += (RuntimePixelFont.GlyphWidth + 1) * scale;
        }
    }

    private void AppendLine(Vector2 from, Vector2 to, Color color, Vector2I targetSize)
    {
        var direction = to - from;
        var length = direction.Length();
        if (length <= 0f)
        {
            return;
        }

        var normal = new Vector2(-direction.Y / length, direction.X / length) * 0.5f;
        AppendTriangle(from - normal, to - normal, to + normal, color, targetSize);
        AppendTriangle(from - normal, to + normal, from + normal, color, targetSize);
    }

    private void AppendQuad(
        Transform2D transform,
        Rect2 rect,
        Color color,
        Vector2I targetSize,
        float u0 = 0f,
        float v0 = 0f,
        float u1 = 0f,
        float v1 = 0f)
    {
        var x0 = rect.Position.X;
        var y0 = rect.Position.Y;
        var x1 = rect.Position.X + Math.Max(1f, rect.Size.X);
        var y1 = rect.Position.Y + Math.Max(1f, rect.Size.Y);
        var topLeft = transform.Xform(new Vector2(x0, y0));
        var topRight = transform.Xform(new Vector2(x1, y0));
        var bottomRight = transform.Xform(new Vector2(x1, y1));
        var bottomLeft = transform.Xform(new Vector2(x0, y1));

        AppendVertex(topLeft, u0, v0, color, targetSize);
        AppendVertex(topRight, u1, v0, color, targetSize);
        AppendVertex(bottomRight, u1, v1, color, targetSize);
        AppendVertex(topLeft, u0, v0, color, targetSize);
        AppendVertex(bottomRight, u1, v1, color, targetSize);
        AppendVertex(bottomLeft, u0, v1, color, targetSize);
    }

    private void AppendQuad(
        Rect2 rect,
        Color color,
        Vector2I targetSize,
        float u0 = 0f,
        float v0 = 0f,
        float u1 = 0f,
        float v1 = 0f)
    {
        AppendQuad(Transform2D.Identity, rect, color, targetSize, u0, v0, u1, v1);
    }

    private void AppendTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, Vector2I targetSize)
    {
        AppendTriangle(a, b, c, color, color, color, targetSize);
    }

    private void AppendTriangle(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Color colorA,
        Color colorB,
        Color colorC,
        Vector2I targetSize)
    {
        AppendVertex(a, u: 0f, v: 0f, colorA, targetSize);
        AppendVertex(b, u: 0f, v: 0f, colorB, targetSize);
        AppendVertex(c, u: 0f, v: 0f, colorC, targetSize);
    }

    private void AppendVertex(Vector2 position, float u, float v, Color color, Vector2I targetSize)
    {
        EnsureVertexScratchCapacity(vertexCount + 1);
        var offset = vertexCount * FloatsPerVertex;
        vertexScratch[offset] = ((position.X / targetSize.X) * 2f) - 1f;
        vertexScratch[offset + 1] = 1f - ((position.Y / targetSize.Y) * 2f);
        vertexScratch[offset + 2] = u;
        vertexScratch[offset + 3] = v;
        vertexScratch[offset + 4] = Math.Clamp(color.R, 0f, 1f);
        vertexScratch[offset + 5] = Math.Clamp(color.G, 0f, 1f);
        vertexScratch[offset + 6] = Math.Clamp(color.B, 0f, 1f);
        vertexScratch[offset + 7] = Math.Clamp(color.A, 0f, 1f);
        vertexCount++;
    }

    private void UploadPendingResources(SdlGpuCommandBufferHandle commandBuffer)
    {
        if (vertexCount == 0 && pendingTextureUploads.Count == 0)
        {
            return;
        }

        if (vertexCount > 0)
        {
            EnsureGpuVertexCapacity(vertexCount);
            var mapped = gpuApi.MapTransferBuffer(backend.Device.Value, vertexTransferBuffer, cycle: true);
            if (mapped == IntPtr.Zero)
            {
                throw new GpuPresenterUnavailableException("Runtime GPU vertex transfer mapping failed: " + gpuApi.GetError());
            }

            Marshal.Copy(vertexScratch, 0, mapped, vertexCount * FloatsPerVertex);
            gpuApi.UnmapTransferBuffer(backend.Device.Value, vertexTransferBuffer);
        }

        var copyPass = gpuApi.BeginCopyPass(commandBuffer.Value);
        if (copyPass == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU copy pass creation failed: " + gpuApi.GetError());
        }

        try
        {
            if (vertexCount > 0)
            {
                var source = new SDL.GPUTransferBufferLocation
                {
                    TransferBuffer = vertexTransferBuffer,
                    Offset = 0
                };
                var destination = new SDL.GPUBufferRegion
                {
                    Buffer = vertexBuffer,
                    Offset = 0,
                    Size = checked((uint)(vertexCount * BytesPerVertex))
                };
                gpuApi.UploadToBuffer(copyPass, in source, in destination, cycle: true);
            }

            for (var index = 0; index < pendingTextureUploads.Count; index++)
            {
                var upload = pendingTextureUploads[index];
                var source = new SDL.GPUTextureTransferInfo
                {
                    TransferBuffer = upload.TransferBuffer,
                    Offset = 0,
                    PixelsPerRow = checked((uint)upload.Width),
                    RowsPerLayer = checked((uint)upload.Height)
                };
                var destination = new SDL.GPUTextureRegion
                {
                    Texture = upload.Texture,
                    MipLevel = 0,
                    Layer = 0,
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = checked((uint)upload.Width),
                    H = checked((uint)upload.Height),
                    D = 1
                };
                gpuApi.UploadToTexture(copyPass, in source, in destination, cycle: false);
                submittedTransferBuffers.Add(upload.TransferBuffer);
            }
        }
        finally
        {
            gpuApi.EndCopyPass(copyPass);
            ReleasePendingTextureUploads(releaseTextures: false);
        }
    }

    private void RenderTarget(
        SdlGpuCommandBufferHandle commandBuffer,
        IntPtr targetTexture,
        Color clearColor,
        IntPtr solidTargetPipeline,
        IntPtr texturedTargetPipeline,
        bool cycle)
    {
        colorTargets[0] = new SDL.GPUColorTargetInfo
        {
            Texture = targetTexture,
            MipLevel = 0,
            LayerOrDepthPlane = 0,
            ClearColor = new SDL.FColor
            {
                R = Math.Clamp(clearColor.R, 0f, 1f),
                G = Math.Clamp(clearColor.G, 0f, 1f),
                B = Math.Clamp(clearColor.B, 0f, 1f),
                A = Math.Clamp(clearColor.A, 0f, 1f)
            },
            LoadOp = SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.Store,
            Cycle = cycle
        };

        var renderPass = gpuApi.BeginRenderPass(commandBuffer.Value, colorTargets);
        if (renderPass == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU render pass creation failed: " + gpuApi.GetError());
        }

        try
        {
            if (vertexCount == 0)
            {
                return;
            }

            vertexBindings[0] = new SDL.GPUBufferBinding
            {
                Buffer = vertexBuffer,
                Offset = 0
            };
            gpuApi.BindVertexBuffers(renderPass, firstSlot: 0, vertexBindings);

            for (var index = 0; index < drawBatches.Count; index++)
            {
                var batch = drawBatches[index];
                if (batch.Kind == RuntimePipelineKind.Textured && batch.Texture.HasValue)
                {
                    gpuApi.BindGraphicsPipeline(renderPass, texturedTargetPipeline);
                    textureBindings[0] = new SDL.GPUTextureSamplerBinding
                    {
                        Texture = batch.Texture.Value.Handle,
                        Sampler = sampler
                    };
                    gpuApi.BindFragmentSamplers(renderPass, firstSlot: 0, textureBindings);
                }
                else
                {
                    gpuApi.BindGraphicsPipeline(renderPass, solidTargetPipeline);
                }

                gpuApi.DrawPrimitives(
                    renderPass,
                    checked((uint)batch.VertexCount),
                    numInstances: 1,
                    checked((uint)batch.FirstVertex),
                    firstInstance: 0);
            }
        }
        finally
        {
            gpuApi.EndRenderPass(renderPass);
        }
    }

    private void EnsureScreenshotResources(Vector2I targetSize)
    {
        if (screenshotTexture != IntPtr.Zero && screenshotTextureSize == targetSize)
        {
            return;
        }

        var device = backend.Device.Value;
        if (screenshotTexture != IntPtr.Zero || screenshotTransferBuffer != IntPtr.Zero)
        {
            screenshotResourcesRecreated++;
        }

        ReleaseGpuHandle(device, screenshotTexture, gpuApi.ReleaseTexture);
        ReleaseGpuHandle(device, screenshotTransferBuffer, gpuApi.ReleaseTransferBuffer);
        screenshotTexture = IntPtr.Zero;
        screenshotTransferBuffer = IntPtr.Zero;

        var textureInfo = new SDL.GPUTextureCreateInfo
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = ScreenshotTextureFormat,
            Usage = SDL.GPUTextureUsageFlags.ColorTarget,
            Width = checked((uint)targetSize.X),
            Height = checked((uint)targetSize.Y),
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL.GPUSampleCount.SampleCount1
        };
        screenshotTexture = gpuApi.CreateTexture(device, in textureInfo);
        if (screenshotTexture == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU screenshot texture creation failed: " + gpuApi.GetError());
        }

        var transferInfo = new SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL.GPUTransferBufferUsage.Download,
            Size = checked((uint)(targetSize.X * targetSize.Y * 4))
        };
        screenshotTransferBuffer = gpuApi.CreateTransferBuffer(device, in transferInfo);
        if (screenshotTransferBuffer == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU screenshot transfer buffer creation failed: " + gpuApi.GetError());
        }

        screenshotTextureSize = targetSize;
    }

    private void DownloadScreenshotTexture(SdlGpuCommandBufferHandle commandBuffer, Vector2I targetSize)
    {
        var copyPass = gpuApi.BeginCopyPass(commandBuffer.Value);
        if (copyPass == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU screenshot copy pass creation failed: " + gpuApi.GetError());
        }

        try
        {
            var source = new SDL.GPUTextureRegion
            {
                Texture = screenshotTexture,
                MipLevel = 0,
                Layer = 0,
                X = 0,
                Y = 0,
                Z = 0,
                W = checked((uint)targetSize.X),
                H = checked((uint)targetSize.Y),
                D = 1
            };
            var destination = new SDL.GPUTextureTransferInfo
            {
                TransferBuffer = screenshotTransferBuffer,
                Offset = 0,
                PixelsPerRow = checked((uint)targetSize.X),
                RowsPerLayer = checked((uint)targetSize.Y)
            };
            gpuApi.DownloadFromTexture(copyPass, in source, in destination);
        }
        finally
        {
            gpuApi.EndCopyPass(copyPass);
        }
    }

    private RuntimeFrameSnapshot ReadScreenshotAfterFence(SdlGpuFenceHandle fence, Vector2I targetSize)
    {
        try
        {
            backend.WaitForFence(fence);
            var mapped = gpuApi.MapTransferBuffer(backend.Device.Value, screenshotTransferBuffer, cycle: false);
            if (mapped == IntPtr.Zero)
            {
                throw new GpuPresenterUnavailableException("Runtime GPU screenshot transfer mapping failed: " + gpuApi.GetError());
            }

            try
            {
                var pixels = new byte[checked(targetSize.X * targetSize.Y * 4)];
                Marshal.Copy(mapped, pixels, 0, pixels.Length);
                return new RuntimeFrameSnapshot(targetSize.X, targetSize.Y, pixels);
            }
            finally
            {
                gpuApi.UnmapTransferBuffer(backend.Device.Value, screenshotTransferBuffer);
            }
        }
        finally
        {
            backend.ReleaseFence(fence);
        }
    }

    private bool TryGetTextureResource(Texture2D texture, out RuntimeTextureResource resource)
    {
        var contentVersion = texture.RenderContentVersion;
        if (textureCache.TryGetValue(texture, out resource))
        {
            if (resource.ContentVersion == contentVersion)
            {
                pendingTextureCacheHits++;
                return true;
            }

            gpuApi.ReleaseTexture(backend.Device.Value, resource.Handle);
            textureCache.Remove(texture);
        }

        if (stagedTextureCache.TryGetValue(texture, out resource))
        {
            if (resource.ContentVersion == contentVersion)
            {
                return true;
            }

            gpuApi.ReleaseTexture(backend.Device.Value, resource.Handle);
            stagedTextureCache.Remove(texture);
        }

        if (!RuntimeTextureResolver.TryCreateTexturePixels(texture, out var width, out var height, out var pixels))
        {
            resource = default;
            return false;
        }

        var device = backend.Device.Value;
        var textureInfo = new SDL.GPUTextureCreateInfo
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
            Usage = SDL.GPUTextureUsageFlags.Sampler,
            Width = checked((uint)width),
            Height = checked((uint)height),
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL.GPUSampleCount.SampleCount1
        };
        var handle = gpuApi.CreateTexture(device, in textureInfo);
        if (handle == IntPtr.Zero)
        {
            throw new GpuPresenterUnavailableException("Runtime GPU texture creation failed: " + gpuApi.GetError());
        }

        var transferInfo = new SDL.GPUTransferBufferCreateInfo
        {
            Usage = SDL.GPUTransferBufferUsage.Upload,
            Size = checked((uint)pixels.Length)
        };
        var transferBuffer = gpuApi.CreateTransferBuffer(device, in transferInfo);
        if (transferBuffer == IntPtr.Zero)
        {
            gpuApi.ReleaseTexture(device, handle);
            throw new GpuPresenterUnavailableException("Runtime GPU texture transfer buffer creation failed: " + gpuApi.GetError());
        }

        var mapped = gpuApi.MapTransferBuffer(device, transferBuffer, cycle: true);
        if (mapped == IntPtr.Zero)
        {
            gpuApi.ReleaseTransferBuffer(device, transferBuffer);
            gpuApi.ReleaseTexture(device, handle);
            throw new GpuPresenterUnavailableException("Runtime GPU texture transfer mapping failed: " + gpuApi.GetError());
        }

        Marshal.Copy(pixels, 0, mapped, pixels.Length);
        gpuApi.UnmapTransferBuffer(device, transferBuffer);

        resource = new RuntimeTextureResource(handle, width, height, contentVersion);
        stagedTextureCache.Add(texture, resource);
        pendingTextureUploads.Add(new PendingTextureUpload(texture, transferBuffer, handle, width, height));
        return true;
    }

    private static Rect2 NormalizeSourceRect(Rect2 sourceRect, int textureWidth, int textureHeight)
    {
        var width = sourceRect.Size.X > 0f ? sourceRect.Size.X : textureWidth;
        var height = sourceRect.Size.Y > 0f ? sourceRect.Size.Y : textureHeight;
        return new Rect2(sourceRect.Position, new Vector2(width, height));
    }

    private void EnsureVertexScratchCapacity(int requestedVertexCount)
    {
        if (requestedVertexCount <= vertexScratch.Length / FloatsPerVertex)
        {
            return;
        }

        var nextCapacity = vertexScratch.Length / FloatsPerVertex;
        while (nextCapacity < requestedVertexCount)
        {
            nextCapacity *= 2;
        }

        Array.Resize(ref vertexScratch, nextCapacity * FloatsPerVertex);
    }

    private void EnsureGpuVertexCapacity(int requestedVertexCount)
    {
        if (requestedVertexCount <= vertexCapacity)
        {
            return;
        }

        var device = backend.Device.Value;
        ReleaseGpuHandle(device, vertexBuffer, gpuApi.ReleaseBuffer);
        ReleaseGpuHandle(device, vertexTransferBuffer, gpuApi.ReleaseTransferBuffer);
        vertexBuffer = IntPtr.Zero;
        vertexTransferBuffer = IntPtr.Zero;

        var nextCapacity = vertexCapacity;
        while (nextCapacity < requestedVertexCount)
        {
            nextCapacity *= 2;
        }

        CreateVertexResources(nextCapacity);
    }

    private void UpdateObservedPresentationSize(Vector2I requestedWindowSize)
    {
        var observedSize = requestedWindowSize;
        if (gpuApi.GetWindowSize(window, out var width, out var height))
        {
            observedSize = new Vector2I(width, height);
        }

        if (observedSize == presentationSize)
        {
            return;
        }

        presentationSize = observedSize;
        backend.Resize(observedSize.X, observedSize.Y, dpiScale: 1f);
        observedPresentationResizes++;
    }

    private RuntimeFrameDiagnostics CreateDiagnostics(
        CanvasItemRenderPlan renderPlan,
        long capturePresenterManagedBytesAllocated,
        int actualDrawCalls)
    {
        return new RuntimeFrameDiagnostics(
            RuntimeFramePresenter.RenderSource,
            RuntimeFramePresenter.GpuPresentationBackend,
            UsedFallbackPresenter: false,
            FallbackReason: string.Empty,
            renderPlan.DrawCallCount,
            actualDrawCalls,
            CountTextureSwitches(renderPlan),
            CountPipelineSwitches(renderPlan),
            textureUploads,
            textureCacheHits,
            presentationResourcesCreated,
            screenshotResourcesRecreated,
            observedPresentationResizes,
            PresentationBackendReconfigurations: 0,
            MaxPresenterManagedBytesPerFrame: 0,
            PresenterMeasuredFrames: 0,
            capturePresenterManagedBytesAllocated);
    }

    private void CommitStagedTextureUploads()
    {
        textureCacheHits += pendingTextureCacheHits;
        pendingTextureCacheHits = 0;
        if (stagedTextureCache.Count == 0)
        {
            return;
        }

        textureUploads += stagedTextureCache.Count;
        foreach (var pair in stagedTextureCache)
        {
            textureCache[pair.Key] = pair.Value;
        }

        stagedTextureCache.Clear();
    }

    private void ReleaseSubmittedTransferBuffers()
    {
        if (submittedTransferBuffers.Count == 0)
        {
            return;
        }

        var device = backend.Device.Value;
        if (device == IntPtr.Zero)
        {
            submittedTransferBuffers.Clear();
            return;
        }

        foreach (var buffer in submittedTransferBuffers)
        {
            gpuApi.ReleaseTransferBuffer(device, buffer);
        }

        submittedTransferBuffers.Clear();
    }

    private void ReleasePendingTextureUploads(bool releaseTextures)
    {
        if (pendingTextureUploads.Count == 0)
        {
            return;
        }

        var device = backend.Device.Value;
        foreach (var upload in pendingTextureUploads)
        {
            if (device != IntPtr.Zero && upload.TransferBuffer != IntPtr.Zero && !submittedTransferBuffers.Contains(upload.TransferBuffer))
            {
                gpuApi.ReleaseTransferBuffer(device, upload.TransferBuffer);
            }

            if (releaseTextures && device != IntPtr.Zero && upload.Texture != IntPtr.Zero)
            {
                gpuApi.ReleaseTexture(device, upload.Texture);
                if (stagedTextureCache.TryGetValue(upload.Source, out var staged) && staged.Handle == upload.Texture)
                {
                    stagedTextureCache.Remove(upload.Source);
                }
            }
        }

        pendingTextureUploads.Clear();
    }

    private void RollbackStagedTextureUploads()
    {
        pendingTextureCacheHits = 0;
        if (stagedTextureCache.Count == 0)
        {
            return;
        }

        var device = backend.Device.Value;
        if (device != IntPtr.Zero)
        {
            foreach (var resource in stagedTextureCache.Values)
            {
                if (resource.Handle != IntPtr.Zero)
                {
                    gpuApi.ReleaseTexture(device, resource.Handle);
                }
            }
        }

        stagedTextureCache.Clear();
    }

    private static int CountTextureSwitches(CanvasItemRenderPlan renderPlan)
    {
        var switches = 0;
        Rid? previous = null;
        for (var index = 0; index < renderPlan.Commands.Count; index++)
        {
            var command = renderPlan.Commands[index];
            if (!command.BatchKey.Texture.IsValid())
            {
                previous = null;
                continue;
            }

            if (previous is null || previous.Value != command.BatchKey.Texture)
            {
                switches++;
                previous = command.BatchKey.Texture;
            }
        }

        return switches;
    }

    private static int CountPipelineSwitches(CanvasItemRenderPlan renderPlan)
    {
        var switches = 0;
        bool? previousTextured = null;
        for (var index = 0; index < renderPlan.Batches.Count; index++)
        {
            var batch = renderPlan.Batches[index];
            var textured = batch.Key.Texture.IsValid();
            if (previousTextured is null || previousTextured.Value != textured)
            {
                switches++;
                previousTextured = textured;
            }
        }

        return switches;
    }

    private static Rect2 Transform(CanvasItemRenderCommand command, Rect2 rect)
    {
        return command.Transform * rect;
    }

    private static Vector2 Transform(CanvasItemRenderCommand command, Vector2 point)
    {
        return command.Transform.Xform(point);
    }

    private static Color GetPolygonVertexColor(CanvasItemRenderCommand command, int index)
    {
        var vertexColor = index >= 0 && index < command.Colors.Count
            ? command.Colors[index]
            : Color.White;
        return command.EffectiveModulate * vertexColor;
    }

    private static IntPtr AllocateStructArray<T>(IReadOnlyList<T> values)
        where T : struct
    {
        var elementSize = Marshal.SizeOf<T>();
        var pointer = Marshal.AllocHGlobal(elementSize * values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            Marshal.StructureToPtr(values[index], IntPtr.Add(pointer, index * elementSize), fDeleteOld: false);
        }

        return pointer;
    }

    private static string FormatIoSummary(IntPtr metadataPointer, uint count)
    {
        if (metadataPointer == IntPtr.Zero || count == 0)
        {
            return string.Empty;
        }

        var elementSize = Marshal.SizeOf<ShaderCross.IOVarMetadata>();
        var itemCount = checked((int)count);
        var items = new string[itemCount];
        for (var index = 0; index < itemCount; index++)
        {
            var item = Marshal.PtrToStructure<ShaderCross.IOVarMetadata>(
                IntPtr.Add(metadataPointer, index * elementSize));
            items[index] = $"{item.Name}@{item.Location}:{item.VectorType}{item.VectorSize}";
        }

        return string.Join(", ", items);
    }

    private static UIntPtr GetNullTerminatedByteLength(IntPtr pointer)
    {
        var byteCount = 0;
        while (Marshal.ReadByte(pointer, byteCount) != 0)
        {
            byteCount++;
        }

        return checked((UIntPtr)byteCount);
    }

    private static void ReleaseGpuHandle(IntPtr device, IntPtr handle, Action<IntPtr, IntPtr> release)
    {
        if (device != IntPtr.Zero && handle != IntPtr.Zero)
        {
            release(device, handle);
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(RuntimeGpuFramePresenter));
        }
    }

    private enum RuntimePipelineKind
    {
        Solid,
        Textured
    }

    private readonly record struct RuntimeTextureResource(IntPtr Handle, int Width, int Height, long ContentVersion);

    private readonly record struct RuntimeDrawBatch(
        int FirstVertex,
        int VertexCount,
        RuntimePipelineKind Kind,
        RuntimeTextureResource? Texture);

    private readonly record struct PendingTextureUpload(Texture2D Source, IntPtr TransferBuffer, IntPtr Texture, int Width, int Height);

    private readonly struct RuntimeGpuShader : IDisposable
    {
        private readonly IntPtr device;

        public RuntimeGpuShader(
            IntPtr device,
            IntPtr handle,
            SDL.GPUShaderFormat format,
            string inputSummary,
            string outputSummary)
        {
            this.device = device;
            Handle = handle;
            Format = format;
            InputSummary = inputSummary;
            OutputSummary = outputSummary;
        }

        public IntPtr Handle { get; }

        public SDL.GPUShaderFormat Format { get; }

        public string InputSummary { get; }

        public string OutputSummary { get; }

        public void Dispose()
        {
            if (device != IntPtr.Zero && Handle != IntPtr.Zero)
            {
                SDL.ReleaseGPUShader(device, Handle);
            }
        }
    }
}

internal static class RuntimeGpuCommandBufferFinalizer
{
    public static void CompleteAfterFailure(
        SdlGpuFrame frame,
        bool swapchainTextureAcquired,
        bool terminalPathStarted,
        Action<SdlGpuFrame> submitAcquiredFrame,
        Func<SdlGpuCommandBufferHandle, bool> cancelUnacquiredFrame)
    {
        ArgumentNullException.ThrowIfNull(submitAcquiredFrame);
        ArgumentNullException.ThrowIfNull(cancelUnacquiredFrame);

        if (terminalPathStarted || !frame.CommandBuffer.IsValid)
        {
            return;
        }

        if (swapchainTextureAcquired)
        {
            submitAcquiredFrame(frame);
            return;
        }

        _ = cancelUnacquiredFrame(frame.CommandBuffer);
    }
}

internal readonly record struct RuntimeFrameDiagnostics(
    string RenderSource,
    string PresentationBackend,
    bool UsedFallbackPresenter,
    string FallbackReason,
    int RenderBatches,
    int ActualDrawCalls,
    int TextureSwitches,
    int PipelineSwitches,
    int TextureUploads,
    int TextureCacheHits,
    int PresentationResourcesCreated,
    int PresentationResourcesRecreated,
    int ObservedPresentationResizes,
    int PresentationBackendReconfigurations,
    long MaxPresenterManagedBytesPerFrame,
    int PresenterMeasuredFrames,
    long CapturePresenterManagedBytesAllocated);

internal readonly record struct RuntimePresentedFrame(
    RuntimeFrameDiagnostics Diagnostics,
    RuntimeFrameSnapshot? Screenshot);

internal sealed class RuntimeFrameSnapshot
{
    public RuntimeFrameSnapshot(int width, int height, byte[] rgbaPixels)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Frame snapshot width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Frame snapshot height must be positive.");
        }

        ArgumentNullException.ThrowIfNull(rgbaPixels);
        if (rgbaPixels.Length != width * height * 4)
        {
            throw new ArgumentException("Frame snapshot pixel buffer length does not match dimensions.", nameof(rgbaPixels));
        }

        Width = width;
        Height = height;
        RgbaPixels = rgbaPixels;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] RgbaPixels { get; }
}
