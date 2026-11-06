#include "pinned-core.h"
#include "memory-layout.h"
#include "stencil.h"
#include <SDL3/SDL_vulkan.h>

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
    if (!device || (abi < 1 || abi > 4)) return -1;
    if (!e2d_render_pinned_core() || SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return 0;
    if (abi >= 3) return format == SDL_GPU_TEXTUREFORMAT_R8_UINT ? e2d_render_stencil_support(device, 1, SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET | (abi == 4 ? SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ : 0), 0) : 0;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData; VkFormat native = depth_format(format);
    if (native == VK_FORMAT_UNDEFINED || !r->vkCmdCopyBufferToImage || !r->vkCmdCopyImageToBuffer || !r->vkCmdCopyImage || !r->vkCmdPipelineBarrier) return 0;
    VkFormatProperties properties; r->vkGetPhysicalDeviceFormatProperties(r->physicalDevice, native, &properties);
    VkFormatFeatureFlags required = VK_FORMAT_FEATURE_TRANSFER_SRC_BIT | VK_FORMAT_FEATURE_TRANSFER_DST_BIT;
    if (abi == 2) required |= VK_FORMAT_FEATURE_SAMPLED_IMAGE_BIT | VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT;
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
    if (!t->activeTexture || (!e2d_stencil_info(info) && depth_format(info->format) == VK_FORMAT_UNDEFINED) || region->mip_level >= info->num_levels || region->layer >= info->layer_count_or_depth || region->z || region->d != 1 || !region->w || !region->h) return false;
    uint32_t width = SDL_max(1u, info->width >> region->mip_level), height = SDL_max(1u, info->height >> region->mip_level);
    return region->x <= width && region->y <= height && region->w <= width - region->x && region->h <= height - region->y;
}
static VkImageLayout ordinary(VulkanTexture *t)
{
    if (t->usage & (SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ)) return VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
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
    bool depth = (t->aspectFlags & VK_IMAGE_ASPECT_DEPTH_BIT) != 0;
    uint32_t bytes = depth ? (container->header.info.format == SDL_GPU_TEXTUREFORMAT_D16_UNORM ? 2 : 4) : 0;
    bool stencil = (t->aspectFlags & VK_IMAGE_ASPECT_STENCIL_BIT) != 0;
    if (pixels * bytes + depth_offset > b->size || (stencil && ((depth && stencil_offset < depth_offset + pixels * bytes) || pixels + stencil_offset > b->size))) goto done;
    if (!retain_texture(c, t) || !retain_buffer(c, b)) goto done;
    VkBufferImageCopy regions[2] = {{.bufferOffset = depth_offset, .imageSubresource = {VK_IMAGE_ASPECT_DEPTH_BIT, region->mip_level, region->layer, 1}, .imageOffset = {region->x, region->y, 0}, .imageExtent = {region->w, region->h, 1}}};
    if (stencil) { regions[1] = regions[0]; regions[1].bufferOffset = stencil_offset; regions[1].imageSubresource.aspectMask = VK_IMAGE_ASPECT_STENCIL_BIT; }
    uint32_t count = stencil ? 2 : 1;
    if (!depth) { regions[0] = regions[1]; count = 1; }
    VkImageLayout layout = upload ? VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL : VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
    transition(c, t, region, layout, false);
    if (upload) r->vkCmdCopyBufferToImage(c->commandBuffer, b->buffer, t->image, layout, count, regions);
    else r->vkCmdCopyImageToBuffer(c->commandBuffer, t->image, layout, b->buffer, count, regions);
    transition(c, t, region, layout, true); result = 1;
done:
    SDL_UnlockRWLock(r->defragLock); return result;
}

E2D_EXPORT int e2d_render_depth_resolve_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (!e2d_render_pinned_core() || SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return 0;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    if (r->physicalDeviceProperties.properties.apiVersion < VK_API_VERSION_1_2) return 0;
    PFN_vkGetInstanceProcAddr instance = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceProperties2 query = instance ? (PFN_vkGetPhysicalDeviceProperties2)instance(r->instance, "vkGetPhysicalDeviceProperties2") : NULL;
    if (!query) return 0;
    VkPhysicalDeviceDepthStencilResolveProperties resolve = {.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_DEPTH_STENCIL_RESOLVE_PROPERTIES};
    VkPhysicalDeviceProperties2 properties = {.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2, .pNext = &resolve};
    query(r->physicalDevice, &properties);
    return ((resolve.supportedDepthResolveModes & VK_RESOLVE_MODE_SAMPLE_ZERO_BIT) ? 1 : 0) |
        ((resolve.supportedStencilResolveModes & VK_RESOLVE_MODE_SAMPLE_ZERO_BIT) ? 2 : 0);
}

