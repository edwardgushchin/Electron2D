"""Compare exact snapshot bytes, allowing checkout line-ending conversion."""
from pathlib import Path
import sys


def compare(expected, actual):
    return expected.read_bytes().replace(b'\r\n', b'\n') == actual.read_bytes().replace(b'\r\n', b'\n')


if __name__ == '__main__':
    expected, actual = map(Path, sys.argv[1:])
    if not compare(expected, actual):
        raise SystemExit(f'API snapshots differ: {expected} and {actual}')
