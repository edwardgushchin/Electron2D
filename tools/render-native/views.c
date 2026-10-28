#include <stdint.h>
#define E2D_RENDER_FILTERING
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

#define VIEW_CACHE "Electron2D.render.texture_views"
typedef struct View {
    SDL_GPUTexture *handle, *root;
    VulkanTexture *native, *source;
    VulkanMemoryAllocation *pinned;
    VkImage image;
    VkDeviceMemory memory;
    uint64_t bytes;
    uint32_t base_level, base_layer, levels, layers, format_count;
    SDL_GPUTextureFormat formats[256];
    bool retired;
    struct View *next;
} View;
typedef struct ViewCache { SDL_Mutex *lock; View *views; uint64_t bytes; } ViewCache;

static bool compatible(SDL_GPUDevice *device)
{
    return device && SDL_GetVersion() == SDL_VERSIONNUM(3, 4, 18) &&
        SDL_strcmp(SDL_GetRevision(), "SDL-3.4.18-release-3.4.18") == 0 &&
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") == 0;
}
E2D_EXPORT int e2d_render_view_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (!compatible(device)) return 0;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    if (!r || !r->vkCreateImage || !r->vkCreateImageView || !r->vkAllocateMemory || !r->vkBindImageMemory || !r->vkCmdPipelineBarrier) return 0;
    PFN_vkGetInstanceProcAddr instance = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr proc = instance ? (PFN_vkGetDeviceProcAddr)instance(r->instance, "vkGetDeviceProcAddr") : NULL;
    return proc && proc(r->logicalDevice, "vkCreateImageView") && proc(r->logicalDevice, "vkAllocateMemory");
}
static void SDLCALL free_cache(void *unused, void *value)
{
    (void)unused; ViewCache *cache = value;
    if (cache->views) SDL_LogError(SDL_LOG_CATEGORY_GPU, "Texture views require retirement before GPU device destruction");
    SDL_DestroyMutex(cache->lock); SDL_free(cache);
}
static ViewCache *view_cache(SDL_GPUDevice *device, bool create)
{
    SDL_PropertiesID props = SDL_GetGPUDeviceProperties(device);
    if (!create) return SDL_GetPointerProperty(props, VIEW_CACHE, NULL);
    if (!SDL_LockProperties(props)) return NULL;
    ViewCache *cache = SDL_GetPointerProperty(props, VIEW_CACHE, NULL);
    if (!cache && create) {
        cache = SDL_calloc(1, sizeof(*cache));
        if (cache) {
            cache->lock = SDL_CreateMutex();
            if (!cache->lock) { SDL_free(cache); cache = NULL; }
            else if (!SDL_SetPointerPropertyWithCleanup(props, VIEW_CACHE, cache, free_cache, NULL)) cache = NULL;
        }
    }
    SDL_UnlockProperties(props); return cache;
}
uint64_t e2d_views_memory(SDL_GPUDevice *device)
{
    ViewCache *cache = view_cache(device, false); if (!cache) return 0;
    SDL_LockMutex(cache->lock); uint64_t bytes = cache->bytes; SDL_UnlockMutex(cache->lock); return bytes;
}
static VkFormat native_format(SDL_GPUTextureFormat format)
{
    return (uint32_t)format < SDL_arraysize(SDLToVK_TextureFormat) ? SDLToVK_TextureFormat[format] : VK_FORMAT_UNDEFINED;
}
static bool depth_format(VkFormat format) { return format >= VK_FORMAT_D16_UNORM && format <= VK_FORMAT_D32_SFLOAT_S8_UINT; }
static bool compatible_format(SDL_GPUTextureFormat a, SDL_GPUTextureFormat b)
{
    VkFormat x = native_format(a), y = native_format(b);
    if (!x || !y) return false;
    if (x == y) return true;
    if (depth_format(x) || depth_format(y)) return false;
    if (x >= VK_FORMAT_BC1_RGB_UNORM_BLOCK && x <= VK_FORMAT_ASTC_12x12_SRGB_BLOCK)
        return y >= VK_FORMAT_BC1_RGB_UNORM_BLOCK && y <= VK_FORMAT_ASTC_12x12_SRGB_BLOCK && ((x - VK_FORMAT_BC1_RGB_UNORM_BLOCK) / 2 == (y - VK_FORMAT_BC1_RGB_UNORM_BLOCK) / 2);
    if (y >= VK_FORMAT_BC1_RGB_UNORM_BLOCK) return false;
    return SDL_GPUTextureFormatTexelBlockSize(a) == SDL_GPUTextureFormatTexelBlockSize(b);
}
static VkComponentMapping swizzle(SDL_GPUTextureFormat format)
{
    if (format == SDL_GPU_TEXTUREFORMAT_A8_UNORM) return (VkComponentMapping){VK_COMPONENT_SWIZZLE_ZERO, VK_COMPONENT_SWIZZLE_ZERO, VK_COMPONENT_SWIZZLE_ZERO, VK_COMPONENT_SWIZZLE_R};
    if (format == SDL_GPU_TEXTUREFORMAT_B4G4R4A4_UNORM) return (VkComponentMapping){VK_COMPONENT_SWIZZLE_G, VK_COMPONENT_SWIZZLE_R, VK_COMPONENT_SWIZZLE_A, VK_COMPONENT_SWIZZLE_B};
    return (VkComponentMapping){VK_COMPONENT_SWIZZLE_IDENTITY, VK_COMPONENT_SWIZZLE_IDENTITY, VK_COMPONENT_SWIZZLE_IDENTITY, VK_COMPONENT_SWIZZLE_IDENTITY};
}
static VkImageAspectFlags aspects(VkFormat format)
{
    if (!depth_format(format)) return VK_IMAGE_ASPECT_COLOR_BIT;
    return VK_IMAGE_ASPECT_DEPTH_BIT | ((format == VK_FORMAT_D24_UNORM_S8_UINT || format == VK_FORMAT_D32_SFLOAT_S8_UINT || format == VK_FORMAT_D16_UNORM_S8_UINT) ? VK_IMAGE_ASPECT_STENCIL_BIT : 0);
}
static bool image_view(VulkanRenderer *r, VkImage image, VkFormat format, VkComponentMapping channels, VkImageAspectFlags aspect, uint32_t layer, uint32_t layers, uint32_t level, uint32_t levels, VkImageViewType type, VkImageView *out)
{
    VkImageViewCreateInfo info = {.sType = VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO, .image = image, .viewType = type, .format = format, .components = channels,
        .subresourceRange = {.aspectMask = aspect, .baseMipLevel = level, .levelCount = levels, .baseArrayLayer = layer, .layerCount = layers}};
    PFN_vkGetInstanceProcAddr instance = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr proc = instance ? (PFN_vkGetDeviceProcAddr)instance(r->instance, "vkGetDeviceProcAddr") : NULL;
    PFN_vkCreateImageView create = proc ? (PFN_vkCreateImageView)proc(r->logicalDevice, "vkCreateImageView") : NULL;
    if (!create) return SDL_SetError("Native texture-view creation procedure is unavailable");
    VkResult result = create(r->logicalDevice, &info, NULL, out);
    return result == VK_SUCCESS || SDL_SetError("Native texture-view creation failed (%d)", result);
}
static void destroy_unpublished(SDL_GPUDevice *device, View *view)
{
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    if (view->native) {
        VulkanTexture *t = view->native;
        if (t->fullView) r->vkDestroyImageView(r->logicalDevice, t->fullView, NULL);
        for (uint32_t i = 0; i < t->subresourceCount; i++) {
            VulkanTextureSubresource *s = &t->subresources[i];
            if (s->renderTargetViews) { if (s->renderTargetViews[0]) r->vkDestroyImageView(r->logicalDevice, s->renderTargetViews[0], NULL); SDL_free(s->renderTargetViews); }
            if (s->computeWriteView) r->vkDestroyImageView(r->logicalDevice, s->computeWriteView, NULL);
            if (s->depthStencilView) r->vkDestroyImageView(r->logicalDevice, s->depthStencilView, NULL);
        }
        SDL_free(t->subresources); SDL_free(t);
    }
    VulkanTextureContainer *c = (VulkanTextureContainer *)view->handle;
    if (c) { SDL_DestroyProperties(c->header.info.props); SDL_free(c->textures); SDL_free(c); }
    if (view->image) r->vkDestroyImage(r->logicalDevice, view->image, NULL);
    if (view->memory) r->vkFreeMemory(r->logicalDevice, view->memory, NULL);
    if (view->pinned) (void)SDL_AtomicDecRef(&view->pinned->referenceCount);
    if (view->source) (void)SDL_AtomicDecRef(&view->source->referenceCount);
    SDL_free(view);
}
static bool wrap(SDL_GPUDevice *device, View *v, VkImage image, const SDL_GPUTextureCreateInfo *info)
{
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    VulkanTextureContainer *c = SDL_calloc(1, sizeof(*c)); v->handle = (SDL_GPUTexture *)c;
    VulkanTexture *t = SDL_calloc(1, sizeof(*t)); v->native = t;
    if (!c || !t) return false;
    c->header.info = *info; c->header.info.props = SDL_CreateProperties(); c->textures = SDL_malloc(sizeof(VulkanTexture *));
    if (!c->header.info.props || !c->textures) return false;
    c->textures[0] = t; c->activeTexture = t; c->textureCount = c->textureCapacity = 1;
    t->container = c; t->image = image; t->aspectFlags = aspects(native_format(info->format)); t->swizzle = swizzle(info->format);
    t->depth = 1; t->levelCount = v->levels; t->layerCount = v->layers; t->type = info->type; t->usage = info->usage;
    t->subresourceCount = info->num_levels * info->layer_count_or_depth;
    t->subresources = SDL_calloc(t->subresourceCount, sizeof(*t->subresources)); if (!t->subresources) { t->subresourceCount = 0; return false; }
    VkImageAspectFlags sampled_aspect = t->aspectFlags & ~VK_IMAGE_ASPECT_STENCIL_BIT;
    if (!image_view(r, image, native_format(info->format), t->swizzle, sampled_aspect, v->base_layer, info->layer_count_or_depth, v->base_level, info->num_levels, info->type == SDL_GPU_TEXTURETYPE_2D ? VK_IMAGE_VIEW_TYPE_2D : VK_IMAGE_VIEW_TYPE_2D_ARRAY, &t->fullView)) return false;
    for (uint32_t layer = 0; layer < info->layer_count_or_depth; layer++) for (uint32_t level = 0; level < info->num_levels; level++) {
        VulkanTextureSubresource *s = &t->subresources[layer * info->num_levels + level];
        s->parent = t; s->layer = v->base_layer + layer; s->level = v->base_level + level;
        if (info->usage & SDL_GPU_TEXTUREUSAGE_COLOR_TARGET) {
            s->renderTargetViews = SDL_calloc(1, sizeof(VkImageView));
            if (!s->renderTargetViews || !image_view(r, image, native_format(info->format), t->swizzle, t->aspectFlags, s->layer, 1, s->level, 1, VK_IMAGE_VIEW_TYPE_2D, s->renderTargetViews)) return false;
        }
        if ((info->usage & (SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_SIMULTANEOUS_READ_WRITE)) &&
            !image_view(r, image, native_format(info->format), t->swizzle, t->aspectFlags, s->layer, 1, s->level, 1, VK_IMAGE_VIEW_TYPE_2D, &s->computeWriteView)) return false;
        if ((info->usage & SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET) &&
            !image_view(r, image, native_format(info->format), t->swizzle, t->aspectFlags, s->layer, 1, s->level, 1, VK_IMAGE_VIEW_TYPE_2D, &s->depthStencilView)) return false;
    }
    return true;
}
static bool publish(SDL_GPUDevice *device, View *view)
{
    ViewCache *cache = view_cache(device, true); if (!cache) return false;
    SDL_LockMutex(cache->lock);
    bool valid = view->bytes <= UINT64_MAX - cache->bytes;
    if (valid) { cache->bytes += view->bytes; view->next = cache->views; cache->views = view; }
    SDL_UnlockMutex(cache->lock); return valid;
}
E2D_EXPORT void e2d_render_views_collect(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1 || !compatible(device)) return;
    ViewCache *cache = view_cache(device, false); if (!cache) return;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    SDL_LockMutex(cache->lock); bool pending = false; for (View *v = cache->views; v; v = v->next) pending |= v->retired; SDL_UnlockMutex(cache->lock);
    if (!pending) return;
    SDL_WaitForGPUIdle(device);
    for (;;) {
        View *ready = NULL;
        SDL_LockMutex(cache->lock);
        for (View **p = &cache->views; *p;) {
            View *v = *p;
            if (v->retired && SDL_GetAtomicInt(&v->native->referenceCount) == 0) { *p = v->next; v->next = ready; ready = v; }
            else p = &v->next;
        }
        SDL_UnlockMutex(cache->lock);
        if (!ready) return;
        for (View *v = ready; v; v = v->next) { v->native->image = VK_NULL_HANDLE; SDL_ReleaseGPUTexture(device, v->handle); }
        SDL_WaitForGPUIdle(device);
        while (ready) {
            View *v = ready; ready = v->next;
            if (v->image) r->vkDestroyImage(r->logicalDevice, v->image, NULL);
            if (v->memory) r->vkFreeMemory(r->logicalDevice, v->memory, NULL);
            if (v->pinned) (void)SDL_AtomicDecRef(&v->pinned->referenceCount);
            if (v->source) (void)SDL_AtomicDecRef(&v->source->referenceCount);
            SDL_LockMutex(cache->lock); cache->bytes -= v->bytes; SDL_UnlockMutex(cache->lock); SDL_free(v);
        }
    }
}
E2D_EXPORT void e2d_render_view_destroy(SDL_GPUDevice *device, uint32_t abi, SDL_GPUTexture *texture)
{
    if (!texture || !device || abi != 1 || !compatible(device)) return;
    ViewCache *cache = view_cache(device, false); if (!cache) return;
    SDL_LockMutex(cache->lock);
    for (View *v = cache->views; v; v = v->next) if (v->handle == texture) { v->retired = true; break; }
    SDL_UnlockMutex(cache->lock); e2d_render_views_collect(device, 1);
}
E2D_EXPORT void e2d_render_views_release(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1 || !compatible(device)) return;
    ViewCache *cache = view_cache(device, false); if (!cache) return;
    SDL_LockMutex(cache->lock); for (View *v = cache->views; v; v = v->next) v->retired = true; SDL_UnlockMutex(cache->lock);
    e2d_render_views_collect(device, 1);
    if (cache->views) SDL_LogError(SDL_LOG_CATEGORY_GPU, "Unsubmitted commands retain native texture views");
    else SDL_ClearProperty(SDL_GetGPUDeviceProperties(device), VIEW_CACHE);
}
E2D_EXPORT SDL_GPUTexture *e2d_render_view_create(SDL_GPUDevice *device, uint32_t abi, SDL_GPUTexture *source, SDL_GPUTextureFormat format, uint32_t layer, uint32_t layers, uint32_t level, uint32_t levels, uint32_t array)
{
    if (e2d_render_view_support(device, abi) != 1 || !source || !layers || !levels || array > 1 || (!array && layers != 1)) return NULL;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData; VulkanTextureContainer *src = (VulkanTextureContainer *)source;
    if (!src->activeTexture || !compatible_format(src->header.info.format, format) ||
        layer >= src->header.info.layer_count_or_depth || layers > src->header.info.layer_count_or_depth - layer || level >= src->header.info.num_levels || levels > src->header.info.num_levels - level) return NULL;
    if (!SDL_GPUTextureSupportsFormat(device, format, array ? SDL_GPU_TEXTURETYPE_2D_ARRAY : SDL_GPU_TEXTURETYPE_2D, src->header.info.usage)) return NULL;
    View *v = SDL_calloc(1, sizeof(*v)); if (!v) return NULL;
    v->root = source; v->base_layer = layer; v->base_level = level;
    ViewCache *cache = view_cache(device, false);
    if (cache) {
        SDL_LockMutex(cache->lock);
        for (View *p = cache->views; p; p = p->next) if (p->handle == source && !p->retired) {
            v->root = p->root; v->base_layer += p->base_layer; v->base_level += p->base_level; v->format_count = p->format_count;
            SDL_memcpy(v->formats, p->formats, p->format_count * sizeof(*p->formats)); break;
        }
        SDL_UnlockMutex(cache->lock);
    }
    if (!v->format_count) { v->format_count = 1; v->formats[0] = src->header.info.format; }
    bool allowed = false; for (uint32_t i = 0; i < v->format_count; i++) allowed |= v->formats[i] == format;
    if (!allowed) { SDL_free(v); return NULL; }
    SDL_LockMutex(r->allocatorLock); SDL_LockRWLockForReading(r->defragLock);
    v->source = src->activeTexture; v->levels = v->source->levelCount; v->layers = v->source->layerCount;
    SDL_AtomicIncRef(&v->source->referenceCount);
    if (v->source->usedRegion) { v->pinned = v->source->usedRegion->allocation; SDL_AtomicIncRef(&v->pinned->referenceCount); }
    // Aliases must keep sharing this physical image when a producer requests native cycling.
    src->canBeCycled = false;
    SDL_UnlockRWLock(r->defragLock); SDL_UnlockMutex(r->allocatorLock);
    SDL_GPUTextureCreateInfo info = src->header.info; info.type = array ? SDL_GPU_TEXTURETYPE_2D_ARRAY : SDL_GPU_TEXTURETYPE_2D;
    info.format = format; info.width = SDL_max(1, info.width >> level); info.height = SDL_max(1, info.height >> level); info.layer_count_or_depth = layers; info.num_levels = levels;
    if (!wrap(device, v, v->source->image, &info) || !publish(device, v)) { destroy_unpublished(device, v); return NULL; }
    return v->handle;
}
E2D_EXPORT SDL_GPUTexture *e2d_render_texture_create(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUTextureCreateInfo *info, const SDL_GPUTextureFormat *formats, uint32_t count)
{
    if (e2d_render_view_support(device, abi) != 1 || !info || !formats || !count || count > 256 || !info->width || !info->height || !info->num_levels || !info->layer_count_or_depth || info->num_levels > 32 || info->layer_count_or_depth > 2048 ||
        (info->type != SDL_GPU_TEXTURETYPE_2D && info->type != SDL_GPU_TEXTURETYPE_2D_ARRAY) || (info->type == SDL_GPU_TEXTURETYPE_2D && info->layer_count_or_depth != 1) || info->sample_count > SDL_GPU_SAMPLECOUNT_8) return NULL;
    bool base = false;
    for (uint32_t i = 0; i < count; i++) { base |= formats[i] == info->format; if (!compatible_format(info->format, formats[i]) || !SDL_GPUTextureSupportsFormat(device, formats[i], info->type, info->usage)) return NULL; }
    if (!base) return NULL;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    View *v = SDL_calloc(1, sizeof(*v)); if (!v) return NULL;
    v->levels = info->num_levels; v->layers = info->layer_count_or_depth; v->format_count = count; SDL_memcpy(v->formats, formats, count * sizeof(*formats));
    VkImageUsageFlags usage = VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT;
    if (info->usage & (SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ)) usage |= VK_IMAGE_USAGE_SAMPLED_BIT;
    if (info->usage & SDL_GPU_TEXTUREUSAGE_COLOR_TARGET) usage |= VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
    if (info->usage & SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET) usage |= VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;
    if (info->usage & (SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_SIMULTANEOUS_READ_WRITE)) usage |= VK_IMAGE_USAGE_STORAGE_BIT;
    VkImageCreateInfo create = {.sType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO, .flags = count > 1 ? VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT : 0, .imageType = VK_IMAGE_TYPE_2D,
        .format = native_format(info->format), .extent = {info->width, info->height, 1}, .mipLevels = info->num_levels, .arrayLayers = info->layer_count_or_depth,
        .samples = (VkSampleCountFlagBits)(1u << info->sample_count), .tiling = VK_IMAGE_TILING_OPTIMAL, .usage = usage, .sharingMode = VK_SHARING_MODE_EXCLUSIVE, .initialLayout = VK_IMAGE_LAYOUT_UNDEFINED};
    VkImageFormatProperties supported;
    if (r->vkGetPhysicalDeviceImageFormatProperties(r->physicalDevice, create.format, create.imageType, create.tiling, create.usage, create.flags, &supported) != VK_SUCCESS ||
        info->width > supported.maxExtent.width || info->height > supported.maxExtent.height || info->num_levels > supported.maxMipLevels || info->layer_count_or_depth > supported.maxArrayLayers || !(supported.sampleCounts & create.samples)) goto fail;
    uint32_t max_levels = 1, side = SDL_max(info->width, info->height); while (side > 1) { side >>= 1; max_levels++; }
    if (info->num_levels > max_levels || (info->sample_count != SDL_GPU_SAMPLECOUNT_1 && info->num_levels != 1)) goto fail;
    if (r->vkCreateImage(r->logicalDevice, &create, NULL, &v->image) != VK_SUCCESS) goto fail;
    VkMemoryRequirements requirements; r->vkGetImageMemoryRequirements(r->logicalDevice, v->image, &requirements);
    uint32_t memory_type = UINT32_MAX;
    for (uint32_t i = 0; i < r->memoryProperties.memoryTypeCount; i++) if ((requirements.memoryTypeBits & (1u << i)) && (r->memoryProperties.memoryTypes[i].propertyFlags & VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)) { memory_type = i; break; }
    if (memory_type == UINT32_MAX) goto fail;
    VkMemoryAllocateInfo memory = {.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO, .allocationSize = requirements.size, .memoryTypeIndex = memory_type};
    if (r->vkAllocateMemory(r->logicalDevice, &memory, NULL, &v->memory) != VK_SUCCESS || r->vkBindImageMemory(r->logicalDevice, v->image, v->memory, 0) != VK_SUCCESS) goto fail;
    v->bytes = requirements.size;
    if (!wrap(device, v, v->image, info)) goto fail;
    v->root = v->handle;
    SDL_GPUCommandBuffer *commands = SDL_AcquireGPUCommandBuffer(device); if (!commands) goto fail;
    VkImageLayout layout = (info->usage & SDL_GPU_TEXTUREUSAGE_SAMPLER) ? VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL :
        (info->usage & SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ) ? VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL :
        (info->usage & SDL_GPU_TEXTUREUSAGE_COLOR_TARGET) ? VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL :
        (info->usage & SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET) ? VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL :
        (info->usage & SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ) ? VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL : VK_IMAGE_LAYOUT_GENERAL;
    VkImageMemoryBarrier barrier = {.sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER, .dstAccessMask = VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT, .oldLayout = VK_IMAGE_LAYOUT_UNDEFINED, .newLayout = layout,
        .srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED, .dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED, .image = v->image,
        .subresourceRange = {.aspectMask = v->native->aspectFlags, .levelCount = v->levels, .layerCount = v->layers}};
    r->vkCmdPipelineBarrier(((VulkanCommandBuffer *)commands)->commandBuffer, VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, 0, 0, NULL, 0, NULL, 1, &barrier);
    if (!SDL_SubmitGPUCommandBuffer(commands)) { SDL_CancelGPUCommandBuffer(commands); goto fail; }
    SDL_WaitForGPUIdle(device);
    if (!publish(device, v)) goto fail;
    return v->handle;
fail:
    destroy_unpublished(device, v); return NULL;
}

