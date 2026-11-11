#!/usr/bin/env bash
set -euo pipefail

runner=$(realpath -- "${1:?Provide the built Electron2D.Tests.dll}")
source=$(dirname "$(realpath -- "$0")")/Fixtures/SystemBell
work=$(mktemp -d)
weston_pid=
cleanup() {
    if [[ -n "$weston_pid" ]]; then
        kill "$weston_pid" 2>/dev/null || true
        wait "$weston_pid" 2>/dev/null || true
    fi
    rm -r -- "$work"
}
trap cleanup EXIT

protocol=$(pkg-config --variable=pkgdatadir wayland-protocols)/staging/xdg-system-bell/xdg-system-bell-v1.xml
wayland-scanner server-header "$protocol" "$work/system-bell-server.h"
wayland-scanner private-code "$protocol" "$work/system-bell-protocol.c"
cc -std=c11 -Wall -Wextra -Werror "$source/x11-bell.c" -lX11 -o "$work/x11-bell"
cc -std=gnu11 -Wall -Wextra -Werror -shared -fPIC -I"$work" \
    $(pkg-config --cflags libweston-15 wayland-server) "$source/weston-bell.c" "$work/system-bell-protocol.c" \
    $(pkg-config --libs wayland-server) -o "$work/weston-bell.so"

export SDL_RENDER_DRIVER=software NO_AT_BRIDGE=1
xvfb-run -a -s "-screen 0 1024x768x24 -nolisten tcp -noreset" bash -euc '
    "$1/x11-bell" > "$1/x11.log" &
    observer=$!
    trap "kill $observer 2>/dev/null || true" EXIT
    for _ in $(seq 1 100); do
        grep -q "^READY$" "$1/x11.log" && break
        kill -0 "$observer"
        sleep 0.05
    done
    grep -q "^READY$" "$1/x11.log"
    SDL_VIDEODRIVER=x11 ELECTRON2D_TEST_SYSTEM_BELL=x11 dotnet "$2"
    wait "$observer"
    cat "$1/x11.log"
' fixture "$work" "$runner"

export XDG_RUNTIME_DIR="$work/runtime"
export WAYLAND_DISPLAY=bell-test
unset WAYLAND_SOCKET
mkdir -m 700 "$XDG_RUNTIME_DIR"
weston --backend=headless --renderer=pixman --shell=kiosk-shell.so --no-config \
    --socket="$WAYLAND_DISPLAY" --modules="$work/weston-bell.so" --log="$work/weston.log" 2> "$work/wayland.log" &
weston_pid=$!
for _ in $(seq 1 100); do
    [[ -S "$XDG_RUNTIME_DIR/$WAYLAND_DISPLAY" ]] && break
    if ! kill -0 "$weston_pid"; then
        cat "$work/weston.log" "$work/wayland.log"
        exit 1
    fi
    sleep 0.05
done
test -S "$XDG_RUNTIME_DIR/$WAYLAND_DISPLAY"
SDL_VIDEODRIVER=wayland ELECTRON2D_TEST_SYSTEM_BELL=wayland dotnet "$runner"
for _ in $(seq 1 100); do
    [[ $(grep -c '^bell destroy$' "$work/wayland.log") == 2 ]] && break
    sleep 0.05
done
cat "$work/wayland.log"
test "$(grep -c '^bell bind$' "$work/wayland.log")" -eq 2
test "$(grep -c '^bell ring surface=' "$work/wayland.log")" -eq 3
test "$(grep -c '^bell ring surface=null$' "$work/wayland.log")" -eq 3
test "$(grep -c '^bell destroy$' "$work/wayland.log")" -eq 2
