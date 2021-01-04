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
        float InterestRate { get; set; }

        float TaxIn { get; set; }

        float TaxOut { get; set; }

        int DailyOverdraftRenownLose { get; set; }

        float ReinvestmentRate { get; set; }
    }

    /// <summary>
    /// Fallback mod settings values if MCM is not used.
    /// </summary>
    public class HardcodedSettings : ISettingsProvider
    {
        public float InterestRate { get; set; } = 0.002f;

        public float TaxIn { get; set; } = 0.025f;

        public float TaxOut { get; set; } = 0.035f;

        public int DailyOverdraftRenownLose { get; set; } = 10;

        public float ReinvestmentRate { get; set; } = 0.2f;
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
                InterestRate = 0.002f,
                TaxIn = 0.025f,
                TaxOut = 0.035f,
                DailyOverdraftRenownLose = 15,
                ReinvestmentRate = 0.2f
            };
            var easy = new Settings()
            {
                InterestRate = 0.008f,
                TaxIn = 0.010f,
                TaxOut = 0.015f,
                DailyOverdraftRenownLose = 10,
                ReinvestmentRate = 0.2f
            };
            var veryEasy = new Settings()
            {
                InterestRate = 0.014f,
                TaxIn = 0.04f,
                TaxOut = 0.06f,
                DailyOverdraftRenownLose = 5,
                ReinvestmentRate = 0.2f
            };

            // Includes the 'Default' preset that MCM provides
            var basePresets = base.GetAvailablePresets();
            basePresets["Default"] = () => realistic;
            basePresets.Add("Realistic", () => realistic);
            basePresets.Add("Easy", () => easy);
            basePresets.Add("Very Easy", () => veryEasy);
            return basePresets;
        }

        [SettingPropertyGroup("{=IronBank_General}General")]
        [SettingPropertyFloatingInteger(
            "{=IronBank_Interests}Interest rate",
            minValue: 0.0f, maxValue: 0.05f, valueFormat: "P", RequireRestart = false,
            HintText = "Daily interest rate generated by your account."
        )]
        public float InterestRate { get; set; }

        [SettingPropertyGroup("{=IronBank_General}General")]
        [SettingPropertyFloatingInteger(
            "{=IronBank_Interests}Taxes on deposit",
            minValue: 0.0f, maxValue: 0.10f, valueFormat: "P", RequireRestart = false,
            HintText = "A tax applyed by the bank on every deposit."
        )]
        public float TaxIn { get; set; }

        [SettingPropertyGroup("{=IronBank_General}General")]
        [SettingPropertyFloatingInteger(
            "{=IronBank_Interests}Taxes on withdraw",
            minValue: 0.0f, maxValue: 0.10f, valueFormat: "P", RequireRestart = false,
            HintText = "A tax applyed by the bank on every widthdraw."
        )]
        public float TaxOut { get; set; }

        [SettingPropertyGroup("{=IronBank_General}General")]
        [SettingPropertyInteger(
            "{=IronBank_Interests}Daily overdraft renown lose",
            minValue: 0, maxValue: 100, valueFormat: "0", RequireRestart = false,
            HintText = "Daily renown lose when your bank account is overdrawn."
        )]
        public int DailyOverdraftRenownLose { get; set; }

        [SettingPropertyGroup("{=IronBank_General}General")]
        [SettingPropertyFloatingInteger(
            "{=IronBank_Interests}Reinvestment rate",
            minValue: 0f, maxValue: 1f, valueFormat: "P", RequireRestart = false,
            HintText = "Rate of interests to deposit in bank account."
        )]
        public float ReinvestmentRate { get; set; }
    }
}