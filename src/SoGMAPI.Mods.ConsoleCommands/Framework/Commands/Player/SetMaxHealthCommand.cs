﻿using System.Linq;
using SoG;
using SoGModdingAPI.Framework;

namespace SoGModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player
{
    /// <summary>A command which edits the player's maximum health.</summary>
    internal class SetMaxHealthCommand : ConsoleCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetMaxHealthCommand()
            : base("player_setmaxhealth", "Sets the player's max health.\n\nUsage: player_setmaxhealth [value]\n- value: an integer amount.") { }

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
                monitor.Log($"You currently have {stats.iMaxHP} max health. Specify a value to change it.", LogLevel.Info);
                return;
            }

            // handle
            if (args.TryGetInt(0, "amount", out int amount, min: 1))
            {
                stats.iBaseMaxHP = amount;
                monitor.Log($"OK, you now have {stats.iMaxHP} max health.", LogLevel.Info);
            }
        }
    }
}
