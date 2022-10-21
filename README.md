# Robotic Inbox

[![🧪 Tested On](https://img.shields.io/badge/🧪%20Tested%20On-A20.6%20b9-blue.svg)](https://7daystodie.com/) [![📦 Automated Release](https://github.com/jonathan-robertson/robotic-inbox/actions/workflows/release.yml/badge.svg)](https://github.com/jonathan-robertson/robotic-inbox/actions/workflows/release.yml)

- [Robotic Inbox](#robotic-inbox)
  - [Summary](#summary)
  - [Auto-Sorting](#auto-sorting)
  - [Placement/Configuration Requirements](#placementconfiguration-requirements)
  - [Compatibility](#compatibility)

## Summary

7 Days to Die modlet: A special container that automatically sorts and distributes items to other nearby storage containers.

## Auto-Sorting

> 📝 Players will find this information in their journals under `Robotic Inbox (1/2) [MOD]`

The `Secure Robotic Inbox` is a new block meant to streamline item/resource organization within a base and help you to get back into the action as quickly as possible.

When placed **within an LCB**, it will automatically send all of the items it receives to nearby player-placed storage containers up to 5 meters away in any direction (so long as they're within the same LCB).

For security reasons, **the Inbox will need to use the same password as any locked storage containers it tries to organize**.

⚠️ While any secure or insecure player-placed storage container will work with the Inbox, **Writable Storage Boxes will describe how the Inbox is interacting with them**, making them the recommended type of container to place near an Inbox.

Any items in the Inbox that are not able to be matched with another container will be left there until you have time to decide which container to store them in.

This new Inbox can be crafted at the Workbench or found for sale by a trader whenever you're ready.

## Placement/Configuration Requirements

> 📝 Players will find this information in their journals under `Robotic Inbox (2/2) [MOD]`

The `Secure Robotic Inbox` has certain requirements depending on the state of the containers it interacts with.

⚠️ Using `Writable Storage Boxes` will make configuring your storage area far easier, thanks to the feedback text that will appear when the Inbox attempts to interact with them.

If the following requirements aren't met, the Inbox will have to skip interactions with the target containers until the necessary adjustments are made:

1. containers not placed by players (ex: POI containers) are ignored.
2. player backpacks and vehicles are ignored.
3. both Inbox and target container must be within the same LCB.
4. target container must be within 5 meters of Inbox.
5. if target container is locked, Inbox must also be lockable, in a locked state, and set to the same password as target container.

## Compatibility

- Server-side mod only: does not work on client's end and client does not need to download anything
