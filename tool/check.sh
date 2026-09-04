#!/bin/bash
# Analyzes and tests the examples that need no window.
#
# The viewport is excluded: it needs Flutter, a macOS host and a Filament
# download, and it is checked by building it rather than by a test suite.
set -uo pipefail
cd "$(dirname "$0")/.."

failures=0

echo "== simulation =="
(cd simulation && dart pub get > /dev/null 2>&1)

if (cd simulation && dart analyze > /tmp/orbis_ex_analyze.log 2>&1); then
  echo "  ok    analyze"
else
  echo "  FAIL  analyze"; tail -20 /tmp/orbis_ex_analyze.log; failures=$((failures+1))
fi

if (cd simulation && dart test > /tmp/orbis_ex_test.log 2>&1); then
  summary=$(tr '\r' '\n' < /tmp/orbis_ex_test.log | tail -1 \
    | sed -e 's/\x1b\[[0-9;]*m//g' -e 's/^[0-9:]* //')
  echo "  ok    $summary"
else
  echo "  FAIL  tests"; tail -25 /tmp/orbis_ex_test.log; failures=$((failures+1))
fi

echo
[ "$failures" -eq 0 ] && echo "everything green" || echo "$failures failing step(s)"
exit "$failures"