typedef struct DepthResolve {
    SDL_GPUDevice *device;
    VulkanTexture *source, *destination;
    VulkanMemoryAllocation *source_pin, *destination_pin;
    SDL_GPUTextureRegion source_region, destination_region;
    VkRenderPass pass;
    VkFramebuffer framebuffer;
    PFN_vkCmdBeginRenderPass begin;
    PFN_vkDestroyFramebuffer destroy_framebuffer;
    PFN_vkDestroyRenderPass destroy_pass;
} DepthResolve;

E2D_EXPORT void e2d_render_depth_resolve_release(SDL_GPUDevice *device, uint32_t abi, DepthResolve *target)
{
    if (!device || abi != 1 || !target || target->device != device) return;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    if (target->framebuffer) target->destroy_framebuffer(r->logicalDevice, target->framebuffer, NULL);
    if (target->pass) target->destroy_pass(r->logicalDevice, target->pass, NULL);
    if (target->source_pin) (void)SDL_AtomicDecRef(&target->source_pin->referenceCount);
    if (target->destination_pin) (void)SDL_AtomicDecRef(&target->destination_pin->referenceCount);
    if (target->source) (void)SDL_AtomicDecRef(&target->source->referenceCount);
    if (target->destination) (void)SDL_AtomicDecRef(&target->destination->referenceCount);
    SDL_free(target);
}

