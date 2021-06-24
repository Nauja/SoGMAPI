using SoG;
using SoGModdingAPI.Framework;

namespace SoGModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player
{
    /// <summary>A command which edits the player's name.</summary>
    internal class SetNameCommand : ConsoleCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetNameCommand()
            : base("player_setname", "Sets the player's name.\n\nUsage: player_setname <target> <name>\n- target: what to rename (one of 'player' or 'farm').\n- name: the new name to set.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            PlayerView player = SGame.Instance.xLocalPlayer;

            // parse arguments
            if (!args.TryGet(0, "target", out string target, oneOf: new[] { "player", "farm" }))
                return;
            args.TryGet(1, "name", out string name, required: false);

            // handle
            switch (target)
            {
                case "player":
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        player.sSaveableName = args[1];
                        monitor.Log($"OK, your name is now {player.sSaveableName}.", LogLevel.Info);
                    }
                    else
                        monitor.Log($"Your name is currently '{player.sSaveableName}'. Type 'help player_setname' for usage.", LogLevel.Info);
                    break;

            }
        }
    }
}
