"""Independent AT-SPI URI/anchor consumer; never opens external resources."""
import sys
import time
import gi
gi.require_version('Atspi', '2.0')
from gi.repository import Atspi, GLib, Gio
Atspi.init()
context = GLib.MainContext.default()
FIRST = 'https://example.invalid/путь?value=😀&encoded=%20'
SECOND = 'file:///tmp/uri%20target.txt#section'


def wait(check):
    deadline = time.monotonic() + 10
    while time.monotonic() < deadline:
        while context.pending(): context.iteration(False)
        try:
            value = check()
            if value: return value
        except GLib.Error:
            pass
        time.sleep(.01)
    raise AssertionError('Native hyperlink state did not converge')


def children(node):
    return [node.get_child_at_index(i) for i in range(node.get_child_count())]


application = wait(lambda: next((n for n in children(Atspi.get_desktop(0)) if n.get_name() == 'Electron2D URL Probe'), None))


def find(name, parent=application):
    parent.clear_cache()
    if parent.get_name() == name: return parent
    for child in children(parent):
        result = find(name, child)
        if result: return result
    return None


def click(node):
    iface = node.get_action_iface()
    index = next(i for i in range(iface.get_n_actions()) if iface.get_localized_name(i) == 'click')
    return iface.do_action(index)


def href(node):
    node.clear_cache()
    return node.get_hyperlink()


def has_href(node):
    node.clear_cache()
    return any(str(name).endswith('Hyperlink') for name in node.get_interfaces())


if '--failure' in sys.argv:
    link = wait(lambda: find('Failing URL'))
    assert href(link).get_uri(0) == FIRST
    try:
        click(link)
        raise AssertionError('Link activation failure was swallowed')
    except GLib.Error as error:
        assert 'Injected link activation failure' in error.message, error.message
    print('PASS native URL activation failure wire reply', flush=True)
    sys.exit(0)

session = Gio.bus_get_sync(Gio.BusType.SESSION, None)
address = session.call_sync('org.a11y.Bus', '/org/a11y/bus', 'org.a11y.Bus', 'GetAddress', None, None, Gio.DBusCallFlags.NONE, 2000, None).unpack()[0]
connection = Gio.DBusConnection.new_for_address_sync(address, Gio.DBusConnectionFlags.AUTHENTICATION_CLIENT | Gio.DBusConnectionFlags.MESSAGE_BUS_CONNECTION, None, None)


def wire(dest, path, interface, method, args=None):
    return connection.call_sync(dest, path, interface, method, args, None, Gio.DBusCallFlags.NONE, 2000, None).unpack()


refs = wire('org.a11y.atspi.Registry', '/org/a11y/atspi/accessible/root', 'org.a11y.atspi.Accessible', 'GetChildren')[0]
destination = next(bus for bus, path in refs if wire(bus, path, 'org.freedesktop.DBus.Properties', 'Get', GLib.Variant('(ss)', ('org.a11y.atspi.Accessible', 'Name')))[0] == 'Electron2D URL Probe')
controller = wait(lambda: find('URL controller'))
actions = controller.get_action_iface()
commands = {actions.get_localized_name(i): i for i in range(actions.get_n_actions())}


def command(number, state):
    assert actions.do_action(commands['command-' + str(number)])
    wait(lambda: find(state))


def invalid(call):
    try:
        call()
        raise AssertionError('Invalid hyperlink anchor accepted')
    except GLib.Error:
        pass


link = wait(lambda: find('Live link'))
stored = wait(lambda: find('Stored link'))
generic = wait(lambda: find('Generic link'))
assert href(link).get_uri(0) == FIRST and href(stored).get_uri(0) == FIRST
assert not has_href(generic)
command(1, 'URL added')
g = wait(lambda: href(generic))
assert g.get_n_anchors() == 1 and g.is_valid()
assert g.get_uri(0) == FIRST and g.get_object(0).get_accessible_id() == generic.get_accessible_id()
assert g.get_start_index() == -1 and g.get_end_index() == -1
assert generic.get_role() == Atspi.Role.PUSH_BUTTON
invalid(lambda: g.get_uri(-1))
invalid(lambda: wire(destination, generic.get_accessible_id(), 'org.a11y.atspi.Hyperlink', 'GetObject', GLib.Variant('(i)', (1,))))
command(2, 'Hybrid')
assert g.get_uri(0) == FIRST and g.get_object(0).get_name() == 'Inside target'
command(3, 'Internal only')
assert g.is_valid() and g.get_n_anchors() == 1 and g.get_uri(0) == ''
assert g.get_object(0).get_name() == 'Inside target'
command(4, 'Target retired')
assert g.is_valid() and g.get_uri(0) == SECOND
assert g.get_object(0).get_accessible_id() == generic.get_accessible_id()
command(5, 'URL cleared')
assert not has_href(generic) and g.get_n_anchors() == 0 and not g.is_valid()
invalid(lambda: g.get_uri(0))
command(6, 'Unicode')
assert href(generic).get_uri(0) == 'relative/�😀'
command(7, 'Long URL')
assert href(generic).get_uri(0) == 'https://example.invalid/' + 'λ' * 3000
command(8, 'Override')
assert href(link).get_uri(0) == 'metadata:override'
assert click(link)
command(9, 'Source changed')
assert href(link).get_uri(0) == SECOND
assert link.get_help_text() == SECOND, 'URI changes must refresh dependent contextual help'
command(10, 'Source override cleared')
assert not has_href(link)
command(11, 'Source restored')
assert href(link).get_uri(0) == FIRST
old = link
command(12, 'Reparented')
link = wait(lambda: find('Live link'))
assert old.get_state_set().contains(Atspi.StateType.DEFUNCT)
assert href(link).get_uri(0) == FIRST
old_generic = href(generic)
command(13, 'Producer retired')
wait(lambda: generic.get_state_set().contains(Atspi.StateType.DEFUNCT))
try:
    value = old_generic.get_uri(0)
    assert value is None
except GLib.Error:
    pass
command(99, 'Measuring')
print('PASS native external/internal/hybrid anchors, literal/Unicode URIs, source overrides, actual activation hook, authoring, interface transitions and retirement', flush=True)
