"""Independent AT-SPI client for authored window metadata."""
import re
import subprocess
import time


def call(connection, bus, path, method, *args):
    result = subprocess.run(
        ["gdbus", "call", *connection, "--dest", bus, "--object-path", path,
         "--method", method, *args],
        check=True, stdout=subprocess.PIPE, text=True, timeout=4,
    )
    return result.stdout.strip()


address = call(["--session"], "org.a11y.Bus", "/org/a11y/bus", "org.a11y.Bus.GetAddress")
connection = ["--address", re.search(r"'([^']+)'", address)[1]]


def children(parent):
    result = call(connection, *parent, "org.a11y.atspi.Accessible.GetChildren")
    return re.findall(r"\('([^']+)',\s*(?:objectpath\s+)?'([^']+)'\)", result)


def metadata(node, field):
    return call(connection, *node, "org.freedesktop.DBus.Properties.Get",
                "org.a11y.atspi.Accessible", field)


def await_named(parent, expected):
    deadline = time.monotonic() + 6
    while time.monotonic() < deadline:
        for node in children(parent):
            if expected in metadata(node, "Name"):
                return node
        time.sleep(.02)
    raise AssertionError(f"Accessible element not found: {expected}")


def await_metadata(node, expected_name, expected_description):
    deadline = time.monotonic() + 6
    while time.monotonic() < deadline:
        if (expected_name in metadata(node, "Name")
                and expected_description == metadata(node, "Description")):
            return
        time.sleep(.02)
    raise AssertionError(f"Accessible metadata did not update: {expected_name}, {expected_description}")


app = await_named(("org.a11y.atspi.Registry", "/org/a11y/atspi/accessible/root"),
                  "Electron2D Window Accessibility Probe")
root = await_named(app, "Accessible workspace")
assert "Workspace description" in metadata(root, "Description")
native = await_named(app, "Native dialog")
embedded = await_named(root, "Embedded dialog")
assert "Native details" in metadata(native, "Description")
assert "Embedded details" in metadata(embedded, "Description")
print("translate", flush=True)
await_metadata(root, "Espace de travail", "(<'Description locale'>,)")
print("clear", flush=True)
await_metadata(root, "Workspace caption", "(<''>,)")
assert embedded not in children(root) and native not in children(app)
