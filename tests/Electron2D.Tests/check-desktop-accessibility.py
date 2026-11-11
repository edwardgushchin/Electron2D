#!/usr/bin/env python3
"""Run under dbus-run-session with a real SDL X11 or Wayland display."""

import os
import queue
import subprocess
import sys
import threading

import dbus
import dbus.mainloop.glib
import dbus.service
from gi.repository import GLib


PROPERTIES = "org.freedesktop.DBus.Properties"
SETTINGS = "org.freedesktop.portal.Settings"
PORTAL = "org.freedesktop.portal.Desktop"
STATUS = "org.a11y.Status"
A11Y = "org.a11y.Bus"


class SettingsService(dbus.service.Object):
    version = 2
    values = {}

    @dbus.service.method(PROPERTIES, in_signature="ss", out_signature="v")
    def Get(self, interface, key):
        assert (interface, key) == (SETTINGS, "version")
        return dbus.UInt32(self.version)

    def read(self, namespace, key, depth):
        if namespace != "org.freedesktop.appearance" or key not in self.values:
            raise dbus.exceptions.DBusException("Unknown preference", name=SETTINGS + ".NotFound")
        value = self.values[key]
        if isinstance(value, str):
            return dbus.String(value, variant_level=depth)
        return dbus.UInt32(value, variant_level=depth)

    @dbus.service.method(SETTINGS, in_signature="ss", out_signature="v")
    def ReadOne(self, namespace, key):
        assert self.version >= 2
        return self.read(namespace, key, 1)

    @dbus.service.method(SETTINGS, in_signature="ss", out_signature="v")
    def Read(self, namespace, key):
        return self.read(namespace, key, 2)


class AccessibilityService(dbus.service.Object):
    reader = None
    enabled = True

    @dbus.service.method(PROPERTIES, in_signature="ss", out_signature="v")
    def Get(self, interface, key):
        assert interface == STATUS
        if key == "IsEnabled":
            if self.enabled is None:
                raise dbus.exceptions.DBusException("Missing accessibility status", name=PROPERTIES + ".NotFound")
            return dbus.Boolean(self.enabled)
        assert key == "ScreenReaderEnabled"
        if self.reader is None:
            raise dbus.exceptions.DBusException("Missing screen-reader status", name=PROPERTIES + ".NotFound")
        if isinstance(self.reader, str):
            return dbus.String(self.reader)
        if isinstance(self.reader, tuple):
            return dbus.Struct([dbus.Boolean(self.reader[0])], signature="b")
        return dbus.Boolean(self.reader)


def run(command):
    assert os.environ.get("SDL_VIDEODRIVER") in {"x11", "wayland"}
    dbus.mainloop.glib.DBusGMainLoop(set_as_default=True)
    bus = dbus.SessionBus()
    portal = dbus.service.BusName(PORTAL, bus=bus, do_not_queue=True)
    a11y = dbus.service.BusName(A11Y, bus=bus, do_not_queue=True)
    settings = SettingsService(portal, "/org/freedesktop/portal/desktop")
    status = AccessibilityService(a11y, "/org/a11y/bus")
    loop = GLib.MainLoop()
    service = threading.Thread(target=loop.run, daemon=True)
    service.start()
    env = dict(os.environ, ELECTRON2D_TEST_DESKTOP_ACCESSIBILITY="linux")
    output = queue.Queue()
    try:
        with subprocess.Popen(command, env=env, stdin=subprocess.PIPE, stdout=subprocess.PIPE, text=True) as child:
            def collect_output():
                for line in child.stdout:
                    output.put(line.rstrip())

            reader = threading.Thread(target=collect_output, daemon=True)
            reader.start()
            try:
                for version, enabled, reader_flag, values, expected in (
                    (2, True, None, {}, "-1 -1 -1 -1"),
                    (2, True, False, {"contrast": 0, "reduced-motion": 1}, "0 0 1 -1"),
                    (2, True, True, {"contrast": 1, "reduced-motion": 0}, "1 1 0 -1"),
                    (2, True, "bad", {"contrast": "bad", "reduced-motion": 2**32 - 1}, "-1 -1 0 -1"),
                    (1, True, (True,), {"contrast": 1, "reduced-motion": 1}, "1 1 1 -1"),
                    (2, False, True, {"contrast": 1}, "0 1 -1 -1"),
                    (0, None, True, {"contrast": 1, "reduced-motion": 1}, "-1 -1 -1 -1"),
                ):
                    settings.version, settings.values = version, values
                    status.enabled, status.reader = enabled, reader_flag
                    child.stdin.write(expected + "\n")
                    child.stdin.flush()
                    assert output.get(timeout=15) == "Preferences matched.", expected
                    print(f"Portal v{version}, preferences (reader, contrast, reduced motion, transparency): {expected}")
                child.stdin.close()
                print(output.get(timeout=10))
                if child.wait(timeout=10) != 0:
                    raise RuntimeError("Desktop accessibility consumer failed")
            finally:
                if child.poll() is None:
                    child.kill()
                    child.wait()
                reader.join(timeout=2)
    finally:
        loop.quit()
        service.join(timeout=2)


if __name__ == "__main__":
    run(sys.argv[1:])
