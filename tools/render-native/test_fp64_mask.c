// Test-only Linux SDL3_DYNAMIC_API shim: refuse optional shaderFloat64 device requests.
#include <SDL3/SDL.h>
#include <dlfcn.h>
#include <stdio.h>

enum {
#define SDL_DYNAPI_PROC(rc, name, params, args, ret) slot_##name,
#include "SDL_dynapi_procs.h"
#undef SDL_DYNAPI_PROC
    slot_count
};
static SDL_GPUDevice *(SDLCALL *actual_create)(SDL_PropertiesID);
static void *(SDLCALL *actual_pointer)(SDL_PropertiesID, const char *, void *);
static bool (SDLCALL *actual_error)(const char *, ...);

static SDL_GPUDevice *SDLCALL masked_create(SDL_PropertiesID props)
{
    const SDL_GPUVulkanOptions *options = actual_pointer(props, SDL_PROP_GPU_DEVICE_CREATE_VULKAN_OPTIONS_POINTER, NULL);
    const Uint32 *features = options ? options->vulkan_10_physical_device_features : NULL;
    if (features && features[39]) {
        fprintf(stderr, "MASKED native shaderFloat64 request\n");
        actual_error("Test native device has no shaderFloat64 feature");
        return NULL;
    }
    return actual_create(props);
}

Sint32 SDLCALL SDL_DYNAPI_entry(Uint32 abi, void *table, Uint32 size)
{
    void *library = dlopen("libSDL3.so.0", RTLD_NOW | RTLD_NOLOAD);
    Sint32 (SDLCALL *entry)(Uint32, void *, Uint32) = library ? dlsym(library, "SDL_DYNAPI_entry") : NULL;
    if (!entry || size != slot_count * sizeof(void *)) return -1;
    Sint32 result = entry(abi, table, size);
    if (result < 0) return result;
    void **functions = table;
    actual_create = functions[slot_SDL_CreateGPUDeviceWithProperties];
    actual_pointer = functions[slot_SDL_GetPointerProperty];
    actual_error = functions[slot_SDL_SetError];
    functions[slot_SDL_CreateGPUDeviceWithProperties] = masked_create;
    return 0;
}
