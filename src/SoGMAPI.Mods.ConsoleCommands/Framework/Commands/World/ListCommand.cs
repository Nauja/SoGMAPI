using System;
using System.Collections.Generic;
using SoG;
using SoGModdingAPI.Framework;
using System.Linq;
 
namespace SoGModdingAPI.Mods.ConsoleCommands.Framework.Commands.World
{
    /// <summary>A command which edits the stats of entities.</summary>
    internal class ListCommand : ConsoleCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ListCommand()
            : base("list", "List entities in current level.\n\nUsage: list target [what]\n- target: type of entity to list.\n- what: attributes to list.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // parse arguments
            if (!args.TryGet(0, "target", out string target))
                return;

            string[] what = args.Skip(1).ToArray();

            // handle
            switch (target)
            {
                case "p":
                case "player":
                    Print(
                        monitor: monitor,
                        values: SGame.Instance.dixPlayers.Values,
                        singular: "player",
                        plural: "players",
                        getId: (_) => _.iConnectionIdentifier.ToString(),
                        getName: (_) => _.sSaveableName,
                        getAttr: (e, attr) =>
                        {
                            if (GetPlayerAttribute(e, attr, out string value))
                                return value;

                            if (GetEntityAttribute(e.xEntity, attr, out value))
                                return value;

                            return "";
                        },
                        what: what
                    );
                    break;
                case "e":
                case "enemy":
                    Print(
                        monitor: monitor,
                        values: SGame.Instance.dixEnemyList.Values,
                        singular: "enemy",
                        plural: "enemies",
                        getId: (_) => _.iID.ToString(),
                        getName: (_) => {
                            try
                            {
                                EnemyDescription desc = EnemyCodex.GetEnemyDescription(_.enType);
                                return desc.sFullName;
                            }
                            catch (Exception e)
                            {
                                return $"<missing {_.enType} description>";
                            }
                        },
                        getAttr: (e, attr) =>
                        {
                            if (GetEntityAttribute(e, attr, out string value))
                                return value;

                            return "";
                        },
                        what: what
                    );
                    break;
                case "n":
                case "npc":
                    Print(
                        monitor: monitor,
                        values: SGame.Instance.dixNPCList.Values,
                        singular: "npc",
                        plural: "npcs",
                        getId: (_) => _.iID.ToString(),
                        getName: (_) => {
                            try
                            {
                                NPCDescription desc = NPCCodex.GetNPCDescription(_.enType);
                                return desc.sFullName;
                            }
                            catch (Exception e)
                            {
                                return $"<missing {_.enType} description>";
                            }
                        },
                        getAttr: (e, attr) =>
                        {
                            if (GetEntityAttribute(e, attr, out string value))
                                return value;

                            return "";
                        },
                        what: what
                    );
                    break;
                case "d":
                case "dynamic":
                    Print(
                        monitor: monitor,
                        values: SGame.Instance.xEntityMaster.dixDynamicEnvironment.Values,
                        singular: "dynamic",
                        plural: "dynamics",
                        getId: (_) => _.iID.ToString(),
                        getName: (_) => _.enType.ToString(),
                        getAttr: (value, attr) => "",
                        what: what
                    );
                    break;
                case "i":
                case "inventory":
                    Print(
                        monitor: monitor,
                        values: SGame.Instance.xLocalPlayer.xInventory.denxInventory.Values,
                        singular: "item",
                        plural: "items",
                        getId: (_) => _.xItemDescription.enType.ToString(),
                        getName: (_) => _.xItemDescription.sFullName,
                        getAttr: (e, attr) =>
                        {
                            if (GetItemAttribute(e, attr, out string value))
                                return value;

                            return "";
                        },
                        what: what
                    );
                    break;
                default:
                    monitor.Log($"Unknown target {target}", LogLevel.Info);
                    break;
            }
        }

        private static void Print<T>(IMonitor monitor, ICollection<T> values, string singular, string plural, Func<T, string> getId, Func<T, string> getName, Func<T, string, string> getAttr, string[] what)
        {
            int total = values.Count;
            string message = $"{total} {(total == 1 ? singular : plural)}";
            CAS.AddChatMessage(message);
            monitor.Log($"{message}{(total == 0 ? "." : ":")}", LogLevel.Info);
            if (total != 0)
            {
                // Display column names
                string columns = String.Join(" ", what.Select(_ => _.ToUpper()));
                monitor.Log($"- ID: NAME {columns}", LogLevel.Info);
                // Display entries
                foreach (var value in values)
                {
                    string attributes = String.Join(" ", what.Select(_ => getAttr(value, _.ToLower())).Where(_ => _.Length != 0));
                    monitor.Log($"- {getId(value)}: {getName(value)} {attributes}", LogLevel.Info);
                }
            }
        }

        private static bool GetPlayerAttribute(PlayerView player, string attr, out string value)
        {
            switch (attr)
            {
                case "level":
                    value = player.xViewStats.iLevel.ToString();
                    return true;
                default:
                    value = "";
                    return false;
            }
        }

        private static bool GetEntityAttribute(WorldActor entity, string attr, out string value)
        {
            switch (attr)
            {
                case "hp":
                    value = entity.xBaseStats.iHP.ToString();
                    return true;
                case "ep":
                    value = entity.xBaseStats.iEP.ToString();
                    return true;
                case "level":
                    value = entity.xBaseStats.iLevel.ToString();
                    return true;
                default:
                    value = "";
                    return false;
            }
        }

        private static bool GetItemAttribute(Inventory.DisplayItem item, string attr, out string value)
        {
            switch (attr)
            {
                case "amount":
                    value = item.iAmount.ToString();
                    return true;
                case "fancyness":
                    value = item.xItemDescription.byFancyness.ToString();
                    return true;
                case "level":
                    value = item.xItemDescription.iInternalLevel.ToString();
                    return true;
                default:
                    value = "";
                    return false;
            }
        }
    }
}
