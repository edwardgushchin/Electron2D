#include <SDL3/SDL.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

typedef struct { uint64_t textures, buffers, total; } Memory;
extern int e2d_render_memory(SDL_GPUDevice *, uint32_t, Memory *);
extern int e2d_render_sampler_view(SDL_GPUDevice *, uint32_t, SDL_GPUTexture *);
extern SDL_GPUSampler *e2d_render_sampler(SDL_GPUDevice *, uint32_t, const SDL_GPUSamplerCreateInfo *, uint32_t, uint32_t, uint32_t, uint32_t);

static void require(bool valid)
{
    if (!valid) { fprintf(stderr, "Native render memory check failed: %s\n", SDL_GetError()); exit(1); }
}
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
int main(int argc, char **argv)
{
    Memory memory = {0};
    SDL_GPUSamplerCreateInfo sampler_info = {.min_filter = SDL_GPU_FILTER_NEAREST, .mag_filter = SDL_GPU_FILTER_NEAREST, .mipmap_mode = SDL_GPU_SAMPLERMIPMAPMODE_NEAREST, .address_mode_u = SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE, .address_mode_v = SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE, .address_mode_w = SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE, .compare_op = SDL_GPU_COMPAREOP_ALWAYS, .max_lod = 1};
    require(e2d_render_sampler(NULL, 1, &sampler_info, 3, 2, 2, 0) == NULL);
    require(e2d_render_sampler((SDL_GPUDevice *)(uintptr_t)1, 0, &sampler_info, 3, 2, 2, 0) == NULL);
    require(e2d_render_sampler((SDL_GPUDevice *)(uintptr_t)1, 1, &sampler_info, 4, 2, 2, 0) == NULL);
    require(e2d_render_sampler((SDL_GPUDevice *)(uintptr_t)1, 1, &sampler_info, 3, 2, 6, 0) == NULL);
    require(e2d_render_memory(NULL, 1, &memory) == -1);
    require(e2d_render_memory((SDL_GPUDevice *)(uintptr_t)1, 0, &memory) == -1);
    if (argc == 2 && SDL_strcmp(argv[1], "--unsupported-version") == 0) {
        require(e2d_render_memory((SDL_GPUDevice *)(uintptr_t)1, 1, &memory) == 0);
        require(e2d_render_sampler((SDL_GPUDevice *)(uintptr_t)1, 1, &sampler_info, 3, 2, 2, 0) == NULL);
        require(e2d_render_sampler_view((SDL_GPUDevice *)(uintptr_t)1, 1, (SDL_GPUTexture *)(uintptr_t)1) == -1);
        puts("Unsupported native core rejected before opaque-device access.");
        return 0;
    }
    require(SDL_Init(SDL_INIT_VIDEO));
    SDL_GPUDevice *device = SDL_CreateGPUDevice(SDL_GPU_SHADERFORMAT_SPIRV, false, "vulkan");
    require(device != NULL);
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
