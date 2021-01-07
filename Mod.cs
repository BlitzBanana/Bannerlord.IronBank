using MCM.Abstractions.Settings.Base.Global;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace IronBank
{
    public class Mod: MBSubModuleBase
    {

        /// <summary>
        /// Compute current world chaos, based on the wars count.
        /// More wars means higher loan costs but higher account interests !
        /// </summary>
        public static float WorldChaos
        {
            get
            {
                float worldChaos = 0f;
                float warImpact = 1f / (float)Math.Pow(Clan.All.Count, 2);

                foreach (Clan clan1 in Clan.All)
                {
                    foreach (Clan clan2 in Clan.All)
                    {
                        worldChaos += clan1.IsAtWarWith(clan2) ? warImpact : 0f;
                    }
                }

                return worldChaos;
            }
        }

        public static ISettingsProvider Settings { get; private set; }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (GlobalSettings<Settings>.Instance is null)
            {
                // Use default settings
                Settings = new HardcodedSettings();
            }
            else
            {
                // Use Mod Configuration Manager
                Settings = GlobalSettings<Settings>.Instance;
            }

            InformationManager.DisplayMessage(new InformationMessage("<b>Iron Bank</b> has loaded.", Colors.Magenta));
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (game.GameType is TaleWorlds.CampaignSystem.Campaign && gameStarter is CampaignGameStarter campaignStarter)
            {
                var bankBehavior = new BankBehavior();
                var bankClanFinanceModel = new BankClanFinanceModel();

                campaignStarter.AddBehavior(bankBehavior);
                campaignStarter.AddModel(bankClanFinanceModel);
            }
        }
    }
}
