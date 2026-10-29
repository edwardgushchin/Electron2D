"""Independent libatspi consumer of rate-limited scenes and unthrottled requests."""
import time
import gi
gi.require_version('Atspi', '2.0')
from gi.repository import Atspi, GLib
Atspi.init()
context = GLib.MainContext.default()


def wait(check):
    deadline = time.monotonic() + 8
    while time.monotonic() < deadline:
        while context.pending(): context.iteration(False)
        result = check()
        if result: return result
        time.sleep(.005)
    raise AssertionError('Scheduled native scene did not converge')


def children(node):
    node.clear_cache()
    return [node.get_child_at_index(i) for i in range(node.get_child_count())]


app = wait(lambda: next((n for n in children(Atspi.get_desktop(0)) if n.get_name() == 'Electron2D Scheduling Probe'), None))
root = wait(lambda: (items[0] if (items := children(app)) else None))


def find(name):
    for node in children(root):
        node.clear_cache()
        if node.get_name() == name: return node


controller = wait(lambda: find('Scheduling controller'))
actions = controller.get_action_iface()
commands = {actions.get_localized_name(i): i for i in range(actions.get_n_actions())}
probe = wait(lambda: find('Scheduled action'))
click = probe.get_action_iface()
click_id = next(i for i in range(click.get_n_actions()) if click.get_localized_name(i) == 'click')


def command(number):
    assert actions.do_action(commands['schedule-' + str(number)])
    controller.clear_cache()
    assert controller.get_name() == 'command-' + str(number), 'Explicit publication was delayed'


command(0)
wait(lambda: find('Last burst value'))
command(1)
assert click.do_action(click_id)
wait(lambda: find('Clicked'))
command(2)
assert not click.do_action(click_id), 'Hidden/disabled source accepted an action between semantic passes'
command(3)
wait(lambda: (probe.clear_cache() or not probe.get_state_set().contains(Atspi.StateType.DEFUNCT)) and probe.get_state_set().contains(Atspi.StateType.SHOWING))
assert click.do_action(click_id)
retired = wait(lambda: find('Retire immediately'))
retired_action = retired.get_action_iface()
command(4)
try:
    result = retired_action.do_action(0)
    assert not result, 'Retired source accepted an action'
except GLib.Error:
    pass
command(5)
wait(lambda: find('Late source'))
command(6)
wait(lambda: find('Paused scene'))
command(99)
wait(lambda: find('Measuring'))
print('PASS independent libatspi scheduled GUI, coalescing, live driver lifetime, immediate native actions/publications/retirement and paused output', flush=True)
