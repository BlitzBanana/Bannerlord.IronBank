using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace IronBank
{
    /// <summary>
    /// Declare a Custom Saver to be able to save BankAccount properties into the savegame.
    /// </summary>
    public class Save : SaveableTypeDefiner
    {
        public Save() : base(BankAccount.SAVE_ID) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(BankAccount), 1);
            AddClassDefinition(typeof(BankLoan), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<BankLoan>));
        }
    }
}
