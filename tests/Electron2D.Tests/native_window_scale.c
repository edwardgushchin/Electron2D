#include <SDL3/SDL.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

static void require(bool condition, const char *message)
{
    if (!condition) {
        fprintf(stderr, "%s: %s\n", message, SDL_GetError());
        exit(EXIT_FAILURE);
    }
}

static void ready(const char *directory, int phase)
{
    char path[4096];
    int length = snprintf(path, sizeof path, "%s/ready-%d", directory, phase);
    require(length > 0 && (size_t)length < sizeof path, "Control path exceeds capacity");
    FILE *file = fopen(path, "w");
    require(file != NULL, "Cannot create control marker");
    require(fputs("ready", file) >= 0 && fclose(file) == 0, "Cannot write control marker");
}

int main(void)
{
    const char *directory = getenv("ELECTRON2D_DPI_CONTROL_DIR");
    require(directory && directory[0], "Run through the isolated Wayland DPI helper");
    /* A native acquisition can stall before the helper observes another frame. */
    alarm(30);
    require(SDL_GetVersion() == SDL_VERSION, "SDL headers and runtime must match");
    require(SDL_Init(SDL_INIT_VIDEO), "Cannot initialize video");
    require(strcmp(SDL_GetCurrentVideoDriver(), "wayland") == 0, "Native Wayland is required");
    int count = getenv("ELECTRON2D_DPI_PROBE_SINGLE") ? 1 : 2;
    bool mailbox = getenv("ELECTRON2D_DPI_PROBE_MAILBOX") != NULL;
    SDL_GPUDevice *device = SDL_CreateGPUDevice(SDL_GPU_SHADERFORMAT_SPIRV, false, "vulkan");
    require(device != NULL, "Cannot create Vulkan GPU device");
    SDL_Window *windows[2];
    int changes[2] = {0}, rendered[2] = {0};
    Uint32 widths[2] = {0}, heights[2] = {0};
    for (int i = 0; i < count; i++) {
        windows[i] = SDL_CreateWindow(i ? "Child native scale" : "Root native scale", i ? 128 : 320, i ? 96 : 240,
                                      SDL_WINDOW_HIDDEN | SDL_WINDOW_RESIZABLE | SDL_WINDOW_HIGH_PIXEL_DENSITY);
        require(windows[i] != NULL, "Cannot create window");
        require(SDL_ClaimWindowForGPUDevice(device, windows[i]), "Cannot claim window");
        if (mailbox)
            require(SDL_SetGPUSwapchainParameters(device, windows[i], SDL_GPU_SWAPCHAINCOMPOSITION_SDR,
                                                 SDL_GPU_PRESENTMODE_MAILBOX), "Mailbox is unavailable");
        require(SDL_ShowWindow(windows[i]), "Cannot show window");
    }
    printf("Native SDL %d: %d windows, %s presentation\n", SDL_GetVersion(), count, mailbox ? "mailbox" : "FIFO");
    fflush(stdout);
    int phase = 0, observed = 0;
    for (int frame = 0; frame < 10000; frame++) {
        SDL_Event event;
        while (SDL_PollEvent(&event)) {
            if (event.type == SDL_EVENT_QUIT) require(false, "Unexpected native quit");
            if (event.type == SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED && phase) {
                for (int i = 0; i < count; i++) {
                    if (event.window.windowID != SDL_GetWindowID(windows[i])) continue;
                    changes[i]++;
                    observed = frame;
                    require(changes[i] <= phase, "Duplicate native scale notification");
                }
            }
        }
        for (int i = 0; i < count; i++) {
            SDL_GPUCommandBuffer *command = SDL_AcquireGPUCommandBuffer(device);
            require(command != NULL, "Cannot acquire command buffer");
            SDL_GPUTexture *texture = NULL;
            require(SDL_WaitAndAcquireGPUSwapchainTexture(command, windows[i], &texture, &widths[i], &heights[i]),
                    "Cannot acquire swapchain texture");
            if (texture) {
                SDL_GPUColorTargetInfo target = {0};
                target.texture = texture;
                target.clear_color = (SDL_FColor){i ? 0 : 1, i ? 1 : 0, 0, 1};
                target.load_op = SDL_GPU_LOADOP_CLEAR;
                target.store_op = SDL_GPU_STOREOP_STORE;
                SDL_GPURenderPass *pass = SDL_BeginGPURenderPass(command, &target, 1, NULL);
                require(pass != NULL, "Cannot begin clear pass");
                SDL_EndGPURenderPass(pass);
                rendered[i]++;
            }
            require(SDL_SubmitGPUCommandBuffer(command), "Cannot submit presentation");
        }
        if (frame == 16) {
            for (int i = 0; i < count; i++)
                require(SDL_fabsf(SDL_GetWindowDisplayScale(windows[i]) - 1) < .0001f, "Initial scale must be one");
            phase = 1;
            ready(directory, 0);
        }
        if (phase && changes[0] == phase && (count == 1 || changes[1] == phase) && frame >= observed + 8) {
            for (int i = 0; i < count; i++) {
                int width, height;
                require(SDL_GetWindowSizeInPixels(windows[i], &width, &height), "Cannot query pixel size");
                require(rendered[i] > 0 && widths[i] == (Uint32)width && heights[i] == (Uint32)height,
                        "Presented swapchain must match current pixel size");
                require(SDL_fabsf(SDL_GetWindowDisplayScale(windows[i]) - (phase == 1 ? 1.25f : 1)) < .0001f,
                        "Unexpected committed scale");
                rendered[i] = 0;
            }
            printf("Native SDL phase %d passed at frame %d\n", phase, frame);
            fflush(stdout);
            if (phase == 2) break;
            phase = 2;
            ready(directory, 1);
        }
        require(frame < 9999, "Frame limit exceeded");
        SDL_Delay(8);
    }
    require(SDL_WaitForGPUIdle(device), "Cannot wait for GPU idle");
    for (int i = count - 1; i >= 0; i--) {
        SDL_ReleaseWindowFromGPUDevice(device, windows[i]);
        SDL_DestroyWindow(windows[i]);
    }
    SDL_DestroyGPUDevice(device);
    SDL_Quit();
    alarm(0);
    return 0;
}
