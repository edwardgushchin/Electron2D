#ifndef ELECTRON2D_STENCIL_H
#define ELECTRON2D_STENCIL_H
#define E2D_STENCIL_PROPERTY "Electron2D.render.stencil_only"
static inline bool e2d_stencil_info(const SDL_GPUTextureCreateInfo *info)
{
    return info && info->props && SDL_GetBooleanProperty(info->props, E2D_STENCIL_PROPERTY, false);
}
#if defined(_WIN32)
__declspec(dllexport)
#else
__attribute__((visibility("default")))
#endif
int e2d_render_stencil_support(SDL_GPUDevice *, uint32_t, SDL_GPUTextureUsageFlags, uint32_t);
VkResult e2d_stencil_render_pass(PFN_vkCreateRenderPass, VkDevice, const VkRenderPassCreateInfo *, const VkAllocationCallbacks *, VkRenderPass *);
#endif
