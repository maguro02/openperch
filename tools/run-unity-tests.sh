#!/usr/bin/env bash
# Runs, on macOS, the Unity test suites that CI cannot run:
#   app-editmode, app-playmode   app/ on Unity 2022.3.62f3
#   harness-editmode             fixtures/ExporterHarness on Unity 2022.3.22f1
#   harness-6000.3-compile       fixtures/ExporterHarness compiled on Unity 6000.3 (skipped when no 6000.3 is installed)
#
# Each editor comes from an environment variable (UNITY_2022_3_62F3, UNITY_2022_3_22F1, UNITY_6000_3) holding either
# the Unity.app bundle or the Unity binary inside it, or else from Unity Hub's default install folder. A variable that
# is set must name an editor; the suites that need it fail otherwise.
# NUnit XML results and editor logs are written to out/test-results/. The exit status is non-zero when a suite fails
# or a required editor is missing.

set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/out/test-results"
HUB_EDITORS="/Applications/Unity/Hub/Editor"
STATUS=0
SUMMARY=()
UNITY=""
UNITY_PROBLEM=""
WORK=""

# The compile check's copy grows a Library of several GB, so it is removed however the script ends.
remove_work() {
  if [ -n "$WORK" ]; then
    rm -rf "$WORK"
    WORK=""
  fi
}
trap remove_work EXIT
trap 'exit 129' HUP
trap 'exit 130' INT
trap 'exit 143' TERM

# Prints the Unity binary of an editor given as Unity.app or as the binary itself.
unity_binary() {
  local candidate="${1%/}"
  case "$candidate" in
    *.app) candidate="$candidate/Contents/MacOS/Unity" ;;
  esac
  [ -x "$candidate" ] || return 1
  printf '%s\n' "$candidate"
}

# Sets UNITY to the editor named by the variable, or, when the variable is not set, to the one of that version in
# Unity Hub's default location. Fails with the reason in UNITY_PROBLEM.
resolve_unity() {
  local variable="$1" version="$2" override
  override="${!variable:-}"
  if [ -n "$override" ]; then
    UNITY="$(unity_binary "$override")" && return 0
    UNITY_PROBLEM="$variable is set but is not a Unity editor: $override"
    return 1
  fi
  UNITY="$(unity_binary "$HUB_EDITORS/$version/Unity.app")" && return 0
  UNITY_PROBLEM="Unity $version not found; install it with Unity Hub or set $variable"
  return 1
}

# Any 6000.3 patch in Unity Hub's default location will do for a compile check.
find_hub_unity_6000_3() {
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
  local log="$OUT/$name.log" code
  if [ ! -d "$ROOT/$project_rel" ]; then
    fail "$name" "project not found: $ROOT/$project_rel"
    return
  fi
  if ! WORK="$(mktemp -d "${TMPDIR:-/tmp}/openperch-compile.XXXXXX")"; then
    WORK=""
    fail "$name" "could not create a temporary folder"
    return
  fi
  # A newer editor upgrades the project it opens, so it gets a copy. packages/ is copied next to it so that the
  # manifest's relative file: paths still resolve.
  mkdir -p "$WORK/$project_rel"
  rsync -a --exclude '/Library/' --exclude '/Temp/' --exclude '/Logs/' --exclude '/obj/' --exclude '/UserSettings/' \
    "$ROOT/$project_rel/" "$WORK/$project_rel/"
  rsync -a "$ROOT/packages/" "$WORK/packages/"
  echo "==> $name: compiling $project_rel"
  "$unity" -batchmode -nographics -quit -projectPath "$WORK/$project_rel" -logFile "$log"
  code=$?
  remove_work
  if [ "$code" -eq 0 ]; then
    record "$name" "PASS"
  else
    fail "$name" "exit code $code, see $log"
  fi
}

mkdir -p "$OUT"

if resolve_unity UNITY_2022_3_62F3 2022.3.62f3; then
  run_tests app-editmode "$UNITY" "$ROOT/app" EditMode
  run_tests app-playmode "$UNITY" "$ROOT/app" PlayMode
else
  fail app-editmode "$UNITY_PROBLEM"
  fail app-playmode "$UNITY_PROBLEM"
fi

if resolve_unity UNITY_2022_3_22F1 2022.3.22f1; then
  run_tests harness-editmode "$UNITY" "$ROOT/fixtures/ExporterHarness" EditMode
else
  fail harness-editmode "$UNITY_PROBLEM"
fi

# Unlike the 2022.3 editors this one is optional, but an editor asked for by UNITY_6000_3 has to exist.
if [ -n "${UNITY_6000_3:-}" ]; then
  if unity_6000="$(unity_binary "$UNITY_6000_3")"; then
    compile_check harness-6000.3-compile "$unity_6000" fixtures/ExporterHarness
  else
    fail harness-6000.3-compile "UNITY_6000_3 is set but is not a Unity editor: $UNITY_6000_3"
  fi
elif unity_6000="$(find_hub_unity_6000_3)"; then
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