E2D_EXPORT DepthResolve *e2d_render_depth_resolve_create(SDL_GPUDevice *device, uint32_t abi,
    const SDL_GPUTextureLocation *source, const SDL_GPUTextureLocation *destination, uint32_t width, uint32_t height)
{
    if (!device || abi != 1 || !source || !destination || !source->texture || !destination->texture ||
        e2d_render_depth_resolve_support(device, abi) <= 0) return NULL;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    DepthResolve *target = NULL; SDL_LockRWLockForReading(r->defragLock);
    VulkanTextureContainer *a = (VulkanTextureContainer *)source->texture, *b = (VulkanTextureContainer *)destination->texture;
    SDL_GPUTextureRegion x = {.texture=source->texture,.mip_level=source->mip_level,.layer=source->layer,.w=width,.h=height,.d=1};
    SDL_GPUTextureRegion y = {.texture=destination->texture,.mip_level=destination->mip_level,.layer=destination->layer,.w=width,.h=height,.d=1};
    int required = !a->activeTexture ? 0 : ((a->activeTexture->aspectFlags & VK_IMAGE_ASPECT_DEPTH_BIT) ? 1 : 0) | ((a->activeTexture->aspectFlags & VK_IMAGE_ASPECT_STENCIL_BIT) ? 2 : 0);
    Sint64 enabled = SDL_GetNumberProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.depth_resolve", 0);
    if ((enabled & required) != required || !region_valid(a, &x) || !region_valid(b, &y) ||
        a->header.info.format != b->header.info.format || e2d_stencil_info(&a->header.info) != e2d_stencil_info(&b->header.info) || a->header.info.sample_count == SDL_GPU_SAMPLECOUNT_1 ||
        b->header.info.sample_count != SDL_GPU_SAMPLECOUNT_1 || source->x || source->y || source->z || destination->x || destination->y || destination->z ||
        width != SDL_max(1u, a->header.info.width >> x.mip_level) || height != SDL_max(1u, a->header.info.height >> x.mip_level) ||
        width != SDL_max(1u, b->header.info.width >> y.mip_level) || height != SDL_max(1u, b->header.info.height >> y.mip_level)) goto done;
    target = SDL_calloc(1, sizeof(*target)); if (!target) goto done;
    target->device = device; target->source = a->activeTexture; target->destination = b->activeTexture;
    target->source_region = x; target->destination_region = y;
    // Cached attachment views must keep both images fixed through allocator defragmentation.
    SDL_AtomicIncRef(&target->source->referenceCount); SDL_AtomicIncRef(&target->destination->referenceCount);
    if (target->source->usedRegion) { target->source_pin = target->source->usedRegion->allocation; SDL_AtomicIncRef(&target->source_pin->referenceCount); }
    if (target->destination->usedRegion) { target->destination_pin = target->destination->usedRegion->allocation; SDL_AtomicIncRef(&target->destination_pin->referenceCount); }
    PFN_vkGetInstanceProcAddr instance = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr proc = (PFN_vkGetDeviceProcAddr)instance(r->instance, "vkGetDeviceProcAddr");
    PFN_vkCreateRenderPass2 create = (PFN_vkCreateRenderPass2)proc(r->logicalDevice, "vkCreateRenderPass2");
    PFN_vkCreateFramebuffer create_framebuffer = (PFN_vkCreateFramebuffer)proc(r->logicalDevice, "vkCreateFramebuffer");
    target->begin = (PFN_vkCmdBeginRenderPass)proc(r->logicalDevice, "vkCmdBeginRenderPass");
    target->destroy_framebuffer = (PFN_vkDestroyFramebuffer)proc(r->logicalDevice, "vkDestroyFramebuffer");
    target->destroy_pass = (PFN_vkDestroyRenderPass)proc(r->logicalDevice, "vkDestroyRenderPass");
    if (!create || !create_framebuffer || !target->begin || !target->destroy_framebuffer || !target->destroy_pass) {
        SDL_SetError("The native depth/stencil resolve entry points are unavailable"); e2d_render_depth_resolve_release(device, abi, target); target = NULL; goto done;
    }
    VkAttachmentDescription2 attachments[2] = {
        {.sType=VK_STRUCTURE_TYPE_ATTACHMENT_DESCRIPTION_2,.format=e2d_stencil_info(&a->header.info) ? VK_FORMAT_S8_UINT : depth_format(a->header.info.format),.samples=(VkSampleCountFlagBits)(1u << a->header.info.sample_count),
         .loadOp=VK_ATTACHMENT_LOAD_OP_LOAD,.storeOp=VK_ATTACHMENT_STORE_OP_STORE,.stencilLoadOp=VK_ATTACHMENT_LOAD_OP_LOAD,.stencilStoreOp=VK_ATTACHMENT_STORE_OP_STORE,
         .initialLayout=VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL,.finalLayout=VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL},
        {.sType=VK_STRUCTURE_TYPE_ATTACHMENT_DESCRIPTION_2,.format=e2d_stencil_info(&b->header.info) ? VK_FORMAT_S8_UINT : depth_format(b->header.info.format),.samples=VK_SAMPLE_COUNT_1_BIT,
         .loadOp=VK_ATTACHMENT_LOAD_OP_DONT_CARE,.storeOp=VK_ATTACHMENT_STORE_OP_STORE,.stencilLoadOp=VK_ATTACHMENT_LOAD_OP_DONT_CARE,.stencilStoreOp=VK_ATTACHMENT_STORE_OP_STORE,
         .initialLayout=VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL,.finalLayout=VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL}};
    VkAttachmentReference2 from = {.sType=VK_STRUCTURE_TYPE_ATTACHMENT_REFERENCE_2,.attachment=0,.layout=VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL,.aspectMask=target->source->aspectFlags};
    VkAttachmentReference2 to = from; to.attachment = 1;
    VkSubpassDescriptionDepthStencilResolve resolve = {.sType=VK_STRUCTURE_TYPE_SUBPASS_DESCRIPTION_DEPTH_STENCIL_RESOLVE,
        .depthResolveMode=(required & 1) ? VK_RESOLVE_MODE_SAMPLE_ZERO_BIT : VK_RESOLVE_MODE_NONE,.stencilResolveMode=(required & 2) ? VK_RESOLVE_MODE_SAMPLE_ZERO_BIT : VK_RESOLVE_MODE_NONE,.pDepthStencilResolveAttachment=&to};
    VkSubpassDescription2 subpass = {.sType=VK_STRUCTURE_TYPE_SUBPASS_DESCRIPTION_2,.pNext=&resolve,.pipelineBindPoint=VK_PIPELINE_BIND_POINT_GRAPHICS,.pDepthStencilAttachment=&from};
    VkRenderPassCreateInfo2 info = {.sType=VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO_2,.attachmentCount=2,.pAttachments=attachments,.subpassCount=1,.pSubpasses=&subpass};
    VkResult result = create(r->logicalDevice, &info, NULL, &target->pass);
    if (result == VK_SUCCESS) {
        VkImageView views[2] = {target->source->subresources[x.layer * target->source->levelCount + x.mip_level].depthStencilView,
            target->destination->subresources[y.layer * target->destination->levelCount + y.mip_level].depthStencilView};
        VkFramebufferCreateInfo fb = {.sType=VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO,.renderPass=target->pass,.attachmentCount=2,.pAttachments=views,.width=width,.height=height,.layers=1};
        result = create_framebuffer(r->logicalDevice, &fb, NULL, &target->framebuffer);
        if (result != VK_SUCCESS) target->framebuffer = VK_NULL_HANDLE;
    } else target->pass = VK_NULL_HANDLE;
    if (result != VK_SUCCESS) { SDL_SetError("Creating a depth/stencil resolve target failed: %d", (int)result); e2d_render_depth_resolve_release(device, abi, target); target = NULL; }
done:
    SDL_UnlockRWLock(r->defragLock); return target;
}

