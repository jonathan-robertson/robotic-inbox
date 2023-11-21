# Robotic Inbox

[![🧪 Tested On](https://img.shields.io/badge/🧪%20Tested%20On-A21.2%20b30-blue.svg)](https://7daystodie.com/) [![📦 Automated Release](https://github.com/jonathan-robertson/robotic-inbox/actions/workflows/release.yml/badge.svg)](https://github.com/jonathan-robertson/robotic-inbox/actions/workflows/release.yml)

- [Robotic Inbox](#robotic-inbox)
  - [Summary](#summary)
    - [Support](#support)
  - [Performance Considerations](#performance-considerations)
  - [Auto-Sorting](#auto-sorting)
  - [Placement/Configuration Requirements](#placementconfiguration-requirements)
  - [Setup](#setup)
    - [Environment / EAC / Hosting Requirements](#environment--eac--hosting-requirements)
    - [Map Considerations for Installation or Uninstallation](#map-considerations-for-installation-or-uninstallation)
    - [Windows/Linux Installation (Server via FTP from Windows PC)](#windowslinux-installation-server-via-ftp-from-windows-pc)
    - [Linux Server Installation (Server via SSH)](#linux-server-installation-server-via-ssh)

## Summary

A special container that automatically sorts and distributes items to other nearby storage containers.

### Support

🗪 If you would like support for this mod, please feel free to reach out to me via [Discord](https://discord.gg/hYa2sNHXya) (my username is `kanaverum`).

## Performance Considerations

As you might imagine, interacting instantly with multiple containers, sorting them, and distributing items to them could all be very taxing on the system.

But your performance should be stellar - here are some things to consider and some things this mod intentionally does to keep game servers running smoothly:

1. Container data is already processed server-side in 7 days to die. This means that adjustments to storage are *most* performant on the server's end rather than on the client's end and this approach actually reduces networking calls vs players organizing/stacking items themselves.
2. Container organization is run on each box within range via a *concurrent* loop. This ensures that as inboxes are working through your players' containers, the server can still process other tasks and avoid lagging.

## Auto-Sorting

> 📝 Players will find this information in their journals under `Robotic Inbox (General Info) [MOD]`

The `Secure Robotic Inbox` is a new block meant to streamline item/resource organization within a base and help you to get back into the action as quickly as possible.

When placed **within an LCB**, it will automatically send all of the items it receives to nearby player-placed storage containers up to 5 meters away in any direction (so long as they're within the same LCB).

For security reasons, **the Inbox will need to use the same password as any locked storage containers it tries to organize**.

⚠️ While any secure or insecure player-placed storage container will work with the Inbox, **Writable Storage Boxes will describe how the Inbox is interacting with them**, making them the recommended type of container to place near an Inbox.

Any items in the Inbox that are not able to be matched with another container will be left there until you have time to decide which container to store them in.

This new Inbox can be crafted at the Workbench or found for sale by a trader whenever you're ready.

## Placement/Configuration Requirements

> 📝 Players will find this information in their journals under `Robotic Inbox (Security Info) [MOD]`

The `Secure Robotic Inbox` has certain requirements depending on the state of the containers it interacts with.

⚠️ Using `Writable Storage Boxes` will make configuring your storage area far easier, thanks to the feedback text that will appear when the Inbox attempts to interact with them.

> ℹ️ In versions prior to 1.3.0, the original text on Writable Storage Boxes could be permanently lost if the temporary text was showing while the last remaining player in the area logged out or the server restarted. As of version 1.3.0 and beyond, this is no longer an issue.

If the following requirements aren't met, the Inbox will have to skip interactions with the target containers until the necessary adjustments are made:

1. containers not placed by players (ex: POI containers) are ignored.
2. player backpacks and vehicles are ignored.
3. both Inbox and target container must be within the same LCB area.
4. target container must be within 5 meters of Inbox, both horizontally and vertically (imagine a cube 11x11 in size with the inbox at the center).
5. if target container is locked, Inbox must also be lockable, in a locked state, and set to the same password as target container.

## Setup

Without proper installation, this mod will not work as expected. Using this guide should help to complete the installation properly.

If you have trouble getting things working, you can reach out to me for support via [Support](#support).

### Environment / EAC / Hosting Requirements

Environment | Compatible | Does EAC Need to be Disabled? | Who needs to install?
--- | --- | --- | ---
Dedicated Server | Yes | no | only server
Peer-to-Peer Hosting | [Not Yet](https://github.com/jonathan-robertson/robotic-inbox/issues/29) | only on the host | only the host
Single Player Game | [Not Yet](https://github.com/jonathan-robertson/robotic-inbox/issues/29) | Yes | self (of course)

> 🤔 If you aren't sure what some of this means, details steps are provided below to walk you through the setup process.

### Map Considerations for Installation or Uninstallation

- Does **adding** this mod require a fresh map?
  - No! You can drop this mod into an ongoing map without any trouble.
- Does **removing** this mod require a fresh map?
  - Since this mod adds new blocks, removing it from a map could cause some issues (previously placed robotic inbox blocks would now throw exceptions in your logs, at the very least).
  - Journal Entries added to the player Journal will unfortunately remain with their Localization stubs (but will otherwise have no text within them and will not impact gameplay at all).

### Windows/Linux Installation (Server via FTP from Windows PC)

1. 📦 Download the latest release by navigating to [this link](https://github.com/jonathan-robertson/robotic-inbox/releases/latest/) and clicking the link for `robotic-inbox.zip`
2. 📂 Unzip this file to a folder named `robotic-inbox` by right-clicking it and choosing the `Extract All...` option (you will find Windows suggests extracting to a new folder named `robotic-inbox` - this is the option you want to use)
3. 🕵️ Locate and create your mods folder (if missing):
    - Windows PC or Server: in another window, paste this address into to the address bar: `%APPDATA%\7DaysToDie`, then enter your `Mods` folder by double-clicking it. If no `Mods` folder is present, you will first need to create it, then enter your `Mods` folder after that
    - FTP: in another window, connect to your server via FTP and navigate to the game folder that should contain your `Mods` folder (if no `Mods` folder is present, you will need to create it in the appropriate location), then enter your `Mods` folder. If you are confused about where your mods folder should go, reach out to your host.
4. 🚚 Move this new `robotic-inbox` folder into your `Mods` folder by dragging & dropping or cutting/copying & pasting, whichever you prefer
5. ♻️ Restart your server to allow this mod to take effect and monitor your logs to ensure it starts successfully:
    - you can search the logs for the word `RoboticInbox`; the name of this mod will appear with that phrase and all log lines it produces will be presented with this prefix for quick reference

### Linux Server Installation (Server via SSH)

1. 🔍 [SSH](https://www.digitalocean.com/community/tutorials/how-to-use-ssh-to-connect-to-a-remote-server) into your server and navigate to the `Mods` folder on your server
    - if you installed 7 Days to Die with [LinuxGSM](https://linuxgsm.com/servers/sdtdserver/) (which I'd highly recommend), the default mods folder will be under `~/serverfiles/Mods` (which you may have to create)
2. 📦 Download the latest `robotic-inbox.zip` release from [this link](https://github.com/jonathan-robertson/robotic-inbox/releases/latest/) with whatever tool you prefer
    - example: `wget https://github.com/jonathan-robertson/robotic-inbox/releases/latest/download/robotic-inbox.zip`
3. 📂 Unzip this file to a folder by the same name: `unzip robotic-inbox.zip -d robotic-inbox`
    - you may need to install `unzip` if it isn't already installed: `sudo apt-get update && sudo apt-get install unzip`
    - once unzipped, you can remove the robotic-inbox download with `rm robotic-inbox.zip`
4. ♻️ Restart your server to allow this mod to take effect and monitor your logs to ensure it starts successfully:
    - you can search the logs for the word `RoboticInbox`; the name of this mod will appear with that phrase and all log lines it produces will be presented with this prefix for quick reference
    - rather than monitoring telnet, I'd recommend viewing the console logs directly because mod and DLL registration happens very early in the startup process and you may miss it if you connect via telnet after this happens
    - you can reference your server config file to identify your logs folder
    - if you installed 7 Days to Die with [LinuxGSM](https://linuxgsm.com/servers/sdtdserver/), your console log will be under `log/console/sdtdserver-console.log`
    - I'd highly recommend using `less` to open this file for a variety of reasons: it's safe to view active files with, easy to search, and can be automatically tailed/followed by pressing a keyboard shortcut so you can monitor logs in realtime
      - follow: `SHIFT+F` (use `CTRL+C` to exit follow mode)
      - exit: `q` to exit less when not in follow mode
      - search: `/RoboticInbox` [enter] to enter search mode for the lines that will be produced by this mod; while in search mode, use `n` to navigate to the next match or `SHIFT+n` to navigate to the previous match
