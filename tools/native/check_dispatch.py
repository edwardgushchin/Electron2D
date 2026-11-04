"""Check cold loading and exact, immutable SDL binding in the real native module."""
import ctypes
from pathlib import Path
import sys


def check_sdl_unloaded():
    if sys.platform.startswith("linux"):
        assert "libSDL3" not in Path("/proc/self/maps").read_text(), "Cold module loaded SDL"
    elif sys.platform == "win32":
        kernel = ctypes.WinDLL("kernel32")
        kernel.GetModuleHandleW.argtypes = [ctypes.c_wchar_p]
        kernel.GetModuleHandleW.restype = ctypes.c_void_p
        assert not kernel.GetModuleHandleW("SDL3.dll"), "Cold module loaded SDL"


engine = ctypes.CDLL(str(Path(sys.argv[1]).resolve()))
engine.e2d_memory_abi.restype = ctypes.c_uint
assert engine.e2d_memory_abi() == 1
check_sdl_unloaded()
# Exercise cold text too: SDL header aliases must never replace libc memory calls.
allocate_type = ctypes.CFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p, ctypes.c_size_t)
resize_type = ctypes.CFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_size_t)
free_type = ctypes.CFUNCTYPE(None, ctypes.c_void_p, ctypes.c_void_p)
engine.e2d_memory_malloc.argtypes = [ctypes.c_size_t]
engine.e2d_memory_malloc.restype = ctypes.c_void_p
engine.e2d_memory_realloc.argtypes = [ctypes.c_void_p, ctypes.c_size_t]
engine.e2d_memory_realloc.restype = ctypes.c_void_p
engine.e2d_memory_free.argtypes = [ctypes.c_void_p]
allocate = allocate_type(lambda context, size: engine.e2d_memory_malloc(size))
resize = resize_type(lambda context, value, size: engine.e2d_memory_realloc(value, size))
free = free_type(lambda context, value: engine.e2d_memory_free(value))
engine.e2d_text_memory.argtypes = [allocate_type, resize_type, free_type]
assert engine.e2d_text_memory(allocate, resize, free) == 0
data = ctypes.create_string_buffer((Path(__file__).resolve().parents[2] / "src/Vendor/ICU/data/TextBreak78.dat").read_bytes())
engine.e2d_text_init.argtypes = [ctypes.c_void_p]
assert engine.e2d_text_init(data) == 0
engine.e2d_break_open.argtypes = [ctypes.c_int, ctypes.c_char_p, ctypes.c_void_p, ctypes.c_int, ctypes.POINTER(ctypes.c_int)]
engine.e2d_break_open.restype = ctypes.c_void_p
engine.e2d_break_close.argtypes = [ctypes.c_void_p]
error = ctypes.c_int()
iterator = engine.e2d_break_open(1, b"zh", None, 0, ctypes.byref(error))
assert iterator and error.value <= 0, "Cold text processing failed before SDL binding"
engine.e2d_break_close(iterator)
check_sdl_unloaded()
engine.e2d_native_bind_sdl.argtypes = [ctypes.c_void_p]
engine.e2d_native_bind_sdl.restype = ctypes.c_int
foreign = ctypes.WinDLL("kernel32") if sys.platform == "win32" else ctypes.CDLL(None)
assert engine.e2d_native_bind_sdl(None) == 0
assert engine.e2d_native_bind_sdl(foreign._handle) == 0
sdl = ctypes.CDLL(str(Path(sys.argv[2]).resolve()))
assert engine.e2d_native_bind_sdl(sdl._handle) == 1
assert engine.e2d_native_bind_sdl(sdl._handle) == 1
assert engine.e2d_native_bind_sdl(foreign._handle) == 0
print("Cold allocation/text leave SDL unloaded; incomplete/rebound cores reject; exact SDL binding is repeatable.")
