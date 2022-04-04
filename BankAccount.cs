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
    public class BankAccount
    {
        /// <summary>
        /// Deposit taxes.
        /// </summary>
        public static float TaxInRate
        {
            get
            {
                return Math.Min(0.010f * Mod.Settings.TaxInScale, 1f);
            }
        }

        /// <summary>
        /// Withdraw taxes.
        /// </summary>
        public static float TaxOutRate
        {
            get
            {
                return Math.Min(0.015f * Mod.Settings.TaxOutScale, 1f);
            }
        }

        /// <summary>
        /// Current interests rate.
        /// </summary>
        public static float InterestsRate
        {
            get
            {
                float chaos = Mod.WorldChaos;
                float rate = (float)(Math.Pow(chaos, 2) * 0.15 + 0.005);
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
                float chaos = Mod.WorldChaos;
                float rate = (float)(Math.Pow(chaos, 2) * 0.18 + 0.010);
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
        /// Bank account Hero.
        /// </summary>
        public Hero Hero
        {
            get
            {
                return Hero.MainHero;
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

        public BankAccount(long gold = 0, float reinvestmentRatio = 0.2f)
        {
            this.Gold = gold;
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
        public (int purse, int account) EstimateInterests()
        {
            int amount = (int)Math.Floor(this.Gold * BankAccount.InterestsRate);
            int purse = (int)Math.Floor((1f - this.ReinvestmentRatio) * amount * (1f - BankAccount.TaxOutRate));
            int account = (int)Math.Ceiling(this.ReinvestmentRatio * amount);

            return (purse, account);
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
            var (purse, account) = this.EstimateInterests();

            this.Gold += account;

            int payments = 0;
            foreach (BankLoan loan in this.Loans)
            {
                var (_payment, _remaining) = loan.CalculatePayment();
                this.Gold += _payment;
                loan.Remaining = _remaining;
                payments += _payment;
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
