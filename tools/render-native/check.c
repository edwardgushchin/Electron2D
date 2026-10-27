#include <SDL3/SDL.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include "memory-layout.h"
#include <SDL3/SDL_vulkan.h>

typedef struct { uint64_t textures, buffers, total; } Memory;
extern int e2d_render_filter_support(SDL_GPUDevice *, uint32_t, uint8_t *, uint32_t);
extern int e2d_render_texture_format(SDL_GPUDevice *, uint32_t, SDL_GPUTexture *);
extern int e2d_render_limits(SDL_GPUDevice *, uint32_t, uint64_t *, uint32_t);
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
    require(SDL_Init(SDL_INIT_VIDEO));
    SDL_GPUDevice *device = SDL_CreateGPUDevice(SDL_GPU_SHADERFORMAT_SPIRV, false, "vulkan");
    require(device != NULL);
    require(e2d_render_sampler(device, 1, &sampler_info, 4, 2, 2, 0) == NULL);
    require(e2d_render_sampler(device, 1, &sampler_info, 2, 4, 2, 0) == NULL);
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
