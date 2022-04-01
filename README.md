Adds a new difficulty, Pluviculture, that uses values from the mod's config file `ConfigurableDifficulty.cfg`.  
Configurable values:  
* Difficulty Scaling
* Player Health Regeneration
* Ally Starting Health
* Ally Maximum Health
* Ally Healing
* Ally Armor
* Ally Fall Damage
* Ally Fall Damage Is Lethal
* Ally Permanent Damage
* Enemy Speed
* Enemy Gold Drops
* Enemy Cooldowns
* Teleporter Radius
* Ambient Level Cap
* Whether completing a run will unlock the Mastery skin for the selected character or not  

Use the `mod_cfgdif_reload` console command to reload the config file without restarting the game.

## Changelog
### 1.0.1:
* Added new configurable values: Ally Fall Damage, Ally Fall Damage Is Lethal, Ally Permanent Damage
* Fixed Enemy Gold Drops and Teleporter Radius using Ally Healing values in the game instead of their own
* Fixed basic stat changes affecting allies and enemies even when not playing on Pluviculture
