# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [pipeline] - 2025-04-14

- remove local references

## [6.0.0] - 2025-02-15

- fix null reference exception
- update references for 7DTD 1.3 (b9)

## [5.0.0] - 2024-12-11

- update references for 7DTD 1.2 (b27)

## [No Release Necessary] - 2024-10-02

- update references for 7DTD 1.1 (b14)

## [4.0.0] - 2024-07-20

- add new admin options
  - add ability to adjust how long sign notices appear
  - add feature for admins to modify area of effect
    - update `cntRoboticInboxDesc` to reference cvar
- add repairable locks that replace owner on repair
- add support for local and p2p, if possible
  - trigger distribution on p2p host
  - trigger sign update on p2p host
- fix bug: when opening inbox multiple times: surrounding containers forget original text
- fix bug: messages lost on shutdown
- centralize sign management
  - update away from spawning coroutines and favor a single, centrally-managed coroutine
- centralize notification/tooltip/sound management
- fix compilation errors
- fix recipe: value -> count in 1.0
- fix trader category -> match workbench
- prevent inbox from being opened during a scan
- remove journal tips (discontinued in 1.0)
- rename inbox blocks to follow new naming standards
- update model to washing machine
  - add multiple color options
  - add tag to colors and scan for block tags
- update recipe: reduce forged iron, add pipe
- update references to storage; this has updated to something new
- verify if any 'player-placed containers' are non-writable and consider removing it (there are some left!)
- update references for 7dtd-1.0-b326
- update readme with screenshots
- update to prioritize scanning closest containers first

## [3.0.1] - 2023-11-21

- fix readme reference to log token

## [3.0.0] - 2023-11-21

- update readme & binary with new support link
- update readme with new setup guide
- update to support a21.2 b30 (stable)

## [2.0.1] - 2023-06-30

- update to support a21 b324 (stable)

## [2.0.0] - 2023-06-25

- add electricTier1 trader stage requirement
- add journal entry on login
- add recipe unlock to robotics magazine
- fix access text sync bug
- fix bug loading block ids on first launch
- fix item dup exploit
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
