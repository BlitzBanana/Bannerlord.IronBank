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
        internal ISettingsProvider _settings;

        /// <summary>
        /// Random savegame identifier (type any large number to not be in conflict with other mods) (yeah strange system IMHO).
        /// </summary>
        public const int SAVE_ID = 2_431_637;

        /// <summary>
        /// Bank account gold amount.
        /// </summary>
        [SaveableProperty(1)]
        public long Gold { get; set; } = 0; // Golds

        /// <summary>
        /// Bank account tax rate for incomming gold transferts.
        /// </summary>
        [SaveableProperty(2)]
        public float TaxIn { get; set; } = 0f; // Percent

        /// <summary>
        /// Bank account tax rate for outgoing gold transferts.
        /// </summary>
        [SaveableProperty(3)]
        public float TaxOut { get; set; } = 0f; // Percent

        /// <summary>
        /// Daily Bank account interest rate.
        /// </summary>
        [SaveableProperty(4)]
        public float InterestRate { get; set; } = 0f; // Percent

        /// <summary>
        /// Daily Bank account interest rate.
        /// </summary>
        [SaveableProperty(5)]
        public float ReinvestmentRate { get; set; } = 0f; // Percent

        public int ID { get { return this.GetHashCode(); } }

        public BankAccount()
        {
            this.Init();
        }

        internal void Init()
        {
            if (GlobalSettings<Settings>.Instance is null)
            {
                // Use default settings
                this._settings = new HardcodedSettings();
            }
            else
            {
                // Use Mod Configuration Manager
                this._settings = GlobalSettings<Settings>.Instance;
            }

            this._settings.PropertyChanged += SettingsChanged;
            this.LoadSettings();
        }

        internal void LoadSettings()
        {
            this.InterestRate = this._settings.InterestRates;
            this.TaxIn = this._settings.TaxIn;
            this.TaxOut = this._settings.TaxOut;
            this.ReinvestmentRate = this._settings.ReinvestmentRate;
        }


        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SAVE_TRIGGERED")
            {
                this.LoadSettings();
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
            return amount > 0 && amount <= Hero.MainHero.Gold;
        }

        /// <summary>
        /// Can Hero withdraw gold from his bank account ?
        /// <para>Checks if the amount is positive and in the Hero gold capacity and does not causes an integer overflow to the Hero purse.</para>
        /// </summary>
        /// <param name="amount">Amount of gold that the Hero wants to withdraw.</param>
        /// <returns>Is this operation possible ?</returns>
        public bool CanWithdraw(int amount)
        {
            return amount > 0 && amount <= this.Gold && (int.MaxValue - amount - this.Gold) >= 0;
        }

        /// <summary>
        /// Make the Hero deposit gold in his bank account.
        /// <para>Removes the gold amount from the Hero purse and adds it on his bank account minus a bank tax.</para>
        /// </summary>
        /// <param name="amount">Amount of gold that the Hero wants to deposit.</param>
        /// <returns>Bank account gold amount, Hero gold amount</returns>
        public (long, int) Deposit(int amount)
        {
            if (!this.CanDeposit(amount)) return (this.Gold, Hero.MainHero.Gold);

            int tax = (int)Math.Ceiling(this.TaxIn * amount);
            Hero.MainHero.ChangeHeroGold(amount * -1);
            this.Gold += amount - tax;

            return (this.Gold, Hero.MainHero.Gold);
        }

        /// <summary>
        /// Make the Hero withdraw gold from his bank account.
        /// <para>Removes the gold amount from the Hero bank account and adds it on his purse minus a bank tax.</para>
        /// </summary>
        /// <param name="amount">Amount of gold that the Hero wants to withdraw.</param>
        /// <returns>Bank account gold amount, Hero gold amount</returns>
        public (long, int) Withdraw(int amount)
        {
            if (!this.CanWithdraw(amount)) return (this.Gold, Hero.MainHero.Gold);

            int tax = (int)Math.Ceiling(this.TaxOut * amount);
            this.Gold -= amount;
            Hero.MainHero.ChangeHeroGold((amount - tax) * 1);

            return (this.Gold, Hero.MainHero.Gold);
        }

        /// <summary>
        /// A season has passed, time to distribute interests.
        /// </summary>
        /// <returns>Amount of interests to deposit into Heor purse, Amount of interests to deposit into bank account</returns>
        public (int, int) CalculateInterests()
        {
            int amount = (int)Math.Floor(this.Gold * this.InterestRate);
            int purse = (int)Math.Floor((1f - this.ReinvestmentRate) * amount);
            int account = (int)Math.Ceiling(this.ReinvestmentRate  * amount);
            return (purse, account);
        }
    }
}
