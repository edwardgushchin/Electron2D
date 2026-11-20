#!/usr/bin/env bash
set -euo pipefail

test_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
if [[ ${1:-} != --session ]]; then
    scratch=$(mktemp -d /tmp/electron2d-file-manager.XXXXXXXX)
    trap 'rm -rf -- "$scratch"' EXIT
    read -r -a gio_flags <<< "$(pkg-config --cflags --libs gio-2.0)"
    cc -std=c11 -Wall -Wextra -Werror -Wno-unused-parameter \
        "$test_dir/file-manager-receiver.c" -o "$scratch/receiver" "${gio_flags[@]}"
    dotnet build "$test_dir/FileManager.Consumer.csproj" -c Release \
        -p:FileManagerLinuxProbe=true --artifacts-path "$scratch/build" "$@"

    cat > "$scratch/dbus.conf" <<'XML'
<busconfig>
  <type>session</type>
  <listen>unix:tmpdir=/tmp</listen>
  <policy context="default">
    <allow own="*"/>
    <allow send_destination="*"/>
    <allow receive_sender="*"/>
  </policy>
</busconfig>
XML
    dbus-run-session --config-file="$scratch/dbus.conf" -- bash "$0" --session "$scratch"
    exit
fi

scratch=$2
app="$scratch/build/bin/FileManager.Consumer/release/FileManager.Consumer.dll"
directory="$scratch/dir 日本語 #?%20"
file="$directory/file 日本語 #?%20.txt"
directory_uri="file://$scratch/dir%20%E6%97%A5%E6%9C%AC%E8%AA%9E%20%23%3F%2520"
file_uri="$directory_uri/file%20%E6%97%A5%E6%9C%AC%E8%AA%9E%20%23%3F%2520.txt"
mkdir -- "$directory"
touch -- "$file"
receiver_pid=

stop_receiver() {
    if [[ -n $receiver_pid ]]; then
        kill "$receiver_pid" 2>/dev/null || true
        wait "$receiver_pid" 2>/dev/null || true
        receiver_pid=
    fi
}
trap stop_receiver EXIT

cat > "$scratch/xdg-open" <<'SH'
#!/bin/sh
printf '%s\t%s\n' "$#" "${1:-}" >> "$FILE_MANAGER_OPEN_LOG"
SH
chmod +x "$scratch/xdg-open"
export PATH="$scratch:$PATH"
export FILE_MANAGER_OPEN_LOG="$scratch/opens"
touch "$FILE_MANAGER_OPEN_LOG"
ulimit -c 0

invoke() {
    timeout 20s dotnet "$app" --linux "$1" "$directory" "$file"
}

wait_for_line() {
    local logfile=$1
    local count=$2
    for ((attempt = 0; attempt < 100; attempt++)); do
        if [[ $(wc -l < "$logfile") -ge $count ]]; then
            return
        fi
        sleep .05
    done
    printf 'Expected %s lines in %s\n' "$count" "$logfile" >&2
    return 1
}

start_receiver() {
    : > "$scratch/requests"
    "$scratch/receiver" "$1" > "$scratch/requests" &
    receiver_pid=$!
    wait_for_line "$scratch/requests" 1
    grep -qx ready "$scratch/requests"
}

invoke open-directory
wait_for_line "$FILE_MANAGER_OPEN_LOG" 1
invoke select-file
wait_for_line "$FILE_MANAGER_OPEN_LOG" 2
invoke select-directory
wait_for_line "$FILE_MANAGER_OPEN_LOG" 3
invoke invalid
printf '1\t%s\n' "$directory_uri" "$directory_uri" "file://$scratch" > "$scratch/expected-opens"
diff -u "$scratch/expected-opens" "$FILE_MANAGER_OPEN_LOG"
echo 'PASS: absent FileManager1 opens the directory or parent exactly once per valid request.'

start_receiver accept
invoke open-directory
invoke select-directory
invoke select-file
invoke select-relative-file
invoke invalid
{
    printf 'ready\n'
    printf 'ShowFolders\t1\t%s\t\n' "$directory_uri"
    printf 'ShowItems\t1\t%s\t\n' "$directory_uri" "$file_uri" "$file_uri"
} > "$scratch/expected-requests"
diff -u "$scratch/expected-requests" "$scratch/requests"
stop_receiver
echo 'PASS: ShowFolders and ShowItems received one exact URI and an empty startup ID.'

start_receiver reject
invoke rejected
printf 'ready\nShowItems\t1\t%s\t\n' "$file_uri" > "$scratch/expected-requests"
diff -u "$scratch/expected-requests" "$scratch/requests"
echo 'PASS: IOException retained the rejection from the service using a broker-style error name.'

# xdg-open is spawned in the background. Compare all launches after the D-Bus phases.
sleep .25
diff -u "$scratch/expected-opens" "$FILE_MANAGER_OPEN_LOG"
echo 'PASS: successful, rejected and invalid calls made no extra directory launches.'
