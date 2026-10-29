#include "pinned-core.h"
#include <stdint.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

typedef struct EmptyFramebuffer
{
    SDL_GPUDevice *device;
    VkRenderPass pass;
    VkFramebuffer framebuffer;
    uint32_t width, height;
    PFN_vkCmdBeginRenderPass begin;
    PFN_vkDestroyFramebuffer destroy_framebuffer;
    PFN_vkDestroyRenderPass destroy_pass;
} EmptyFramebuffer;

static bool compatible(SDL_GPUDevice *device)
{
    return device && e2d_render_pinned_core() &&
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") == 0;
}

static PFN_vkGetDeviceProcAddr device_proc(const VulkanRenderer *renderer)
{
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    return proc ? (PFN_vkGetDeviceProcAddr)proc(renderer->instance, "vkGetDeviceProcAddr") : NULL;
}

E2D_EXPORT int e2d_render_empty_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (!compatible(device)) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetDeviceProcAddr proc = device_proc(renderer);
    if (!proc || !proc(renderer->logicalDevice, "vkCreateRenderPass") || !proc(renderer->logicalDevice, "vkDestroyRenderPass") ||
        !proc(renderer->logicalDevice, "vkCreateFramebuffer") || !proc(renderer->logicalDevice, "vkDestroyFramebuffer") ||
        !proc(renderer->logicalDevice, "vkCmdBeginRenderPass")) return 0;
    // The retained SDL pipeline adapter exposes exact 1/2/4/8 sample states.
    return renderer->physicalDeviceProperties.properties.limits.framebufferNoAttachmentsSampleCounts & 15;
}

E2D_EXPORT void e2d_render_empty_release(SDL_GPUDevice *device, uint32_t abi, EmptyFramebuffer *target)
{
    if (!target || abi != 1 || target->device != device) return;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    // The managed owner retires recorded and submitted commands before release.
    if (target->framebuffer) target->destroy_framebuffer(renderer->logicalDevice, target->framebuffer, NULL);
    if (target->pass) target->destroy_pass(renderer->logicalDevice, target->pass, NULL);
    SDL_free(target);
}

E2D_EXPORT EmptyFramebuffer *e2d_render_empty_create(SDL_GPUDevice *device, uint32_t abi, uint32_t width, uint32_t height, uint32_t samples)
{
    if (!device || abi != 1 || samples > 3 || !width || !height) return NULL;
    int support = e2d_render_empty_support(device, abi);
    if (support <= 0 || !(support & (1u << samples))) return NULL;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    const VkPhysicalDeviceLimits *limits = &renderer->physicalDeviceProperties.properties.limits;
    if (width > limits->maxFramebufferWidth || height > limits->maxFramebufferHeight) return NULL;
    PFN_vkGetDeviceProcAddr proc = device_proc(renderer);
    PFN_vkCreateRenderPass create_pass = (PFN_vkCreateRenderPass)proc(renderer->logicalDevice, "vkCreateRenderPass");
    PFN_vkCreateFramebuffer create_framebuffer = (PFN_vkCreateFramebuffer)proc(renderer->logicalDevice, "vkCreateFramebuffer");
    EmptyFramebuffer *target = SDL_calloc(1, sizeof(*target));
    if (!target) return NULL;
    target->device = device; target->width = width; target->height = height;
    target->begin = (PFN_vkCmdBeginRenderPass)proc(renderer->logicalDevice, "vkCmdBeginRenderPass");
    target->destroy_pass = (PFN_vkDestroyRenderPass)proc(renderer->logicalDevice, "vkDestroyRenderPass");
    target->destroy_framebuffer = (PFN_vkDestroyFramebuffer)proc(renderer->logicalDevice, "vkDestroyFramebuffer");
    VkSubpassDescription subpass = {.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS};
    VkRenderPassCreateInfo pass = {.sType = VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO, .subpassCount = 1, .pSubpasses = &subpass};
    VkResult result = create_pass(renderer->logicalDevice, &pass, NULL, &target->pass);
    if (result == VK_SUCCESS) {
        VkFramebufferCreateInfo info = {.sType = VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO,
            .renderPass = target->pass, .width = width, .height = height, .layers = 1};
        result = create_framebuffer(renderer->logicalDevice, &info, NULL, &target->framebuffer);
        if (result != VK_SUCCESS) target->framebuffer = VK_NULL_HANDLE;
    } else target->pass = VK_NULL_HANDLE;
    if (result != VK_SUCCESS) {
        SDL_SetError("Creating an empty Vulkan framebuffer failed: %d", (int)result);
        e2d_render_empty_release(device, abi, target); return NULL;
    }
    return target;
}

E2D_EXPORT SDL_GPURenderPass *e2d_render_empty_begin(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, EmptyFramebuffer *target)
{
    if (!device || abi != 1 || !commands || !target || !compatible(device) || target->device != device) return NULL;
    VulkanCommandBuffer *command = (VulkanCommandBuffer *)commands;
    if (command->common.device != device || command->renderer != (VulkanRenderer *)device->driverData ||
        (device->debug_mode && (command->common.submitted || command->common.render_pass.in_progress ||
        command->common.compute_pass.in_progress || command->common.copy_pass.in_progress))) return NULL;
    VkRenderPassBeginInfo info = {.sType = VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO, .renderPass = target->pass,
        .framebuffer = target->framebuffer, .renderArea = {.extent = {target->width, target->height}}};
    target->begin(command->commandBuffer, &info, VK_SUBPASS_CONTENTS_INLINE);
    RenderPass *pass = &command->common.render_pass;
    if (device->debug_mode) {
        pass->in_progress = true; pass->num_color_targets = 0; pass->depth_stencil_target = NULL;
        device->max_viewport_width = target->width; device->max_viewport_height = target->height;
    }
    SDL_GPURenderPass *handle = (SDL_GPURenderPass *)pass;
    SDL_GPUViewport viewport = {.w = (float)target->width, .h = (float)target->height, .max_depth = 1};
    SDL_Rect scissor = {.w = (int)target->width, .h = (int)target->height};
    SDL_SetGPUViewport(handle, &viewport); SDL_SetGPUScissor(handle, &scissor);
    SDL_SetGPUBlendConstants(handle, (SDL_FColor){1, 1, 1, 1}); SDL_SetGPUStencilReference(handle, 0);
    return handle;
}
