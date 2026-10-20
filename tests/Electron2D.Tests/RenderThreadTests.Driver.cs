using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderThreadTests
{
    private sealed class Driver(Window window, string backend, bool hlsl) : Node
    {
        private readonly AutoResetEvent _request = new(false), _accepted = new(false);
        private Thread? _producer;
        private volatile bool _stop;
        private Exception? _producerError;
        private Action _work = null!, _read = null!;
        private RenderingDevice? _device;
        private RID _pipeline, _buffer, _outputSet, _inputSet;
        private readonly float[] _input = [1, 2, 3, 4];
        private readonly byte[] _result = new byte[16];
        private int _phase, _frame, _ran, _reads, _idleFrames;
        private bool _disabledRan, _idle;
        private ulong _beforeDisabled, _idleDrawn;
        private long _before, _ownerBytes, _producerBytes;
        internal bool Completed;
        internal WeakReference? CanceledCapture;
        internal int CanceledInvocations;
        internal void Prepare()
        {
            ProcessEnabled = true; _work = Work; _read = Read;
            Worker(() => RenderingServer.CallOnRenderThread(Setup)); RenderingServer.CallOnRenderThread(static () => { });
            if (_device is not null)
            {
                var completed = false; byte[] input = [17, 0, 0, 0], output = new byte[4]; var failure = new ExpectedFailure("sync after queued native write");
                Worker(() => RenderingServer.CallOnRenderThread(() => { _device.BufferUpdate(_buffer, 0, 4, input); _device.BufferGetDataAsync(_buffer, output.AsMemory(), () => completed = true); throw failure; }));
                try { RenderingServer.ForceSync(); throw new InvalidOperationException("Expected sync callback failure."); } catch (ExpectedFailure error) { Check(ReferenceEquals(error, failure) && completed && output[0] == 17, "ForceSync completes recorded native work and readbacks before reporting callback errors."); }
            }
            _producer = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        _request.WaitOne(); if (_stop) break;
                        var before = GC.GetAllocatedBytesForCurrentThread(); Check(!RenderingServer.IsOnRenderThread(), "Prepared producer remains off-owner."); RenderingServer.CallOnRenderThread(_work);
                        if (_frame is >= 16 and < 48) _producerBytes += GC.GetAllocatedBytesForCurrentThread() - before; _accepted.Set();
                    }
                }
                catch (Exception error) { _producerError = error; _accepted.Set(); }
            }); _producer.Start(); _beforeDisabled = Engine.GetFramesDrawn(); RenderingServer.FramePostDraw += Captured;
        }
        private void Setup()
        {
            Check(RenderingServer.IsOnRenderThread(), "Worker-requested native program setup executes on the owner."); _device = RenderingServer.GetRenderingDevice();
            if (backend == "compatibility") { Check(_device is null, "Compatibility callback has no programmable device."); return; }
            Check(_device is not null, "GPU callback acquires the active retained device.");
            using var input = typeof(RenderThreadTests).Assembly.GetManifestResourceStream("Electron2D.Compute." + (hlsl ? "LocalComputeHLSL" : "LocalCompute") + ".comp.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); using var module = new RDShaderSPIRV { BytecodeCompute = bytes.ToArray() };
            var shader = _device!.ShaderCreateFromSPIRV(module); _pipeline = _device.ComputePipelineCreate(shader); _buffer = _device.StorageBufferCreate(16); float[] values = [2, 4, 3, 0]; var uniform = _device.UniformBufferCreate(16, MemoryMarshal.AsBytes(values.AsSpan()));
            using var output = new RDUniform { Binding = 7, UniformType = UniformType.StorageBuffer }; output.AddID(_buffer); _outputSet = _device.UniformSetCreate([output], shader, 3);
            using var settings = new RDUniform { Binding = 4, UniformType = UniformType.UniformBuffer }; settings.AddID(uniform); _inputSet = _device.UniformSetCreate([settings], shader, 2);
        }
        protected override void OnProcess(double delta)
        {
            if (_phase == 0) { Worker(() => RenderingServer.CallOnRenderThread(() => { _disabledRan = true; RenderingServer.SetDefaultClearColor(Colors.Red); })); _phase = 1; return; }
            if (_phase == 1)
            {
                Check(_disabledRan && Engine.GetFramesDrawn() == _beforeDisabled && !RenderingServer.RenderLoopEnabled, "Disabled automatic rendering still drains worker work without drawing."); RenderingServer.ForceSync(); RenderingServer.RenderLoopEnabled = true; _phase = 2;
            }
            if (_idle && _idleFrames++ < 3) { Check(Engine.GetFramesDrawn() == _idleDrawn, "Unchanged low-processor frames skip drawing while the worker waits."); return; }
            _before = GC.GetAllocatedBytesForCurrentThread(); _request.Set(); Check(_accepted.WaitOne(10000), "Prepared producer queues its request."); Check(_producerError is null, "Prepared producer has no error.");
        }
        private void Work()
        {
            Check(RenderingServer.IsOnRenderThread() && _ran == _frame, "Each prepared callback executes once, before its canvas frame."); _ran++;
            RenderingServer.SetDefaultClearColor(_frame % 2 == 0 ? Colors.Red : Colors.Blue);
            if (_device is null) return;
            _input[0] = _frame; _device.BufferUpdate(_buffer, 0, 16, MemoryMarshal.AsBytes(_input.AsSpan())); var list = _device.ComputeListBegin(); _device.ComputeListBindComputePipeline(list, _pipeline); _device.ComputeListBindUniformSet(list, _outputSet, 3); _device.ComputeListBindUniformSet(list, _inputSet, 2); _device.ComputeListDispatch(list, 1, 1, 1); _device.ComputeListEnd(); _device.BufferGetDataAsync(_buffer, _result.AsMemory(), _read);
        }
        private void Read()
        { Check(RenderingServer.IsOnRenderThread() && MemoryMarshal.Cast<byte, float>(_result.AsSpan())[0] == _frame * 2 + 3, "Worker-created program executes native arithmetic and completes its current-frame queued readback on owner."); _reads++; }
        private void Captured()
        {
            if (_phase != 2) return;
            Check(_ran == _frame + 1 && (_device is null || _reads == _ran), "Queue/native work completes before FramePostDraw.");
            if (_frame is >= 16 and < 48) { _ownerBytes += GC.GetAllocatedBytesForCurrentThread() - _before; Check(_ownerBytes == 0 && _producerBytes == 0, "Prepared enqueue, owner callbacks, native execution/readback and full canvas frame allocate zero managed bytes on both participating threads."); }
            if (_frame is 0 or 47)
            {
                using var pixels = window.GetTexture().GetImage()!; var color = pixels.GetPixel(8, 8); Check(color.R == (_frame == 0 ? 1 : 0) && color.B == (_frame == 0 ? 0 : 1), "Worker callback changes actual red/blue canvas pixels.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_THREAD_OUTPUT") is { } output) { Directory.CreateDirectory(output); pixels.SavePNG(System.IO.Path.Combine(output, backend + (hlsl ? "-hlsl" : "") + "-" + _frame + ".png")); }
            }
            _frame++;
            if (_frame == 48) { _idle = true; _idleDrawn = Engine.GetFramesDrawn(); return; }
            if (_frame < 49) return;
            Check(_idleFrames >= 4, "A worker request resumes rendering after unchanged frames."); Completed = true; CanceledCapture = QueueCapture(() => CanceledInvocations++); RenderingServer.FramePostDraw -= Captured; window.Tree!.Quit();
        }
        internal void Stop()
        {
            _stop = true; _request.Set(); if (_producer is not null) Check(_producer.Join(10000), "Prepared producer terminates after session cleanup."); _request.Dispose(); _accepted.Dispose(); Check(_device is null || _device.IsDisposed, "Renderer-owned device retires after worker-produced native work.");
        }
    }
}
