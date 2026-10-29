#include "pinned-core.h"
#include "memory-layout.h"

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

static VkFormat depth_format(SDL_GPUTextureFormat format)
{
    switch (format) {
    case SDL_GPU_TEXTUREFORMAT_D16_UNORM: return VK_FORMAT_D16_UNORM;
    case SDL_GPU_TEXTUREFORMAT_D24_UNORM: return VK_FORMAT_X8_D24_UNORM_PACK32;
    case SDL_GPU_TEXTUREFORMAT_D32_FLOAT: return VK_FORMAT_D32_SFLOAT;
    case SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT: return VK_FORMAT_D24_UNORM_S8_UINT;
    case SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT: return VK_FORMAT_D32_SFLOAT_S8_UINT;
    default: return VK_FORMAT_UNDEFINED;
    }
}
E2D_EXPORT int e2d_render_aspect_support(SDL_GPUDevice *device, uint32_t abi, SDL_GPUTextureFormat format)
{
    if (!device || abi != 1) return -1;
    if (!e2d_render_pinned_core() || SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return 0;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData; VkFormat native = depth_format(format);
    if (native == VK_FORMAT_UNDEFINED || !r->vkCmdCopyBufferToImage || !r->vkCmdCopyImageToBuffer || !r->vkCmdCopyImage || !r->vkCmdPipelineBarrier) return 0;
    VkFormatProperties properties; r->vkGetPhysicalDeviceFormatProperties(r->physicalDevice, native, &properties);
    VkFormatFeatureFlags required = VK_FORMAT_FEATURE_TRANSFER_SRC_BIT | VK_FORMAT_FEATURE_TRANSFER_DST_BIT;
    return (properties.optimalTilingFeatures & required) == required;
}
static VulkanCommandBuffer *command_for(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands)
{
    if (!device || abi != 1 || !commands || !e2d_render_pinned_core() || SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return NULL;
    VulkanCommandBuffer *c = (VulkanCommandBuffer *)commands;
    if (c->common.device != device || c->renderer != (VulkanRenderer *)device->driverData || (device->debug_mode && (c->common.submitted || !c->common.copy_pass.in_progress))) return NULL;
    return c;
}
#define RETAIN(items, count, capacity, value, reference) do { \
    bool found = false; for (int i = 0; i < (count); i++) found |= (items)[i] == (value); \
    if (!found) { \
        if ((count) == (capacity)) { \
            int next = SDL_max(4, (capacity) * 2); \
            void *grown = SDL_realloc((items), sizeof(*(items)) * next); if (!grown) return false; \
            (items) = grown; (capacity) = next; \
        } \
        SDL_AtomicIncRef(reference); (items)[(count)++] = (value); \
    } \
} while (0)
static bool retain_texture(VulkanCommandBuffer *c, VulkanTexture *t)
{
    RETAIN(c->usedTextures, c->usedTextureCount, c->usedTextureCapacity, t, &t->referenceCount);
    // Pin SDL allocator regions through recording as well as ordinary resource retirement.
    if (t->usedRegion) RETAIN(c->texturesUsedInPendingTransfers, c->texturesUsedInPendingTransfersCount, c->texturesUsedInPendingTransfersCapacity, t, &t->usedRegion->allocation->referenceCount);
    return true;
}
static bool retain_buffer(VulkanCommandBuffer *c, VulkanBuffer *b)
{
    RETAIN(c->usedBuffers, c->usedBufferCount, c->usedBufferCapacity, b, &b->referenceCount);
    return true;
}
#undef RETAIN
static bool region_valid(VulkanTextureContainer *t, const SDL_GPUTextureRegion *region)
{
    const SDL_GPUTextureCreateInfo *info = &t->header.info;
    if (!t->activeTexture || depth_format(info->format) == VK_FORMAT_UNDEFINED || region->mip_level >= info->num_levels || region->layer >= info->layer_count_or_depth || region->z || region->d != 1 || !region->w || !region->h) return false;
    uint32_t width = SDL_max(1u, info->width >> region->mip_level), height = SDL_max(1u, info->height >> region->mip_level);
    return region->x <= width && region->y <= height && region->w <= width - region->x && region->h <= height - region->y;
}
static VkImageLayout ordinary(VulkanTexture *t)
{
    if (t->usage & SDL_GPU_TEXTUREUSAGE_SAMPLER) return VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
    return VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;
}
static void transition(VulkanCommandBuffer *c, VulkanTexture *t, const SDL_GPUTextureRegion *region, VkImageLayout layout, bool restore)
{
    VkImageMemoryBarrier barrier = {.sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
        .srcAccessMask = VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT, .dstAccessMask = VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT,
        .oldLayout = restore ? layout : ordinary(t), .newLayout = restore ? ordinary(t) : layout,
        .srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED, .dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED, .image = t->image,
        .subresourceRange = {t->aspectFlags, region->mip_level, 1, region->layer, 1}};
    c->renderer->vkCmdPipelineBarrier(c->commandBuffer, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, 0, 0, NULL, 0, NULL, 1, &barrier);
}
E2D_EXPORT int e2d_render_aspect_transfer(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, const SDL_GPUTextureRegion *region, SDL_GPUTransferBuffer *transfer, uint32_t depth_offset, uint32_t stencil_offset, uint32_t upload)
{
    VulkanCommandBuffer *c = command_for(device, abi, commands);
    if (!c || !region || !region->texture || !transfer || upload > 1 || depth_offset % 4 || stencil_offset % 4) return -1;
    VulkanRenderer *r = c->renderer; int result = -1; SDL_LockRWLockForReading(r->defragLock);
    VulkanTextureContainer *container = (VulkanTextureContainer *)region->texture; VulkanBuffer *b = ((VulkanBufferContainer *)transfer)->activeBuffer;
    if (!region_valid(container, region) || container->header.info.sample_count != SDL_GPU_SAMPLECOUNT_1 || !b || b->type != VULKAN_BUFFER_TYPE_TRANSFER) goto done;
    VulkanTexture *t = container->activeTexture; uint64_t pixels = (uint64_t)region->w * region->h;
    uint32_t bytes = container->header.info.format == SDL_GPU_TEXTUREFORMAT_D16_UNORM ? 2 : 4;
    bool stencil = (t->aspectFlags & VK_IMAGE_ASPECT_STENCIL_BIT) != 0;
    if (pixels * bytes + depth_offset > b->size || (stencil && (stencil_offset < depth_offset + pixels * bytes || pixels + stencil_offset > b->size))) goto done;
    if (!retain_texture(c, t) || !retain_buffer(c, b)) goto done;
    VkBufferImageCopy regions[2] = {{.bufferOffset = depth_offset, .imageSubresource = {VK_IMAGE_ASPECT_DEPTH_BIT, region->mip_level, region->layer, 1}, .imageOffset = {region->x, region->y, 0}, .imageExtent = {region->w, region->h, 1}}};
    if (stencil) { regions[1] = regions[0]; regions[1].bufferOffset = stencil_offset; regions[1].imageSubresource.aspectMask = VK_IMAGE_ASPECT_STENCIL_BIT; }
    VkImageLayout layout = upload ? VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL : VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
    transition(c, t, region, layout, false);
    if (upload) r->vkCmdCopyBufferToImage(c->commandBuffer, b->buffer, t->image, layout, stencil ? 2 : 1, regions);
    else r->vkCmdCopyImageToBuffer(c->commandBuffer, t->image, layout, b->buffer, stencil ? 2 : 1, regions);
    transition(c, t, region, layout, true); result = 1;
done:
    SDL_UnlockRWLock(r->defragLock); return result;
}
E2D_EXPORT int e2d_render_aspect_copy(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, const SDL_GPUTextureLocation *source, const SDL_GPUTextureLocation *destination, uint32_t width, uint32_t height)
{
    VulkanCommandBuffer *c = command_for(device, abi, commands);
    if (!c || !source || !destination || !source->texture || !destination->texture) return -1;
    VulkanRenderer *r = c->renderer; int result = -1; SDL_LockRWLockForReading(r->defragLock);
    VulkanTextureContainer *a = (VulkanTextureContainer *)source->texture, *b = (VulkanTextureContainer *)destination->texture;
    SDL_GPUTextureRegion x = {.texture=source->texture,.mip_level=source->mip_level,.layer=source->layer,.x=source->x,.y=source->y,.w=width,.h=height,.d=1};
    SDL_GPUTextureRegion y = {.texture=destination->texture,.mip_level=destination->mip_level,.layer=destination->layer,.x=destination->x,.y=destination->y,.w=width,.h=height,.d=1};
    if (!region_valid(a, &x) || !region_valid(b, &y) || a->header.info.format != b->header.info.format || a->header.info.sample_count != b->header.info.sample_count || (a->activeTexture->image == b->activeTexture->image && x.mip_level == y.mip_level && x.layer == y.layer)) goto done;
    VulkanTexture *src = a->activeTexture, *dst = b->activeTexture;
    if (!retain_texture(c, src) || !retain_texture(c, dst)) goto done;
    VkImageCopy regions[2] = {{.srcSubresource = {VK_IMAGE_ASPECT_DEPTH_BIT, x.mip_level, x.layer, 1}, .srcOffset = {x.x,x.y,0}, .dstSubresource = {VK_IMAGE_ASPECT_DEPTH_BIT, y.mip_level, y.layer, 1}, .dstOffset = {y.x,y.y,0}, .extent = {width,height,1}}};
    bool stencil = (src->aspectFlags & VK_IMAGE_ASPECT_STENCIL_BIT) != 0;
    if (stencil) { regions[1] = regions[0]; regions[1].srcSubresource.aspectMask = regions[1].dstSubresource.aspectMask = VK_IMAGE_ASPECT_STENCIL_BIT; }
    transition(c, src, &x, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, false); transition(c, dst, &y, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, false);
    r->vkCmdCopyImage(c->commandBuffer, src->image, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, dst->image, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, stencil ? 2 : 1, regions);
    transition(c, src, &x, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, true); transition(c, dst, &y, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, true); result = 1;
done:
    SDL_UnlockRWLock(r->defragLock); return result;
}