E2D_EXPORT int e2d_render_depth_resolve_record(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, DepthResolve *target)
{
    VulkanCommandBuffer *c = command_for(device, abi, commands);
    if (!c || !target || target->device != device) return -1;
    VulkanRenderer *r = c->renderer; SDL_LockRWLockForReading(r->defragLock);
    if (!retain_texture(c, target->source) || !retain_texture(c, target->destination)) { SDL_UnlockRWLock(r->defragLock); return -1; }
    transition(c, target->source, &target->source_region, VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, false);
    transition(c, target->destination, &target->destination_region, VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, false);
    VkRenderPassBeginInfo info = {.sType=VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO,.renderPass=target->pass,.framebuffer=target->framebuffer,
        .renderArea={.extent={target->source_region.w,target->source_region.h}}};
    target->begin(c->commandBuffer, &info, VK_SUBPASS_CONTENTS_INLINE); r->vkCmdEndRenderPass(c->commandBuffer);
    transition(c, target->source, &target->source_region, VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, true);
    transition(c, target->destination, &target->destination_region, VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, true);
    SDL_UnlockRWLock(r->defragLock); return 1;
}
E2D_EXPORT int e2d_render_aspect_copy(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, const SDL_GPUTextureLocation *source, const SDL_GPUTextureLocation *destination, uint32_t width, uint32_t height)
{
    VulkanCommandBuffer *c = command_for(device, abi, commands);
    if (!c || !source || !destination || !source->texture || !destination->texture) return -1;
    VulkanRenderer *r = c->renderer; int result = -1; SDL_LockRWLockForReading(r->defragLock);
    VulkanTextureContainer *a = (VulkanTextureContainer *)source->texture, *b = (VulkanTextureContainer *)destination->texture;
    SDL_GPUTextureRegion x = {.texture=source->texture,.mip_level=source->mip_level,.layer=source->layer,.x=source->x,.y=source->y,.w=width,.h=height,.d=1};
    SDL_GPUTextureRegion y = {.texture=destination->texture,.mip_level=destination->mip_level,.layer=destination->layer,.x=destination->x,.y=destination->y,.w=width,.h=height,.d=1};
    if (!region_valid(a, &x) || !region_valid(b, &y) || a->header.info.format != b->header.info.format || e2d_stencil_info(&a->header.info) != e2d_stencil_info(&b->header.info) || a->header.info.sample_count != b->header.info.sample_count || (a->activeTexture->image == b->activeTexture->image && x.mip_level == y.mip_level && x.layer == y.layer)) goto done;
    VulkanTexture *src = a->activeTexture, *dst = b->activeTexture;
    if (!retain_texture(c, src) || !retain_texture(c, dst)) goto done;
    VkImageCopy regions[2] = {{.srcSubresource = {VK_IMAGE_ASPECT_DEPTH_BIT, x.mip_level, x.layer, 1}, .srcOffset = {x.x,x.y,0}, .dstSubresource = {VK_IMAGE_ASPECT_DEPTH_BIT, y.mip_level, y.layer, 1}, .dstOffset = {y.x,y.y,0}, .extent = {width,height,1}}};
    bool stencil = (src->aspectFlags & VK_IMAGE_ASPECT_STENCIL_BIT) != 0;
    if (stencil) { regions[1] = regions[0]; regions[1].srcSubresource.aspectMask = regions[1].dstSubresource.aspectMask = VK_IMAGE_ASPECT_STENCIL_BIT; }
    uint32_t count = stencil ? 2 : 1;
    if ((src->aspectFlags & VK_IMAGE_ASPECT_DEPTH_BIT) == 0) { regions[0] = regions[1]; count = 1; }
    transition(c, src, &x, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, false); transition(c, dst, &y, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, false);
    r->vkCmdCopyImage(c->commandBuffer, src->image, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, dst->image, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, count, regions);
    transition(c, src, &x, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, true); transition(c, dst, &y, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, true); result = 1;
done:
    SDL_UnlockRWLock(r->defragLock); return result;
}
