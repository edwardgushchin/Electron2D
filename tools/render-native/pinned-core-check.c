#define SDL_STATIC_LIB
#include <SDL3/SDL.h>
#include <string.h>
#undef NDEBUG
#include <assert.h>
static int version;
static const char *revision;
int SDLCALL SDL_GetVersion(void) { return version; }
const char *SDLCALL SDL_GetRevision(void) { return revision; }
int SDLCALL SDL_strcmp(const char *left, const char *right) { return strcmp(left, right); }
#include "pinned-core.h"
int main(void)
{
    version = SDL_VERSIONNUM(3, 4, 20);
    revision = "SDL-3.4.20-release-3.4.20"; assert(e2d_render_pinned_core());
    revision = "SDL-release-3.4.20-0-gf00dc919c (Electron2D memory 1)"; assert(e2d_render_pinned_core());
    version++; assert(!e2d_render_pinned_core()); version--;
    revision = "SDL-release-3.4.20-0-gf00dc919c"; assert(!e2d_render_pinned_core());
    revision = "SDL-release-3.4.20-0-gf00dc919c (Electron2D memory 2)"; assert(!e2d_render_pinned_core());
    revision = "SDL-3.4.20-release-3.4.20-extra"; assert(!e2d_render_pinned_core());
}
