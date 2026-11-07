#!/usr/bin/env bash
set -euo pipefail

# Use only the X server reserved for this test. Weston reads its RMLVO settings.
: "${ELECTRON2D_TEST_X11_PRIVATE_DISPLAY:?Start a private Xvfb first}"
test "$ELECTRON2D_TEST_X11_PRIVATE_DISPLAY" = "$DISPLAY"
setxkbmap -layout us,ru
root=$(mktemp -d)
trap 'rm -rf -- "$root"' EXIT
cp -aL "${XKB_CONFIG_ROOT:-/usr/share/X11/xkb}/." "$root/"
export XKB_CONFIG_ROOT="$root"

# Put the F25–F35 raw slots into the private compositor's keymap.
awk '
    /^xkb_symbols "evdev"/ { evdev = 1 }
    evdev && /^};/ {
        count = split("LVL5 ALT META SUPR HYPR I208 I209 I210 I211 I212 I213", keys)
        for (i = 1; i <= count; i++)
            printf "    replace key <%s> { [ F%d ] };\n", keys[i], i + 24
        evdev = 0
    }
    { print }
' "$root/symbols/inet" > "$root/symbols/inet.fixture"
mv "$root/symbols/inet.fixture" "$root/symbols/inet"
export ELECTRON2D_TEST_WAYLAND_EXTENDED_KEYS=1
"$@"
