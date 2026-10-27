#include <stdint.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

E2D_EXPORT SDL_GPUSampler *e2d_render_sampler_border(SDL_GPUDevice *device, uint32_t abi,
    const SDL_GPUSamplerCreateInfo *info, uint32_t u, uint32_t v, uint32_t border)
{
    if (!device || !info || abi != 1 || u > 3 || v > 3 || (u != 3 && v != 3) || border > 5) {
        SDL_SetError("Invalid border sampler arguments");
        return NULL;
    }
    if (SDL_GetVersion() != SDL_VERSIONNUM(3, 4, 18) ||
        SDL_strcmp(SDL_GetRevision(), "SDL-3.4.18-release-3.4.18") != 0 ||
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) {
        SDL_SetError("Border sampling requires the pinned Vulkan core");
        return NULL;
    }
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr instance_proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr device_proc = instance_proc ? (PFN_vkGetDeviceProcAddr)instance_proc(renderer->instance, "vkGetDeviceProcAddr") : NULL;
    PFN_vkCreateSampler create = device_proc ? (PFN_vkCreateSampler)device_proc(renderer->logicalDevice, "vkCreateSampler") : NULL;
    PFN_vkDestroySampler destroy = device_proc ? (PFN_vkDestroySampler)device_proc(renderer->logicalDevice, "vkDestroySampler") : NULL;
    if (!create || !destroy) {
        SDL_SetError("Vulkan sampler functions are unavailable");
        return NULL;
    }
    SDL_GPUSampler *sampler = SDL_CreateGPUSampler(device, info);
    if (!sampler) return NULL;
    VkSamplerCreateInfo native = {
        .sType = VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO,
        .minFilter = (VkFilter)info->min_filter,
        .magFilter = (VkFilter)info->mag_filter,
        .mipmapMode = (VkSamplerMipmapMode)info->mipmap_mode,
        .addressModeU = (VkSamplerAddressMode)u,
        .addressModeV = (VkSamplerAddressMode)v,
        .addressModeW = VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE,
        .mipLodBias = info->mip_lod_bias,
        .anisotropyEnable = info->enable_anisotropy,
        .maxAnisotropy = info->max_anisotropy,
        .compareEnable = info->enable_compare,
        .compareOp = (VkCompareOp)(info->compare_op - 1),
        .minLod = info->min_lod,
        .maxLod = info->max_lod,
        .borderColor = (VkBorderColor)border
    };
    VkSampler handle;
    VkResult result = create(renderer->logicalDevice, &native, NULL, &handle);
    if (result != VK_SUCCESS) {
        SDL_ReleaseGPUSampler(device, sampler);
        SDL_SetError("Vulkan border sampler creation failed (%d)", result);
        return NULL;
    }
    // The SDL wrapper has not escaped; retain its reference counting and deferred release.
    VulkanSampler *wrapper = (VulkanSampler *)sampler;
    destroy(renderer->logicalDevice, wrapper->sampler, NULL);
    wrapper->sampler = handle;
    return sampler;
}
