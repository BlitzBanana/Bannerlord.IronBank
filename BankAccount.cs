using MCM.Abstractions.Settings.Base.Global;
using System;
using TaleWorlds.CampaignSystem;
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

        public IBankSettingsProvider Settings { get; set; }

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

        public BankAccount(string heroId, long gold = 0)
        {
            this.HeroId = heroId;
            this.Gold = gold;
            this.Init();
        }

        /// <summary>
        /// Load settings from saved values or hardcoded ones.
        /// Must be called after a savegame loading in SyncData method of the Behavior.
        /// Does nothing if already initialized.
        /// </summary>
        internal void Init()
        {
            if (this.Settings != null) return;
            if (GlobalSettings<BankSettings>.Instance is null)
            {
                // Use default settings
                this.Settings = new HardcodedBankSettings();
            }
            else
            {
                // Use Mod Configuration Manager
                this.Settings = GlobalSettings<BankSettings>.Instance;
            }
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
        /// <returns>Bank account gold amount, Hero gold amount</returns>
        public (long, int) Deposit(int amount)
        {
            if (!this.CanDeposit(amount)) return (this.Gold, this.Hero.Gold);

            int tax = (int)Math.Ceiling(this.Settings.TaxIn * amount);
            this.Hero.ChangeHeroGold(amount * -1);
            this.Gold += amount - tax;

            return (this.Gold, this.Hero.Gold);
        }

        /// <summary>
        /// Make the Hero withdraw gold from his bank account.
        /// <para>Removes the gold amount from the Hero bank account and adds it on his purse minus a bank tax.</para>
        /// </summary>
        /// <param name="amount">Amount of gold that the Hero wants to withdraw.</param>
        /// <returns>Bank account gold amount, Hero gold amount</returns>
        public (long, int) Withdraw(int amount)
        {
            if (!this.CanWithdraw(amount)) return (this.Gold, this.Hero.Gold);

            int tax = (int)Math.Ceiling(this.Settings.TaxOut * amount);
            this.Gold -= amount;
            this.Hero.ChangeHeroGold((amount - tax) * 1);

            return (this.Gold, this.Hero.Gold);
        }

        /// <summary>
        /// Compute due interests and the repartition.
        /// </summary>
        /// <returns>Amount of interests to deposit into Hero purse, Amount of interests to deposit into bank account</returns>
        public (int, int) CalculateInterests()
        {
            int amount = (int)Math.Floor(this.Gold * this.Settings.InterestRate);
            int purse = (int)Math.Floor((1f - this.Settings.ReinvestmentRate) * amount);
            int account = (int)Math.Ceiling(this.Settings.ReinvestmentRate * amount);
            return (purse, account);
        }

        /// <summary>
        /// Time to distribute interests.
        /// </summary>
        /// <returns>Amount of interests to deposit into Hero purse, Amount of interests to deposit into bank account</returns>
        public (int, int) ApplyInterests()
        {
            var (purse, account) = this.CalculateInterests();

            this.Hero.ChangeHeroGold(purse);
            this.Gold += account;

            return (purse, account);
        }
    }
}
