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

internal readonly record struct RuntimeHostTimingDiagnostics(
    double InputTimeSeconds,
    double PhysicsTimeSeconds,
    double ProcessTimeSeconds,
    double RenderPlanTimeSeconds,
    double SubmitTimeSeconds,
    double PresentTimeSeconds,
    double SchedulerClampedTimeSeconds,
    double SchedulerDroppedTimeSeconds,
    double SchedulerRequestedWaitTimeSeconds,
    double SchedulerObservedWaitTimeSeconds,
    double SchedulerPauseWaitTimeSeconds,
    int SchedulerTargetFrameRate,
    double SchedulerTargetFrameIntervalSeconds,
    int SchedulerPhysicsSteps,
    int SchedulerSoftwareWaits,
    int SchedulerPausedWaits);

/// <summary>
/// Describes the result of a visible Electron2D runtime loop.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="RuntimeHostResult"/> is intentionally machine-readable so
/// automated checks can prove that a project opened a runtime window, presented
/// at least one frame and saved a screenshot when requested.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Instances are immutable and may be read from any thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="RuntimeHost"/>
/// <seealso cref="RuntimeHostOptions"/>
internal sealed class RuntimeHostResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeHostResult"/> type.
    /// </summary>
    ///
    /// <param name="succeeded">Whether the run completed without a host-level failure.</param>
    /// <param name="windowCreated">Whether the runtime window was created.</param>
    /// <param name="windowShown">Whether the runtime window was shown.</param>
    /// <param name="framePresented">Whether at least one frame was presented to the window.</param>
    /// <param name="eventPumpObserved">Whether the event pump was executed.</param>
    /// <param name="inputEventsDispatched">The number of input events dispatched into the scene tree.</param>
    /// <param name="frameCount">The number of frames presented by the host.</param>
    /// <param name="drawCommands">The number of draw commands in the final frame.</param>
    /// <param name="windowWidth">The observed logical window width.</param>
    /// <param name="windowHeight">The observed logical window height.</param>
    /// <param name="pixelWidth">The observed physical pixel width.</param>
    /// <param name="pixelHeight">The observed physical pixel height.</param>
    /// <param name="videoDriver">The display driver name reported by the backend.</param>
    /// <param name="screenshotPath">The screenshot path requested by the caller, if any.</param>
    /// <param name="screenshotSaved">Whether a screenshot file was written.</param>
    /// <param name="diagnosticMessage">A host-level diagnostic message, or an empty string on success.</param>
    /// <param name="renderDiagnostics">The final runtime renderer diagnostics.</param>
    /// <param name="timingDiagnostics">The final host frame scheduler diagnostics.</param>
    ///
    /// <remarks>
    /// The constructor stores values exactly as observed by
    /// <see cref="RuntimeHost"/>. It does not validate that a
    /// screenshot still exists on disk after the result is returned.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Use it from the thread that owns the
    /// runtime host.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    public RuntimeHostResult(
        bool succeeded,
        bool windowCreated,
        bool windowShown,
        bool framePresented,
        bool eventPumpObserved,
        int inputEventsDispatched,
        int frameCount,
        int drawCommands,
        int windowWidth,
        int windowHeight,
        int pixelWidth,
        int pixelHeight,
        string videoDriver,
        string? screenshotPath,
        bool screenshotSaved,
        string diagnosticMessage,
        RuntimeFrameDiagnostics renderDiagnostics,
        RuntimeHostTimingDiagnostics timingDiagnostics)
    {
        Succeeded = succeeded;
        WindowCreated = windowCreated;
        WindowShown = windowShown;
        FramePresented = framePresented;
        EventPumpObserved = eventPumpObserved;
        InputEventsDispatched = inputEventsDispatched;
        FrameCount = frameCount;
        DrawCommands = drawCommands;
        WindowWidth = windowWidth;
        WindowHeight = windowHeight;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        VideoDriver = videoDriver;
        ScreenshotPath = screenshotPath;
        ScreenshotSaved = screenshotSaved;
        DiagnosticMessage = diagnosticMessage;
        RenderSource = renderDiagnostics.RenderSource;
        PresentationBackend = renderDiagnostics.PresentationBackend;
        UsedFallbackPresenter = renderDiagnostics.UsedFallbackPresenter;
        FallbackReason = renderDiagnostics.FallbackReason;
        RenderBatches = renderDiagnostics.RenderBatches;
        ActualDrawCalls = renderDiagnostics.ActualDrawCalls;
        TextureSwitches = renderDiagnostics.TextureSwitches;
        PipelineSwitches = renderDiagnostics.PipelineSwitches;
        TextureUploads = renderDiagnostics.TextureUploads;
        TextureCacheHits = renderDiagnostics.TextureCacheHits;
        PresentationResourcesCreated = renderDiagnostics.PresentationResourcesCreated;
        PresentationResourcesRecreated = renderDiagnostics.PresentationResourcesRecreated;
        ObservedPresentationResizes = renderDiagnostics.ObservedPresentationResizes;
        PresentationBackendReconfigurations = renderDiagnostics.PresentationBackendReconfigurations;
        MaxPresenterManagedBytesPerFrame = renderDiagnostics.MaxPresenterManagedBytesPerFrame;
        PresenterMeasuredFrames = renderDiagnostics.PresenterMeasuredFrames;
        CapturePresenterManagedBytesAllocated = renderDiagnostics.CapturePresenterManagedBytesAllocated;
        InputTimeSeconds = timingDiagnostics.InputTimeSeconds;
        PhysicsTimeSeconds = timingDiagnostics.PhysicsTimeSeconds;
        ProcessTimeSeconds = timingDiagnostics.ProcessTimeSeconds;
        RenderPlanTimeSeconds = timingDiagnostics.RenderPlanTimeSeconds;
        SubmitTimeSeconds = timingDiagnostics.SubmitTimeSeconds;
        PresentTimeSeconds = timingDiagnostics.PresentTimeSeconds;
        SchedulerClampedTimeSeconds = timingDiagnostics.SchedulerClampedTimeSeconds;
        SchedulerDroppedTimeSeconds = timingDiagnostics.SchedulerDroppedTimeSeconds;
        SchedulerRequestedWaitTimeSeconds = timingDiagnostics.SchedulerRequestedWaitTimeSeconds;
        SchedulerObservedWaitTimeSeconds = timingDiagnostics.SchedulerObservedWaitTimeSeconds;
        SchedulerPauseWaitTimeSeconds = timingDiagnostics.SchedulerPauseWaitTimeSeconds;
        SchedulerTargetFrameRate = timingDiagnostics.SchedulerTargetFrameRate;
        SchedulerTargetFrameIntervalSeconds = timingDiagnostics.SchedulerTargetFrameIntervalSeconds;
        SchedulerPhysicsSteps = timingDiagnostics.SchedulerPhysicsSteps;
        SchedulerSoftwareWaits = timingDiagnostics.SchedulerSoftwareWaits;
        SchedulerPausedWaits = timingDiagnostics.SchedulerPausedWaits;
    }

    /// <summary>
    /// Gets whether the run completed without a host-level failure.
    /// </summary>
    ///
    /// <value><c>true</c> on success; otherwise, <c>false</c>.</value>
    ///
    /// <remarks>
    /// User script diagnostics are reported by the scene tree and do not
    /// automatically change this value in the minimal preview host.
    /// </remarks>
    ///
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets whether the runtime window was created.
    /// </summary>
    /// <value><c>true</c> if the window was created; otherwise, <c>false</c>.</value>
    /// <remarks>This value proves that the run did not stay in a text-only path.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public bool WindowCreated { get; }

    /// <summary>
    /// Gets whether the runtime window was shown.
    /// </summary>
    /// <value><c>true</c> if the backend reported a shown window; otherwise, <c>false</c>.</value>
    /// <remarks>Some headless display drivers may create a window without showing a desktop surface.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public bool WindowShown { get; }

    /// <summary>
    /// Gets whether at least one frame was presented.
    /// </summary>
    /// <value><c>true</c> after a frame is copied to the window surface; otherwise, <c>false</c>.</value>
    /// <remarks>Reference-game acceptance checks require this value to be <c>true</c>.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public bool FramePresented { get; }

    /// <summary>
    /// Gets whether the event pump executed during the run.
    /// </summary>
    /// <value><c>true</c> when the host polled window/input events; otherwise, <c>false</c>.</value>
    /// <remarks>This is a host-level marker, not a guarantee that user input was received.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public bool EventPumpObserved { get; }

    /// <summary>
    /// Gets the number of input events dispatched into the scene tree.
    /// </summary>
    /// <value>The dispatched input event count.</value>
    /// <remarks>Automated runs may report zero when no user input was pending.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int InputEventsDispatched { get; }

    /// <summary>
    /// Gets the number of frames presented by the host.
    /// </summary>
    /// <value>The presented frame count.</value>
    /// <remarks>For a positive <see cref="RuntimeHostOptions.FrameLimit"/>, this normally equals that limit.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int FrameCount { get; }

    /// <summary>
    /// Gets the number of draw commands in the final frame.
    /// </summary>
    /// <value>The final draw command count.</value>
    /// <remarks>A value greater than zero proves that scene canvas/UI drawing reached the renderer submission path.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int DrawCommands { get; }

    /// <summary>
    /// Gets the observed logical window width.
    /// </summary>
    /// <value>The logical window width in pixels.</value>
    /// <remarks>The value is captured after the final frame.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int WindowWidth { get; }

    /// <summary>
    /// Gets the observed logical window height.
    /// </summary>
    /// <value>The logical window height in pixels.</value>
    /// <remarks>The value is captured after the final frame.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int WindowHeight { get; }

    /// <summary>
    /// Gets the observed physical pixel width.
    /// </summary>
    /// <value>The physical pixel width reported by the window backend.</value>
    /// <remarks>This can differ from <see cref="WindowWidth"/> on high-DPI displays.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int PixelWidth { get; }

    /// <summary>
    /// Gets the observed physical pixel height.
    /// </summary>
    /// <value>The physical pixel height reported by the window backend.</value>
    /// <remarks>This can differ from <see cref="WindowHeight"/> on high-DPI displays.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int PixelHeight { get; }

    /// <summary>
    /// Gets the display driver name reported by the backend.
    /// </summary>
    /// <value>The display driver name, or <c>unknown</c> when unavailable.</value>
    /// <remarks>The value is diagnostic only and should not be used for gameplay branching.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public string VideoDriver { get; }

    /// <summary>
    /// Gets the requested screenshot path.
    /// </summary>
    /// <value>The requested path, or <c>null</c> when no screenshot was requested.</value>
    /// <remarks>Use <see cref="ScreenshotSaved"/> to check whether the file was written.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public string? ScreenshotPath { get; }

    /// <summary>
    /// Gets whether a screenshot file was written.
    /// </summary>
    /// <value><c>true</c> when <see cref="ScreenshotPath"/> was written; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Screenshot writing uses the final presented frame for bounded runs and
    /// the first presented frame for interactive runs where <see cref="RuntimeHostOptions.FrameLimit"/>
    /// is <c>0</c>.
    /// </remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public bool ScreenshotSaved { get; }

    /// <summary>
    /// Gets a host-level diagnostic message.
    /// </summary>
    /// <value>An empty string on success, or a failure summary when the host could not complete the run.</value>
    /// <remarks>The message does not include secret values or native handles.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public string DiagnosticMessage { get; }

    /// <summary>
    /// Gets the internal renderer path that presented the final frame.
    /// </summary>
    /// <value>The renderer source label.</value>
    /// <remarks>This value is diagnostic and is not part of the public game API.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public string RenderSource { get; }

    /// <summary>
    /// Gets the internal presentation backend used by the final frame presenter.
    /// </summary>
    /// <value>The presenter backend label.</value>
    /// <remarks>This value is diagnostic and is not part of the public game API.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public string PresentationBackend { get; }

    /// <summary>
    /// Gets whether the runtime switched from the primary presenter to a fallback presenter.
    /// </summary>
    /// <value><c>true</c> when a fallback presenter was used; otherwise, <c>false</c>.</value>
    /// <remarks>The fallback presenter is selected only after the primary backend cannot be created or cannot present.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public bool UsedFallbackPresenter { get; }

    /// <summary>
    /// Gets the reason that caused fallback presenter selection.
    /// </summary>
    /// <value>The fallback reason, or an empty string when the primary presenter was used.</value>
    /// <remarks>The message is intended for diagnostics and must not contain secret values.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public string FallbackReason { get; }

    /// <summary>
    /// Gets the batch count reported for the final frame.
    /// </summary>
    /// <value>The final frame batch count.</value>
    /// <remarks>The value comes from the shared canvas render plan.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int RenderBatches { get; }

    /// <summary>
    /// Gets the actual low-level draw-call count reported for the final frame.
    /// </summary>
    /// <value>The final frame presenter draw-call count.</value>
    /// <remarks>This can differ from <see cref="RenderBatches"/> when a presenter cannot submit one batch with one native draw call.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int ActualDrawCalls { get; }

    /// <summary>
    /// Gets the texture switch count for the final frame.
    /// </summary>
    /// <value>The number of texture changes in draw order.</value>
    /// <remarks>Ordering barriers may make this value larger than upload count.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int TextureSwitches { get; }

    /// <summary>
    /// Gets the graphics pipeline switch count for the final frame.
    /// </summary>
    /// <value>The number of solid/textured pipeline changes in draw order.</value>
    /// <remarks>This value is separate from <see cref="TextureSwitches"/> because textured batches can change sampler state without changing pipeline.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int PipelineSwitches { get; }

    /// <summary>
    /// Gets the number of texture resources uploaded during the run.
    /// </summary>
    /// <value>The cumulative texture upload count.</value>
    /// <remarks>Repeated use of the same texture should increase <see cref="TextureCacheHits"/> instead.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int TextureUploads { get; }

    /// <summary>
    /// Gets the number of texture cache hits during the run.
    /// </summary>
    /// <value>The cumulative texture cache hit count.</value>
    /// <remarks>This value proves unchanged textures were reused by the presenter.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int TextureCacheHits { get; }

    /// <summary>
    /// Gets the number of presentation resources created during the run.
    /// </summary>
    /// <value>The cumulative presentation resource creation count.</value>
    /// <remarks>A steady-size window should create the presenter resources once.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int PresentationResourcesCreated { get; }

    /// <summary>
    /// Gets the number of Electron2D-owned screenshot resource recreations caused by size changes.
    /// </summary>
    /// <value>The cumulative screenshot texture or readback resource recreation count.</value>
    /// <remarks>This value is separate from observed window-size changes and backend reconfiguration.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int PresentationResourcesRecreated { get; }

    /// <summary>
    /// Gets the number of observed window-size changes in the final run.
    /// </summary>
    /// <value>The observed presentation resize count.</value>
    /// <remarks>This count is separate from owned presentation resource recreation.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int ObservedPresentationResizes { get; }

    /// <summary>
    /// Gets the number of actual backend or swapchain presentation reconfigurations in the final run.
    /// </summary>
    /// <value>The backend or swapchain reconfiguration count.</value>
    /// <remarks>Observed window-size changes do not increase this value unless the presenter performs a real backend reconfiguration.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int PresentationBackendReconfigurations { get; }

    /// <summary>
    /// Gets the maximum managed bytes allocated across measured presenter frames.
    /// </summary>
    /// <value>The maximum measured presenter-boundary allocation after warm-up.</value>
    /// <remarks>This value intentionally starts at the presenter boundary after the render plan has already been built.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public long MaxPresenterManagedBytesPerFrame { get; }

    /// <summary>
    /// Gets the number of presenter frames included in allocation measurement.
    /// </summary>
    /// <value>The measured presenter frame count after warm-up.</value>
    /// <remarks>Warm-up frames are excluded so one-time presenter setup does not masquerade as steady-state allocation.</remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int PresenterMeasuredFrames { get; }

    /// <summary>
    /// Gets managed bytes allocated by the active presenter during capture frames.
    /// </summary>
    /// <value>The managed allocation count measured around the active presenter call for requested captures.</value>
    /// <remarks>
    /// This value is separate from <see cref="MaxPresenterManagedBytesPerFrame"/> because captures intentionally allocate a
    /// readback buffer. PNG encoding and file writes happen after this presenter-boundary measurement.
    /// </remarks>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public long CapturePresenterManagedBytesAllocated { get; }

    /// <summary>
    /// Gets the accumulated host time spent dispatching input.
    /// </summary>
    /// <value>The input dispatch time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double InputTimeSeconds { get; }

    /// <summary>
    /// Gets the accumulated host time spent advancing fixed physics.
    /// </summary>
    /// <value>The physics callback time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double PhysicsTimeSeconds { get; }

    /// <summary>
    /// Gets the accumulated host time spent in process and draw callbacks.
    /// </summary>
    /// <value>The process callback time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double ProcessTimeSeconds { get; }

    /// <summary>
    /// Gets the accumulated host time spent building the render plan.
    /// </summary>
    /// <value>The render-plan build time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double RenderPlanTimeSeconds { get; }

    /// <summary>
    /// Gets the accumulated host time spent submitting frames to the presenter.
    /// </summary>
    /// <value>The presenter submission time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double SubmitTimeSeconds { get; }

    /// <summary>
    /// Gets the accumulated host time spent in the present phase.
    /// </summary>
    /// <value>The presenter presentation time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double PresentTimeSeconds { get; }

    /// <summary>
    /// Gets the interactive scheduler time removed by the maximum delta clamp.
    /// </summary>
    /// <value>The clamped time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double SchedulerClampedTimeSeconds { get; }

    /// <summary>
    /// Gets the interactive scheduler time dropped after bounded physics catch-up.
    /// </summary>
    /// <value>The dropped time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double SchedulerDroppedTimeSeconds { get; }

    /// <summary>
    /// Gets the accumulated software wait requested by the frame scheduler.
    /// </summary>
    /// <value>The requested deadline wait time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double SchedulerRequestedWaitTimeSeconds { get; }

    /// <summary>
    /// Gets the accumulated time actually spent in software deadline waits.
    /// </summary>
    /// <value>The observed deadline wait time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double SchedulerObservedWaitTimeSeconds { get; }

    /// <summary>
    /// Gets the accumulated time spent waiting while the runtime window was paused.
    /// </summary>
    /// <value>The observed pause wait time in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double SchedulerPauseWaitTimeSeconds { get; }

    /// <summary>
    /// Gets the scheduler-selected target frame rate.
    /// </summary>
    /// <value>The target frame rate in hertz.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int SchedulerTargetFrameRate { get; }

    /// <summary>
    /// Gets the scheduler-selected target frame interval.
    /// </summary>
    /// <value>The target frame interval in seconds.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public double SchedulerTargetFrameIntervalSeconds { get; }

    /// <summary>
    /// Gets the number of fixed physics steps executed by the host scheduler.
    /// </summary>
    /// <value>The accumulated physics scheduler step count.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int SchedulerPhysicsSteps { get; }

    /// <summary>
    /// Gets the number of software waits issued for frame deadlines.
    /// </summary>
    /// <value>The software wait count.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int SchedulerSoftwareWaits { get; }

    /// <summary>
    /// Gets the number of pause waits issued while the window was inactive.
    /// </summary>
    /// <value>The pause wait count.</value>
    /// <threadsafety>This property is immutable.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    public int SchedulerPausedWaits { get; }
}
