// Test-only format and render-pass failure injection, preserving ordinary Vulkan operations.
#include <SDL3/SDL.h>
#include <SDL3/SDL_vulkan.h>
#include "memory-layout.h"
#include <windows.h>
#include <string.h>
enum {
#define SDL_DYNAPI_PROC(rc, name, params, args, ret) slot_##name,
#include "SDL_dynapi_procs.h"
#undef SDL_DYNAPI_PROC
    slot_count
};
static PFN_vkGetPhysicalDeviceImageFormatProperties image_properties;
static PFN_vkCreateRenderPass create_pass;
static SDL_GPUDevice *(SDLCALL *actual_create)(SDL_PropertiesID);
static Sint64 (SDLCALL *actual_number)(SDL_PropertiesID, const char *, Sint64);
static int mode(const char *expected)
{
    char value[32]; return GetEnvironmentVariableA("ELECTRON2D_STENCIL_FAULT", value, sizeof(value)) && strcmp(value, expected) == 0;
}
static VkResult VKAPI_CALL masked_image(VkPhysicalDevice device, VkFormat format, VkImageType type, VkImageTiling tiling, VkImageUsageFlags usage, VkImageCreateFlags flags, VkImageFormatProperties *properties)
{
    if (format == VK_FORMAT_S8_UINT && mode("format")) return VK_ERROR_FORMAT_NOT_SUPPORTED;
    return image_properties(device, format, type, tiling, usage, flags, properties);
}
static Sint64 SDLCALL masked_number(SDL_PropertiesID properties, const char *name, Sint64 fallback)
{
    Sint64 value = actual_number(properties, name, fallback);
    if (strcmp(name, "Electron2D.gpu.depth_resolve") == 0) {
        if (mode("resolve-only")) return value & 2;
        if (mode("resolve-missing")) return value & 1;
    }
    return value;
}
static VkResult VKAPI_CALL masked_pass(VkDevice device, const VkRenderPassCreateInfo *info, const VkAllocationCallbacks *allocator, VkRenderPass *pass)
{
    if (mode("pass")) for (uint32_t i = 0; i < info->attachmentCount; i++) if (info->pAttachments[i].format == VK_FORMAT_S8_UINT) return VK_ERROR_OUT_OF_DEVICE_MEMORY;
    return create_pass(device, info, allocator, pass);
}
static SDL_GPUDevice *SDLCALL masked_create(SDL_PropertiesID props)
{
    SDL_GPUDevice *device = actual_create(props);
    if (device) {
        VulkanRenderer *r = (VulkanRenderer *)device->driverData;
        image_properties = r->vkGetPhysicalDeviceImageFormatProperties; create_pass = r->vkCreateRenderPass;
        r->vkGetPhysicalDeviceImageFormatProperties = masked_image; r->vkCreateRenderPass = masked_pass;
    }
    return device;
}
__declspec(dllexport) Sint32 SDLCALL SDL_DYNAPI_entry(Uint32 abi, void *table, Uint32 size)
{
    HMODULE library = GetModuleHandleW(L"SDL3.dll");
    Sint32 (SDLCALL *entry)(Uint32, void *, Uint32) = library ? (void *)GetProcAddress(library, "SDL_DYNAPI_entry") : NULL;
    if (!entry || size != slot_count * sizeof(void *)) return -1;
    Sint32 result = entry(abi, table, size); if (result < 0) return result;
    void **functions = table; actual_create = functions[slot_SDL_CreateGPUDeviceWithProperties]; functions[slot_SDL_CreateGPUDeviceWithProperties] = masked_create;
    actual_number = functions[slot_SDL_GetNumberProperty]; functions[slot_SDL_GetNumberProperty] = masked_number; return 0;
}
