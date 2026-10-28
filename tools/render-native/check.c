#include <SDL3/SDL.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

typedef struct { uint64_t textures, buffers, total; } Memory;
extern int e2d_render_graphics_storage_support(SDL_GPUDevice *, uint32_t);
extern int e2d_render_storage_barrier(SDL_GPUDevice *, uint32_t, SDL_GPUCommandBuffer *);
extern int e2d_render_shader16_support(SDL_GPUDevice *, uint32_t);
extern int e2d_render_filter_support(SDL_GPUDevice *, uint32_t, uint8_t *, uint32_t);
extern int e2d_render_texture_format(SDL_GPUDevice *, uint32_t, SDL_GPUTexture *);
extern int e2d_render_limits(SDL_GPUDevice *, uint32_t, uint64_t *, uint32_t);
extern int e2d_render_memory(SDL_GPUDevice *, uint32_t, Memory *);
extern int e2d_render_sampler_view(SDL_GPUDevice *, uint32_t, SDL_GPUTexture *);
extern SDL_GPUSampler *e2d_render_sampler(SDL_GPUDevice *, uint32_t, const SDL_GPUSamplerCreateInfo *, uint32_t, uint32_t, uint32_t, uint32_t);

extern int e2d_render_empty_support(SDL_GPUDevice *, uint32_t);
extern void *e2d_render_empty_create(SDL_GPUDevice *, uint32_t, uint32_t, uint32_t, uint32_t);
extern SDL_GPURenderPass *e2d_render_empty_begin(SDL_GPUDevice *, uint32_t, SDL_GPUCommandBuffer *, void *);
extern void e2d_render_empty_release(SDL_GPUDevice *, uint32_t, void *);

extern SDL_GPUGraphicsPipeline *e2d_render_line_pipeline(SDL_GPUDevice *, uint32_t, const SDL_GPUGraphicsPipelineCreateInfo *, float, uint32_t);
typedef struct MultisampleState { uint32_t mask, flags; float minimum; } MultisampleState;
extern int e2d_render_multisample_support(SDL_GPUDevice *, uint32_t);
extern SDL_GPUGraphicsPipeline *e2d_render_multisample_pipeline(SDL_GPUDevice *, uint32_t, const SDL_GPUGraphicsPipelineCreateInfo *, float, uint32_t, const MultisampleState *);
typedef struct DepthStencilState { uint32_t flags, front_mask, front_write, front_reference, back_mask, back_write, back_reference; float minimum, maximum; } DepthStencilState;
extern int e2d_render_depth_support(SDL_GPUDevice *, uint32_t);
extern SDL_GPUGraphicsPipeline *e2d_render_depth_pipeline(SDL_GPUDevice *, uint32_t, const SDL_GPUGraphicsPipelineCreateInfo *, float, uint32_t, const MultisampleState *, const DepthStencilState *);
extern int e2d_render_image_support(SDL_GPUDevice *, uint32_t);
extern void e2d_render_image_release(SDL_GPUDevice *, uint32_t);
extern int e2d_render_image_transition(SDL_GPUDevice *, uint32_t, SDL_GPUCommandBuffer *, SDL_GPUTexture *const *, uint32_t, uint32_t);
extern int e2d_render_bind_image(SDL_GPUDevice *, uint32_t, SDL_GPUCommandBuffer *, SDL_GPUTexture *, uint32_t, uint32_t, uint32_t);
extern int e2d_render_prepare_images(SDL_GPUDevice *, uint32_t, SDL_GPUCommandBuffer *, uint32_t, SDL_GPUTexture *const *, uint32_t);
extern SDL_GPUGraphicsPipeline *e2d_render_images_pipeline(SDL_GPUDevice *, uint32_t, const SDL_GPUGraphicsPipelineCreateInfo *, float, uint32_t, const MultisampleState *, const DepthStencilState *, uint32_t);
extern int e2d_render_raster_support(SDL_GPUDevice *, uint32_t);
extern int e2d_render_logic_support(SDL_GPUDevice *, uint32_t);
extern SDL_GPUGraphicsPipeline *e2d_render_logic_pipeline(SDL_GPUDevice *, uint32_t, const SDL_GPUGraphicsPipelineCreateInfo *, float, uint32_t, const MultisampleState *, const DepthStencilState *, uint32_t, uint32_t);
extern SDL_GPUGraphicsPipeline *e2d_render_raster_pipeline(SDL_GPUDevice *, uint32_t, const SDL_GPUGraphicsPipelineCreateInfo *);

