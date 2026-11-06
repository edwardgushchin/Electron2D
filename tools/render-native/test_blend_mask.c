// Test-only logical device negotiation refusal; physical queries remain unchanged.
#include <SDL3/SDL.h>
#include <windows.h>
#include <stdlib.h>

enum {
#define SDL_DYNAPI_PROC(rc, name, params, args, ret) slot_##name,
#include "SDL_dynapi_procs.h"
#undef SDL_DYNAPI_PROC
    slot_count
};
static void *(SDLCALL *actual_pointer)(SDL_PropertiesID, const char *, void *);
static SDL_GPUDevice *(SDLCALL *actual_create)(SDL_PropertiesID);
static bool (SDLCALL *actual_error)(const char *, ...);
static SDL_GPUDevice *SDLCALL masked_create(SDL_PropertiesID props)
{
    const SDL_GPUVulkanOptions *options = actual_pointer(props, SDL_PROP_GPU_DEVICE_CREATE_VULKAN_OPTIONS_POINTER, NULL);
    if (getenv("ELECTRON2D_BLEND_REFUSAL") && options && options->vulkan_10_physical_device_features && ((const Uint32 *)options->vulkan_10_physical_device_features)[7]) {
        actual_error("Test device refuses optional dualSrcBlend"); return NULL;
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
    actual_pointer = functions[slot_SDL_GetPointerProperty]; actual_create = functions[slot_SDL_CreateGPUDeviceWithProperties]; actual_error = functions[slot_SDL_SetError];
    functions[slot_SDL_CreateGPUDeviceWithProperties] = masked_create; return 0;
}
