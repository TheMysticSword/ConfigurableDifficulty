using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
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
    [R2APISubmoduleDependency(nameof(DifficultyAPI), nameof(CommandHelper))]
    public class ConfigurableDifficultyPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.themysticsword.configurabledifficulty";
        public const string PluginName = "ConfigurableDifficulty";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<float> difficultyScaling;
        public static ConfigEntry<float> playerRegen;
        public static ConfigEntry<float> allyStartingHealth;
        public static ConfigEntry<float> allyMaxHealth;
        public static ConfigEntry<float> allyHealing;
        public static ConfigEntry<float> allyArmor;
        public static ConfigEntry<float> allyFallDamage;
        public static ConfigEntry<bool> allyFallDamageIsLethal;
        public static ConfigEntry<float> allyPermanentDamage;
        public static ConfigEntry<float> enemySpeed;
        public static ConfigEntry<float> enemyCooldowns;
        public static ConfigEntry<float> enemyGoldDrops;
        public static ConfigEntry<float> teleporterRadius;
        public static ConfigEntry<int> ambientLevelCap;
        public static ConfigEntry<bool> countsAsHardMode;

        public static DifficultyDef configurableDifficultyDef;
        public static DifficultyIndex configurableDifficultyIndex;

        public static ConfigFile config;

        public void Awake()
        {
            CommandHelper.AddToConsoleWhenReady();

            config = new ConfigFile(Paths.ConfigPath + "\\ConfigurableDifficulty.cfg", true);
            difficultyScaling = config.Bind("", "DifficultyScaling", 50f, "Difficulty scaling over time (in %). Set to positive values to increase scaling, set to negative values to reduce it.");
            playerRegen = config.Bind("", "PlayerRegen", -40f, "Player health regeneration (in %). Set to positive values to increase regen, set to negative values to reduce it.");
            allyStartingHealth = config.Bind("", "AllyStartingHealth", 50f, "Ally starting health (in %). Set between 0-100% to change how much health allies spawn with.");
            allyMaxHealth = config.Bind("", "AllyMaxHealth", 0f, "Ally maximum health (in %). Set to positive values to increase health, set to negative values to reduce it.");
            allyHealing = config.Bind("", "AllyHealing", -50f, "Ally healing (in %). Set to positive values to increase healing, set to negative values to reduce it. Values of -100% and below make healing 0.");
            allyArmor = config.Bind("", "AllyArmor", 0f, "Ally armor. Set to positive values to increase armor, set to negative values to reduce it.");
            allyFallDamage = config.Bind("", "AllyFallDamage", 100f, "Ally fall damage (in %). Set to positive values to increase fall damage, set to negative values to reduce it. Values of -100% and below make fall damage 0.");
            allyFallDamageIsLethal = config.Bind("", "AllyFallDamageIsLethal", true, "Allies can die from fall damage.");
            allyPermanentDamage = config.Bind("", "AllyPermanentDamage", 40f, "Whenever an ally takes damage, their maximum health is reduced by a portion of taken damage (in %). Set to positive values to increase permanent damage, set to negative values to reduce it. Values of 0% and below disable permanent damage.");
            enemySpeed = config.Bind("", "EnemySpeed", 40f, "Enemy movement speed (in %). Set to positive values to increase speed, set to negative values to reduce it.");
            enemyCooldowns = config.Bind("", "EnemyCooldowns", -50f, "Enemy skill cooldowns (in %). Set to positive values to increase cooldowns, set to negative values to reduce them.");
            enemyGoldDrops = config.Bind("", "EnemyGoldDrops", -20f, "Enemy gold drops (in %). Set to positive values to increase gold drops, set to negative values to reduce them. Values of -100% and below set gold drops to 0.");
            teleporterRadius = config.Bind("", "TeleporterRadius", -50f, "Teleporter radius (in %). Set to positive values to increase the radius, set to negative values to reduce it. Values of -100% and below make the radius 0.");
            ambientLevelCap = config.Bind("", "AmbientLevelCap", 99, "Ambient level cap. Ambient level affects monsters and NPC allies.");
            countsAsHardMode = config.Bind("", "CountsAsHardMode", true, "Completing a run on this difficulty will unlock the selected survivor's Mastery skin.");

            OnConfigReloaded();

            var assetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "configurabledifficultyassetbundle"));

            configurableDifficultyDef = new DifficultyDef(
                2f,
                "DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_NAME",
                null,
                "DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION",
                new Color32(181, 206, 195, 255),
                "mod_cfgdif",
                false
            );
            SetConfigRelatedDifficultyDefValues();
            configurableDifficultyIndex = DifficultyAPI.AddDifficulty(configurableDifficultyDef, assetBundle.LoadAsset<Sprite>("Assets/Misc/Textures/texConfigurableDifficultyIcon.png"));

            On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;

            On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            IL.RoR2.HealthComponent.Heal += HealthComponent_Heal;
            IL.RoR2.DeathRewards.OnKilledServer += DeathRewards_OnKilledServer;
            On.RoR2.HoldoutZoneController.Awake += HoldoutZoneController_Awake;
            On.RoR2.Run.RecalculateDifficultyCoefficentInternal += Run_RecalculateDifficultyCoefficentInternal;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            On.RoR2.HealthComponent.Awake += HealthComponent_Awake;
        }

        private struct ConfigurableDifficultyDescriptionSection
        {
            public ConfigEntry<float> configEntryFloat;
            public ConfigEntry<int> configEntryInt;
            public ConfigEntry<bool> configEntryBool;
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
                this.isDelta = false;
                this.deltaStartingValue = 0f;
                this.moreIsBetter = false;
                this.showOnlyIfValueIsDifferent = false;
                this.showIfTrue = showIfTrue;
            }
        }
        private string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token)
        {
            var result = orig(self, token);
            if (token == configurableDifficultyDef.descriptionToken)
            {
                StringBuilder stringBuilder = new StringBuilder(result);
                
                var sections = new List<ConfigurableDifficultyDescriptionSection>()
                {
                    new ConfigurableDifficultyDescriptionSection(difficultyScaling, moreIsBetter: false),
                    new ConfigurableDifficultyDescriptionSection(playerRegen),
                    new ConfigurableDifficultyDescriptionSection(allyStartingHealth, deltaStartingValue: 100f),
                    new ConfigurableDifficultyDescriptionSection(allyMaxHealth),
                    new ConfigurableDifficultyDescriptionSection(allyHealing),
                    new ConfigurableDifficultyDescriptionSection(allyArmor),
                    new ConfigurableDifficultyDescriptionSection(allyFallDamage, moreIsBetter: false),
                    new ConfigurableDifficultyDescriptionSection(allyFallDamageIsLethal),
                    new ConfigurableDifficultyDescriptionSection(allyPermanentDamage, moreIsBetter: false),
                    new ConfigurableDifficultyDescriptionSection(enemySpeed, moreIsBetter: false),
                    new ConfigurableDifficultyDescriptionSection(enemyCooldowns),
                    new ConfigurableDifficultyDescriptionSection(enemyGoldDrops),
                    new ConfigurableDifficultyDescriptionSection(teleporterRadius),
                    new ConfigurableDifficultyDescriptionSection(ambientLevelCap, isDelta: false, showOnlyIfValueIsDifferent: true),
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
                            stringBuilder.Append(self.GetLocalizedFormattedStringByToken(sectionTokenPrefix + name.ToUpperInvariant(), formattedValue));
                        }
                    }
                    if (section.configEntryBool != null && section.configEntryBool.Value == section.showIfTrue)
                    {
                        TryAddFirstNewline();
                        stringBuilder.Append(System.Environment.NewLine);
                        stringBuilder.Append(self.GetLocalizedStringByToken(sectionTokenPrefix + section.configEntryBool.Definition.Key.ToUpperInvariant()));
                    }
                }

                if (!atLeastOneRuleChanged)
                {
                    stringBuilder.Append(System.Environment.NewLine);
                    stringBuilder.Append(System.Environment.NewLine);
                    stringBuilder.Append(self.GetLocalizedStringByToken("DIFFICULTY_CONFIGURABLEDIFFICULTYMOD_DESCRIPTION_NOCHANGES"));
                }

                result = stringBuilder.ToString();
            }
            return result;
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
                        if (sender.isPlayerControlled) args.regenMultAdd += playerRegen.Value / 100f;
                        args.healthMultAdd += allyMaxHealth.Value / 100f;
                        args.armorAdd += allyArmor.Value;
                        break;
                    case TeamIndex.Monster:
                        if (enemySpeed.Value > 0f) args.moveSpeedMultAdd += enemySpeed.Value / 100f;
                        else args.moveSpeedReductionMultAdd -= enemySpeed.Value / 100f;
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
                    if (hc.body.teamComponent.teamIndex == TeamIndex.Player && Run.instance.selectedDifficulty == configurableDifficultyIndex)
                    {
                        healAmount *= Mathf.Max(1f + allyHealing.Value / 100f, 0f);
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
        }

        private void HoldoutZoneController_calcRadius(ref float radius)
        {
            if (Run.instance.selectedDifficulty == configurableDifficultyIndex)
            {
                radius *= Mathf.Max(1f + teleporterRadius.Value / 100f, 0f);
            }
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
                if (self.body && self.body.teamComponent.teamIndex == TeamIndex.Player)
                {
                    damageInfo.damage *= 1f + allyFallDamage.Value / 100f;
                    if (allyFallDamageIsLethal.Value)
                    {
                        damageInfo.damageType &= ~DamageType.NonLethal;
                        damageInfo.damageType |= DamageType.BypassOneShotProtection;
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
                if (victimBody && victimBody.teamComponent.teamIndex == TeamIndex.Player && Run.instance.selectedDifficulty == configurableDifficultyIndex)
                {
                    float takenDamagePercent = damageReport.damageDealt / healthComponent.fullCombinedHealth * 100f;
                    int permanentDamage = Mathf.FloorToInt(takenDamagePercent * allyPermanentDamage.Value / 100f);
                    for (int l = 0; l < permanentDamage; l++)
                    {
                        victimBody.AddBuff(RoR2Content.Buffs.PermanentCurse);
                    }
                }
            }
        }

        [ConCommand(commandName = "mod_cfgdif_reload", flags = ConVarFlags.None, helpText = "Reload the config file of ConfigurableDifficulty.")]
        private static void CCReloadConfig(ConCommandArgs args)
        {
            config.Reload();
            OnConfigReloaded();
        }

        public static void OnConfigReloaded()
        {
            playerRegen.Value = Mathf.Clamp(playerRegen.Value, 0f, 100f);
            allyHealing.Value = Mathf.Max(allyHealing.Value, -100f);
            allyFallDamage.Value = Mathf.Max(allyFallDamage.Value, -100f);
            allyPermanentDamage.Value = Mathf.Max(allyPermanentDamage.Value, 0f);
            enemyGoldDrops.Value = Mathf.Max(enemyGoldDrops.Value, -100f);
            teleporterRadius.Value = Mathf.Max(teleporterRadius.Value, -100f);

            SetConfigRelatedDifficultyDefValues();
        }

        public static void SetConfigRelatedDifficultyDefValues()
        {
            if (configurableDifficultyDef != null)
            {
                configurableDifficultyDef.scalingValue = 2f + difficultyScaling.Value / 50f;
                configurableDifficultyDef.countsAsHardMode = countsAsHardMode.Value;
            }
        }
    }
}
