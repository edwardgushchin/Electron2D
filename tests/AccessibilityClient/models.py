"""Independent AT-SPI producer action consumer; model storage stays inside the producer."""
import sys
import time
import gi
gi.require_version('Atspi', '2.0')
from gi.repository import Atspi, GLib
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
    raise AssertionError('Native producer model state did not converge')


def children(node):
    return [node.get_child_at_index(i) for i in range(node.get_child_count())]


application = wait(lambda: next((n for n in children(Atspi.get_desktop(0)) if n.get_name() == 'Electron2D Model Probe'), None))


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


if '--failure' in sys.argv:
    node = wait(lambda: find('Failing model'))
    try:
        click(node)
        raise AssertionError('Producer model failure was swallowed')
    except GLib.Error as error:
        assert 'Injected producer model failure' in error.message, error.message
    print('PASS native producer model failure wire reply', flush=True)
    sys.exit(0)

controller = wait(lambda: find('Model controller'))
actions = controller.get_action_iface()
commands = {actions.get_localized_name(i): i for i in range(actions.get_n_actions())}


def command(number, state):
    assert actions.do_action(commands['command-' + str(number)])
    wait(lambda: find(state))


node = wait(lambda: find('Model action'))
assert click(node)
wait(lambda: find('First 1'))
command(1, 'Replaced')
assert click(node)
wait(lambda: find('Second 1'))
command(2, 'Cleared')
assert click(node)
node.clear_cache()
assert node.get_name() == 'Second 1'
command(3, 'Restored')
assert click(node)
wait(lambda: find('First 2'))
source = wait(lambda: find('Scene source'))
source_id = source.get_accessible_id()
command(4, 'Reattached')
wait(lambda: source.get_state_set().contains(Atspi.StateType.DEFUNCT))
command(5, 'Fresh source')
assert wait(lambda: find('Scene source')).get_accessible_id() != source_id
command(6, 'Reentrant')
assert click(node)
wait(lambda: node.get_state_set().contains(Atspi.StateType.DEFUNCT))
command(7, 'Retired')
command(99, 'Measuring')
print('PASS native action uses current producer model across replacement, clearing, source retirement and reentrant element retirement', flush=True)
