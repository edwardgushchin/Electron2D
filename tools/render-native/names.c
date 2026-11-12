#include "pinned-core.h"
#include "d3d12-views.h"
#define COBJMACROS
#define E2D_RENDER_DRIVER_D3D12
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

#if defined(SDL_PLATFORM_WIN32)
static int label_d3d12_object(ID3D12Object *object, const wchar_t *name)
{
    if (!object) return -1;
    return SUCCEEDED(ID3D12Object_SetName(object, name)) ? 1 : -2;
}

static int label_d3d12_resource(uint32_t kind, void *resource, const wchar_t *name)
{
    switch (kind) {
        case E2D_NAME_BUFFER: {
            D3D12BufferContainer *container = resource;
            if (!container->bufferCount) return -1;
            for (uint32_t i = 0; i < container->bufferCount; ++i) {
                int result = label_d3d12_object((ID3D12Object *)container->buffers[i]->handle, name);
                if (result != 1) return result;
            }
            return 1;
        }
        case E2D_NAME_IMAGE: {
            D3D12TextureContainer *container = resource;
            if (!container->textureCount) return -1;
            for (uint32_t i = 0; i < container->textureCount; ++i) {
                int result = label_d3d12_object((ID3D12Object *)container->textures[i]->resource, name);
                if (result != 1) return result;
            }
            return 1;
        }
        case E2D_NAME_COMPUTE_PIPELINE:
            return label_d3d12_object((ID3D12Object *)((struct D3D12ComputePipeline *)resource)->pipelineState, name);
        case E2D_NAME_GRAPHICS_PIPELINE:
            return label_d3d12_object((ID3D12Object *)((struct D3D12GraphicsPipeline *)resource)->pipelineState, name);
        default: return 0;
    }
}
#endif

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

static VulkanRenderer *debug_renderer(SDL_GPUDevice *device)
{
    const char *driver = SDL_GetGPUDeviceDriver(device);
    if (!e2d_render_pinned_core() || !driver || SDL_strcmp(driver, "vulkan") != 0) return NULL;
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData;
    return renderer && renderer->supportsDebugUtils ? renderer : NULL;
}

E2D_EXPORT int e2d_render_resource_name(SDL_GPUDevice *device, uint32_t abi, uint32_t kind, void *resource, const char *name)
{
    if (!device || abi != 1 || !resource || !name) return -1;
#if defined(SDL_PLATFORM_WIN32)
    if (e2d_d3d12_device(device)) {
        if (kind >= E2D_NAME_IMAGE_VIEW && kind <= E2D_NAME_COMPUTE_SHADER) return 0;
        wchar_t *wide = NULL;
        if (*name) {
            wide = (wchar_t *)SDL_iconv_string("UTF-16LE", "UTF-8", name, SDL_strlen(name) + 1);
            if (!wide) return -2;
        }
        int result = label_d3d12_resource(kind, resource, wide ? wide : L"");
        SDL_free(wide);
        return result;
    }
#endif
    VulkanRenderer *renderer = debug_renderer(device);
    if (!renderer || !renderer->vkSetDebugUtilsObjectNameEXT) return 0;
    if (kind < E2D_NAME_BUFFER || kind > E2D_NAME_IMAGE_VIEW)
        return label_resource(renderer, kind, resource, name);

    // Defragmentation can replace physical buffer and image handles inside a live container.
    SDL_LockRWLockForReading(renderer->defragLock);
    int result = label_resource(renderer, kind, resource, name);
    SDL_UnlockRWLock(renderer->defragLock);
    return result;
}

E2D_EXPORT int e2d_render_command_label(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *command,
    const char *name, float r, float g, float b, float a, uint32_t replace)
{
    if (!device || abi != 1) return -1;
    VulkanRenderer *renderer = debug_renderer(device);
    if (!renderer || !renderer->vkCmdBeginDebugUtilsLabelEXT || !renderer->vkCmdEndDebugUtilsLabelEXT) return 0;
    if (!command) return 1;
    VulkanCommandBuffer *buffer = (VulkanCommandBuffer *)command;
    if (buffer->renderer != renderer || !buffer->commandBuffer) return -1;

    if (name) {
        const VkDebugUtilsLabelEXT info = {
            .sType = VK_STRUCTURE_TYPE_DEBUG_UTILS_LABEL_EXT,
            .pLabelName = name,
            .color = { r, g, b, a }
        };
        if (replace) renderer->vkCmdEndDebugUtilsLabelEXT(buffer->commandBuffer);
        renderer->vkCmdBeginDebugUtilsLabelEXT(buffer->commandBuffer, &info);
    } else {
        renderer->vkCmdEndDebugUtilsLabelEXT(buffer->commandBuffer);
    }
    return 1;
}
