[![](https://img.shields.io/badge/-Void_Crew_Modding_Team-111111?style=just-the-label&logo=github&labelColor=24292f)](https://github.com/Void-Crew-Modding-Team)
![](https://img.shields.io/badge/Game%20Version-1.0.4-111111?style=flat&labelColor=24292f&color=111111)
[![](https://img.shields.io/discord/1180651062550593536.svg?&logo=discord&logoColor=ffffff&style=flat&label=Discord&labelColor=24292f&color=111111)](https://discord.gg/g2u5wpbMGu "Void Crew Modding Discord")

# Void Saving

Version 0.4.0  
For Game Version 1.0.4  
Developed by Dragon  
Requires:  BepInEx-BepInExPack-5.4.2100, VoidCrewModdingTeam-VoidManager-1.2.6


---------------------

### 💡 Function(s) - Saves the game while in Void, allowing users to load back up where they left off.

- Saves game data on warp via manual or auto save.
- Iron Man Mode - Users are limited to one save, which is deleted on ship death. This mode is on by default to maintain the spirit of the gameplay.
- Save Management - Save files can be viewed and deleted in-game via the F5 menu.
- Data saved: 
  - Generation data for next sectors
  - Modules
  - Mods and batteries in module sockets
  - Carriables lying around in the ship and in shelves
  - Module and ship systems power states
  - Ship powered on state
  - Relics
  - Ship health
  - Breaches
  - Defects
  - Unlocked Blueprints
  - Completed missions
  - Booster states
  - Ammo in turrets
  - Ammo in KPDs
  - Ammo in crates/batteries
  - Shield Directions
  - Shield Charges
  - Engine Trims and Enhancement Panels
  - Breakers
  - Fabricator level
  - Mission Statistics
  - Void Drive charge states
  - Atmosphere levels
  - Airlock Safeties
  - Door states
  - Life support/brain Mode Selection

### 🎮 Client Usage

- Loading
  - While in hub, select a save file via F5 > Mod Settings > Void Saving
  - Click 'Load Save'
  - Start a match by having all players sit down.
- Saving
  - While in a run, in void, F5 > Mod Settings > Void Saving
  - Select a save file to overwrite or create a new save file by entering a name and clicking 'Save Game'
- Save file deletion
  - Go to the settings menu (F5 > Mod Settings > Void Saving)
  - Click the X button next to the save entry you wish to delete.
  - Confirm deletion.
- Configuration
  - Configure Iron Man Mode in hub. Iron Man Mode is on by default, default and next run mode can be configured.

### 👥 Multiplayer Functionality

- ✅ Host
  - Only the host needs this mod.
  - Must be the host who started the session.

---------------------

## 🔧 Install Instructions - **Install following the normal BepInEx procedure.**

Ensure that you have [BepInEx 5](https://thunderstore.io/c/void-crew/p/BepInEx/BepInExPack/) (stable version 5 **MONO**) and [VoidManager](https://thunderstore.io/c/void-crew/p/VoidCrewModdingTeam/VoidManager/) installed.

#### ✔️ Mod installation - **Unzip the contents into the BepInEx plugin directory**

Drag and drop `VoidSaving.dll` into `Void Crew\BepInEx\plugins`
