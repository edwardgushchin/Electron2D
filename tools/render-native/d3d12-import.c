#include "pinned-core.h"
#include <stdint.h>
#define COBJMACROS
#define E2D_RENDER_DRIVER_D3D12
#include "memory-layout.h"

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

#if defined(SDL_PLATFORM_WIN32)
static const IID device_iid = { 0x189819f1, 0x1db6, 0x4b57, { 0xbe, 0x54, 0x18, 0x21, 0x33, 0x9b, 0x85, 0xf7 } };

static bool resource_matches(ID3D12Resource *resource, const D3D12Renderer *renderer,
    const SDL_GPUTextureCreateInfo *info, DXGI_FORMAT format, D3D12_RESOURCE_FLAGS required)
{
    ID3D12Device *owner = NULL;
    if (FAILED(ID3D12Resource_GetDevice(resource, &device_iid, (void **)&owner))) return false;
    bool same_device = owner == renderer->device;
    ID3D12Device_Release(owner);
    if (!same_device) return false;

    D3D12_RESOURCE_DESC desc;
    ID3D12Resource_GetDesc(resource, &desc);
    if (desc.Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE2D ||
        desc.Width != info->width || desc.Height != info->height ||
        desc.DepthOrArraySize != info->layer_count_or_depth || desc.MipLevels != info->num_levels ||
        desc.Format != format || desc.SampleDesc.Count != 1 || desc.SampleDesc.Quality != 0 ||
        desc.Layout != D3D12_TEXTURE_LAYOUT_UNKNOWN || (desc.Flags & required) != required ||
        (desc.Flags & (D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE | D3D12_RESOURCE_FLAG_ALLOW_CROSS_ADAPTER |
            D3D12_RESOURCE_FLAG_ALLOW_SIMULTANEOUS_ACCESS | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL))) return false;

    D3D12_HEAP_PROPERTIES heap;
    D3D12_HEAP_FLAGS flags;
    if (FAILED(ID3D12Resource_GetHeapProperties(resource, &heap, &flags)) ||
        heap.Type != D3D12_HEAP_TYPE_DEFAULT ||
        (flags & (D3D12_HEAP_FLAG_SHARED | D3D12_HEAP_FLAG_SHARED_CROSS_ADAPTER))) return false;

    D3D12_COMMAND_QUEUE_DESC queue;
    ID3D12CommandQueue_GetDesc(renderer->commandQueue, &queue);
    const UINT visible = heap.VisibleNodeMask ? heap.VisibleNodeMask : 1;
    const UINT node = queue.NodeMask ? queue.NodeMask : 1;
    return (visible & node) == node;
}

static void write_srv(ID3D12Device *device, D3D12Texture *texture, const SDL_GPUTextureCreateInfo *info, DXGI_FORMAT format)
{
    D3D12_SHADER_RESOURCE_VIEW_DESC desc = { 0 };
    desc.Format = format;
    desc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
    if (info->type == SDL_GPU_TEXTURETYPE_2D_ARRAY) {
        desc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2DARRAY;
        desc.Texture2DArray.MipLevels = info->num_levels;
        desc.Texture2DArray.ArraySize = info->layer_count_or_depth;
    } else {
        desc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
        desc.Texture2D.MipLevels = info->num_levels;
    }
    ID3D12Device_CreateShaderResourceView(device, texture->resource, &desc, texture->srvHandle.cpuHandle);
}
static void write_target_views(ID3D12Device *device, D3D12Texture *texture, bool array, DXGI_FORMAT format)
{
    for (uint32_t index = 0; index < texture->subresourceCount; ++index) {
        D3D12TextureSubresource *sub = &texture->subresources[index];
        if (sub->rtvHandles) {
            D3D12_RENDER_TARGET_VIEW_DESC desc = { 0 };
            desc.Format = format;
            if (array) {
                desc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2DARRAY;
                desc.Texture2DArray.MipSlice = sub->level;
                desc.Texture2DArray.FirstArraySlice = sub->layer;
                desc.Texture2DArray.ArraySize = 1;
            } else {
                desc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;
                desc.Texture2D.MipSlice = sub->level;
            }
            ID3D12Device_CreateRenderTargetView(device, texture->resource, &desc, sub->rtvHandles[0].cpuHandle);
        }
        if (sub->uavHandle.heap) {
            D3D12_UNORDERED_ACCESS_VIEW_DESC desc = { 0 };
            desc.Format = format;
            desc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D;
            desc.Texture2D.MipSlice = sub->level;
            ID3D12Device_CreateUnorderedAccessView(device, texture->resource, NULL, &desc, sub->uavHandle.cpuHandle);
        }
    }
}

#endif

