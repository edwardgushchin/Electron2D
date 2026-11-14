#include "pinned-core.h"
#include <stdint.h>
#include <math.h>
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
enum { STENCIL_COMPARE_MASK = 16, STENCIL_WRITE_MASK = 32, STENCIL_REFERENCE = 64, DYNAMIC_STENCIL = 112 };
typedef struct StencilCommands { PFN_vkCmdSetStencilCompareMask compare_mask; PFN_vkCmdSetStencilWriteMask write_mask; } StencilCommands;
typedef struct BlendState { uint32_t count, factors[8][4]; } BlendState;
extern VulkanGraphicsPipelineResourceLayout *e2d_images_layout(SDL_GPUDevice *, const uint32_t *, uint32_t);
extern int e2d_render_image_support(SDL_GPUDevice *, uint32_t);
typedef struct RasterState { PFN_vkCreateGraphicsPipelines create; PFN_vkCreateRenderPass create_pass; float width; bool discard, pass_failed; MultisampleState samples; DepthStencilState depth; SDL_GPUDevice *device; const uint32_t *counts; uint32_t images, logic; const BlendState *blend; VulkanGraphicsPipelineResourceLayout *image_layout; } RasterState;
static E2D_THREAD_LOCAL RasterState state;
static VkResult VKAPI_CALL stencil_pipeline_pass(VkDevice device, const VkRenderPassCreateInfo *info, const VkAllocationCallbacks *allocator, VkRenderPass *pass)
{
    VkResult result = e2d_stencil_render_pass(state.create_pass, device, info, allocator, pass);
    state.pass_failed = result != VK_SUCCESS; return result;
}

static VkResult VKAPI_CALL raster_pipeline(VkDevice device, VkPipelineCache cache, uint32_t count,
    const VkGraphicsPipelineCreateInfo *infos, const VkAllocationCallbacks *allocator, VkPipeline *pipelines)
{
    if (state.pass_failed) return VK_ERROR_INITIALIZATION_FAILED;
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
        }
        if (info.pDynamicState && (state.depth.flags & (2 | DYNAMIC_STENCIL))) {
            dynamic = *info.pDynamicState;
            if (dynamic.dynamicStateCount > SDL_arraysize(dynamic_states) - 2) return VK_ERROR_INITIALIZATION_FAILED;
            // Keep independent static references unless the draw list explicitly owns them.
            bool static_reference = (state.depth.flags & (2 | STENCIL_REFERENCE)) == 2;
            uint32_t n = 0;
            for (uint32_t i = 0; i < dynamic.dynamicStateCount; i++)
                if (!static_reference || dynamic.pDynamicStates[i] != VK_DYNAMIC_STATE_STENCIL_REFERENCE) dynamic_states[n++] = dynamic.pDynamicStates[i];
            if (state.depth.flags & STENCIL_COMPARE_MASK) dynamic_states[n++] = VK_DYNAMIC_STATE_STENCIL_COMPARE_MASK;
            if (state.depth.flags & STENCIL_WRITE_MASK) dynamic_states[n++] = VK_DYNAMIC_STATE_STENCIL_WRITE_MASK;
            dynamic.dynamicStateCount = n; dynamic.pDynamicStates = dynamic_states; info.pDynamicState = &dynamic;
        }
        info.pDepthStencilState = &depth;
    }
    VkPipelineColorBlendStateCreateInfo color = {.sType = VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO};
    VkPipelineColorBlendAttachmentState attachments[8];
    if (state.logic || state.blend) {
        if (info.pColorBlendState) color = *info.pColorBlendState;
        if (state.logic) { color.logicOpEnable = true; color.logicOp = (VkLogicOp)(state.logic - 1); }
        if (state.blend) {
            if (color.attachmentCount != state.blend->count || color.attachmentCount > SDL_arraysize(attachments) || (color.attachmentCount && !color.pAttachments)) return VK_ERROR_INITIALIZATION_FAILED;
            for (uint32_t i = 0; i < color.attachmentCount; i++) {
                attachments[i] = color.pAttachments[i];
                attachments[i].srcColorBlendFactor = (VkBlendFactor)state.blend->factors[i][0];
                attachments[i].dstColorBlendFactor = (VkBlendFactor)state.blend->factors[i][1];
                attachments[i].srcAlphaBlendFactor = (VkBlendFactor)state.blend->factors[i][2];
                attachments[i].dstAlphaBlendFactor = (VkBlendFactor)state.blend->factors[i][3];
            }
            color.pAttachments = attachments;
        }
        info.pColorBlendState = &color;
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
_Static_assert(offsetof(VkPhysicalDeviceFeatures, shaderStorageImageMultisample) == 30 * sizeof(VkBool32), "multisample storage feature word");
E2D_EXPORT int e2d_render_multisample_support(SDL_GPUDevice *device, uint32_t abi)
{
    if (!device || (abi != 1 && abi != 2)) return -1;
    if (e2d_render_raster_support(device, 1) != 1) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures query = proc ? (PFN_vkGetPhysicalDeviceFeatures)proc(renderer->instance, "vkGetPhysicalDeviceFeatures") : NULL;
    if (!query) return 0;
    VkPhysicalDeviceFeatures features; query(renderer->physicalDevice, &features);
    return (features.sampleRateShading ? 1 : 0) | (features.alphaToOne ? 2 : 0) | (abi == 2 && features.shaderStorageImageMultisample ? 4 : 0);
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

static SDL_GPUGraphicsPipeline *create_raster_pipeline(SDL_GPUDevice *device, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, bool discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images, uint32_t logic, const BlendState *blend)
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
    local.vkCreateRenderPass = depth && (depth->flags & 4) ? stencil_pipeline_pass : renderer->vkCreateRenderPass; local.vkDestroyRenderPass = renderer->vkDestroyRenderPass;
    local.vkSetDebugUtilsObjectNameEXT = renderer->vkSetDebugUtilsObjectNameEXT;
    local.vkCreateGraphicsPipelines = raster_pipeline;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetDeviceProcAddr device_proc = (PFN_vkGetDeviceProcAddr)proc(renderer->instance, "vkGetDeviceProcAddr");
    RasterState previous = state;
    state = (RasterState){.create = (PFN_vkCreateGraphicsPipelines)device_proc(renderer->logicalDevice, "vkCreateGraphicsPipelines"), .create_pass = renderer->vkCreateRenderPass, .width = width, .discard = discard, .samples = samples ? *samples : (MultisampleState){0}, .depth = depth ? *depth : (DepthStencilState){0}, .device = device, .counts = counts, .images = images, .logic = logic, .blend = blend};
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
    return abi == 1 ? create_raster_pipeline(device, info, 1, true, NULL, NULL, 0, 0, NULL) : NULL;
}

static SDL_GPUGraphicsPipeline *create_line_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images, uint32_t logic, const BlendState *blend)
{
    if (!device || abi != 1 || !info || discard > 1 || !isfinite(width) || width <= 0) return NULL;
    if (width != 1) {
        LineLimits limits;
        if (e2d_render_line_support(device, 1, &limits) != 1 ||
            !SDL_GetBooleanProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.wide_lines", false) ||
            !isfinite(limits.minimum) || !isfinite(limits.maximum) || limits.minimum <= 0 || limits.maximum < limits.minimum) return NULL;
        width = SDL_clamp(width, limits.minimum, limits.maximum);
    }
    return create_raster_pipeline(device, info, width, discard != 0, samples, depth, images, logic, blend);
}

