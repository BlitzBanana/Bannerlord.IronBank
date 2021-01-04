using MCM.Abstractions.Settings.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace IronBank
{
    public class Mod: MBSubModuleBase
    {
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

            if (game.GameType is Campaign && gameStarter is CampaignGameStarter campaignStarter)
            {
                var bankBehavior = new BankBehavior();
                var bankClanFinanceModel = new BankClanFinanceModel();

                campaignStarter.AddBehavior(bankBehavior);
                campaignStarter.AddModel(bankClanFinanceModel);
            }
        }
    }
}
