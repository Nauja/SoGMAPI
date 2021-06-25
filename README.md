# Secrets of Grindea Modding API

[![Release](https://img.shields.io/badge/release-v0.1a-blue.svg)]()
[![Status](https://img.shields.io/badge/status-alpha-red.svg)]()

SoGMAPI is an open-source modding framework and API for [Secret of Grindea](http://store.steampowered.com/app/269770/), forked from [SMAPI](https://github.com/Pathoschild/SMAPI), that lets you play the game with mods. It's safely installed alongside the game's executable, and doesn't change any of your game files.

## Disclaimer

**Everything here is in alpha and should be treated as such.**

This means that you should not use it in a real game and that you should always make a backup of your saves as they may become corrupted or incompatible with the game at any time. Plus, at the moment it may be very unstable and break some features or the game.

I will not be responsible if you lose your saves.

**SMAPI** is an open-source modding framework and API for [Stardew Valley](https://stardewvalley.net/)
that lets you play the game with mods. It's safely installed alongside the game's executable, and
doesn't change any of your game files. It serves seven main purposes:

## Features

1. **Load mods into the game.**  
   _SoGMAPI loads mods when the game is starting up so they can interact with it. (Code mods aren't
   possible without SoGMAPI to load them.)_

2. **Provide APIs and events for mods.**  
   _SoGMAPI provides APIs and events which let mods interact with the game in ways they otherwise
   couldn't._

3. **Provide update checks.**  
   _SoGMAPI automatically checks for new versions of your installed mods, and notifies you when any
   are available._

4. **Provide compatibility checks.**  
   _SoGMAPI automatically detects outdated or broken code in mods, and safely disables them before
   they cause problems._

5. **Back up your save files.**  
   _SoGMAPI automatically creates a daily backup of your saves and keeps ten backups (via the bundled
   Save Backup mod), in case something goes wrong._

## Documentation

Most of the documentation available for [SMAPI](https://github.com/Pathoschild/SMAPI) should also work for
SoGMAPI as this is a fork with the same structure, same installer and same modding API.

Have questions? You can ask me on GitHub or send me an email. 

### For players
* [Player guide (SMAPI)](https://stardewvalleywiki.com/Modding:Player_Guide)

### For modders
* [Modding documentation (SMAPI)](https://smapi.io/docs)
* [Mod build configuration (SMAPI)](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md)
* [Release notes](release-notes.md)

### For SoGMAPI developers
* [Technical docs (SMAPI)](docs/technical/sogmapi.md)
