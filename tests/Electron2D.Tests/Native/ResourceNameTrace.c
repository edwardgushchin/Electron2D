#include "memory-layout.h"
#include <string.h>

#if defined(_WIN32)
#define E2D_TEST_EXPORT __declspec(dllexport)
#else
#define E2D_TEST_EXPORT __attribute__((visibility("default")))
#endif

static VulkanRenderer *renderer;
static PFN_vkSetDebugUtilsObjectNameEXT original;
static char expected_name[128];
static VkObjectType expected_type;
static int count, mismatch, refuse;

static VKAPI_ATTR VkResult VKAPI_CALL observe(VkDevice device, const VkDebugUtilsObjectNameInfoEXT *info)
{
    ++count;
    const char *actual = info->pObjectName ? info->pObjectName : "";
    if (device != renderer->logicalDevice || info->sType != VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT ||
        info->objectType != expected_type || !info->objectHandle || strcmp(actual, expected_name) != 0 ||
        (!*expected_name && info->pObjectName))
        ++mismatch;
    return refuse ? VK_ERROR_UNKNOWN : original(device, info);
}

E2D_TEST_EXPORT int e2d_test_name_trace_open(SDL_GPUDevice *device)
{
    if (!device || renderer) return 0;
    VulkanRenderer *candidate = (VulkanRenderer *)device->driverData;
    if (!candidate || !candidate->supportsDebugUtils || !candidate->vkSetDebugUtilsObjectNameEXT) return 0;
    renderer = candidate;
    original = renderer->vkSetDebugUtilsObjectNameEXT;
    renderer->vkSetDebugUtilsObjectNameEXT = observe;
    return 1;
}

E2D_TEST_EXPORT int e2d_test_name_trace_expect(uint32_t type, const char *name, int fail)
{
    if (!renderer || !name || strlen(name) >= sizeof(expected_name)) return 0;
    memcpy(expected_name, name, strlen(name) + 1);
    expected_type = (VkObjectType)type;
    count = mismatch = 0;
    refuse = fail;
    return 1;
}

E2D_TEST_EXPORT int e2d_test_name_trace_count(void)
{
    return mismatch ? -1 : count;
}

E2D_TEST_EXPORT void e2d_test_name_trace_close(void)
{
    if (renderer) renderer->vkSetDebugUtilsObjectNameEXT = original;
    renderer = NULL;
    original = NULL;
}
