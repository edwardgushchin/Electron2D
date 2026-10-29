#include "pinned-core.h"
#include <stdint.h>
#include <math.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

#if defined(_WIN32)
#define E2D_EXPORT __declspec(dllexport)
#define E2D_THREAD_LOCAL __declspec(thread)
#else
#define E2D_EXPORT __attribute__((visibility("default")))
#define E2D_THREAD_LOCAL _Thread_local
#endif

typedef struct RasterLayout
{
    uint32_t counts[8];
    int next_id;
    struct RasterLayout *next;
} RasterLayout;
typedef struct RasterCache { SDL_Mutex *lock; RasterLayout *layouts; } RasterCache;
static void SDLCALL destroy_cache(void *userdata, void *value)
{
    (void)userdata; RasterCache *cache = value;
    while (cache->layouts) { RasterLayout *entry = cache->layouts; cache->layouts = entry->next; SDL_free(entry); }
    SDL_DestroyMutex(cache->lock); SDL_free(cache);
}
static RasterCache *raster_cache(SDL_GPUDevice *device)
{
    SDL_PropertiesID props = SDL_GetGPUDeviceProperties(device);
    if (!SDL_LockProperties(props)) return NULL;
    RasterCache *cache = SDL_GetPointerProperty(props, "Electron2D.render.raster_layouts", NULL);
    if (!cache) {
        cache = SDL_calloc(1, sizeof(*cache));
        if (cache) {
            cache->lock = SDL_CreateMutex();
            if (!cache->lock) { SDL_free(cache); cache = NULL; }
            else if (!SDL_SetPointerPropertyWithCleanup(props, "Electron2D.render.raster_layouts", cache, destroy_cache, NULL)) cache = NULL;
        }
    }
    SDL_UnlockProperties(props); return cache;
}
typedef struct MultisampleState { uint32_t mask, flags; float minimum; } MultisampleState;
typedef struct DepthStencilState { uint32_t flags, front_mask, front_write, front_reference, back_mask, back_write, back_reference; float minimum, maximum; } DepthStencilState;
extern VulkanGraphicsPipelineResourceLayout *e2d_images_layout(SDL_GPUDevice *, const uint32_t *, uint32_t);
extern int e2d_render_image_support(SDL_GPUDevice *, uint32_t);
typedef struct RasterState { PFN_vkCreateGraphicsPipelines create; float width; bool discard; MultisampleState samples; DepthStencilState depth; SDL_GPUDevice *device; const uint32_t *counts; uint32_t images, logic; VulkanGraphicsPipelineResourceLayout *image_layout; } RasterState;
static E2D_THREAD_LOCAL RasterState state;

static VkResult VKAPI_CALL raster_pipeline(VkDevice device, VkPipelineCache cache, uint32_t count,
    const VkGraphicsPipelineCreateInfo *infos, const VkAllocationCallbacks *allocator, VkPipeline *pipelines)
{
    if (count != 1 || !infos || !infos->pRasterizationState || !state.create) return VK_ERROR_INITIALIZATION_FAILED;
    VkGraphicsPipelineCreateInfo info = *infos;
    VkPipelineRasterizationStateCreateInfo raster = *info.pRasterizationState;
    raster.rasterizerDiscardEnable = state.discard; raster.lineWidth = state.width; info.pRasterizationState = &raster;
    VkPipelineMultisampleStateCreateInfo multisample;
    VkSampleMask mask = state.samples.mask;
    if (info.pMultisampleState) {
        multisample = *info.pMultisampleState;
        multisample.sampleShadingEnable = (state.samples.flags & 2) != 0;
        multisample.minSampleShading = state.samples.minimum;
        multisample.alphaToOneEnable = (state.samples.flags & 4) != 0;
        multisample.pSampleMask = (state.samples.flags & 1) ? &mask : NULL;
        info.pMultisampleState = &multisample;
    }
    VkPipelineDepthStencilStateCreateInfo depth;
    VkPipelineDynamicStateCreateInfo dynamic;
    VkDynamicState dynamic_states[16];
    if (state.depth.flags) {
        if (!info.pDepthStencilState) return VK_ERROR_INITIALIZATION_FAILED;
        depth = *info.pDepthStencilState;
        depth.depthBoundsTestEnable = (state.depth.flags & 1) != 0;
        depth.minDepthBounds = state.depth.minimum; depth.maxDepthBounds = state.depth.maximum;
        if (state.depth.flags & 2) {
            depth.front.compareMask = state.depth.front_mask; depth.front.writeMask = state.depth.front_write; depth.front.reference = state.depth.front_reference;
            depth.back.compareMask = state.depth.back_mask; depth.back.writeMask = state.depth.back_write; depth.back.reference = state.depth.back_reference;
            if (info.pDynamicState) {
                dynamic = *info.pDynamicState;
                if (dynamic.dynamicStateCount > SDL_arraysize(dynamic_states)) return VK_ERROR_INITIALIZATION_FAILED;
                uint32_t n = 0;
                for (uint32_t i = 0; i < dynamic.dynamicStateCount; i++)
                    if (dynamic.pDynamicStates[i] != VK_DYNAMIC_STATE_STENCIL_REFERENCE) dynamic_states[n++] = dynamic.pDynamicStates[i];
                // SDL's shared dynamic reference must not override the copied independent faces.
                dynamic.dynamicStateCount = n; dynamic.pDynamicStates = dynamic_states; info.pDynamicState = &dynamic;
            }
        }
        info.pDepthStencilState = &depth;
    }
    VkPipelineColorBlendStateCreateInfo color = {.sType = VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO};
    if (state.logic) {
        if (info.pColorBlendState) color = *info.pColorBlendState;
        color.logicOpEnable = true; color.logicOp = (VkLogicOp)(state.logic - 1); info.pColorBlendState = &color;
    }
    if (state.discard) info.stageCount = 1;
    if (state.images) {
        state.image_layout = e2d_images_layout(state.device, state.counts, state.images);
        if (!state.image_layout) return VK_ERROR_OUT_OF_HOST_MEMORY;
        info.layout = state.image_layout->pipelineLayout;
    }
    return state.create(device, cache, 1, &info, allocator, pipelines);
}

