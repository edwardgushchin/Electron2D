#include <X11/XKBlib.h>
#include <poll.h>
#include <stdio.h>

int main(void)
{
    Display *display = XOpenDisplay(NULL);
    int event_base, opcode, error, major = XkbMajorVersion, minor = XkbMinorVersion;
    if (!display ||
        !XkbQueryExtension(display, &opcode, &event_base, &error, &major, &minor) ||
        !XkbSelectEvents(display, XkbUseCoreKbd, XkbBellNotifyMask, XkbBellNotifyMask))
        return 2;

    XSync(display, False);
    puts("READY");
    fflush(stdout);
    struct pollfd connection = { .fd = ConnectionNumber(display), .events = POLLIN };
    int bells = 0;
    for (;;)
    {
        if (XPending(display) == 0 && poll(&connection, 1, bells >= 3 ? 250 : 5000) <= 0)
            break;

        XkbEvent event;
        XNextEvent(display, &event.core);
        if (event.type == event_base && event.any.xkb_type == XkbBellNotify)
        {
            if (event.bell.event_only)
                return 3;
            bells++;
        }
    }

    printf("XKB bell notifications: %d\n", bells);
    XCloseDisplay(display);
    return bells == 3 ? 0 : 1;
}
