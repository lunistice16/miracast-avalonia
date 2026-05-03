#!/bin/bash
set -e

echo "========================================="
echo " MiraQt Linux Environment Updater"
echo "========================================="

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "1. Pulling latest MiraQt code..."
cd "$DIR"
git pull origin main

echo ""
echo "2. Applying patches and rebuilding GNOME Network Displays daemon..."
cd "$DIR/../gnome-network-displays"

# Ensure we have a clean state by attempting to reverse the patch first (ignoring errors if it's not applied)
patch -R -p0 < "$DIR/MiraQt/Patches/02-disable-periodic-scan.patch" 2>/dev/null || true

# Apply the new patch
echo "Applying 02-disable-periodic-scan.patch..."
patch -p0 < "$DIR/MiraQt/Patches/02-disable-periodic-scan.patch"

echo "Compiling and installing daemon..."
meson compile -C build
sudo meson install -C build

echo ""
echo "3. Cleaning up old daemons..."
killall gnome-network-displays-daemon 2>/dev/null || true

echo "========================================="
echo " Update complete! You can now run MiraQt."
echo " cd \"$DIR\" && dotnet run --project MiraQt"
echo "========================================="
