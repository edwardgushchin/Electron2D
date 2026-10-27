// Test-only Linux preload: mask ordinary linear features while preserving sampled support.
#define VK_NO_PROTOTYPES
#include <vulkan/vulkan.h>
#include <SDL3/SDL_vulkan.h>
#include <dlfcn.h>
#include <string.h>

static PFN_vkGetInstanceProcAddr actual_instance;
static PFN_vkGetPhysicalDeviceFormatProperties actual_properties;
static void VKAPI_CALL masked_properties(VkPhysicalDevice device, VkFormat format, VkFormatProperties *properties)
{
    actual_properties(device, format, properties);
    if (format == VK_FORMAT_R32G32B32A32_SFLOAT || format == VK_FORMAT_D32_SFLOAT)
        properties->optimalTilingFeatures &= ~VK_FORMAT_FEATURE_SAMPLED_IMAGE_FILTER_LINEAR_BIT;
}
static PFN_vkVoidFunction VKAPI_CALL masked_instance(VkInstance instance, const char *name)
{
    PFN_vkVoidFunction result = actual_instance(instance, name);
    if (result && strcmp(name, "vkGetPhysicalDeviceFormatProperties") == 0) {
        actual_properties = (PFN_vkGetPhysicalDeviceFormatProperties)result;
        return (PFN_vkVoidFunction)masked_properties;
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
