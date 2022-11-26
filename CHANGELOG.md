# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
