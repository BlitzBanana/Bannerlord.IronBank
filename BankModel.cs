using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;

namespace IronBank
{
    public class BankModel : DefaultClanFinanceModel
    {
        public override float CalculateClanGoldChange(Clan clan, StatExplainer explanation = null, bool applyWithdrawals = false)
        {
            float gold = base.CalculateClanGoldChange(clan, explanation, applyWithdrawals);

            if (clan.StringId == "player_faction" && explanation != null)
            {
                var (purse, _) = BankBehavior.BankAccount.CalculateInterests();

                if (purse > 0)
                {
                    explanation.AddLine("Bank", purse, StatExplainer.OperationType.Add);
                    return gold + purse;
                }
            }

            return gold;
        }
    }
}
