#ifndef ELECTRON2D_PINNED_CORE_H
#define ELECTRON2D_PINNED_CORE_H
#include <SDL3/SDL.h>
static inline bool e2d_render_pinned_core(void)
{
    if (SDL_GetVersion() != SDL_VERSIONNUM(3, 4, 18)) return false;
    const char *revision = SDL_GetRevision();
    return SDL_strcmp(revision, "SDL-3.4.18-release-3.4.18") == 0 ||
        SDL_strcmp(revision, "SDL-release-3.4.18-0-g829a65d76 (Electron2D memory 1)") == 0;
}
#endif
