## 1.2.0
* Added missing LanguageAPI dependency
* Added Risk of Options support
* Removed the `mod_cfgdif_reload` console command
* Fixed MonsoonPlayerHelper item being given to players if CountsAsHardMode is enabled
## 1.1.0
* Added config options for all player, ally and enemy stats
* Added config options: Teleporter Charge Speed, Teleporter Discharge Speed, Player/Ally/Enemy Starting Items, Player Starting Equipment
* Organized config options into sections
* Fixed Player Regen being capped between 0% and 100% values instead of Ally Starting Health
* Fixed missing RecalculateStatsAPI module dependency causing stat changes to not apply without certain other mods
## 1.0.1
* Added new configurable values: Ally Fall Damage, Ally Fall Damage Is Lethal, Ally Permanent Damage
* Fixed Enemy Gold Drops and Teleporter Radius using Ally Healing values in the game instead of their own
* Fixed basic stat changes affecting allies and enemies even when not playing on Pluviculture
