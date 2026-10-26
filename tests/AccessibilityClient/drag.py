"""Independent libatspi consumer of integer custom actions and real GUI transfer."""
import sys
import time
import gi
gi.require_version('Atspi', '2.0')
from gi.repository import Atspi, GLib
Atspi.init()
context = GLib.MainContext.default()


def wait(check):
    deadline = time.monotonic() + 9
    while time.monotonic() < deadline:
        while context.pending(): context.iteration(False)
        try:
            result = check()
            if result: return result
        except GLib.Error:
            pass
        time.sleep(.01)
    raise AssertionError('Native drag state did not converge')


def children(node):
    return [node.get_child_at_index(i) for i in range(node.get_child_count())]


def app():
    return next((node for node in children(Atspi.get_desktop(0)) if node.get_name() == 'Electron2D Native Drag Probe'), None)


application = wait(app)
root = wait(lambda: application.get_child_at_index(0))

def find(prefix):
    return next((node for node in children(root) if node.get_name().startswith(prefix)), None)


def names(node):
    iface = node.get_action_iface()
    return [iface.get_localized_name(i) for i in range(iface.get_n_actions())]


def action(node, label):
    assert node.get_action_iface().do_action(names(node).index(label)), label


if '--failure' in sys.argv:
    node = wait(lambda: find('Failing custom'))
    try: action(node, 'fail')
    except GLib.Error as error:
        assert error.message == 'Injected custom failure -2147483648', str(error)
        print('PASS native custom failure wire reply')
        sys.exit(0)
    raise AssertionError('Native custom exception was swallowed')

controller = wait(lambda: find('Transfer controller'))
source, target = wait(lambda: find('Inventory')), wait(lambda: find('Destination'))
meta = wait(lambda: find('Metadata'))
guard, unbound = wait(lambda: find('Guarded custom')), wait(lambda: find('Unbound custom'))
retiring = wait(lambda: find('Retiring custom'))
assert meta.get_name() == 'Metadata     context', repr(meta.get_name())
initial = names(controller)
assert len(initial) == len(set(initial)) == 20
assert not guard.get_action_iface().do_action(0)
assert not unbound.get_action_iface().do_action(0)
action(controller, 'enable'); action(guard, 'guarded-action')
action(controller, 'custom-min'); action(controller, 'custom-max'); action(controller, 'check-ids')
old_index = names(controller).index('custom-min')
action(controller, 'rename')
wait(lambda: names(controller)[old_index] == 'custom-min-renamed')
assert len(names(controller)) == len(initial)
action(controller, 'unicode'); wait(lambda: meta.get_name() == 'A�😀    �Z')
action(controller, 'long'); wait(lambda: meta.get_name().endswith('λ' * 3000))
action(controller, 'clear'); wait(lambda: meta.get_name() == 'A�😀')
announcements = []
def announced(event, data=None):
    if event.source.get_application().get_name() == 'Electron2D Native Drag Probe': announcements.append((event.detail1, event.any_data))
listener = Atspi.EventListener.new(announced, None)
assert listener.register('object:announcement')
action(controller, 'grab')
wait(lambda: 'Package can be dropped here' in target.get_name())
wait(lambda: any(priority == 2 and 'Package grabbed' in text for priority, text in announcements))
action(controller, 'block'); wait(lambda: 'cannot be dropped here' in target.get_name())
action(controller, 'accept'); wait(lambda: 'can be dropped here' in target.get_name())
action(controller, 'drop'); wait(lambda: target.get_name() == 'Destination')
action(controller, 'keyboard-grab'); wait(lambda: 'can be dropped here' in target.get_name())
action(controller, 'keyboard-drop'); wait(lambda: target.get_name() == 'Destination')
action(controller, 'grab'); action(controller, 'cancel'); wait(lambda: target.get_name() == 'Destination')
action(controller, 'null-grab'); wait(lambda: target.get_name() == 'Destination')
for command in ('line', 'editor', 'capacity'): action(controller, command)
retired_iface = retiring.get_action_iface()
action(retiring, 'retire')
wait(lambda: retiring.get_state_set().contains(Atspi.StateType.DEFUNCT))
try: assert not retired_iface.do_action(0), 'Retired custom action still invokes'
except GLib.Error: pass
action(controller, 'finish')
listener.deregister('object:announcement')
Atspi.exit()
print('PASS libatspi custom IDs/descriptions, metadata UTF-8, native grab announcements, keyboard/context transitions, text/capacity routing and retirement')
