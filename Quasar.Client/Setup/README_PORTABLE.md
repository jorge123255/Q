# Quasar Client - Portable Mode

This document explains how Quasar Client works in portable mode.

## What is Portable Mode?

Portable mode allows you to run the Quasar Client without installing it on the system. This means:

- No files are copied to AppData or Program Files
- No registry entries are created
- No startup items are added
- All settings and logs are kept in a 'data' folder next to the executable

## How Portable Mode Works

The Quasar Client automatically detects if it's running from a non-standard installation location and enters portable mode by default. Specifically:

1. If the client executable is not located in AppData, Program Files, or Program Files (x86), it runs in portable mode
2. A folder named `data` is created next to the executable to store configuration and logs
3. All data is stored locally rather than in system locations
4. The client will not attempt to install itself or add startup entries

## Using the Portable Client

Using the portable client is incredibly simple:

1. Copy the Quasar Client executable to any location (e.g., a USB drive, a regular folder)
2. Run the executable directly
3. The client will automatically run in portable mode

That's it! No special setup or configuration required.

## Advantages of Portable Mode

- Easy to use on different systems without installation
- Self-contained within a single directory
- Can be run from removable media like USB drives
- No elevation required (in most cases)
- Leaves no traces in the Windows registry or system folders

## Notes on Security

- The portable mode still requires the necessary permissions to function
- Some advanced features might have limited functionality depending on user rights
- For full functionality, it's recommended to run the client with appropriate permissions