E2D_EXPORT int e2d_render_raster_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (!e2d_render_pinned_core() ||
        SDL_strcmp(SDL_GetGPUDeviceDriver(device), "vulkan") != 0) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr device_proc = proc ? (PFN_vkGetDeviceProcAddr)proc(renderer->instance, "vkGetDeviceProcAddr") : NULL;
    return device_proc && device_proc(renderer->logicalDevice, "vkCreateGraphicsPipelines") ? 1 : 0;
}

_Static_assert(offsetof(VkPhysicalDeviceFeatures, sampleRateShading) == 6 * sizeof(VkBool32), "sample shading feature word");
_Static_assert(offsetof(VkPhysicalDeviceFeatures, alphaToOne) == 17 * sizeof(VkBool32), "alpha-to-one feature word");
E2D_EXPORT int e2d_render_multisample_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (e2d_render_raster_support(device, 1) != 1) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures query = proc ? (PFN_vkGetPhysicalDeviceFeatures)proc(renderer->instance, "vkGetPhysicalDeviceFeatures") : NULL;
    if (!query) return 0;
    VkPhysicalDeviceFeatures features; query(renderer->physicalDevice, &features);
    return (features.sampleRateShading ? 1 : 0) | (features.alphaToOne ? 2 : 0);
}

_Static_assert(offsetof(VkPhysicalDeviceFeatures, depthBounds) == 14 * sizeof(VkBool32), "depth bounds feature word");
E2D_EXPORT int e2d_render_depth_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (e2d_render_raster_support(device, 1) != 1) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures query = proc ? (PFN_vkGetPhysicalDeviceFeatures)proc(renderer->instance, "vkGetPhysicalDeviceFeatures") : NULL;
    if (!query) return 1;
    VkPhysicalDeviceFeatures features; query(renderer->physicalDevice, &features);
    return 1 | (features.depthBounds ? 2 : 0);
}

typedef struct LineLimits { float minimum, maximum, granularity; } LineLimits;
_Static_assert(offsetof(VkPhysicalDeviceFeatures, wideLines) == 15 * sizeof(VkBool32), "wide line feature word");

E2D_EXPORT int e2d_render_line_support(SDL_GPUDevice *device, uint32_t abi, LineLimits *limits)
{
    if (!device || abi != 1 || !limits) return -1;
    if (e2d_render_raster_support(device, 1) != 1) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures query = proc ? (PFN_vkGetPhysicalDeviceFeatures)proc(renderer->instance, "vkGetPhysicalDeviceFeatures") : NULL;
    if (!query) return 0;
    VkPhysicalDeviceFeatures features; query(renderer->physicalDevice, &features);
    if (!features.wideLines) return 0;
    const VkPhysicalDeviceLimits *p = &renderer->physicalDeviceProperties.properties.limits;
    *limits = (LineLimits){p->lineWidthRange[0], p->lineWidthRange[1], p->lineWidthGranularity};
    return 1;
}

