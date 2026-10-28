#define VK_NO_PROTOTYPES
#include <vulkan/vulkan.h>
#include <SDL3/SDL_vulkan.h>
#include <dlfcn.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

static PFN_vkGetInstanceProcAddr actual_instance;
static PFN_vkGetDeviceProcAddr actual_device;
static VkResult VKAPI_CALL failed_pipeline(VkDevice device, VkPipelineCache cache, uint32_t count, const VkGraphicsPipelineCreateInfo *info, const VkAllocationCallbacks *allocator, VkPipeline *pipeline)
{
    (void)device; (void)cache; (void)count; (void)info; (void)allocator; (void)pipeline;
    fprintf(stderr, "MASKED native raster-discard pipeline creation failure\n"); return VK_ERROR_OUT_OF_DEVICE_MEMORY;
}
static PFN_vkVoidFunction VKAPI_CALL masked_device(VkDevice device, const char *name)
{
    if (strcmp(name, "vkCreateGraphicsPipelines") == 0) {
        if (getenv("ELECTRON2D_TEST_RASTER_FAILURE")) return (PFN_vkVoidFunction)failed_pipeline;
        fprintf(stderr, "MASKED native raster-discard pipeline procedure\n"); return NULL;
    }
    return actual_device(device, name);
}
static PFN_vkVoidFunction VKAPI_CALL masked_instance(VkInstance instance, const char *name)
{
    PFN_vkVoidFunction result = actual_instance(instance, name);
    if (result && strcmp(name, "vkGetDeviceProcAddr") == 0) {
        actual_device = (PFN_vkGetDeviceProcAddr)result;
        return (PFN_vkVoidFunction)masked_device;
    }
    return result;
}
SDL_FunctionPointer SDLCALL SDL_Vulkan_GetVkGetInstanceProcAddr(void)
{
    SDL_FunctionPointer (SDLCALL *getter)(void) = dlsym(RTLD_NEXT, "SDL_Vulkan_GetVkGetInstanceProcAddr");
    if (!getter) {
        void *library = dlopen("libSDL3.so.0", RTLD_NOW | RTLD_NOLOAD);
        if (library) {
            getter = dlsym(library, "SDL_Vulkan_GetVkGetInstanceProcAddr");
            dlclose(library);
        }
    }
    if (!getter) return NULL;
    actual_instance = (PFN_vkGetInstanceProcAddr)getter();
    return actual_instance ? (SDL_FunctionPointer)masked_instance : NULL;
}
