# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## TODO

fix on-close, game does not think this has a matching SecureInboxBlockId

```log
2023-06-13T01:55:14 327.679 INF [RoboticInbox.StorageManager] DEBUG: SecureInboxBlockId != source.blockValue.Block.blockID at 188, 77, 1106
2023-06-13T01:55:18 331.628 INF [RoboticInbox.StorageManager] DEBUG: Distribute called for tile entity at 188, 77, 1106
```

## [2.0.0] - 2023-06-13

- optimize distribution coroutine
- fix bug loading block ids on first launch
- update console command for a21
- update patches for a21
- update to a21 modinfo file format
- update to a21 references

## [1.4.0] - 2023-04-22

- take advantage of land claim if one is present
- update inbox to work outside of land claims

## [1.3.1] - 2023-03-19

- add console command to toggle debug mode
- fix map bounds check
- update formatting to align with csharp standards
- update inbox names for block naming standards
- update inbox to no longer be terrain decoration
- update inbox to no longer default rotate on face

## [1.3.0] - 2022-11-29

- fix issue causing container text loss

## [1.2.0] - 2022-11-28

- lock coordinates to valid map positions
- prevent inbox with broken lock from processing
- update inbox sync to coroutine for performance
- update journal entries, descriptions, and readme

## [1.1.0] - 2022-11-27

- prevent inboxes from syncing with each other
- prevent inbox from placement outside of LCB

## [1.0.1] - 2022-11-26

- fix bug when detecting if in range of LCB

## [1.0.0] - 2022-10-21

- add auth checks before distributing items
- add container unlock hook
- add crafting recipe
- add hook to fire when closing a container
- add journal entries
- add secure and inSecure Robotic Inbox blocks
- add sound effects on the tile entities that are sorted
- auto-sort target after move
- limit checks to only boxes within the same LCB as the source
- move content from source to target on close
- only sort if source is within LCB
- repeat check for multiple blocks around source (not just y+1)
- update signed target with denied reason
- update signed target with moved item count
