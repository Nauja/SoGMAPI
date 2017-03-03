# Secrets of Grindea ModLoader

[![Release](https://img.shields.io/badge/release-v0.1a-blue.svg)]()
[![Status](https://img.shields.io/badge/status-alpha-red.svg)]()

Modding API for Secret of Grindea

## How it works

The official game executable **Secrets Of Grindea.exe** is modified to include the ModLoader. The ModLoader is a bridge between the game source code and the modding API. It takes care of loading and initialiazing mods installed in the **Mods** subfolder when the game starts.

This repository contains:
* A launcher: allowing to install the ModLoader and manage installed mods.
* A modding API: provide an interface to mod the game.
* Samples: show how to use the modding API to create mods.

This repository won't contain:
* The game source code.
* The ModLoader source code.
