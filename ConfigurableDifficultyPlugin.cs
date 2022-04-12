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

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ConfigurableDifficulty
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [R2APISubmoduleDependency(nameof(DifficultyAPI), nameof(CommandHelper), nameof(RecalculateStatsAPI))]
    public class ConfigurableDifficultyPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.themysticsword.configurabledifficulty";
        public const string PluginName = "ConfigurableDifficulty";
        public const string PluginVersion = "1.0.1";

        // General
        public static ConfigEntry<float> difficultyScaling;
        public static ConfigEntry<float> teleporterRadius;
        public static ConfigEntry<float> teleporterChargeSpeed;
        public static ConfigEntry<float> teleporterDischargeSpeed;
        public static ConfigEntry<int> ambientLevelCap;
        public static ConfigEntry<bool> countsAsHardMode;

        // Player stats
        public static ConfigEntry<float> playerMaxHealth;
        public static ConfigEntry<float> playerMaxShield;
        public static ConfigEntry<float> playerRegen;
        public static ConfigEntry<float> playerBaseRegen;
        public static ConfigEntry<float> playerSpeed;
        public static ConfigEntry<float> playerJumpPower;
        public static ConfigEntry<float> playerDamage;
        public static ConfigEntry<float> playerAttackSpeed;
        public static ConfigEntry<float> playerCrit;
        public static ConfigEntry<float> playerCritDamage;
        public static ConfigEntry<float> playerArmor;
        public static ConfigEntry<float> playerCurse;
        public static ConfigEntry<float> playerCooldowns;

        // Player modifiers
        public static ConfigEntry<string> playerStartingItems;
        public static ConfigEntry<string> playerStartingEquipment;

        // Ally stats
        public static ConfigEntry<float> allyMaxHealth;
        public static ConfigEntry<float> allyMaxShield;
        public static ConfigEntry<float> allyRegen;
        public static ConfigEntry<float> allyBaseRegen;
        public static ConfigEntry<float> allySpeed;
        public static ConfigEntry<float> allyJumpPower;
        public static ConfigEntry<float> allyDamage;
        public static ConfigEntry<float> allyAttackSpeed;
        public static ConfigEntry<float> allyCrit;
        public static ConfigEntry<float> allyCritDamage;
        public static ConfigEntry<float> allyArmor;
        public static ConfigEntry<float> allyCurse;
        public static ConfigEntry<float> allyCooldowns;

        // Ally modifiers
        public static ConfigEntry<float> allyStartingHealth;
        public static ConfigEntry<float> allyHealing;
        public static ConfigEntry<float> allyFallDamage;
        public static ConfigEntry<bool> allyFallDamageIsLethal;
        public static ConfigEntry<float> allyPermanentDamage;
        public static ConfigEntry<string> allyStartingItems;

        // Enemy stats
        public static ConfigEntry<float> enemyMaxHealth;
        public static ConfigEntry<float> enemyMaxShield;
        public static ConfigEntry<float> enemyRegen;
        public static ConfigEntry<float> enemyBaseRegen;
        public static ConfigEntry<float> enemySpeed;
        public static ConfigEntry<float> enemyJumpPower;
        public static ConfigEntry<float> enemyDamage;
        public static ConfigEntry<float> enemyAttackSpeed;
        public static ConfigEntry<float> enemyCrit;
        public static ConfigEntry<float> enemyCritDamage;
        public static ConfigEntry<float> enemyArmor;
        public static ConfigEntry<float> enemyCurse;
        public static ConfigEntry<float> enemyCooldowns;

        // Enemy modifiers
        public static ConfigEntry<float> enemyStartingHealth;
        public static ConfigEntry<float> enemyHealing;
        public static ConfigEntry<float> enemyFallDamage;
        public static ConfigEntry<bool> enemyFallDamageIsLethal;
        public static ConfigEntry<float> enemyPermanentDamage;
        public static ConfigEntry<float> enemyGoldDrops;
        public static ConfigEntry<string> enemyStartingItems;

        public static DifficultyDef configurableDifficultyDef;
        public static DifficultyIndex configurableDifficultyIndex;

        public static ConfigFile config;
        public static BepInEx.Logging.ManualLogSource logger;

        public void Awake()
        {
            CommandHelper.AddToConsoleWhenReady();

            config = new ConfigFile(Paths.ConfigPath + "\\ConfigurableDifficulty.cfg", true);
            logger = Logger;
            
            difficultyScaling = config.Bind("General", "DifficultyScaling", 50f, "Difficulty scaling over time (in %)");
            teleporterRadius = config.Bind("General", "TeleporterRadius", -50f, "Teleporter radius (in %). Values of -100% and below set the radius to 0m.");
            teleporterChargeSpeed = config.Bind("General", "TeleporterChargeSpeed", 0f, "Teleporter charge speed (in %). Values of -100% and below make the teleporter unchargeable.");
            teleporterDischargeSpeed = config.Bind("General", "TeleporterDischargeSpeed", 0f, "Teleporter discharge speed when all players are outside the radius (in % per second)");
            ambientLevelCap = config.Bind("General", "AmbientLevelCap", 99, "Ambient level cap. Ambient level affects monsters and NPC allies.");
            countsAsHardMode = config.Bind("General", "CountsAsHardMode", true, "Completing a run on this difficulty will unlock the selected survivor's Mastery skin.");

            playerMaxHealth = config.Bind("Player stats", "PlayerMaxHealth", 0f, "Player maximum health (in %)");
            playerMaxShield = config.Bind("Player stats", "PlayerMaxShield", 0f, "Player maximum shield (in %)");
            playerRegen = config.Bind("Player stats", "PlayerRegen", -40f, "Player health regeneration (in %)");
            playerBaseRegen = config.Bind("Player stats", "PlayerBaseRegen", 0f, "Player base health regeneration (in HP/s)");
            playerSpeed = config.Bind("Player stats", "PlayerSpeed", 0f, "Player movement speed (in %)");
            playerJumpPower = config.Bind("Player stats", "PlayerJumpPower", 0f, "Player jump power (in %)");
            playerDamage = config.Bind("Player stats", "PlayerDamage", 0f, "Player damage (in %)");
            playerAttackSpeed = config.Bind("Player stats", "PlayerAttackSpeed", 0f, "Player attack speed (in %)");
            playerCrit = config.Bind("Player stats", "PlayerCrit", 0f, "Player critical strike chance (in %)");
            playerCritDamage = config.Bind("Player stats", "PlayerCritDamage", 0f, "Player critical strike chance damage (in %)");
            playerArmor = config.Bind("Player stats", "PlayerArmor", 0f, "Player armor");
            playerCurse = config.Bind("Player stats", "PlayerCurse", 0f, "Player maximum health reduction");
            playerCooldowns = config.Bind("Player stats", "PlayerCooldowns", 0f, "Player skill cooldowns");

            playerStartingItems = config.Bind("Player modifiers", "PlayerStartingItems", "", "Player starting items. Uses internal item names, comma-separated. Add a colon and a number to select the amount of the starting item. Example: Squid,Seed,GhostOnKill:3,BarrierOnOverHeal,FlatHealth,HeadHunter:99");
            playerStartingEquipment = config.Bind("Player modifiers", "PlayerStartingEquipment", "", "Player starting equipment. Uses the internal equipment name. Example: GoldGat");

            allyMaxHealth = config.Bind("Ally stats", "AllyMaxHealth", 0f, "Ally maximum health (in %)");
            allyMaxShield = config.Bind("Ally stats", "AllyMaxShield", 0f, "Ally maximum shield (in %)");
            allyRegen = config.Bind("Ally stats", "AllyRegen", 0f, "Ally health regeneration (in %)");
            allyBaseRegen = config.Bind("Player stats", "AllyBaseRegen", 0f, "Ally base health regeneration (in HP/s)");
            allySpeed = config.Bind("Ally stats", "AllySpeed", 0f, "Ally movement speed (in %)");
            allyJumpPower = config.Bind("Ally stats", "AllyJumpPower", 0f, "Ally jump power (in %)");
            allyDamage = config.Bind("Ally stats", "AllyDamage", 0f, "Ally damage (in %)");
            allyAttackSpeed = config.Bind("Ally stats", "AllyAttackSpeed", 0f, "Ally attack speed (in %)");
            allyCrit = config.Bind("Ally stats", "AllyCrit", 0f, "Ally critical strike chance (in %)");
            allyCritDamage = config.Bind("Ally stats", "AllyCritDamage", 0f, "Ally critical strike chance damage (in %)");
            allyArmor = config.Bind("Ally stats", "AllyArmor", 0f, "Ally armor");
            allyCurse = config.Bind("Ally stats", "AllyCurse", 0f, "Ally maximum health reduction");
            allyCooldowns = config.Bind("Ally stats", "AllyCooldowns", 0f, "Ally skill cooldowns");

            allyStartingHealth = config.Bind("Ally modifiers", "AllyStartingHealth", 50f, "Ally starting health (in %, between 0-100%)");
            allyHealing = config.Bind("Ally modifiers", "AllyHealing", -50f, "Ally healing (in %). Values of -100% and below disable ally healing.");
            allyFallDamage = config.Bind("Ally modifiers", "AllyFallDamage", 100f, "Ally fall damage (in %). Values of -100% and below disable ally fall damage.");
            allyFallDamageIsLethal = config.Bind("Ally modifiers", "AllyFallDamageIsLethal", true, "Allies can die from fall damage.");
            allyPermanentDamage = config.Bind("Ally modifiers", "AllyPermanentDamage", 40f, "Whenever an ally takes damage, their maximum health is reduced by a portion of taken damage (in %). Values of 0% and below disable permanent damage.");
            allyStartingItems = config.Bind("Ally modifiers", "AllyStartingItems", "", "Ally starting items. Uses internal item names, comma-separated. Add a colon and a number to select the amount of the starting item. Example: Squid,Seed,GhostOnKill:3,BarrierOnOverHeal,FlatHealth,HeadHunter:99");

            enemyMaxHealth = config.Bind("Enemy stats", "EnemyMaxHealth", 0f, "Enemy maximum health (in %)");
            enemyMaxShield = config.Bind("Enemy stats", "EnemyMaxShield", 0f, "Enemy maximum shield (in %)");
            enemyRegen = config.Bind("Enemy stats", "EnemyRegen", 0f, "Enemy health regeneration (in %)");
            enemyBaseRegen = config.Bind("Player stats", "EnemyBaseRegen", 0f, "Enemy base health regeneration (in HP/s)");
            enemySpeed = config.Bind("Enemy stats", "EnemySpeed", 40f, "Enemy movement speed (in %)");
            enemyJumpPower = config.Bind("Enemy stats", "EnemyJumpPower", 0f, "Enemy jump power (in %)");
            enemyDamage = config.Bind("Enemy stats", "EnemyDamage", 0f, "Enemy damage (in %)");
            enemyAttackSpeed = config.Bind("Enemy stats", "EnemyAttackSpeed", 0f, "Enemy attack speed (in %)");
            enemyCrit = config.Bind("Enemy stats", "EnemyCrit", 0f, "Enemy critical strike chance (in %)");
            enemyCritDamage = config.Bind("Enemy stats", "EnemyCritDamage", 0f, "Enemy critical strike chance damage (in %)");
            enemyArmor = config.Bind("Enemy stats", "EnemyArmor", 0f, "Enemy armor");
            enemyCurse = config.Bind("Enemy stats", "EnemyCurse", 0f, "Enemy maximum health reduction");
            enemyCooldowns = config.Bind("Enemy stats", "EnemyCooldowns", -50f, "Enemy skill cooldowns");

            enemyStartingHealth = config.Bind("Enemy modifiers", "EnemyStartingHealth", 100f, "Enemy starting health (in %, between 0-100%)");
            enemyHealing = config.Bind("Enemy modifiers", "EnemyHealing", 0f, "Enemy healing (in %). Values of -100% and below disable enemy healing.");
            enemyFallDamage = config.Bind("Enemy modifiers", "EnemyFallDamage", 0f, "Enemy fall damage (in %). Values of -100% and below disable enemy fall damage.");
            enemyFallDamageIsLethal = config.Bind("Enemy modifiers", "EnemyFallDamageIsLethal", false, "Allies can die from fall damage.");
            enemyPermanentDamage = config.Bind("Enemy modifiers", "EnemyPermanentDamage", 0f, "Whenever an enemy takes damage, their maximum health is reduced by a portion of taken damage (in %). Values of 0% and below disable permanent damage.");
            enemyGoldDrops = config.Bind("Enemy modifiers", "EnemyGoldDrops", -20f, "Enemy gold drops (in %). Set to positive values to increase gold drops, set to negative values to reduce them. Values of -100% and below set gold drops to 0.");
            enemyStartingItems = config.Bind("Enemy modifiers", "EnemyStartingItems", "", "Enemy starting items. Uses internal item names, comma-separated. Add a colon and a number to select the amount of the starting item. Example: Squid,Seed,GhostOnKill:3,BarrierOnOverHeal,FlatHealth,HeadHunter:99");

            OnConfigReloaded();

            var assetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "configurabledifficultyassetbundle"));

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
            configurableDifficultyIndex = DifficultyAPI.AddDifficulty(configurableDifficultyDef, assetBundle.LoadAsset<Sprite>("Assets/Misc/Textures/texConfigurableDifficultyIcon.png"));

            On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;
            RoR2.Language.onCurrentLanguageChanged += Language_onCurrentLanguageChanged;

            On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            IL.RoR2.HealthComponent.Heal += HealthComponent_Heal;
            IL.RoR2.DeathRewards.OnKilledServer += DeathRewards_OnKilledServer;
            On.RoR2.HoldoutZoneController.Awake += HoldoutZoneController_Awake;
            On.RoR2.Run.RecalculateDifficultyCoefficentInternal += Run_RecalculateDifficultyCoefficentInternal;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            On.RoR2.HealthComponent.Awake += HealthComponent_Awake;
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
                    if (self.teamIndex == TeamIndex.Player && Run.instance.selectedDifficulty == configurableDifficultyIndex)
                    {
                        healthComponent.Networkhealth = healthComponent.fullHealth * allyStartingHealth.Value / 100f;
                    }
                }
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

        [ConCommand(commandName = "mod_cfgdif_reload", flags = ConVarFlags.None, helpText = "Reload the config file of ConfigurableDifficulty.")]
        private static void CCReloadConfig(ConCommandArgs args)
        {
            config.Reload();
            OnConfigReloaded();
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

        public static void OnConfigReloaded()
        {
            playerRegen.Value = Mathf.Clamp(playerRegen.Value, 0f, 100f);
            allyHealing.Value = Mathf.Max(allyHealing.Value, -100f);
            allyFallDamage.Value = Mathf.Max(allyFallDamage.Value, -100f);
            allyPermanentDamage.Value = Mathf.Max(allyPermanentDamage.Value, 0f);
            enemyGoldDrops.Value = Mathf.Max(enemyGoldDrops.Value, -100f);
            teleporterRadius.Value = Mathf.Max(teleporterRadius.Value, -100f);

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
                new ConfigurableDifficultyDescriptionSection(difficultyScaling, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(teleporterRadius),
                new ConfigurableDifficultyDescriptionSection(teleporterChargeSpeed),
                new ConfigurableDifficultyDescriptionSection(teleporterDischargeSpeed, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(ambientLevelCap, isDelta: false, showOnlyIfValueIsDifferent: true),

                new ConfigurableDifficultyDescriptionSection(playerMaxHealth),
                new ConfigurableDifficultyDescriptionSection(playerMaxShield),
                new ConfigurableDifficultyDescriptionSection(playerRegen),
                new ConfigurableDifficultyDescriptionSection(playerSpeed),
                new ConfigurableDifficultyDescriptionSection(playerJumpPower),
                new ConfigurableDifficultyDescriptionSection(playerDamage),
                new ConfigurableDifficultyDescriptionSection(playerAttackSpeed),
                new ConfigurableDifficultyDescriptionSection(playerCrit),
                new ConfigurableDifficultyDescriptionSection(playerCritDamage),
                new ConfigurableDifficultyDescriptionSection(playerArmor),
                new ConfigurableDifficultyDescriptionSection(playerCurse, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(playerCooldowns, moreIsBetter: false),

                new ConfigurableDifficultyDescriptionSection(playerStartingItems),
                new ConfigurableDifficultyDescriptionSection(playerStartingEquipment),

                new ConfigurableDifficultyDescriptionSection(allyMaxHealth),
                new ConfigurableDifficultyDescriptionSection(allyMaxShield),
                new ConfigurableDifficultyDescriptionSection(allyRegen),
                new ConfigurableDifficultyDescriptionSection(allySpeed),
                new ConfigurableDifficultyDescriptionSection(allyJumpPower),
                new ConfigurableDifficultyDescriptionSection(allyDamage),
                new ConfigurableDifficultyDescriptionSection(allyAttackSpeed),
                new ConfigurableDifficultyDescriptionSection(allyCrit),
                new ConfigurableDifficultyDescriptionSection(allyCritDamage),
                new ConfigurableDifficultyDescriptionSection(allyArmor),
                new ConfigurableDifficultyDescriptionSection(allyCurse, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(allyCooldowns, moreIsBetter: false),

                new ConfigurableDifficultyDescriptionSection(allyStartingHealth, deltaStartingValue: 100f),
                new ConfigurableDifficultyDescriptionSection(allyHealing),
                new ConfigurableDifficultyDescriptionSection(allyFallDamage, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(allyFallDamageIsLethal),
                new ConfigurableDifficultyDescriptionSection(allyPermanentDamage, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(allyStartingItems),

                new ConfigurableDifficultyDescriptionSection(enemyMaxHealth, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyMaxShield, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyRegen, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemySpeed, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyJumpPower, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyDamage, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyAttackSpeed, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyCrit, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyCritDamage, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyArmor, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyCurse),
                new ConfigurableDifficultyDescriptionSection(enemyCooldowns),

                new ConfigurableDifficultyDescriptionSection(enemyStartingHealth, deltaStartingValue: 100f),
                new ConfigurableDifficultyDescriptionSection(enemyHealing, moreIsBetter: false),
                new ConfigurableDifficultyDescriptionSection(enemyFallDamage),
                new ConfigurableDifficultyDescriptionSection(enemyFallDamageIsLethal),
                new ConfigurableDifficultyDescriptionSection(enemyPermanentDamage),
                new ConfigurableDifficultyDescriptionSection(enemyGoldDrops),
                new ConfigurableDifficultyDescriptionSection(enemyStartingItems),

                new ConfigurableDifficultyDescriptionSection(countsAsHardMode)
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
