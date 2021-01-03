using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;

namespace IronBank
{
    /// <summary>
    /// Allows us to add an explanation to the Hero daily gold change tooltip.
    /// </summary>
    public class BankClanFinanceModel : DefaultClanFinanceModel
    {
        public override float CalculateClanGoldChange(Clan clan, StatExplainer explanation = null, bool applyWithdrawals = false)
        {
            float gold = base.CalculateClanGoldChange(clan, explanation, applyWithdrawals);

            if (clan.Leader.StringId == Hero.MainHero.StringId && explanation != null)
            {
                var (purse, _) = BankBehavior.BankAccount.CalculateInterests();

                if (purse > 0)
                {
                    explanation.AddLine("Iron Bank", purse, StatExplainer.OperationType.Add);
                    return gold + purse;
                }
            }

            return gold;
        }
    }
}
