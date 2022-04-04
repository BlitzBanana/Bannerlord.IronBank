using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;

namespace IronBank
{
    /// <summary>
    /// Allows us to add an explanation to the Hero daily gold change tooltip.
    /// </summary>
    public class BankClanFinanceModel : DefaultClanFinanceModel
    {
        public override ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false)
        {
            ExplainedNumber gold = base.CalculateClanGoldChange(clan, includeDescriptions, applyWithdrawals);

            if (clan.Leader.StringId == Hero.MainHero.StringId)
            {
                var (purse, _) = BankBehavior.BankAccount.EstimateInterests();

                if (purse > 0)
                {
                    gold.Add(purse, new TaleWorlds.Localization.TextObject("Iron Bank - interests"));
                    return gold;
                }
            }

            return gold;
        }
    }
}
