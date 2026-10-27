#include <stdint.h>
#define E2D_RENDER_FILTERING
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

static bool compatible(SDL_GPUDevice *device)
{
    return device && SDL_GetVersion() == SDL_VERSIONNUM(3, 4, 18) &&
        SDL_strcmp(SDL_GetRevision(), "SDL-3.4.18-release-3.4.18") == 0 &&
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") == 0;
}

E2D_EXPORT int e2d_render_filter_support(SDL_GPUDevice *device, uint32_t abi, uint8_t *out, uint32_t count)
{
    if (!device || !out || abi != 1 || count != SDL_arraysize(SDLToVK_TextureFormat)) return -1;
    if (!compatible(device)) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFormatProperties properties = proc ? (PFN_vkGetPhysicalDeviceFormatProperties)proc(renderer->instance, "vkGetPhysicalDeviceFormatProperties") : NULL;
    if (!properties) return -1;
    uint8_t values[SDL_arraysize(SDLToVK_TextureFormat)] = {0};
    for (uint32_t i = 1; i < count; ++i) {
        SDL_GPUTextureUsageFlags usage = SDL_GPU_TEXTUREUSAGE_SAMPLER;
        if (i >= SDL_GPU_TEXTUREFORMAT_D16_UNORM && i <= SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT) usage |= SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET;
        if (SDLToVK_TextureFormat[i] == VK_FORMAT_UNDEFINED ||
            !SDL_GPUTextureSupportsFormat(device, (SDL_GPUTextureFormat)i, SDL_GPU_TEXTURETYPE_2D, usage)) continue;
        VkFormatProperties format = {0};
        properties(renderer->physicalDevice, SDLToVK_TextureFormat[i], &format);
        if ((format.optimalTilingFeatures & VK_FORMAT_FEATURE_SAMPLED_IMAGE_BIT) != 0) {
            values[i] = 1;
            if ((format.optimalTilingFeatures & VK_FORMAT_FEATURE_SAMPLED_IMAGE_FILTER_LINEAR_BIT) != 0) values[i] |= 2;
        }
    }
    SDL_memcpy(out, values, sizeof(values));
    return 1;
}

E2D_EXPORT int e2d_render_texture_format(SDL_GPUDevice *device, uint32_t abi, SDL_GPUTexture *texture)
{
    if (!device || !texture || abi != 1 || !compatible(device)) return -1;
    const TextureCommonHeader *header = (const TextureCommonHeader *)texture;
    uint32_t format = header->info.format;
    return format > 0 && format < SDL_arraysize(SDLToVK_TextureFormat) ? (int)format : -1;
}

E2D_EXPORT int e2d_render_shader16_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (!compatible(device)) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    if (renderer->physicalDeviceProperties.properties.apiVersion < VK_API_VERSION_1_2) return 0;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures2 query = proc ? (PFN_vkGetPhysicalDeviceFeatures2)proc(renderer->instance, "vkGetPhysicalDeviceFeatures2") : NULL;
    if (!query && proc) query = (PFN_vkGetPhysicalDeviceFeatures2)proc(renderer->instance, "vkGetPhysicalDeviceFeatures2KHR");
    if (!query) return 0;
    VkPhysicalDeviceVulkan12Features f12 = {.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_VULKAN_1_2_FEATURES};
    VkPhysicalDeviceVulkan11Features f11 = {.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_VULKAN_1_1_FEATURES, .pNext = &f12};
    VkPhysicalDeviceFeatures2 features = {.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_FEATURES_2, .pNext = &f11};
    query(renderer->physicalDevice, &features);
    return (f11.storageBuffer16BitAccess ? 1 : 0) | (f11.uniformAndStorageBuffer16BitAccess ? 2 : 0) |
        (f11.storagePushConstant16 ? 4 : 0) | (f11.storageInputOutput16 ? 8 : 0) | (f12.shaderFloat16 ? 16 : 0);
}
