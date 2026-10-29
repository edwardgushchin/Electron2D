"""Generate the pinned SDL allocator translation unit without changing its source."""

import argparse
import hashlib
from pathlib import Path

SOURCE_SHA256 = "9101ca62e3274c03306b1930b33c6b5496f51b34dca906fadd2c94802142cc9a"
NOTICE = "/* Generated Electron2D allocation profile; the pinned input source is unchanged. */\n"
ENV_SHA256 = "fbf8eadd78a41dd011716707c055cd8b52908ba7e75ceda8d21cebcff6212db1"
ENV_ORIGINAL = "                SDL_InsertIntoHashTable(env->strings, variable, value, true);"
ENV_REPLACEMENT = """                if (!SDL_InsertIntoHashTable(env->strings, variable, value, true)) {
                    SDL_free(variable);
                }"""
PROPERTIES_SHA256 = "c88a9c7b82861c1fa8a094f2a2b51cb0961a4e1036e2082fff65cc2dc290b407"
IO_SHA256 = "b9c4e6d5af4dc93855af42a418a64bc5bd0bc66afee2aa4e8762bd8269b1a0c4"
IO_DECLARATIONS = """bool E2D_ReserveIOPointer(SDL_PropertiesID props, const char *name);
bool E2D_ResizeIOPointer(SDL_PropertiesID props, const char *name, void **value, size_t size);

"""
IO_HELPERS = """
// Reserve ownership metadata before allocating an IO buffer; resize cannot lose its pointer.
bool E2D_ReserveIOPointer(SDL_PropertiesID props, const char *name)
{
    SDL_Property *property = (SDL_Property *)SDL_calloc(1, sizeof(*property));
    if (!property) return false;
    property->type = SDL_PROPERTY_TYPE_POINTER;
    return SDL_PrivateSetProperty(props, name, property);
}

bool E2D_ResizeIOPointer(SDL_PropertiesID props, const char *name, void **value, size_t size)
{
    SDL_Properties *properties = NULL;
    SDL_FindInHashTable(SDL_properties, (const void *)(uintptr_t)props, (const void **)&properties);
    if (!properties) return SDL_SetError("IO buffer properties were retired");
    bool result = false;
    SDL_LockMutex(properties->lock);
    SDL_Property *property = NULL;
    if (SDL_FindInHashTable(properties->props, name, (const void **)&property) &&
        property->type == SDL_PROPERTY_TYPE_POINTER && !property->cleanup) {
        void *base = SDL_realloc(*value, size);
        if (base) {
            property->value.pointer_value = base;
            *value = base;
            result = true;
        }
    }
    SDL_UnlockMutex(properties->lock);
    return result;
}
"""
IO_EDITS = (
    ("typedef struct IOStreamDynamicMemData", IO_DECLARATIONS + "typedef struct IOStreamDynamicMemData"),
    ("    size_t chunks = (((iodata->end - iodata->data.base) + size) / chunksize) + 1;\n    size_t length = (chunks * chunksize);",
     "    size_t capacity = iodata->data.base ? (size_t)(iodata->end - iodata->data.base) : 0;\n    if (size > SIZE_MAX - capacity) return SDL_OutOfMemory();\n    size_t chunks = (capacity + size) / chunksize;\n    if (chunks == SIZE_MAX || chunks + 1 > SIZE_MAX / chunksize) return SDL_OutOfMemory();\n    size_t length = (chunks + 1) * chunksize;\n    size_t here_offset = iodata->data.base ? (size_t)(iodata->data.here - iodata->data.base) : 0;\n    size_t stop_offset = iodata->data.base ? (size_t)(iodata->data.stop - iodata->data.base) : 0;"),
    ("    Uint8 *base = (Uint8 *)SDL_realloc(iodata->data.base, length);\n    if (!base) {",
     "    void *replacement = iodata->data.base;\n    if (!E2D_ResizeIOPointer(SDL_GetIOProperties(iodata->stream), SDL_PROP_IOSTREAM_DYNAMIC_MEMORY_POINTER, &replacement, length)) {"),
    ("    size_t here_offset = (iodata->data.here - iodata->data.base);\n    size_t stop_offset = (iodata->data.stop - iodata->data.base);",
     "    Uint8 *base = (Uint8 *)replacement;"),
    ("    return SDL_SetPointerProperty(SDL_GetIOProperties(iodata->stream), SDL_PROP_IOSTREAM_DYNAMIC_MEMORY_POINTER, base);",
     "    return true;"),
    ("        iodata->stream = iostr;",
     "        iodata->stream = iostr;\n        SDL_PropertiesID props = SDL_GetIOProperties(iostr);\n        if (!props || !E2D_ReserveIOPointer(props, SDL_PROP_IOSTREAM_DYNAMIC_MEMORY_POINTER)) {\n            SDL_CloseIO(iostr);\n            return NULL;\n        }"),
)
ORIGINAL = """#ifdef HAVE_MALLOC
static void * SDLCALL real_malloc(size_t s) { return malloc(s); }
static void * SDLCALL real_calloc(size_t n, size_t s) { return calloc(n, s); }
static void * SDLCALL real_realloc(void *p, size_t s) { return realloc(p,s); }
static void   SDLCALL real_free(void *p) { free(p); }
#else
#define real_malloc dlmalloc
#define real_calloc dlcalloc
#define real_realloc dlrealloc
#define real_free dlfree
#endif"""
REPLACEMENT = """#include "memory.h"
#define real_malloc e2d_memory_malloc
#define real_calloc e2d_memory_calloc
#define real_realloc e2d_memory_realloc
#define real_free e2d_memory_free"""


def generate(source, destination):
    raw = source.read_bytes()
    text = raw.decode("utf-8")
    if source.name in ("SDL_properties.c", "SDL_iostream.c"):
        expected = PROPERTIES_SHA256 if source.name == "SDL_properties.c" else IO_SHA256
        if hashlib.sha256(raw).hexdigest() != expected:
            raise ValueError("SDL ownership source differs from the pinned archive")
        if source.name == "SDL_properties.c":
            text += IO_HELPERS
        else:
            for original, replacement in IO_EDITS:
                if text.count(original) != 1:
                    raise ValueError("SDL dynamic IO ownership definition changed")
                text = text.replace(original, replacement)
        destination.write_text(NOTICE + text, encoding="utf-8")
        return
    environment = source.name == "SDL_getenv.c"
    expected = ENV_SHA256 if environment else SOURCE_SHA256
    original = ENV_ORIGINAL if environment else ORIGINAL
    replacement = ENV_REPLACEMENT if environment else REPLACEMENT
    if hashlib.sha256(raw).hexdigest() != expected:
        raise ValueError("SDL allocation source differs from the pinned archive")
    if text.count(original) != (2 if environment else 1):
        raise ValueError("SDL default allocator definition changed")
    destination.write_text(NOTICE + text.replace(original, replacement), encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    generate(args.source, args.destination)
