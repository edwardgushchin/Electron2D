using SDL3;

namespace Electron2D;

internal sealed partial class CompatibilityCanvasBackend
{
    internal override CanvasRenderTiming CreateRenderTiming() => new GLTiming(this);
    private sealed unsafe class GLTiming : CanvasRenderTiming
    {
        private readonly nint _window, _context;
        private readonly uint _first, _last, _bits;
        private readonly bool _disjoint;
        private readonly delegate* unmanaged<int, uint*, void> _delete;
        private readonly delegate* unmanaged<uint, uint, void> _counter;
        private readonly delegate* unmanaged<uint, uint, int*, void> _available;
        private readonly delegate* unmanaged<uint, uint, ulong*, void> _result;
        private readonly delegate* unmanaged<uint, int*, void> _getInteger;
        private readonly delegate* unmanaged<uint> _error;
        private readonly delegate* unmanaged<void> _flush;
        private bool _pending;
        private const uint Timestamp = 0x8E28, CounterBits = 0x8864, Result = 0x8866, Available = 0x8867;
        internal GLTiming(CompatibilityCanvasBackend owner)
        {
            if (owner.Driver is not ("opengl" or "opengles2") || owner._graphics.Context == 0) throw new NotSupportedException("Native canvas timing requires an integrated GL timestamp context.");
            _window = SDL.GetRenderWindow(owner._renderer.DangerousGetHandle()); _context = owner._graphics.Context;
            if (_window == 0) throw Failure("query measured renderer window");
            var previous = Enter();
            try
            {
                _disjoint = owner.Driver == "opengles2"; var suffix = _disjoint ? "EXT" : "";
                var generate = (delegate* unmanaged<int, uint*, void>)Function("glGenQueries" + suffix);
                _delete = (delegate* unmanaged<int, uint*, void>)Function("glDeleteQueries" + suffix);
                _counter = (delegate* unmanaged<uint, uint, void>)Function("glQueryCounter" + suffix);
                _available = (delegate* unmanaged<uint, uint, int*, void>)Function("glGetQueryObjectiv" + suffix);
                _result = (delegate* unmanaged<uint, uint, ulong*, void>)Function("glGetQueryObjectui64v" + suffix);
                _getInteger = (delegate* unmanaged<uint, int*, void>)Function("glGetIntegerv");
                _error = (delegate* unmanaged<uint>)Function("glGetError"); _flush = (delegate* unmanaged<void>)Function("glFlush");
                var query = (delegate* unmanaged<uint, uint, int*, void>)Function("glGetQueryiv" + suffix);
                int bits = 0; query(Timestamp, CounterBits, &bits);
                if (_error() != 0 || bits is < 1 or > 64) throw new NotSupportedException("The active GL context has no timestamp counter.");
                _bits = (uint)bits;
                uint* ids = stackalloc uint[2] { 0, 0 }; generate(2, ids);
                if (_error() != 0 || ids[0] == 0 || ids[1] == 0) { _delete(2, ids); throw new InvalidOperationException("Native GL timestamp storage could not be created."); }
                _first = ids[0]; _last = ids[1];
            }
            finally { Leave(previous); }
        }
        private static nint Function(string name)
        { var pointer = SDL.GLGetProcAddress(name); return pointer != 0 ? pointer : throw new NotSupportedException("The active GL context has no " + name + " timestamp adapter."); }
        private (nint Window, nint Context) Enter()
        {
            var previous = (SDL.GLGetCurrentWindow(), SDL.GLGetCurrentContext());
            if (previous.Item1 != _window || previous.Item2 != _context) Check(SDL.GLMakeCurrent(_window, _context), "select measured GL context");
            return previous;
        }
        private void Leave((nint Window, nint Context) previous)
        { if (previous.Window != _window || previous.Context != _context) Check(SDL.GLMakeCurrent(previous.Window, previous.Context), "restore measured GL context"); }
        internal void Begin()
        {
            var previous = Enter(); try { _counter(_first, Timestamp); CheckError(); } finally { Leave(previous); }
        }
        internal void End()
        {
            var previous = Enter(); try { _counter(_last, Timestamp); CheckError(); _flush(); _pending = true; } finally { Leave(previous); }
        }
        protected override bool TryCollect(bool wait, out ulong nanoseconds)
        {
            nanoseconds = 0; if (!_pending) return false;
            var previous = Enter();
            try
            {
                int ready = 0; _available(_last, Available, &ready); CheckError(); if (!wait && ready == 0) return false;
                ulong first = 0, last = 0; _result(_first, Result, &first); _result(_last, Result, &last); CheckError(); _pending = false;
                if (_disjoint) { int disjoint = 0; _getInteger(0x8FBB, &disjoint); CheckError(); if (disjoint != 0) throw new InvalidOperationException("The native GL timestamp clock became disjoint."); }
                nanoseconds = unchecked(last - first); if (_bits < 64) nanoseconds &= (1UL << (int)_bits) - 1; return true;
            }
            finally { Leave(previous); }
        }
        private void CheckError() { if (_error() != 0) throw new InvalidOperationException("The native GL timestamp operation failed."); }
        protected override void Discard() { if (_pending) _ = TryCollect(true, out _); }
        public override void Dispose()
        {
            uint* ids = stackalloc uint[2] { _first, _last };
            try { Discard(); }
            finally { var previous = Enter(); try { _delete(2, ids); CheckError(); } finally { Leave(previous); } }
        }
    }
}
