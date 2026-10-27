#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <dlfcn.h>

#define LOAD(name, type, args) type (*name) args = dlsym(library, #name); if (!name) { fprintf(stderr, "%s\n", dlerror()); return 3; }
#define SDL_INIT_VIDEO 0x20u
#define SDL_GPU_SHADERFORMAT_SPIRV (1u << 1)
#define CHECK(value) do { if (!(value)) { fprintf(stderr, "failure: %s: %s\n", #value, SDL_GetError()); return 2; } } while (0)

int main(int argc, char **argv)
{
    if (argc != 6) return 3;
    for (int i = 2; i < argc; i++) if (argv[i][0] < '0' || argv[i][0] > '1' || argv[i][1]) return 3;
    bool computeFirst = atoi(argv[2]), renderRetiresFirst = atoi(argv[3]);
    bool lease = atoi(argv[4]), extraVideo = atoi(argv[5]);
    void *library = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
    if (!library) { fprintf(stderr, "%s\n", dlerror()); return 3; }
    LOAD(SDL_InitSubSystem, bool, (uint32_t));
    LOAD(SDL_QuitSubSystem, void, (uint32_t));
    LOAD(SDL_GetError, const char *, (void));
    LOAD(SDL_GetVersion, int, (void));
    LOAD(SDL_GetRevision, const char *, (void));
    LOAD(SDL_Delay, void, (uint32_t));
    LOAD(SDL_CreateGPUDevice, void *, (uint32_t, bool, const char *));
    LOAD(SDL_DestroyGPUDevice, void, (void *));
    LOAD(SDL_CreateWindow, void *, (const char *, int, int, uint64_t));
    LOAD(SDL_DestroyWindow, void, (void *));
    LOAD(SDL_ClaimWindowForGPUDevice, bool, (void *, void *));
    LOAD(SDL_ReleaseWindowFromGPUDevice, void, (void *, void *));
    LOAD(SDL_Vulkan_LoadLibrary, bool, (const char *));
    LOAD(SDL_Vulkan_UnloadLibrary, void, (void));
    setbuf(stdout, NULL);
    printf("SDL %d %s\n", SDL_GetVersion(), SDL_GetRevision());
    CHECK(SDL_InitSubSystem(SDL_INIT_VIDEO));
    if (lease) CHECK(SDL_Vulkan_LoadLibrary(NULL));
    void *compute = NULL;
    if (computeFirst) {
        if (extraVideo) CHECK(SDL_InitSubSystem(SDL_INIT_VIDEO));
        CHECK(compute = SDL_CreateGPUDevice(SDL_GPU_SHADERFORMAT_SPIRV, false, "vulkan"));
        puts("compute created before render");
    }
    void *window = SDL_CreateWindow("SDL device lifetime probe", 320, 240, 0);
    CHECK(window);
    void *render = SDL_CreateGPUDevice(SDL_GPU_SHADERFORMAT_SPIRV, false, "vulkan");
    CHECK(render); CHECK(SDL_ClaimWindowForGPUDevice(render, window));
    puts("render created and claimed");
    if (!computeFirst) {
        if (extraVideo) CHECK(SDL_InitSubSystem(SDL_INIT_VIDEO));
        CHECK(compute = SDL_CreateGPUDevice(SDL_GPU_SHADERFORMAT_SPIRV, false, "vulkan"));
        puts("compute created after render");
    }
    // Let an external sampler observe the loaded driver before retirement.
    SDL_Delay(1200);
    if (!renderRetiresFirst) {
        SDL_DestroyGPUDevice(compute);
        if (extraVideo) SDL_QuitSubSystem(SDL_INIT_VIDEO);
        puts("compute retired before render");
    }
    puts("releasing claimed window");
    SDL_ReleaseWindowFromGPUDevice(render, window);
    puts("window released");
    SDL_DestroyGPUDevice(render); SDL_DestroyWindow(window);
    if (renderRetiresFirst) {
        SDL_DestroyGPUDevice(compute);
        if (extraVideo) SDL_QuitSubSystem(SDL_INIT_VIDEO);
    }
    if (lease) SDL_Vulkan_UnloadLibrary();
    SDL_QuitSubSystem(SDL_INIT_VIDEO);
    dlclose(library);
    puts("all retired");
    return 0;
}
