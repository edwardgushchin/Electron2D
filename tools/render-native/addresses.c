#include <stdint.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif
#define ADDRESS_CACHE "Electron2D.gpu.address_buffers"
_Static_assert(offsetof(VkPhysicalDeviceVulkan12Features, bufferDeviceAddress) == offsetof(VkPhysicalDeviceVulkan12Features, samplerMirrorClampToEdge) + 38 * sizeof(VkBool32), "address feature word");

typedef struct AddressBuffer {
    VulkanBufferContainer container;
    VulkanBuffer native;
    VulkanUniformBuffer uniform;
    VulkanBuffer *buffers[1];
    VulkanMemoryUsedRegion region;
    VulkanMemoryAllocation allocation;
    VkDeviceAddress address;
    bool retired;
    struct AddressBuffer *next;
} AddressBuffer;
typedef struct AddressCache { SDL_Mutex *lock; AddressBuffer *buffers; uint64_t bytes; } AddressCache;
static bool compatible(SDL_GPUDevice *device)
{
    return device && SDL_GetVersion() == SDL_VERSIONNUM(3, 4, 18) &&
        SDL_strcmp(SDL_GetRevision(), "SDL-3.4.18-release-3.4.18") == 0 && SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") == 0;
}
E2D_EXPORT int e2d_render_address_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || (abi != 1 && abi != 2)) return -1;
    if (!compatible(device)) return 0;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    if (abi == 2) return r->vkCreateBuffer && r->vkAllocateMemory && r->vkBindBufferMemory ? 1 : 0;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures2 query = proc ? (PFN_vkGetPhysicalDeviceFeatures2)proc(r->instance, "vkGetPhysicalDeviceFeatures2") : NULL;
    PFN_vkGetDeviceProcAddr get = proc ? (PFN_vkGetDeviceProcAddr)proc(r->instance, "vkGetDeviceProcAddr") : NULL;
    if (r->physicalDeviceProperties.properties.apiVersion < VK_API_VERSION_1_2 || !query || !get || !r->vkCreateBuffer || !r->vkAllocateMemory || !r->vkBindBufferMemory) return 0;
    VkPhysicalDeviceVulkan12Features advanced = {.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_VULKAN_1_2_FEATURES};
    VkPhysicalDeviceFeatures2 features = {.sType = VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_FEATURES_2, .pNext = &advanced};
    query(r->physicalDevice, &features); return advanced.bufferDeviceAddress ? 1 : 0;
}
static void SDLCALL free_cache(void *unused, void *value)
{
    (void)unused; AddressCache *c = value;
    if (c->buffers) SDL_LogError(SDL_LOG_CATEGORY_GPU, "Address buffers require retirement before GPU device destruction");
    SDL_DestroyMutex(c->lock); SDL_free(c);
}
static AddressCache *cache(SDL_GPUDevice *device, bool create)
{
    SDL_PropertiesID props = SDL_GetGPUDeviceProperties(device);
    if (!create) return SDL_GetPointerProperty(props, ADDRESS_CACHE, NULL);
    if (!SDL_LockProperties(props)) return NULL;
    AddressCache *c = SDL_GetPointerProperty(props, ADDRESS_CACHE, NULL);
    if (!c) {
        c = SDL_calloc(1, sizeof(*c));
        if (c) {
            c->lock = SDL_CreateMutex();
            if (!c->lock) { SDL_free(c); c = NULL; }
            else if (!SDL_SetPointerPropertyWithCleanup(props, ADDRESS_CACHE, c, free_cache, NULL)) c = NULL;
        }
    }
    SDL_UnlockProperties(props); return c;
}
uint64_t e2d_addresses_memory(SDL_GPUDevice *device)
{
    AddressCache *c = cache(device, false); if (!c) return 0;
    SDL_LockMutex(c->lock); uint64_t bytes = c->bytes; SDL_UnlockMutex(c->lock); return bytes;
}
static void destroy(VulkanRenderer *r, AddressBuffer *b)
{
    if (b->native.buffer) r->vkDestroyBuffer(r->logicalDevice, b->native.buffer, NULL);
    if (b->allocation.memory) r->vkFreeMemory(r->logicalDevice, b->allocation.memory, NULL);
    SDL_free(b->container.debugName); SDL_free(b);
}
E2D_EXPORT SDL_GPUBuffer *e2d_render_address_create(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUBufferCreateInfo *info, uint32_t uniform, uint64_t *address)
{
    if (address) *address = 0;
    if (!address || !info || uniform > 3 || !info->size || !compatible(device) || abi != 1 || ((uniform & 2) && !SDL_GetBooleanProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.buffer_device_address", false))) return NULL;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    AddressBuffer *b = SDL_calloc(1, sizeof(*b)); if (!b) return NULL;
    VkBufferUsageFlags usage = VK_BUFFER_USAGE_TRANSFER_SRC_BIT | VK_BUFFER_USAGE_TRANSFER_DST_BIT;
    if (uniform & 2) usage |= VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT;
    if (info->usage & SDL_GPU_BUFFERUSAGE_VERTEX) usage |= VK_BUFFER_USAGE_VERTEX_BUFFER_BIT;
    if (info->usage & SDL_GPU_BUFFERUSAGE_INDEX) usage |= VK_BUFFER_USAGE_INDEX_BUFFER_BIT;
    if (info->usage & SDL_GPU_BUFFERUSAGE_INDIRECT) usage |= VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT;
    if (info->usage & (SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ | SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ | SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE)) usage |= VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;
    if (uniform & 1) usage |= VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT;
    VkBufferCreateInfo buffer = {.sType = VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO, .size = (uniform & 1) && info->size < MAX_UBO_SECTION_SIZE ? MAX_UBO_SECTION_SIZE : info->size, .usage = usage, .sharingMode = VK_SHARING_MODE_EXCLUSIVE};
    if (r->vkCreateBuffer(r->logicalDevice, &buffer, NULL, &b->native.buffer) != VK_SUCCESS) goto fail;
    VkMemoryRequirements requirements; r->vkGetBufferMemoryRequirements(r->logicalDevice, b->native.buffer, &requirements);
    uint32_t type = UINT32_MAX;
    for (uint32_t i = 0; i < r->memoryProperties.memoryTypeCount; i++) if ((requirements.memoryTypeBits & (1u << i)) && (r->memoryProperties.memoryTypes[i].propertyFlags & VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT)) { type = i; break; }
    if (type == UINT32_MAX) goto fail;
    VkMemoryAllocateFlagsInfo flags = {.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_FLAGS_INFO, .flags = VK_MEMORY_ALLOCATE_DEVICE_ADDRESS_BIT};
    VkMemoryAllocateInfo memory = {.sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO, .pNext = (uniform & 2) ? &flags : NULL, .allocationSize = requirements.size, .memoryTypeIndex = type};
    if (r->vkAllocateMemory(r->logicalDevice, &memory, NULL, &b->allocation.memory) != VK_SUCCESS || r->vkBindBufferMemory(r->logicalDevice, b->native.buffer, b->allocation.memory, 0) != VK_SUCCESS) goto fail;
    PFN_vkGetInstanceProcAddr instance = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr proc = instance ? (PFN_vkGetDeviceProcAddr)instance(r->instance, "vkGetDeviceProcAddr") : NULL;
    PFN_vkGetBufferDeviceAddress query = proc ? (PFN_vkGetBufferDeviceAddress)proc(r->logicalDevice, "vkGetBufferDeviceAddress") : NULL;
    if ((uniform & 2) && !query) goto fail;
    VkBufferDeviceAddressInfo query_info = {.sType = VK_STRUCTURE_TYPE_BUFFER_DEVICE_ADDRESS_INFO, .buffer = b->native.buffer};
    if (uniform & 2) { b->address = query(r->logicalDevice, &query_info); if (!b->address) goto fail; }
    b->container.activeBuffer = &b->native; b->container.buffers = b->buffers; b->container.bufferCount = b->container.bufferCapacity = 1; b->container.dedicated = true;
    b->buffers[0] = &b->native; b->native.container = &b->container; b->native.type = VULKAN_BUFFER_TYPE_GPU; b->native.usage = info->usage; b->native.size = info->size;
    // Transfer commands retain this standalone allocation; it never enters SDL's relocatable allocator.
    b->uniform.buffer = &b->native; b->native.usedRegion = &b->region; b->region.allocation = &b->allocation; b->allocation.size = requirements.size;
    AddressCache *c = cache(device, true); if (!c) goto fail;
    SDL_LockMutex(c->lock); b->next = c->buffers; c->buffers = b; c->bytes += requirements.size; SDL_UnlockMutex(c->lock);
    *address = b->address; return (SDL_GPUBuffer *)&b->container;
fail:
    destroy(r, b); SDL_SetError("Addressable GPU buffer allocation failed"); return NULL;
}
static void retire(SDL_GPUDevice *device, SDL_GPUBuffer *handle, bool all)
{
    AddressCache *c = cache(device, false); if (!c) return;
    SDL_LockMutex(c->lock);
    bool pending = false;
    for (AddressBuffer *b = c->buffers; b; b = b->next) { if (all || (SDL_GPUBuffer *)&b->container == handle) b->retired = true; pending |= b->retired; }
    SDL_UnlockMutex(c->lock); if (!pending) return; SDL_WaitForGPUIdle(device);
    SDL_LockMutex(c->lock);
    for (AddressBuffer **p = &c->buffers; *p;) {
        AddressBuffer *b = *p;
        if (b->retired && SDL_GetAtomicInt(&b->native.referenceCount) == 0 && SDL_GetAtomicInt(&b->allocation.referenceCount) == 0) { *p = b->next; c->bytes -= b->allocation.size; destroy((VulkanRenderer *)device->driverData, b); }
        else p = &b->next;
    }
    SDL_UnlockMutex(c->lock);
}
E2D_EXPORT void e2d_render_address_destroy(SDL_GPUDevice *device, uint32_t abi, SDL_GPUBuffer *buffer)
{
    if (abi == 1 && compatible(device)) retire(device, buffer, false);
}
E2D_EXPORT void e2d_render_addresses_release(SDL_GPUDevice *device, uint32_t abi)
{
    if (abi != 1 || !compatible(device)) return;
    retire(device, NULL, true);
    AddressCache *c = cache(device, false); if (c && !c->buffers) SDL_ClearProperty(SDL_GetGPUDeviceProperties(device), ADDRESS_CACHE);
}
E2D_EXPORT int e2d_render_addresses_prepare(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands)
{
    if (abi != 1 || !commands || !compatible(device)) return -1;
    VulkanCommandBuffer *command = (VulkanCommandBuffer *)commands;
    VulkanRenderer *r = (VulkanRenderer *)device->driverData;
    if (command->common.device != device || command->renderer != r) return -1;
    AddressCache *c = cache(device, false); if (!c) return 1;
    SDL_LockMutex(c->lock);
    for (AddressBuffer *b = c->buffers; b; b = b->next) {
        if (b->retired) continue;
        bool found = false;
        for (int32_t i = 0; i < command->usedBufferCount; i++) if (command->usedBuffers[i] == &b->native) { found = true; break; }
        if (found) continue;
        if (command->usedBufferCount == command->usedBufferCapacity) {
            int32_t capacity = command->usedBufferCapacity ? command->usedBufferCapacity * 2 : 16;
            VulkanBuffer **buffers = SDL_realloc(command->usedBuffers, (size_t)capacity * sizeof(*buffers));
            if (!buffers) { SDL_UnlockMutex(c->lock); return -1; }
            command->usedBuffers = buffers; command->usedBufferCapacity = capacity;
        }
        command->usedBuffers[command->usedBufferCount++] = &b->native; SDL_AtomicIncRef(&b->native.referenceCount);
    }
    SDL_UnlockMutex(c->lock);
    VkMemoryBarrier memory = {.sType = VK_STRUCTURE_TYPE_MEMORY_BARRIER, .srcAccessMask = VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT, .dstAccessMask = VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT};
    r->vkCmdPipelineBarrier(command->commandBuffer, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, 0, 1, &memory, 0, NULL, 0, NULL);
    return 1;
}

E2D_EXPORT int e2d_render_address_uniform(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, uint32_t stage, uint32_t slot, SDL_GPUBuffer *buffer, void **previous)
{
    if (abi != 1 || !commands || stage > 2 || slot >= MAX_UNIFORM_BUFFERS_PER_STAGE || !previous || !compatible(device)) return -1;
    VulkanCommandBuffer *c = (VulkanCommandBuffer *)commands;
    if (c->common.device != device || c->renderer != (VulkanRenderer *)device->driverData) return -1;
    VulkanUniformBuffer **uniforms = stage == 0 ? c->vertexUniformBuffers : stage == 1 ? c->fragmentUniformBuffers : c->computeUniformBuffers;
    if (buffer) {
        AddressCache *owner = cache(device, false); if (!owner) return -1;
        SDL_LockMutex(owner->lock); AddressBuffer *b = owner->buffers;
        while (b && (SDL_GPUBuffer *)&b->container != buffer) b = b->next;
        if (!b || b->retired) { SDL_UnlockMutex(owner->lock); return -1; }
        *previous = uniforms[slot]; uniforms[slot] = &b->uniform; SDL_UnlockMutex(owner->lock);
    } else uniforms[slot] = *previous;
    if (stage == 0) { c->needNewVertexUniformDescriptorSet = true; c->needNewVertexUniformOffsets = true; }
    else if (stage == 1) { c->needNewFragmentUniformDescriptorSet = true; c->needNewFragmentUniformOffsets = true; }
    else { c->needNewComputeUniformDescriptorSet = true; c->needNewComputeUniformOffsets = true; }
    return 1;
}
