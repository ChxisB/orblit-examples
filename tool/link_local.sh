#!/bin/bash
# Points the examples at sibling checkouts instead of git dependencies, so a
# change to the engine, to networking or to scripting can be tried here before
# it is pushed.
#
# This is the visible cost of separate repositories. It is one command, and the
# overrides are not committed, so nobody ships a build wired to a local path.
#
# It is also what makes these apps buildable in CI at all: the git dependencies
# are SSH URLs, which a runner has no key for. Checking the repositories out
# and pointing at them is how CI resolves them without handing a runner a
# deploy key.
#
# Usage: link_local.sh [engine] [networking] [scripting]
#
# Only the engine is required. An app whose dependencies are not all present is
# skipped and said so, rather than failing the whole run — somebody building
# only the viewport should not need a networking checkout, and CI's renderer
# job clones one repository instead of three.
set -euo pipefail
cd "$(dirname "$0")/.."

resolve() { (cd "$1" 2>/dev/null && pwd) || echo ""; }

ENGINE=$(resolve "${1:-../orbis}")
NET=$(resolve "${2:-../orbis-net}")
SCRIPT=$(resolve "${3:-../orbis-script}")

if [ -z "$ENGINE" ]; then
  echo "No engine checkout at ${1:-../orbis}. Clone Orbis-Engine/orbis beside this one."
  exit 1
fi

linked=()

# The simulation is the only thing here that needs networking.
if [ -n "$NET" ]; then
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
  (cd simulation && dart pub get > /dev/null)
  printf '  %-10s -> %s, %s\n' simulation "$ENGINE" "$NET"
else
  rm -f simulation/pubspec_overrides.yaml
  echo "  simulation skipped: no networking checkout at ${2:-../orbis-net}"
fi

# The gallery shows the scripted examples, so it needs scripting as well.
if [ -n "$SCRIPT" ]; then
  cat > gallery/pubspec_overrides.yaml <<YAML
# Written by tool/link_local.sh. Not committed.
dependency_overrides:
  orbis_camera:
    path: $ENGINE/packages/orbis_camera
  orbis_examples:
    path: $ENGINE/packages/orbis_examples
  orbis_filament:
    path: $ENGINE/packages/orbis_filament
  orbis_light:
    path: $ENGINE/packages/orbis_light
  orbis_weather:
    path: $ENGINE/packages/orbis_weather
  orbis_script:
    path: $SCRIPT/packages/orbis_script
  orbis_script_scene:
    path: $SCRIPT/packages/orbis_script_scene
  orbis_script_ui:
    path: $SCRIPT/packages/orbis_script_ui
YAML
  linked+=(gallery)
else
  rm -f gallery/pubspec_overrides.yaml
  echo "  gallery skipped: no scripting checkout at ${3:-../orbis-script}"
fi

cat > viewport/pubspec_overrides.yaml <<YAML
# Written by tool/link_local.sh. Not committed.
dependency_overrides:
  orbis_filament:
    path: $ENGINE/packages/orbis_filament
YAML
linked+=(viewport)

# The gallery and the viewport are Flutter apps, so they resolve with Flutter
# rather than Dart.
if command -v flutter > /dev/null; then
  for app in "${linked[@]}"; do
    (cd "$app" && flutter pub get > /dev/null)
    printf '  %-10s -> %s\n' "$app" "$ENGINE"
  done
else
  echo "  ${linked[*]} skipped: no flutter on PATH"
fi
