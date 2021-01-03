using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;

namespace IronBank
{
    public class Mod: MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            InformationManager.DisplayMessage(new InformationMessage("<b>Iron Bank</b> has loaded."));
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (game.GameType is Campaign && gameStarter is CampaignGameStarter campaignStarter)
            {
                var bankBehavior = new BankBehavior();
                var bankModel = new BankModel();

                campaignStarter.AddBehavior(bankBehavior);
                campaignStarter.AddModel(bankModel);
            }
        }
    }
}
