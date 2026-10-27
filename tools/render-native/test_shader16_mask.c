// Test-only Linux SDL3_DYNAMIC_API shim: refuse selected 16-bit shader arithmetic/storage or Vulkan 1.2 requests.
#include <SDL3/SDL.h>
#include <dlfcn.h>
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
static bool (SDLCALL *actual_error)(const char *, ...);

static SDL_GPUDevice *SDLCALL masked_create(SDL_PropertiesID props)
{
    const SDL_GPUVulkanOptions *options = actual_pointer(props, SDL_PROP_GPU_DEVICE_CREATE_VULKAN_OPTIONS_POINTER, NULL);
    typedef struct Features { Uint32 type; const void *next; Uint32 flags[47]; } Features;
    const char *mask = getenv("ELECTRON2D_TEST_MASK_SHADER16"); if (!mask) mask = "all";
    const Uint32 *core = options ? options->vulkan_10_physical_device_features : NULL;
    Uint32 requested = core && core[41] ? 32 : 0;
    for (const Features *f = options ? options->feature_list : NULL; f; f = f->next) {
        if (f->type == 51 && f->flags[7]) requested |= 16;
        if (f->type == 49) for (unsigned i = 0; i < 4; ++i) if (f->flags[i]) requested |= 1u << i;
    }
    Uint32 refused = strcmp(mask, "integer") == 0 ? 32 : strcmp(mask, "arithmetic") == 0 ? 16 : strcmp(mask, "buffer") == 0 ? 3 :
        strcmp(mask, "uniform") == 0 ? 2 : strcmp(mask, "push") == 0 ? 4 : strcmp(mask, "io") == 0 ? 8 : 63;
    if ((requested & refused) || (strcmp(mask, "api") == 0 && options && options->vulkan_api_version >= ((1u << 22) | (2u << 12)))) {
        fprintf(stderr, "MASKED native shader16 request %s flags=%u\n", mask, requested);
        actual_error("Test native device refuses the requested 16-bit shader feature"); return NULL;
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
