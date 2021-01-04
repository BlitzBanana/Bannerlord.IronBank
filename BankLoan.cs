using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace IronBank
{
    /// <summary>
    /// Bank account of the Hero.
    /// </summary>
    [SaveableClass(BankLoan.SAVE_ID)]
    public class BankLoan
    {
        /// <summary>
        /// Random savegame identifier (type any large number to not be in conflict with other mods) (yeah strange system IMHO).
        /// </summary>
        public const int SAVE_ID = 2_433_637;

        /// <summary>
        /// Amount borrowed.
        /// </summary>
        [SaveableProperty(1)]
        public int Amount { get; set; }

        /// <summary>
        /// Amount remaining to pay.
        /// </summary>
        [SaveableProperty(2)]
        public int Remaining { get; set; }

        /// <summary>
        /// Bank loan gold amount.
        /// </summary>
        [SaveableProperty(3)]
        public int Cost { get; set; }

        /// <summary>
        /// Total amount to repay.
        /// </summary>
        public int Total { get { return this.Amount + this.Cost; } }

        /// <summary>
        /// Due payments every days.
        /// </summary>
        [SaveableProperty(4)]
        public int Repays { get; set; }

        /// <summary>
        /// Loan date.
        /// </summary>
        [SaveableProperty(5)]
        public float Date { get; set; }

        /// <summary>
        /// Loan duration in days.
        /// </summary>
        [SaveableProperty(5)]
        public int Duration { get; set; }

        /// <summary>
        /// Loan payment delay in days.
        /// </summary>
        [SaveableProperty(5)]
        public int Delay { get; set; }

        /// <summary>
        /// Bank account Hero identity.
        /// </summary>
        [SaveableProperty(6)]
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
        /// Compute current world chaos, based on the wars count.
        /// </summary>
        public static float WorldChaos
        {
            get
            {
                float worldChaos = 0f;
                float warImpact = 1f / (float)Math.Pow(Kingdom.All.Count, 2);

                foreach (Kingdom kingdom1 in Kingdom.All)
                {
                    foreach (Kingdom kingdom2 in Kingdom.All)
                    {
                        worldChaos += kingdom1.IsAtWarWith(kingdom2) ? warImpact : 0f;
                    }
                }

                return worldChaos;
            }
        }

        /// <summary>
        /// Calculate Hero loan capacity, based on its renown.
        /// </summary>
        /// <param name="hero">Hero</param>
        /// <returns>Hero maximum loan amount</returns>
        public static int LoanCapacity(Hero hero)
        {
            var renown = hero.Clan.Renown;
            return (int)Math.Floor(renown * renown * 0.04f + renown * 50f);
        }

        /// <summary>
        /// Calculate a loan cost, based on current world wars.
        /// </summary>
        /// <param name="amount">Amount of the loan to simulate</param>
        /// <returns>Loan cost</returns>
        public static int LoanCost(int amount)
        {
            return (int)Math.Ceiling(amount * (WorldChaos * 1.5f));
        }

        /// <summary>
        /// Calculate a loan payments, based on its value and duration.
        /// </summary>
        /// <param name="total">Total loan value (amount + cost)</param>
        /// <param name="duration">Loan duration in days</param>
        /// <returns>Daily payment value (negative value)</returns>
        public static int LoanPayments(int total, int duration = 31)
        {
            return (int)Math.Ceiling((float)total / (float)duration) * -1;
        }

        public BankLoan(string heroId, int amount, int cost, int delay = 15, int duration = 31)
        {
            this.HeroId = heroId;
            this.Amount = amount;
            this.Cost = cost;
            this.Remaining = amount + cost;
            this.Cost = cost;
            this.Date = (float)CampaignTime.Now.ToDays;
            this.Duration = duration;
            this.Delay = delay;

            this.Hero.ChangeHeroGold(amount);
        }

        /// <summary>
        /// Compute current payment value and remaining loan value.
        /// </summary>
        /// <returns>
        /// Payment amount,
        /// Remaining loan value
        /// </returns>
        public (int payment, int remaining) CalculatePayment()
        {
            if (CampaignTime.Days(this.Date).ElapsedDaysUntilNow < this.Delay) return (0, this.Remaining);

            int payment = LoanPayments(this.Remaining, this.Duration);
            int remaining = this.Remaining + payment;
            return (payment, remaining);
        }
    }
}
