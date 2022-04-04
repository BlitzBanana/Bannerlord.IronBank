using MCM.Abstractions.Settings.Base.Global;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

/// <summary>
/// Main documentation:
/// https://docs.bannerlordmodding.com/
/// </summary>

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
                var kingdoms = Kingdom.All;
                var kingdomsCount = kingdoms.Count();
                var warImpact = 1f / ((kingdomsCount * (kingdomsCount + 1f)) / 2f);
                var chaos = 0f;

                foreach (var kingdomA in kingdoms)
                {
                    foreach (var kingdomB in kingdoms)
                    {
                        if (kingdomA.Id != kingdomB.Id && kingdomA.IsAtWarWith(kingdomB))
                        {
                            chaos += warImpact;
                        }
                    }
                }

                return chaos;
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
