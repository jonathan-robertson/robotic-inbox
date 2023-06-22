# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## TODO

- test locks for security, etc.
- test trader presence and trader stage
- test recipe mechanics... consider if they should unlock in a specific magazine

- 🪲 Notification Text Gets Stuck#30
  - Immediately accessing container after push causes the notification text to stay that way.
  - see `StorageManager.ShowTemporaryText`... seems SetText can't flip back if player is in the container (maybe it never could?)

## [2.0.0] - UNRELEASED

- add electricTier1 trader stage requirement
- fix bug loading block ids on first launch
- optimize distribution coroutine
- update console command for a21
- update flow from bottom to top
- update patches for a21
- update to a21 mod-info file format
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