static void require_at(bool valid, int line)
{
    if (!valid) { fprintf(stderr, "Native render check failed at line %d: %s\n", line, SDL_GetError()); exit(1); }
}
#define require(valid) require_at((valid), __LINE__)
static Memory read_memory(SDL_GPUDevice *device)
{
    Memory memory = {0};
    require(e2d_render_memory(device, 1, &memory) == 1);
    require(memory.total >= memory.textures && memory.total - memory.textures >= memory.buffers);
    return memory;
}
static SDL_AtomicInt stop, failed;
static int reader(void *device)
{
    while (!SDL_GetAtomicInt(&stop)) {
        Memory memory = {0};
        if (e2d_render_memory(device, 1, &memory) != 1 || memory.total < memory.textures || memory.total - memory.textures < memory.buffers)
            SDL_SetAtomicInt(&failed, 1);
    }
    return 0;
}
typedef struct RasterChecks { SDL_GPUDevice *device; SDL_GPUGraphicsPipelineCreateInfo info; } RasterChecks;
static int raster_thread(void *data)
{
    RasterChecks *checks = data;
    for (int i = 0; i < 32; i++) {
        SDL_GPUGraphicsPipeline *pipeline = e2d_render_raster_pipeline(checks->device, 1, &checks->info);
        require(pipeline != NULL); SDL_ReleaseGPUGraphicsPipeline(checks->device, pipeline);
    }
    return 0;
}
static void image_check(SDL_GPUDevice *device, SDL_GPUShaderCreateInfo shader_info, const uint32_t *fragment, uint32_t fragment_size)
{
    require(e2d_render_image_support(device, 1) == 1);
    shader_info.num_samplers = 1; shader_info.num_storage_textures = 2; shader_info.num_storage_buffers = 1; shader_info.num_uniform_buffers = 1;
    SDL_GPUShader *vs = SDL_CreateGPUShader(device, &shader_info); require(vs != NULL);
    shader_info.code = (const uint8_t *)fragment; shader_info.code_size = fragment_size; shader_info.stage = SDL_GPU_SHADERSTAGE_FRAGMENT;
    SDL_GPUShader *fs = SDL_CreateGPUShader(device, &shader_info); require(fs != NULL);
    SDL_GPUGraphicsPipelineCreateInfo info = {.vertex_shader = vs, .fragment_shader = fs, .primitive_type = SDL_GPU_PRIMITIVETYPE_TRIANGLELIST, .rasterizer_state = {.enable_depth_clip = true}};
    MultisampleState samples = {0}; DepthStencilState depth = {0};
    require(e2d_render_images_pipeline(device, 1, &info, 1, 1, &samples, &depth, 257) == NULL);
    // These empty shaders exercise descriptor ownership without requiring graphics store instructions.
    require(SDL_SetNumberProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.gpu.graphics_storage", 3));
    SDL_GPUGraphicsPipeline *pipeline = e2d_render_images_pipeline(device, 1, &info, 1, 1, &samples, &depth, 257); require(pipeline != NULL);
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData; int ids = SDL_GetAtomicInt(&renderer->layoutResourceID);
    for (int i = 0; i < 32; i++) {
        SDL_GPUGraphicsPipeline *copy = e2d_render_images_pipeline(device, 1, &info, 1, 1, &samples, &depth, 257); require(copy != NULL);
        require(((VulkanGraphicsPipeline *)pipeline)->resourceLayout == ((VulkanGraphicsPipeline *)copy)->resourceLayout); SDL_ReleaseGPUGraphicsPipeline(device, copy);
    }
    require(SDL_GetAtomicInt(&renderer->layoutResourceID) == ids);
    SDL_GPUTextureCreateInfo texture_info = {.type = SDL_GPU_TEXTURETYPE_2D, .format = SDL_GPU_TEXTUREFORMAT_R32_UINT, .usage = SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE, .width = 2, .height = 2, .layer_count_or_depth = 1, .num_levels = 1};
    SDL_GPUTexture *texture = SDL_CreateGPUTexture(device, &texture_info); require(texture != NULL);
    SDL_GPUSamplerCreateInfo sampler_info = {.max_lod = 1}; SDL_GPUSampler *sampler = SDL_CreateGPUSampler(device, &sampler_info); require(sampler != NULL);
    SDL_GPUBufferCreateInfo buffer_info = {.size = 16, .usage = SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ}; SDL_GPUBuffer *buffer = SDL_CreateGPUBuffer(device, &buffer_info); require(buffer != NULL);
    void *empty = e2d_render_empty_create(device, 1, 2, 2, 0); require(empty != NULL);
    for (int cycle = 0; cycle < 3; cycle++) {
        SDL_GPUCommandBuffer *commands = SDL_AcquireGPUCommandBuffer(device); require(commands != NULL);
        require(e2d_render_image_transition(device, 1, commands, &texture, 1, 1) == 1);
        require(e2d_render_prepare_images(device, 1, commands, 257, &texture, 1) == -1);
        SDL_GPURenderPass *pass = e2d_render_empty_begin(device, 1, commands, empty); require(pass != NULL);
        require(e2d_render_image_transition(device, 1, commands, &texture, 1, 0) == -1);
        SDL_BindGPUGraphicsPipeline(pass, pipeline);
        SDL_GPUTextureSamplerBinding sampled = {.texture = texture, .sampler = sampler};
        SDL_BindGPUVertexSamplers(pass, 0, &sampled, 1); SDL_BindGPUFragmentSamplers(pass, 0, &sampled, 1);
        SDL_BindGPUVertexStorageBuffers(pass, 0, &buffer, 1); SDL_BindGPUFragmentStorageBuffers(pass, 0, &buffer, 1);
        for (uint32_t stage = 0; stage < 2; stage++) for (uint32_t slot = 0; slot < 2; slot++) require(e2d_render_bind_image(device, 1, commands, texture, slot, stage, slot == 0) == 1);
        require(e2d_render_prepare_images(device, 1, commands, 258, &texture, 1) == -1);
        for (int i = 0; i < 260; i++) require(e2d_render_prepare_images(device, 1, commands, 257, &texture, 1) == 1);
        VulkanCommandBuffer *native = (VulkanCommandBuffer *)commands;
        for (int i = 0; i < 2; i++) { DescriptorSetPool *pool = &native->descriptorSetCache->pools[((VulkanGraphicsPipeline *)pipeline)->resourceLayout->descriptorSetLayouts[i * 2]->ID]; require(pool->poolCount == 3 && pool->descriptorSetCount == 384 && pool->descriptorSetIndex == 260); }
        require(native->usedTextureCount == 1);
        SDL_EndGPURenderPass(pass); require(e2d_render_image_transition(device, 1, commands, &texture, 1, 0) == 1);
        require(SDL_SubmitGPUCommandBuffer(commands)); require(SDL_WaitForGPUIdle(device));
    }
    require(SDL_GetAtomicInt(&renderer->layoutResourceID) == ids);
    SDL_ReleaseGPUGraphicsPipeline(device, pipeline); SDL_ReleaseGPUShader(device, vs); SDL_ReleaseGPUShader(device, fs); SDL_ReleaseGPUTexture(device, texture); SDL_ReleaseGPUSampler(device, sampler); SDL_ReleaseGPUBuffer(device, buffer); e2d_render_empty_release(device, 1, empty);
    require(SDL_WaitForGPUIdle(device)); e2d_render_image_release(device, 1); e2d_render_image_release(device, 1);
    require(SDL_GetPointerProperty(SDL_GetGPUDeviceProperties(device), "Electron2D.render.image_layouts", NULL) == NULL);
    puts("Native mixed image descriptors: bounded layout IDs, 260-set pool rollover/reuse, original sampled alias, texture tracking, ABI/pass/profile guards and idle cache retirement passed");
}
static void raster_check(SDL_GPUDevice *device)
{
    uint32_t vertex[] = {0x07230203, 0x10000, 0, 5, 0, 0x20011, 1, 0x3000e, 0, 1, 0x5000f, 0, 1, 0x6e69616d, 0, 0x20013, 2, 0x30021, 3, 2, 0x50036, 2, 1, 0, 3, 0x200f8, 4, 0x100fd, 0x10038};
    uint32_t fragment[] = {0x07230203, 0x10000, 0, 5, 0, 0x20011, 1, 0x3000e, 0, 1, 0x5000f, 4, 1, 0x6e69616d, 0, 0x30010, 1, 7, 0x20013, 2, 0x30021, 3, 2, 0x50036, 2, 1, 0, 3, 0x200f8, 4, 0x100fd, 0x10038};
    SDL_GPUShaderCreateInfo shader_info = {.code_size = sizeof(vertex), .code = (uint8_t *)vertex, .entrypoint = "main", .format = SDL_GPU_SHADERFORMAT_SPIRV, .stage = SDL_GPU_SHADERSTAGE_VERTEX};
    SDL_GPUShader *vs = SDL_CreateGPUShader(device, &shader_info); require(vs != NULL);
    shader_info.code_size = sizeof(fragment); shader_info.code = (uint8_t *)fragment; shader_info.stage = SDL_GPU_SHADERSTAGE_FRAGMENT;
    SDL_GPUShader *fs = SDL_CreateGPUShader(device, &shader_info); require(fs != NULL);
    RasterChecks checks = {.device = device, .info = {.vertex_shader = vs, .fragment_shader = fs, .primitive_type = SDL_GPU_PRIMITIVETYPE_TRIANGLELIST, .rasterizer_state = {.enable_depth_clip = true}}};
    VulkanRenderer *renderer = (VulkanRenderer *)device->driverData;
    PFN_vkCreateGraphicsPipelines original = renderer->vkCreateGraphicsPipelines;
    bool nonsolid = renderer->supportsFillModeNonSolid;
    renderer->supportsFillModeNonSolid = false; checks.info.rasterizer_state.fill_mode = SDL_GPU_FILLMODE_LINE;
    require(e2d_render_line_pipeline(device, 1, &checks.info, 1, 0) == NULL);
    require(e2d_render_raster_pipeline(device, 1, &checks.info) == NULL);
    renderer->supportsFillModeNonSolid = nonsolid; checks.info.rasterizer_state.fill_mode = SDL_GPU_FILLMODE_FILL;

    int vertex_refs = SDL_GetAtomicInt(&((VulkanShader *)vs)->referenceCount);
    int fragment_refs = SDL_GetAtomicInt(&((VulkanShader *)fs)->referenceCount);
    if (SDL_getenv("ELECTRON2D_TEST_RASTER_FAILURE")) {
        for (int i = 0; i < 3; i++) {
            require(e2d_render_raster_pipeline(device, 1, &checks.info) == NULL);
            require(SDL_GetAtomicInt(&((VulkanShader *)vs)->referenceCount) == vertex_refs);
            require(SDL_GetAtomicInt(&((VulkanShader *)fs)->referenceCount) == fragment_refs);
        }
        puts("Native raster failure restores both shader reference counts in three attempts");
    } else {
        require(e2d_render_raster_support(device, 1) == 1);
        int first_id = SDL_GetAtomicInt(&renderer->layoutResourceID);
        SDL_GPUGraphicsPipeline *pipeline = e2d_render_raster_pipeline(device, 1, &checks.info); require(pipeline != NULL);
        VulkanGraphicsPipeline *native = (VulkanGraphicsPipeline *)pipeline;
        for (int i = 0; i < 4; i++) require(native->resourceLayout->descriptorSetLayouts[i]->ID >= (uint32_t)first_id && native->resourceLayout->descriptorSetLayouts[i]->ID < (uint32_t)first_id + 4);
        require(SDL_GetAtomicInt(&renderer->layoutResourceID) == first_id + 4);
        SDL_ReleaseGPUGraphicsPipeline(device, pipeline); require(SDL_WaitForGPUIdle(device));
        SDL_Thread *a = SDL_CreateThread(raster_thread, "raster A", &checks), *b = SDL_CreateThread(raster_thread, "raster B", &checks); require(a != NULL && b != NULL);
        SDL_WaitThread(a, NULL); SDL_WaitThread(b, NULL); require(SDL_WaitForGPUIdle(device));
        require(SDL_GetAtomicInt(&renderer->layoutResourceID) == first_id + 4);
        require(SDL_GetAtomicInt(&((VulkanShader *)vs)->referenceCount) == vertex_refs);
        require(SDL_GetAtomicInt(&((VulkanShader *)fs)->referenceCount) == fragment_refs);
        puts("Native raster layout IDs, 64 concurrent creations/retirements and shader references passed");
    }
    require(renderer->vkCreateGraphicsPipelines == original);
    shader_info.code = (const uint8_t *)vertex; shader_info.code_size = sizeof(vertex); shader_info.stage = SDL_GPU_SHADERSTAGE_VERTEX;
    if (!getenv("ELECTRON2D_TEST_RASTER_FAILURE")) image_check(device, shader_info, fragment, sizeof(fragment));
    SDL_ReleaseGPUShader(device, vs); SDL_ReleaseGPUShader(device, fs); require(SDL_WaitForGPUIdle(device));
}
typedef struct LineLimits { float minimum, maximum, granularity; } LineLimits;
extern int e2d_render_line_support(SDL_GPUDevice *, uint32_t, LineLimits *);
int main(int argc, char **argv)
{
    MultisampleState samples = {0};
    DepthStencilState depth = {0};
    require(e2d_render_logic_support(NULL, 1) == -1);
    require(e2d_render_logic_support((SDL_GPUDevice *)(uintptr_t)1, 0) == -1);
    require(e2d_render_logic_pipeline((SDL_GPUDevice *)(uintptr_t)1, 0, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples, &depth, 0, 1) == NULL);
    require(e2d_render_logic_pipeline((SDL_GPUDevice *)(uintptr_t)1, 1, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples, &depth, 0, 0) == NULL);
    require(e2d_render_logic_pipeline((SDL_GPUDevice *)(uintptr_t)1, 1, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples, &depth, 0, 17) == NULL);
    require(e2d_render_image_support(NULL, 1) == -1);
    require(e2d_render_image_support((SDL_GPUDevice *)(uintptr_t)1, 0) == -1);
    require(e2d_render_image_transition((SDL_GPUDevice *)(uintptr_t)1, 0, (SDL_GPUCommandBuffer *)(uintptr_t)1, NULL, 0, 1) == -1);
    require(e2d_render_bind_image((SDL_GPUDevice *)(uintptr_t)1, 0, (SDL_GPUCommandBuffer *)(uintptr_t)1, (SDL_GPUTexture *)(uintptr_t)1, 0, 0, 1) == -1);
    require(e2d_render_prepare_images((SDL_GPUDevice *)(uintptr_t)1, 0, (SDL_GPUCommandBuffer *)(uintptr_t)1, 0, NULL, 0) == -1);
    e2d_render_image_release((SDL_GPUDevice *)(uintptr_t)1, 0);
    require(e2d_render_depth_support(NULL, 1) == -1);
    require(e2d_render_depth_support((SDL_GPUDevice *)(uintptr_t)1, 0) == -1);
    require(e2d_render_depth_pipeline((SDL_GPUDevice *)(uintptr_t)1, 0, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples, &depth) == NULL);
    require(e2d_render_depth_pipeline((SDL_GPUDevice *)(uintptr_t)1, 1, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples, NULL) == NULL);
    depth.flags = 4; require(e2d_render_depth_pipeline((SDL_GPUDevice *)(uintptr_t)1, 1, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples, &depth) == NULL);
    depth.flags = 0; depth.minimum = -1; require(e2d_render_depth_pipeline((SDL_GPUDevice *)(uintptr_t)1, 1, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples, &depth) == NULL);
    depth.minimum = 0; depth.back_reference = 256; require(e2d_render_depth_pipeline((SDL_GPUDevice *)(uintptr_t)1, 1, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples, &depth) == NULL);
    require(e2d_render_multisample_support(NULL, 1) == -1);
    require(e2d_render_multisample_support((SDL_GPUDevice *)(uintptr_t)1, 0) == -1);
    require(e2d_render_multisample_pipeline((SDL_GPUDevice *)(uintptr_t)1, 0, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples) == NULL);
    require(e2d_render_multisample_pipeline((SDL_GPUDevice *)(uintptr_t)1, 1, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, NULL) == NULL);
    samples.flags = 8; require(e2d_render_multisample_pipeline((SDL_GPUDevice *)(uintptr_t)1, 1, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples) == NULL);
    samples.flags = 0; samples.minimum = -1; require(e2d_render_multisample_pipeline((SDL_GPUDevice *)(uintptr_t)1, 1, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 1, 0, &samples) == NULL);
    LineLimits lines = {0};
    require(e2d_render_line_support(NULL, 1, &lines) == -1);
    require(e2d_render_line_support((SDL_GPUDevice *)(uintptr_t)1, 0, &lines) == -1);
    require(e2d_render_line_support((SDL_GPUDevice *)(uintptr_t)1, 1, NULL) == -1);
    require(e2d_render_line_pipeline((SDL_GPUDevice *)(uintptr_t)1, 0, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1, 4, 0) == NULL);
    require(e2d_render_raster_support(NULL, 1) == -1);
    require(e2d_render_raster_support((SDL_GPUDevice *)(uintptr_t)1, 0) == -1);
    require(e2d_render_raster_pipeline((SDL_GPUDevice *)(uintptr_t)1, 0, (const SDL_GPUGraphicsPipelineCreateInfo *)(uintptr_t)1) == NULL);
    require(e2d_render_graphics_storage_support(NULL, 1) == -1);
    require(e2d_render_graphics_storage_support((SDL_GPUDevice *)(uintptr_t)1, 0) == -1);
    require(e2d_render_storage_barrier(NULL, 1, NULL) == -1);
    require(e2d_render_storage_barrier((SDL_GPUDevice *)(uintptr_t)1, 0, (SDL_GPUCommandBuffer *)(uintptr_t)1) == -1);
    require(e2d_render_shader16_support(NULL, 1) == -1);
    require(e2d_render_shader16_support(NULL, 2) == -1);
    require(e2d_render_shader16_support(NULL, 3) == -1);
    require(e2d_render_shader16_support((SDL_GPUDevice *)(uintptr_t)1, 4) == -1);
    require(e2d_render_shader16_support((SDL_GPUDevice *)(uintptr_t)1, 0) == -1);
    enum { FILTER_COUNT = SDL_GPU_TEXTUREFORMAT_ASTC_12x12_FLOAT + 1 };
    uint8_t filters[FILTER_COUNT]; SDL_memset(filters, 0xA5, sizeof(filters));
    require(e2d_render_filter_support(NULL, 1, filters, FILTER_COUNT) == -1);
    require(e2d_render_filter_support((SDL_GPUDevice *)(uintptr_t)1, 0, filters, FILTER_COUNT) == -1);
    require(e2d_render_filter_support((SDL_GPUDevice *)(uintptr_t)1, 1, filters, FILTER_COUNT - 1) == -1);
    require(e2d_render_filter_support((SDL_GPUDevice *)(uintptr_t)1, 1, NULL, FILTER_COUNT) == -1);
    for (uint32_t i = 0; i < FILTER_COUNT; ++i) require(filters[i] == 0xA5);
    require(e2d_render_texture_format(NULL, 1, NULL) == -1);
    require(e2d_render_texture_format((SDL_GPUDevice *)(uintptr_t)1, 0, (SDL_GPUTexture *)(uintptr_t)1) == -1);
    uint64_t limits[37] = {0};
    require(e2d_render_limits(NULL, 1, limits, 37) == -1);
    require(e2d_render_limits((SDL_GPUDevice *)(uintptr_t)1, 0, limits, 37) == -1);
    require(e2d_render_limits((SDL_GPUDevice *)(uintptr_t)1, 1, limits, 36) == -1);
    require(e2d_render_limits((SDL_GPUDevice *)(uintptr_t)1, 1, NULL, 37) == -1);
    Memory memory = {0};
    SDL_GPUSamplerCreateInfo sampler_info = {.min_filter = SDL_GPU_FILTER_NEAREST, .mag_filter = SDL_GPU_FILTER_NEAREST, .mipmap_mode = SDL_GPU_SAMPLERMIPMAPMODE_NEAREST, .address_mode_u = SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE, .address_mode_v = SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE, .address_mode_w = SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE, .compare_op = SDL_GPU_COMPAREOP_ALWAYS, .max_lod = 1};
    require(e2d_render_sampler(NULL, 1, &sampler_info, 3, 2, 2, 0) == NULL);
    require(e2d_render_sampler((SDL_GPUDevice *)(uintptr_t)1, 0, &sampler_info, 3, 2, 2, 0) == NULL);
    require(e2d_render_sampler((SDL_GPUDevice *)(uintptr_t)1, 1, &sampler_info, 5, 2, 2, 0) == NULL);
    require(e2d_render_sampler((SDL_GPUDevice *)(uintptr_t)1, 1, &sampler_info, 3, 2, 6, 0) == NULL);
    require(e2d_render_memory(NULL, 1, &memory) == -1);
    require(e2d_render_memory((SDL_GPUDevice *)(uintptr_t)1, 0, &memory) == -1);
    if (argc == 2 && SDL_strcmp(argv[1], "--unsupported-version") == 0) {
        require(e2d_render_memory((SDL_GPUDevice *)(uintptr_t)1, 1, &memory) == 0);
        require(e2d_render_sampler((SDL_GPUDevice *)(uintptr_t)1, 1, &sampler_info, 3, 2, 2, 0) == NULL);
        require(e2d_render_sampler_view((SDL_GPUDevice *)(uintptr_t)1, 1, (SDL_GPUTexture *)(uintptr_t)1) == -1);
        require(e2d_render_limits((SDL_GPUDevice *)(uintptr_t)1, 1, limits, 37) == 0);
        require(e2d_render_filter_support((SDL_GPUDevice *)(uintptr_t)1, 1, filters, FILTER_COUNT) == 0);
        for (uint32_t i = 0; i < FILTER_COUNT; ++i) require(filters[i] == 0xA5);
        require(e2d_render_texture_format((SDL_GPUDevice *)(uintptr_t)1, 1, (SDL_GPUTexture *)(uintptr_t)1) == -1);
        puts("Unsupported native core rejected before opaque-device access.");
        return 0;
    }
    require(e2d_render_empty_support(NULL, 1) == -1);
    require(e2d_render_empty_support((SDL_GPUDevice *)(uintptr_t)1, 0) == -1);
    require(e2d_render_empty_create((SDL_GPUDevice *)(uintptr_t)1, 0, 7, 5, 0) == NULL);
    require(e2d_render_empty_begin((SDL_GPUDevice *)(uintptr_t)1, 0, (SDL_GPUCommandBuffer *)(uintptr_t)1, (void *)(uintptr_t)1) == NULL);
    require(SDL_Init(SDL_INIT_VIDEO));
    SDL_GPUDevice *device = SDL_CreateGPUDevice(SDL_GPU_SHADERFORMAT_SPIRV, true, "vulkan");
    require(device != NULL);
    int logic_features = e2d_render_logic_support(device, 1); require(logic_features == 0 || logic_features == 1);
    printf("Native logical color feature=%d; ABI and operation-range guards passed\n", logic_features);
    raster_check(device);
    int multisample_features = e2d_render_multisample_support(device, 1); require(multisample_features >= 0 && multisample_features <= 3);
    printf("Native multisample feature mask=%d (sample shading=1, alpha-to-one=2)\n", multisample_features);
    int depth_features = e2d_render_depth_support(device, 1); require(depth_features >= 0 && depth_features <= 3);
    printf("Native depth feature mask=%d (per-face=1, depth bounds=2)\n", depth_features);

    int wide = e2d_render_line_support(device, 1, &lines); require(wide == 0 || wide == 1);
    if (wide) { require(lines.minimum > 0 && lines.maximum >= lines.minimum && lines.granularity > 0); printf("Native line range %g..%g, granularity %g (physical feature; logical opt-in independent)\n", lines.minimum, lines.maximum, lines.granularity); }

    require(e2d_render_sampler(device, 1, &sampler_info, 4, 2, 2, 0) == NULL);
    require(e2d_render_sampler(device, 1, &sampler_info, 2, 4, 2, 0) == NULL);
    int empty_samples = e2d_render_empty_support(device, 1); require(empty_samples > 0 && empty_samples <= 15);
    Memory empty_before = read_memory(device);
    void *empty = e2d_render_empty_create(device, 1, 7, 5, 0); require(empty != NULL);
    require(e2d_render_empty_create(device, 1, 0, 5, 0) == NULL);
    require(e2d_render_empty_create(device, 1, UINT32_MAX, 5, 0) == NULL);
    require(e2d_render_empty_create(device, 1, 7, 5, 4) == NULL);
    Memory empty_after = read_memory(device);
    require(empty_before.buffers == empty_after.buffers && empty_before.textures == empty_after.textures && empty_before.total == empty_after.total);
    SDL_GPUCommandBuffer *empty_commands = SDL_AcquireGPUCommandBuffer(device); require(empty_commands != NULL);
    SDL_GPUCopyPass *empty_copy = SDL_BeginGPUCopyPass(empty_commands); require(empty_copy != NULL);
    require(e2d_render_empty_begin(device, 1, empty_commands, empty) == NULL); SDL_EndGPUCopyPass(empty_copy);
    SDL_GPURenderPass *empty_pass = e2d_render_empty_begin(device, 1, empty_commands, empty); require(empty_pass != NULL);
    require(e2d_render_empty_begin(device, 1, empty_commands, empty) == NULL);
    SDL_EndGPURenderPass(empty_pass); require(SDL_SubmitGPUCommandBuffer(empty_commands)); require(SDL_WaitForGPUIdle(device));
    e2d_render_empty_release(device, 1, empty);
    printf("Native attachmentless sample mask=%d, ABI/pass/extent guards and zero image/buffer allocation passed\n", empty_samples);
    int graphics_storage = e2d_render_graphics_storage_support(device, 1); require(graphics_storage >= 0 && graphics_storage <= 3);
    SDL_GPUCommandBuffer *storage_commands = SDL_AcquireGPUCommandBuffer(device); require(storage_commands != NULL);
    require(e2d_render_storage_barrier(device, 1, storage_commands) == 1);
    SDL_GPUCopyPass *storage_copy = SDL_BeginGPUCopyPass(storage_commands); require(storage_copy != NULL);
    require(e2d_render_storage_barrier(device, 1, storage_commands) == -1);
    SDL_EndGPUCopyPass(storage_copy);
    require(e2d_render_storage_barrier(device, 1, storage_commands) == 1);
    require(SDL_CancelGPUCommandBuffer(storage_commands));
    printf("Native graphics storage support mask=%d and idle/pass barrier guards passed\n", graphics_storage);
    int shader16 = e2d_render_shader16_support(device, 1); require(shader16 >= 0 && shader16 <= 31);
    int shader16_v2 = e2d_render_shader16_support(device, 2); require(shader16_v2 >= 0 && shader16_v2 <= 63 && (shader16_v2 & 31) == shader16);
    int narrow_v3 = e2d_render_shader16_support(device, 3); require(narrow_v3 >= 0 && narrow_v3 <= 1023 && (narrow_v3 & 63) == shader16_v2);
    printf("Native narrow feature masks ABI1=%d ABI2=%d ABI3=%d\n", shader16, shader16_v2, narrow_v3);
    require(e2d_render_filter_support(device, 1, filters, FILTER_COUNT) == 1);
    require(filters[SDL_GPU_TEXTUREFORMAT_INVALID] == 0);
    require(filters[SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM] == 3);
    require(filters[SDL_GPU_TEXTUREFORMAT_R32_UINT] == 1);
    for (uint32_t i = 0; i < FILTER_COUNT; ++i) require(filters[i] == 0 || filters[i] == 1 || filters[i] == 3);
    const VulkanRenderer *filter_renderer = (const VulkanRenderer *)device->driverData;
    PFN_vkGetInstanceProcAddr instance_proc = (PFN_vkGetInstanceProcAddr)SDL_Vulkan_GetVkGetInstanceProcAddr();
    PFN_vkGetPhysicalDeviceFormatProperties format_properties = (PFN_vkGetPhysicalDeviceFormatProperties)instance_proc(filter_renderer->instance, "vkGetPhysicalDeviceFormatProperties");
    require(format_properties != NULL);
    const struct { SDL_GPUTextureFormat sdl; VkFormat vk; SDL_GPUTextureUsageFlags usage; } oracle[] = {
        {SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM, VK_FORMAT_R8G8B8A8_UNORM, SDL_GPU_TEXTUREUSAGE_SAMPLER},
        {SDL_GPU_TEXTUREFORMAT_R32_UINT, VK_FORMAT_R32_UINT, SDL_GPU_TEXTUREUSAGE_SAMPLER},
        {SDL_GPU_TEXTUREFORMAT_R32G32B32A32_FLOAT, VK_FORMAT_R32G32B32A32_SFLOAT, SDL_GPU_TEXTUREUSAGE_SAMPLER},
        {SDL_GPU_TEXTUREFORMAT_D32_FLOAT, VK_FORMAT_D32_SFLOAT, SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET}
    };
    for (uint32_t i = 0; i < SDL_arraysize(oracle); ++i) {
        VkFormatProperties props = {0}; format_properties(filter_renderer->physicalDevice, oracle[i].vk, &props);
        uint8_t expected = 0;
        if ((props.optimalTilingFeatures & VK_FORMAT_FEATURE_SAMPLED_IMAGE_BIT) != 0 && SDL_GPUTextureSupportsFormat(device, oracle[i].sdl, SDL_GPU_TEXTURETYPE_2D, oracle[i].usage))
            expected = 1 | ((props.optimalTilingFeatures & VK_FORMAT_FEATURE_SAMPLED_IMAGE_FILTER_LINEAR_BIT) != 0 ? 2 : 0);
        require(filters[oracle[i].sdl] == expected);
        if (expected != 0) {
            SDL_GPUTextureCreateInfo info = {.type=SDL_GPU_TEXTURETYPE_2D, .format=oracle[i].sdl, .usage=oracle[i].usage, .width=1, .height=1, .layer_count_or_depth=1, .num_levels=1, .sample_count=SDL_GPU_SAMPLECOUNT_1};
            SDL_GPUTexture *texture = SDL_CreateGPUTexture(device, &info); require(texture != NULL);
            require(e2d_render_texture_format(device, 1, texture) == (int)oracle[i].sdl);
            SDL_ReleaseGPUTexture(device, texture);
        }
    }
    uint8_t filter_again[FILTER_COUNT]; require(e2d_render_filter_support(device, 1, filter_again, FILTER_COUNT) == 1);
    require(SDL_memcmp(filters, filter_again, sizeof(filters)) == 0);

    require(e2d_render_limits(device, 1, limits, 37) == 1);
    for (uint32_t i = 0; i < 37; ++i) require((i == 11 || i == 13 || i == 14) ? limits[i] == 0 : limits[i] > 0);
    uint64_t again[37]; require(e2d_render_limits(device, 1, again, 37) == 1);
    require(SDL_memcmp(limits, again, sizeof(limits)) == 0);
    require(limits[12] >= 4096 && limits[31] >= 128 && limits[26] > 0);
    for (uint32_t color = 0; color < 6; ++color) {
        SDL_GPUSampler *sampler = e2d_render_sampler(device, 1, &sampler_info, 3, 2, color, 0);
        require(sampler != NULL); SDL_ReleaseGPUSampler(device, sampler);
    }
    sampler_info.max_lod = 0;
    SDL_GPUSampler *coordinate = e2d_render_sampler(device, 1, &sampler_info, 2, 2, 2, 1);
    require(coordinate != NULL); SDL_ReleaseGPUSampler(device, coordinate);
    sampler_info.min_filter = SDL_GPU_FILTER_LINEAR;
    require(e2d_render_sampler(device, 1, &sampler_info, 2, 2, 2, 1) == NULL);
    sampler_info.min_filter = SDL_GPU_FILTER_NEAREST; sampler_info.max_lod = 1;
    require(e2d_render_sampler(device, 1, &sampler_info, 2, 2, 2, 1) == NULL);
    sampler_info.max_lod = 0;
    require(e2d_render_sampler(device, 1, &sampler_info, 1, 2, 2, 1) == NULL);
    require(e2d_render_sampler_view((SDL_GPUDevice *)(uintptr_t)1, 0, (SDL_GPUTexture *)(uintptr_t)1) == -1);
    SDL_GPUTextureCreateInfo view_info = {.type = SDL_GPU_TEXTURETYPE_2D, .format = SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
        .usage = SDL_GPU_TEXTUREUSAGE_SAMPLER, .width = 2, .height = 2, .layer_count_or_depth = 1, .num_levels = 1, .sample_count = SDL_GPU_SAMPLECOUNT_1};
    for (int kind = 0; kind < 3; ++kind) {
        view_info.type = kind == 2 ? SDL_GPU_TEXTURETYPE_2D_ARRAY : SDL_GPU_TEXTURETYPE_2D;
        view_info.num_levels = kind == 1 ? 2 : 1;
        SDL_GPUTexture *texture = SDL_CreateGPUTexture(device, &view_info); require(texture != NULL);
        require(e2d_render_sampler_view(device, 1, texture) == (kind == 0)); SDL_ReleaseGPUTexture(device, texture);
    }
    require(SDL_WaitForGPUIdle(device));
    SDL_PropertiesID relaxed_properties = SDL_CreateProperties(); require(relaxed_properties != 0);
    require(SDL_SetStringProperty(relaxed_properties, SDL_PROP_GPU_DEVICE_CREATE_NAME_STRING, "vulkan"));
    require(SDL_SetBooleanProperty(relaxed_properties, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_SPIRV_BOOLEAN, true));
    require(SDL_SetBooleanProperty(relaxed_properties, SDL_PROP_GPU_DEVICE_CREATE_FEATURE_ANISOTROPY_BOOLEAN, false));
    SDL_GPUDevice *relaxed = SDL_CreateGPUDeviceWithProperties(relaxed_properties); SDL_DestroyProperties(relaxed_properties); require(relaxed != NULL);
    sampler_info.enable_anisotropy = true; sampler_info.max_anisotropy = 4;
    require(e2d_render_sampler(relaxed, 1, &sampler_info, 2, 2, 2, 0) == NULL);
    sampler_info.enable_anisotropy = false; SDL_DestroyGPUDevice(relaxed);
    Memory initial = read_memory(device);
    SDL_Thread *thread = SDL_CreateThread(reader, "render-memory-reader", device);
    require(thread != NULL);
    for (int i = 0; i < 32; ++i) {
        SDL_GPUBufferCreateInfo buffer_info = {.usage = SDL_GPU_BUFFERUSAGE_VERTEX, .size = 1024 * 1024};
        SDL_GPUBuffer *buffer = SDL_CreateGPUBuffer(device, &buffer_info);
        require(buffer != NULL);
        Memory buffers = read_memory(device);
        require(buffers.buffers >= initial.buffers + buffer_info.size);
        SDL_GPUTextureCreateInfo texture_info = {.type = SDL_GPU_TEXTURETYPE_2D, .format = SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
            .usage = SDL_GPU_TEXTUREUSAGE_SAMPLER, .width = 17, .height = 19, .layer_count_or_depth = 1, .num_levels = 1,
            .sample_count = SDL_GPU_SAMPLECOUNT_1};
        SDL_GPUTexture *texture = SDL_CreateGPUTexture(device, &texture_info);
        require(texture != NULL);
        Memory textures = read_memory(device);
        require(textures.textures >= initial.textures + 17 * 19 * 4 && textures.total > textures.textures + textures.buffers);
        SDL_ReleaseGPUBuffer(device, buffer); SDL_ReleaseGPUTexture(device, texture);
        require(SDL_WaitForGPUIdle(device));
        Memory retired = read_memory(device);
        require(retired.textures == initial.textures && retired.buffers == initial.buffers);
    }
    SDL_SetAtomicInt(&stop, 1); SDL_WaitThread(thread, NULL);
    require(SDL_GetAtomicInt(&failed) == 0);
    Memory retained = read_memory(device);
    printf("32 native allocation/retirement cycles and concurrent snapshots; texture=%llu buffer=%llu committed=%llu.\n",
        (unsigned long long)retained.textures, (unsigned long long)retained.buffers, (unsigned long long)retained.total);
    SDL_DestroyGPUDevice(device); SDL_Quit();
    return 0;
}
