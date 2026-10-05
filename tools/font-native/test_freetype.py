"""Execute the private FreeType WOFF2 decoder on the engine's bundled font."""

import ctypes
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
library = ctypes.CDLL(str(Path(sys.argv[1]).resolve()))
library.FT_Init_FreeType.argtypes = [ctypes.POINTER(ctypes.c_void_p)]
library.FT_New_Memory_Face.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_long,
                                    ctypes.c_long, ctypes.POINTER(ctypes.c_void_p)]
library.FT_Get_Char_Index.argtypes = [ctypes.c_void_p, ctypes.c_ulong]
library.FT_Done_Face.argtypes = [ctypes.c_void_p]
library.FT_Done_FreeType.argtypes = [ctypes.c_void_p]
engine, face = ctypes.c_void_p(), ctypes.c_void_p()
assert library.FT_Init_FreeType(ctypes.byref(engine)) == 0
try:
    font = next((root / "src/Scene/Theme/Fonts").glob("*.woff2"))
    data = ctypes.create_string_buffer(font.read_bytes())
    assert library.FT_New_Memory_Face(engine, data, len(data) - 1, 0, ctypes.byref(face)) == 0
    try:
        assert library.FT_Get_Char_Index(face, ord("A")) > 0
    finally:
        assert library.FT_Done_Face(face) == 0
    invalid = ctypes.create_string_buffer(b"not a font")
    assert library.FT_New_Memory_Face(engine, invalid, len(invalid) - 1, 0, ctypes.byref(face)) != 0
finally:
    assert library.FT_Done_FreeType(engine) == 0
print("Private FreeType decodes the bundled WOFF2 font and resolves its glyphs")
