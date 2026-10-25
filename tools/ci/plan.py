"""Choose inexpensive feedback or complete qualification without guessing changed code."""
import json
import os
from pathlib import PurePosixPath, Path
import subprocess


def documentation(path):
    item = PurePosixPath(path)
    return (item.suffix == '.md' and (len(item.parts) == 1 or item.parts[0] == 'docs'))


def plan(event_name, event, paths):
    if event_name == 'workflow_dispatch':
        full = event.get('inputs', {}).get('profile', 'full') != 'fast'
        return full, 'manual full qualification' if full else 'manual fast feedback'
    if event_name == 'schedule':
        return True, 'scheduled clean qualification'
    if paths and all(documentation(path) for path in paths):
        return False, 'documentation only'
    if event_name == 'pull_request' and event.get('pull_request', {}).get('draft') is True:
        return False, 'draft development feedback'
    return True, 'code/unknown changes require full qualification'


def changed_paths(event_name, event):
    before = (event.get('pull_request', {}).get('base', {}).get('sha')
              if event_name == 'pull_request' else event.get('before'))
    if not before or set(before) == {'0'}:
        return None
    try:
        return subprocess.check_output(['git', 'diff', '--name-only', '-z', before, 'HEAD'],
                                       stderr=subprocess.DEVNULL).decode().split('\0')[:-1]
    except (subprocess.CalledProcessError, UnicodeDecodeError):
        return None


if __name__ == '__main__':
    event = json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text())
    name = os.environ['GITHUB_EVENT_NAME']
    full, reason = plan(name, event, changed_paths(name, event))
    print('full=' + str(full).lower())
    print('rebuild=' + str(name == 'schedule' or event.get('inputs', {}).get('profile') == 'clean').lower())
    print('reason=' + reason)
