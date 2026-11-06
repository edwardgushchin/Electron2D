// Test-only SDL dynamic API override for independent resolve gates and optional API negotiation.
#include <SDL3/SDL.h>
#include <windows.h>
#include <stdlib.h>
#include <string.h>
enum {
#define SDL_DYNAPI_PROC(rc, name, params, args, ret) slot_##name,
#include "SDL_dynapi_procs.h"
#undef SDL_DYNAPI_PROC
    slot_count
};
static Sint64 (SDLCALL *actual_number)(SDL_PropertiesID, const char *, Sint64);
static void *(SDLCALL *actual_pointer)(SDL_PropertiesID, const char *, void *);
static SDL_GPUDevice *(SDLCALL *actual_create)(SDL_PropertiesID);
static bool (SDLCALL *actual_error)(const char *, ...);
static Sint64 SDLCALL masked_number(SDL_PropertiesID props, const char *name, Sint64 fallback)
{
    Sint64 value = actual_number(props, name, fallback);
    const char *mask = getenv("ELECTRON2D_DEPTH_RESOLVE_REFUSAL");
    if (mask && strcmp(name, "Electron2D.gpu.depth_resolve") == 0) return strcmp(mask, "stencil") == 0 ? value & 1 : 0;
    return value;
}
static SDL_GPUDevice *SDLCALL masked_create(SDL_PropertiesID props)
{
    const char *mask = getenv("ELECTRON2D_DEPTH_RESOLVE_REFUSAL");
    const SDL_GPUVulkanOptions *options = actual_pointer(props, SDL_PROP_GPU_DEVICE_CREATE_VULKAN_OPTIONS_POINTER, NULL);
    if (mask && strcmp(mask, "api") == 0 && options && options->vulkan_api_version >= ((1u << 22) | (2u << 12))) {
        actual_error("Test device refuses optional Vulkan 1.2"); return NULL;
    }
    return actual_create(props);
}
__declspec(dllexport) Sint32 SDLCALL SDL_DYNAPI_entry(Uint32 abi, void *table, Uint32 size)
{
    HMODULE library = GetModuleHandleW(L"SDL3.dll");
    Sint32 (SDLCALL *entry)(Uint32, void *, Uint32) = library ? (void *)GetProcAddress(library, "SDL_DYNAPI_entry") : NULL;
    if (!entry || size != slot_count * sizeof(void *)) return -1;
    Sint32 result = entry(abi, table, size); if (result < 0) return result;
    void **functions = table;
    actual_number = functions[slot_SDL_GetNumberProperty]; actual_pointer = functions[slot_SDL_GetPointerProperty];
    actual_create = functions[slot_SDL_CreateGPUDeviceWithProperties]; actual_error = functions[slot_SDL_SetError];
    functions[slot_SDL_GetNumberProperty] = masked_number; functions[slot_SDL_CreateGPUDeviceWithProperties] = masked_create;
    return 0;
}