static SDL_GPUGraphicsPipeline *create_raster_pipeline(SDL_GPUDevice *device, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, bool discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images, uint32_t logic)
{
    if (!device || !info || !info->vertex_shader || !info->fragment_shader || e2d_render_raster_support(device, 1) != 1) return NULL;
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData;
    if (info->rasterizer_state.fill_mode == SDL_GPU_FILLMODE_LINE && !renderer->supportsFillModeNonSolid) { SDL_SetError("Native polygon outlines are unavailable"); return NULL; }
    RasterCache *cache = raster_cache(device); if (!cache) return NULL;
    const VulkanShader *vertex = (VulkanShader *)info->vertex_shader, *fragment = (VulkanShader *)info->fragment_shader;
    uint32_t counts[8] = {vertex->numSamplers, vertex->numStorageTextures, vertex->numStorageBuffers, vertex->numUniformBuffers,
        fragment->numSamplers, fragment->numStorageTextures, fragment->numStorageBuffers, fragment->numUniformBuffers};
    SDL_LockMutex(cache->lock);
    RasterLayout *layout = cache->layouts;
    while (layout && SDL_memcmp(layout->counts, counts, sizeof(counts)) != 0) layout = layout->next;
    if (!layout) {
        layout = SDL_calloc(1, sizeof(*layout));
        if (!layout) { SDL_UnlockMutex(cache->lock); return NULL; }
        SDL_memcpy(layout->counts, counts, sizeof(counts));
        layout->next_id = SDL_AddAtomicInt(&renderer->layoutResourceID, 4);
        layout->next = cache->layouts; cache->layouts = layout;
    }
    // Only immutable device fields and shared, locked layout caches enter this call-local renderer view.
    VulkanRenderer local = {0};
    local.logicalDevice = renderer->logicalDevice;
    local.debugMode = renderer->debugMode; local.supportsDebugUtils = renderer->supportsDebugUtils;
    local.supportsFillModeNonSolid = renderer->supportsFillModeNonSolid; local.fillModeOnlyWarning = 1;
    local.graphicsPipelineLayoutFetchLock = renderer->graphicsPipelineLayoutFetchLock;
    local.graphicsPipelineResourceLayoutHashTable = renderer->graphicsPipelineResourceLayoutHashTable;
    local.descriptorSetLayoutFetchLock = renderer->descriptorSetLayoutFetchLock;
    local.descriptorSetLayoutHashTable = renderer->descriptorSetLayoutHashTable;
    // Reserve once per descriptor-count profile: gaps must not grow per pipeline in SDL's indexed pools.
    SDL_SetAtomicInt(&local.layoutResourceID, layout->next_id);
    local.vkCreateDescriptorSetLayout = renderer->vkCreateDescriptorSetLayout;
    local.vkCreatePipelineLayout = renderer->vkCreatePipelineLayout; local.vkDestroyPipelineLayout = renderer->vkDestroyPipelineLayout;
    local.vkCreateRenderPass = renderer->vkCreateRenderPass; local.vkDestroyRenderPass = renderer->vkDestroyRenderPass;
    local.vkSetDebugUtilsObjectNameEXT = renderer->vkSetDebugUtilsObjectNameEXT;
    local.vkCreateGraphicsPipelines = raster_pipeline;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr device_proc = (PFN_vkGetDeviceProcAddr)proc(renderer->instance, "vkGetDeviceProcAddr");
    RasterState previous = state;
    state = (RasterState){.create = (PFN_vkCreateGraphicsPipelines)device_proc(renderer->logicalDevice, "vkCreateGraphicsPipelines"), .width = width, .discard = discard, .samples = samples ? *samples : (MultisampleState){0}, .depth = depth ? *depth : (DepthStencilState){0}, .device = device, .counts = counts, .images = images, .logic = logic};
    SDL_GPUGraphicsPipeline *pipeline = device->CreateGraphicsPipeline((SDL_GPURenderer *)&local, info);
    if (pipeline && images) ((VulkanGraphicsPipeline *)pipeline)->resourceLayout = state.image_layout;
    state = previous;
    layout->next_id = SDL_max(layout->next_id, SDL_GetAtomicInt(&local.layoutResourceID));
    SDL_UnlockMutex(cache->lock);
    if (!pipeline) {
        // Pinned SDL retains both shader references before a failed native pipeline creation.
        (void)SDL_AtomicDecRef(&((VulkanShader *)info->vertex_shader)->referenceCount);
        (void)SDL_AtomicDecRef(&((VulkanShader *)info->fragment_shader)->referenceCount);
    }
    return pipeline;
}

