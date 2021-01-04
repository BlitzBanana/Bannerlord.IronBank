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
        private const string BANK_MENU_LOAN_ID = "bank_loan";
        private const string BANK_MENU_LEAVE_ID = "bank_leave";
        private const int BANK_MENU_INDEX = 4;
        private const int BANK_MENU_DEPOSIT_INDEX = 1;
        private const int BANK_MENU_WITHDRAW_INDEX = 2;
        private const int BANK_MENU_LOAN_INDEX = 3;
        private const int BANK_MENU_LEAVE_INDEX = 4;

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
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(() =>
            {
                var (purse, account, payment) = BankAccount.ApplyDailyTransactions();

                if (purse > 0)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage($"Iron Bank: We sent you <b>{purse}</b><img src=\"Icons\\Coin@2x\"> from interests.")
                    );
                }

                if (account > 0)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage($"Iron Bank: We credited your account <b>{account}</b><img src=\"Icons\\Coin@2x\"> from interests.")
                    );
                }

                if (payment < 0)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage($"Iron Bank: We widthdrawed <b>{payment * -1}</b><img src=\"Icons\\Coin@2x\"> from your current loans.")
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
                        InformationManager.ShowTextInquiry(new TextInquiryData(
                            titleText: $"How much do you want to deposit ?",
                            text: $"Your current balance is <b>{BankAccount.Gold}</b>.\n" +
                                $"A tax of {Mod.Settings.TaxIn:P} is applyed to every deposit.",
                            isAffirmativeOptionShown: true,
                            isNegativeOptionShown: true,
                            affirmativeText: "Deposit",
                            negativeText: "Back",
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
                            negativeAction: new Action(() => { }),
                            textCondition: new Func<string, bool>((string input) =>
                            {
                                return int.TryParse(input, out int amount) && BankAccount.CanDeposit(amount);
                            })
                        ), true);
                        return;
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
                        InformationManager.ShowTextInquiry(new TextInquiryData(
                            titleText: $"How much do you want to withdraw ?",
                            text: $"Your current balance is <b>{BankAccount.Gold}</b>.\n" +
                                $"A tax of {Mod.Settings.TaxOut:P} is applyed to every withdraw.",
                            isAffirmativeOptionShown: true,
                            isNegativeOptionShown: true,
                            affirmativeText: "Withdraw",
                            negativeText: "Back",
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
                            negativeAction: new Action(() => { }),
                            textCondition: new Func<string, bool>((string input) =>
                            {
                                return int.TryParse(input, out int amount) && BankAccount.CanWithdraw(amount);
                            })
                        ), true);
                        return;
                    }
                );

                // Loan option
                campaignGameStarter.AddGameMenuOption(
                    menuId: BANK_MENU_ID,
                    optionId: BANK_MENU_LOAN_ID,
                    optionText: "{=bank_account_LOAN}Take a loan",
                    index: BANK_MENU_LOAN_INDEX,
                    isLeave: false,
                    isRepeatable: false,
                    condition: delegate (MenuCallbackArgs args)
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        args.IsEnabled = Hero.MainHero.Gold >= 0 && BankLoan.LoanCapacity(Hero.MainHero) > 0;
                        return true;
                    },
                    consequence: delegate (MenuCallbackArgs args)
                    {
                        var capacity = BankLoan.LoanCapacity(Hero.MainHero);
                        InformationManager.ShowTextInquiry(new TextInquiryData(
                            titleText: $"How much do you want ?",
                            text: $"Your current balance is <b>{BankAccount.Gold}</b><img src=\"Icons\\Coin@2x\">.\n" +
                                $"You maximum loan capacity is <b>{capacity}</b><img src=\"Icons\\Coin@2x\">.",
                            isAffirmativeOptionShown: true,
                            isNegativeOptionShown: true,
                            affirmativeText: "Loan",
                            negativeText: "Back",
                            affirmativeAction: new Action<string>((string input) =>
                            {
                                if (int.TryParse(input, out int amount) && capacity >= amount)
                                {
                                    int cost = BankLoan.LoanCost(amount);
                                    int payment = BankLoan.LoanPayments(amount + cost);
                                    InformationManager.ShowInquiry(new InquiryData(
                                        titleText: "Are you sure ?",
                                        text: $"Borrowing {amount}<img src=\"Icons\\Coin@2x\"> will cost you <b>{cost}</b><img src=\"Icons\\Coin@2x\"> of bank interests.\n" +
                                            $"You will begin to pay <b>{payment}</b><img src=\"Icons\\Coin@2x\"> a day for <b>31 days</b> in <b>15 days</b>.",
                                        isAffirmativeOptionShown: true,
                                        isNegativeOptionShown: false,
                                        affirmativeText: "Yes",
                                        negativeText: "No",
                                        affirmativeAction: new Action(() =>
                                        {
                                            var loan = new BankLoan(Hero.MainHero.StringId, amount, cost);
                                            BankAccount.Loans.Add(loan);
                                        }),
                                        negativeAction: new Action(() => { })
                                    ));
                                }
                                else
                                {
                                    InformationManager.DisplayMessage(new InformationMessage($"Your bank transfer encountered a problem and could not be processed."));
                                }
                            }),
                            negativeAction: new Action(() => { }),
                            textCondition: new Func<string, bool>((string input) =>
                            {
                                return int.TryParse(input, out int amount) && capacity >= amount;
                            })
                        ), true);
                        return;
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
                $"Daily interest rate is {Mod.Settings.InterestRate:P}.",
                false
            );
        }
    }
}
