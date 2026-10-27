#include <stdint.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

E2D_EXPORT SDL_GPUSampler *e2d_render_sampler(SDL_GPUDevice *device, uint32_t abi,
    const SDL_GPUSamplerCreateInfo *info, uint32_t u, uint32_t v, uint32_t border, uint32_t unnormalized)
{
    if (!device || !info || abi != 1 || u > 3 || v > 3 || border > 5 || unnormalized > 1) {
        SDL_SetError("Invalid native sampler arguments");
        return NULL;
    }
    if (SDL_GetVersion() != SDL_VERSIONNUM(3, 4, 18) ||
        SDL_strcmp(SDL_GetRevision(), "SDL-3.4.18-release-3.4.18") != 0 ||
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) {
        SDL_SetError("Native sampling requires the pinned Vulkan core");
        return NULL;
    }
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData;
    if (unnormalized && (info->min_filter != info->mag_filter || info->mipmap_mode != SDL_GPU_SAMPLERMIPMAPMODE_NEAREST ||
        info->min_lod != 0 || info->max_lod != 0 || info->enable_anisotropy || info->enable_compare || u < 2 || v < 2)) {
        SDL_SetError("Invalid unnormalized sampler state");
        return NULL;
    }
    if (info->enable_anisotropy && device->validate_feature_anisotropy_disabled) {
        SDL_SetError("Anisotropic sampling was disabled for this native device");
        return NULL;
    }
    float max_bias = renderer->physicalDeviceProperties.properties.limits.maxSamplerLodBias;
    if (info->mip_lod_bias < -max_bias || info->mip_lod_bias > max_bias) {
        SDL_SetError("Sampler LOD bias exceeds the native device limit");
        return NULL;
    }
    if (!unnormalized && u != 3 && v != 3) return SDL_CreateGPUSampler(device, info);

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
        .borderColor = (VkBorderColor)border,
        .unnormalizedCoordinates = unnormalized
    };
    VkSampler handle;
    VkResult result = create(renderer->logicalDevice, &native, NULL, &handle);
    if (result != VK_SUCCESS) {
        SDL_ReleaseGPUSampler(device, sampler);
        SDL_SetError("Vulkan sampler creation failed (%d)", result);
        return NULL;
    }
    // The SDL wrapper has not escaped; retain its reference counting and deferred release.
    VulkanSampler *wrapper = (VulkanSampler *)sampler;
    destroy(renderer->logicalDevice, wrapper->sampler, NULL);
    wrapper->sampler = handle;
    return sampler;
}

E2D_EXPORT int e2d_render_sampler_view(SDL_GPUDevice *device, uint32_t abi, SDL_GPUTexture *texture)
{
    if (!device || !texture || abi != 1) return -1;
    if (SDL_GetVersion() != SDL_VERSIONNUM(3, 4, 18) ||
        SDL_strcmp(SDL_GetRevision(), "SDL-3.4.18-release-3.4.18") != 0 ||
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return -1;
    const TextureCommonHeader *header = (const TextureCommonHeader *)texture;
    return header->info.type == SDL_GPU_TEXTURETYPE_2D && header->info.layer_count_or_depth == 1 &&
        header->info.num_levels == 1 && header->info.sample_count == SDL_GPU_SAMPLECOUNT_1;
}
