#include "pinned-core.h"
#include <stdint.h>
#include <stddef.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

_Static_assert(offsetof(VkPhysicalDeviceFeatures, vertexPipelineStoresAndAtomics) == 25 * sizeof(VkBool32), "vertex feature word");
_Static_assert(offsetof(VkPhysicalDeviceFeatures, fragmentStoresAndAtomics) == 26 * sizeof(VkBool32), "fragment feature word");

static bool compatible(SDL_GPUDevice *device)
{
    return device && e2d_render_pinned_core() &&
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") == 0;
}

E2D_EXPORT int e2d_render_graphics_storage_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (!compatible(device)) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures query = proc ? (PFN_vkGetPhysicalDeviceFeatures)proc(renderer->instance, "vkGetPhysicalDeviceFeatures") : NULL;
    PFN_vkGetDeviceProcAddr device_proc = proc ? (PFN_vkGetDeviceProcAddr)proc(renderer->instance, "vkGetDeviceProcAddr") : NULL;
    if (!query || !device_proc || !device_proc(renderer->logicalDevice, "vkCmdPipelineBarrier")) return 0;
    VkPhysicalDeviceFeatures features;
    query(renderer->physicalDevice, &features);
    return (features.vertexPipelineStoresAndAtomics ? 1 : 0) | (features.fragmentStoresAndAtomics ? 2 : 0);
}

E2D_EXPORT int e2d_render_storage_barrier(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands)
{
    if (!device || !commands || abi != 1 || !compatible(device)) return -1;
    VulkanCommandBuffer *command = (VulkanCommandBuffer *)commands;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    // SDL only initializes and maintains pass/submission flags in debug mode.
    if (command->common.device != device || command->renderer != renderer || (device->debug_mode && (command->common.submitted ||
        command->common.render_pass.in_progress || command->common.compute_pass.in_progress || command->common.copy_pass.in_progress))) return -1;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr device_proc = proc ? (PFN_vkGetDeviceProcAddr)proc(renderer->instance, "vkGetDeviceProcAddr") : NULL;
    PFN_vkCmdPipelineBarrier barrier = device_proc ? (PFN_vkCmdPipelineBarrier)device_proc(renderer->logicalDevice, "vkCmdPipelineBarrier") : NULL;
    if (!barrier) return -1;
    // SDL tracks graphics storage reads only; include writes and read-before-write hazards across pass boundaries.
    VkMemoryBarrier memory = {.sType = VK_STRUCTURE_TYPE_MEMORY_BARRIER,
        .srcAccessMask = VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT,
        .dstAccessMask = VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT};
    barrier(command->commandBuffer, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, 0, 1, &memory, 0, NULL, 0, NULL);
    return 1;
}
