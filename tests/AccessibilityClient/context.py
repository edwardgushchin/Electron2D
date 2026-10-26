"""Independent libatspi consumer of contextual names/help and real shortcut sources."""
import time
import sys
import gi
gi.require_version('Atspi', '2.0')
from gi.repository import Atspi, GLib, Gio
Atspi.init()
context = GLib.MainContext.default()


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
    raise AssertionError('Native contextual state did not converge')


def children(node):
    return [node.get_child_at_index(i) for i in range(node.get_child_count())]


application = wait(lambda: next((n for n in children(Atspi.get_desktop(0)) if n.get_name() == 'Electron2D Context Probe'), None))


def find(name, parent=application):
    parent.clear_cache()
    if parent.get_name() == name: return parent
    for child in children(parent):
        match = find(name, child)
        if match: return match
    return None


if '--binding-failure' in sys.argv:
    node = wait(lambda: find('Failing shortcut'))
    iface = node.get_action_iface()
    index = next(i for i in range(iface.get_n_actions()) if iface.get_localized_name(i) == 'click')
    try:
        iface.get_key_binding(index)
        raise AssertionError('Binding failure was swallowed')
    except GLib.Error as error:
        assert error.message == 'Injected shortcut description failure', error.message
    print('PASS native binding description failure wire reply', flush=True)
    sys.exit(0)


controller = wait(lambda: find('Context controller'))
actions = controller.get_action_iface()
commands = {actions.get_localized_name(i): i for i in range(actions.get_n_actions())}
assert actions.get_key_binding(0) == ';;Ctrl+C'


def command(number, state):
    assert actions.do_action(commands['command-' + str(number)])
    wait(lambda: find(state))


def click_index(node):
    iface = node.get_action_iface()
    return next(i for i in range(iface.get_n_actions()) if iface.get_localized_name(i) == 'click')


def binding(node):
    return node.get_action_iface().get_key_binding(click_index(node))


button = wait(lambda: find('Move Row one'))
assert button.get_help_text() == 'Use arrow keys'
assert button.get_attributes()['class'] == 'ContextButton'
assert binding(button) == ';;Before first query'
command(0, 'First edit')
assert binding(button).startswith(';;') and binding(button).endswith('N')
session = Gio.bus_get_sync(Gio.BusType.SESSION, None)
address = session.call_sync('org.a11y.Bus', '/org/a11y/bus', 'org.a11y.Bus', 'GetAddress', None, None, Gio.DBusCallFlags.NONE, 2000, None).unpack()[0]
connection = Gio.DBusConnection.new_for_address_sync(address, Gio.DBusConnectionFlags.AUTHENTICATION_CLIENT | Gio.DBusConnectionFlags.MESSAGE_BUS_CONNECTION, None, None)


def wire(dest, path, interface, method, args=None):
    return connection.call_sync(dest, path, interface, method, args, None, Gio.DBusCallFlags.NONE, 2000, None).unpack()


refs = wire('org.a11y.atspi.Registry', '/org/a11y/atspi/accessible/root', 'org.a11y.atspi.Accessible', 'GetChildren')[0]
destination = next(bus for bus, path in refs if wire(bus, path, 'org.freedesktop.DBus.Properties', 'Get', GLib.Variant('(ss)', ('org.a11y.atspi.Accessible', 'Name')))[0] == 'Electron2D Context Probe')
path = button.get_accessible_id()
entries = wire(destination, path, 'org.a11y.atspi.Action', 'GetActions')[0]
assert any(name == 'click' and keys == binding(button) for name, description, keys in entries)
try:
    wire(destination, path, 'org.a11y.atspi.Action', 'GetKeyBinding', GLib.Variant('(i)', (-1,)))
    raise AssertionError('Negative action ordinal accepted')
except GLib.Error:
    pass
victim = wait(lambda: find('Victim Slot'))
command(1, 'Context changed')
wait(lambda: find('Move Row two'))
assert button.get_help_text() == 'Use Enter'
command(2, 'Binding changed')
assert binding(button) == ';;F8'
command(3, 'Override')
assert binding(button) == ';;Alt+X'
button.clear_cache()
attrs = button.get_attributes()
assert attrs['class'] == 'custom-button' and attrs['roledescription'] == 'delivery action'
assert button.get_role() == Atspi.Role.PUSH_BUTTON
command(4, 'Borrowed edited')
assert binding(button) == ';;F9'
button.clear_cache()
assert button.get_attributes()['roledescription'] == 'delivery action'
command(5, 'Cleared')
assert binding(button) == ''
button.clear_cache()
assert 'class' not in button.get_attributes() and 'roledescription' not in button.get_attributes()
command(13, 'Name override')
wait(lambda: find('Manual name'))
command(6, 'Unicode')
wait(lambda: find('Move  �😀  extra'))
assert button.get_help_text() == 'λ' * 3000
command(12, 'Long name')
wait(lambda: find('Move ' + 'λ' * 3000 + ' extra'))
old = button
command(7, 'Reparented')
button = wait(lambda: find('Move Other'))
assert old.get_state_set().contains(Atspi.StateType.DEFUNCT)
assert button.get_help_text() == 'λ' * 3000
command(8, 'Boundary')
button = wait(lambda: find('Move'))
command(9, 'Retire queued')
wait(lambda: victim.get_state_set().contains(Atspi.StateType.DEFUNCT))
command(10, 'Menu visible')
item = wait(lambda: find('Menu action'))
assert binding(item).startswith(';;') and binding(item).endswith('F7')
command(11, 'Menu edited')
assert binding(item) == ';;F6'
command(99, 'Measuring')
wait(lambda: find('Move one Other context') or find('Move two Other context') or find('Move Other context'))
print('PASS native contextual name/help, ancestor updates, keybinding edits/overrides, role/class attributes, reparenting/boundaries and callback retirement', flush=True)
