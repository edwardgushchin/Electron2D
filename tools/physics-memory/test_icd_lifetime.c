#define _GNU_SOURCE
#include <dlfcn.h>
#include <errno.h>
#include <limits.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <vulkan/vk_icd.h>

static void require(bool condition, const char *message)
{
    if (!condition) {
        fprintf(stderr, "%s\n", message);
        exit(EXIT_FAILURE);
    }
}

static void check_modules(const char *unmapped)
{
    FILE *maps = fopen("/proc/self/maps", "r");
    require(maps != NULL, "Cannot inspect native mappings.");
    char line[8192];
    while (fgets(line, sizeof line, maps)) {
        require(!strcasestr(line, "nvidia") && !strstr(line, "libSDL") && !strstr(line, "libcoreclr") &&
                    !strstr(line, "libvulkan.so"),
                "This control excludes hardware, SDL, CoreCLR and the Vulkan loader.");
        require(!unmapped || !strstr(line, unmapped), "The driver remains in procfs mappings.");
    }
    require(!ferror(maps), "Cannot finish reading native mappings.");
    require(fclose(maps) == 0, "Cannot close native mappings.");
}

static void close_driver(void *driver, const char *path)
{
    require(dlclose(driver) == 0, "Cannot close the driver.");
    void *remaining = dlopen(path, RTLD_NOW | RTLD_NOLOAD);
    if (remaining)
        dlclose(remaining);
    require(remaining == NULL, "The driver remains mapped after its last close.");
    check_modules(path);
}

static void enumerate(void *driver, char *name)
{
    PFN_vk_icdNegotiateLoaderICDInterfaceVersion negotiate =
        (PFN_vk_icdNegotiateLoaderICDInterfaceVersion)dlsym(driver, "vk_icdNegotiateLoaderICDInterfaceVersion");
    PFN_vk_icdGetInstanceProcAddr get = (PFN_vk_icdGetInstanceProcAddr)dlsym(driver, "vk_icdGetInstanceProcAddr");
    require(negotiate && get, "Missing driver interface.");
    uint32_t version = 5;
    require(negotiate(&version) == VK_SUCCESS && version == 5, "Driver interface 5 is required.");
    PFN_vkCreateInstance create = (PFN_vkCreateInstance)get(VK_NULL_HANDLE, "vkCreateInstance");
    require(create != NULL, "Missing instance creation.");
    VkApplicationInfo app = {.sType = VK_STRUCTURE_TYPE_APPLICATION_INFO, .apiVersion = VK_API_VERSION_1_0};
    VkInstanceCreateInfo info = {.sType = VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO, .pApplicationInfo = &app};
    VkInstance instance = VK_NULL_HANDLE;
    require(create(&info, NULL, &instance) == VK_SUCCESS && instance != VK_NULL_HANDLE, "Instance creation failed.");
    PFN_vkDestroyInstance destroy = (PFN_vkDestroyInstance)get(instance, "vkDestroyInstance");
    PFN_vkEnumeratePhysicalDevices devices =
        (PFN_vkEnumeratePhysicalDevices)get(instance, "vkEnumeratePhysicalDevices");
    PFN_vkGetPhysicalDeviceProperties properties =
        (PFN_vkGetPhysicalDeviceProperties)get(instance, "vkGetPhysicalDeviceProperties");
    require(destroy && devices && properties, "Missing instance functions.");
    uint32_t count = 0;
    require(devices(instance, &count, NULL) == VK_SUCCESS && count == 1, "Exactly one software device is required.");
    VkPhysicalDevice device;
    require(devices(instance, &count, &device) == VK_SUCCESS && count == 1, "Software-device enumeration failed.");
    VkPhysicalDeviceProperties value;
    properties(device, &value);
    require(value.deviceType == VK_PHYSICAL_DEVICE_TYPE_CPU && strstr(value.deviceName, "llvmpipe"),
            "The device must be lavapipe.");
    require(!strpbrk(value.deviceName, "\"\\\n\r"), "Unexpected device name.");
    strcpy(name, value.deviceName);
    check_modules(NULL);
    destroy(instance, NULL);
}

int main(int argc, char **argv)
{
    require(argc == 4, "Expected explicit libvulkan_lvp.so, load|reopen|retain and 1 through 32 cycles.");
    char path[PATH_MAX];
    require(realpath(argv[1], path) != NULL && strrchr(path, '/') &&
                strcmp(strrchr(path, '/') + 1, "libvulkan_lvp.so") == 0,
            "An existing explicit lavapipe library is required.");
    bool load = strcmp(argv[2], "load") == 0, retain = strcmp(argv[2], "retain") == 0;
    require(load || retain || strcmp(argv[2], "reopen") == 0, "Unknown lifetime mode.");
    char *end;
    errno = 0;
    long cycles = strtol(argv[3], &end, 10);
    require(!errno && end != argv[3] && !*end && cycles >= 1 && cycles <= 32, "Cycles must be 1 through 32.");
    check_modules(path);
    // Resolve the standard C++ runtime first so reverse relocations cannot pin the driver.
    require(dlsym(RTLD_DEFAULT, "__cxa_throw") != NULL, "Link libstdc++ before opening the ICD.");
    void *driver = NULL;
    char name[VK_MAX_PHYSICAL_DEVICE_NAME_SIZE] = "";
    for (long cycle = 0; cycle < cycles; cycle++) {
        if (!driver)
            driver = dlopen(path, RTLD_NOW | RTLD_LOCAL);
        require(driver != NULL, "Cannot load lavapipe.");
        if (!load)
            enumerate(driver, name);
        if (!retain) {
            close_driver(driver, path);
            driver = NULL;
        }
    }
    if (driver)
        close_driver(driver, path);
    check_modules(path);
    printf("{\"Mode\":\"%s\",\"Cycles\":%ld,\"Enumerations\":%ld,\"Device\":\"%s\",\"DriverUnmapped\":true}\n", argv[2],
           cycles, load ? 0 : cycles, name);
    return 0;
}