E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_line_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard)
{
    return create_line_pipeline(device, abi, info, width, discard, NULL, NULL, 0, 0, NULL);
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
    return valid_multisample(device, abi, info, samples) ? create_line_pipeline(device, abi, info, width, discard, samples, NULL, 0, 0, NULL) : NULL;
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
static SDL_GPUGraphicsPipeline *create_state_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images, uint32_t logic, const BlendState *blend)
{
    uint32_t required = ((images & 255) ? 1 : 0) | ((images & 65280) ? 2 : 0);
    bool stencil = abi == 3;
    if (stencil && (!depth || !(depth->flags & 4) || !info || info->target_info.depth_stencil_format != SDL_GPU_TEXTUREFORMAT_R8_UINT || e2d_render_stencil_support(device, 1, SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET, info->multisample_state.sample_count) != 1)) return NULL;
    if (abi == 2 || stencil) {
        if ((images >> 18) || ((images >> 16) & ~required)) return NULL;
        required = images >> 16; images &= 65535; abi = 1;
    }
    if (!depth || (depth->flags & ~((stencil ? 7u : 3u) | DYNAMIC_STENCIL)) || !isfinite(depth->minimum) || !isfinite(depth->maximum) || depth->minimum < 0 || depth->maximum > 1 || depth->minimum > 1 || depth->maximum < 0 ||
        (depth->front_mask | depth->front_write | depth->front_reference | depth->back_mask | depth->back_write | depth->back_reference) > 255 || images > 65535 || logic > 16) return NULL;
    if (!valid_multisample(device, abi, info, samples)) return NULL;
    if (depth->flags && !info->target_info.has_depth_stencil_target) return NULL;
    if ((depth->flags & 1) && (depth->minimum > depth->maximum || !SDL_GetBooleanProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.depth_bounds", false))) return NULL;
    if ((depth->flags & (2 | DYNAMIC_STENCIL)) && !info->depth_stencil_state.enable_stencil_test) return NULL;
    if (images) {
        if (e2d_render_image_support(device, 1) != 1) return NULL;
        if (required & ~(uint32_t)SDL_GetNumberProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.graphics_storage", 0)) { SDL_SetError("Graphics image stores require an enabled feature for each writing stage"); return NULL; }
    }
    if (logic && (e2d_render_logic_support(device, 1) != 1 || !SDL_GetBooleanProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.logic_op", false))) return NULL;
    return create_line_pipeline(device, abi, info, width, discard, samples, depth, images, logic, blend);
}
E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_depth_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth)
{
    return create_state_pipeline(device, abi, info, width, discard, samples, depth, 0, 0, NULL);
}

static void set_stencil(PFN_vkCmdSetStencilReference set, VkCommandBuffer command, uint32_t front, uint32_t back)
{
    if (front == back) set(command, VK_STENCIL_FACE_FRONT_AND_BACK, front);
    else {
        set(command, VK_STENCIL_FACE_FRONT_BIT, front);
        set(command, VK_STENCIL_FACE_BACK_BIT, back);
    }
}

E2D_EXPORT int e2d_render_dynamic_stencil(SDL_GPUDevice *device, uint32_t abi, SDL_GPUCommandBuffer *commands, const DepthStencilState *depth, StencilCommands *functions)
{
    if (abi != 1 || !device || !depth || !functions || !(depth->flags & DYNAMIC_STENCIL)) return -1;
    if (!commands) {
        if (e2d_render_raster_support(device, 1) != 1) return 0;
        const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
        PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
        PFN_vkGetDeviceProcAddr get = proc ? (PFN_vkGetDeviceProcAddr)proc(renderer->instance, "vkGetDeviceProcAddr") : NULL;
        if (!get || !renderer->vkCmdSetStencilReference) return 0;
        functions->compare_mask = (PFN_vkCmdSetStencilCompareMask)get(renderer->logicalDevice, "vkCmdSetStencilCompareMask");
        functions->write_mask = (PFN_vkCmdSetStencilWriteMask)get(renderer->logicalDevice, "vkCmdSetStencilWriteMask");
        return functions->compare_mask && functions->write_mask ? 1 : 0;
    }
    VulkanCommandBuffer *command = (VulkanCommandBuffer *)commands;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    if (!functions->compare_mask || !functions->write_mask || command->common.device != device || command->renderer != renderer ||
        (device->debug_mode && (command->common.submitted || !command->common.render_pass.in_progress))) return -1;
    if (depth->flags & STENCIL_COMPARE_MASK) set_stencil(functions->compare_mask, command->commandBuffer, depth->front_mask, depth->back_mask);
    if (depth->flags & STENCIL_WRITE_MASK) set_stencil(functions->write_mask, command->commandBuffer, depth->front_write, depth->back_write);
    if (depth->flags & STENCIL_REFERENCE) set_stencil(renderer->vkCmdSetStencilReference, command->commandBuffer, depth->front_reference, depth->back_reference);
    return 1;
}
E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_images_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images)
{
    return images ? create_state_pipeline(device, abi, info, width, discard, samples, depth, images, 0, NULL) : NULL;
}
E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_logic_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images, uint32_t logic)
{
    return logic ? create_state_pipeline(device, abi, info, width, discard, samples, depth, images, logic, NULL) : NULL;
}

_Static_assert(offsetof(VkPhysicalDeviceFeatures, dualSrcBlend) == 7 * sizeof(VkBool32), "dual source feature word");
E2D_EXPORT int e2d_render_blend_support(SDL_GPUDevice *device, uint32_t abi, uint32_t *maximum)
{
    if (!device || abi != 1 || !maximum) return -1;
    *maximum = 0;
    if (e2d_render_raster_support(device, 1) != 1) return 0;
    const VulkanRenderer *renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFeatures query = proc ? (PFN_vkGetPhysicalDeviceFeatures)proc(renderer->instance, "vkGetPhysicalDeviceFeatures") : NULL;
    PFN_vkGetPhysicalDeviceProperties properties = proc ? (PFN_vkGetPhysicalDeviceProperties)proc(renderer->instance, "vkGetPhysicalDeviceProperties") : NULL;
    if (!query || !properties) return 0;
    VkPhysicalDeviceFeatures features; query(renderer->physicalDevice, &features);
    VkPhysicalDeviceProperties limits; properties(renderer->physicalDevice, &limits);
    *maximum = features.dualSrcBlend ? limits.limits.maxFragmentDualSrcAttachments : 0;
    return 1 | (*maximum ? 2 : 0);
}
E2D_EXPORT SDL_GPUGraphicsPipeline *e2d_render_blend_pipeline(SDL_GPUDevice *device, uint32_t abi, const SDL_GPUGraphicsPipelineCreateInfo *info, float width, uint32_t discard, const MultisampleState *samples, const DepthStencilState *depth, uint32_t images, uint32_t logic, const BlendState *blend)
{
    uint32_t maximum;
    if (!device || (abi != 1 && abi != 2) || !blend || !info || blend->count == 0 || blend->count > 8 || blend->count != info->target_info.num_color_targets || e2d_render_blend_support(device, 1, &maximum) < 1) return NULL;
    for (uint32_t i = 0; i < blend->count; i++) for (uint32_t j = 0; j < 4; j++) {
        if (blend->factors[i][j] > VK_BLEND_FACTOR_ONE_MINUS_SRC1_ALPHA) return NULL;
        if (blend->factors[i][j] >= VK_BLEND_FACTOR_SRC1_COLOR && (blend->count > maximum || !SDL_GetBooleanProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.dual_source_blend", false))) return NULL;
    }
    return create_state_pipeline(device, abi, info, width, discard, samples, depth, images, logic, blend);
}