E2D_EXPORT SDL_GPUTexture *e2d_render_view_region(SDL_GPUDevice *device, uint32_t abi, SDL_GPUTexture *texture, uint32_t *layer, uint32_t *level)
{
    if (!texture || !layer || !level || abi != 1 || !compatible(device)) return NULL;
    const SDL_GPUTextureCreateInfo *info = &((VulkanTextureContainer *)texture)->header.info;
    if (*layer >= info->layer_count_or_depth || *level >= info->num_levels) return NULL;
    ViewCache *cache = view_cache(device, false); if (!cache) return texture;
    SDL_GPUTexture *root = texture;
    SDL_LockMutex(cache->lock);
    for (View *v = cache->views; v; v = v->next) if (v->handle == texture) {
        if (v->retired) root = NULL;
        else { root = v->root; *layer += v->base_layer; *level += v->base_level; }
        break;
    }
    SDL_UnlockMutex(cache->lock); return root;
}
E2D_EXPORT int e2d_render_view_format_support(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUTextureCreateInfo *info, SDL_GPUTextureFormat format)
{
    if (!info || e2d_render_view_support(device, abi) != 1) return 0;
    if (!compatible_format(info->format, format) || !SDL_GPUTextureSupportsFormat(device, format, info->type, info->usage) || info->sample_count > SDL_GPU_SAMPLECOUNT_8) return 0;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    VkImageUsageFlags usage = VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT;
    if (info->usage & (SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ)) usage |= VK_IMAGE_USAGE_SAMPLED_BIT;
    if (info->usage & SDL_GPU_TEXTUREUSAGE_COLOR_TARGET) usage |= VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
    if (info->usage & SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET) usage |= VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;
    if (info->usage & (SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_SIMULTANEOUS_READ_WRITE)) usage |= VK_IMAGE_USAGE_STORAGE_BIT;
    VkImageFormatProperties supported;
    return r->vkGetPhysicalDeviceImageFormatProperties(r->physicalDevice, native_format(format), VK_IMAGE_TYPE_2D, VK_IMAGE_TILING_OPTIMAL, usage, info->format == format ? 0 : VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT, &supported) == VK_SUCCESS &&
        (supported.sampleCounts & (1u << info->sample_count)) != 0;
}
typedef struct TextureRange { SDL_GPUTexture *texture; uint32_t layer, layers, level, levels; } TextureRange;
E2D_EXPORT int e2d_render_view_overlap(SDL_GPUDevice *device, uint32_t abi, const TextureRange *a, const TextureRange *b)
{
    if (!a || !b || !a->texture || !b->texture || !a->layers || !b->layers || !a->levels || !b->levels || abi != 1 || !compatible(device)) return -1;
    VulkanTexture *x = ((VulkanTextureContainer *)a->texture)->activeTexture, *y = ((VulkanTextureContainer *)b->texture)->activeTexture;
    if (!x || !y) return -1;
    if (x->image != y->image) return 0;
    uint64_t al = a->layer, bl = b->layer, am = a->level, bm = b->level;
    ViewCache *cache = view_cache(device, false);
    if (cache) {
        SDL_LockMutex(cache->lock);
        for (View *v = cache->views; v; v = v->next) {
            if (v->handle == a->texture) { al += v->base_layer; am += v->base_level; }
            if (v->handle == b->texture) { bl += v->base_layer; bm += v->base_level; }
        }
        SDL_UnlockMutex(cache->lock);
    }
    return al < bl + b->layers && bl < al + a->layers && am < bm + b->levels && bm < am + a->levels;
}

bool e2d_views_general_sample(SDL_GPUDevice *device, VkImageView sampled, SDL_GPUTexture *const *general, uint32_t count)
{
    ViewCache *cache = view_cache(device, false); if (!cache) return false;
    bool found = false;
    SDL_LockMutex(cache->lock);
    for (View *v = cache->views; v && !found; v = v->next) {
        VkImage image = v->native->fullView == sampled ? v->native->image : v->source && v->source->fullView == sampled ? v->source->image : VK_NULL_HANDLE;
        if (image) for (uint32_t i = 0; i < count; i++) if (((VulkanTextureContainer *)general[i])->activeTexture->image == image) { found = true; break; }
    }
    SDL_UnlockMutex(cache->lock); return found;
}
