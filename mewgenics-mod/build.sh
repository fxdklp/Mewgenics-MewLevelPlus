#!/usr/bin/env bash
# MewLevelPlus 一键编译 + 部署脚本
# 用法：在 mewgenics-mod 目录下执行  bash build.sh
set -e

MSVC_VER="14.44.35207"
SDK_VER="10.0.26100.0"
VS_BIN="/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/$MSVC_VER/bin/Hostx64/x64"
LIB_MSVC="C:\\Program Files (x86)\\Microsoft Visual Studio\\2022\\BuildTools\\VC\\Tools\\MSVC\\$MSVC_VER\\lib\\x64"
LIB_UM="C:\\Program Files (x86)\\Windows Kits\\10\\Lib\\$SDK_VER\\um\\x64"
LIB_UCRT="C:\\Program Files (x86)\\Windows Kits\\10\\Lib\\$SDK_VER\\ucrt\\x64"
GAME="E:/SteamLibrary/steamapps/common/Mewgenics"
HERE="$(cd "$(dirname "$0")" && pwd)"

# 绕过 vcvarsall（其内部 reg.exe 被沙箱/黑名单拦截），手动指定链接器与库路径
export PATH="$VS_BIN:$PATH"
export LIB="$LIB_MSVC;$LIB_UM;$LIB_UCRT"

cd "$HERE/src"
dotnet publish -c Release -p:IlcUseEnvironmentalTools=true

cp "bin/Release/net8.0/win-x64/publish/MewLevelPlus.dll" "$GAME/Mods/MewLevelPlus.dll"
echo "built and deployed: $GAME/Mods/MewLevelPlus.dll"
