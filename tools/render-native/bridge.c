#include <stdint.h>
#include "memory-layout.h"

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

extern uint64_t e2d_views_memory(SDL_GPUDevice *);

typedef struct E2DRenderMemory {
    uint64_t textures, buffers, total;
} E2DRenderMemory;

static bool add(uint64_t *target, uint64_t bytes)
{
    if (bytes > UINT64_MAX - *target) return false;
    *target += bytes;
    return true;
}

/* The ABI is private to the pinned native package, never to a game or another SDL release. */
E2D_EXPORT int e2d_render_memory(SDL_GPUDevice *device, uint32_t abi, E2DRenderMemory *out)
{
    if (!out || !device || abi != 1) return -1;
    *out = (E2DRenderMemory){0};
    if (SDL_GetVersion() != SDL_VERSIONNUM(3, 4, 18) ||
        SDL_strcmp(SDL_GetRevision(), "SDL-3.4.18-release-3.4.18") != 0 ||
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return 0;
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData;
    if (!renderer || !renderer->allocatorLock || !renderer->memoryAllocator) return -1;
    E2DRenderMemory result = {0};
    bool valid = true;
    SDL_LockMutex(renderer->allocatorLock);
    for (uint32_t i = 0; i < VK_MAX_MEMORY_TYPES && valid; ++i) {
        VulkanMemorySubAllocator *sub = &renderer->memoryAllocator->subAllocators[i];
        for (uint32_t j = 0; j < sub->allocationCount && valid; ++j) {
            VulkanMemoryAllocation *allocation = sub->allocations[j];
            if (allocation->allocator != sub || allocation->usedRegionCount > allocation->usedRegionCapacity ||
                !add(&result.total, allocation->size)) { valid = false; break; }
            uint64_t occupied = 0;
            for (uint32_t k = 0; k < allocation->usedRegionCount; ++k) {
                VulkanMemoryUsedRegion *used = allocation->usedRegions[k];
                if (used->allocation != allocation || used->isBuffer > 1 || !add(&occupied, used->resourceSize) ||
                    occupied > allocation->size || !add(used->isBuffer ? &result.buffers : &result.textures, used->resourceSize)) {
                    valid = false; break;
                }
            }
        }
    }
    SDL_UnlockMutex(renderer->allocatorLock);
    uint64_t views = e2d_views_memory(device);
    if (!valid || !add(&result.textures, views) || !add(&result.total, views)) return -1;
    *out = result;
    return 1;
}
