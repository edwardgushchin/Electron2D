#define VK_NO_PROTOTYPES
#include <vulkan/vulkan.h>
#include <SDL3/SDL_vulkan.h>
#include <dlfcn.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

static PFN_vkGetInstanceProcAddr actual_instance;
static PFN_vkGetPhysicalDeviceFeatures actual_features;
static void VKAPI_CALL masked_features(VkPhysicalDevice device, VkPhysicalDeviceFeatures *features)
{
    actual_features(device, features);
    static bool reported;
    if (!reported) { fprintf(stderr, "MASKED native wide-line feature query\n"); reported = true; }
    features->wideLines = VK_FALSE;
}
static PFN_vkVoidFunction VKAPI_CALL masked_instance(VkInstance instance, const char *name)
{
    PFN_vkVoidFunction result = actual_instance(instance, name);
    if (result && strcmp(name, "vkGetPhysicalDeviceFeatures") == 0) {
        actual_features = (PFN_vkGetPhysicalDeviceFeatures)result;
        return (PFN_vkVoidFunction)masked_features;
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
