#include <stdint.h>
#include <math.h>
#include "timestamp-scale.h"
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

typedef struct TimestampPool {
    VulkanRenderer *renderer;
    VkQueryPool pool;
    uint32_t capacity, bits;
    float period;
    PFN_vkCreateQueryPool create;
    PFN_vkDestroyQueryPool destroy;
    PFN_vkCmdResetQueryPool reset;
    PFN_vkCmdWriteTimestamp write;
    PFN_vkGetQueryPoolResults read;
} TimestampPool;

static bool compatible(SDL_GPUDevice *device)
{
    return device && SDL_GetVersion() == SDL_VERSIONNUM(3, 4, 18) &&
        SDL_strcmp(SDL_GetRevision(), "SDL-3.4.18-release-3.4.18") == 0 &&
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") == 0;
}

E2D_EXPORT int e2d_render_timestamp_create(SDL_GPUDevice *device, uint32_t abi, uint32_t capacity, void **result)
{
    if (!device || abi != 1 || !capacity || !result) return -1;
    *result = NULL;
    if (!compatible(device)) return 0;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr instance = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    if (!r || !instance) return 0;
    PFN_vkGetDeviceProcAddr get = (PFN_vkGetDeviceProcAddr)instance(r->instance, "vkGetDeviceProcAddr");
    if (!get) return 0;
    TimestampPool value = {.renderer = r, .capacity = capacity, .period = r->physicalDeviceProperties.properties.limits.timestampPeriod};
    value.create = (PFN_vkCreateQueryPool)get(r->logicalDevice, "vkCreateQueryPool");
    value.destroy = (PFN_vkDestroyQueryPool)get(r->logicalDevice, "vkDestroyQueryPool");
    value.reset = (PFN_vkCmdResetQueryPool)get(r->logicalDevice, "vkCmdResetQueryPool");
    value.write = (PFN_vkCmdWriteTimestamp)get(r->logicalDevice, "vkCmdWriteTimestamp");
    value.read = (PFN_vkGetQueryPoolResults)get(r->logicalDevice, "vkGetQueryPoolResults");
    if (!value.create || !value.destroy || !value.reset || !value.write || !value.read || !isfinite(value.period) || value.period <= 0) return 0;
    uint32_t count = 0;
    r->vkGetPhysicalDeviceQueueFamilyProperties(r->physicalDevice, &count, NULL);
    if (r->queueFamilyIndex >= count) return -1;
    VkQueueFamilyProperties *families = SDL_calloc(count, sizeof(*families));
    if (!families) return -1;
    r->vkGetPhysicalDeviceQueueFamilyProperties(r->physicalDevice, &count, families);
    value.bits = families[r->queueFamilyIndex].timestampValidBits;
    SDL_free(families);
    if (!value.bits || value.bits > 64) return 0;
    TimestampPool *pool = SDL_malloc(sizeof(*pool));
    if (!pool) return -1;
    *pool = value;
    VkQueryPoolCreateInfo info = {.sType = VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO, .queryType = VK_QUERY_TYPE_TIMESTAMP, .queryCount = capacity};
    if (pool->create(r->logicalDevice, &info, NULL, &pool->pool) != VK_SUCCESS) { SDL_free(pool); return -1; }
    // First capture may be inside an empty render pass, where a query reset cannot be recorded.
    SDL_GPUCommandBuffer *command = SDL_AcquireGPUCommandBuffer(device);
    if (!command) goto fail;
    pool->reset(((VulkanCommandBuffer *)command)->commandBuffer, pool->pool, 0, capacity);
    SDL_GPUFence *fence = SDL_SubmitGPUCommandBufferAndAcquireFence(command);
    if (!fence) goto fail;
    bool waited = SDL_WaitForGPUFences(device, true, &fence, 1);
    SDL_ReleaseGPUFence(device, fence);
    if (!waited) goto fail;
    *result = pool;
    return 1;
fail:
    SDL_WaitForGPUIdle(device);
    pool->destroy(r->logicalDevice, pool->pool, NULL); SDL_free(pool); return -1;
}

E2D_EXPORT int e2d_render_timestamp_reset(SDL_GPUDevice *device, uint32_t abi, TimestampPool *pool, SDL_GPUCommandBuffer *command)
{
    if (!pool || abi != 1 || !compatible(device) || !command) return -1;
    VulkanCommandBuffer *buffer = (VulkanCommandBuffer *)command;
    if (pool->renderer != buffer->renderer || buffer->renderer != (VulkanRenderer *)device->driverData) return -1;
    pool->reset(buffer->commandBuffer, pool->pool, 0, pool->capacity); return 1;
}

E2D_EXPORT int e2d_render_timestamp_write(SDL_GPUDevice *device, uint32_t abi, TimestampPool *pool, SDL_GPUCommandBuffer *command, uint32_t index)
{
    if (!pool || abi != 1 || !compatible(device) || !command || index >= pool->capacity) return -1;
    VulkanCommandBuffer *buffer = (VulkanCommandBuffer *)command;
    if (pool->renderer != buffer->renderer || buffer->renderer != (VulkanRenderer *)device->driverData) return -1;
    pool->write(buffer->commandBuffer, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, pool->pool, index); return 1;
}

E2D_EXPORT int e2d_render_timestamp_read(SDL_GPUDevice *device, uint32_t abi, TimestampPool *pool, uint32_t count, uint64_t *values)
{
    if (!pool || abi != 1 || !compatible(device) || pool->renderer != (VulkanRenderer *)device->driverData || !values || !count || count > pool->capacity) return -1;
    if (pool->read(pool->renderer->logicalDevice, pool->pool, 0, count, (size_t)count * sizeof(*values), values, sizeof(*values), VK_QUERY_RESULT_64_BIT) != VK_SUCCESS) return -1;
    for (uint32_t i = 0; i < count; i++)
        if (!timestamp_ns(values[i], pool->bits, pool->period, &values[i])) return -1;
    return 1;
}

E2D_EXPORT int e2d_render_timestamp_elapsed(SDL_GPUDevice *device, uint32_t abi, TimestampPool *pool, uint64_t *nanoseconds)
{
    if (!pool || abi != 1 || !compatible(device) || pool->renderer != (VulkanRenderer *)device->driverData || !nanoseconds || pool->capacity < 2) return -1;
    uint64_t values[2];
    if (pool->read(pool->renderer->logicalDevice, pool->pool, 0, 2, sizeof(values), values, sizeof(*values), VK_QUERY_RESULT_64_BIT) != VK_SUCCESS) return -1;
    return timestamp_ns(values[1] - values[0], pool->bits, pool->period, nanoseconds) ? 1 : -1;
}

E2D_EXPORT void e2d_render_timestamp_destroy(SDL_GPUDevice *device, uint32_t abi, TimestampPool *pool)
{
    if (!pool || abi != 1 || !compatible(device) || pool->renderer != (VulkanRenderer *)device->driverData) return;
    pool->destroy(pool->renderer->logicalDevice, pool->pool, NULL); SDL_free(pool);
}
