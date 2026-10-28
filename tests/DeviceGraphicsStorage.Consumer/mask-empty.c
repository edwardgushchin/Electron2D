#define VK_NO_PROTOTYPES
#include <vulkan/vulkan.h>
#include <SDL3/SDL_vulkan.h>
#include <dlfcn.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

static PFN_vkGetInstanceProcAddr actual_instance;
static PFN_vkGetDeviceProcAddr actual_device;
static PFN_vkDestroyRenderPass actual_destroy;
static uint32_t created, retired;
static VkResult VKAPI_CALL failed_framebuffer(VkDevice device, const VkFramebufferCreateInfo *info, const VkAllocationCallbacks *allocator, VkFramebuffer *framebuffer)
{
    (void)device; (void)info; (void)allocator;
    *framebuffer = (VkFramebuffer)(uintptr_t)0xDEADBEEF;
    created++; fprintf(stderr, "MASKED native attachmentless creation failure %u\n", created); return VK_ERROR_OUT_OF_DEVICE_MEMORY;
}
static void VKAPI_CALL destroy_pass(VkDevice device, VkRenderPass pass, const VkAllocationCallbacks *allocator)
{
    actual_destroy(device, pass, allocator); retired++;
    fprintf(stderr, "RETIRED native attachmentless failed creation %u\n", retired);
}
static PFN_vkVoidFunction VKAPI_CALL masked_device(VkDevice device, const char *name)
{
    if (strcmp(name, "vkCreateFramebuffer") == 0) {
        if (getenv("ELECTRON2D_TEST_EMPTY_FAILURE")) return (PFN_vkVoidFunction)failed_framebuffer;
        fprintf(stderr, "MASKED native attachmentless framebuffer procedure\n"); return NULL;
    }
    PFN_vkVoidFunction result = actual_device(device, name);
    if (getenv("ELECTRON2D_TEST_EMPTY_FAILURE") && strcmp(name, "vkDestroyRenderPass") == 0) {
        actual_destroy = (PFN_vkDestroyRenderPass)result; return (PFN_vkVoidFunction)destroy_pass;
    }
    return result;
}
static PFN_vkVoidFunction VKAPI_CALL masked_instance(VkInstance instance, const char *name)
{
    PFN_vkVoidFunction result = actual_instance(instance, name);
    if (result && strcmp(name, "vkGetDeviceProcAddr") == 0) {
        actual_device = (PFN_vkGetDeviceProcAddr)result;
        return (PFN_vkVoidFunction)masked_device;
    }
    return result;
}
SDL_FunctionPointer SDLCALL SDL_Vulkan_GetVkGetInstanceProcAddr(void)
{
    SDL_FunctionPointer (SDLCALL *getter)(void) = dlsym(RTLD_NEXT, "SDL_Vulkan_GetVkGetInstanceProcAddr");
    if (!getter) {
        void *library = dlopen("libSDL3.so.0", RTLD_NOW | RTLD_NOLOAD);
        if (library) {
            getter = dlsym(library, "SDL_Vulkan_GetVkGetInstanceProcAddr");
            dlclose(library);
        }
    }
    if (!getter) return NULL;
    actual_instance = (PFN_vkGetInstanceProcAddr)getter();
    return actual_instance ? (SDL_FunctionPointer)masked_instance : NULL;
}
