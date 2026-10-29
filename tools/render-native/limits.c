#include "pinned-core.h"
#include <stdint.h>
#include "memory-layout.h"

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

E2D_EXPORT int e2d_render_limits(SDL_GPUDevice *device, uint32_t abi, uint64_t *out, uint32_t count)
{
    if (!device || !out || abi != 1 || count != 37) return -1;
    if (!e2d_render_pinned_core() ||
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    const VkPhysicalDeviceLimits *p = &renderer->physicalDeviceProperties.properties.limits;
    const uint64_t values[37] = {
        p->maxBoundDescriptorSets, p->maxColorAttachments,
        p->maxDescriptorSetSampledImages, p->maxDescriptorSetSamplers,
        p->maxDescriptorSetStorageBuffers, p->maxDescriptorSetStorageImages,
        p->maxDescriptorSetUniformBuffers, p->maxDrawIndexedIndexValue,
        p->maxFramebufferHeight, p->maxFramebufferWidth, p->maxImageArrayLayers,
        0, p->maxImageDimension2D, 0, 0,
        p->maxPerStageDescriptorSampledImages, p->maxPerStageDescriptorSamplers,
        p->maxPerStageDescriptorStorageBuffers, p->maxPerStageDescriptorStorageImages,
        p->maxPerStageDescriptorUniformBuffers, p->maxPushConstantsSize,
        p->maxUniformBufferRange, p->maxVertexInputAttributeOffset,
        p->maxVertexInputAttributes, p->maxVertexInputBindings, p->maxVertexInputBindingStride,
        p->minUniformBufferOffsetAlignment, p->maxComputeSharedMemorySize,
        p->maxComputeWorkGroupCount[0], p->maxComputeWorkGroupCount[1], p->maxComputeWorkGroupCount[2],
        p->maxComputeWorkGroupInvocations,
        p->maxComputeWorkGroupSize[0], p->maxComputeWorkGroupSize[1], p->maxComputeWorkGroupSize[2],
        p->maxViewportDimensions[0], p->maxViewportDimensions[1]
    };
    SDL_memcpy(out, values, sizeof(values));
    return 1;
}
