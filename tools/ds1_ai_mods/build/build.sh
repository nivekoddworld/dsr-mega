#!/usr/bin/env bash
# End-to-end Linux build: source .lua -> bytecode -> repacked luabnd.dcx.
# Requires the original archive copied in (kept out of git; see README).
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
SRC="$ROOT/ds1_ai_mods/asylum_demon_slam.lua"
ORIG="${1:?usage: build.sh <path-to-original m18_01_00_00.luabnd.dcx>}"
OUT="$ROOT/ds1_ai_mods/dist/m18_01_00_00.luabnd.dcx"
[ -x "$HERE/luac" ] || "$HERE/build_luac.sh"
"$HERE/luac" -o /tmp/asylum_demon_slam.luac "$SRC"
dotnet run -c Release --project "$HERE/repack" -- "$ORIG" 223200_battle.lua /tmp/asylum_demon_slam.luac "$OUT"
echo "Done: $OUT"
