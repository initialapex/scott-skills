#!/usr/bin/env bash
# Build and run the host-side unit tests with gcc or clang (Linux, macOS, Git Bash, CI).
# The code under test depends on interfaces only, so no hardware or vendor SDK is needed.
set -eu

root="$(cd "$(dirname "$0")" && pwd)"
out="${TMPDIR:-/tmp}/fw-host-test"
mkdir -p "$out"

cc="${CC:-}"
if [ -z "$cc" ]; then
    if command -v gcc >/dev/null 2>&1; then cc=gcc
    elif command -v clang >/dev/null 2>&1; then cc=clang
    else echo "No C compiler found (set CC, or install gcc or clang)." >&2; exit 1
    fi
fi

# Address and undefined-behavior sanitizers catch overruns and bad casts for free on a PC.
"$cc" -std=c99 -Wall -Wextra -Wpedantic -Werror -g -fsanitize=address,undefined \
    -I"$root/common" -I"$root/hal" -I"$root/drivers" -I"$root/services" -I"$root/tests" \
    "$root/drivers/drv_tmp102.c" \
    "$root/services/svc_thermal_monitor.c" \
    "$root/tests/fw_test.c" \
    "$root/tests/mock_i2c.c" \
    "$root/tests/test_tmp102.c" \
    "$root/tests/test_thermal_monitor.c" \
    "$root/tests/test_main.c" \
    -o "$out/fw_tests"

"$out/fw_tests"
