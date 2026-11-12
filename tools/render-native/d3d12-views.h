#ifndef ELECTRON2D_D3D12_VIEWS_H
#define ELECTRON2D_D3D12_VIEWS_H

#include <SDL3/SDL_gpu.h>
#include <stdint.h>

#define E2D_D3D12_VIEW_ABI 6
#define E2D_D3D12_ARRAY_ABI 7
#define E2D_VIEW_CACHE "Electron2D.render.texture_views"

bool e2d_d3d12_device(SDL_GPUDevice *device);
int e2d_d3d12_view_format(SDL_GPUDevice *device, const SDL_GPUTextureCreateInfo *info, SDL_GPUTextureFormat format);
SDL_GPUTexture *e2d_d3d12_view_create(SDL_GPUDevice *device, SDL_GPUTexture *source,
    SDL_GPUTextureFormat format, uint32_t layer, uint32_t layers, uint32_t level, uint32_t levels, uint32_t array);
SDL_GPUTexture *e2d_d3d12_view_region(SDL_GPUTexture *texture, uint32_t *layer, uint32_t *level);

int e2d_d3d12_compute_arrays(SDL_GPUDevice *device, SDL_GPUCommandBuffer *commands,
    SDL_GPUTexture *const *images, uint32_t count, uint32_t prepare);

#endif
