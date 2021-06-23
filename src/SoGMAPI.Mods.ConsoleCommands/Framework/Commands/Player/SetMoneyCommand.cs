using System.Linq;
using SoG;
using SoGModdingAPI.Framework;

namespace SoGModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player
{
    /// <summary>A command which edits the player's current money.</summary>
    internal class SetMoneyCommand : ConsoleCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetMoneyCommand()
            : base("player_setmoney", "Sets the player's money.\n\nUsage: player_setmoney <value>\n- value: an integer amount.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            Inventory inventory = SGame.Instance.xLocalPlayer.xInventory;

            // validate
            if (!args.Any())
            {
                monitor.Log($"You currently have {inventory.GetMoney()} gold. Specify a value to change it.", LogLevel.Info);
                return;
            }

            // handle
            string amountStr = args[0];
            if (int.TryParse(amountStr, out int amount))
            {
                inventory.SetMoney(amount);
                monitor.Log($"OK, you now have {inventory.GetMoney()} gold.", LogLevel.Info);
            }
            else
                this.LogArgumentNotInt(monitor);
        }
    }
}
