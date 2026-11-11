"""Read the real AT-SPI registry and confirm the tree exposes the selected source."""
import re
import subprocess
import sys


def call(bus, path, method, *args, address=None):
    connection = ["--session"] if address is None else ["--address", address]
    result = subprocess.run(
        ["gdbus", "call", *connection, "--dest", bus, "--object-path", path, "--method", method, *args],
        check=True, stdout=subprocess.PIPE, text=True, timeout=4,
    )
    return result.stdout.strip()


address = re.search(r"'([^']+)'", call("org.a11y.Bus", "/org/a11y/bus", "org.a11y.Bus.GetAddress"))[1]


def children(bus, path):
    result = call(bus, path, "org.a11y.atspi.Accessible.GetChildren", address=address)
    return re.findall(r"\('([^']+)',\s*(?:objectpath\s+)?'([^']+)'\)", result)


def name(bus, path):
    return call(bus, path, "org.freedesktop.DBus.Properties.Get", "org.a11y.atspi.Accessible", "Name", address=address)


apps = children("org.a11y.atspi.Registry", "/org/a11y/atspi/accessible/root")
app, = (candidate for candidate in apps if "Electron2D Scene Accessibility Probe" in name(*candidate))
window, = children(*app)
names = [name(*source) for source in children(*window)]
expected = "Automatic scene button" if sys.argv[1] == "automatic" else "Manual button"
assert len(names) == 1 and expected in names[0], names
print(f"Independent AT-SPI registry and window expose {expected}.")
