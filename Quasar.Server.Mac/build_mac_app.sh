#!/bin/bash

# Quasar Server macOS Application Builder
# This script builds a macOS .app bundle for Quasar Server

# Exit on error
set -e

echo "Building Quasar Server macOS App Bundle..."

# Get the directory where the script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# Define paths
APP_NAME="Quasar.app"
APP_CONTENTS="$SCRIPT_DIR/$APP_NAME/Contents"
APP_MACOS="$APP_CONTENTS/MacOS"
APP_RESOURCES="$APP_CONTENTS/Resources"
SERVER_BIN_DIR="$PROJECT_DIR/Quasar.Server/bin/Release"
COMMON_BIN_DIR="$PROJECT_DIR/Quasar.Common/bin/Release"

# Check if Mono is installed
if ! command -v mono &> /dev/null; then
    echo "Error: Mono is not installed. Please install Mono to build the macOS app."
    echo "You can install it using: brew install mono"
    exit 1
fi

# Check if the server has been built
if [ ! -f "$SERVER_BIN_DIR/Quasar.Server.exe" ]; then
    echo "Error: Quasar.Server.exe not found in $SERVER_BIN_DIR"
    echo "Please build the server project first."
    exit 1
fi

# Create app bundle structure
echo "Creating app bundle structure..."
mkdir -p "$APP_MACOS"
mkdir -p "$APP_RESOURCES"

# Copy Info.plist
cp "$SCRIPT_DIR/Info.plist" "$APP_CONTENTS/"

# Copy launcher script
cp "$SCRIPT_DIR/QuasarServer" "$APP_MACOS/"
chmod +x "$APP_MACOS/QuasarServer"

# Copy server binaries
echo "Copying server binaries..."
cp "$SERVER_BIN_DIR"/*.exe "$APP_RESOURCES/"
cp "$SERVER_BIN_DIR"/*.dll "$APP_RESOURCES/"
cp "$SERVER_BIN_DIR"/*.config "$APP_RESOURCES/"

# Copy common libraries if not already included
if [ -f "$COMMON_BIN_DIR/Quasar.Common.dll" ]; then
    cp -n "$COMMON_BIN_DIR/Quasar.Common.dll" "$APP_RESOURCES/"
fi

# Create an icon file placeholder (to be replaced with actual icon)
touch "$APP_RESOURCES/AppIcon.icns"

echo "App bundle created at: $SCRIPT_DIR/$APP_NAME"
echo ""
echo "To run the application:"
echo "1. Open Finder and navigate to $SCRIPT_DIR"
echo "2. Double-click on $APP_NAME"
echo ""
echo "Note: Make sure Mono is installed on the target system."