E2D_EXPORT int e2d_render_d3d12_texture_import(SDL_GPUDevice *device, uint32_t abi,
    const SDL_GPUTextureCreateInfo *info, uint64_t bits, SDL_GPUTexture **out)
{
    if (!out) return -1;
    *out = NULL;
#if defined(SDL_PLATFORM_WIN32)
    if (!device || (abi != 1 && abi != 2) || !e2d_render_pinned_core() ||
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "direct3d12") != 0) return 0;
    const D3D12Renderer *renderer = (const D3D12Renderer *)device->driverData;
    if (!renderer || renderer->sdlGPUDevice != device || !renderer->device || !renderer->commandQueue) return 0;
    const SDL_GPUTextureUsageFlags storage = SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ |
        SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_SIMULTANEOUS_READ_WRITE;
    const SDL_GPUTextureUsageFlags allowed = SDL_GPU_TEXTUREUSAGE_SAMPLER |
        (abi == 2 ? SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | storage : 0);
    if (!info || !bits || bits > UINTPTR_MAX || (uint32_t)info->format >= SDL_arraysize(SDLToD3D12_TextureFormat) ||
        !(info->usage & SDL_GPU_TEXTUREUSAGE_SAMPLER) || (info->usage & ~allowed) ||
        info->sample_count != SDL_GPU_SAMPLECOUNT_1 ||
        !info->width || !info->height || info->width > D3D12_REQ_TEXTURE2D_U_OR_V_DIMENSION ||
        info->height > D3D12_REQ_TEXTURE2D_U_OR_V_DIMENSION || !info->num_levels ||
        info->num_levels > D3D12_REQ_MIP_LEVELS || !info->layer_count_or_depth ||
        info->layer_count_or_depth > D3D12_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION ||
        (info->type != SDL_GPU_TEXTURETYPE_2D && info->type != SDL_GPU_TEXTURETYPE_2D_ARRAY) ||
        (info->type == SDL_GPU_TEXTURETYPE_2D && info->layer_count_or_depth != 1)) return -1;
    DXGI_FORMAT format = SDLToD3D12_TextureFormat[info->format];
    if (format == DXGI_FORMAT_UNKNOWN) return 0;
    ID3D12Resource *resource = (ID3D12Resource *)(uintptr_t)bits;
    const bool target = (info->usage & SDL_GPU_TEXTUREUSAGE_COLOR_TARGET) != 0;
    const bool uav = (info->usage & storage) != 0;
    if (target && info->num_levels != 1) return -1;
    if (uav && (info->type != SDL_GPU_TEXTURETYPE_2D || (info->usage & storage) != storage)) return -1;
    const D3D12_RESOURCE_FLAGS required = (target ? D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET : 0) |
        (uav ? D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS : 0);
    if (!resource_matches(resource, renderer, info, format, required)) return -1;

    // Let SDL own every descriptor slot and its retirement. A writable stub needs the
    // same subresource grid but only the minimum width required for its mip count.
    SDL_GPUTextureCreateInfo stub = *info;
    if (required) {
        stub.width = 1u << (info->num_levels - 1);
        stub.height = 1;
    } else {
        stub.type = SDL_GPU_TEXTURETYPE_2D;
        stub.width = stub.height = 4;
        stub.layer_count_or_depth = stub.num_levels = 1;
    }
    stub.props = 0;
    SDL_GPUTexture *handle = SDL_CreateGPUTexture(device, &stub);
    if (!handle) return -2;
    D3D12TextureContainer *container = (D3D12TextureContainer *)handle;
    D3D12Texture *texture = container->activeTexture;
    if (!texture || !texture->srvHandle.heap || !texture->srvHandle.cpuHandle.ptr || !texture->subresources) {
        SDL_ReleaseGPUTexture(device, handle);
        SDL_WaitForGPUIdle(device);
        SDL_SetError("The Direct3D 12 import needs an SDL sampled descriptor");
        return -2;
    }

    const uint32_t count = info->num_levels * info->layer_count_or_depth;
    if (!required && count > 1) {
        D3D12TextureSubresource *subresources = SDL_calloc(count, sizeof(*subresources));
        if (!subresources) {
            SDL_ReleaseGPUTexture(device, handle);
            SDL_WaitForGPUIdle(device);
            return -2;
        }
        for (uint32_t index = 0; index < count; index++) {
            subresources[index].parent = texture;
            subresources[index].layer = index / info->num_levels;
            subresources[index].level = index % info->num_levels;
            subresources[index].depth = 1;
            subresources[index].index = index;
        }
        SDL_free(texture->subresources);
        texture->subresources = subresources;
        texture->subresourceCount = count;
    }

    if (required) {
        for (uint32_t index = 0; index < count; ++index) {
            const D3D12TextureSubresource *sub = &texture->subresources[index];
            if ((target && (!sub->rtvHandles || !sub->rtvHandles[0].heap || !sub->rtvHandles[0].cpuHandle.ptr)) ||
                (uav && (!sub->uavHandle.heap || !sub->uavHandle.cpuHandle.ptr))) {
                SDL_ReleaseGPUTexture(device, handle);
                SDL_WaitForGPUIdle(device);
                SDL_SetError("The Direct3D 12 import needs SDL writable descriptors");
                return -2;
            }
        }
    }

    SDL_PropertiesID props = container->header.info.props;
    container->header.info = *info;
    container->header.info.props = props;
    container->canBeCycled = false;
    ID3D12Resource_AddRef(resource);
    ID3D12Resource *stub_resource = texture->resource;
    texture->resource = resource;
    write_srv(renderer->device, texture, info, format);
    if (required) write_target_views(renderer->device, texture, info->type == SDL_GPU_TEXTURETYPE_2D_ARRAY, format);
    ID3D12Resource_Release(stub_resource);
    *out = handle;
    return 1;
#else
    (void)device; (void)abi; (void)info; (void)bits;
    return 0;
#endif
}
