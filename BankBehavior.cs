using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace IronBank
{
    /// <summary>
    /// Manage lifecycle hooks events and game menus.
    /// </summary>
    public class BankBehavior : CampaignBehaviorBase
    {
        private const string TOWN_MENU_ID = "town";
        private const string BANK_MENU_ID = "bank";
        private const string BANK_MENU_DEPOSIT_ID = "bank_deposit";
        private const string BANK_MENU_WITHDRAW_ID = "bank_withdraw";
        private const string BANK_MENU_TAKE_LOAN_ID = "bank_take_loan";
        private const string BANK_MENU_SEE_LOANS_ID = "bank_see_loans";
        private const string BANK_MENU_REINVESTMENT_ID = "bank_reinvestment";
        private const string BANK_MENU_LEAVE_ID = "bank_leave";
        private const int BANK_MENU_INDEX = 4;
        private const int BANK_MENU_DEPOSIT_INDEX = 1;
        private const int BANK_MENU_WITHDRAW_INDEX = 2;
        private const int BANK_MENU_TAKE_LOAN_INDEX = 3;
        private const int BANK_MENU_SEE_LOANs_INDEX = 4;
        private const int BANK_MENU_REINVESTMENT_INDEX = 5;
        private const int BANK_MENU_LEAVE_INDEX = 6;

        private static BankAccount _bank_account = null;

        public static BankAccount BankAccount
        {
            get
            {
                if (_bank_account == null)
                {
                    _bank_account = new BankAccount(Hero.MainHero.StringId);
                }
                return _bank_account;
            }
        }

        /// <summary>
        /// Saves or Load data from a savegame.
        /// </summary>
        /// <param name="dataStore"></param>
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("IronBank.BankAccount", ref _bank_account);
        }

        public override void RegisterEvents()
        {
            // Daily hook to process payments
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>((Clan clan) =>
            {
                if (clan.StringId != Hero.MainHero.Clan.StringId) return;

                var (purse, account, payment) = BankAccount.ApplyDailyTransactions();

                if (purse > 0)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage($"Iron Bank: <b>{purse:+#;-#;0}</b><img src=\"Icons\\Coin@2x\"> from interests.")
                    );
                }

                if (account > 0)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage($"Iron Bank: <b>{account:+#;-#;0}</b><img src=\"Icons\\Coin@2x\"> to your account from interests.")
                    );
                }

                if (payment < 0)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage($"Iron Bank: <b>{payment:+#;-#;0}</b><img src=\"Icons\\Coin@2x\"> to your account from your current loans.")
                    );
                }
            }));

            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>((CampaignGameStarter campaignGameStarter) =>
            {
                // Bank option in town menu
                campaignGameStarter.AddGameMenuOption(
                    menuId: TOWN_MENU_ID,
                    optionId: BANK_MENU_ID,
                    optionText: "{=town_bank}Go to the bank",
                    index: BANK_MENU_INDEX,
                    isLeave: false,
                    isRepeatable: false,
                    condition: delegate (MenuCallbackArgs args)
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return true;
                    },
                    consequence: delegate (MenuCallbackArgs args)
                    {
                        args.MenuContext.SwitchToMenu(BANK_MENU_ID);
                        return;
                    }
                );

                // Inside the bank menu
                campaignGameStarter.AddGameMenu(
                    menuId: BANK_MENU_ID,
                    menuText: "{=bank_account}{IronBank_Menu_Bank}",
                    menuFlags: GameMenu.MenuFlags.none,
                    overlay: TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.None,
                    initDelegate: delegate (MenuCallbackArgs args)
                    {
                        this.UpdateBankMenuText();
                    }
                );

                // Deposit option
                campaignGameStarter.AddGameMenuOption(
                    menuId: BANK_MENU_ID,
                    optionId: BANK_MENU_DEPOSIT_ID,
                    optionText: "{=bank_account_deposit}Make a deposit",
                    index: BANK_MENU_DEPOSIT_INDEX,
                    isLeave: false,
                    isRepeatable: false,
                    condition: delegate (MenuCallbackArgs args)
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        args.IsEnabled = Hero.MainHero.Gold >= 0;
                        return true;
                    },
                    consequence: delegate (MenuCallbackArgs args)
                    {
                        this.AskDeposit();
                    }
                );

                // Withdraw option
                campaignGameStarter.AddGameMenuOption(
                    menuId: BANK_MENU_ID,
                    optionId: BANK_MENU_WITHDRAW_ID,
                    optionText: "{=bank_account_withdraw}Make a withdraw",
                    index: BANK_MENU_WITHDRAW_INDEX,
                    isLeave: false,
                    isRepeatable: false,
                    condition: delegate (MenuCallbackArgs args)
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        args.IsEnabled = Hero.MainHero.Gold >= 0;
                        return true;
                    },
                    consequence: delegate (MenuCallbackArgs args)
                    {
                        this.AskWithdraw();
                    }
                );

                // Loan option
                campaignGameStarter.AddGameMenuOption(
                    menuId: BANK_MENU_ID,
                    optionId: BANK_MENU_TAKE_LOAN_ID,
                    optionText: "{=bank_account_take_loan}Take a loan",
                    index: BANK_MENU_TAKE_LOAN_INDEX,
                    isLeave: false,
                    isRepeatable: false,
                    condition: delegate (MenuCallbackArgs args)
                    {
                        var capacity = new BankLoanCapacity(Hero.MainHero);
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        args.IsEnabled = Hero.MainHero.Gold >= 0 && capacity.MinAmount > 0;
                        return true;
                    },
                    consequence: delegate (MenuCallbackArgs args)
                    {
                        var capacity = new BankLoanCapacity(Hero.MainHero);
                        this.AskLoanAmount(capacity);
                    }
                );

                // Current loans
                campaignGameStarter.AddGameMenuOption(
                    menuId: BANK_MENU_ID,
                    optionId: BANK_MENU_SEE_LOANS_ID,
                    optionText: "{=bank_account_see_loans}See my loans",
                    index: BANK_MENU_SEE_LOANs_INDEX,
                    isLeave: false,
                    isRepeatable: false,
                    condition: delegate (MenuCallbackArgs args)
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        args.IsEnabled = BankAccount.Loans.Count > 0;
                        return true;
                    },
                    consequence: delegate (MenuCallbackArgs args)
                    {
                        this.ShowLoans();
                    }
                );

                // Reinvestment ratio setting
                campaignGameStarter.AddGameMenuOption(
                    menuId: BANK_MENU_ID,
                    optionId: BANK_MENU_REINVESTMENT_ID,
                    optionText: "{=bank_account_reinvestment}Set my reinvestment ratio",
                    index: BANK_MENU_REINVESTMENT_INDEX,
                    isLeave: false,
                    isRepeatable: false,
                    condition: delegate (MenuCallbackArgs args)
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        args.IsEnabled = Hero.MainHero.Gold >= 0;
                        return true;
                    },
                    consequence: delegate (MenuCallbackArgs args)
                    {
                        this.AskReinvestmentRatio();
                    }
                );

                // Leave option
                campaignGameStarter.AddGameMenuOption(
                    menuId: BANK_MENU_ID,
                    optionId: BANK_MENU_LEAVE_ID,
                    optionText: "{=bank_account_leave}Return to the city",
                    index: BANK_MENU_LEAVE_INDEX,
                    isLeave: true,
                    isRepeatable: false,
                    condition: delegate (MenuCallbackArgs args)
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                        return true;
                    },
                    consequence: delegate (MenuCallbackArgs args)
                    {
                        args.MenuContext.SwitchToMenu(TOWN_MENU_ID);
                        return;
                    }
                );
            }));
        }

        /// <summary>
        /// Updates the bank menu headline text variable with current values.
        /// </summary>
        private void UpdateBankMenuText()
        {
            MBTextManager.SetTextVariable(
                "IronBank_Menu_Bank",
                $"\"Welcome to the <b>Iron Bank</b> embassy of {Settlement.CurrentSettlement?.EncyclopediaLinkWithName}\", says the emissary.\n" +
                $"Your bank account balance is <b>{BankAccount.Gold}</b>{{GOLD_ICON}}.\n" +
                $"Daily interest rate is {BankAccount.InterestsRate:P} ({BankAccount.ReinvestmentRatio * 100:0}% will stay on your account).",
                false
            );
        }

        /// <summary>
        /// Show an input popup to ask for a deposit amount and process it.
        /// </summary>
        private void AskDeposit()
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: $"How much do you want to deposit ?",
                text: $"Your current balance is <b>{BankAccount.Gold}</b>.\n" +
                    $"A tax of {BankAccount.TaxInRate:P} is applyed to every deposit.",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Deposit",
                negativeText: "Back",
                textCondition: new Func<string, bool>((string input) =>
                {
                    return int.TryParse(input, out int amount) && BankAccount.CanDeposit(amount);
                }),
                affirmativeAction: new Action<string>((string input) =>
                {
                    if (int.TryParse(input, out int amount) && BankAccount.CanDeposit(amount))
                    {
                        BankAccount.Deposit(amount);
                        this.UpdateBankMenuText();
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Your bank transfer encountered a problem and could not be processed."));
                    }
                }),
                negativeAction: new Action(() => { })
            ), true);
        }

        /// <summary>
        /// Show an input popup to ask for a withdraw amount and process it.
        /// </summary>
        private void AskWithdraw()
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: $"How much do you want to withdraw ?",
                text: $"Your current balance is <b>{BankAccount.Gold}</b>.\n" +
                    $"A tax of {BankAccount.TaxOutRate:P} is applyed to every withdraw.",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Withdraw",
                negativeText: "Back",
                textCondition: new Func<string, bool>((string input) =>
                {
                    return int.TryParse(input, out int amount) && BankAccount.CanWithdraw(amount);
                }),
                affirmativeAction: new Action<string>((string input) =>
                {
                    if (int.TryParse(input, out int amount) && BankAccount.CanWithdraw(amount))
                    {
                        BankAccount.Withdraw(amount);
                        this.UpdateBankMenuText();
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Your bank transfer encountered a problem and could not be processed."));
                    }
                }),
                negativeAction: new Action(() => { })
            ), true);
        }

        /// <summary>
        /// Show an text popup listing Hero current loans.
        /// </summary>
        private void ShowLoans()
        {
            string text = "";

            foreach (var loan in BankAccount.Loans)
            {
                text += $"~ Loan took on {CampaignTime.Days(loan.Date)} for <b>{loan.Amount}</b><img src=\"Icons\\Coin@2x\">\n";
                text += (loan.Remaining == loan.Total)
                    ? $"Daily payments will start on {CampaignTime.Days(loan.PaymentsStartDate)}\n"
                    : $"Daily payments started on {CampaignTime.Days(loan.PaymentsStartDate)}\n";
                text += $"Daily payments will end on {CampaignTime.Days(loan.PaymentsEndDate)}\n";
                text += $"Daily payments: <b>{loan.Payments * -1}</b><img src=\"Icons\\Coin@2x\">\n";
                text += $"Remaining to pay: <b>{loan.Remaining}</b><img src=\"Icons\\Coin@2x\">\n";
                text += $" \n";
                text += $" \n";
            }

            InformationManager.ShowInquiry(new InquiryData(
                titleText: $"Your current loans.",
                text: text,
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: false,
                affirmativeText: "Ok",
                negativeText: "Back",
                affirmativeAction: new Action(() => { }),
                negativeAction: new Action(() => { })
            ), true);
        }

        /// <summary>
        /// Show an input popup to ask for Hero reinvestment ratio and save it.
        /// </summary>
        private void AskReinvestmentRatio()
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: $"How much of interest will be keept on your account ?",
                text: $"Set a percentage (0-100)." +
                    $"- 0 all your interests will go to your purse." +
                    $"- 100 all your interests will go to your account.",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Set",
                negativeText: "Back",
                textCondition: new Func<string, bool>((string input) =>
                {
                    return int.TryParse(input, out int amount) && amount >= 0 && amount <= 100;
                }),
                affirmativeAction: new Action<string>((string input) =>
                {
                    if (int.TryParse(input, out int amount) && amount >= 0 && amount <= 100)
                    {
                        BankAccount.ReinvestmentRatio = amount / 100f;
                        this.UpdateBankMenuText();
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Iron Bank encountered a problem."));
                    }
                }),
                negativeAction: new Action(() => { })
            ), true);
        }

        /// <summary>
        /// Loan step 1 - Show an input popup to ask for the loan amount.
        /// </summary>
        private void AskLoanAmount(BankLoanCapacity capacity)
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: $"How much do you want, Sir?",
                text: $"Your current balance is <b>{BankAccount.Gold}</b><img src=\"Icons\\Coin@2x\">.\n" +
                    $"You maximum loan capacity is <b>{capacity.MaxAmount}</b><img src=\"Icons\\Coin@2x\">.",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Continue",
                negativeText: "Cancel",
                textCondition: new Func<string, bool>((string amountInput) =>
                {
                    return int.TryParse(amountInput, out int amount)
                        && capacity.MinAmount <= amount
                        && capacity.MaxAmount >= amount;
                }),
                affirmativeAction: new Action<string>((string amountInput) =>
                {
                    if (int.TryParse(amountInput, out int amount) && capacity.MinAmount <= amount && capacity.MaxAmount >= amount)
                    {
                        this.AskLoanDuration(capacity, amount);
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Your loan simulation has been aborted."));
                    }
                }),
                negativeAction: new Action(() => { })
            ), true);
        }

        /// <summary>
        /// Loan step 2 - Show an input popup to ask for the loan duration.
        /// </summary>
        private void AskLoanDuration(BankLoanCapacity capacity, int amount)
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: $"How long do you want to repay?",
                text: $"You can loan for {capacity.MaxDuration} days maximum.",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Continue",
                negativeText: "Back",
                textCondition: new Func<string, bool>((string durationInput) =>
                {
                    return int.TryParse(durationInput, out int duration)
                        && capacity.MinDuration <= duration
                        && capacity.MaxDuration >= duration;
                }),
                affirmativeAction: new Action<string>((string durationInput) =>
                {
                    if (int.TryParse(durationInput, out int duration) && capacity.MinDuration <= duration && capacity.MaxDuration >= duration)
                    {
                        this.AskLoanDelay(capacity, amount, duration);
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Your loan simulation has been aborted."));
                    }
                }),
                negativeAction: new Action(() =>
                {
                    this.AskLoanAmount(capacity);
                })
            ), true);
        }

        /// <summary>
        /// Loan step 3 - Show an input popup to ask for the loan delay.
        /// </summary>
        private void AskLoanDelay(BankLoanCapacity capacity, int amount, int duration)
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: $"How long would you like to wait before repaying?",
                text: $"You can push back payment for {capacity.MaxDelay} days maximum.",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Continue",
                negativeText: "Back",
                textCondition: new Func<string, bool>((string delayInput) =>
                {
                    return int.TryParse(delayInput, out int delay)
                        && capacity.MinDelay <= delay
                        && capacity.MaxDelay >= delay;
                }),
                affirmativeAction: new Action<string>((string delayInput) =>
                {
                    if (int.TryParse(delayInput, out int delay) && capacity.MinDelay <= delay && capacity.MaxDelay >= delay)
                    {
                        this.AskLoanConfirmation(capacity, amount, duration, delay);
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Your loan simulation has been aborted."));
                    }
                }),
                negativeAction: new Action(() =>
                {
                    this.AskLoanDuration(capacity, amount);
                })
            ), true);
        }

        /// <summary>
        /// Loan step 4 - Show a text popup to ask for the loan confirmation and process it.
        /// </summary>
        private void AskLoanConfirmation(BankLoanCapacity capacity, int amount, int duration, int delay)
        {
            var simulation = new BankLoanSimulation(Hero.MainHero, amount, delay, duration);
            InformationManager.ShowInquiry(new InquiryData(
                titleText: "Are you sure, their is no comming back with us ?",
                text: $"You will begin to pay <b>{simulation.Payments * -1}</b><img src=\"Icons\\Coin@2x\"> a day from your account for <b>{simulation.Duration} days</b> in <b>{simulation.Delay} days</b>. " +
                    $"Borrowing from us {amount}<img src=\"Icons\\Coin@2x\"> will cost you <b>{simulation.Cost}</b><img src=\"Icons\\Coin@2x\"> of bank interests for a total of <b>{simulation.Total}</b><img src=\"Icons\\Coin@2x\"> repaid.\n",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Agree",
                negativeText: "Refuse",
                affirmativeAction: new Action(() =>
                {
                    BankAccount.Loans.Add(new BankLoan(simulation));
                }),
                negativeAction: new Action(() =>
                {
                    this.AskLoanDelay(capacity, amount, duration);
                })
            ));
        }
    }
}
