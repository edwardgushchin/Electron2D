#include "pinned-core.h"
#include "memory-layout.h"

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

enum E2DNameKind {
    E2D_NAME_BUFFER = 1, E2D_NAME_IMAGE, E2D_NAME_IMAGE_VIEW, E2D_NAME_SAMPLER,
    E2D_NAME_SHADER, E2D_NAME_COMPUTE_SHADER, E2D_NAME_COMPUTE_PIPELINE, E2D_NAME_GRAPHICS_PIPELINE
};

static int label(VulkanRenderer *renderer, VkObjectType type, uint64_t handle, const char *name)
{
    if (!handle) return -1;
    VkDebugUtilsObjectNameInfoEXT info = {
        .sType = VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT,
        .objectType = type,
        .objectHandle = handle,
        .pObjectName = *name ? name : NULL
    };
    return renderer->vkSetDebugUtilsObjectNameEXT(renderer->logicalDevice, &info) == VK_SUCCESS ? 1 : -2;
}

static int label_resource(VulkanRenderer *renderer, uint32_t kind, void *resource, const char *name)
{
    switch (kind) {
        case E2D_NAME_BUFFER: {
            VulkanBufferContainer *container = resource;
            if (!container->bufferCount) return -1;
            for (uint32_t i = 0; i < container->bufferCount; ++i) {
                int result = label(renderer, VK_OBJECT_TYPE_BUFFER, (uint64_t)container->buffers[i]->buffer, name);
                if (result != 1) return result;
            }
            return 1;
        }
        case E2D_NAME_IMAGE: {
            VulkanTextureContainer *container = resource;
            if (!container->textureCount) return -1;
            for (uint32_t i = 0; i < container->textureCount; ++i) {
                int result = label(renderer, VK_OBJECT_TYPE_IMAGE, (uint64_t)container->textures[i]->image, name);
                if (result != 1) return result;
            }
            return 1;
        }
        case E2D_NAME_IMAGE_VIEW:
            return label(renderer, VK_OBJECT_TYPE_IMAGE_VIEW,
                (uint64_t)((VulkanTextureContainer *)resource)->activeTexture->fullView, name);
        case E2D_NAME_SAMPLER:
            return label(renderer, VK_OBJECT_TYPE_SAMPLER, (uint64_t)((VulkanSampler *)resource)->sampler, name);
        case E2D_NAME_SHADER:
            return label(renderer, VK_OBJECT_TYPE_SHADER_MODULE, (uint64_t)((VulkanShader *)resource)->shaderModule, name);
        case E2D_NAME_COMPUTE_SHADER:
            return label(renderer, VK_OBJECT_TYPE_SHADER_MODULE, (uint64_t)((VulkanComputePipeline *)resource)->shaderModule, name);
        case E2D_NAME_COMPUTE_PIPELINE:
            return label(renderer, VK_OBJECT_TYPE_PIPELINE, (uint64_t)((VulkanComputePipeline *)resource)->pipeline, name);
        case E2D_NAME_GRAPHICS_PIPELINE:
            return label(renderer, VK_OBJECT_TYPE_PIPELINE, (uint64_t)((VulkanGraphicsPipeline *)resource)->pipeline, name);
        default: return -1;
    }
}

E2D_EXPORT int e2d_render_resource_name(SDL_GPUDevice *device, uint32_t abi, uint32_t kind, void *resource, const char *name)
{
    if (!device || abi != 1 || !resource || !name) return -1;
    const char *driver = SDL_GetGPUDeviceDriver(device);
    if (!e2d_render_pinned_core() || !driver || SDL_strcmp(driver, "vulkan") != 0) return 0;
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData;
    if (!renderer || !renderer->supportsDebugUtils || !renderer->vkSetDebugUtilsObjectNameEXT) return 0;
    if (kind < E2D_NAME_BUFFER || kind > E2D_NAME_IMAGE_VIEW)
        return label_resource(renderer, kind, resource, name);

    // Defragmentation can replace physical buffer and image handles inside a live container.
    SDL_LockRWLockForReading(renderer->defragLock);
    int result = label_resource(renderer, kind, resource, name);
    SDL_UnlockRWLock(renderer->defragLock);
    return result;
}
