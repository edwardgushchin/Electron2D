#include "memory-layout.h"
#include <string.h>

#if defined(_WIN32)
#define E2D_TEST_EXPORT __declspec(dllexport)
#else
#define E2D_TEST_EXPORT __attribute__((visibility("default")))
#endif

static VulkanRenderer *renderer;
static PFN_vkCmdBeginDebugUtilsLabelEXT begin_label;
static PFN_vkCmdEndDebugUtilsLabelEXT end_label;
static PFN_vkCmdDraw draw;
static PFN_vkCmdDispatch dispatch;
static PFN_vkCmdCopyBuffer copy_buffer;
static PFN_vkEndCommandBuffer end_commands;
static VkCommandBuffer labeled;
static char expected_name[128];
static float expected_color[4];
enum Event { BEGIN, END, DRAW, DISPATCH, COPY, UNCLOSED, EVENT_COUNT };
static int counts[EVENT_COUNT], mismatch;

static VKAPI_ATTR void VKAPI_CALL observe_begin(VkCommandBuffer command, const VkDebugUtilsLabelEXT *info)
{
    ++counts[BEGIN];
    if (labeled || !command || info->sType != VK_STRUCTURE_TYPE_DEBUG_UTILS_LABEL_EXT || info->pNext ||
        strcmp(info->pLabelName, expected_name) != 0 || memcmp(info->color, expected_color, sizeof(expected_color)) != 0)
        ++mismatch;
    labeled = command;
    begin_label(command, info);
}

static VKAPI_ATTR void VKAPI_CALL observe_end(VkCommandBuffer command)
{
    ++counts[END];
    if (!labeled || labeled != command) ++mismatch;
    labeled = VK_NULL_HANDLE;
    end_label(command);
}

static VKAPI_ATTR void VKAPI_CALL observe_draw(VkCommandBuffer command, uint32_t vertices, uint32_t instances,
    uint32_t first_vertex, uint32_t first_instance)
{
    if (command == labeled) ++counts[DRAW];
    draw(command, vertices, instances, first_vertex, first_instance);
}

static VKAPI_ATTR void VKAPI_CALL observe_dispatch(VkCommandBuffer command, uint32_t x, uint32_t y, uint32_t z)
{
    if (command == labeled) ++counts[DISPATCH];
    dispatch(command, x, y, z);
}

static VKAPI_ATTR void VKAPI_CALL observe_copy(VkCommandBuffer command, VkBuffer source, VkBuffer destination,
    uint32_t region_count, const VkBufferCopy *regions)
{
    if (command == labeled) ++counts[COPY];
    copy_buffer(command, source, destination, region_count, regions);
}

static VKAPI_ATTR VkResult VKAPI_CALL observe_submit(VkCommandBuffer command)
{
    if (command == labeled) ++counts[UNCLOSED];
    return end_commands(command);
}

E2D_TEST_EXPORT int e2d_test_labels_open(SDL_GPUDevice *device)
{
    if (!device || renderer) return 0;
    VulkanRenderer *candidate = (VulkanRenderer *)device->driverData;
    if (!candidate || !candidate->supportsDebugUtils ||
        !candidate->vkCmdBeginDebugUtilsLabelEXT || !candidate->vkCmdEndDebugUtilsLabelEXT) return 0;
    renderer = candidate;
    begin_label = renderer->vkCmdBeginDebugUtilsLabelEXT;
    end_label = renderer->vkCmdEndDebugUtilsLabelEXT;
    draw = renderer->vkCmdDraw;
    dispatch = renderer->vkCmdDispatch;
    copy_buffer = renderer->vkCmdCopyBuffer;
    end_commands = renderer->vkEndCommandBuffer;
    renderer->vkCmdBeginDebugUtilsLabelEXT = observe_begin;
    renderer->vkCmdEndDebugUtilsLabelEXT = observe_end;
    renderer->vkCmdDraw = observe_draw;
    renderer->vkCmdDispatch = observe_dispatch;
    renderer->vkCmdCopyBuffer = observe_copy;
    renderer->vkEndCommandBuffer = observe_submit;
    labeled = VK_NULL_HANDLE;
    mismatch = 0;
    memset(counts, 0, sizeof(counts));
    return 1;
}

E2D_TEST_EXPORT int e2d_test_labels_expect(const char *name, float r, float g, float b, float a)
{
    if (!renderer || !name || strlen(name) >= sizeof(expected_name)) return 0;
    memcpy(expected_name, name, strlen(name) + 1);
    expected_color[0] = r;
    expected_color[1] = g;
    expected_color[2] = b;
    expected_color[3] = a;
    return 1;
}

E2D_TEST_EXPORT int e2d_test_labels_count(int index)
{
    return !mismatch && index >= 0 && index < EVENT_COUNT ? counts[index] : -1;
}

E2D_TEST_EXPORT void e2d_test_labels_close(void)
{
    if (!renderer) return;
    renderer->vkCmdBeginDebugUtilsLabelEXT = begin_label;
    renderer->vkCmdEndDebugUtilsLabelEXT = end_label;
    renderer->vkCmdDraw = draw;
    renderer->vkCmdDispatch = dispatch;
    renderer->vkCmdCopyBuffer = copy_buffer;
    renderer->vkEndCommandBuffer = end_commands;
    renderer = NULL;
}
