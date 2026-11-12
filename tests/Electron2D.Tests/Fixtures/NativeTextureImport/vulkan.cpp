#define WIN32_LEAN_AND_MEAN
#define VK_NO_PROTOTYPES
#include <windows.h>
#include <vulkan/vulkan.h>

struct Context {
    VkInstance instance;
    VkPhysicalDevice physical;
    VkDevice device;
    VkQueue queue;
    uint32_t family;
};

struct Image {
    VkImage image;
    VkDeviceMemory memory;
};

static PFN_vkGetInstanceProcAddr InstanceProc()
{
    const auto loader = GetModuleHandleW(L"vulkan-1.dll");
    return loader ? reinterpret_cast<PFN_vkGetInstanceProcAddr>(GetProcAddress(loader, "vkGetInstanceProcAddr")) : nullptr;
}

static PFN_vkGetDeviceProcAddr DeviceProc(const Context& context)
{
    const auto instance = InstanceProc();
    return instance ? reinterpret_cast<PFN_vkGetDeviceProcAddr>(instance(context.instance, "vkGetDeviceProcAddr")) : nullptr;
}

#define DEVICE_PROC(name) const auto name = reinterpret_cast<PFN_##name>(deviceProc(context.device, #name))

extern "C" __declspec(dllexport) void __cdecl DestroyImage(const Context& context, Image& image)
{
    const auto deviceProc = DeviceProc(context);
    DEVICE_PROC(vkDestroyImage);
    DEVICE_PROC(vkFreeMemory);
    if (image.image) vkDestroyImage(context.device, image.image, nullptr);
    if (image.memory) vkFreeMemory(context.device, image.memory, nullptr);
    image = {};
}

extern "C" __declspec(dllexport) VkResult __cdecl ClearImage(const Context& context, const Image& image,
    uint32_t layers, uint32_t levels, uint32_t phase, VkImageLayout oldLayout)
{
    const auto deviceProc = DeviceProc(context);
    if (!deviceProc) return VK_ERROR_INITIALIZATION_FAILED;
    DEVICE_PROC(vkCreateCommandPool);
    DEVICE_PROC(vkDestroyCommandPool);
    DEVICE_PROC(vkAllocateCommandBuffers);
    DEVICE_PROC(vkBeginCommandBuffer);
    DEVICE_PROC(vkEndCommandBuffer);
    DEVICE_PROC(vkCmdPipelineBarrier);
    DEVICE_PROC(vkCmdClearColorImage);
    DEVICE_PROC(vkCreateFence);
    DEVICE_PROC(vkDestroyFence);
    DEVICE_PROC(vkQueueSubmit);
    DEVICE_PROC(vkWaitForFences);
    VkCommandPoolCreateInfo poolInfo{ VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO };
    poolInfo.queueFamilyIndex = context.family;
    VkCommandPool pool{};
    auto result = vkCreateCommandPool(context.device, &poolInfo, nullptr, &pool);
    if (result != VK_SUCCESS) return result;
    VkCommandBufferAllocateInfo allocate{ VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO };
    allocate.commandPool = pool;
    allocate.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
    allocate.commandBufferCount = 1;
    VkCommandBuffer command{};
    VkCommandBufferBeginInfo begin{ VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO };
    begin.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;
    if ((result = vkAllocateCommandBuffers(context.device, &allocate, &command)) == VK_SUCCESS &&
        (result = vkBeginCommandBuffer(command, &begin)) == VK_SUCCESS)
    {
        VkImageMemoryBarrier barrier{ VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER };
        barrier.srcAccessMask = oldLayout == VK_IMAGE_LAYOUT_UNDEFINED ? 0 : VK_ACCESS_SHADER_READ_BIT;
        barrier.dstAccessMask = VK_ACCESS_TRANSFER_WRITE_BIT;
        barrier.oldLayout = oldLayout;
        barrier.newLayout = VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
        barrier.srcQueueFamilyIndex = barrier.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
        barrier.image = image.image;
        barrier.subresourceRange = { VK_IMAGE_ASPECT_COLOR_BIT, 0, levels, 0, layers };
        vkCmdPipelineBarrier(command, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, VK_PIPELINE_STAGE_TRANSFER_BIT,
            0, 0, nullptr, 0, nullptr, 1, &barrier);
        const VkClearColorValue palette[] = { {{1, 0, 0, 1}}, {{0, 1, 0, 1}}, {{0, 0, 1, 1}}, {{1, 1, 0, 1}} };
        for (uint32_t layer = 0; layer < layers; ++layer)
            for (uint32_t mip = 0; mip < levels; ++mip)
            {
                const VkImageSubresourceRange range{ VK_IMAGE_ASPECT_COLOR_BIT, mip, 1, layer, 1 };
                vkCmdClearColorImage(command, image.image, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                    &palette[(phase + 2 * layer + mip) % 4], 1, &range);
            }
        barrier.srcAccessMask = VK_ACCESS_TRANSFER_WRITE_BIT;
        barrier.dstAccessMask = VK_ACCESS_SHADER_READ_BIT;
        barrier.oldLayout = VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
        barrier.newLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
        vkCmdPipelineBarrier(command, VK_PIPELINE_STAGE_TRANSFER_BIT, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
            0, 0, nullptr, 0, nullptr, 1, &barrier);
        result = vkEndCommandBuffer(command);
    }
    VkFence fence{};
    const VkFenceCreateInfo fenceInfo{ VK_STRUCTURE_TYPE_FENCE_CREATE_INFO };
    if (result == VK_SUCCESS && (result = vkCreateFence(context.device, &fenceInfo, nullptr, &fence)) == VK_SUCCESS)
    {
        VkSubmitInfo submit{ VK_STRUCTURE_TYPE_SUBMIT_INFO };
        submit.commandBufferCount = 1;
        submit.pCommandBuffers = &command;
        if ((result = vkQueueSubmit(context.queue, 1, &submit, fence)) == VK_SUCCESS)
            result = vkWaitForFences(context.device, 1, &fence, VK_TRUE, UINT64_MAX);
        vkDestroyFence(context.device, fence, nullptr);
    }
    vkDestroyCommandPool(context.device, pool, nullptr);
    return result;
}

