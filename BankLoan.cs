using System;
using System.Linq;
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
        /// Loan date.
        /// </summary>
        [SaveableProperty(4)]
        public float Date { get; set; }

        /// <summary>
        /// Loan duration in days.
        /// </summary>
        [SaveableProperty(5)]
        public int Duration { get; set; }

        /// <summary>
        /// Loan payment delay in days.
        /// </summary>
        [SaveableProperty(6)]
        public int Delay { get; set; }

        /// <summary>
        /// Start date of payments in days.
        /// </summary>
        public float PaymentsStartDate
        {
            get
            {
                return this.Date + this.Delay;
            }
        }

        /// <summary>
        /// Estimated end date of payments in days.
        /// </summary>
        public float PaymentsEndDate
        {
            get
            {
                return this.Date + this.Delay + this.Duration;
            }
        }

        /// <summary>
        /// Amount to repay daily.
        /// </summary>
        [SaveableProperty(7)]
        public int Payments { get; set; }

        /// <summary>
        /// Bank account Hero identity.
        /// </summary>
        [SaveableProperty(8)]
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

        public BankLoan(BankLoanSimulation simulation)
        {
            this.HeroId = simulation.HeroId;
            this.Amount = simulation.Amount;
            this.Cost = simulation.Cost;
            this.Payments = simulation.Payments;
            this.Remaining = simulation.Total;
            this.Date = (float)CampaignTime.Now.ToDays;
            this.Duration = simulation.Duration;
            this.Delay = simulation.Delay;

            this.Hero.ChangeHeroGold(simulation.Amount);
        }

        /// <summary>
        /// Current payment value and remaining loan value.
        /// </summary>
        /// <returns>
        /// Payment amount,
        /// Remaining loan value
        /// </returns>
        public (int payment, int remaining) CalculatePayment()
        {
            if (CampaignTime.Now.ToDays < this.PaymentsStartDate) return (0, this.Remaining);
            int value = Math.Max(this.Payments, this.Remaining * -1);
            return (value, this.Remaining + value);
        }
    }

    public struct BankLoanCapacity
    {
        public int MinDelay { get; }
        public int MaxDelay { get; }
        public int MinDuration { get; }
        public int MaxDuration { get; }
        public int MinAmount { get; }
        public int MaxAmount { get; }

        public BankLoanCapacity(Hero hero)
        {
            float renown = hero.Clan.Renown;
            int currentLoansCount= BankBehavior.BankAccount.Loans.Count;
            int currentLoansAmount = BankBehavior.BankAccount.Loans.Sum(loan => loan.Amount);
            int absoluteMaxAmount = Math.Max(1, (int)Math.Floor(renown * renown * 0.04f + renown * 50f));

            // The max amount is directly proportional of Hero.renown
            this.MinAmount = 1;
            this.MaxAmount = currentLoansCount < 4 ? absoluteMaxAmount - currentLoansAmount : 0;

            // The delay is clamped to (5, 31)
            this.MinDelay = 1;
            this.MaxDelay = (int)Math.Max(5, renown / 800f * (float)CampaignTime.DaysInSeason);

            // The duration is clamped to (10, 62)
            this.MinDuration = 1;
            this.MaxDuration = (int)Math.Max(10, renown / 250f * (float)CampaignTime.DaysInSeason);
        }
    }

    public struct BankLoanSimulation {
        /// <summary>
        /// Compute current world chaos, based on the wars count.
        /// More wars means higher loan costs but higher account interests !
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

        public string HeroId { get; private set; }
        public int Amount { get; private set; }
        public int Cost { get; private set; }
        public int Delay { get; private set; }
        public int Duration { get; private set; }
        public int Payments { get; private set; }
        public int Total
        {
            get
            {
                return this.Amount + this.Cost;
            }
        }

        public BankLoanSimulation(Hero hero, int amount, int delay = 15, int duration = 31)
        {
            int totalDuration = delay + duration;
            float rate = BankLoanSimulation.WorldChaos / 50f;
            int cost = (int)Math.Ceiling(amount * rate * totalDuration);
            int total = amount + cost;
            int payments = total / duration * -1;

            this.HeroId = hero.StringId;
            this.Amount = amount;
            this.Cost = cost;
            this.Delay = delay;
            this.Duration = duration;
            this.Payments = payments;
        }
    }
}
