#!/usr/bin/env bash
set -euo pipefail
test -n "$ELECTRON2D_TEST_WAYLAND_PRIVATE_DISPLAY"
test "$WAYLAND_DISPLAY" = "$ELECTRON2D_TEST_WAYLAND_PRIVATE_DISPLAY"
test -S "$SWAYSOCK"

if [[ $1 == click ]]; then
    exec "$ELECTRON2D_TEST_WAYLAND_POINTER_ACTOR" "$2" "$3" 1440 601
fi

# Wait for the provider's own selector in this private compositor.
for _ in {1..100}; do
    for pid in $(pgrep -x slurp || true); do
        if grep -Fzxq "XDG_RUNTIME_DIR=$XDG_RUNTIME_DIR" "/proc/$pid/environ" 2>/dev/null; then
            sleep .12
            case "$1" in
                pick)
                    "$ELECTRON2D_TEST_WAYLAND_POINTER_ACTOR" "$2" "$3" 1440 601
                    ;;
                cancel) wtype -s 180 -k Escape -s 80 ;;
                *) exit 2 ;;
            esac
            exit 0
        fi
    done
    sleep .04
done
echo "The portal's selector did not open." >&2
exit 1
