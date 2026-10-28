#include <stdint.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#endif

extern SDL_GPUTexture *e2d_render_view_region(SDL_GPUDevice *, uint32_t, SDL_GPUTexture *, uint32_t *, uint32_t *);
extern bool e2d_views_general_sample(SDL_GPUDevice *, VkImageView, SDL_GPUTexture *const *, uint32_t);

#define IMAGE_CACHE "Electron2D.render.image_layouts"
typedef struct ImageLayout {
    uint32_t counts[8], masks;
    VulkanGraphicsPipelineResourceLayout native;
    DescriptorSetLayout sets[4];
    struct ImageLayout *next;
} ImageLayout;
typedef struct ImageCache { SDL_Mutex *lock; ImageLayout *layouts; bool released; } ImageCache;

static void SDLCALL free_cache(void *userdata, void *value)
{
    (void)userdata; ImageCache *cache = value;
    if (cache->layouts && !cache->released) SDL_LogError(SDL_LOG_CATEGORY_GPU, "Image layouts require retirement before GPU device destruction");
    while (cache->layouts) { ImageLayout *node = cache->layouts; cache->layouts = node->next; SDL_free(node); }
    SDL_DestroyMutex(cache->lock); SDL_free(cache);
}
static bool compatible(SDL_GPUDevice *device)
{
    return device && SDL_GetVersion() == SDL_VERSIONNUM(3, 4, 18) &&
        SDL_strcmp(SDL_GetRevision(), "SDL-3.4.18-release-3.4.18") == 0 &&
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") == 0;
}
E2D_EXPORT int e2d_render_image_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (!compatible(device)) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr instance = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr proc = instance ? (PFN_vkGetDeviceProcAddr)instance(renderer->instance, "vkGetDeviceProcAddr") : NULL;
    const char *names[] = {"vkCreateDescriptorSetLayout", "vkCreatePipelineLayout", "vkCreateDescriptorPool", "vkAllocateDescriptorSets", "vkUpdateDescriptorSets", "vkCmdBindDescriptorSets", "vkCmdPipelineBarrier"};
    if (!proc) return 0;
    for (uint32_t i = 0; i < SDL_arraysize(names); i++) if (!proc(renderer->logicalDevice, names[i])) return 0;
    return 1;
}
static ImageCache *image_cache(SDL_GPUDevice *device)
{
    SDL_PropertiesID props = SDL_GetGPUDeviceProperties(device);
    if (!SDL_LockProperties(props)) return NULL;
    ImageCache *cache = SDL_GetPointerProperty(props, IMAGE_CACHE, NULL);
    if (!cache) {
        cache = SDL_calloc(1, sizeof(*cache));
        if (cache) {
            cache->lock = SDL_CreateMutex();
            if (!cache->lock) { SDL_free(cache); cache = NULL; }
            else if (!SDL_SetPointerPropertyWithCleanup(props, IMAGE_CACHE, cache, free_cache, NULL)) cache = NULL;
        }
    }
    SDL_UnlockProperties(props); return cache;
}
static void destroy_layout(VulkanRenderer *renderer, ImageLayout *layout)
{
    if (layout->native.pipelineLayout) renderer->vkDestroyPipelineLayout(renderer->logicalDevice, layout->native.pipelineLayout, NULL);
    for (uint32_t i = 0; i < 4; i++) if (layout->sets[i].descriptorSetLayout)
        renderer->vkDestroyDescriptorSetLayout(renderer->logicalDevice, layout->sets[i].descriptorSetLayout, NULL);
}
E2D_EXPORT void e2d_render_image_release(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return;
    ImageCache *cache = SDL_GetPointerProperty(SDL_GetGPUDeviceProperties(device), IMAGE_CACHE, NULL);
    if (!cache) return;
    SDL_WaitForGPUIdle(device);
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData;
    for (ImageLayout *layout = cache->layouts; layout; layout = layout->next) destroy_layout(renderer, layout);
    cache->released = true; SDL_ClearProperty(SDL_GetGPUDeviceProperties(device), IMAGE_CACHE);
}
static bool create_set(VulkanRenderer *renderer, DescriptorSetLayout *set, uint32_t stage, uint32_t masks)
{
    VkDescriptorSetLayoutBinding bindings[36]; uint32_t count = 0;
    for (uint32_t i = 0; i < set->samplerCount; i++) { bindings[count] = (VkDescriptorSetLayoutBinding){.binding = count, .descriptorType = VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, .descriptorCount = 1, .stageFlags = stage}; count++; }
    for (uint32_t i = 0; i < set->storageTextureCount; i++) { bindings[count] = (VkDescriptorSetLayoutBinding){.binding = count, .descriptorType = (masks & (1u << i)) ? VK_DESCRIPTOR_TYPE_STORAGE_IMAGE : VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, .descriptorCount = 1, .stageFlags = stage}; count++; }
    for (uint32_t i = 0; i < set->storageBufferCount; i++) { bindings[count] = (VkDescriptorSetLayoutBinding){.binding = count, .descriptorType = VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, .descriptorCount = 1, .stageFlags = stage}; count++; }
    for (uint32_t i = 0; i < set->uniformBufferCount; i++) { bindings[count] = (VkDescriptorSetLayoutBinding){.binding = count, .descriptorType = VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC, .descriptorCount = 1, .stageFlags = stage}; count++; }
    VkDescriptorSetLayoutCreateInfo info = {.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO, .bindingCount = count, .pBindings = bindings};
    if (renderer->vkCreateDescriptorSetLayout(renderer->logicalDevice, &info, NULL, &set->descriptorSetLayout) != VK_SUCCESS) return false;
    return true;
}
VulkanGraphicsPipelineResourceLayout *e2d_images_layout(SDL_GPUDevice *device, const uint32_t *counts, uint32_t masks)
{
    if (!masks || (masks & ~65535u) || !counts || !compatible(device)) return NULL;
    for (uint32_t stage = 0; stage < 2; stage++)
        if (counts[stage * 4] > 16 || counts[stage * 4 + 1] > 8 || counts[stage * 4 + 2] > 8 || counts[stage * 4 + 3] > 4 || ((masks >> (stage * 8)) & 255) >> counts[stage * 4 + 1]) return NULL;
    ImageCache *cache = image_cache(device); if (!cache) return NULL;
    SDL_LockMutex(cache->lock);
    for (ImageLayout *node = cache->layouts; node; node = node->next) if (node->masks == masks && SDL_memcmp(node->counts, counts, sizeof(node->counts)) == 0) { SDL_UnlockMutex(cache->lock); return &node->native; }
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData;
    ImageLayout *node = SDL_calloc(1, sizeof(*node)); if (!node) { SDL_UnlockMutex(cache->lock); return NULL; }
    SDL_memcpy(node->counts, counts, sizeof(node->counts)); node->masks = masks;
    node->native = (VulkanGraphicsPipelineResourceLayout){.vertexSamplerCount = counts[0], .vertexStorageTextureCount = counts[1], .vertexStorageBufferCount = counts[2], .vertexUniformBufferCount = counts[3], .fragmentSamplerCount = counts[4], .fragmentStorageTextureCount = counts[5], .fragmentStorageBufferCount = counts[6], .fragmentUniformBufferCount = counts[7]};
    VkDescriptorSetLayout layouts[4];
    for (uint32_t i = 0; i < 4; i++) {
        uint32_t base = (i / 2) * 4;
        if (i & 1) node->sets[i].uniformBufferCount = counts[base + 3];
        else { node->sets[i].samplerCount = counts[base]; node->sets[i].storageTextureCount = counts[base + 1]; node->sets[i].storageBufferCount = counts[base + 2]; }
        if (!create_set(renderer, &node->sets[i], i < 2 ? VK_SHADER_STAGE_VERTEX_BIT : VK_SHADER_STAGE_FRAGMENT_BIT, (i & 1) ? 0 : ((masks >> (base * 2)) & 255))) goto fail;
        node->native.descriptorSetLayouts[i] = &node->sets[i]; layouts[i] = node->sets[i].descriptorSetLayout;
    }
    VkPipelineLayoutCreateInfo info = {.sType = VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO, .setLayoutCount = 4, .pSetLayouts = layouts};
    if (renderer->vkCreatePipelineLayout(renderer->logicalDevice, &info, NULL, &node->native.pipelineLayout) != VK_SUCCESS) goto fail;
    uint32_t first = (uint32_t)SDL_AddAtomicInt(&renderer->layoutResourceID, 4);
    for (uint32_t i = 0; i < 4; i++) node->sets[i].ID = first + i;
    node->next = cache->layouts; cache->layouts = node; SDL_UnlockMutex(cache->lock); return &node->native;
fail:
    destroy_layout(renderer, node); SDL_free(node); SDL_UnlockMutex(cache->lock); return NULL;
}
static bool track_texture(VulkanCommandBuffer *command, VulkanTexture *texture)
{
    for (int i = 0; i < command->usedTextureCount; i++) if (command->usedTextures[i] == texture) return true;
    if (command->usedTextureCount == command->usedTextureCapacity) {
        int capacity = SDL_max(4, command->usedTextureCapacity * 2);
        void *items = SDL_realloc(command->usedTextures, sizeof(VulkanTexture *) * capacity); if (!items) return false;
        command->usedTextures = items; command->usedTextureCapacity = capacity;
    }
    SDL_AtomicIncRef(&texture->referenceCount); command->usedTextures[command->usedTextureCount++] = texture; return true;
}
static VulkanCommandBuffer *checked_command(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands)
{
    if (!device || abi != 1 || !commands || !compatible(device)) return NULL;
    VulkanCommandBuffer *command = (VulkanCommandBuffer *)commands;
    return command->common.device == device && command->renderer == (VulkanRenderer *)device->driverData && !(device->debug_mode && command->common.submitted) ? command : NULL;
}
static int transition_images(SDL_GPUDevice *device, VulkanCommandBuffer *command, SDL_GPUTexture *const *textures, uint32_t count, uint32_t general)
{
    VulkanRenderer *renderer = command->renderer; VulkanTexture *roots[16];
    for (uint32_t i = 0; i < count; i++) {
        VulkanTextureContainer *container = (VulkanTextureContainer *)textures[i];
        if (!container || !container->activeTexture || container->activeTexture->aspectFlags != VK_IMAGE_ASPECT_COLOR_BIT || !container->activeTexture->subresources[0].computeWriteView) return -1;
        uint32_t layer = 0, level = 0; SDL_GPUTexture *root = e2d_render_view_region(device, 1, textures[i], &layer, &level);
        if (!root) return -1;
        roots[i] = ((VulkanTextureContainer *)root)->activeTexture;
    }
    for (uint32_t i = 0; i < count; i++) if (!track_texture(command, roots[i]) || !track_texture(command, ((VulkanTextureContainer *)textures[i])->activeTexture)) return -1;
    for (uint32_t i = 0; i < count; i++) {
        VulkanTexture *texture = roots[i];
        bool repeated = false; for (uint32_t j = 0; j < i; j++) repeated |= roots[j]->image == texture->image;
        if (repeated) continue;
        VkImageLayout ordinary = (texture->usage & (SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ)) ? VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL :
            (texture->usage & SDL_GPU_TEXTUREUSAGE_COLOR_TARGET) ? VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL :
            (texture->usage & SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ) ? VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL : VK_IMAGE_LAYOUT_GENERAL;
        VkImageMemoryBarrier barrier = {.sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER, .srcAccessMask = VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT, .dstAccessMask = VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT,
            .oldLayout = general ? ordinary : VK_IMAGE_LAYOUT_GENERAL, .newLayout = general ? VK_IMAGE_LAYOUT_GENERAL : ordinary, .srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED, .dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED, .image = texture->image,
            .subresourceRange = {.aspectMask = texture->aspectFlags, .levelCount = texture->levelCount, .layerCount = texture->layerCount}};
        renderer->vkCmdPipelineBarrier(command->commandBuffer, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, 0, 0, NULL, 0, NULL, 1, &barrier);
    }
    return 1;
}
E2D_EXPORT int e2d_render_image_transition(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, SDL_GPUTexture *const *textures, uint32_t count, uint32_t general)
{
    if (!textures || count > 16 || general > 1) return -1;
    VulkanCommandBuffer *command = checked_command(device, abi, commands); if (!command) return -1;
    if (device->debug_mode && (command->common.render_pass.in_progress || command->common.compute_pass.in_progress || command->common.copy_pass.in_progress)) return -1;
    return transition_images(device, command, textures, count, general);
}
E2D_EXPORT int e2d_render_bind_image(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, SDL_GPUTexture *image, uint32_t slot, uint32_t fragment, uint32_t storage)
{
    if (!image || slot >= 8 || fragment > 1 || storage > 1) return -1;
    VulkanCommandBuffer *command = checked_command(device, abi, commands); if (!command) return -1;
    VulkanTexture *texture = ((VulkanTextureContainer *)image)->activeTexture;
    if (!texture || !command->currentGraphicsPipeline || !track_texture(command, texture)) return -1;
    VkImageView view = storage ? texture->subresources[0].computeWriteView : texture->fullView; if (!view) return -1;
    if (fragment) { command->fragmentStorageTextureViewBindings[slot] = view; command->needNewFragmentResourceDescriptorSet = true; }
    else { command->vertexStorageTextureViewBindings[slot] = view; command->needNewVertexResourceDescriptorSet = true; }
    return 1;
}
static VkDescriptorSet acquire_set(VulkanRenderer *renderer, VulkanCommandBuffer *command, DescriptorSetLayout *layout, uint32_t mask)
{
    DescriptorSetCache *cache = command->descriptorSetCache;
    if (layout->ID >= cache->poolCount) {
        void *items = SDL_realloc(cache->pools, sizeof(DescriptorSetPool) * (layout->ID + 1)); if (!items) return VK_NULL_HANDLE;
        cache->pools = items; SDL_memset(cache->pools + cache->poolCount, 0, sizeof(DescriptorSetPool) * (layout->ID + 1 - cache->poolCount)); cache->poolCount = layout->ID + 1;
    }
    DescriptorSetPool *pool = &cache->pools[layout->ID];
    if (pool->descriptorSetIndex == pool->descriptorSetCount) {
        VkDescriptorPoolSize sizes[4]; uint32_t n = 0, writes = 0;
        for (uint32_t i = 0; i < layout->storageTextureCount; i++) writes += (mask >> i) & 1;
        if (layout->samplerCount) sizes[n++] = (VkDescriptorPoolSize){VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, layout->samplerCount * DESCRIPTOR_POOL_SIZE};
        if (writes) sizes[n++] = (VkDescriptorPoolSize){VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, writes * DESCRIPTOR_POOL_SIZE};
        if (layout->storageTextureCount > writes) sizes[n++] = (VkDescriptorPoolSize){VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, (layout->storageTextureCount - writes) * DESCRIPTOR_POOL_SIZE};
        if (layout->storageBufferCount) sizes[n++] = (VkDescriptorPoolSize){VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, layout->storageBufferCount * DESCRIPTOR_POOL_SIZE};
        VkDescriptorPoolCreateInfo info = {.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO, .maxSets = DESCRIPTOR_POOL_SIZE, .poolSizeCount = n, .pPoolSizes = sizes}; VkDescriptorPool created;
        if (renderer->vkCreateDescriptorPool(renderer->logicalDevice, &info, NULL, &created) != VK_SUCCESS) return VK_NULL_HANDLE;
        VkDescriptorSetLayout layouts[DESCRIPTOR_POOL_SIZE]; VkDescriptorSet sets[DESCRIPTOR_POOL_SIZE];
        for (uint32_t i = 0; i < DESCRIPTOR_POOL_SIZE; i++) layouts[i] = layout->descriptorSetLayout;
        VkDescriptorSetAllocateInfo allocation = {.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO, .descriptorPool = created, .descriptorSetCount = DESCRIPTOR_POOL_SIZE, .pSetLayouts = layouts};
        if (renderer->vkAllocateDescriptorSets(renderer->logicalDevice, &allocation, sets) != VK_SUCCESS) goto fail;
        void *items = SDL_realloc(pool->descriptorPools, sizeof(VkDescriptorPool) * (pool->poolCount + 1)); if (!items) goto fail; pool->descriptorPools = items;
        items = SDL_realloc(pool->descriptorSets, sizeof(VkDescriptorSet) * (pool->descriptorSetCount + DESCRIPTOR_POOL_SIZE)); if (!items) goto fail; pool->descriptorSets = items;
        pool->descriptorPools[pool->poolCount++] = created; SDL_memcpy(pool->descriptorSets + pool->descriptorSetCount, sets, sizeof(sets)); pool->descriptorSetCount += DESCRIPTOR_POOL_SIZE;
        goto ready;
fail:
        renderer->vkDestroyDescriptorPool(renderer->logicalDevice, created, NULL); return VK_NULL_HANDLE;
    }
ready:
    return pool->descriptorSets[pool->descriptorSetIndex++];
}
E2D_EXPORT int e2d_render_prepare_compute_images(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, SDL_GPUTexture *const *general, uint32_t count, uint32_t prepare)
{
    if (prepare > 1 || abi != 2 || !general || count > 8) { SDL_SetError("Invalid compute image adapter ABI or image count"); return -1; }
    VulkanCommandBuffer *command = checked_command(device, 1, commands);
    if (!command || !command->currentComputePipeline || (device->debug_mode && !command->common.compute_pass.in_progress)) { SDL_SetError("Compute image descriptors require an active owned compute pipeline"); return -1; }
    for (uint32_t i = 0; i < count; i++) if (!general[i] || !((VulkanTextureContainer *)general[i])->activeTexture) { SDL_SetError("Invalid compute storage image"); return -1; }
    VulkanRenderer *renderer = command->renderer;
    VulkanComputePipelineResourceLayout *pipeline = command->currentComputePipeline->resourceLayout;
    if (pipeline->numSamplers > 16 || pipeline->numReadonlyStorageTextures > 8 || pipeline->numReadonlyStorageBuffers > 8) { SDL_SetError("Compute image descriptors exceed native capacity"); return -1; }
    if (pipeline->numReadWriteStorageTextures != count || command->readWriteComputeStorageTextureSubresourceCount != 0) { SDL_SetError("Compute image adapter requires its own complete storage bindings"); return -1; }
    if (!prepare) {
        int result = transition_images(device, command, general, count, 0);
        for (uint32_t i = 0; i < count; i++) { command->readWriteComputeStorageTextureViewBindings[i] = VK_NULL_HANDLE; if (device->debug_mode) command->common.compute_pass.read_write_storage_texture_bound[i] = false; }
        return result;
    }
    bool shared = false;
    for (uint32_t i = 0; i < pipeline->numSamplers; i++) shared |= e2d_views_general_sample(device, command->computeSamplerTextureViewBindings[i], general, count);
    if (shared) {
        DescriptorSetLayout *layout = pipeline->descriptorSetLayouts[0];
        VkDescriptorSet set = acquire_set(renderer, command, layout, 0); if (!set) { SDL_SetError("Could not acquire compute image descriptor set"); return -1; }
        VkWriteDescriptorSet writes[32]; VkDescriptorImageInfo images[24]; VkDescriptorBufferInfo buffers[8]; uint32_t n = 0;
        for (uint32_t i = 0; i < pipeline->numSamplers; i++) {
            VkImageView view = command->computeSamplerTextureViewBindings[i];
            images[n] = (VkDescriptorImageInfo){command->computeSamplerBindings[i], view, e2d_views_general_sample(device, view, general, count) ? VK_IMAGE_LAYOUT_GENERAL : VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL};
            writes[n] = (VkWriteDescriptorSet){.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET, .dstSet = set, .dstBinding = n, .descriptorCount = 1, .descriptorType = VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, .pImageInfo = &images[n]}; n++;
        }
        for (uint32_t i = 0; i < pipeline->numReadonlyStorageTextures; i++) {
            images[n] = (VkDescriptorImageInfo){.imageView = command->readOnlyComputeStorageTextureViewBindings[i], .imageLayout = VK_IMAGE_LAYOUT_GENERAL};
            writes[n] = (VkWriteDescriptorSet){.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET, .dstSet = set, .dstBinding = n, .descriptorCount = 1, .descriptorType = VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, .pImageInfo = &images[n]}; n++;
        }
        for (uint32_t i = 0; i < pipeline->numReadonlyStorageBuffers; i++) {
            buffers[i] = (VkDescriptorBufferInfo){command->readOnlyComputeStorageBufferBindings[i], 0, VK_WHOLE_SIZE};
            writes[n] = (VkWriteDescriptorSet){.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET, .dstSet = set, .dstBinding = n, .descriptorCount = 1, .descriptorType = VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, .pBufferInfo = &buffers[i]}; n++;
        }
        renderer->vkUpdateDescriptorSets(renderer->logicalDevice, n, writes, 0, NULL);
        renderer->vkCmdBindDescriptorSets(command->commandBuffer, VK_PIPELINE_BIND_POINT_COMPUTE, pipeline->pipelineLayout, 0, 1, &set, 0, NULL);
        command->computeReadOnlyDescriptorSet = set; command->needNewComputeReadOnlyDescriptorSet = false;
    }
    // SDL's ordinary compute binding transitions one subresource; the adapter owns the full physical image scope.
    if (transition_images(device, command, general, count, 1) != 1) { SDL_SetError("Could not retain compute storage images"); return -1; }
    for (uint32_t i = 0; i < count; i++) { command->readWriteComputeStorageTextureViewBindings[i] = ((VulkanTextureContainer *)general[i])->activeTexture->subresources[0].computeWriteView; if (device->debug_mode) command->common.compute_pass.read_write_storage_texture_bound[i] = true; }
    command->needNewComputeReadWriteDescriptorSet = true;
    return 1;
}
E2D_EXPORT int e2d_render_prepare_images(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, uint32_t masks, SDL_GPUTexture *const *general, uint32_t count)
{
    if (!masks || masks > 65535 || !general || count > 16) return -1;
    VulkanCommandBuffer *command = checked_command(device, abi, commands); if (!command || !command->currentGraphicsPipeline) return -1;
    VulkanRenderer *renderer = command->renderer; VulkanGraphicsPipelineResourceLayout *pipeline = command->currentGraphicsPipeline->resourceLayout;
    ImageCache *cache = SDL_GetPointerProperty(SDL_GetGPUDeviceProperties(device), IMAGE_CACHE, NULL); if (!cache) return -1;
    SDL_LockMutex(cache->lock);
    ImageLayout *profile = cache->layouts;
    while (profile && (&profile->native != pipeline || profile->masks != masks)) profile = profile->next;
    SDL_UnlockMutex(cache->lock); if (!profile) return -1;
    for (uint32_t i = 0; i < count; i++) if (!general[i] || !((VulkanTextureContainer *)general[i])->activeTexture) return -1;
    for (uint32_t stage = 0; stage < 2; stage++) {
        DescriptorSetLayout *layout = pipeline->descriptorSetLayouts[stage * 2];
        VkDescriptorSet set = acquire_set(renderer, command, layout, (masks >> (stage * 8)) & 255); if (!set) return -1;
        VkWriteDescriptorSet writes[32]; VkDescriptorImageInfo images[24]; VkDescriptorBufferInfo buffers[8]; uint32_t n = 0, image_count = 0;
        VkImageView *sampler_views = stage ? command->fragmentSamplerTextureViewBindings : command->vertexSamplerTextureViewBindings;
        VkSampler *samplers = stage ? command->fragmentSamplerBindings : command->vertexSamplerBindings;
        VkImageView *storage_views = stage ? command->fragmentStorageTextureViewBindings : command->vertexStorageTextureViewBindings;
        VkBuffer *storage_buffers = stage ? command->fragmentStorageBufferBindings : command->vertexStorageBufferBindings;
        for (uint32_t i = 0; i < layout->samplerCount; i++) {
            VkImageLayout image_layout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
            for (uint32_t j = 0; j < count; j++) if (sampler_views[i] == ((VulkanTextureContainer *)general[j])->activeTexture->fullView) image_layout = VK_IMAGE_LAYOUT_GENERAL;
            if (image_layout != VK_IMAGE_LAYOUT_GENERAL && e2d_views_general_sample(device, sampler_views[i], general, count)) image_layout = VK_IMAGE_LAYOUT_GENERAL;
            images[image_count] = (VkDescriptorImageInfo){samplers[i], sampler_views[i], image_layout};
            writes[n] = (VkWriteDescriptorSet){.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET, .dstSet = set, .dstBinding = n, .descriptorCount = 1, .descriptorType = VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, .pImageInfo = &images[image_count++]}; n++;
        }
        for (uint32_t i = 0; i < layout->storageTextureCount; i++) {
            images[image_count] = (VkDescriptorImageInfo){.imageView = storage_views[i], .imageLayout = VK_IMAGE_LAYOUT_GENERAL};
            writes[n] = (VkWriteDescriptorSet){.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET, .dstSet = set, .dstBinding = n, .descriptorCount = 1, .descriptorType = (masks & (1u << (stage * 8 + i))) ? VK_DESCRIPTOR_TYPE_STORAGE_IMAGE : VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, .pImageInfo = &images[image_count++]}; n++;
        }
        for (uint32_t i = 0; i < layout->storageBufferCount; i++) {
            buffers[i] = (VkDescriptorBufferInfo){storage_buffers[i], 0, VK_WHOLE_SIZE}; writes[n] = (VkWriteDescriptorSet){.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET, .dstSet = set, .dstBinding = n, .descriptorCount = 1, .descriptorType = VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, .pBufferInfo = &buffers[i]}; n++;
        }
        renderer->vkUpdateDescriptorSets(renderer->logicalDevice, n, writes, 0, NULL);
        renderer->vkCmdBindDescriptorSets(command->commandBuffer, VK_PIPELINE_BIND_POINT_GRAPHICS, pipeline->pipelineLayout, stage * 2, 1, &set, 0, NULL);
        if (stage) { command->fragmentResourceDescriptorSet = set; command->needNewFragmentResourceDescriptorSet = false; }
        else { command->vertexResourceDescriptorSet = set; command->needNewVertexResourceDescriptorSet = false; }
    }
    return 1;
}
