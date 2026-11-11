#include <libweston/libweston.h>
#include <stdio.h>
#include <wayland-server-core.h>
#include "system-bell-server.h"

static void destroy(struct wl_client *client, struct wl_resource *resource)
{
    (void)client;
    fputs("bell destroy\n", stderr);
    wl_resource_destroy(resource);
}

static void ring(struct wl_client *client, struct wl_resource *resource, struct wl_resource *surface)
{
    (void)client;
    (void)resource;
    fprintf(stderr, "bell ring surface=%s\n", surface ? "window" : "null");
}

static const struct xdg_system_bell_v1_interface requests = {
    .destroy = destroy,
    .ring = ring,
};

static void bind_bell(struct wl_client *client, void *data, uint32_t version, uint32_t id)
{
    (void)data;
    struct wl_resource *resource = wl_resource_create(client, &xdg_system_bell_v1_interface, version, id);
    if (!resource)
    {
        wl_client_post_no_memory(client);
        return;
    }
    fputs("bell bind\n", stderr);
    wl_resource_set_implementation(resource, &requests, NULL, NULL);
}

WL_EXPORT int wet_module_init(struct weston_compositor *compositor, int *argc, char *argv[])
{
    (void)argc;
    (void)argv;
    return wl_global_create(compositor->wl_display, &xdg_system_bell_v1_interface, 1, NULL, bind_bell) ? 0 : -1;
}
