# FGTools Mobile

![Logo](Assets/GitHubImages/FGToolsMSplash.png)

<p align="center">Mobile version of FGTools</p>

# Features
- Round loader that allows you load any round in game in singleplayer
- Ability to set custom FPS Limit
- FG Debug (FPS Counter, network statistics, etc)
- Unity log tracking
- Some cheats (never included in the release version)

# Screenshots

![Screenshot](Assets/GitHubImages/S1.png)

![Screenshot](Assets/GitHubImages/S2.png)

![Screenshot](Assets/GitHubImages/S3.png)

# Building
### Project
- Open it with Visual Studio
- Update references in Lib folder if needed (to do that use [this version of MelonLoader](https://github.com/LavaGang/MelonLoader/releases/tag/v0.5.7) on the desktop version of the game)
- To build with cheats included set "Cheats" configuration in configuration manager

### UI Bundle
- Download Unity Hub
- Install Unity ``2021.3.16f1`` with android build tools 
- Open the project and select Build AssetBundles in the default context menu
- Your bundle will be in the AssetBundles folder inside the project

# Installing
## Without modifying anything 
- Download latest release
- Install it 

## With modifying 
### With access to Android/data folder
- Make sure your game is patched by this version of [LemonLoader](https://github.com/LemonLoader/MelonLoader_057/releases/tag/0.2.0.1) 
- Navigate Android/data/com.Mediatonic.FallGuys_client/files/
- Put the "NOT_FGTools" folder from the Assets folder and NOT FGTools.dll that you built into the"Mods" folder 
- Lunch the game

### Without access to Android/data folder
- Get Fall Guys apk that patched via this version of [LemonLoader](https://github.com/LemonLoader/MelonLoader_057/releases/tag/0.2.0.1) 
- Open it with some APK editor (MT Manager or APKTool M on Android or APK Editor Studio on Windows)
- Inside the Assets folder create a folder named "copyToData" there you need to create "Mods" folder and also put the NOT_FGTools folder from the Assets folder in this repo 
- Pack the APK and sign it
- Once all done launch the game

<br>

> # Note
>
> - <b>First launch takes up to ten minutes, the game also may crash</b>
> - <b>if you don't want to wait for game resources to download, backup .obb file inside the Android/obb/com.Mediatonic FallGuys_client/ folder, just restore it after APK installing</b>
<br><br>

# FAQ
### Can I use this on Emulator?
- LemonLoader doesn't have official emulator support but you can try

# Credits
- Made using [LemonLoader](https://github.com/LemonLoader/MelonLoader_057)