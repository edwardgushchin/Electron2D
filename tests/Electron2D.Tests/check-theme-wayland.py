#!/usr/bin/python3
"""Exercise native Wayland theme queries against a private Settings portal."""

import os
from pathlib import Path
import queue
import subprocess
import sys
import threading


if "--in-session" not in sys.argv:
    result = subprocess.run(["dbus-run-session", "--", sys.executable, __file__, "--in-session"],
                            capture_output=True, text=True)
    print(result.stdout, end="")
    if result.returncode:
        print(result.stderr, end="", file=sys.stderr)
    raise SystemExit(result.returncode)

import dbus
import dbus.mainloop.glib
import dbus.service
from gi.repository import GLib


PORTAL_PATH = "/org/freedesktop/portal/desktop"
PORTAL_NAME = "org.freedesktop.portal.Desktop"
PROJECT = Path(__file__).resolve().parent / "Electron2D.Tests.csproj"


dbus.mainloop.glib.DBusGMainLoop(set_as_default=True)
bus = dbus.SessionBus()
name = dbus.service.BusName(PORTAL_NAME, bus=bus, do_not_queue=True)


class Settings(dbus.service.Object):
    version = 1
    scheme = 0

    def __init__(self):
        super().__init__(bus, PORTAL_PATH)

    @dbus.service.method("org.freedesktop.DBus.Properties", in_signature="ss", out_signature="v")
    def Get(self, interface, property_name):
        assert (interface, property_name) == ("org.freedesktop.portal.Settings", "version")
        return dbus.UInt32(self.version, variant_level=1)

    @dbus.service.method("org.freedesktop.portal.Settings", in_signature="ss", out_signature="v")
    def Read(self, namespace, key):
        assert (namespace, key) == ("org.freedesktop.appearance", "color-scheme")
        return dbus.UInt32(self.scheme, variant_level=2)

    @dbus.service.signal("org.freedesktop.portal.Settings", signature="ssv")
    def SettingChanged(self, namespace, key, value):
        pass

    @dbus.service.method("org.electron2d.ThemeTest", in_signature="u", out_signature="")
    def Update(self, scheme):
        self.scheme = int(scheme)
        self.SettingChanged("org.freedesktop.appearance", "color-scheme", dbus.UInt32(scheme))


settings = Settings()
loop = GLib.MainLoop()
thread = threading.Thread(target=loop.run, daemon=True)
thread.start()


def environment(version, scheme, signals=False):
    values = os.environ.copy()
    values.pop("LD_LIBRARY_PATH", None)
    values.update(
        SDL_VIDEODRIVER="wayland",
        GDK_BACKEND="wayland",
        NO_AT_BRIDGE="1",
        GIO_USE_VFS="local",
        ELECTRON2D_TEST_DISPLAY_THEME_PORTAL="1",
        ELECTRON2D_TEST_THEME_VERSION=str(version),
        ELECTRON2D_TEST_THEME_SCHEME=str(scheme),
        ELECTRON2D_TEST_THEME_SIGNALS="1" if signals else "0",
    )
    return values


command = ["dotnet", "run", "--project", str(PROJECT), "-c", "Release", "--no-build", "--no-restore"]
try:
    for version, scheme in ((0, 1), (1, 0)):
        settings.version, settings.scheme = version, scheme
        result = subprocess.run(command, env=environment(version, scheme), capture_output=True, text=True, timeout=15)
        if result.returncode or result.stdout.strip() != f"PROBE_OK:{version}:{scheme}":
            raise RuntimeError(f"Theme probe {version}/{scheme} failed:\n{result.stdout}{result.stderr}")

    settings.version, settings.scheme = 1, 1
    child = subprocess.Popen(command, env=environment(1, 1, signals=True), stdin=subprocess.PIPE,
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, bufsize=1)
    lines = queue.Queue()

    def collect_lines():
        for line in child.stdout:
            lines.put(line.strip())

    reader = threading.Thread(target=collect_lines, daemon=True)
    reader.start()

    def read(expected):
        try:
            actual = lines.get(timeout=10)
        except queue.Empty as error:
            raise RuntimeError(f"Theme child timed out waiting for {expected}; exit status: {child.poll()}") from error
        if actual != expected:
            raise RuntimeError(f"Expected {expected}, got {actual}; child exit status: {child.poll()}")

    try:
        for scheme in (2, 0, 1):
            read(f"READY:{scheme}")
            subprocess.run(["gdbus", "call", "--session", "--dest", PORTAL_NAME,
                            "--object-path", PORTAL_PATH, "--method", "org.electron2d.ThemeTest.Update",
                            str(scheme)], check=True, capture_output=True, text=True, timeout=5)
            child.stdin.write(f"GO:{scheme}\n")
            child.stdin.flush()
            read(f"DONE:{scheme}")
        if child.wait(timeout=5):
            raise RuntimeError(f"Theme child failed: {child.stderr.read()}")
    finally:
        if child.poll() is None:
            child.kill()
            child.wait()
    print("Wayland private-portal theme checks passed: version zero, unset, and three native changes.")
finally:
    loop.quit()
    thread.join(timeout=1)
