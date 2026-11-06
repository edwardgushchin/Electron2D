#include "pinned-core.h"
#include <stdint.h>
#include <stddef.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

_Static_assert(offsetof(VkPhysicalDeviceShaderAtomicFloatFeaturesEXT, shaderBufferFloat64Atomics) == offsetof(VkPhysicalDeviceShaderAtomicFloatFeaturesEXT, shaderBufferFloat32Atomics) + 2 * sizeof(VkBool32), "double buffer feature words");
_Static_assert(offsetof(VkPhysicalDeviceShaderAtomicFloatFeaturesEXT, shaderSharedFloat64AtomicAdd) == offsetof(VkPhysicalDeviceShaderAtomicFloatFeaturesEXT, shaderBufferFloat32Atomics) + 7 * sizeof(VkBool32), "double shared feature words");
_Static_assert(VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_SHADER_ATOMIC_FLOAT_FEATURES_EXT == 1000260000, "floating atomic feature type");
_Static_assert(offsetof(VkPhysicalDeviceShaderAtomicFloatFeaturesEXT, shaderImageFloat32AtomicAdd) == offsetof(VkPhysicalDeviceShaderAtomicFloatFeaturesEXT, shaderBufferFloat32Atomics) + 9 * sizeof(VkBool32), "floating atomic feature words");

E2D_EXPORT int e2d_render_float_atomic_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || (abi != 1 && abi != 2)) return -1;
    if (!e2d_render_pinned_core() || SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return 0;
    const VulkanRenderer *r = (const VulkanRenderer *)device->driverData;
    if (r->physicalDeviceProperties.properties.apiVersion < VK_API_VERSION_1_2) return 0;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures2 query = proc ? (PFN_vkGetPhysicalDeviceFeatures2)proc(r->instance, "vkGetPhysicalDeviceFeatures2") : NULL;
    PFN_vkEnumerateDeviceExtensionProperties enumerate = proc ? (PFN_vkEnumerateDeviceExtensionProperties)proc(r->instance, "vkEnumerateDeviceExtensionProperties") : NULL;
    uint32_t count = 0;
    if (!query || !enumerate || enumerate(r->physicalDevice, NULL, &count, NULL) != VK_SUCCESS || !count || count > 4096) return 0;
    VkExtensionProperties *extensions = SDL_calloc(count, sizeof(*extensions));
    if (!extensions) return abi == 1 ? -1 : -2;
    bool found = false;
    if (enumerate(r->physicalDevice, NULL, &count, extensions) == VK_SUCCESS)
        for (uint32_t i = 0; i < count; ++i) found |= SDL_strcmp(extensions[i].extensionName, VK_EXT_SHADER_ATOMIC_FLOAT_EXTENSION_NAME) == 0;
    SDL_free(extensions);
    if (!found) return 0;
    VkPhysicalDeviceShaderAtomicFloatFeaturesEXT atomics = {.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_SHADER_ATOMIC_FLOAT_FEATURES_EXT};
    VkPhysicalDeviceFeatures2 features = {.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_FEATURES_2, .pNext = &atomics};
    query(r->physicalDevice, &features);
    // Physical support only; the managed factory records the successfully enabled logical mask separately.
    return (atomics.shaderBufferFloat32Atomics ? 1 : 0) | (atomics.shaderBufferFloat32AtomicAdd ? 2 : 0) |
        (atomics.shaderSharedFloat32Atomics ? 4 : 0) | (atomics.shaderSharedFloat32AtomicAdd ? 8 : 0) |
        (atomics.shaderImageFloat32Atomics ? 16 : 0) | (atomics.shaderImageFloat32AtomicAdd ? 32 : 0) |
        (abi == 2 ? (atomics.shaderBufferFloat64Atomics ? 64 : 0) | (atomics.shaderBufferFloat64AtomicAdd ? 128 : 0) |
            (atomics.shaderSharedFloat64Atomics ? 256 : 0) | (atomics.shaderSharedFloat64AtomicAdd ? 512 : 0) : 0);
}
