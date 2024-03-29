# Windows 10 Mobile Toolbox

A desktop tool to help with various task and configurations with Windows Phones. Note this is currently *in development* software!

<img src="1.png" width="400" height="225"> <img src="2.png" width="400" height="225">
<img src="3.png" width="400" height="225"> <img src="4.png" width="400" height="225">


### Updated:
Fixes:
- Tried to improve detection on app load if device is plugged in already.
- Fixed device info not being found for certain devices with Nokia branding.
- Fixed issue where registry wouldn't mount due to some devices missing certain registry values by default.
- Fixed issue with Updater telling user files need to be downloaded when the files are already downloaded.

New feature:
- Added first test of a Registry tab allowing users to modify specific registry values. (This is WIP)

(NOTE: Disk reading is slow on my tests, I plan to look at improving this. Also there way be a few bugs, this is my first time using disk management.)


# What can it do?
### General:
- Manage booting into different states (Flash, Mass Storage and Normal)
- Push Updates to your device (10549 > 15254)

### Modify Device Settings:
 - Windows Update
 - Windows Firewall
 - Page File
 - Dev Mode
 - Device Portal
 - C:\ MTP Access
 - Local Crash Dumps
 - Flight Signing

### Backup device
- Seperate Partitions
- Full EMMC backup

### FFU Conversion
- WP8.x FFU Conversion to VHD
- WP8.x FFU Manifest/Viewable Info
- W10M FFU Manifest/Viewable Info

### Registry
- Modify user selected values in HKLM\SYSTEM and HKLM\SOFTWARE

More to be added

## Notes:

- Third party tools are used with this tool, all rights reserved to the owners.
- This tool includes "iutool" suite and "thor2.exe"
- WPInternals is required to automate various tasks, the latest will be downloaded through the app, and saved in the apps's data folder
- Uses DiscUtils nuget for vhd handling
- Uses WindowsAPICodePack for OpenFolder dialog
