#!/usr/bin/env bash
# Pass the Release test assembly built for Linux. Requires a C compiler, pkg-config,
# wayland-server development files, wayland-protocols, and the Wayland scanner.
set -euo pipefail
test_assembly=$(realpath "${1:?Pass the Linux Electron2D.Tests.dll path}")
source_dir=$(dirname "$(realpath "$0")")/Fixtures
work=$(mktemp -d)
fixture_pid=

stop_fixture() {
    if [[ -n "$fixture_pid" ]]; then
        kill "$fixture_pid" 2>/dev/null || true
        wait "$fixture_pid" 2>/dev/null || true
        fixture_pid=
    fi
}
cleanup() {
    stop_fixture
    rm -rf -- "$work"
}
trap cleanup EXIT

protocols=$(pkg-config --variable=pkgdatadir wayland-protocols)
shell_protocol=$protocols/stable/xdg-shell/xdg-shell.xml
wayland-scanner server-header "$shell_protocol" "$work/xdg-shell-server.h"
wayland-scanner private-code "$shell_protocol" "$work/xdg-shell.c"
cc -Wall -Wextra -Werror -I"$work" $(pkg-config --cflags wayland-server) \
    "$source_dir/wayland-screen-dpi.c" "$work/xdg-shell.c" \
    $(pkg-config --libs wayland-server) -o "$work/compositor"
printf 'libwayland-server %s\n' "$(pkg-config --modversion wayland-server)"

export XDG_RUNTIME_DIR=$work
export SDL_VIDEO_DRIVER=wayland
# This fixture implements the display protocol required by SDL, without GTK's desktop globals.
export GDK_BACKEND=none
export SDL_VIDEO_WAYLAND_PREFER_LIBDECOR=0
export SDL_VIDEO_WAYLAND_ALLOW_LIBDECOR=0
export ELECTRON2D_TEST_SCREEN_DPI=wayland
unset WAYLAND_SOCKET

for profile in physical missing; do
    export WAYLAND_DISPLAY=electron2d-dpi-$profile
    export ELECTRON2D_TEST_WAYLAND_PRIVATE_DISPLAY=$WAYLAND_DISPLAY
    "$work/compositor" "$WAYLAND_DISPLAY" "$profile" &
    fixture_pid=$!
    for ((attempt = 0; attempt < 100; attempt++)); do
        if [[ -S "$work/$WAYLAND_DISPLAY" ]]; then
            break
        fi
        kill -0 "$fixture_pid"
        sleep 0.05
    done
    if [[ ! -S "$work/$WAYLAND_DISPLAY" ]]; then
        echo "The Wayland fixture did not open its socket." >&2
        exit 1
    fi
    timeout 30s dotnet "$test_assembly" "$profile"
    stop_fixture
done
