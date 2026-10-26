"""Exercise native window scale changes in an owned, isolated Mutter session."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import resource
import shlex
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
    parser.add_argument("--devkit", action="store_true", help="Show the owned output in Mutter's nested Devkit viewer")
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
    capture_source = Path(__file__).resolve().parents[1] / "Electron2D.Tests/native_wayland_capture.c"
    capture_binary = output / "capture-drain"
    flags = shlex.split(subprocess.check_output(["pkg-config", "--cflags", "--libs", "libpipewire-0.3"], text=True, timeout=10))
    build = ["cc", "-std=c11", "-D_POSIX_C_SOURCE=200809L", "-O2", "-Wall", "-Wextra", "-Werror", str(capture_source), *flags, "-o", str(capture_binary)]
    with (output / "capture-build.log").open("w") as log:
        subprocess.run(build, stdout=log, stderr=subprocess.STDOUT, check=True, timeout=30)
    (output / "capture-build.json").write_text(json.dumps({"command": build,
        "sourceSHA256": hashlib.sha256(capture_source.read_bytes()).hexdigest(),
        "binarySHA256": hashlib.sha256(capture_binary.read_bytes()).hexdigest()}, indent=2) + "\n")
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    bus = Gio.bus_get_sync(Gio.BusType.SESSION, None)
    occupied = bus.call_sync("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner",
                             GLib.Variant("(s)", ("org.gnome.Mutter.DisplayConfig",)), None, Gio.DBusCallFlags.NONE, 5000, None).unpack()[0]
    if occupied:
        raise RuntimeError("Native DPI tests require their own empty session bus")
    socket = "electron2d-dpi-" + str(os.getpid())
    environment = dict(os.environ, __GL_SHADER_DISK_CACHE="0", MESA_SHADER_CACHE_DISABLE="true", GSETTINGS_BACKEND="memory", GIO_USE_VFS="local", NO_AT_BRIDGE="1", GTK_A11Y="none", GDK_DEBUG="no-portals", GTK_USE_PORTAL="0",
                       XDG_CONFIG_HOME=str(output / "config"), XDG_CACHE_HOME=str(output / "cache"), XDG_DATA_HOME=str(output / "data"))
    command = ["mutter", "--wayland", "--no-x11", "--wayland-display=" + socket]
    command += ["--devkit", "--devkit-args=--monitor-size=640x480"] if args.devkit else ["--headless", "--virtual-monitor=1280x960"]
    applied = []
    engine = None
    capture = None
    capture_session = None
    captures = []
    started = time.monotonic()

    def call(method, parameters=None):
        return bus.call_sync("org.gnome.Mutter.DisplayConfig", "/org/gnome/Mutter/DisplayConfig", "org.gnome.Mutter.DisplayConfig",
                             method, parameters, None, Gio.DBusCallFlags.NONE, 5000, None).unpack()

    def sc(path, interface, method, parameters=None):
        return bus.call_sync("org.gnome.Mutter.ScreenCast", path, interface, method, parameters, None, Gio.DBusCallFlags.NONE, 5000, None).unpack()

    def stop_capture():
        nonlocal capture, capture_session
        if capture is not None:
            if capture.poll() is None:
                capture.terminate()
                try:
                    capture.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    capture.kill()
                    capture.wait(timeout=3)
            marker = output / captures[-1]["marker"]
            captures[-1].update(exit=capture.returncode, frames=int(marker.read_text()) if marker.exists() else 0)
            capture = None
        if capture_session is not None:
            try:
                sc(capture_session, "org.gnome.Mutter.ScreenCast.Session", "Stop")
            except GLib.Error as error:
                if captures:
                    captures[-1]["stopError"] = str(error)
                else:
                    (output / "capture-stop-error.txt").write_text(str(error))
            capture_session = None


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
                    if not initial[1]:
                        if time.monotonic() >= deadline:
                            raise TimeoutError("Compositor output readiness deadline")
                        time.sleep(.05)
                        continue
                    break
                except GLib.Error:
                    if time.monotonic() >= deadline:
                        raise TimeoutError("Compositor readiness deadline")
                    time.sleep(.05)
            (output / "initial-state.json").write_text(json.dumps(initial, indent=2) + "\n")

            def configure_scale(scale):
                owner = bus.call_sync("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "GetConnectionUnixProcessID",
                                      GLib.Variant("(s)", ("org.gnome.Mutter.DisplayConfig",)), None, Gio.DBusCallFlags.NONE, 5000, None).unpack()[0]
                if owner != compositor.pid:
                    raise RuntimeError("Refusing to configure a compositor outside this test")
                state = call("GetCurrentState")
                if len(state[1]) != 1 or not state[1][0][0][0].startswith("Meta-"):
                    raise RuntimeError("Scale changes require exactly one owned virtual output")
                monitor = state[1][0]
                mode = next(mode for mode in monitor[1] if mode[6].get("is-current"))
                if not any(abs(scale - supported) < .0001 for supported in mode[5]):
                    raise RuntimeError(f"Compositor does not support requested scale {scale}")
                logical = [(0, 0, scale, 0, True, [(monitor[0][0], mode[0], {})])]
                call("ApplyMonitorsConfig", GLib.Variant("(uua(iiduba(ssa{sv}))a{sv})", (state[0], 1, logical, {})))
                committed = call("GetCurrentState")
                if len(committed[2]) != 1 or abs(committed[2][0][2] - scale) >= .0001:
                    raise RuntimeError("Compositor did not commit the requested scale")
                return {"scale": scale, "ownerPID": owner, "state": committed, "seconds": time.monotonic() - started}

            if args.devkit:
                # The viewer inherits its host's scale; the consumer requires a known initial scale.
                (output / "normalized-state.json").write_text(json.dumps(configure_scale(1.0), indent=2) + "\n")

            def start_capture():
                nonlocal capture, capture_session
                stop_capture()
                owner = bus.call_sync("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "GetConnectionUnixProcessID",
                                      GLib.Variant("(s)", ("org.gnome.Mutter.ScreenCast",)), None, Gio.DBusCallFlags.NONE, 5000, None).unpack()[0]
                if owner != compositor.pid:
                    raise RuntimeError("Capture must target the owned compositor")
                state = call("GetCurrentState")
                if len(state[1]) != 1 or not state[1][0][0][0].startswith("Meta-"):
                    raise RuntimeError("Capture requires exactly one owned virtual output")
                connector = state[1][0][0][0]
                capture_session = sc("/org/gnome/Mutter/ScreenCast", "org.gnome.Mutter.ScreenCast", "CreateSession", GLib.Variant("(a{sv})", ({},)))[0]
                stream = sc(capture_session, "org.gnome.Mutter.ScreenCast.Session", "RecordMonitor",
                            GLib.Variant("(sa{sv})", (connector, {"cursor-mode": GLib.Variant("u", 0)})))[0]
                nodes = []
                subscription = bus.signal_subscribe("org.gnome.Mutter.ScreenCast", "org.gnome.Mutter.ScreenCast.Stream", "PipeWireStreamAdded",
                    stream, None, Gio.DBusSignalFlags.NONE, lambda *values: nodes.append(values[5].unpack()[0]), None)
                try:
                    sc(capture_session, "org.gnome.Mutter.ScreenCast.Session", "Start")
                    deadline = time.monotonic() + 10
                    while not nodes:
                        if time.monotonic() >= deadline:
                            raise TimeoutError("Owned capture node deadline")
                        GLib.MainContext.default().iteration(False)
                        time.sleep(.01)
                finally:
                    bus.signal_unsubscribe(subscription)
                snapshot = json.loads(subprocess.check_output(["pw-dump"], timeout=5))
                node = next(item for item in snapshot if item["id"] == nodes[0])
                client_id = node["info"]["props"]["client.id"]
                client = next(item for item in snapshot if item["id"] == client_id)
                if int(client["info"]["props"]["application.process.id"]) != compositor.pid:
                    raise RuntimeError("Capture node does not belong to the owned compositor")
                prefix = "capture-" + str(len(applied))
                marker = output / (prefix + "-frames")
                with (output / (prefix + ".log")).open("w") as log:
                    capture = subprocess.Popen([str(capture_binary), str(nodes[0]), str(marker)], env=environment, stdout=log, stderr=subprocess.STDOUT)
                captures.append({"pid": capture.pid, "node": nodes[0], "compositorPID": owner, "connector": connector,
                    "session": capture_session, "marker": marker.name, "source": node})
                deadline = time.monotonic() + 12
                while not marker.exists():
                    if capture.poll() is not None:
                        raise RuntimeError("Capture failed before its first buffer")
                    if time.monotonic() >= deadline:
                        raise TimeoutError("Capture first-buffer deadline")
                    GLib.MainContext.default().iteration(False)
                    time.sleep(.01)

            start_capture()

            def apply(scale):
                applied.append(configure_scale(scale))
                (output / "applied.json").write_text(json.dumps(applied, indent=2) + "\n")
                # Mutter retires monitor recordings when their logical geometry changes.
                start_capture()

            environment.update(WAYLAND_DISPLAY=socket, SDL_VIDEODRIVER="wayland", GDK_BACKEND="wayland",
                               DBUS_SESSION_BUS_ADDRESS="unix:path=" + str(output / "no-engine-session-bus"), ELECTRON2D_DPI_CONTROL_DIR=str(control))
            command = (["dotnet", str(consumer)] if consumer.suffix == ".dll" else [str(consumer)]) + ["--native-dpi", "--gpu" if args.gpu else "--cpu"]
            command += (["--threaded"] if args.threaded else []) + (["--compatibility"] if args.compatibility else [])
            with (output / "engine.log").open("w") as log:
                engine = subprocess.Popen(command, env=environment, stdout=log, stderr=subprocess.STDOUT)
                (output / "engine-child.json").write_text(json.dumps({"pid": engine.pid, "command": command}, indent=2) + "\n")
                phase = 0
                while engine.poll() is None:
                    if capture.poll() is not None:
                        raise RuntimeError("Capture ended during native presentation")
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
            try:
                stop_capture()
            finally:
                if compositor.poll() is None:
                    os.killpg(compositor.pid, signal.SIGTERM)
                try:
                    compositor.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    os.killpg(compositor.pid, signal.SIGKILL)
                    compositor.wait(timeout=10)
                (output / "captures.json").write_text(json.dumps(captures, indent=2) + "\n")
            (output / "controller.json").write_text(json.dumps({"engineExit": None if engine is None else engine.returncode,
                "compositorExit": compositor.returncode, "devkit": args.devkit, "applied": applied, "seconds": time.monotonic() - started}, indent=2) + "\n")
    # Monitor recordings publish damage, not one buffer per client presentation.
    if len(captures) != 3 or any(item.get("frames", 0) < 1 for item in captures):
        raise RuntimeError("Every monitor configuration requires a nonempty capture stream")
    print("Native DPI compositor round trip passed.", flush=True)


if __name__ == "__main__":
    main()
