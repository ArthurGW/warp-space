# Warp Space

## Procedurally-generated spaceship layouts using Answer Set Programming

Submitted for the module "CM3070 - Final Project" for the BSc Computer Science degree at the University of London.

## Installation

This repository features a binary release of the version of the project submitted for CM3070, see
[releases](https://github.com/ArthurGW/warp-space/releases).

This is in the form of a 7zip archive. Extract the archive and run `warp-space.exe`.

This only runs on Microsoft Windows.

## Project Structure

### LevelGenerator

A procedural level generator, using Answer Set Programming (ASP) to generate spaceship levels. The generation process is
driven by C++ code, which uses [clingo](https://github.com/potassco/clingo)'s C++ API to process ASP specifications into
level designs, before parsing the returned logical symbols into an abstract representation of a level.

This package also features a C# API, auto-generated using [CppSharp](https://github.com/mono/CppSharp) from the C++ 
headers.

Finally, it features python code used during prototyping and evaluating the pure-ASP generator (i.e. bypassing the C++
and C# APIs). This code uses an executable version of clingo, rather than its API.

### Other Folders

These define a basic Unity game, which uses the C# LevelGenerator API to generate levels. The main purpose of the game
is to control and demonstrate the level generation. As such, it features only basic graphics and gameplay, and no "win
condition". Instead, the player can progress through an infinite series of levels, with their score being the number of
levels they get through before their inevitable demise.

## Requirements

### LevelGenerator

#### C++ generator

- clingo - this is included as a git submodule, run `git submodule update --init --remote --recursive` to fetch it
- C++ 14
- CMake >= 3.28
- ninja (optional, but required by the installation batch scripts)
- Microsoft Windows - at present, this project is only set up to build on Windows
- Visual Studio 2022 - other compilers have not been tested, but may work

#### C# bindings

- CppSharp - this is included as a git submodule, run `git submodule update --init --remote --recursive` to fetch it
  - Tested with this commit: https://github.com/mono/CppSharp/tree/e093f713b90d49528de13afd859e3d1f34cf3049
- .NET SDK - tested with version 9.0.107
- CMake >= 3.28

### Unity Game

Tested with Unity version 6.0 (6000.0.50f1).

#### External assets used

- Skybox/game background: [here](https://assetstore.unity.com/packages/2d/textures-materials/sky/galactic-green-skybox-10992)
- Background music: [3 tracks from here](https://assetstore.unity.com/packages/audio/music/electronic/ambient-cyberpunk-music-wirescapes-325867)
- Alarm sound: [Sci_Fi_Alarm_loop_14 from here](https://assetstore.unity.com/packages/audio/ambient/sci-fi/sci-fi-alarm-sfx-238043)
- All other sound FX: [here](https://assetstore.unity.com/packages/audio/sound-fx/shapeforms-audio-free-sound-effects-183649)
- Font: [here](https://assetstore.unity.com/packages/2d/fonts/fatality-fps-gaming-font-216954)