extern "C" __declspec(dllexport) VkResult __cdecl CreateImage(const Context& context, uint32_t width,
    uint32_t height, uint32_t layers, uint32_t levels, Image& image)
{
    image = {};
    const auto instance = InstanceProc();
    const auto deviceProc = DeviceProc(context);
    if (!instance || !deviceProc) return VK_ERROR_INITIALIZATION_FAILED;
    const auto memoryProperties = reinterpret_cast<PFN_vkGetPhysicalDeviceMemoryProperties>(
        instance(context.instance, "vkGetPhysicalDeviceMemoryProperties"));
    DEVICE_PROC(vkCreateImage);
    DEVICE_PROC(vkGetImageMemoryRequirements);
    DEVICE_PROC(vkAllocateMemory);
    DEVICE_PROC(vkBindImageMemory);
    VkImageCreateInfo info{ VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO };
    info.imageType = VK_IMAGE_TYPE_2D;
    info.format = VK_FORMAT_R8G8B8A8_UNORM;
    info.extent = { width, height, 1 };
    info.mipLevels = levels;
    info.arrayLayers = layers;
    info.samples = VK_SAMPLE_COUNT_1_BIT;
    info.tiling = VK_IMAGE_TILING_OPTIMAL;
    info.usage = VK_IMAGE_USAGE_SAMPLED_BIT | VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT;
    info.sharingMode = VK_SHARING_MODE_EXCLUSIVE;
    auto result = vkCreateImage(context.device, &info, nullptr, &image.image);
    if (result != VK_SUCCESS) return result;
    VkMemoryRequirements requirements{};
    vkGetImageMemoryRequirements(context.device, image.image, &requirements);
    VkPhysicalDeviceMemoryProperties properties{};
    memoryProperties(context.physical, &properties);
    uint32_t index = 0;
    while (index < properties.memoryTypeCount &&
        (!(requirements.memoryTypeBits & (1u << index)) ||
         !(properties.memoryTypes[index].propertyFlags & VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT))) ++index;
    if (index == properties.memoryTypeCount) result = VK_ERROR_FEATURE_NOT_PRESENT;
    else
    {
        VkMemoryAllocateInfo allocate{ VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO };
        allocate.allocationSize = requirements.size;
        allocate.memoryTypeIndex = index;
        if ((result = vkAllocateMemory(context.device, &allocate, nullptr, &image.memory)) == VK_SUCCESS &&
            (result = vkBindImageMemory(context.device, image.image, image.memory, 0)) == VK_SUCCESS)
            result = ClearImage(context, image, layers, levels, 0, VK_IMAGE_LAYOUT_UNDEFINED);
    }
    if (result != VK_SUCCESS) DestroyImage(context, image);
    return result;
}
