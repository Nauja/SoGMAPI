using System.Linq;
using SoG;
using SoGModdingAPI.Framework;
 
namespace SoGModdingAPI.Mods.ConsoleCommands.Framework.Commands.World
{
    /// <summary>A command which edits the stats of entities.</summary>
    internal class StatCommand : ConsoleCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public StatCommand()
            : base("stat", "Modify the stats of an entity.\n\nUsage: stat [value]\n- value: an integer amount.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // parse arguments
            if (!args.TryGet(0, "target", out string target))
                return;

            WorldActor entity = SGame.Instance.xEntityMaster.FindEntityByTag(target) as WorldActor;
            if (entity == null)
            {
                monitor.Log($"Target {target} not found", LogLevel.Info);
                return;
            }

            BaseStats stats = entity.xBaseStats;

            if (!args.TryGet(1, "stat", out string stat))
                return;

            // handle
            switch (target)
            {
                case "hp":
                    {
                        if (!args.TryGetInt(2, "value", out int value, required: false))
                        {
                            monitor.Log($"Current hp is {stats.iHP}", LogLevel.Info);
                        }
                        else
                        {
                            stats.iHP = value;
                            monitor.Log($"Your hp is now {stats.iHP}", LogLevel.Info);
                        }
                    }
                    break;
                case "ep":
                    {
                        if (!args.TryGetInt(2, "value", out int value, required: false))
                        {
                            monitor.Log($"Current ep is {stats.iEP}", LogLevel.Info);
                        }
                        else
                        {
                            stats.iHP = value;
                            monitor.Log($"Your ep is now {stats.iEP}", LogLevel.Info);
                        }
                    }
                    break;
                default:
                    monitor.Log($"Unknown stat {stat}", LogLevel.Info);
                    break;
            }
        }
    }
}
