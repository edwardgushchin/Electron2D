#include "pinned-core.h"
#define E2D_RENDER_FILTERING
#define E2D_RENDER_DRIVER_D3D12
#include "memory-layout.h"
#include "stencil.h"

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

enum E2DDriverResource {
    E2D_LOGICAL_DEVICE, E2D_PHYSICAL_DEVICE, E2D_TOPMOST_OBJECT, E2D_COMMAND_QUEUE,
    E2D_QUEUE_FAMILY, E2D_TEXTURE, E2D_TEXTURE_VIEW, E2D_TEXTURE_DATA_FORMAT,
    E2D_SAMPLER, E2D_UNIFORM_SET, E2D_BUFFER, E2D_COMPUTE_PIPELINE, E2D_RENDER_PIPELINE
};

static uint64_t texture_resource(const VulkanTextureContainer *container, uint32_t kind)
{
    const VulkanTexture *texture = container->activeTexture;
    if (!texture) return 0;
    if (kind == E2D_TEXTURE) return (uint64_t)texture->image;
    if (kind == E2D_TEXTURE_VIEW) return (uint64_t)texture->fullView;
    if (e2d_stencil_info(&container->header.info)) return VK_FORMAT_S8_UINT;
    const uint32_t format = container->header.info.format;
    return format < SDL_arraysize(SDLToVK_TextureFormat) ? SDLToVK_TextureFormat[format] : VK_FORMAT_UNDEFINED;
}

#if defined(SDL_PLATFORM_WIN32)
static int d3d12_texture_resource(const D3D12TextureContainer *container, uint32_t kind, uint64_t *value)
{
    const D3D12Texture *texture = container->activeTexture;
    if (!texture || !texture->resource) return -1;
    if (kind == E2D_TEXTURE) {
        *value = (uint64_t)(uintptr_t)texture->resource;
    } else if (kind == E2D_TEXTURE_VIEW) {
        if (!texture->srvHandle.heap) return 0;
        *value = (uint64_t)texture->srvHandle.cpuHandle.ptr;
    } else {
        const uint32_t format = container->header.info.format;
        if (format >= SDL_arraysize(SDLToD3D12_TextureFormat)) return -1;
        const bool depthTarget = (container->header.info.usage & SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET) != 0;
        *value = depthTarget && !texture->srvHandle.heap
            ? SDLToD3D12_DepthFormat[format] : SDLToD3D12_TextureFormat[format];
    }
    return *value ? 1 : -1;
}

static int d3d12_resource(SDL_GPUDevice *device, uint32_t kind, const void *resource, uint64_t *out)
{
    const D3D12Renderer *renderer = (const D3D12Renderer *)device->driverData;
    if (!renderer || renderer->sdlGPUDevice != device) return -1;

    uint64_t value;
    switch (kind) {
        case E2D_LOGICAL_DEVICE: value = (uint64_t)(uintptr_t)renderer->device; break;
        case E2D_PHYSICAL_DEVICE: value = (uint64_t)(uintptr_t)renderer->adapter; break;
        case E2D_TOPMOST_OBJECT: value = (uint64_t)(uintptr_t)renderer->factory; break;
        case E2D_COMMAND_QUEUE: value = (uint64_t)(uintptr_t)renderer->commandQueue; break;
        case E2D_QUEUE_FAMILY:
        case E2D_UNIFORM_SET: return 0;
        case E2D_TEXTURE:
        case E2D_TEXTURE_VIEW:
        case E2D_TEXTURE_DATA_FORMAT: return d3d12_texture_resource(resource, kind, out);
        case E2D_SAMPLER: value = (uint64_t)((const D3D12Sampler *)resource)->handle.cpuHandle.ptr; break;
        case E2D_BUFFER: {
            const D3D12Buffer *active = ((const D3D12BufferContainer *)resource)->activeBuffer;
            value = active ? (uint64_t)(uintptr_t)active->handle : 0;
            break;
        }
        case E2D_COMPUTE_PIPELINE: value = (uint64_t)(uintptr_t)((const struct D3D12ComputePipeline *)resource)->pipelineState; break;
        case E2D_RENDER_PIPELINE: value = (uint64_t)(uintptr_t)((const struct D3D12GraphicsPipeline *)resource)->pipelineState; break;
        default: return -1;
    }

    if (!value) return -1;
    *out = value;
    return 1;
}
#endif

E2D_EXPORT int e2d_render_driver_resource(SDL_GPUDevice *device, uint32_t abi, uint32_t kind,
    const void *resource, uint64_t *out)
{
    if (!device || abi != 1 || !out || kind > E2D_RENDER_PIPELINE ||
        (kind >= E2D_TEXTURE && !resource)) return -1;
    const char *driver = SDL_GetGPUDeviceDriver(device);
    if (!e2d_render_pinned_core() || !driver) return 0;
#if defined(SDL_PLATFORM_WIN32)
    if (SDL_strcmp(driver, "direct3d12") == 0) return d3d12_resource(device, kind, resource, out);
#endif
    if (SDL_strcmp(driver, "vulkan") != 0) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    if (!renderer) return -1;

    uint64_t value;
    switch (kind) {
        case E2D_LOGICAL_DEVICE: value = (uint64_t)(uintptr_t)renderer->logicalDevice; break;
        case E2D_PHYSICAL_DEVICE: value = (uint64_t)(uintptr_t)renderer->physicalDevice; break;
        case E2D_TOPMOST_OBJECT: value = (uint64_t)(uintptr_t)renderer->instance; break;
        case E2D_COMMAND_QUEUE: value = (uint64_t)(uintptr_t)renderer->unifiedQueue; break;
        case E2D_QUEUE_FAMILY:
            if (renderer->queueFamilyIndex == SDL_MAX_UINT32) return -1;
            value = renderer->queueFamilyIndex;
            break;
        case E2D_TEXTURE:
        case E2D_TEXTURE_VIEW:
        case E2D_TEXTURE_DATA_FORMAT:
            SDL_LockRWLockForReading(renderer->defragLock);
            value = texture_resource(resource, kind);
            SDL_UnlockRWLock(renderer->defragLock);
            break;
        case E2D_SAMPLER: value = (uint64_t)((const VulkanSampler *)resource)->sampler; break;
        case E2D_UNIFORM_SET: return 0;
        case E2D_BUFFER: {
            SDL_LockRWLockForReading(renderer->defragLock);
            const VulkanBuffer *active = ((const VulkanBufferContainer *)resource)->activeBuffer;
            value = active ? (uint64_t)active->buffer : 0;
            SDL_UnlockRWLock(renderer->defragLock);
            break;
        }
        case E2D_COMPUTE_PIPELINE: value = (uint64_t)((const VulkanComputePipeline *)resource)->pipeline; break;
        case E2D_RENDER_PIPELINE: value = (uint64_t)((const VulkanGraphicsPipeline *)resource)->pipeline; break;
        default: return -1;
    }

    if (!value && kind != E2D_QUEUE_FAMILY) return -1;
    *out = value;
    return 1;
}
