# Razer Reader
[![Version number](https://img.shields.io/badge/version-1.1.1-ff6262)](https://github.com/mashirochan/FFXIV-RazerReader)
[![Download count](https://img.shields.io/endpoint?url=https%3A%2F%2Fqzysathwfhebdai6xgauhz4q7m0mzmrf.lambda-url.us-east-1.on.aws%2FRazerReader&color=%23ff6262)](https://github.com/mashirochan/FFXIV-RazerReader)
[![Availability](https://img.shields.io/badge/availability-stable-limegreen)](https://github.com/mashirochan/FFXIV-RazerReader)

![image](https://github.com/user-attachments/assets/047b0550-0209-44f0-a0a1-429a3a5ee27b)


A simple plugin that displays the remaining battery life in your wireless Razer devices.

Please note that this plugin requires [Razer Synapse v4](https://www.razer.com/synapse-4) to work, and _**only works with Razer devices**_. If I discover in the future that other peripheral brands report their battery levels in a similar way to Razer, I may include them as well in a larger, more ambitious plugin.

For any questions or bugs, please [Create an Issue](https://github.com/mashirochan/FFXIV-RazerReader/issues/new/choose).

## Configuration

* Window Enabled: Whether or not the Main Window is visible `(Default: YES)`
* Movable Window: Whether or not the Main Window is movable `(Default: NO)`
* Use Dalamud BG Style: Whether or not the user's Dalamud background style should be used `(Default: NO)`
* BG Opacity: The background opacity to use if the prior setting is disabled `(Default: 100)`
* Window Border: Whether or not the Main Window has a border `(Default: YES)`
* Color Percent: Whether or not the battery levels are colored `(Default: YES)`
* Show Battery Icon: Whether or not a battery level icon is visible `(Default: YES)`
* Show Device Name: Whether or not the device name is visible `(Default: YES)`
* Show DTR Entry: Whether or not the lowest battery device is displayed in the server bar `(Default: NO)`
* Low Battery Notification: Whether or not a low battery notification is displayed `(Default: YES)`
* Low Battery Level: The percent at which a device is considered low battery `(Default: 15)`
* Individual Devices: Once devices are detected, they can be toggled off to not be shown in the Main Window

## How To Use

### Prerequisites

Razer Reader assumes all the following prerequisites are met:

* XIVLauncher, FINAL FANTASY XIV, and Dalamud have all been installed and the game has been run with Dalamud at least once.
* XIVLauncher is installed to its default directories and configurations.
* Razer Synapse v4 is installed.
* Valid logs exist in `%localappdata%\Razer\RazerAppEngine\User Data\Logs\systray_systrayv2.log`.

### Activating in-game

1. Launch the game and use `/xlplugins` in chat or `xlplugins` in the Dalamud Console to open up the Dalamud Plugin Installer.
    * In here, go to `All Plugins`, and search for `Razer Reader`. Click Install.
3. You should now be able to use `/prazer` (chat) or `prazer` (console) to open the Razer Reader config!
