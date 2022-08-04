#!/bin/sh

# Lepracaun - Varies of .NET Synchronization Context.
# Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
#
# Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0

echo ""
echo "==========================================================="
echo "Build Lepracaun"
echo ""

# git clean -xfd

dotnet restore
dotnet pack -p:Configuration=Release -o artifacts
