using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Electron2D;

internal sealed unsafe partial class WaylandInput
{
    private const string XKBClientLibrary = "libxkbcommon.so.0";
    private readonly List<KeySample> _keys = new(32);
    private int _nextKey;

    internal bool HasKeyboardAdapter => _xkbLibrary != 0;

    internal int LayoutCount => ActiveKeyboard()?.LayoutCount ?? 0;

    internal int CurrentLayout => ActiveKeyboard()?.CurrentLayout ?? -1;

    internal string LayoutName(int index)
    {
        var keyboard = ActiveKeyboard();
        if (keyboard is null || (uint)index >= (uint)keyboard.LayoutCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return keyboard.LayoutName(index);
    }

    internal uint SymbolForPhysical(Key key)
    {
        var keyboard = ActiveKeyboard();
        var evdev = PhysicalKeyCodes.GetValueOrDefault(key);
        return keyboard is not null && evdev != 0 ? KeySymbol(keyboard.State, evdev + 8) : 0;
    }

    private KeyboardState? ActiveKeyboard()
    {
        DispatchPending();
        KeyboardState? available = null;
        foreach (var seat in _seats.Values)
        {
            if (seat.Keyboard is not { State: not 0 } keyboard)
                continue;
            if (keyboard.Focus != 0)
                return keyboard;
            available ??= keyboard;
        }
        return available;
    }

    internal readonly record struct KeySample(nint Surface, uint Raw, bool Pressed, uint Symbol, uint BaseSymbol);

    internal bool TryReadKey(nint surface, ushort raw, bool pressed, out KeySample key)
    {
        DispatchPending();
        for (var i = _nextKey; i < _keys.Count; i++)
        {
            key = _keys[i];
            if (key.Surface == surface && key.Raw == raw && key.Pressed == pressed)
            {
                _nextKey = i + 1;
                return true;
            }
        }
        key = default;
        return false;
    }

    internal void ClearKeys()
    {
        _keys.Clear();
        _nextKey = 0;
    }

    private sealed class KeyboardState(WaylandInput owner, nint proxy) : IDisposable
    {
        private const uint MaximumKeymapSize = 16 * 1024 * 1024;
        private readonly WaylandInput _owner = owner;
        private readonly nint _proxy = proxy;
        private GCHandle _listener;
        private nint _context;
        private uint _depressed;
        private uint _latched;
        private uint _locked;
        private uint _group;

        internal nint Focus;
        internal nint State { get; private set; }
        internal int LayoutCount => checked((int)CountLayouts(StateKeymap(State)));
        internal int CurrentLayout => (uint)LayoutCount > _group ? (int)_group : -1;

        internal string LayoutName(int index) =>
            Marshal.PtrToStringUTF8(KeymapLayoutName(StateKeymap(State), (uint)index)) ?? "";

        internal static KeyboardState Open(WaylandInput owner, nint seat)
        {
            Argument newID = default;
            var proxy = RequireHandle(MarshalRequest(seat, 1,
                NativeLibrary.GetExport(owner._library, "wl_keyboard_interface"), ProxyVersion(seat), 0, &newID));
            var keyboard = new KeyboardState(owner, proxy);
            try
            {
                keyboard._context = RequireHandle(CreateXKBContext(0));
                keyboard._listener = GCHandle.Alloc(keyboard);
                Listen(proxy, &KeyboardEvent, keyboard._listener);
                return keyboard;
            }
            catch
            {
                keyboard.Dispose();
                throw;
            }
        }

        private void SetKeymap(uint format, int fd, uint size)
        {
            using var file = new SafeFileHandle((nint)fd, ownsHandle: true);
            if (format == 0)
            {
                ReplaceState(0);
                return;
            }
            if (format != 1 || size == 0 || size > MaximumKeymapSize)
                throw new InvalidOperationException("Unsupported Wayland keyboard keymap.");

            var bytes = GC.AllocateUninitializedArray<byte>((int)size);
            using var stream = new FileStream(file, System.IO.FileAccess.Read);
            stream.ReadExactly(bytes);
            if (bytes[^1] != 0)
                throw new InvalidOperationException("The Wayland keyboard keymap must end with a null byte.");
            fixed (byte* data = bytes)
            {
                var keymap = ReadXKBKeymap(_context, data, size - 1, 1, 0);
                if (keymap == 0)
                    throw new InvalidOperationException("Could not read the Wayland keyboard keymap.");
                try { ReplaceState(RequireHandle(CreateXKBState(keymap))); }
                finally { FreeXKBKeymap(keymap); }
            }
            ApplyModifiers();
        }

        private void ReplaceState(nint next)
        {
            if (State != 0) FreeXKBState(State);
            State = next;
        }

        private void ApplyModifiers()
        {
            if (State != 0)
                UpdateModifiers(State, _depressed, _latched, _locked, 0, 0, _group);
        }

        private void ObserveKey(uint key, uint state)
        {
            if (State == 0 || Focus == 0 || state > 1 || key > ushort.MaxValue)
                return;
            var code = key + 8;
            var layout = KeyLayout(State, code);
            var count = KeymapSymbolsAtLevel(StateKeymap(State), code, layout, 0, out var symbols);
            var baseSymbol = count > 0 ? *symbols : 0;
            _owner._keys.Add(new KeySample(Focus, key, state == 1, KeySymbol(State, code), baseSymbol));
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int KeyboardEvent(nint context, nint proxy, uint opcode, nint message, Argument* arguments)
        {
            var keyboard = (KeyboardState)GCHandle.FromIntPtr(context).Target!;
            try
            {
                switch (opcode)
                {
                    case 0: // keymap
                        keyboard.SetKeymap(arguments[0].UInt, arguments[1].FD, arguments[2].UInt);
                        break;
                    case 1: // enter
                        keyboard.Focus = arguments[1].Object;
                        break;
                    case 2: // leave
                        keyboard.Focus = 0;
                        break;
                    case 3: // key
                        keyboard.ObserveKey(arguments[2].UInt, arguments[3].UInt);
                        break;
                    case 4: // modifiers
                        keyboard._depressed = arguments[1].UInt;
                        keyboard._latched = arguments[2].UInt;
                        keyboard._locked = arguments[3].UInt;
                        keyboard._group = arguments[4].UInt;
                        keyboard.ApplyModifiers();
                        break;
                }
            }
            catch (Exception error) { keyboard._owner._listenerFailure ??= error; }
            return 0;
        }

        public void Dispose()
        {
            ReleaseProxy(_proxy, opcode: 0, minimumVersion: 3);
            if (_listener.IsAllocated) _listener.Free();
            ReplaceState(0);
            if (_context != 0) FreeXKBContext(_context);
        }
    }

    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_context_new")]
    private static partial nint CreateXKBContext(int flags);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_context_unref")]
    private static partial void FreeXKBContext(nint context);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_keymap_new_from_buffer")]
    private static partial nint ReadXKBKeymap(nint context, byte* data, nuint length, int format, int flags);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_keymap_unref")]
    private static partial void FreeXKBKeymap(nint keymap);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_keymap_num_layouts")]
    private static partial uint CountLayouts(nint keymap);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_keymap_layout_get_name")]
    private static partial nint KeymapLayoutName(nint keymap, uint layout);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_keymap_key_get_syms_by_level")]
    private static partial int KeymapSymbolsAtLevel(nint keymap, uint key, uint layout, uint level, out uint* symbols);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_state_new")]
    private static partial nint CreateXKBState(nint keymap);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_state_unref")]
    private static partial void FreeXKBState(nint state);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_state_get_keymap")]
    private static partial nint StateKeymap(nint state);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_state_update_mask")]
    private static partial uint UpdateModifiers(nint state, uint depressed, uint latched, uint locked,
        uint depressedGroup, uint latchedGroup, uint lockedGroup);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_state_key_get_layout")]
    private static partial uint KeyLayout(nint state, uint key);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_state_key_get_one_sym")]
    private static partial uint KeySymbol(nint state, uint key);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_keysym_to_utf32")]
    private static partial uint SymbolToUTF32(uint symbol);
    [LibraryImport(XKBClientLibrary, EntryPoint = "xkb_keysym_to_upper")]
    private static partial uint UpperSymbol(uint symbol);
}
