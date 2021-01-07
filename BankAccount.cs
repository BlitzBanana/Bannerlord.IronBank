using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace IronBank
{
    /// <summary>
    /// Bank account of the Hero.
    /// </summary>
    [SaveableClass(BankAccount.SAVE_ID)]
    public class BankAccount
    {
        /// <summary>
        /// Random savegame identifier (type any large number to not be in conflict with other mods) (yeah strange system IMHO).
        /// </summary>
        public const int SAVE_ID = 2_431_637;
        
        /// <summary>
        /// Deposit taxes.
        /// </summary>
        public static float TaxInRate
        {
            get
            {
                return 0.010f * Mod.Settings.TaxInScale;
            }
        }

        /// <summary>
        /// Withdraw taxes.
        /// </summary>
        public static float TaxOutRate
        {
            get
            {
                return 0.015f * Mod.Settings.TaxOutScale;
            }
        }

        /// <summary>
        /// Current interests rate.
        /// </summary>
        public static float InterestsRate
        {
            get
            {
                float rate = Mod.WorldChaos * Mod.WorldChaos * 0.005f + Mod.WorldChaos * 0.086f - 0.015f;
                return rate * Mod.Settings.InterestsScale;
            }
        }

        /// <summary>
        /// Current loan interests rate.
        /// </summary>
        public static float LoanInterestsRate
        {
            get
            {
                float rate = Mod.WorldChaos * Mod.WorldChaos * 0.005f + Mod.WorldChaos * 0.086f + 0.005f;
                return Math.Max(0.004f, rate * Mod.Settings.LoanInterestsScale);
            }
        }

        /// <summary>
        /// Renown lose when the account is overdrafted.
        /// </summary>
        public static float RenownPenalty
        {
            get
            {
                return -10 * Mod.Settings.DailyOverdraftRenownLoseScale;
            }
        }

        /// <summary>
        /// Bank account gold amount.
        /// </summary>
        [SaveableProperty(1)]
        public long Gold { get; set; }

        /// <summary>
        /// Bank account Hero identity.
        /// </summary>
        [SaveableProperty(2)]
        public string HeroId { get; set; }

        /// <summary>
        /// Bank account Hero.
        /// </summary>
        public Hero Hero
        {
            get
            {
                return Hero.FindFirst((Hero hero) => hero.StringId == this.HeroId);
            }
        }

        /// <summary>
        /// Bank account loans.
        /// </summary>
        [SaveableProperty(3)]
        public List<BankLoan> Loans { get; set; } = new List<BankLoan>();

        /// <summary>
        /// Bank account Hero identity.
        /// </summary>
        [SaveableProperty(4)]
        public float ReinvestmentRatio { get; set; }

        /// <summary>
        /// Bank golds from taxes and loans.
        /// </summary>
        [SaveableProperty(5)]
        public long BankGold { get; set; }

        public BankAccount(string heroId, long gold = 0, float reinvestmentRatio = 0.2f)
        {
            this.HeroId = heroId;
            this.Gold = gold;
            this.BankGold = 0;
            this.ReinvestmentRatio = reinvestmentRatio;
        }

        /// <summary>
        /// Can Hero deposit gold in his bank account ?
        /// <para>Checks if the amount is positive and in the Hero gold capacity.</para>
        /// </summary>
        /// <param name="amount">Amount of gold that the Hero wants to deposit.</param>
        /// <returns>Is this operation possible ?</returns>
        public bool CanDeposit(int amount)
        {
            return amount > 0 && amount <= this.Hero.Gold;
        }

        /// <summary>
        /// Can Hero withdraw gold from his bank account ?
        /// <para>Checks if the amount is positive and in the account gold capacity and does not causes an integer overflow to the Hero purse.</para>
        /// </summary>
        /// <param name="amount">Amount of gold that the Hero wants to withdraw.</param>
        /// <returns>Is this operation possible ?</returns>
        public bool CanWithdraw(int amount)
        {
            return amount > 0 && amount <= this.Gold && (int.MaxValue - amount - this.Hero.Gold) >= 0;
        }

        /// <summary>
        /// Make the Hero deposit gold in his bank account.
        /// <para>Removes the gold amount from the Hero purse and adds it on his bank account minus a bank tax.</para>
        /// </summary>
        /// <param name="amount">Amount of gold that the Hero wants to deposit.</param>
        /// <returns>
        /// Bank account gold amount,
        /// Hero gold amount
        /// </returns>
        public (long account, int purse) Deposit(int amount)
        {
            if (!this.CanDeposit(amount)) return (this.Gold, this.Hero.Gold);

            int tax = (int)Math.Ceiling(amount * BankAccount.TaxInRate);
            this.Hero.ChangeHeroGold(amount * -1);
            this.Gold += amount - tax;
            this.BankGold += tax;

            if (tax > 0)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"Iron Bank: You paid a deposit tax of {tax}<img src=\"Icons\\Coin@2x\">.")
                );
            }

            return (this.Gold, this.Hero.Gold);
        }

        /// <summary>
        /// Make the Hero withdraw gold from his bank account.
        /// <para>Removes the gold amount from the Hero bank account and adds it on his purse minus a bank tax.</para>
        /// </summary>
        /// <param name="amount">Amount of gold that the Hero wants to withdraw.</param>
        /// <returns>
        /// Bank account gold amount,
        /// Hero gold amount
        /// </returns>
        public (long account, int purse) Withdraw(int amount)
        {
            if (!this.CanWithdraw(amount)) return (this.Gold, this.Hero.Gold);

            int tax = (int)Math.Ceiling(amount * BankAccount.TaxOutRate);
            this.Gold -= amount;
            this.Hero.ChangeHeroGold(amount - tax);
            this.BankGold += tax;

            if (tax > 0)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"Iron Bank: You paid a withdraw tax of {tax}<img src=\"Icons\\Coin@2x\">.")
                );
            }

            return (this.Gold, this.Hero.Gold);
        }

        /// <summary>
        /// Compute due interests and the repartition.
        /// </summary>
        /// <returns>
        /// Amount of interests to deposit into Hero purse,
        /// Amount of interests to deposit into bank account
        /// </returns>
        public (int purse, int account, int bank) EstimateInterests()
        {
            int amount = (int)Math.Floor(this.Gold * BankAccount.InterestsRate);
            int purse = (int)Math.Floor((1f - this.ReinvestmentRatio) * amount * 0.98);
            int account = (int)Math.Ceiling(this.ReinvestmentRatio * amount * 0.95);
            int bank = (int)Math.Ceiling(this.ReinvestmentRatio * amount * 0.05);

            return (purse, account, bank);
        }

        /// <summary>
        /// Time to distribute interests.
        /// </summary>
        /// <returns>
        /// Amount of interests to add to Hero purse,
        /// Amount of interests to add to bank account,
        /// Amount of payments due to remove from bank account
        /// </returns>
        public (int purse, int account, int payments) ApplyDailyTransactions()
        {
            var (purse, account, bank) = this.EstimateInterests();

            this.Hero.ChangeHeroGold(purse);
            this.Gold += account;
            this.BankGold += bank; // Bank takes management fees

            int payments = 0;
            foreach (BankLoan loan in this.Loans)
            {
                var (_payment, _remaining) = loan.CalculatePayment();
                this.Gold += _payment;
                loan.Remaining = _remaining;
                payments += _payment;

                if (loan.Remaining == 0)
                {
                    this.BankGold += loan.Cost;
                }
            }

            // Hero account is overdrawn, let's make him lose some renown
            if (this.Gold < 0 && Mod.Settings.DailyOverdraftRenownLoseScale > 0f)
            {
                this.Hero.Clan.AddRenown(BankAccount.RenownPenalty);
                InformationManager.DisplayMessage(
                    new InformationMessage($"Iron Bank: You owes us <b>{this.Gold}</b><img src=\"Icons\\Coin@2x\"> from your loans, be careful.", Colors.Magenta)
                );
                InformationManager.DisplayMessage(
                    new InformationMessage($"Iron Bank: {BankAccount.RenownPenalty:+#;-#;0} renown penalty was applyed to your clan.", Colors.Magenta)
                );
            }

            // Remove fully repaid loans
            this.Loans = this.Loans.Where((loan) => loan.Remaining > 0).ToList();

            return (purse, account, payments);
        }
    }
}
