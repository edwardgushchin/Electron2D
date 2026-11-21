using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using SDL3;
using DriverResource = Electron2D.RenderingDevice.DriverResource;

internal static partial class RenderingRuntimeTests
{
    private static void RunPendingDirect3D12ViewportTiming()
    {
        var root = new Window { Title = "D3D12 pending viewport retirement", Size = new(320, 240) };
        var completed = false;
        root.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingDriverName() == "direct3d12", "The retirement fixture requires the Direct3D 12 queue.");
            var rid = RenderingServer.ViewportCreate();
            RenderingServer.ViewportSetSize(rid, 64, 64);
            RenderingServer.ViewportSetActive(rid, true);
            RenderingServer.ViewportSetUpdateMode(rid, ViewportUpdateMode.Always);
            RenderingServer.ViewportSetMeasureRenderTime(rid, true);
            RenderingServer.FramePostDraw += Retire;

            void Retire()
            {
                RenderingServer.FramePostDraw -= Retire;
                FreeViewportWithPendingDirect3D12Timing(rid);
                completed = true;
                root.Tree!.Quit();
            }
        };
        Check(Engine.Run(root) == 0 && completed, "The pending Direct3D 12 retirement scenario completed.");
    }

    private static unsafe void FreeViewportWithPendingDirect3D12Timing(RID rid)
    {
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags staticField = BindingFlags.Static | BindingFlags.NonPublic;
        var server = RenderingServer.Service!;
        var view = Viewport.ResolveViewportRID(rid);
        var backend = (GPUCanvasBackend)typeof(RenderingServer).GetField("_backend", instance)!.GetValue(server)!;
        var timing = (CanvasRenderTiming)typeof(RenderingServer).GetMethod("ViewportTiming", instance)!.Invoke(server, [view])!;
        var fenceField = timing.GetType().GetField("_fence", instance)!;
        var queries = timing.GetType().GetField("_queries", instance)!.GetValue(timing)!;
        var pool = (SafeHandle)typeof(GPUTimestampQueries).GetField("_pool", instance)!.GetValue(queries)!;
        var target = backend.FindTarget(view)!;
        var waitField = typeof(SDL).GetField("WaitForGPUFencesPointerNativeFunction", staticField)!;
        var original = (Delegate)waitField.GetValue(null)!;
        var wait = original.Method.CreateDelegate<Func<nint, bool, nint, uint, bool>>(original.Target);

        var device = RenderingServer.GetRenderingDevice()!;
        var logicalDevice = (nint)device.GetDriverResource(DriverResource.LogicalDevice, default, 0);
        var queue = (nint)device.GetDriverResource(DriverResource.CommandQueue, default, 0);

        // Use the borrowed renderer's native queue so the gate precedes the real SDL submission.
        var fenceId = new Guid("0a753dcf-c4d8-4b91-adf6-be5a60d95a76");
        var createFence = (delegate* unmanaged[Stdcall]<nint, ulong, int, Guid*, nint*, int>)(*(nint**)logicalDevice)[36];
        var queueWait = (delegate* unmanaged[Stdcall]<nint, nint, ulong, int>)(*(nint**)queue)[15];
        nint gate = 0;
        Marshal.ThrowExceptionForHR(createFence(logicalDevice, 0, 0, &fenceId, &gate));
        Check(gate != 0, "Direct3D 12 must return a native queue gate fence.");
        var signal = (delegate* unmanaged[Stdcall]<nint, ulong, int>)(*(nint**)gate)[10];
        var gateHandle = gate;

        nint submissionFence = 0;
        var observedWait = false;

        bool WaitForRetirement(nint sdlDevice, bool waitAll, nint fences, uint count)
        {
            if (submissionFence == 0 || !waitAll || count != 1 || *(nint*)fences != submissionFence)
                return wait(sdlDevice, waitAll, fences, count);

            Check(!SDL.QueryGPUFence(sdlDevice, submissionFence), "The viewport submission must still be pending when retirement begins.");
            Check(!pool.IsClosed, "Retirement must keep the native query pool alive while the submission is pending.");
            Marshal.ThrowExceptionForHR(signal(gateHandle, 1));
            var completed = wait(sdlDevice, waitAll, fences, count);
            Check(completed && SDL.QueryGPUFence(sdlDevice, submissionFence), "SDL must complete the same viewport submission during retirement.");
            Check(!pool.IsClosed, "The native query pool must remain alive through completion.");
            observedWait = true;
            return completed;
        }

        try
        {
            timing.BeginFrame();
            // Release the queue on timeout so an unexpected GPU wait fails the assertion instead of hanging the test.
            var watchdog = new System.Threading.Timer(state =>
            {
                _ = signal(gateHandle, 1);
            }, null, 5000, Timeout.Infinite);
            try
            {
                Marshal.ThrowExceptionForHR(queueWait(queue, gate, 1));
                // Render only this viewport so present, readback and other viewports cannot drain its fence.
                backend.Draw(target, [], [], Colors.Blue, true, 0, [], new CanvasLightingFrame(),
                    processParticles: false, timing: timing);
                timing.EndFrame();
                submissionFence = (nint)fenceField.GetValue(timing)!;
                Check(submissionFence != 0, "A pending SDL fence must remain owned by this viewport.");

                Func<nint, bool, nint, uint, bool> observer = WaitForRetirement;
                waitField.SetValue(null, observer.Method.CreateDelegate(waitField.FieldType, observer.Target));
                RenderingServer.FreeRID(rid);
                Check(observedWait && pool.IsClosed, "FreeRID must wait for submission completion before releasing the query pool.");
                Console.WriteLine("D3D12 viewport retirement: SDL fence unsignaled at wait entry, complete after native wait, query pool closed after FreeRID.");
            }
            finally
            {
                waitField.SetValue(null, original);
                watchdog.DisposeAsync().GetAwaiter().GetResult();
            }
        }
        finally
        {
            // Unblock any submitted work on assertion failure before the renderer unwinds.
            _ = signal(gate, 1);
            Marshal.Release(gate);
        }
    }
}
