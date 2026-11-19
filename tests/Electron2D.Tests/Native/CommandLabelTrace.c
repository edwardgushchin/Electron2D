#define COBJMACROS
#define E2D_RENDER_DRIVER_D3D12
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
static char expected_name[1024];
static float expected_color[4];
enum Event { BEGIN, END, DRAW, DISPATCH, COPY, UNCLOSED, CANCEL, EVENT_COUNT };
static int counts[EVENT_COUNT], mismatch;

#if defined(SDL_PLATFORM_WIN32)
static SDL_GPUDevice *d3d_device;
static SDL_GPUCommandBuffer *(*acquire)(SDL_GPURenderer *);
static bool (*cancel)(SDL_GPUCommandBuffer *);
static ID3D12GraphicsCommandList *active_list;
static ID3D12GraphicsCommandListVtbl *original;
static ID3D12GraphicsCommandListVtbl observed;
static wchar_t expected_wide[236];
static uint64_t expected_rgb;
static bool d3d_label;

static void STDMETHODCALLTYPE observe_d3d_begin(ID3D12GraphicsCommandList *list, UINT metadata,
    const void *payload, UINT size)
{
    ++counts[BEGIN];
    if (list != active_list || d3d_label || metadata != 2 || !payload ||
        size < 32 || size > 496 || size % 8 != 0) {
        ++mismatch;
    } else {
        const uint64_t *record = payload;
        const wchar_t *name = (const wchar_t *)(record + 3);
        const size_t name_size = (wcslen(expected_wide) + 1) * sizeof(*name);
        if (record[0] != 0x800 || record[1] != expected_rgb || record[2] != UINT64_C(0x0400000000000000) ||
            size - 24 != (name_size + 7) / 8 * 8 || memcmp(name, expected_wide, name_size) != 0) {
            ++mismatch;
        }
    }
    d3d_label = true;
    original->BeginEvent(list, metadata, payload, size);
}

static void STDMETHODCALLTYPE observe_d3d_end(ID3D12GraphicsCommandList *list)
{
    ++counts[END];
    if (list != active_list || !d3d_label) {
        ++mismatch;
    }
    d3d_label = false;
    original->EndEvent(list);
}

static void STDMETHODCALLTYPE observe_d3d_draw(ID3D12GraphicsCommandList *list,
    UINT vertices, UINT instances, UINT first_vertex, UINT first_instance)
{
    if (d3d_label) {
        ++counts[DRAW];
    }
    original->DrawInstanced(list, vertices, instances, first_vertex, first_instance);
}

static void STDMETHODCALLTYPE observe_d3d_dispatch(ID3D12GraphicsCommandList *list, UINT x, UINT y, UINT z)
{
    if (d3d_label) {
        ++counts[DISPATCH];
    }
    original->Dispatch(list, x, y, z);
}

static void STDMETHODCALLTYPE observe_d3d_copy(ID3D12GraphicsCommandList *list, ID3D12Resource *dst,
    UINT64 dst_offset, ID3D12Resource *src, UINT64 src_offset, UINT64 size)
{
    if (d3d_label) {
        ++counts[COPY];
    }
    original->CopyBufferRegion(list, dst, dst_offset, src, src_offset, size);
}

static HRESULT STDMETHODCALLTYPE observe_d3d_close(ID3D12GraphicsCommandList *list)
{
    if (d3d_label) {
        ++counts[UNCLOSED];
    }
    list->lpVtbl = original;
    active_list = NULL;
    d3d_label = false;
    HRESULT result = original->Close(list);
    if (FAILED(result)) {
        ++mismatch;
    }
    return result;
}

static SDL_GPUCommandBuffer *observe_d3d_acquire(SDL_GPURenderer *driver)
{
    SDL_GPUCommandBuffer *command = acquire(driver);
    if (!command) {
        return NULL;
    }
    ID3D12GraphicsCommandList *list = ((D3D12CommandBuffer *)command)->graphicsCommandList;
    if (!list || active_list) {
        ++mismatch;
        return command;
    }
    active_list = list;
    original = list->lpVtbl;
    observed = *original;
    observed.BeginEvent = observe_d3d_begin;
    observed.EndEvent = observe_d3d_end;
    observed.DrawInstanced = observe_d3d_draw;
    observed.Dispatch = observe_d3d_dispatch;
    observed.CopyBufferRegion = observe_d3d_copy;
    observed.Close = observe_d3d_close;
    list->lpVtbl = &observed;
    return command;
}

static bool observe_d3d_cancel(SDL_GPUCommandBuffer *command)
{
    ++counts[CANCEL];
    d3d_device->AcquireCommandBuffer = acquire;
    d3d_device->Cancel = cancel;
    d3d_device = NULL;
    bool result = cancel(command);
    if (!result) {
        ++mismatch;
    }
    return result;
}

E2D_TEST_EXPORT int e2d_test_labels_expect_d3d12(const wchar_t *name, uint32_t rgb)
{
    if (!d3d_device || !name || wcslen(name) >= SDL_arraysize(expected_wide)) {
        return 0;
    }
    wcscpy_s(expected_wide, SDL_arraysize(expected_wide), name);
    expected_rgb = rgb;
    return 1;
}
#endif

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

E2D_TEST_EXPORT int e2d_test_labels_open(SDL_GPUDevice *device, int direct3d)
{
    if (!device || renderer) return 0;
    mismatch = 0;
    memset(counts, 0, sizeof(counts));
#if defined(SDL_PLATFORM_WIN32)
    if (d3d_device) {
        return 0;
    }
    if (direct3d && strcmp(device->backend, "direct3d12") == 0) {
        d3d_device = device;
        acquire = device->AcquireCommandBuffer;
        cancel = device->Cancel;
        device->AcquireCommandBuffer = observe_d3d_acquire;
        device->Cancel = observe_d3d_cancel;
        return 1;
    }
#endif
    if (direct3d || strcmp(device->backend, "vulkan") != 0) {
        return 0;
    }
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
#if defined(SDL_PLATFORM_WIN32)
    if (d3d_device) {
        d3d_device->AcquireCommandBuffer = acquire;
        d3d_device->Cancel = cancel;
        if (active_list) {
            active_list->lpVtbl = original;
            active_list = NULL;
            d3d_label = false;
        }
        d3d_device = NULL;
        return;
    }
#endif
    if (!renderer) return;
    renderer->vkCmdBeginDebugUtilsLabelEXT = begin_label;
    renderer->vkCmdEndDebugUtilsLabelEXT = end_label;
    renderer->vkCmdDraw = draw;
    renderer->vkCmdDispatch = dispatch;
    renderer->vkCmdCopyBuffer = copy_buffer;
    renderer->vkEndCommandBuffer = end_commands;
    renderer = NULL;
}
