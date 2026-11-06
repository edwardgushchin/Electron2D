// Test-only SDL3_DYNAMIC_API shim: refuse the floating atomic feature chain before native device creation.
#include <SDL3/SDL.h>
#if defined(_WIN32)
#include <windows.h>
#define TEST_EXPORT __declspec(dllexport)
#else
#include <dlfcn.h>
#define TEST_EXPORT __attribute__((visibility("default")))
#endif
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

enum {
#define SDL_DYNAPI_PROC(rc, name, params, args, ret) slot_##name,
#include "SDL_dynapi_procs.h"
#undef SDL_DYNAPI_PROC
    slot_count
};
static SDL_GPUDevice *(SDLCALL *actual_create)(SDL_PropertiesID);
static void *(SDLCALL *actual_pointer)(SDL_PropertiesID, const char *, void *);
static Sint64 (SDLCALL *actual_number)(SDL_PropertiesID, const char *, Sint64);
static bool (SDLCALL *actual_error)(const char *, ...);

static SDL_GPUDevice *SDLCALL masked_create(SDL_PropertiesID props)
{
    const SDL_GPUVulkanOptions *options = actual_pointer(props, SDL_PROP_GPU_DEVICE_CREATE_VULKAN_OPTIONS_POINTER, NULL);
    const char *mask = getenv("ELECTRON2D_TEST_FLOAT_ATOMIC_MASK");
    const Uint32 *core = options ? options->vulkan_10_physical_device_features : NULL;
    if (mask && strcmp(mask, "precision") == 0 && core && core[39]) {
        actual_error("Test native device has no shaderFloat64"); return NULL;
    }
    typedef struct Feature { Uint32 type; const struct Feature *next; Uint32 flags[12]; } Feature;
    const Feature *feature = options ? options->feature_list : NULL;
    for (int depth = 0; feature && depth < 16; ++depth, feature = feature->next) {
        if (feature->type == 1000260000 && (!mask || strcmp(mask, "all") == 0 ||
            (strcmp(mask, "wide") == 0 && (feature->flags[2] || feature->flags[3] || feature->flags[6] || feature->flags[7])))) {
            fprintf(stderr, "MASKED native floating atomic feature request\n");
            actual_error("Test native device has no floating atomic extension");
            return NULL;
        }
    }
    return actual_create(props);
}

static Sint64 SDLCALL masked_number(SDL_PropertiesID props, const char *name, Sint64 fallback)
{
    Sint64 value = actual_number(props, name, fallback);
    const char *mask = getenv("ELECTRON2D_TEST_FLOAT_ATOMIC_MASK");
    if (mask && strcmp(name, "Electron2D.gpu.float64_atomics") == 0) {
        if (strcmp(mask, "base") == 0) return value & 5;
        if (strcmp(mask, "add") == 0) return value & 10;
        if (strcmp(mask, "buffer") == 0) return value & 3;
        if (strcmp(mask, "shared") == 0) return value & 12;
    }
    return value;
}

TEST_EXPORT Sint32 SDLCALL SDL_DYNAPI_entry(Uint32 abi, void *table, Uint32 size)
{
#if defined(_WIN32)
    HMODULE library = GetModuleHandleW(L"SDL3.dll");
    Sint32 (SDLCALL *entry)(Uint32, void *, Uint32) = library ? (void *)GetProcAddress(library, "SDL_DYNAPI_entry") : NULL;
#else
    void *library = dlopen("libSDL3.so.0", RTLD_NOW | RTLD_NOLOAD);
    Sint32 (SDLCALL *entry)(Uint32, void *, Uint32) = library ? dlsym(library, "SDL_DYNAPI_entry") : NULL;
#endif
    if (!entry || size != slot_count * sizeof(void *)) return -1;
    Sint32 result = entry(abi, table, size);
    if (result < 0) return result;
    void **functions = table;
    actual_create = functions[slot_SDL_CreateGPUDeviceWithProperties];
    actual_pointer = functions[slot_SDL_GetPointerProperty];
    actual_error = functions[slot_SDL_SetError];
    actual_number = functions[slot_SDL_GetNumberProperty];
    functions[slot_SDL_GetNumberProperty] = masked_number;
    functions[slot_SDL_CreateGPUDeviceWithProperties] = masked_create;
    return 0;
}
