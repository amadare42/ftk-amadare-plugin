# Amadare's Quality of Life plugin

This plugin provides a lot of small configurable tweaks and features that doesn't affect gameplay but make your life easier.


## Features
- Equipment loadouts
- Extended stats display
    - Poison entry will display how much rounds it will still be active
    - XP bar now shows experience relative to current level
    - Added note to description and exclamation mark to title if encounter will disappear on leave
- Preventing weird steam initialization issue that makes game crashing and reloading on start
- Skip intro
- Open inventory on first "I" press instead of focusing quick panel

## Thunderstore installation (soon)

1. Install [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager)
2. Install `AmadareQoL` mod
3. Start game by using Start Modded button

## Manual installation

1. Install latest [Bepinex](https://github.com/BepInEx/BepInEx/releases) 5.* version. You can refer to installation instructions [here](https://docs.bepinex.dev/articles/user_guide/installation/index.html)
2. Either install HookGenPatcher or add `MMHOOK_Assembly-CSharp.dll` to `BepInEx\plugins` folder from [this repo](https://github.com/ftk-modding/stripped-binaries)

## Build

1. You'll need to provide stripped and publicized binaries. Those are included as git submodule. In order to get those you can:
   - When cloning this repository use `git clone --recurse-submodules`
   - If repository is already checked out, use `git submodule update --init --recursive`
   - Download or build them yourself using instructions from [this repo](https://github.com/ftk-modding/stripped-binaries)
2. (optional) Set `BuiltPluginDestPath` property in project file so after build binaries will be copied to specified location.

## License

MIT