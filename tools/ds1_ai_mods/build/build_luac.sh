#!/usr/bin/env bash
# Builds a Linux luac that emits Dark Souls Remastered-compatible Lua 5.0 bytecode.
# DSR is x64 LLP64 (Windows): sizeof(Instruction)==4. Stock Lua 5.0 on Linux LP64
# would make it 8, so we force it to `unsigned int`. lua_Number stays double (8).
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
WORK="${1:-/tmp/lua-build}"
mkdir -p "$WORK" && cd "$WORK"
[ -f lua-5.0.2.tar.gz ] || curl -sSL -o lua-5.0.2.tar.gz https://www.lua.org/ftp/lua-5.0.2.tar.gz
rm -rf lua-5.0.2 && tar xzf lua-5.0.2.tar.gz
cd lua-5.0.2
sed -i 's/typedef unsigned long Instruction;/typedef unsigned int Instruction;/' src/llimits.h
INCS="-I include -I src -I src/lib"
SRCS=$(ls src/*.c src/lib/*.c | grep -v 'src/lua.c')
gcc -O2 $INCS -c $SRCS
gcc -O2 -DLUA_OPNAMES $INCS -c src/lopcodes.c -o lopcodes.o   # luac/print.c needs luaP_opnames
gcc -O2 $INCS -c src/luac/luac.c src/luac/print.c
gcc -O2 -o "$HERE/luac" *.o -lm
echo "Built $HERE/luac"
"$HERE/luac" -v
