#include "pinned-core.h"
#include "memory-layout.h"
#include "stencil.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#define E2D_THREAD_LOCAL __declspec(thread)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#define E2D_THREAD_LOCAL _Thread_local
#endif

E2D_EXPORT int e2d_render_stencil_support(SDL_GPUDevice *device, uint32_t abi, SDL_GPUTextureUsageFlags usage, uint32_t samples)
{
    if (!device || abi != 1 || samples > SDL_GPU_SAMPLECOUNT_8) return -1;
    if (!e2d_render_pinned_core() || SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return 0;
    if (usage & ~(SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ)) return 0;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    VkImageUsageFlags flags = VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT;
    if (usage & (SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ)) flags |= VK_IMAGE_USAGE_SAMPLED_BIT;
    VkImageFormatProperties properties;
    return r->vkGetPhysicalDeviceImageFormatProperties(r->physicalDevice, VK_FORMAT_S8_UINT, VK_IMAGE_TYPE_2D, VK_IMAGE_TILING_OPTIMAL, flags, 0, &properties) == VK_SUCCESS && (properties.sampleCounts & (1u << samples)) ? 1 : 0;
}

VkResult e2d_stencil_render_pass(PFN_vkCreateRenderPass create, VkDevice device, const VkRenderPassCreateInfo *info, const VkAllocationCallbacks *allocator, VkRenderPass *pass)
{
    if (!create || !info || info->subpassCount != 1 || !info->pSubpasses || !info->pSubpasses->pDepthStencilAttachment || !info->pAttachments || info->attachmentCount > 17) return VK_ERROR_INITIALIZATION_FAILED;
    uint32_t depth = info->pSubpasses->pDepthStencilAttachment->attachment;
    if (depth >= info->attachmentCount || info->pAttachments[depth].format != VK_FORMAT_R8_UINT) return VK_ERROR_INITIALIZATION_FAILED;
    VkAttachmentDescription attachments[17]; SDL_memcpy(attachments, info->pAttachments, info->attachmentCount * sizeof(*attachments));
    attachments[depth].format = VK_FORMAT_S8_UINT;
    attachments[depth].loadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE; attachments[depth].storeOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
    VkRenderPassCreateInfo actual = *info; actual.pAttachments = attachments;
    return create(device, &actual, allocator, pass);
}

typedef struct StencilPassState { PFN_vkCreateRenderPass create; PFN_vkCmdBeginRenderPass begin; bool recorded; } StencilPassState;
static E2D_THREAD_LOCAL StencilPassState state;
static VkResult VKAPI_CALL stencil_render_pass(VkDevice device, const VkRenderPassCreateInfo *info, const VkAllocationCallbacks *allocator, VkRenderPass *pass)
{
    return e2d_stencil_render_pass(state.create, device, info, allocator, pass);
}
static void VKAPI_CALL stencil_record_pass(VkCommandBuffer commands, const VkRenderPassBeginInfo *info, VkSubpassContents contents)
{
    state.begin(commands, info, contents); state.recorded = true;
}
E2D_EXPORT SDL_GPURenderPass *e2d_render_stencil_begin(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, const SDL_GPUColorTargetInfo *colors, uint32_t count, const SDL_GPUDepthStencilTargetInfo *depth)
{
    if (!device || abi != 1 || !commands || !depth || count > 8 || (count && !colors) || e2d_render_stencil_support(device, abi, SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET, 0) != 1 || !depth->texture) return NULL;
    VulkanCommandBuffer *c = (VulkanCommandBuffer *)commands; VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    if (c->common.device != device || c->renderer != r || (device->debug_mode && (c->common.submitted || c->common.copy_pass.in_progress || c->common.compute_pass.in_progress || c->common.render_pass.in_progress))) return NULL;
    VulkanTextureContainer *texture = (VulkanTextureContainer *)depth->texture;
    if (!e2d_stencil_info(&texture->header.info) || depth->cycle) return NULL;
    for (uint32_t i = 0; i < count; i++) if (colors[i].cycle || colors[i].cycle_resolve_texture) return NULL;
    // The reserved R8_UINT depth-slot key cannot collide with an ordinary color/depth render pass.
    VulkanRenderer local = *r; local.vkCreateRenderPass = stencil_render_pass; local.vkCmdBeginRenderPass = stencil_record_pass;
    StencilPassState previous = state; state = (StencilPassState){r->vkCreateRenderPass, r->vkCmdBeginRenderPass, false}; c->renderer = &local;
    SDL_GPURenderPass *pass = SDL_BeginGPURenderPass(commands, colors, count, depth);
    bool recorded = state.recorded; c->renderer = r; state = previous;
    if (!recorded) { c->common.render_pass.in_progress = false; return NULL; }
    return pass;
}
