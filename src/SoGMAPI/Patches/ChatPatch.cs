using System;
using System.Diagnostics.CodeAnalysis;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
using SoG;
#endif
using SoGModdingAPI.Enums;
using SoGModdingAPI.Framework;
using SoGModdingAPI.Framework.Patching;
using SoGModdingAPI.Framework.Reflection;

namespace SoGModdingAPI.Patches
{
    /// <summary>Harmony patches which notify SoGMAPI for chat events.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class ChatPatch : IHarmonyPatch
    {
        /*********
        ** Fields
        *********/
        /// <summary>Simplifies access to private code.</summary>
        private static Reflector Reflection;

        /// <summary>Whether the game is running running the code in <see cref="Game1.loadForNewGame"/>.</summary>
        private static bool IsInLoadForNewGame;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="reflection">Simplifies access to private code.</param>
        public ChatPatch(Reflector reflection)
        {
            ChatPatch.Reflection = reflection;
        }

        /// <inheritdoc />
#if HARMONY_2
        public void Apply(Harmony harmony)
#else
        public void Apply(HarmonyInstance harmony)
#endif
        {
            // detect CreatedBasicInfo
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1._Chat_ParseCommand)),
                prefix: new HarmonyMethod(this.GetType(), nameof(ChatPatch.Before_Game1_Chat_ParseCommand))
            );

        }


        /*********
        ** Private methods
        *********/
        /// <summary>Called before <see cref="TitleMenu.createdNewCharacter"/>.</summary>
        /// <returns>Returns whether to execute the original method.</returns>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        private static bool Before_Game1_Chat_ParseCommand(string sMessage, long iConnection)
        {
            return !SCore.Instance.HandleCommand(sMessage);
        }
    }
}
