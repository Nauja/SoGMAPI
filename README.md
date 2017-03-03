# Secrets of Grindea ModLoader

[![Release](https://img.shields.io/badge/release-v0.1a-blue.svg)]()
[![Status](https://img.shields.io/badge/status-alpha-red.svg)]()

Modding API for Secret of Grindea

## Alpha

**Everything here is in alpha and should be treated as such.**

This means that you should not use it in a real game and that you should always make a backup of your saves as they may become corrupted or incompatible with the game at any time. Plus, at the moment it may be very unstable and break some features or the game.

I will not be responsible if you lose your saves.

## How it works

The official game executable **Secrets Of Grindea.exe** is modified to include the ModLoader. The ModLoader is a bridge between the game source code and the modding API. It takes care of loading and initialiazing mods installed in the **Mods** subfolder when the game starts.

For each [official version of the game](http://secretsofgrindea.com/forum/index.php?forums/patch-notes.10/) I will upload here a modified version of the game executable with the injected ModLoader. You can manually replace your game executable by the modified one, but it is recommended to use the launcher as it will automatically install the correct version and keep you up to date.

This repository contains:
* The modified game executable: containing the ModLoader.
* A launcher: allowing to install the ModLoader and manage installed mods.
* A modding API: provide an interface to mod the game.
* Samples: show how to use the modding API to create mods.

This repository won't contain:
* The game source code.
* The ModLoader source code.
