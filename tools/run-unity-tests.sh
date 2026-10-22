#!/usr/bin/env bash
# Runs, on macOS, the Unity test suites that CI cannot run:
#   app-editmode, app-playmode   app/ on Unity 2022.3.62f3
#   harness-editmode             fixtures/ExporterHarness on Unity 2022.3.22f1
#   harness-6000.3-compile       fixtures/ExporterHarness compiled on Unity 6000.3 (skipped when no 6000.3 is installed)
#
# Each editor comes from an environment variable (UNITY_2022_3_62F3, UNITY_2022_3_22F1, UNITY_6000_3) holding either
# the Unity.app bundle or the Unity binary inside it, or else from Unity Hub's default install folder.
# NUnit XML results and editor logs are written to out/test-results/. The exit status is non-zero when a suite fails
# or a required editor is missing.

set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/out/test-results"
HUB_EDITORS="/Applications/Unity/Hub/Editor"
STATUS=0
SUMMARY=()

# Prints the Unity binary of an editor given as Unity.app or as the binary itself.
unity_binary() {
  local candidate="${1%/}"
  case "$candidate" in
    *.app) candidate="$candidate/Contents/MacOS/Unity" ;;
  esac
  [ -x "$candidate" ] || return 1
  printf '%s\n' "$candidate"
}

# Prints the editor to use for a version: the override when set, otherwise Unity Hub's default location.
find_unity() {
  local override="$1" version="$2"
  if [ -n "$override" ]; then
    unity_binary "$override"
  else
    unity_binary "$HUB_EDITORS/$version/Unity.app"
  fi
}

# Any 6000.3 patch will do for a compile check.
find_unity_6000_3() {
  if [ -n "${UNITY_6000_3:-}" ]; then
    unity_binary "$UNITY_6000_3"
    return
  fi
  local candidate found=""
  for candidate in "$HUB_EDITORS"/6000.3.*/Unity.app; do
    if [ -d "$candidate" ]; then
      found="$candidate"
    fi
  done
  [ -n "$found" ] || return 1
  unity_binary "$found"
}

record() {
  SUMMARY+=("$(printf '%-24s %s' "$1" "$2")")
}

fail() {
  record "$1" "FAIL: $2"
  STATUS=1
}

run_tests() {
  local name="$1" unity="$2" project="$3" platform="$4"
  local results="$OUT/$name.xml" log="$OUT/$name.log" code
  if [ ! -d "$project" ]; then
    fail "$name" "project not found: $project"
    return
  fi
  rm -f "$results"
  echo "==> $name: $platform tests of ${project#"$ROOT"/}"
  # -runTests quits the editor by itself; adding -quit would stop it before the tests run.
  "$unity" -batchmode -projectPath "$project" -runTests -testPlatform "$platform" -testResults "$results" -logFile "$log"
  code=$?
  if [ "$code" -eq 0 ] && [ -f "$results" ]; then
    record "$name" "PASS"
  else
    fail "$name" "exit code $code, see $log"
  fi
}

compile_check() {
  local name="$1" unity="$2" project_rel="$3"
  local log="$OUT/$name.log" work code
  if [ ! -d "$ROOT/$project_rel" ]; then
    fail "$name" "project not found: $ROOT/$project_rel"
    return
  fi
  if ! work="$(mktemp -d "${TMPDIR:-/tmp}/openperch-compile.XXXXXX")"; then
    fail "$name" "could not create a temporary folder"
    return
  fi
  # A newer editor upgrades the project it opens, so it gets a copy. packages/ is copied next to it so that the
  # manifest's relative file: paths still resolve.
  mkdir -p "$work/$project_rel"
  rsync -a --exclude '/Library/' --exclude '/Temp/' --exclude '/Logs/' --exclude '/obj/' --exclude '/UserSettings/' \
    "$ROOT/$project_rel/" "$work/$project_rel/"
  rsync -a "$ROOT/packages/" "$work/packages/"
  echo "==> $name: compiling $project_rel"
  "$unity" -batchmode -nographics -quit -projectPath "$work/$project_rel" -logFile "$log"
  code=$?
  rm -rf "$work"
  if [ "$code" -eq 0 ]; then
    record "$name" "PASS"
  else
    fail "$name" "exit code $code, see $log"
  fi
}

mkdir -p "$OUT"

if unity_app="$(find_unity "${UNITY_2022_3_62F3:-}" 2022.3.62f3)"; then
  run_tests app-editmode "$unity_app" "$ROOT/app" EditMode
  run_tests app-playmode "$unity_app" "$ROOT/app" PlayMode
else
  fail app-editmode "Unity 2022.3.62f3 not found; install it with Unity Hub or set UNITY_2022_3_62F3"
  fail app-playmode "Unity 2022.3.62f3 not found; install it with Unity Hub or set UNITY_2022_3_62F3"
fi

if unity_harness="$(find_unity "${UNITY_2022_3_22F1:-}" 2022.3.22f1)"; then
  run_tests harness-editmode "$unity_harness" "$ROOT/fixtures/ExporterHarness" EditMode
else
  fail harness-editmode "Unity 2022.3.22f1 not found; install it with Unity Hub or set UNITY_2022_3_22F1"
fi

if unity_6000="$(find_unity_6000_3)"; then
  compile_check harness-6000.3-compile "$unity_6000" fixtures/ExporterHarness
else
  record harness-6000.3-compile "SKIP: no Unity 6000.3 found; install one or set UNITY_6000_3"
fi

echo
echo "Results and logs: $OUT"
for line in ${SUMMARY[@]+"${SUMMARY[@]}"}; do
  echo "  $line"
done
exit "$STATUS"
