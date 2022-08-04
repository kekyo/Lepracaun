@echo off

rem Lepracaun - Varies of .NET Synchronization Context.
rem Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
rem
rem Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0

echo.
echo "==========================================================="
echo "Build Lepracaun"
echo.

rem git clean -xfd

dotnet restore
dotnet pack -p:Configuration=Release -o artifacts
