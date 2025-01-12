# JumpPoint

JumpPoint is a Windows application that allows users to quickly access their shortcuts using a global hotkey (ALT + SPACE).

## Features

- Global hotkey (ALT + SPACE) to show the application
- Displays and executes shortcuts from a directory
- Supports arguments

## How It Works

1. **Open Shortcuts Directory**: Press `F1` to open the directory where shortcuts can be located. You can use `.lnk` shortcuts or `.ini` files.

**INI File Structure**: The `.ini` file should have the following structure:
    ```ini
    [Shortcut]
    Name=Shortcut Name
    path=C:\Path\To\Executable.exe
    arguments=--example-argument $$
    ```

The `arguments` field allows you to pass command-line arguments to the executable when the shortcut is activated. $$ is a wildcard and can be entered by the user.

2. **Global Hotkey**: The application registers a global hotkey (ALT + SPACE) that shows the application when pressed.

3. **Focus Handling**: The application hides itself when it loses focus by handling the `Deactivated` event.

4. **Taskbar Visibility**: The application is configured not to show in the taskbar by setting the `ShowInTaskbar` property to `false`.

5. **Shortcut Management**: The application loads shortcuts from a specified directory and displays them in a list. Users can interact with the list to search and select shortcuts.

## Installation

1. Clone the repository:
   ```sh
   git clone https://github.com/bstudtma/jump-point.git
   cd jump-point

dotnet build --configuration Release

dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true --self-contained false

## Usage

Run the published executable from the `bin/Release/net5.0/win-x64/publish` directory. The application will start minimized to the system tray. Use the `ALT + SPACE` hotkey to show the application. Interact with the system tray icon to open or exit the application. The application will hide itself when it loses focus.

Use the `ALT + F4` hotkey to close the application.

Use `F1` to open the configuration directory.

## Requirements

- .NET 7.0 or later installed on the target machine.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License

This project is licensed under the MIT License. See the LICENSE file for details.
