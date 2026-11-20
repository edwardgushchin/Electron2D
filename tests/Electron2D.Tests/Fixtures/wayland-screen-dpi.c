#include <stdio.h>
#include <string.h>
#include <wayland-server.h>
#include "xdg-shell-server.h"

struct output {
    const char *name;
    int x, width, height, width_mm, height_mm, scale, transform;
};

static void destroy_resource(struct wl_client *client, struct wl_resource *resource)
{
    (void)client;
    wl_resource_destroy(resource);
}

static const struct wl_output_interface output_requests = {
    .release = destroy_resource,
};

static void bind_output(struct wl_client *client, void *data, uint32_t version, uint32_t id)
{
    const struct output *output = data;
    struct wl_resource *resource = wl_resource_create(client, &wl_output_interface, version, id);
    if (!resource) {
        wl_client_post_no_memory(client);
        return;
    }

    wl_resource_set_implementation(resource, &output_requests, NULL, NULL);
    wl_output_send_geometry(resource, output->x, 0, output->width_mm, output->height_mm,
                            WL_OUTPUT_SUBPIXEL_UNKNOWN, "Fixture", "Panel", output->transform);
    wl_output_send_mode(resource, WL_OUTPUT_MODE_CURRENT | WL_OUTPUT_MODE_PREFERRED,
                        output->width, output->height, 60000);
    if (version >= WL_OUTPUT_SCALE_SINCE_VERSION)
        wl_output_send_scale(resource, output->scale);
    if (version >= WL_OUTPUT_NAME_SINCE_VERSION) {
        wl_output_send_name(resource, output->name);
        wl_output_send_description(resource, "Fixture Panel");
    }
    if (version >= WL_OUTPUT_DONE_SINCE_VERSION)
        wl_output_send_done(resource);
}

static void set_region(struct wl_client *client, struct wl_resource *surface,
                       struct wl_resource *region)
{
    (void)client;
    (void)surface;
    (void)region;
}

static void set_buffer_scale(struct wl_client *client, struct wl_resource *surface, int32_t scale)
{
    (void)client;
    (void)surface;
    (void)scale;
}

static const struct wl_surface_interface surface_requests = {
    .destroy = destroy_resource,
    .set_opaque_region = set_region,
    .set_input_region = set_region,
    .set_buffer_scale = set_buffer_scale,
};

static void change_region(struct wl_client *client, struct wl_resource *resource,
                          int32_t x, int32_t y, int32_t width, int32_t height)
{
    (void)client;
    (void)resource;
    (void)x;
    (void)y;
    (void)width;
    (void)height;
}

static const struct wl_region_interface region_requests = {
    .destroy = destroy_resource,
    .add = change_region,
    .subtract = change_region,
};

static void create_region(struct wl_client *client, struct wl_resource *compositor, uint32_t id)
{
    (void)compositor;
    struct wl_resource *region = wl_resource_create(client, &wl_region_interface, 1, id);
    if (!region) {
        wl_client_post_no_memory(client);
        return;
    }

    wl_resource_set_implementation(region, &region_requests, NULL, NULL);
}

static void create_surface(struct wl_client *client, struct wl_resource *compositor, uint32_t id)
{
    struct wl_resource *surface = wl_resource_create(client, &wl_surface_interface,
                                                     wl_resource_get_version(compositor), id);
    if (!surface) {
        wl_client_post_no_memory(client);
        return;
    }

    wl_resource_set_implementation(surface, &surface_requests, NULL, NULL);
}

static const struct wl_compositor_interface compositor_requests = {
    .create_surface = create_surface,
    .create_region = create_region,
};

static const struct xdg_wm_base_interface shell_requests = {
    .destroy = destroy_resource,
};

static void bind_global(struct wl_client *client, void *data, uint32_t version, uint32_t id)
{
    const struct wl_interface *interface = data;
    struct wl_resource *resource = wl_resource_create(client, interface, version, id);
    if (!resource) {
        wl_client_post_no_memory(client);
        return;
    }

    const void *requests = &shell_requests;
    if (interface == &wl_compositor_interface)
        requests = &compositor_requests;
    wl_resource_set_implementation(resource, requests, NULL, NULL);
}

int main(int argc, char **argv)
{
    if (argc != 3 || (strcmp(argv[2], "physical") != 0 && strcmp(argv[2], "missing") != 0)) {
        fprintf(stderr, "Usage: %s <socket> <physical|missing>\n", argv[0]);
        return 2;
    }

    struct output first = {"DPI-1", 0, 1920, 1080, 400, 300, 1, WL_OUTPUT_TRANSFORM_NORMAL};
    struct output second = {"DPI-2", 1920, 2560, 1440, 520, 320, 2, WL_OUTPUT_TRANSFORM_90};
    if (strcmp(argv[2], "missing") == 0) {
        first.width_mm = 0;
        first.height_mm = 0;
        second.height_mm = 0;
    }

    /* The fixture only supports display discovery and hidden surfaces. */
    struct wl_display *display = wl_display_create();
    if (!display)
        return 1;

    if (!wl_global_create(display, &wl_compositor_interface, 4, (void *)&wl_compositor_interface, bind_global) ||
        !wl_global_create(display, &xdg_wm_base_interface, 1, (void *)&xdg_wm_base_interface, bind_global) ||
        !wl_global_create(display, &wl_output_interface, 4, &first, bind_output) ||
        !wl_global_create(display, &wl_output_interface, 4, &second, bind_output) ||
        wl_display_add_socket(display, argv[1]) != 0) {
        fprintf(stderr, "Could not publish the private Wayland globals or socket.\n");
        wl_display_destroy(display);
        return 1;
    }

    printf("Wayland DPI fixture v1 (%s)\n", argv[2]);
    fflush(stdout);
    wl_display_run(display);
    wl_display_destroy_clients(display);
    wl_display_destroy(display);
    return 0;
}