E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_raster_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info)
{
    return abi == 1 ? create_raster_pipeline(device, info, 1, true, NULL, NULL, 0, 0) : NULL;
}

static SDL_GPUGraphicsPipeline *create_line_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images, uint32_t logic)
{
    if (!device || abi != 1 || !info || discard > 1 || !isfinite(width) || width <= 0) return NULL;
    if (width != 1) {
        LineLimits limits;
        if (e2d_render_line_support(device, 1, &limits) != 1 ||
            !SDL_GetBooleanProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.wide_lines", false) ||
            !isfinite(limits.minimum) || !isfinite(limits.maximum) || limits.minimum <= 0 || limits.maximum < limits.minimum) return NULL;
        width = SDL_clamp(width, limits.minimum, limits.maximum);
    }
    return create_raster_pipeline(device, info, width, discard != 0, samples, depth, images, logic);
}

E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_line_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard)
{
    return create_line_pipeline(device, abi, info, width, discard, NULL, NULL, 0, 0);
}

static bool valid_multisample(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, const MultisampleState *samples)
{
    if (!device || abi != 1 || !info || !samples || (samples->flags & ~7u) || !isfinite(samples->minimum) || samples->minimum < 0 || samples->minimum > 1) return false;
    if (e2d_render_raster_support(device, 1) != 1 || info->multisample_state.sample_count > SDL_GPU_SAMPLECOUNT_8) return false;
    uint32_t required = (samples->flags >> 1) & 3;
    uint32_t enabled = (uint32_t)SDL_GetNumberProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.multisample_features", 0);
    return (required & ~enabled) == 0;
}

E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_multisample_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples)
{
    return valid_multisample(device, abi, info, samples) ? create_line_pipeline(device, abi, info, width, discard, samples, NULL, 0, 0) : NULL;
}

_Static_assert(offsetof(VkPhysicalDeviceFeatures, logicOp) == 8 * sizeof(VkBool32), "logic operation feature word");
E2D_EXPORT int e2d_render_logic_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || abi != 1) return -1;
    if (e2d_render_raster_support(device, 1) != 1) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures query = proc ? (PFN_vkGetPhysicalDeviceFeatures)proc(renderer->instance, "vkGetPhysicalDeviceFeatures") : NULL;
    if (!query) return 0;
    VkPhysicalDeviceFeatures features; query(renderer->physicalDevice, &features); return features.logicOp ? 1 : 0;
}
static SDL_GPUGraphicsPipeline *create_state_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images, uint32_t logic)
{
    uint32_t required = ((images & 255) ? 1 : 0) | ((images & 65280) ? 2 : 0);
    if (abi == 2) {
        if ((images >> 18) || ((images >> 16) & ~required)) return NULL;
        required = images >> 16; images &= 65535; abi = 1;
    }
    if (!depth || (depth->flags & ~3u) || !isfinite(depth->minimum) || !isfinite(depth->maximum) || depth->minimum < 0 || depth->maximum > 1 || depth->minimum > 1 || depth->maximum < 0 ||
        (depth->front_mask | depth->front_write | depth->front_reference | depth->back_mask | depth->back_write | depth->back_reference) > 255 || images > 65535 || logic > 16) return NULL;
    if (!valid_multisample(device, abi, info, samples)) return NULL;
    if (depth->flags && !info->target_info.has_depth_stencil_target) return NULL;
    if ((depth->flags & 1) && (depth->minimum > depth->maximum || !SDL_GetBooleanProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.depth_bounds", false))) return NULL;
    if ((depth->flags & 2) && !info->depth_stencil_state.enable_stencil_test) return NULL;
    if (images) {
        if (e2d_render_image_support(device, 1) != 1) return NULL;
        if (required & ~(uint32_t)SDL_GetNumberProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.graphics_storage", 0)) { SDL_SetError("Graphics image stores require an enabled feature for each writing stage"); return NULL; }
    }
    if (logic && (e2d_render_logic_support(device, 1) != 1 || !SDL_GetBooleanProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.logic_op", false))) return NULL;
    return create_line_pipeline(device, abi, info, width, discard, samples, depth, images, logic);
}
E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_depth_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth)
{
    return create_state_pipeline(device, abi, info, width, discard, samples, depth, 0, 0);
}
E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_images_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images)
{
    return images ? create_state_pipeline(device, abi, info, width, discard, samples, depth, images, 0) : NULL;
}
E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_logic_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images, uint32_t logic)
{
    return logic ? create_state_pipeline(device, abi, info, width, discard, samples, depth, images, logic) : NULL;
}
