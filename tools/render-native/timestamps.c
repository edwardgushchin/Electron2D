#include "pinned-core.h"
#include <stdint.h>
#include <math.h>
#include "timestamp-scale.h"
#include "d3d12-views.h"
#define COBJMACROS
#define E2D_RENDER_DRIVER_D3D12
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

#if defined(SDL_PLATFORM_WIN32)
typedef struct D3D12TimestampPool {
    SDL_GPUDevice *device;
    ID3D12QueryHeap *heap;
    ID3D12Resource *readback;
    uint64_t frequency;
    uint32_t capacity;
    uint32_t written;
} D3D12TimestampPool;

static const IID query_heap_iid = {0x0d9658ae, 0xed45, 0x469e, {0xa6, 0x1d, 0x97, 0x0e, 0xc5, 0x83, 0xca, 0xb4}};
static const IID resource_iid = {0x696442be, 0xa72e, 0x4059, {0xbc, 0x79, 0x5b, 0x5c, 0x98, 0x04, 0x0f, 0xad}};

static ID3D12GraphicsCommandList *d3d12_timestamp_list(SDL_GPUDevice *device, SDL_GPUCommandBuffer *command)
{
    D3D12CommandBuffer *buffer = (D3D12CommandBuffer *)command;
    if (!buffer || buffer->common.device != device || buffer->renderer != (D3D12Renderer *)device->driverData) {
        return NULL;
    }
    return buffer->graphicsCommandList;
}

static void d3d12_timestamp_destroy(D3D12TimestampPool *pool)
{
    if (pool->readback) {
        ID3D12Resource_Release(pool->readback);
    }
    if (pool->heap) {
        ID3D12QueryHeap_Release(pool->heap);
    }
    SDL_free(pool);
}

static int d3d12_timestamp_read(D3D12TimestampPool *pool, uint32_t count, uint64_t *values, bool elapsed)
{
    if (count > pool->written) {
        return -1;
    }
    D3D12_RANGE read = {0, count * sizeof(*values)};
    D3D12_RANGE no_write = {0, 0};
    void *data = NULL;
    if (FAILED(ID3D12Resource_Map(pool->readback, 0, &read, &data))) {
        return -1;
    }
    // The caller has already waited on the SDL submission fence.
    const uint64_t *ticks = data;
    bool ok = true;
    if (elapsed) {
        ok = timestamp_frequency_ns(ticks[1] - ticks[0], pool->frequency, values);
    } else {
        for (uint32_t index = 0; index < count && ok; ++index) {
            ok = timestamp_frequency_ns(ticks[index], pool->frequency, &values[index]);
        }
    }
    ID3D12Resource_Unmap(pool->readback, 0, &no_write);
    return ok ? 1 : -1;
}
#endif

E2D_EXPORT int e2d_render_d3d12_timestamp_create(SDL_GPUDevice *device, uint32_t abi, uint32_t capacity, void **result)
{
    if (!device || abi != 1 || !capacity || !result || capacity > SIZE_MAX / sizeof(uint64_t)) {
        return -1;
    }
    *result = NULL;
#if defined(SDL_PLATFORM_WIN32)
    if (!e2d_d3d12_device(device)) {
        return 0;
    }
    D3D12Renderer *renderer = (D3D12Renderer *)device->driverData;
    uint64_t frequency = 0;
    if (FAILED(ID3D12CommandQueue_GetTimestampFrequency(renderer->commandQueue, &frequency)) || !frequency) {
        return 0;
    }
    D3D12_COMMAND_QUEUE_DESC queue;
    ID3D12CommandQueue_GetDesc(renderer->commandQueue, &queue);
    if (queue.Type != D3D12_COMMAND_LIST_TYPE_DIRECT) {
        return 0;
    }
    D3D12TimestampPool *pool = SDL_calloc(1, sizeof(*pool));
    if (!pool) {
        return -1;
    }
    pool->device = device;
    pool->capacity = capacity;
    pool->frequency = frequency;
    D3D12_QUERY_HEAP_DESC queries = {D3D12_QUERY_HEAP_TYPE_TIMESTAMP, capacity, queue.NodeMask};
    UINT node = queue.NodeMask ? queue.NodeMask : 1;
    D3D12_HEAP_PROPERTIES heap = {D3D12_HEAP_TYPE_READBACK, D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
        D3D12_MEMORY_POOL_UNKNOWN, node, node};
    D3D12_RESOURCE_DESC buffer = {
        .Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
        .Width = (uint64_t)capacity * sizeof(uint64_t),
        .Height = 1,
        .DepthOrArraySize = 1,
        .MipLevels = 1,
        .SampleDesc = {1, 0},
        .Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR
    };
    if (FAILED(ID3D12Device_CreateQueryHeap(renderer->device, &queries, &query_heap_iid, (void **)&pool->heap)) ||
        FAILED(ID3D12Device_CreateCommittedResource(renderer->device, &heap, D3D12_HEAP_FLAG_NONE, &buffer,
            D3D12_RESOURCE_STATE_COPY_DEST, NULL, &resource_iid, (void **)&pool->readback))) {
        d3d12_timestamp_destroy(pool);
        return -1;
    }
    *result = pool;
    return 1;
#else
    return 0;
#endif
}

