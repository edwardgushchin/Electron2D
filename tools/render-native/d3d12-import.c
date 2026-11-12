#include "pinned-core.h"
#include "d3d12-views.h"
#include <stdint.h>
#define COBJMACROS
#define E2D_RENDER_DRIVER_D3D12
#include "memory-layout.h"

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

bool e2d_d3d12_device(SDL_GPUDevice *device)
{
#if defined(SDL_PLATFORM_WIN32)
    if (!device || SDL_strcmp(SDL_GetGPUDeviceDriver(device), "direct3d12") != 0 || !e2d_render_pinned_core()) return false;
    const D3D12Renderer *renderer = (const D3D12Renderer *)device->driverData;
    return renderer && renderer->sdlGPUDevice == device && renderer->device && renderer->commandQueue;
#else
    (void)device;
    return false;
#endif
}

#if defined(SDL_PLATFORM_WIN32)
#define VIEW_SOURCE "Electron2D.render.d3d12_view_source"
static const SDL_GPUTextureUsageFlags storage = SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ |
    SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_SIMULTANEOUS_READ_WRITE;

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

static void write_srv(ID3D12Device *device, D3D12Texture *texture, const SDL_GPUTextureCreateInfo *info, DXGI_FORMAT format, uint32_t layer, uint32_t level)
{
    D3D12_SHADER_RESOURCE_VIEW_DESC desc = { 0 };
    desc.Format = format;
    desc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
    if (info->type == SDL_GPU_TEXTURETYPE_2D_ARRAY) {
        desc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2DARRAY;
        desc.Texture2DArray.MostDetailedMip = level;
        desc.Texture2DArray.FirstArraySlice = layer;
        desc.Texture2DArray.MipLevels = info->num_levels;
        desc.Texture2DArray.ArraySize = info->layer_count_or_depth;
    } else {
        desc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
        desc.Texture2D.MostDetailedMip = level;
        desc.Texture2D.MipLevels = info->num_levels;
    }
    ID3D12Device_CreateShaderResourceView(device, texture->resource, &desc, texture->srvHandle.cpuHandle);
}
static void write_target_views(ID3D12Device *device, D3D12Texture *texture, bool array,
    bool array_storage, DXGI_FORMAT format)
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
            if (array) {
                desc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2DARRAY;
                desc.Texture2DArray.MipSlice = sub->level;
                desc.Texture2DArray.FirstArraySlice = sub->layer;
                desc.Texture2DArray.ArraySize = array_storage && index == 0 ? texture->subresourceCount : 1;
            } else {
                desc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D;
                desc.Texture2D.MipSlice = sub->level;
            }
            ID3D12Device_CreateUnorderedAccessView(device, texture->resource, NULL, &desc, sub->uavHandle.cpuHandle);
        }
    }
}


