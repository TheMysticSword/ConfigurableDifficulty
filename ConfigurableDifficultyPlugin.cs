using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using MysticsRisky2Utils;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace ConfigurableDifficulty
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(DifficultyAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(MysticsRisky2UtilsPlugin.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class ConfigurableDifficultyPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.themysticsword.configurabledifficulty";
        public const string PluginName = "ConfigurableDifficulty";
        public const string PluginVersion = "1.2.0";

        // General
        public static ConfigOptions.ConfigurableValue<float> difficultyScaling;
        public static ConfigOptions.ConfigurableValue<float> teleporterRadius;
        public static ConfigOptions.ConfigurableValue<float> teleporterChargeSpeed;
        public static ConfigOptions.ConfigurableValue<float> teleporterDischargeSpeed;
        public static ConfigOptions.ConfigurableValue<int> ambientLevelCap;
        public static ConfigOptions.ConfigurableValue<bool> countsAsHardMode;

        // Player stats
        public static ConfigOptions.ConfigurableValue<float> playerMaxHealth;
        public static ConfigOptions.ConfigurableValue<float> playerMaxShield;
        public static ConfigOptions.ConfigurableValue<float> playerRegen;
        public static ConfigOptions.ConfigurableValue<float> playerBaseRegen;
        public static ConfigOptions.ConfigurableValue<float> playerSpeed;
        public static ConfigOptions.ConfigurableValue<float> playerJumpPower;
        public static ConfigOptions.ConfigurableValue<float> playerDamage;
        public static ConfigOptions.ConfigurableValue<float> playerAttackSpeed;
        public static ConfigOptions.ConfigurableValue<float> playerCrit;
        public static ConfigOptions.ConfigurableValue<float> playerCritDamage;
        public static ConfigOptions.ConfigurableValue<float> playerArmor;
        public static ConfigOptions.ConfigurableValue<float> playerCurse;
        public static ConfigOptions.ConfigurableValue<float> playerCooldowns;

        // Player modifiers
        public static ConfigOptions.ConfigurableValue<string> playerStartingItems;
        public static ConfigOptions.ConfigurableValue<string> playerStartingEquipment;

        // Ally stats
        public static ConfigOptions.ConfigurableValue<float> allyMaxHealth;
        public static ConfigOptions.ConfigurableValue<float> allyMaxShield;
        public static ConfigOptions.ConfigurableValue<float> allyRegen;
        public static ConfigOptions.ConfigurableValue<float> allyBaseRegen;
        public static ConfigOptions.ConfigurableValue<float> allySpeed;
        public static ConfigOptions.ConfigurableValue<float> allyJumpPower;
        public static ConfigOptions.ConfigurableValue<float> allyDamage;
        public static ConfigOptions.ConfigurableValue<float> allyAttackSpeed;
        public static ConfigOptions.ConfigurableValue<float> allyCrit;
        public static ConfigOptions.ConfigurableValue<float> allyCritDamage;
        public static ConfigOptions.ConfigurableValue<float> allyArmor;
        public static ConfigOptions.ConfigurableValue<float> allyCurse;
        public static ConfigOptions.ConfigurableValue<float> allyCooldowns;

        // Ally modifiers
        public static ConfigOptions.ConfigurableValue<float> allyStartingHealth;
        public static ConfigOptions.ConfigurableValue<float> allyHealing;
        public static ConfigOptions.ConfigurableValue<float> allyFallDamage;
        public static ConfigOptions.ConfigurableValue<bool> allyFallDamageIsLethal;
        public static ConfigOptions.ConfigurableValue<float> allyPermanentDamage;
        public static ConfigOptions.ConfigurableValue<string> allyStartingItems;

        // Enemy stats
        public static ConfigOptions.ConfigurableValue<float> enemyMaxHealth;
        public static ConfigOptions.ConfigurableValue<float> enemyMaxShield;
        public static ConfigOptions.ConfigurableValue<float> enemyRegen;
        public static ConfigOptions.ConfigurableValue<float> enemyBaseRegen;
        public static ConfigOptions.ConfigurableValue<float> enemySpeed;
        public static ConfigOptions.ConfigurableValue<float> enemyJumpPower;
        public static ConfigOptions.ConfigurableValue<float> enemyDamage;
        public static ConfigOptions.ConfigurableValue<float> enemyAttackSpeed;
        public static ConfigOptions.ConfigurableValue<float> enemyCrit;
        public static ConfigOptions.ConfigurableValue<float> enemyCritDamage;
        public static ConfigOptions.ConfigurableValue<float> enemyArmor;
        public static ConfigOptions.ConfigurableValue<float> enemyCurse;
        public static ConfigOptions.ConfigurableValue<float> enemyCooldowns;

        // Enemy modifiers
        public static ConfigOptions.ConfigurableValue<float> enemyStartingHealth;
        public static ConfigOptions.ConfigurableValue<float> enemyHealing;
        public static ConfigOptions.ConfigurableValue<float> enemyFallDamage;
        public static ConfigOptions.ConfigurableValue<bool> enemyFallDamageIsLethal;
        public static ConfigOptions.ConfigurableValue<float> enemyPermanentDamage;
        public static ConfigOptions.ConfigurableValue<float> enemyGoldDrops;
        public static ConfigOptions.ConfigurableValue<string> enemyStartingItems;

        public static DifficultyDef configurableDifficultyDef;
        public static DifficultyIndex configurableDifficultyIndex;

        public static ConfigFile config;
        public static BepInEx.Logging.ManualLogSource logger;

        public void Awake()
        {
            config = Config;
            logger = Logger;
            
            difficultyScaling = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "General", "DifficultyScaling", 50f, -1000f, 1000f, description: "Difficulty scaling over time (in %)", onChanged: (x) => MarkConfigDirty());
            teleporterRadius = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "General", "TeleporterRadius", -50f, -100f, 1000f, description: "Teleporter radius (in %). Values of -100% and below set the radius to 0m.", onChanged: (x) => MarkConfigDirty());
            teleporterChargeSpeed = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "General", "TeleporterChargeSpeed", 0f, -100f, 1000f, description: "Teleporter charge speed (in %). Values of -100% and below make the teleporter unchargeable.", onChanged: (x) => MarkConfigDirty());
            teleporterDischargeSpeed = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "General", "TeleporterDischargeSpeed", 0f, 0f, 1000f, description: "Teleporter discharge speed when all players are outside the radius (in % per second)", onChanged: (x) => MarkConfigDirty());
            ambientLevelCap = ConfigOptions.ConfigurableValue.CreateInt(PluginGUID, PluginName, config, "General", "AmbientLevelCap", 99, 1, 1000000, description: "Ambient level cap. Ambient level affects monsters and NPC allies.", onChanged: (x) => MarkConfigDirty());
            countsAsHardMode = ConfigOptions.ConfigurableValue.CreateBool(PluginGUID, PluginName, config, "General", "CountsAsHardMode", true, description: "Completing a run on this difficulty will unlock the selected survivor's Mastery skin.", onChanged: (x) => MarkConfigDirty());

            playerMaxHealth = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerMaxHealth", 0f, -1000f, 1000f, description: "Player maximum health (in %)", onChanged: (x) => MarkConfigDirty());
            playerMaxShield = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerMaxShield", 0f, -1000f, 1000f, description: "Player maximum shield (in %)", onChanged: (x) => MarkConfigDirty());
            playerRegen = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerRegen", -40f, -1000f, 1000f, description: "Player health regeneration (in %)", onChanged: (x) => MarkConfigDirty());
            playerBaseRegen = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerBaseRegen", 0f, -1000f, 1000f, description: "Player base health regeneration (in HP/s)", onChanged: (x) => MarkConfigDirty());
            playerSpeed = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerSpeed", 0f, -1000f, 1000f, description: "Player movement speed (in %)", onChanged: (x) => MarkConfigDirty());
            playerJumpPower = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerJumpPower", 0f, -1000f, 1000f, description: "Player jump power (in %)", onChanged: (x) => MarkConfigDirty());
            playerDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerDamage", 0f, -1000f, 1000f, description: "Player damage (in %)", onChanged: (x) => MarkConfigDirty());
            playerAttackSpeed = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerAttackSpeed", 0f, -1000f, 1000f, description: "Player attack speed (in %)", onChanged: (x) => MarkConfigDirty());
            playerCrit = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerCrit", 0f, -1000f, 1000f, description: "Player critical strike chance (in %)", onChanged: (x) => MarkConfigDirty());
            playerCritDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerCritDamage", 0f, -1000f, 1000f, description: "Player critical strike chance damage (in %)", onChanged: (x) => MarkConfigDirty());
            playerArmor = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerArmor", 0f, -1000f, 1000f, description: "Player armor", onChanged: (x) => MarkConfigDirty());
            playerCurse = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerCurse", 0f, -1000f, 1000f, description: "Player maximum health reduction", onChanged: (x) => MarkConfigDirty());
            playerCooldowns = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "PlayerCooldowns", 0f, -1000f, 1000f, description: "Player skill cooldowns", onChanged: (x) => MarkConfigDirty());

            playerStartingItems = ConfigOptions.ConfigurableValue.CreateString(PluginGUID, PluginName, config, "Player modifiers", "PlayerStartingItems", "", description: "Player starting items. Uses internal item names, comma-separated. Add a colon and a number to select the amount of the starting item. Example: Squid,Seed,GhostOnKill:3,BarrierOnOverHeal,FlatHealth,HeadHunter:99", onChanged: (x) => MarkConfigDirty());
            playerStartingEquipment = ConfigOptions.ConfigurableValue.CreateString(PluginGUID, PluginName, config, "Player modifiers", "PlayerStartingEquipment", "", description: "Player starting equipment. Uses the internal equipment name. Example: GoldGat", onChanged: (x) => MarkConfigDirty());

            allyMaxHealth = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyMaxHealth", 0f, -1000f, 1000f, description: "Ally maximum health (in %)", onChanged: (x) => MarkConfigDirty());
            allyMaxShield = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyMaxShield", 0f, -1000f, 1000f, description: "Ally maximum shield (in %)", onChanged: (x) => MarkConfigDirty());
            allyRegen = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyRegen", 0f, -1000f, 1000f, description: "Ally health regeneration (in %)", onChanged: (x) => MarkConfigDirty());
            allyBaseRegen = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "AllyBaseRegen", 0f, -1000f, 1000f, description: "Ally base health regeneration (in HP/s)", onChanged: (x) => MarkConfigDirty());
            allySpeed = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllySpeed", 0f, -1000f, 1000f, description: "Ally movement speed (in %)", onChanged: (x) => MarkConfigDirty());
            allyJumpPower = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyJumpPower", 0f, -1000f, 1000f, description: "Ally jump power (in %)", onChanged: (x) => MarkConfigDirty());
            allyDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyDamage", 0f, -1000f, 1000f, description: "Ally damage (in %)", onChanged: (x) => MarkConfigDirty());
            allyAttackSpeed = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyAttackSpeed", 0f, -1000f, 1000f, description: "Ally attack speed (in %)", onChanged: (x) => MarkConfigDirty());
            allyCrit = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyCrit", 0f, -1000f, 1000f, description: "Ally critical strike chance (in %)", onChanged: (x) => MarkConfigDirty());
            allyCritDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyCritDamage", 0f, -1000f, 1000f, description: "Ally critical strike chance damage (in %)", onChanged: (x) => MarkConfigDirty());
            allyArmor = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyArmor", 0f, -1000f, 1000f, description: "Ally armor", onChanged: (x) => MarkConfigDirty());
            allyCurse = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyCurse", 0f, -1000f, 1000f, description: "Ally maximum health reduction", onChanged: (x) => MarkConfigDirty());
            allyCooldowns = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally stats", "AllyCooldowns", 0f, -1000f, 1000f, description: "Ally skill cooldowns", onChanged: (x) => MarkConfigDirty());

            allyStartingHealth = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally modifiers", "AllyStartingHealth", 50f, 0f, 100f, description: "Ally starting health (in %, between 0-100%)", onChanged: (x) => MarkConfigDirty());
            allyHealing = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally modifiers", "AllyHealing", -50f, -100f, 1000000f, description: "Ally healing (in %). Values of -100% and below disable ally healing.", onChanged: (x) => MarkConfigDirty());
            allyFallDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally modifiers", "AllyFallDamage", 100f, -100f, 1000000f, description: "Ally fall damage (in %). Values of -100% and below disable ally fall damage.", onChanged: (x) => MarkConfigDirty());
            allyFallDamageIsLethal = ConfigOptions.ConfigurableValue.CreateBool(PluginGUID, PluginName, config, "Ally modifiers", "AllyFallDamageIsLethal", true, description: "Allies can die from fall damage.", onChanged: (x) => MarkConfigDirty());
            allyPermanentDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Ally modifiers", "AllyPermanentDamage", 40f, 0f, 1000000f, description: "Whenever an ally takes damage, their maximum health is reduced by a portion of taken damage (in %). Values of 0% and below disable permanent damage.", onChanged: (x) => MarkConfigDirty());
            allyStartingItems = ConfigOptions.ConfigurableValue.CreateString(PluginGUID, PluginName, config, "Ally modifiers", "AllyStartingItems", "", description: "Ally starting items. Uses internal item names, comma-separated. Add a colon and a number to select the amount of the starting item. Example: Squid,Seed,GhostOnKill:3,BarrierOnOverHeal,FlatHealth,HeadHunter:99", onChanged: (x) => MarkConfigDirty());

            enemyMaxHealth = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyMaxHealth", 0f, -1000f, 1000f, description: "Enemy maximum health (in %)", onChanged: (x) => MarkConfigDirty());
            enemyMaxShield = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyMaxShield", 0f, -1000f, 1000f, description: "Enemy maximum shield (in %)", onChanged: (x) => MarkConfigDirty());
            enemyRegen = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyRegen", 0f, -1000f, 1000f, description: "Enemy health regeneration (in %)", onChanged: (x) => MarkConfigDirty());
            enemyBaseRegen = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Player stats", "EnemyBaseRegen", 0f, -1000f, 1000f, description: "Enemy base health regeneration (in HP/s)", onChanged: (x) => MarkConfigDirty());
            enemySpeed = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemySpeed", 40f, -1000f, 1000f, description: "Enemy movement speed (in %)", onChanged: (x) => MarkConfigDirty());
            enemyJumpPower = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyJumpPower", 0f, -1000f, 1000f, description: "Enemy jump power (in %)", onChanged: (x) => MarkConfigDirty());
            enemyDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyDamage", 0f, -1000f, 1000f, description: "Enemy damage (in %)", onChanged: (x) => MarkConfigDirty());
            enemyAttackSpeed = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyAttackSpeed", 0f, -1000f, 1000f, description: "Enemy attack speed (in %)", onChanged: (x) => MarkConfigDirty());
            enemyCrit = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyCrit", 0f, -1000f, 1000f, description: "Enemy critical strike chance (in %)", onChanged: (x) => MarkConfigDirty());
            enemyCritDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyCritDamage", 0f, -1000f, 1000f, description: "Enemy critical strike chance damage (in %)", onChanged: (x) => MarkConfigDirty());
            enemyArmor = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyArmor", 0f, -1000f, 1000f, description: "Enemy armor", onChanged: (x) => MarkConfigDirty());
            enemyCurse = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyCurse", 0f, -1000f, 1000f, description: "Enemy maximum health reduction", onChanged: (x) => MarkConfigDirty());
            enemyCooldowns = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy stats", "EnemyCooldowns", -50f, -1000f, 1000f, description: "Enemy skill cooldowns", onChanged: (x) => MarkConfigDirty());

            enemyStartingHealth = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy modifiers", "EnemyStartingHealth", 100f, 0f, 100f, description: "Enemy starting health (in %, between 0-100%)", onChanged: (x) => MarkConfigDirty());
            enemyHealing = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy modifiers", "EnemyHealing", 0f, -100f, 1000f, description: "Enemy healing (in %). Values of -100% and below disable enemy healing.", onChanged: (x) => MarkConfigDirty());
            enemyFallDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy modifiers", "EnemyFallDamage", 0f, -100f, 1000f, description: "Enemy fall damage (in %). Values of -100% and below disable enemy fall damage.", onChanged: (x) => MarkConfigDirty());
            enemyFallDamageIsLethal = ConfigOptions.ConfigurableValue.CreateBool(PluginGUID, PluginName, config, "Enemy modifiers", "EnemyFallDamageIsLethal", false, description: "Allies can die from fall damage.", onChanged: (x) => MarkConfigDirty());
            enemyPermanentDamage = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy modifiers", "EnemyPermanentDamage", 0f, 0f, 1000f, description: "Whenever an enemy takes damage, their maximum health is reduced by a portion of taken damage (in %). Values of 0% and below disable permanent damage.", onChanged: (x) => MarkConfigDirty());
            enemyGoldDrops = ConfigOptions.ConfigurableValue.CreateFloat(PluginGUID, PluginName, config, "Enemy modifiers", "EnemyGoldDrops", -20f, -100f, 1000f, description: "Enemy gold drops (in %). Set to positive values to increase gold drops, set to negative values to reduce them. Values of -100% and below set gold drops to 0.", onChanged: (x) => MarkConfigDirty());
            enemyStartingItems = ConfigOptions.ConfigurableValue.CreateString(PluginGUID, PluginName, config, "Enemy modifiers", "EnemyStartingItems", "", description: "Enemy starting items. Uses internal item names, comma-separated. Add a colon and a number to select the amount of the starting item. Example: Squid,Seed,GhostOnKill:3,BarrierOnOverHeal,FlatHealth,HeadHunter:99", onChanged: (x) => MarkConfigDirty());

            MarkConfigDirty();

            var assetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "configurabledifficultyassetbundle"));
            var difficultyIcon = assetBundle.LoadAsset<Sprite>("Assets/Misc/Textures/texConfigurableDifficultyIcon.png");

            configurableDifficultyDef = new DifficultyDef(
                2f,
                "DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_NAME",
                null,
                "DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION_DYNAMIC",
                new Color32(181, 206, 195, 255),
                "mod_cfgdif",
                false
            );
            SetConfigRelatedDifficultyDefValues();
            configurableDifficultyIndex = DifficultyAPI.AddDifficulty(configurableDifficultyDef, difficultyIcon);

            if (MysticsRisky2Utils.SoftDependencies.SoftDependencyManager.RiskOfOptionsDependency.enabled)
            {
                MysticsRisky2Utils.SoftDependencies.SoftDependencyManager.RiskOfOptionsDependency.RegisterModInfo(PluginGUID, PluginName, "Adds a new difficulty with many configurable values to make the game easier or harder.", difficultyIcon);
            }

            Language.onCurrentLanguageChanged += Language_onCurrentLanguageChanged;
            RoR2Application.onLoad += () =>
            {
                On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;
            };

            On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
            Run.onPlayerFirstCreatedServer += Run_onPlayerFirstCreatedServer;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            IL.RoR2.HealthComponent.Heal += HealthComponent_Heal;
            IL.RoR2.DeathRewards.OnKilledServer += DeathRewards_OnKilledServer;
            On.RoR2.HoldoutZoneController.Awake += HoldoutZoneController_Awake;
            On.RoR2.HoldoutZoneController.FixedUpdate += HoldoutZoneController_FixedUpdate;
            On.RoR2.Run.RecalculateDifficultyCoefficentInternal += Run_RecalculateDifficultyCoefficentInternal;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            On.RoR2.HealthComponent.Awake += HealthComponent_Awake;
            On.RoR2.CharacterMaster.Awake += CharacterMaster_Awake;
            On.RoR2.UI.EnemyInfoPanel.SetDisplayDataForViewer += EnemyInfoPanel_SetDisplayDataForViewer;
        }

        private string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token)
        {
            if (langTokenStrings.ContainsKey(self.name) && langTokenStrings[self.name].ContainsKey(token))
                return langTokenStrings[self.name][token];

            if (Language.english != null && langTokenStrings.ContainsKey(Language.english.name) && langTokenStrings[Language.english.name].ContainsKey(token))
                return langTokenStrings[Language.english.name][token];

            return orig(self, token);
        }

        private void Language_onCurrentLanguageChanged()
        {
            RequestConstructAllStrings();
        }

        private void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);
            if (NetworkServer.active)
            {
                HealthComponent healthComponent = body.healthComponent;
                if (healthComponent)
                {
                    if (Run.instance.selectedDifficulty == configurableDifficultyIndex)
                    {
                        switch (self.teamIndex)
                        {
                            case TeamIndex.Player:
                                healthComponent.Networkhealth = healthComponent.fullHealth * allyStartingHealth.Value / 100f;
                                break;
                            case TeamIndex.Monster:
                                healthComponent.Networkhealth = healthComponent.fullHealth * enemyStartingHealth.Value / 100f;
                                break;
                        }
                    }
                }
            }
        }

        private void Run_onPlayerFirstCreatedServer(Run run, PlayerCharacterMasterController pcmc)
        {
            if (run.selectedDifficulty == configurableDifficultyIndex && pcmc.master.inventory.GetItemCount(RoR2Content.Items.MonsoonPlayerHelper) > 0)
            {
                pcmc.master.inventory.RemoveItem(RoR2Content.Items.MonsoonPlayerHelper);
            }
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (Run.instance.selectedDifficulty == configurableDifficultyIndex)
            {
                switch (sender.teamComponent.teamIndex)
                {
                    case TeamIndex.Player:
                        if (sender.isPlayerControlled)
                        {
                            args.healthMultAdd += playerMaxHealth.Value / 100f;
                            args.baseShieldAdd += sender.maxHealth * playerMaxShield.Value / 100f;
                            args.regenMultAdd += playerRegen.Value / 100f;
                            args.baseRegenAdd += playerBaseRegen.Value;
                            if (playerSpeed.Value > 0f) args.moveSpeedMultAdd += playerSpeed.Value / 100f;
                            else args.moveSpeedReductionMultAdd -= playerSpeed.Value / 100f;
                            args.jumpPowerMultAdd += playerJumpPower.Value / 100f;
                            args.damageMultAdd += playerDamage.Value / 100f;
                            args.attackSpeedMultAdd += playerAttackSpeed.Value / 100f;
                            args.critAdd += playerCrit.Value / 100f;
                            args.critDamageMultAdd += playerCritDamage.Value / 100f;
                            args.armorAdd += playerArmor.Value;
                            args.baseCurseAdd += playerCurse.Value / 100f;
                            args.cooldownMultAdd += playerCooldowns.Value / 100f;
                        }
                        args.healthMultAdd += allyMaxHealth.Value / 100f;
                        args.baseShieldAdd += sender.maxHealth * allyMaxShield.Value / 100f;
                        args.regenMultAdd += allyRegen.Value / 100f;
                        args.baseRegenAdd += allyBaseRegen.Value;
                        if (allySpeed.Value > 0f) args.moveSpeedMultAdd += allySpeed.Value / 100f;
                        else args.moveSpeedReductionMultAdd -= allySpeed.Value / 100f;
                        args.jumpPowerMultAdd += allyJumpPower.Value / 100f;
                        args.damageMultAdd += allyDamage.Value / 100f;
                        args.attackSpeedMultAdd += allyAttackSpeed.Value / 100f;
                        args.critAdd += allyCrit.Value / 100f;
                        args.critDamageMultAdd += allyCritDamage.Value / 100f;
                        args.armorAdd += allyArmor.Value;
                        args.baseCurseAdd += allyCurse.Value / 100f;
                        args.cooldownMultAdd += allyCooldowns.Value / 100f;
                        break;
                    case TeamIndex.Monster:
                        args.healthMultAdd += enemyMaxHealth.Value / 100f;
                        args.baseShieldAdd += sender.maxHealth * enemyMaxShield.Value / 100f;
                        args.regenMultAdd += enemyRegen.Value / 100f;
                        args.baseRegenAdd += enemyBaseRegen.Value;
                        if (enemySpeed.Value > 0f) args.moveSpeedMultAdd += enemySpeed.Value / 100f;
                        else args.moveSpeedReductionMultAdd -= enemySpeed.Value / 100f;
                        args.jumpPowerMultAdd += enemyJumpPower.Value / 100f;
                        args.damageMultAdd += enemyDamage.Value / 100f;
                        args.attackSpeedMultAdd += enemyAttackSpeed.Value / 100f;
                        args.critAdd += enemyCrit.Value / 100f;
                        args.critDamageMultAdd += enemyCritDamage.Value / 100f;
                        args.armorAdd += enemyArmor.Value;
                        args.baseCurseAdd += enemyCurse.Value / 100f;
                        args.cooldownMultAdd += enemyCooldowns.Value / 100f;
                        break;
                }
            }
        }

        private void HealthComponent_Heal(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            var healAmountPos = 1;

            if (c.TryGotoNext(
                x => x.MatchMul()
            ) && c.TryGotoNext(
                MoveType.After,
                x => x.MatchStarg(healAmountPos)
            ))
            {
                c.GotoNext(MoveType.AfterLabel);
                c.Emit(OpCodes.Ldarg, 0);
                c.Emit(OpCodes.Ldarg, healAmountPos);
                c.EmitDelegate<System.Func<HealthComponent, float, float>>((hc, healAmount) =>
                {
                    if (Run.instance.selectedDifficulty == configurableDifficultyIndex)
                    {
                        switch (hc.body.teamComponent.teamIndex)
                        {
                            case TeamIndex.Player:
                                healAmount *= Mathf.Max(1f + allyHealing.Value / 100f, 0f);
                                break;
                            case TeamIndex.Monster:
                                healAmount *= Mathf.Max(1f + enemyHealing.Value / 100f, 0f);
                                break;
                        }
                    }
                    return healAmount;
                });
                c.Emit(OpCodes.Starg, healAmountPos);
            }
            else
            {
                Logger.LogError("Failed to hook ally healing modifier");
            }
        }

        private void DeathRewards_OnKilledServer(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt<DeathRewards>("get_goldReward")
            ))
            {
                c.EmitDelegate<System.Func<uint, uint>>((droppedGold) =>
                {
                    if (Run.instance.selectedDifficulty == configurableDifficultyIndex)
                    {
                        droppedGold = (uint)(droppedGold * Mathf.Max(1f + enemyGoldDrops.Value / 100f, 0f));
                    }
                    return droppedGold;
                });
            }
            else
            {
                Logger.LogError("Failed to hook enemy gold drop modifier");
            }
        }

        private void HoldoutZoneController_Awake(On.RoR2.HoldoutZoneController.orig_Awake orig, HoldoutZoneController self)
        {
            orig(self);
            self.calcRadius += HoldoutZoneController_calcRadius;
            self.calcChargeRate += HoldoutZoneController_calcChargeRate;
        }

        private void HoldoutZoneController_calcRadius(ref float radius)
        {
            if (Run.instance.selectedDifficulty == configurableDifficultyIndex)
            {
                radius *= Mathf.Max(1f + teleporterRadius.Value / 100f, 0f);
            }
        }

        private void HoldoutZoneController_calcChargeRate(ref float rate)
        {
            if (Run.instance.selectedDifficulty == configurableDifficultyIndex)
            {
                rate *= 1f + teleporterChargeSpeed.Value / 100f;
            }
        }

        private void HoldoutZoneController_FixedUpdate(On.RoR2.HoldoutZoneController.orig_FixedUpdate orig, HoldoutZoneController self)
        {
            self.dischargeRate += teleporterDischargeSpeed.Value / 100f;
            orig(self);
            self.dischargeRate -= teleporterDischargeSpeed.Value / 100f;
        }

        private bool ambientLevelCapChanged = false;
        private int oldAmbientLevelCap;
        private void Run_RecalculateDifficultyCoefficentInternal(On.RoR2.Run.orig_RecalculateDifficultyCoefficentInternal orig, Run self)
        {
            if (self.selectedDifficulty == configurableDifficultyIndex)
            {
                if (!ambientLevelCapChanged)
                {
                    ambientLevelCapChanged = true;
                    oldAmbientLevelCap = Run.ambientLevelCap;
                }
                Run.ambientLevelCap = ambientLevelCap.Value;
            }
            else
            {
                if (ambientLevelCapChanged)
                {
                    ambientLevelCapChanged = false;
                    Run.ambientLevelCap = oldAmbientLevelCap;
                }
            }
            orig(self);
        }

        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (damageInfo != null && damageInfo.damageType.HasFlag(DamageType.FallDamage) && Run.instance.selectedDifficulty == configurableDifficultyIndex)
            {
                if (self.body)
                {
                    switch (self.body.teamComponent.teamIndex)
                    {
                        case TeamIndex.Player:
                            damageInfo.damage *= 1f + allyFallDamage.Value / 100f;
                            if (allyFallDamageIsLethal.Value)
                            {
                                damageInfo.damageType &= ~DamageType.NonLethal;
                                damageInfo.damageType |= DamageType.BypassOneShotProtection;
                            }
                            break;
                        case TeamIndex.Monster:
                            damageInfo.damage *= 1f + enemyFallDamage.Value / 100f;
                            if (enemyFallDamageIsLethal.Value)
                            {
                                damageInfo.damageType &= ~DamageType.NonLethal;
                                damageInfo.damageType |= DamageType.BypassOneShotProtection;
                            }
                            break;
                    }
                }
            }
            orig(self, damageInfo);
        }

        private void HealthComponent_Awake(On.RoR2.HealthComponent.orig_Awake orig, HealthComponent self)
        {
            self.gameObject.AddComponent<ConfigurableDifficultyDamageReceiver>();
            orig(self);
        }

        public class ConfigurableDifficultyDamageReceiver : MonoBehaviour, IOnTakeDamageServerReceiver
        {
            public HealthComponent healthComponent;
            public CharacterBody victimBody;

            public void Start()
            {
                healthComponent = GetComponent<HealthComponent>();
                if (!healthComponent)
                {
                    Object.Destroy(this);
                    return;
                }
                victimBody = healthComponent.body;
            }

            public void OnTakeDamageServer(DamageReport damageReport)
            {
                if (victimBody && Run.instance.selectedDifficulty == configurableDifficultyIndex)
                {
                    switch (victimBody.teamComponent.teamIndex)
                    {
                        case TeamIndex.Player:
                            {
                                float takenDamagePercent = damageReport.damageDealt / healthComponent.fullCombinedHealth * 100f;
                                int permanentDamage = Mathf.FloorToInt(takenDamagePercent * allyPermanentDamage.Value / 100f);
                                for (int l = 0; l < permanentDamage; l++)
                                {
                                    victimBody.AddBuff(RoR2Content.Buffs.PermanentCurse);
                                }
                            }
                            break;
                        case TeamIndex.Monster:
                            {
                                float takenDamagePercent = damageReport.damageDealt / healthComponent.fullCombinedHealth * 100f;
                                int permanentDamage = Mathf.FloorToInt(takenDamagePercent * enemyPermanentDamage.Value / 100f);
                                for (int l = 0; l < permanentDamage; l++)
                                {
                                    victimBody.AddBuff(RoR2Content.Buffs.PermanentCurse);
                                }
                            }
                            break;
                    }
                }
            }
        }

        private void CharacterMaster_Awake(On.RoR2.CharacterMaster.orig_Awake orig, CharacterMaster self)
        {
            orig(self);
            if (Run.instance.selectedDifficulty == configurableDifficultyIndex)
            {
                switch (self.teamIndex)
                {
                    case TeamIndex.Player:
                        if (self.playerCharacterMasterController)
                        {
                            foreach (var itemAndCount in playerStartingItemList)
                                self.inventory.GiveItem(itemAndCount.Key, itemAndCount.Value);
                            if (playerStartingEquipmentIndex != EquipmentIndex.None)
                            {
                                self.inventory.SetEquipment(new EquipmentState(playerStartingEquipmentIndex, Run.FixedTimeStamp.negativeInfinity, 1), 0);
                            }
                        }
                        foreach (var itemAndCount in allyStartingItemList)
                            self.inventory.GiveItem(itemAndCount.Key, itemAndCount.Value);
                        break;
                    case TeamIndex.Monster:
                        foreach (var itemAndCount in enemyStartingItemList)
                            self.inventory.GiveItem(itemAndCount.Key, itemAndCount.Value);
                        break;
                }
            }
        }

        private void EnemyInfoPanel_SetDisplayDataForViewer(On.RoR2.UI.EnemyInfoPanel.orig_SetDisplayDataForViewer orig, RoR2.UI.HUD hud, List<BodyIndex> bodyIndices, ItemIndex[] itemAcquisitionOrderBuffer, int itemAcquisitonOrderLength, int[] itemStacks)
        {
            if (Run.instance && Run.instance.selectedDifficulty == configurableDifficultyIndex)
            {
                itemAcquisitionOrderBuffer = HG.ArrayUtils.Join(enemyStartingItemList.Keys.ToArray(), itemAcquisitionOrderBuffer);
                itemAcquisitonOrderLength += enemyStartingItemList.Count;
                itemStacks = HG.ArrayUtils.Join(enemyStartingItemList.Values.ToArray(), itemStacks);
            }
            orig(hud, bodyIndices, itemAcquisitionOrderBuffer, itemAcquisitonOrderLength, itemStacks);
        }

        public static Dictionary<string, Dictionary<string, string>> langTokenStrings = new Dictionary<string, Dictionary<string, string>>();
        public static void AddOrReplaceLanguageString(string lang, string token, string str)
        {
            Dictionary<string, string> dict;
            if (!langTokenStrings.ContainsKey(lang))
            {
                dict = new Dictionary<string, string>();
                langTokenStrings.Add(lang, dict);
            }
            else dict = langTokenStrings[lang];

            if (!dict.ContainsKey(token)) dict.Add(token, str);
            else dict[token] = str;
        }

        public static Dictionary<ItemIndex, int> playerStartingItemList = new Dictionary<ItemIndex, int>();
        public static Dictionary<ItemIndex, int> allyStartingItemList = new Dictionary<ItemIndex, int>();
        public static Dictionary<ItemIndex, int> enemyStartingItemList = new Dictionary<ItemIndex, int>();
        public static EquipmentIndex playerStartingEquipmentIndex = EquipmentIndex.None;

        public static bool configDirty = false;
        public static void MarkConfigDirty()
        {
            if (configDirty) return;

            configDirty = true;
            RoR2Application.onNextUpdate += OnConfigReloaded;
        }
        public static void OnConfigReloaded()
        {
            configDirty = false;

            SetConfigRelatedDifficultyDefValues();
            RefreshItemLists();
            RefreshEquipment();

            RequestConstructAllStrings();
        }

        public static void SetConfigRelatedDifficultyDefValues()
        {
            if (configurableDifficultyDef != null)
            {
                configurableDifficultyDef.scalingValue = 2f + difficultyScaling.Value / 50f;
                configurableDifficultyDef.countsAsHardMode = countsAsHardMode.Value;
            }
        }

        public static void RefreshItemLists()
        {
            ItemCatalog.availability.CallWhenAvailable(() =>
            {
                void RefreshItemList(string configString, Dictionary<ItemIndex, int> itemList, string listName)
                {
                    itemList.Clear();
                    if (configString.Length > 0)
                    {
                        var splitString = configString.Trim().Split(',');
                        foreach (var singleString in splitString)
                        {
                            var singleStringSplit = singleString.Split(':');
                            var itemName = singleStringSplit[0];
                            var itemCount = singleStringSplit.Length >= 2 && int.TryParse(singleStringSplit[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var ic) ? Mathf.Max(ic, 1) : 1;
                            var itemIndex = ItemCatalog.FindItemIndex(itemName);
                            if (itemIndex != ItemIndex.None)
                            {
                                itemList.Add(itemIndex, itemCount);
                            }
                            else
                            {
                                logger.LogError(string.Format("Item with internal name \"{0}\" wasn't found and won't be added to item list {1}", itemName, listName));
                            }
                        }
                    }
                }

                RefreshItemList(playerStartingItems.Value, playerStartingItemList, "PLAYERSTARTINGITEMS");
                RefreshItemList(allyStartingItems.Value, allyStartingItemList, "ALLYSTARTINGITEMS");
                RefreshItemList(enemyStartingItems.Value, enemyStartingItemList, "ENEMYSTARTINGITEMS");

                RequestConstructAllStrings();
            });
        }

        public static void RefreshEquipment()
        {
            EquipmentCatalog.availability.CallWhenAvailable(() =>
            {
                void RefreshEquipmentSingle(string configString, ref EquipmentIndex equipmentIndex, string equipmentTypeName)
                {
                    configString = configString.Trim();
                    if (configString.Length > 0)
                    {
                        var eq = EquipmentCatalog.FindEquipmentIndex(configString);
                        if (eq != EquipmentIndex.None) equipmentIndex = eq;
                        else logger.LogError(string.Format("Equipment with internal name \"{0}\" wasn't found and won't be set as {1}", configString, equipmentTypeName));
                    }
                }

                RefreshEquipmentSingle(playerStartingEquipment.Value, ref playerStartingEquipmentIndex, "PLAYERSTARTINGEQUIPMENT");

                RequestConstructAllStrings();
            });
        }

        private struct ConfigurableDifficultyDescriptionSection
        {
            public ConfigEntry<float> configEntryFloat;
            public ConfigEntry<int> configEntryInt;
            public ConfigEntry<bool> configEntryBool;
            public ConfigEntry<string> configEntryString;
            public bool isDelta;
            public float deltaStartingValue;
            public bool moreIsBetter;
            public bool showOnlyIfValueIsDifferent;
            public bool showIfTrue;

            public ConfigurableDifficultyDescriptionSection(ConfigEntry<float> configEntry, bool isDelta = true, float deltaStartingValue = 0f, bool moreIsBetter = true, bool showOnlyIfValueIsDifferent = false)
            {
                this.configEntryFloat = configEntry;
                this.configEntryInt = null;
                this.configEntryBool = null;
                this.configEntryString = null;
                this.isDelta = isDelta;
                this.deltaStartingValue = deltaStartingValue;
                this.moreIsBetter = moreIsBetter;
                this.showOnlyIfValueIsDifferent = showOnlyIfValueIsDifferent;
                this.showIfTrue = true;
            }

            public ConfigurableDifficultyDescriptionSection(ConfigEntry<int> configEntry, bool isDelta = true, float deltaStartingValue = 0f, bool moreIsBetter = true, bool showOnlyIfValueIsDifferent = false)
            {
                this.configEntryFloat = null;
                this.configEntryInt = configEntry;
                this.configEntryBool = null;
                this.configEntryString = null;
                this.isDelta = isDelta;
                this.deltaStartingValue = deltaStartingValue;
                this.moreIsBetter = moreIsBetter;
                this.showOnlyIfValueIsDifferent = showOnlyIfValueIsDifferent;
                this.showIfTrue = true;
            }

            public ConfigurableDifficultyDescriptionSection(ConfigEntry<bool> configEntry, bool showIfTrue = true)
            {
                this.configEntryFloat = null;
                this.configEntryInt = null;
                this.configEntryBool = configEntry;
                this.configEntryString = null;
                this.isDelta = false;
                this.deltaStartingValue = 0f;
                this.moreIsBetter = false;
                this.showOnlyIfValueIsDifferent = false;
                this.showIfTrue = showIfTrue;
            }

            public ConfigurableDifficultyDescriptionSection(ConfigEntry<string> configEntry, bool showOnlyIfValueIsDifferent = true)
            {
                this.configEntryFloat = null;
                this.configEntryInt = null;
                this.configEntryBool = null;
                this.configEntryString = configEntry;
                this.isDelta = false;
                this.deltaStartingValue = 0f;
                this.moreIsBetter = false;
                this.showOnlyIfValueIsDifferent = showOnlyIfValueIsDifferent;
                this.showIfTrue = true;
            }
        }
        public static bool constructAllStringsPending = false;
        public static bool constructAllStringsFixedUpdateSubbed = false;
        public static void RequestConstructAllStrings()
        {
            constructAllStringsPending = true;

            if (!constructAllStringsFixedUpdateSubbed)
            {
                constructAllStringsFixedUpdateSubbed = true;
                RoR2Application.onFixedUpdate += () =>
                {
                    if (constructAllStringsPending)
                    {
                        constructAllStringsPending = false;

                        if (Language.english != null) ConstructAllStrings(Language.english);
                        if (Language.currentLanguage != null) ConstructAllStrings(Language.currentLanguage);
                    }
                };
            }
        }
        public static void ConstructAllStrings(Language language)
        {
            ConstructItemListStrings(language);
            ConstructEquipmentStrings(language);
            ConstructDifficultyDescriptionString(language);
        }
        public static void ConstructItemListStrings(Language language)
        {
            void ConstructItemListString(Dictionary<ItemIndex, int> itemList, string listName)
            {
                var formattedItemListStringBuilder = new StringBuilder();
                var firstItemAdded = false;
                foreach (var kvp in itemList)
                {
                    var itemDef = ItemCatalog.GetItemDef(kvp.Key);
                    if (itemDef)
                    {
                        if (!firstItemAdded) firstItemAdded = true;
                        else formattedItemListStringBuilder.Append(", ");

                        formattedItemListStringBuilder.AppendFormat(
                            "{0}{1}",
                            Util.GenerateColoredString(
                                Language.currentLanguage.GetLocalizedStringByToken(itemDef.nameToken),
                                ColorCatalog.GetColor(ItemTierCatalog.GetItemTierDef(itemDef.tier).colorIndex)
                            ),
                            kvp.Value > 1 ? "(" + kvp.Value + ")" : ""
                        );
                    }
                }

                AddOrReplaceLanguageString(
                    language.name,
                    "DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION_" + listName,
                    language.GetLocalizedFormattedStringByToken(
                        "DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION_ITEMLIST_" + listName,
                        formattedItemListStringBuilder.ToString()
                    )
                );
            }

            ConstructItemListString(playerStartingItemList, "PLAYERSTARTINGITEMS");
            ConstructItemListString(allyStartingItemList, "ALLYSTARTINGITEMS");
            ConstructItemListString(enemyStartingItemList, "ENEMYSTARTINGITEMS");
        }
        public static void ConstructEquipmentStrings(Language language)
        {
            void ConstructEquipmentString(EquipmentIndex equipmentIndex, string equipmentTypeName)
            {
                var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (equipmentDef)
                {
                    AddOrReplaceLanguageString(
                        language.name,
                        "DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION_" + equipmentTypeName,
                        language.GetLocalizedFormattedStringByToken(
                            "DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION_EQUIPMENT_" + equipmentTypeName,
                            Util.GenerateColoredString(
                                Language.currentLanguage.GetLocalizedStringByToken(equipmentDef.nameToken),
                                ColorCatalog.GetColor(ColorCatalog.ColorIndex.Equipment)
                            )
                        )
                    );
                }
            }

            ConstructEquipmentString(playerStartingEquipmentIndex, "PLAYERSTARTINGEQUIPMENT");
        }
        public static void ConstructDifficultyDescriptionString(Language language)
        {
            StringBuilder stringBuilder = new StringBuilder(language.GetLocalizedStringByToken("DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION"));

            var sections = new List<ConfigurableDifficultyDescriptionSection>()
            {
                new ConfigurableDifficultyDescriptionSection(difficultyScaling.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(teleporterRadius.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(teleporterChargeSpeed.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(teleporterDischargeSpeed.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(ambientLevelCap.bepinexConfigEntry, isDelta: false, showOnlyIfValueIsDifferent: true),

                new ConfigurableDifficultyDescriptionSection(playerMaxHealth.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerMaxShield.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerRegen.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerSpeed.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerJumpPower.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerDamage.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerAttackSpeed.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerCrit.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerCritDamage.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerArmor.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerCurse.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(playerCooldowns.bepinexConfigEntry, moreIsBetter: false),

                new ConfigurableDifficultyDescriptionSection(playerStartingItems.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(playerStartingEquipment.bepinexConfigEntry),

                new ConfigurableDifficultyDescriptionSection(allyMaxHealth.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyMaxShield.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyRegen.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allySpeed.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyJumpPower.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyDamage.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyAttackSpeed.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyCrit.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyCritDamage.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyArmor.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyCurse.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(allyCooldowns.bepinexConfigEntry, moreIsBetter: false),

                new ConfigurableDifficultyDescriptionSection(allyStartingHealth.bepinexConfigEntry, deltaStartingValue: 100f),
                new ConfigurableDifficultyDescriptionSection(allyHealing.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyFallDamage.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(allyFallDamageIsLethal.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(allyPermanentDamage.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(allyStartingItems.bepinexConfigEntry),

                new ConfigurableDifficultyDescriptionSection(enemyMaxHealth.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyMaxShield.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyRegen.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemySpeed.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyJumpPower.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyDamage.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyAttackSpeed.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyCrit.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyCritDamage.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyArmor.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyCurse.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(enemyCooldowns.bepinexConfigEntry),

                new ConfigurableDifficultyDescriptionSection(enemyStartingHealth.bepinexConfigEntry, deltaStartingValue: 100f),
                new ConfigurableDifficultyDescriptionSection(enemyHealing.bepinexConfigEntry, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyFallDamage.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(enemyFallDamageIsLethal.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(enemyPermanentDamage.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(enemyGoldDrops.bepinexConfigEntry),
                new ConfigurableDifficultyDescriptionSection(enemyStartingItems.bepinexConfigEntry),

                new ConfigurableDifficultyDescriptionSection(countsAsHardMode.bepinexConfigEntry)
            };

            var sectionTokenPrefix = "DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION_";
            bool atLeastOneRuleChanged = false;
            void TryAddFirstNewline()
            {
                if (!atLeastOneRuleChanged)
                {
                    stringBuilder.Append(System.Environment.NewLine);
                    atLeastOneRuleChanged = true;
                }
            }

            foreach (var section in sections)
            {
                if (section.configEntryFloat != null || section.configEntryInt != null)
                {
                    var name = "";
                    var value = 0f;
                    var defaultValue = 0f;
                    if (section.configEntryFloat != null)
                    {
                        name = section.configEntryFloat.Definition.Key;
                        value = section.configEntryFloat.Value;
                        defaultValue = (float)section.configEntryFloat.DefaultValue;
                    }
                    if (section.configEntryInt != null)
                    {
                        name = section.configEntryInt.Definition.Key;
                        value = section.configEntryInt.Value;
                        defaultValue = (int)section.configEntryInt.DefaultValue;
                    }
                    if ((section.isDelta && value != section.deltaStartingValue || !section.isDelta) && (section.showOnlyIfValueIsDifferent && value != defaultValue || !section.showOnlyIfValueIsDifferent))
                    {
                        var formattedValue = Mathf.RoundToInt(Mathf.Abs(section.isDelta ? section.deltaStartingValue - value : value)).ToString();
                        if (section.isDelta)
                        {
                            var style = value > section.deltaStartingValue == section.moreIsBetter ? "<style=cIsHealing>" : "<style=cIsHealth>";
                            formattedValue = style + (value > section.deltaStartingValue ? "+" : "-") + formattedValue;
                        }

                        TryAddFirstNewline();
                        stringBuilder.Append(System.Environment.NewLine);
                        stringBuilder.Append(language.GetLocalizedFormattedStringByToken(sectionTokenPrefix + name.ToUpperInvariant(), formattedValue));
                    }
                }
                if (section.configEntryBool != null && section.configEntryBool.Value == section.showIfTrue)
                {
                    TryAddFirstNewline();
                    stringBuilder.Append(System.Environment.NewLine);
                    stringBuilder.Append(language.GetLocalizedStringByToken(sectionTokenPrefix + section.configEntryBool.Definition.Key.ToUpperInvariant()));
                }
                if (section.configEntryString != null && (section.showOnlyIfValueIsDifferent && section.configEntryString.Value != (string)section.configEntryString.DefaultValue || !section.showOnlyIfValueIsDifferent))
                {
                    TryAddFirstNewline();
                    stringBuilder.Append(System.Environment.NewLine);
                    stringBuilder.Append(language.GetLocalizedStringByToken(sectionTokenPrefix + section.configEntryString.Definition.Key.ToUpperInvariant()));
                }
            }

            if (!atLeastOneRuleChanged)
            {
                stringBuilder.Append(System.Environment.NewLine);
                stringBuilder.Append(System.Environment.NewLine);
                stringBuilder.Append(language.GetLocalizedStringByToken("DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION_NOCHANGES"));
            }

            AddOrReplaceLanguageString(language.name, configurableDifficultyDef.descriptionToken, stringBuilder.ToString());
        }
    }
}
