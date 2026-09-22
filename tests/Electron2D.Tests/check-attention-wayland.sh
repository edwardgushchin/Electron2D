#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
trace=$(mktemp)
trap 'rm -f "$trace"' EXIT
env -u LD_LIBRARY_PATH ELECTRON2D_TEST_DISPLAY_ATTENTION=1 SDL_VIDEODRIVER=wayland WAYLAND_DEBUG=client \
  dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release --no-restore 2>"$trace"
python3 - "$trace" <<'PY'
from pathlib import Path
import sys

trace = Path(sys.argv[1]).read_text()
foreground = trace.split('FOREGROUND_BEGIN', 1)[1].split('FOREGROUND_END', 1)[0]
attention = trace.split('ATTENTION_BEGIN', 1)[1].split('ATTENTION_END', 1)[0]
assert 'xdg_activation' not in foreground, 'Foreground sent a Wayland activation request.'
assert attention.count('.get_activation_token(') == 1, 'Attention did not request one token.'
assert 'xdg_activation_token_v1#' in attention and '.commit()' in attention, 'Attention did not commit its token.'
assert '.set_serial(' not in attention, 'Attention supplied an input serial.'
assert attention.count('.activate(') == 1, 'Attention did not submit its activation token.'
print('Wayland foreground and attention protocol checks passed.')
PY
