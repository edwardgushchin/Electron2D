#include <stdint.h>
#include <stdio.h>
#include <math.h>
#include "timestamp-scale.h"
#include <SDL3/SDL.h>

extern int e2d_render_timestamp_create(SDL_GPUDevice *, uint32_t, uint32_t, void **);
extern int e2d_render_timestamp_reset(SDL_GPUDevice *, uint32_t, void *, SDL_GPUCommandBuffer *);
extern int e2d_render_timestamp_write(SDL_GPUDevice *, uint32_t, void *, SDL_GPUCommandBuffer *, uint32_t);
extern int e2d_render_timestamp_read(SDL_GPUDevice *, uint32_t, void *, uint32_t, uint64_t *);
extern void e2d_render_timestamp_destroy(SDL_GPUDevice *, uint32_t, void *);
#define CHECK(condition) do { if (!(condition)) { fprintf(stderr, "timestamp check failed at %d: %s\n", __LINE__, #condition); return 1; } } while (0)

int main(void)
{
    void *pool = NULL; uint64_t values[4] = {0};
    CHECK(timestamp_ns(UINT64_MAX, 64, 1, &values[0]) && values[0] == UINT64_MAX);
    CHECK(timestamp_ns(UINT64_MAX, 64, .5f, &values[0]) && values[0] == UINT64_MAX / 2);
    CHECK(timestamp_ns(123456789, 64, 2.5f, &values[0]) && values[0] == 308641972);
    CHECK(timestamp_ns(UINT64_MAX, 8, 1, &values[0]) && values[0] == 255);
    CHECK(timestamp_ns(1, 64, 0x1p-149f, &values[0]) && values[0] == 0);
    CHECK(!timestamp_ns(UINT64_MAX, 64, 2, &values[0]));
    CHECK(!timestamp_ns(1, 0, 1, &values[0]) && !timestamp_ns(1, 65, 1, &values[0]));
    CHECK(!timestamp_ns(1, 64, NAN, &values[0]) && !timestamp_ns(1, 64, -1, &values[0]));
    CHECK(e2d_render_timestamp_create(NULL, 1, 3, &pool) == -1);
    CHECK(SDL_Init(SDL_INIT_VIDEO));
    SDL_GPUDevice *device = SDL_CreateGPUDevice(SDL_GPU_SHADERFORMAT_SPIRV, true, "vulkan"); CHECK(device);
    CHECK(e2d_render_timestamp_create(device, 9, 3, &pool) == -1);
    CHECK(e2d_render_timestamp_create(device, 1, 0, &pool) == -1);
    CHECK(e2d_render_timestamp_create(device, 1, 3, &pool) == 1 && pool);
    CHECK(e2d_render_timestamp_read(device, 1, pool, 1, values) == -1);
    CHECK(e2d_render_timestamp_read(device, 1, pool, 0, values) == -1);
    CHECK(e2d_render_timestamp_read(device, 1, pool, 4, values) == -1);
    CHECK(e2d_render_timestamp_reset(device, 1, pool, NULL) == -1);
    SDL_GPUCommandBuffer *command = SDL_AcquireGPUCommandBuffer(device); CHECK(command);
    CHECK(e2d_render_timestamp_reset(device, 1, pool, command) == 1);
    CHECK(e2d_render_timestamp_write(device, 1, pool, command, 3) == -1);
    CHECK(e2d_render_timestamp_write(device, 1, pool, command, 0) == 1);
    CHECK(e2d_render_timestamp_write(device, 1, pool, command, 1) == 1);
    SDL_GPUFence *fence = SDL_SubmitGPUCommandBufferAndAcquireFence(command); CHECK(fence);
    CHECK(SDL_WaitForGPUFences(device, true, &fence, 1)); SDL_ReleaseGPUFence(device, fence);
    CHECK(e2d_render_timestamp_read(device, 1, pool, 2, values) == 1 && values[0] > 0 && values[1] >= values[0]);
    CHECK(e2d_render_timestamp_read(device, 1, pool, 3, values) == -1);
    e2d_render_timestamp_destroy(device, 9, pool);
    e2d_render_timestamp_destroy(device, 1, pool);
    SDL_DestroyGPUDevice(device); SDL_Quit();
    puts("Native timestamp ABI, unwritten queries, ordered real GPU readback and retirement passed."); return 0;
}
