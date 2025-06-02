# Quasar Server for macOS

This directory contains the files necessary to run Quasar Server as a native macOS application.

## Requirements

- macOS 10.12 (Sierra) or later
- Mono Framework 6.0 or later

## Installation

### Installing Mono

Quasar Server on macOS requires the Mono framework. You can install it using:

1. **Homebrew** (recommended):
   ```
   brew install mono
   ```

2. **Direct download**:
   Download the Mono installer from the [official website](https://www.mono-project.com/download/stable/).

## Building the macOS Application Bundle

1. First, build the Quasar.Server project in Release mode
2. Navigate to this directory in Terminal
3. Run the build script:
   ```
   ./build_mac_app.sh
   ```
4. The script will create a `Quasar.app` application bundle

## Running the Application

Simply double-click the `Quasar.app` bundle to run the application.

Alternatively, you can run it from the terminal:
```
open Quasar.app
```

## Troubleshooting

1. **Application won't open**:
   - Make sure Mono is installed correctly
   - Try running from the terminal to see error messages:
     ```
     ./Quasar.app/Contents/MacOS/QuasarServer
     ```

2. **Missing permissions**:
   - Right-click on the app bundle and select "Open" the first time to bypass Gatekeeper

## Known Issues and Limitations

- Some Windows-specific features may not be available on macOS
- The UI uses Mono's WinForms implementation, which may have slight visual differences

## Adding a Custom Icon

To replace the default icon:
1. Create an .icns file (macOS icon format)
2. Replace the `AppIcon.icns` file in `Quasar.app/Contents/Resources/`

## Directory Structure

The macOS app bundle has the following structure:

```
Quasar.app/
├── Contents/
│   ├── Info.plist         # Application metadata
│   ├── MacOS/
│   │   └── QuasarServer   # Launch script
│   └── Resources/
│       ├── AppIcon.icns   # Application icon
│       └── *.exe, *.dll   # Application binaries
```
