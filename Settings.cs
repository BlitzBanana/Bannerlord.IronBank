﻿using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Settings.Base;
using MCM.Abstractions.Settings.Base.Global;
using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace IronBank
{
    /// <summary>
    /// Generic mod settings interface.
    /// </summary>
    public interface ISettingsProvider
    {
        float InterestsScale { get; set; }

        float TaxInScale { get; set; }

        float TaxOutScale { get; set; }

        float DailyOverdraftRenownLoseScale { get; set; }
    }

    /// <summary>
    /// Fallback mod settings values if MCM is not used.
    /// </summary>
    public class HardcodedSettings : ISettingsProvider
    {
        public float InterestsScale { get; set; } = 0.002f;

        public float TaxInScale { get; set; } = 0.025f;

        public float TaxOutScale { get; set; } = 0.035f;

        public float DailyOverdraftRenownLoseScale { get; set; } = 10;
    }

    /// <summary>
    /// Mod settings values that are persisted by MCM and shared across campaigns.
    /// </summary>
    public class Settings : AttributeGlobalSettings<Settings>, ISettingsProvider
    {
        public override string Id => "IronBank";

        public override string DisplayName => new TextObject(
            "{=Settings_Name}Iron Bank {VERSION}",
            new Dictionary<string, TextObject>
            {
                { "VERSION", new TextObject(typeof(Settings).Assembly.GetName().Version.ToString(3)) }
            }
        ).ToString();

        /// <summary>
        /// Mod settings are persisted to %documents%\Mount and Blade II Bannerlord\Configs\ModSettings\Global\IronBank\IronBank.json
        /// </summary>
        public override string FolderName { get; } = "IronBank";

        public override string FormatType { get; } = "json2";

        /// <summary>
        /// Provide some settings value presets.
        /// </summary>
        /// <returns>Settings presets</returns>
        public override IDictionary<string, Func<BaseSettings>> GetAvailablePresets()
        {
            var realistic = new Settings()
            {
                InterestsScale = 0.5f,
                TaxInScale = 1.4f,
                TaxOutScale = 1.4f,
                DailyOverdraftRenownLoseScale = 1.4f
            };
            var easy = new Settings()
            {
                InterestsScale = 1f,
                TaxInScale = 1f,
                TaxOutScale = 1f,
                DailyOverdraftRenownLoseScale = 1f
            };
            var veryEasy = new Settings()
            {
                InterestsScale = 1.4f,
                TaxInScale = 0.4f,
                TaxOutScale = 0.4f,
                DailyOverdraftRenownLoseScale = 0.5f
            };

            // Includes the 'Default' preset that MCM provides
            var basePresets = base.GetAvailablePresets();
            basePresets["Default"] = () => easy;
            basePresets.Add("Realistic", () => realistic);
            basePresets.Add("Easy", () => easy);
            basePresets.Add("Very Easy", () => veryEasy);
            return basePresets;
        }

        [SettingPropertyGroup("{=IronBank_General}General")]
        [SettingPropertyFloatingInteger(
            "{=IronBank_Interests}Interests rate",
            minValue: 0.0f, maxValue: 3f, valueFormat: "P", RequireRestart = false,
            HintText = "Daily interest rate generated by your account. 0 = no interests (disabled), 100 = base settings (normal), 300 = x3 (easy)"
        )]
        public float InterestsScale { get; set; }

        [SettingPropertyGroup("{=IronBank_General}General")]
        [SettingPropertyFloatingInteger(
            "{=IronBank_Interests}Taxes on deposit",
            minValue: 0.0f, maxValue: 3f, valueFormat: "P", RequireRestart = false,
            HintText = "A tax applyed by the bank on every deposit. 0 = no tax (easy), 100 = base settings (normal), 300 = x3 (hard)"
        )]
        public float TaxInScale { get; set; }

        [SettingPropertyGroup("{=IronBank_General}General")]
        [SettingPropertyFloatingInteger(
            "{=IronBank_Interests}Taxes on withdraw",
            minValue: 0.0f, maxValue: 3f, valueFormat: "P", RequireRestart = false,
            HintText = "A tax applyed by the bank on every widthdraw. 0 = no tax (easy), 100 = base settings (normal), 300 = x3 (hard)"
        )]
        public float TaxOutScale { get; set; }

        [SettingPropertyGroup("{=IronBank_General}General")]
        [SettingPropertyFloatingInteger(
            "{=IronBank_Interests}Daily overdraft renown lose",
            minValue: 0.0f, maxValue: 3f, valueFormat: "P", RequireRestart = false,
            HintText = "Daily renown lose when your bank account is overdrawn. 0 = no penatly (easy), 100 = base settings (normal), 300 = x3 (hard)"
        )]
        public float DailyOverdraftRenownLoseScale { get; set; }
    }
}
