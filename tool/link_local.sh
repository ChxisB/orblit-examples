#!/bin/bash
# Points the examples at sibling checkouts instead of git dependencies, so a
# change to the engine or to networking can be tried here before it is pushed.
#
# This is the visible cost of separate repositories. It is one command, and the
# overrides are not committed, so nobody ships a build wired to a local path.
set -euo pipefail
cd "$(dirname "$0")/.."

SCRIPT=$(cd "${3:-../orbis-script}" 2>/dev/null && pwd) || SCRIPT=""
ENGINE=$(cd "${1:-../orbis}" 2>/dev/null && pwd) || {
  echo "No engine checkout at ${1:-../orbis}. Clone Orbis-Engine/orbis beside this one."
  exit 1
}
NET=$(cd "${2:-../orbis-net}" 2>/dev/null && pwd) || {
  echo "No networking checkout at ${2:-../orbis-net}. Clone Orbis-Engine/orbis-net beside this one."
  exit 1
}

cat > simulation/pubspec_overrides.yaml <<YAML
# Written by tool/link_local.sh. Not committed.
dependency_overrides:
  orbis_core:
    path: $ENGINE/packages/orbis_core
  orbis_codegen:
    path: $ENGINE/packages/orbis_codegen
  orbis_net:
    path: $NET
YAML

cat > gallery/pubspec_overrides.yaml <<YAML
# Written by tool/link_local.sh. Not committed.
dependency_overrides:
  orbis_camera:
    path: $ENGINE/packages/orbis_camera
  orbis_filament:
    path: $ENGINE/packages/orbis_filament
  orbis_light:
    path: $ENGINE/packages/orbis_light
  orbis_weather:
    path: $ENGINE/packages/orbis_weather
${SCRIPT:+  orbis_script_ui:
    path: $SCRIPT/packages/orbis_script_ui}
YAML

cat > viewport/pubspec_overrides.yaml <<YAML
# Written by tool/link_local.sh. Not committed.
dependency_overrides:
  orbis_filament:
    path: $ENGINE/packages/orbis_filament
YAML

(cd simulation && dart pub get > /dev/null)
echo "  simulation -> $ENGINE, $NET"

# The gallery and the viewport are Flutter apps, so they resolve with Flutter
# rather than Dart.
if command -v flutter > /dev/null; then
  for app in gallery viewport; do
    (cd "$app" && flutter pub get > /dev/null)
    printf '  %-10s -> %s\n' "$app" "$ENGINE"
  done
else
  echo "  gallery, viewport skipped: no flutter on PATH"
fi