static bool compatible(SDL_GPUDevice *device)
{
    return device && e2d_render_pinned_core() &&
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

E2D_EXPORT int e2d_render_timestamp_reset(SDL_GPUDevice *device, uint32_t abi, void *handle, SDL_GPUCommandBuffer *command)
{
#if defined(SDL_PLATFORM_WIN32)
    if (handle && abi == 1 && e2d_d3d12_device(device)) {
        D3D12TimestampPool *pool = handle;
        if (pool->device != device || !d3d12_timestamp_list(device, command)) {
            return -1;
        }
        pool->written = 0;
        return 1;
    }
#endif
    TimestampPool *pool = handle;
    if (!pool || abi != 1 || !compatible(device) || !command) return -1;
    VulkanCommandBuffer *buffer = (VulkanCommandBuffer *)command;
    if (pool->renderer != buffer->renderer || buffer->renderer != (VulkanRenderer *)device->driverData) return -1;
    pool->reset(buffer->commandBuffer, pool->pool, 0, pool->capacity); return 1;
}

E2D_EXPORT int e2d_render_timestamp_write(SDL_GPUDevice *device, uint32_t abi, void *handle, SDL_GPUCommandBuffer *command, uint32_t index)
{
#if defined(SDL_PLATFORM_WIN32)
    if (handle && abi == 1 && e2d_d3d12_device(device)) {
        D3D12TimestampPool *pool = handle;
        ID3D12GraphicsCommandList *list = d3d12_timestamp_list(device, command);
        if (pool->device != device || !list || index != pool->written || index >= pool->capacity) {
            return -1;
        }
        // SDL 3.4.18 binds render targets without opening a D3D12 render pass.
        // Resolve only the slot just written, even inside an SDL draw list.
        ID3D12GraphicsCommandList_EndQuery(list, pool->heap, D3D12_QUERY_TYPE_TIMESTAMP, index);
        ID3D12GraphicsCommandList_ResolveQueryData(list, pool->heap, D3D12_QUERY_TYPE_TIMESTAMP,
            index, 1, pool->readback, (uint64_t)index * sizeof(uint64_t));
        ++pool->written;
        return 1;
    }
#endif
    TimestampPool *pool = handle;
    if (!pool || abi != 1 || !compatible(device) || !command || index >= pool->capacity) return -1;
    VulkanCommandBuffer *buffer = (VulkanCommandBuffer *)command;
    if (pool->renderer != buffer->renderer || buffer->renderer != (VulkanRenderer *)device->driverData) return -1;
    pool->write(buffer->commandBuffer, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, pool->pool, index); return 1;
}

E2D_EXPORT int e2d_render_timestamp_read(SDL_GPUDevice *device, uint32_t abi, void *handle, uint32_t count, uint64_t *values)
{
#if defined(SDL_PLATFORM_WIN32)
    if (handle && abi == 1 && e2d_d3d12_device(device)) {
        D3D12TimestampPool *pool = handle;
        return pool->device == device && count && values ? d3d12_timestamp_read(pool, count, values, false) : -1;
    }
#endif
    TimestampPool *pool = handle;
    if (!pool || abi != 1 || !compatible(device) || pool->renderer != (VulkanRenderer *)device->driverData || !values || !count || count > pool->capacity) return -1;
    if (pool->read(pool->renderer->logicalDevice, pool->pool, 0, count, (size_t)count * sizeof(*values), values, sizeof(*values), VK_QUERY_RESULT_64_BIT) != VK_SUCCESS) return -1;
    for (uint32_t i = 0; i < count; i++)
        if (!timestamp_ns(values[i], pool->bits, pool->period, &values[i])) return -1;
    return 1;
}

E2D_EXPORT int e2d_render_timestamp_elapsed(SDL_GPUDevice *device, uint32_t abi, void *handle, uint64_t *nanoseconds)
{
#if defined(SDL_PLATFORM_WIN32)
    if (handle && abi == 1 && e2d_d3d12_device(device)) {
        D3D12TimestampPool *pool = handle;
        return pool->device == device && nanoseconds ? d3d12_timestamp_read(pool, 2, nanoseconds, true) : -1;
    }
#endif
    TimestampPool *pool = handle;
    if (!pool || abi != 1 || !compatible(device) || pool->renderer != (VulkanRenderer *)device->driverData || !nanoseconds || pool->capacity < 2) return -1;
    uint64_t values[2];
    if (pool->read(pool->renderer->logicalDevice, pool->pool, 0, 2, sizeof(values), values, sizeof(*values), VK_QUERY_RESULT_64_BIT) != VK_SUCCESS) return -1;
    return timestamp_ns(values[1] - values[0], pool->bits, pool->period, nanoseconds) ? 1 : -1;
}

E2D_EXPORT void e2d_render_timestamp_destroy(SDL_GPUDevice *device, uint32_t abi, void *handle)
{
#if defined(SDL_PLATFORM_WIN32)
    if (handle && abi == 1 && e2d_d3d12_device(device)) {
        D3D12TimestampPool *pool = handle;
        if (pool->device == device) {
            d3d12_timestamp_destroy(pool);
        }
        return;
    }
#endif
    TimestampPool *pool = handle;
    if (!pool || abi != 1 || !compatible(device) || pool->renderer != (VulkanRenderer *)device->driverData) return;
    pool->destroy(pool->renderer->logicalDevice, pool->pool, NULL); SDL_free(pool);
}
