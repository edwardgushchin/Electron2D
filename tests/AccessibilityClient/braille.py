"""Check object attributes and signals on the real AT-SPI D-Bus connection."""

import time

import gi

gi.require_version("Atspi", "2.0")
from gi.repository import Atspi, Gio, GLib

ACCESSIBLE = "org.a11y.atspi.Accessible"
ACTION = "org.a11y.atspi.Action"
OBJECT_EVENT = "org.a11y.atspi.Event.Object"
CACHE = "org.a11y.atspi.Cache"
PROPERTIES = "org.freedesktop.DBus.Properties"
REGISTRY_ROOT = ("org.a11y.atspi.Registry", "/org/a11y/atspi/accessible/root")


def call(connection, ref, interface, method, args=None):
    reply = connection.call_sync(
        *ref, interface, method, args, None, Gio.DBusCallFlags.NONE, 3000, None
    )
    return reply.unpack()


def wait(check, message):
    context = GLib.MainContext.default()
    deadline = time.monotonic() + 8
    while time.monotonic() < deadline:
        while context.pending():
            context.iteration(False)
        value = check()
        if value:
            return value
        time.sleep(0.01)
    raise AssertionError(message)


def main():
    session = Gio.bus_get_sync(Gio.BusType.SESSION, None)
    address, = call(session, ("org.a11y.Bus", "/org/a11y/bus"), "org.a11y.Bus", "GetAddress")
    connection = Gio.DBusConnection.new_for_address_sync(
        address,
        Gio.DBusConnectionFlags.AUTHENTICATION_CLIENT | Gio.DBusConnectionFlags.MESSAGE_BUS_CONNECTION,
        None,
        None,
    )

    def children(ref):
        return call(connection, ref, ACCESSIBLE, "GetChildren")[0]

    def name(ref):
        return call(
            connection, ref, PROPERTIES, "Get", GLib.Variant("(ss)", (ACCESSIBLE, "Name"))
        )[0]

    def find(ref, expected):
        if name(ref) == expected:
            return ref
        for child in children(ref):
            found = find(child, expected)
            if found:
                return found
        return None

    app = wait(
        lambda: next(
            (ref for ref in children(REGISTRY_ROOT) if name(ref) == "Electron2D Braille Probe"), None
        ),
        "The braille producer was not registered.",
    )
    target = wait(lambda: find(app, "Ordinary name"), "The braille target was not published.")
    controller = wait(lambda: find(app, "Braille controller"), "The controller was not published.")
    destination, path = target
    changes = []
    ordinary_changes = []
    retirements = []
    acknowledged = set()

    def receive(_, sender, signal_path, interface, member, params):
        args = params.unpack()
        if signal_path == path and interface == OBJECT_EVENT:
            if member == "AttributesChanged":
                changes.append((params.get_type_string(), args))
            elif member == "PropertyChange":
                ordinary_changes.append(args[0])
            elif member == "StateChanged" and args[0] == "defunct":
                retirements.append((member, args[:2]))
        elif member == "PropertyChange" and args[0] == "accessible-description":
            acknowledged.add(args[3])
        elif member == "RemoveAccessible" and args == ((sender, path),):
            retirements.append((member, args[0]))

    for interface, member, signal_path in (
        (OBJECT_EVENT, None, path),
        (OBJECT_EVENT, "PropertyChange", controller[1]),
        (CACHE, "RemoveAccessible", "/org/a11y/atspi/cache"),
    ):
        connection.signal_subscribe(
            destination, interface, member, signal_path, None, Gio.DBusSignalFlags.NONE, receive
        )

    methods, = call(connection, controller, ACTION, "GetActions")
    commands = {entry[0]: index for index, entry in enumerate(methods)}

    def assert_values(label=None, role_description=None):
        attributes, = call(connection, target, ACCESSIBLE, "GetAttributes")
        assert name(target) == "Ordinary name"
        assert call(connection, target, ACCESSIBLE, "GetRole") == (int(Atspi.Role.PUSH_BUTTON),)
        assert attributes["roledescription"] == "Ordinary role description", attributes
        for key, expected in (("braillelabel", label), ("brailleroledescription", role_description)):
            if expected is None:
                assert key not in attributes, attributes
            else:
                assert attributes.get(key) == expected, attributes

    def step(index, count, label=None, role_description=None):
        event_start = len(changes)
        command = f"step-{index}"
        assert call(
            connection, controller, ACTION, "DoAction", GLib.Variant("(i)", (commands[command],))
        ) == (True,)
        # This signal follows the mutations on the same connection, so silent steps need no delay.
        wait(lambda: command in acknowledged, f"The producer did not acknowledge {command}.")
        expected_event = ("(siiva{sv})", ("", 0, 0, 0, {}))
        assert changes[event_start:] == [expected_event] * count, (command, changes[event_start:])
        assert not ordinary_changes, ordinary_changes
        if index != 10:
            assert_values(label, role_description)

    assert_values()
    step(0, 1, "⠃⠗ имя")
    step(1, 1, "⠃⠗ имя", "⠗ роль 🔔")
    step(2, 1, "Contrôle ⠉", "⠗ роль 🔔")
    step(3, 1, "Contrôle ⠉", "操作 ⠏")
    step(4, 0, "Contrôle ⠉", "操作 ⠏")
    step(5, 1, None, "操作 ⠏")
    step(6, 2, "⠃⠗ имя")
    step(7, 1)
    step(8, 0)
    step(9, 2, "⠃⠗ имя", "⠗ роль 🔔")
    step(10, 0)
    assert retirements == [("StateChanged", ("defunct", 1)), ("RemoveAccessible", target)], retirements
    assert find(app, "Ordinary name") is None
    try:
        call(connection, target, ACCESSIBLE, "GetAttributes")
    except GLib.Error as error:
        assert Gio.DBusError.get_remote_error(error) in (
            "org.freedesktop.DBus.Error.UnknownMethod",
            "org.freedesktop.DBus.Error.UnknownObject",
        ), error
    else:
        raise AssertionError("The released AT-SPI path still returns attributes.")
    print(
        f"PASS 11 AT-SPI steps, {len(changes)} AttributesChanged signals, "
        "Unicode values and stable ordinary semantics, invalid RIDs and object retirement.",
        flush=True,
    )


if __name__ == "__main__":
    main()
