"""Exercise native window scale changes in an owned, isolated Mutter session."""
import argparse
import json
import os
from pathlib import Path
import resource
import signal
import subprocess
import sys
import time


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("consumer", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--gpu", action="store_true")
    parser.add_argument("--threaded", action="store_true")
    parser.add_argument("--compatibility", action="store_true")
    parser.add_argument("--in-session", action="store_true", help=argparse.SUPPRESS)
    args = parser.parse_args()
    if not args.in_session:
        os.execvp("dbus-run-session", ["dbus-run-session", "--", sys.executable, __file__, *sys.argv[1:], "--in-session"])
    from gi.repository import Gio, GLib

    consumer = args.consumer.resolve(strict=True)
    output = args.output.resolve()
    output.mkdir()
    control = output / "control"
    control.mkdir()
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    bus = Gio.bus_get_sync(Gio.BusType.SESSION, None)
    occupied = bus.call_sync("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner",
                             GLib.Variant("(s)", ("org.gnome.Mutter.DisplayConfig",)), None, Gio.DBusCallFlags.NONE, 5000, None).unpack()[0]
    if occupied:
        raise RuntimeError("Native DPI tests require their own empty session bus")
    socket = "electron2d-dpi-" + str(os.getpid())
    environment = dict(os.environ, __GL_SHADER_DISK_CACHE="0", MESA_SHADER_CACHE_DISABLE="true", GSETTINGS_BACKEND="memory", GIO_USE_VFS="local", NO_AT_BRIDGE="1", GTK_USE_PORTAL="0",
                       XDG_CONFIG_HOME=str(output / "config"), XDG_CACHE_HOME=str(output / "cache"), XDG_DATA_HOME=str(output / "data"))
    command = ["mutter", "--headless", "--wayland", "--no-x11", "--virtual-monitor=1280x960", "--wayland-display=" + socket]
    applied = []
    engine = None
    started = time.monotonic()

    def call(method, parameters=None):
        return bus.call_sync("org.gnome.Mutter.DisplayConfig", "/org/gnome/Mutter/DisplayConfig", "org.gnome.Mutter.DisplayConfig",
                             method, parameters, None, Gio.DBusCallFlags.NONE, 5000, None).unpack()

    with (output / "compositor.log").open("w") as compositor_log, subprocess.Popen(
            command, env=environment, stdout=compositor_log, stderr=subprocess.STDOUT, start_new_session=True) as compositor:
        (output / "compositor-child.json").write_text(json.dumps({"pid": compositor.pid, "command": command, "socket": socket}, indent=2) + "\n")
        try:
            deadline = time.monotonic() + 20
            while True:
                if compositor.poll() is not None:
                    raise RuntimeError("Owned compositor exited before readiness")
                try:
                    initial = call("GetCurrentState")
                    break
                except GLib.Error:
                    if time.monotonic() >= deadline:
                        raise TimeoutError("Compositor readiness deadline")
                    time.sleep(.05)
            (output / "initial-state.json").write_text(json.dumps(initial, indent=2) + "\n")

            def apply(scale):
                owner = bus.call_sync("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "GetConnectionUnixProcessID",
                                      GLib.Variant("(s)", ("org.gnome.Mutter.DisplayConfig",)), None, Gio.DBusCallFlags.NONE, 5000, None).unpack()[0]
                if owner != compositor.pid:
                    raise RuntimeError("Refusing to configure a compositor outside this test")
                state = call("GetCurrentState")
                monitor = state[1][0]
                mode = next(mode for mode in monitor[1] if mode[6].get("is-current"))
                if not any(abs(scale - supported) < .0001 for supported in mode[5]):
                    raise RuntimeError(f"Compositor does not support requested scale {scale}")
                logical = [(0, 0, scale, 0, True, [(monitor[0][0], mode[0], {})])]
                call("ApplyMonitorsConfig", GLib.Variant("(uua(iiduba(ssa{sv}))a{sv})", (state[0], 1, logical, {})))
                applied.append({"scale": scale, "ownerPID": owner, "state": call("GetCurrentState"), "seconds": time.monotonic() - started})
                (output / "applied.json").write_text(json.dumps(applied, indent=2) + "\n")

            environment.update(WAYLAND_DISPLAY=socket, SDL_VIDEODRIVER="wayland", GDK_BACKEND="wayland",
                               DBUS_SESSION_BUS_ADDRESS="unix:path=" + str(output / "no-engine-session-bus"), ELECTRON2D_DPI_CONTROL_DIR=str(control))
            command = (["dotnet", str(consumer)] if consumer.suffix == ".dll" else [str(consumer)]) + ["--native-dpi", "--gpu" if args.gpu else "--cpu"]
            command += (["--threaded"] if args.threaded else []) + (["--compatibility"] if args.compatibility else [])
            with (output / "engine.log").open("w") as log:
                engine = subprocess.Popen(command, env=environment, stdout=log, stderr=subprocess.STDOUT)
                (output / "engine-child.json").write_text(json.dumps({"pid": engine.pid, "command": command}, indent=2) + "\n")
                phase = 0
                while engine.poll() is None:
                    if time.monotonic() - started >= 170:
                        raise TimeoutError("Native DPI process deadline")
                    if compositor.poll() is not None:
                        raise RuntimeError("Owned compositor exited during native DPI check")
                    if phase < 2 and (control / ("ready-" + str(phase))).exists():
                        apply(1.25 if phase == 0 else 1.0)
                        phase += 1
                    time.sleep(.02)
                if engine.wait() != 0 or phase != 2:
                    raise RuntimeError(f"Native DPI check failed: exit={engine.returncode}, transitions={phase}")
        finally:
            if engine is not None and engine.poll() is None:
                engine.terminate()
                try:
                    engine.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    engine.kill()
                    engine.wait(timeout=10)
            if compositor.poll() is None:
                os.killpg(compositor.pid, signal.SIGTERM)
            try:
                compositor.wait(timeout=10)
            except subprocess.TimeoutExpired:
                os.killpg(compositor.pid, signal.SIGKILL)
                compositor.wait(timeout=10)
            (output / "controller.json").write_text(json.dumps({"engineExit": None if engine is None else engine.returncode,
                "compositorExit": compositor.returncode, "applied": applied, "seconds": time.monotonic() - started}, indent=2) + "\n")
    print("Native DPI compositor round trip passed.", flush=True)


if __name__ == "__main__":
    main()
