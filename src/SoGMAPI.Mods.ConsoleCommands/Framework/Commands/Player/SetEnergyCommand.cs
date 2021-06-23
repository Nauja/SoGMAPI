using System.Linq;
using SoG;
using SoGModdingAPI.Framework;

namespace SoGModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player
{
    /// <summary>A command which edits the player's current energy.</summary>
    internal class SetEnergyCommand : ConsoleCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetEnergyCommand()
            : base("player_setenergy", "Sets the player's energy.\n\nUsage: player_setenergy [value]\n- value: an integer amount.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            BaseStats stats = SGame.Instance.xLocalPlayer.xEntity.xBaseStats;

            // validate
            if (!args.Any())
            {
                monitor.Log($"You currently have {stats.iEP} energy. Specify a value to change it.", LogLevel.Info);
                return;
            }

            // handle
            string amountStr = args[0];
            if (int.TryParse(amountStr, out int amount))
            {
                stats.iEP = amount;
                monitor.Log($"OK, you now have {stats.iEP} energy.", LogLevel.Info);
            }
            else
                this.LogArgumentNotInt(monitor);
        }
    }
}
