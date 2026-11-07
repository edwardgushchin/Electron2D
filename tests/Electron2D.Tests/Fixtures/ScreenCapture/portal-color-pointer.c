#define _DEFAULT_SOURCE

#include <linux/input-event-codes.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <unistd.h>
#include <wayland-client.h>
#include "virtual-pointer.h"

static struct wl_seat *seat;
static struct zwlr_virtual_pointer_manager_v1 *manager;

static void global(void *data, struct wl_registry *registry, uint32_t name, const char *interface, uint32_t version)
{
    if (strcmp(interface, wl_seat_interface.name) == 0 && !seat)
        seat = wl_registry_bind(registry, name, &wl_seat_interface, 1);
    if (strcmp(interface, zwlr_virtual_pointer_manager_v1_interface.name) == 0)
        manager = wl_registry_bind(registry, name, &zwlr_virtual_pointer_manager_v1_interface, 1);
}

static void removed(void *data, struct wl_registry *registry, uint32_t name) {}
static const struct wl_registry_listener registry_listener = { global, removed };

static uint32_t now(void)
{
    struct timespec time;
    clock_gettime(CLOCK_MONOTONIC, &time);
    return (uint32_t)((uint64_t)time.tv_sec * 1000 + time.tv_nsec / 1000000);
}

int main(int argc, char **argv)
{
    if (argc != 5)
        return 2;
    unsigned x = strtoul(argv[1], NULL, 10);
    unsigned y = strtoul(argv[2], NULL, 10);
    unsigned width = strtoul(argv[3], NULL, 10);
    unsigned height = strtoul(argv[4], NULL, 10);
    if (x >= width || y >= height)
        return 2;

    struct wl_display *display = wl_display_connect(NULL);
    if (!display)
        return 2;
    struct wl_registry *registry = wl_display_get_registry(display);
    wl_registry_add_listener(registry, &registry_listener, NULL);
    if (wl_display_roundtrip(display) < 0 || !seat || !manager)
        return 2;

    struct zwlr_virtual_pointer_v1 *pointer = zwlr_virtual_pointer_manager_v1_create_virtual_pointer(manager, seat);
    if (wl_display_roundtrip(display) < 0)
        return 2;
    // Wait for the selector to observe the new seat capability.
    usleep(150000);
    zwlr_virtual_pointer_v1_motion_absolute(pointer, now(), x, y, width, height);
    zwlr_virtual_pointer_v1_frame(pointer);
    if (wl_display_roundtrip(display) < 0)
        return 2;
    usleep(40000);
    zwlr_virtual_pointer_v1_button(pointer, now(), BTN_LEFT, WL_POINTER_BUTTON_STATE_PRESSED);
    zwlr_virtual_pointer_v1_frame(pointer);
    if (wl_display_roundtrip(display) < 0)
        return 2;
    usleep(40000);
    zwlr_virtual_pointer_v1_button(pointer, now(), BTN_LEFT, WL_POINTER_BUTTON_STATE_RELEASED);
    zwlr_virtual_pointer_v1_frame(pointer);
    if (wl_display_roundtrip(display) < 0)
        return 2;
    zwlr_virtual_pointer_v1_destroy(pointer);
    zwlr_virtual_pointer_manager_v1_destroy(manager);
    wl_seat_destroy(seat);
    wl_registry_destroy(registry);
    wl_display_disconnect(display);
    return 0;
}