static D3D12TextureContainer *make_wrapper(SDL_GPUDevice *device, const SDL_GPUTextureCreateInfo *info)
{
    const bool target = (info->usage & SDL_GPU_TEXTUREUSAGE_COLOR_TARGET) != 0;
    const bool uav = (info->usage & storage) != 0;
    const bool srv = (info->usage & (SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ)) != 0;
    // Let SDL own every descriptor slot and its retirement. A writable stub needs the
    // same subresource grid but only the minimum width required for its mip count.
    SDL_GPUTextureCreateInfo stub = *info;
    if (target || uav) {
        stub.width = 1u << (info->num_levels - 1);
        stub.height = 1;
    } else {
        stub.type = SDL_GPU_TEXTURETYPE_2D;
        stub.width = stub.height = 4;
        stub.layer_count_or_depth = stub.num_levels = 1;
    }
    stub.props = 0;
    SDL_GPUTexture *handle = SDL_CreateGPUTexture(device, &stub);
    if (!handle) return NULL;
    D3D12TextureContainer *container = (D3D12TextureContainer *)handle;
    D3D12Texture *texture = container->activeTexture;
    if (!texture || !texture->subresources || (srv && (!texture->srvHandle.heap || !texture->srvHandle.cpuHandle.ptr))) {
        SDL_ReleaseGPUTexture(device, handle);
        SDL_WaitForGPUIdle(device);
        SDL_SetError("The Direct3D 12 wrapper is missing its SDL texture or sampled descriptor");
        return NULL;
    }

    const uint32_t count = info->num_levels * info->layer_count_or_depth;
    if (!(target || uav) && count > 1) {
        D3D12TextureSubresource *subresources = SDL_calloc(count, sizeof(*subresources));
        if (!subresources) {
            SDL_ReleaseGPUTexture(device, handle);
            SDL_WaitForGPUIdle(device);
            return NULL;
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

    if (target || uav) {
        for (uint32_t index = 0; index < count; ++index) {
            const D3D12TextureSubresource *sub = &texture->subresources[index];
            if ((target && (!sub->rtvHandles || !sub->rtvHandles[0].heap || !sub->rtvHandles[0].cpuHandle.ptr)) ||
                (uav && (!sub->uavHandle.heap || !sub->uavHandle.cpuHandle.ptr))) {
                SDL_ReleaseGPUTexture(device, handle);
                SDL_WaitForGPUIdle(device);
                SDL_SetError("The Direct3D 12 wrapper needs SDL writable descriptors");
                return NULL;
            }
        }
    }

    return container;
}

static void bind_resource(D3D12TextureContainer *container, ID3D12Device *device, ID3D12Resource *resource,
    const SDL_GPUTextureCreateInfo *info, DXGI_FORMAT format, uint32_t layer, uint32_t level, uint32_t resource_levels, bool array_storage)
{
    D3D12Texture *texture = container->activeTexture;
    for (uint32_t i = 0; i < texture->subresourceCount; ++i) {
        D3D12TextureSubresource *sub = &texture->subresources[i];
        sub->layer += layer;
        sub->level += level;
        sub->index = sub->layer * resource_levels + sub->level;
    }

    SDL_PropertiesID props = container->header.info.props;
    container->header.info = *info;
    container->header.info.props = props;
    container->canBeCycled = false;
    ID3D12Resource_AddRef(resource);
    ID3D12Resource *stub_resource = texture->resource;
    texture->resource = resource;
    if (texture->srvHandle.heap) write_srv(device, texture, info, format, layer, level);
    if (info->usage & (SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | storage))
        write_target_views(device, texture, info->type == SDL_GPU_TEXTURETYPE_2D_ARRAY, array_storage, format);
    ID3D12Resource_Release(stub_resource);
}

#endif

E2D_EXPORT int e2d_render_d3d12_texture_import(SDL_GPUDevice *device, uint32_t abi,
    const SDL_GPUTextureCreateInfo *info, uint64_t bits, SDL_GPUTexture **out)
{
    if (!out) return -1;
    *out = NULL;
#if defined(SDL_PLATFORM_WIN32)
    if ((abi != 1 && abi != 2) || !e2d_d3d12_device(device)) return 0;
    const D3D12Renderer *renderer = (const D3D12Renderer *)device->driverData;
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
    if (uav && (info->usage & storage) != storage) return -1;
    const D3D12_RESOURCE_FLAGS required = (target ? D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET : 0) |
        (uav ? D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS : 0);
    if (!resource_matches(resource, renderer, info, format, required)) return -1;

    D3D12TextureContainer *container = make_wrapper(device, info);
    if (!container) return -2;
    bind_resource(container, renderer->device, resource, info, format, 0, 0, info->num_levels, false);
    *out = (SDL_GPUTexture *)container;
    return 1;
#else
    (void)device; (void)abi; (void)info; (void)bits;
    return 0;
#endif
}


#if defined(SDL_PLATFORM_WIN32)
static DXGI_FORMAT typeless_family(SDL_GPUTextureFormat format)
{
    if ((uint32_t)format >= SDL_arraysize(SDLToD3D12_TextureFormat)) return DXGI_FORMAT_UNKNOWN;
    switch (SDLToD3D12_TextureFormat[format]) {
        case DXGI_FORMAT_R8G8B8A8_UNORM:
        case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB: return DXGI_FORMAT_R8G8B8A8_TYPELESS;
        case DXGI_FORMAT_B8G8R8A8_UNORM:
        case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB: return DXGI_FORMAT_B8G8R8A8_TYPELESS;
        case DXGI_FORMAT_BC1_UNORM:
        case DXGI_FORMAT_BC1_UNORM_SRGB: return DXGI_FORMAT_BC1_TYPELESS;
        case DXGI_FORMAT_BC2_UNORM:
        case DXGI_FORMAT_BC2_UNORM_SRGB: return DXGI_FORMAT_BC2_TYPELESS;
        case DXGI_FORMAT_BC3_UNORM:
        case DXGI_FORMAT_BC3_UNORM_SRGB: return DXGI_FORMAT_BC3_TYPELESS;
        case DXGI_FORMAT_BC7_UNORM:
        case DXGI_FORMAT_BC7_UNORM_SRGB: return DXGI_FORMAT_BC7_TYPELESS;
        default: return DXGI_FORMAT_UNKNOWN;
    }
}

static bool compatible_view_format(SDL_GPUTextureFormat base, SDL_GPUTextureFormat view, uint32_t abi)
{
    if (base == view) return true;
    if (abi != E2D_D3D12_SAMPLED_FORMAT_ABI && abi != E2D_D3D12_MUTABLE_FORMAT_ABI &&
        abi != E2D_D3D12_TRANSFER_TARGET_VIEW_ABI && abi != E2D_D3D12_LAYER_VIEW_ABI && abi != E2D_D3D12_LAYER_SAMPLED_ABI) return false;
    const DXGI_FORMAT family = typeless_family(base);
    return family != DXGI_FORMAT_UNKNOWN && family == typeless_family(view);
}

static bool view_format(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUTextureCreateInfo *info, SDL_GPUTextureFormat format)
{
    const SDL_GPUTextureUsageFlags color = SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;
    const bool transfer_target = abi == E2D_D3D12_TRANSFER_TARGET_VIEW_ABI;
    const bool layer_view = abi == E2D_D3D12_LAYER_VIEW_ABI;
    const bool sampled_only = abi == E2D_D3D12_SAMPLED_FORMAT_ABI || abi == E2D_D3D12_LAYER_SAMPLED_ABI;
    const SDL_GPUTextureUsageFlags required = transfer_target || abi == E2D_D3D12_MUTABLE_FORMAT_ABI ? color :
        layer_view ? color | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ | storage :
        SDL_GPU_TEXTUREUSAGE_SAMPLER | (abi == E2D_D3D12_ARRAY_ABI ? SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ : 0);
    const SDL_GPUTextureUsageFlags allowed = transfer_target ? color : color | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ | storage;
    return info && compatible_view_format(info->format, format, abi) && info->sample_count == SDL_GPU_SAMPLECOUNT_1 &&
        (info->type == SDL_GPU_TEXTURETYPE_2D || info->type == SDL_GPU_TEXTURETYPE_2D_ARRAY) &&
        (info->usage & required) && !(info->usage & ~allowed) &&
        (uint32_t)format < SDL_arraysize(SDLToD3D12_TextureFormat) &&
        SDLToD3D12_TextureFormat[format] != DXGI_FORMAT_UNKNOWN &&
        SDL_GPUTextureSupportsFormat(device, format, info->type,
            sampled_only ? SDL_GPU_TEXTUREUSAGE_SAMPLER : info->usage);
}
#endif

int e2d_d3d12_view_format(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUTextureCreateInfo *info, SDL_GPUTextureFormat format)
{
#if defined(SDL_PLATFORM_WIN32)
    return view_format(device, abi, info, format);
#else
    (void)device; (void)abi; (void)info; (void)format;
    return 0;
#endif
}

SDL_GPUTexture *e2d_d3d12_view_region(SDL_GPUTexture *texture, uint32_t *layer, uint32_t *level)
{
#if defined(SDL_PLATFORM_WIN32)
    D3D12TextureContainer *container = (D3D12TextureContainer *)texture;
    if (!container->activeTexture || *layer >= container->header.info.layer_count_or_depth ||
        *level >= container->header.info.num_levels) return NULL;
    SDL_GPUTexture *source = SDL_GetPointerProperty(container->header.info.props, VIEW_SOURCE, NULL);
    if (!source) return texture;
    *layer += container->activeTexture->subresources[0].layer;
    *level += container->activeTexture->subresources[0].level;
    return source;
#else
    (void)texture; (void)layer; (void)level;
    return NULL;
#endif
}

SDL_GPUTexture *e2d_d3d12_view_create(SDL_GPUDevice *device, uint32_t abi, SDL_GPUTexture *source,
    SDL_GPUTextureFormat format, uint32_t layer, uint32_t layers, uint32_t level, uint32_t levels, uint32_t array)
{
#if defined(SDL_PLATFORM_WIN32)
    D3D12TextureContainer *container = (D3D12TextureContainer *)source;
    SDL_GPUTextureCreateInfo info = container->header.info;
    const bool array_storage = array == 2;
    const bool layer_target = !array && info.type == SDL_GPU_TEXTURETYPE_2D_ARRAY &&
        (abi == E2D_D3D12_TRANSFER_TARGET_VIEW_ABI || abi == E2D_D3D12_LAYER_VIEW_ABI || abi == E2D_D3D12_LAYER_SAMPLED_ABI);
    if (!view_format(device, abi, &info, format) || array > 2 || !layers || !levels ||
        layer >= info.layer_count_or_depth || layers > info.layer_count_or_depth - layer ||
        level >= info.num_levels || levels > info.num_levels - level ||
        (!array && (layers != 1 || (info.type == SDL_GPU_TEXTURETYPE_2D_ARRAY && !layer_target))) ||
        (array_storage && (info.type != SDL_GPU_TEXTURETYPE_2D_ARRAY || levels != 1 || (info.usage & storage) != storage))) {
        SDL_SetError("Unsupported Direct3D 12 source view format, usage or range");
        return NULL;
    }

    // Resolve nested native wrappers to the same physical owner and coordinates.
    source = e2d_d3d12_view_region(source, &layer, &level);
    if (!source) return NULL;
    container = (D3D12TextureContainer *)source;
    const SDL_GPUTextureCreateInfo *root = &container->header.info;
    if (format != root->format) {
        D3D12_RESOURCE_DESC native;
        ID3D12Resource_GetDesc(container->activeTexture->resource, &native);
        if (!compatible_view_format(root->format, format, abi) || native.Format != typeless_family(format)) {
            SDL_SetError("Alternate Direct3D 12 views require compatible typeless storage");
            return NULL;
        }
    }
    // The RTV uses the physical array shape to select the layer of this ordinary view.
    info.type = (array || layer_target) ? SDL_GPU_TEXTURETYPE_2D_ARRAY : SDL_GPU_TEXTURETYPE_2D;
    info.format = format;
    if (abi == E2D_D3D12_SAMPLED_FORMAT_ABI || abi == E2D_D3D12_LAYER_SAMPLED_ABI) info.usage = SDL_GPU_TEXTUREUSAGE_SAMPLER;
    info.width = SDL_max(1u, root->width >> level);
    info.height = SDL_max(1u, root->height >> level);
    info.layer_count_or_depth = layers;
    info.num_levels = levels;
    info.props = 0;

    D3D12TextureContainer *view = make_wrapper(device, &info);
    if (!view) return NULL;
    if (!SDL_SetPointerProperty(view->header.info.props, VIEW_SOURCE, source) ||
        !SDL_SetPointerProperty(SDL_GetGPUDeviceProperties(device), E2D_VIEW_CACHE, device)) {
        SDL_ReleaseGPUTexture(device, (SDL_GPUTexture *)view);
        return NULL;
    }

    const D3D12Renderer *renderer = (const D3D12Renderer *)device->driverData;
    bind_resource(view, renderer->device, container->activeTexture->resource,
        &info, SDLToD3D12_TextureFormat[format], layer, level, root->num_levels, array_storage);
    container->canBeCycled = false;
    return (SDL_GPUTexture *)view;
#else
    (void)device; (void)abi; (void)source; (void)format; (void)layer; (void)layers; (void)level; (void)levels; (void)array;
    return NULL;
#endif
}

SDL_GPUTexture *e2d_d3d12_texture_create(SDL_GPUDevice *device, uint32_t abi,
    const SDL_GPUTextureCreateInfo *info, const SDL_GPUTextureFormat *formats, uint32_t count)
{
#if defined(SDL_PLATFORM_WIN32)
    if (!info || !formats || count != 2 || formats[0] == formats[1] ||
        (formats[0] != info->format && formats[1] != info->format) ||
        !view_format(device, abi == E2D_D3D12_MUTABLE_FORMAT_ABI ? abi : E2D_D3D12_VIEW_ABI, info, info->format) ||
        !view_format(device, abi, info, formats[0]) || !view_format(device, abi, info, formats[1]) ||
        !info->width || !info->height || info->width > D3D12_REQ_TEXTURE2D_U_OR_V_DIMENSION ||
        info->height > D3D12_REQ_TEXTURE2D_U_OR_V_DIMENSION || !info->layer_count_or_depth ||
        info->layer_count_or_depth > D3D12_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION ||
        (info->type == SDL_GPU_TEXTURETYPE_2D && info->layer_count_or_depth != 1) ||
        !info->num_levels || info->num_levels > D3D12_REQ_MIP_LEVELS) return NULL;
    uint32_t levels = 1;
    for (uint32_t side = SDL_max(info->width, info->height); side > 1; side >>= 1) ++levels;
    if (info->num_levels > levels) return NULL;

    D3D12_RESOURCE_DESC desc = { 0 };
    desc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
    desc.Width = info->width;
    desc.Height = info->height;
    desc.DepthOrArraySize = (UINT16)info->layer_count_or_depth;
    desc.MipLevels = (UINT16)info->num_levels;
    desc.Format = typeless_family(info->format);
    desc.SampleDesc.Count = 1;
    if (info->usage & SDL_GPU_TEXTUREUSAGE_COLOR_TARGET) desc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
    if (info->usage & storage) desc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
    const D3D12_HEAP_PROPERTIES heap = { D3D12_HEAP_TYPE_DEFAULT, D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
        D3D12_MEMORY_POOL_UNKNOWN, 1, 1 };
    static const IID resource_iid = { 0x696442be, 0xa72e, 0x4059, { 0xbc, 0x79, 0x5b, 0x5c, 0x98, 0x04, 0x0f, 0xad } };
    const D3D12Renderer *renderer = (const D3D12Renderer *)device->driverData;
    ID3D12Resource *resource = NULL;
    // Match SDL's default state for an attachment without a shader-readable role.
    D3D12_RESOURCE_STATES initial = (info->usage & SDL_GPU_TEXTUREUSAGE_COLOR_TARGET) &&
        !(info->usage & (SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ))
        ? D3D12_RESOURCE_STATE_RENDER_TARGET : D3D12_RESOURCE_STATE_ALL_SHADER_RESOURCE;
    HRESULT result = ID3D12Device_CreateCommittedResource(renderer->device, &heap, D3D12_HEAP_FLAG_NONE, &desc,
        initial, NULL, &resource_iid, (void **)&resource);
    if (FAILED(result)) {
        SDL_SetError("Cannot create typeless Direct3D 12 texture (HRESULT 0x%08x)", (unsigned)result);
        return NULL;
    }
    D3D12TextureContainer *wrapper = make_wrapper(device, info);
    if (wrapper)
        bind_resource(wrapper, renderer->device, resource, info, SDLToD3D12_TextureFormat[info->format],
            0, 0, info->num_levels, false);
    ID3D12Resource_Release(resource);
    return (SDL_GPUTexture *)wrapper;
#else
    (void)device; (void)abi; (void)info; (void)formats; (void)count;
    return NULL;
#endif
}

int e2d_d3d12_compute_arrays(SDL_GPUDevice *device, SDL_GPUCommandBuffer *commands,
    SDL_GPUTexture *const *images, uint32_t count, uint32_t prepare)
{
#if defined(SDL_PLATFORM_WIN32)
    if (!commands || !images || count > 8 || prepare > 1 || !e2d_d3d12_device(device)) return -1;
    D3D12CommandBuffer *command = (D3D12CommandBuffer *)commands;
    if (command->common.device != device || command->renderer != (D3D12Renderer *)device->driverData || !command->graphicsCommandList ||
        (device->debug_mode && (command->common.submitted || !command->common.compute_pass.in_progress))) return -1;

    for (uint32_t i = 0; i < count; ++i) {
        const D3D12TextureContainer *container = (const D3D12TextureContainer *)images[i];
        if (!container || !container->activeTexture) return -1;
        if (container->header.info.type == SDL_GPU_TEXTURETYPE_2D_ARRAY &&
            (container->header.info.num_levels != 1 ||
             !(container->header.info.usage & (SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ)) ||
             container->activeTexture->subresourceCount != container->header.info.layer_count_or_depth)) return -1;
    }

    // SDL binds the UAV and transitions layer zero. Transition the remaining layers of the selected mip.
    D3D12_RESOURCE_BARRIER barriers[64];
    uint32_t pending = 0;
    const D3D12_RESOURCE_STATES idle = D3D12_RESOURCE_STATE_ALL_SHADER_RESOURCE;
    const D3D12_RESOURCE_STATES writing = D3D12_RESOURCE_STATE_UNORDERED_ACCESS;
    for (uint32_t i = 0; i < count; ++i) {
        const D3D12TextureContainer *container = (const D3D12TextureContainer *)images[i];
        if (container->header.info.type != SDL_GPU_TEXTURETYPE_2D_ARRAY) continue;
        const D3D12Texture *texture = container->activeTexture;
        for (uint32_t layer = 1; layer < texture->subresourceCount; ++layer) {
            D3D12_RESOURCE_BARRIER *barrier = &barriers[pending++];
            barrier->Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
            barrier->Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE;
            barrier->Transition = (D3D12_RESOURCE_TRANSITION_BARRIER){ texture->resource,
                texture->subresources[layer].index, prepare ? idle : writing, prepare ? writing : idle };
            if (pending == SDL_arraysize(barriers)) {
                ID3D12GraphicsCommandList_ResourceBarrier(command->graphicsCommandList, pending, barriers);
                pending = 0;
            }
        }
    }
    if (pending) ID3D12GraphicsCommandList_ResourceBarrier(command->graphicsCommandList, pending, barriers);
    return 1;
#else
    (void)device; (void)commands; (void)images; (void)count; (void)prepare;
    return -1;
#endif
}
