using System.Collections.Generic;
using SoGModdingAPI.Enums;
using SoGModdingAPI.Events;
using SoGModdingAPI.Utilities;
using SoG;

namespace SoGModdingAPI
{
    /// <summary>Provides information about the current game state.</summary>
    public static class Context
    {
        /*********
        ** Fields
        *********/
        /// <summary>Whether the player has loaded a save and the world has finished initializing.</summary>
        private static readonly PerScreen<bool> IsWorldReadyForScreen = new PerScreen<bool>();

        /// <summary>The current stage in the game's loading process.</summary>
        private static readonly PerScreen<LoadStage> LoadStageForScreen = new PerScreen<LoadStage>();

        /// <summary>Whether a player save has been loaded.</summary>
        internal static bool IsSaveLoaded => false; // @todo

        /// <summary>Whether the game is currently writing to the save file.</summary>
        internal static bool IsSaving => false; // @todo

        /// <summary>The active split-screen instance IDs.</summary>
        internal static readonly ISet<int> ActiveScreenIds = new HashSet<int>();

        /// <summary>The last screen ID that was removed from the game, used to synchronize <see cref="PerScreen{T}"/>.</summary>
        internal static int LastRemovedScreenId = -1;

        /// <summary>The current stage in the game's loading process.</summary>
        internal static LoadStage LoadStage
        {
            get => Context.LoadStageForScreen.Value;
            set => Context.LoadStageForScreen.Value = value;
        }

        /// <summary>Whether the in-game world is completely unloaded and not in the process of being loaded. The world may still exist in memory at this point, but should be ignored.</summary>
        internal static bool IsWorldFullyUnloaded => Context.LoadStage == LoadStage.ReturningToTitle || Context.LoadStage == LoadStage.None;


        /*********
        ** Accessors
        *********/
        /****
        ** Game/player state
        ****/
        /// <summary>Whether the game has performed core initialization. This becomes true right before the first update tick.</summary>
        public static bool IsGameLaunched { get; internal set; }

        /// <summary>Whether the player has loaded a save and the world has finished initializing.</summary>
        public static bool IsWorldReady
        {
            get => Context.IsWorldReadyForScreen.Value;
            set => Context.IsWorldReadyForScreen.Value = value;
        }

        /// <summary>Whether <see cref="IsWorldReady"/> is true and the player is free to act in the world (no menu is displayed, no cutscene is in progress, etc).</summary>
        public static bool IsPlayerFree => true; // @todo

        /// <summary>Whether <see cref="IsPlayerFree"/> is true and the player is free to move (e.g. not using a tool).</summary>
        public static bool CanPlayerMove => true; // @todo

        /// <summary>Whether the game is currently running the draw loop. This isn't relevant to most mods, since you should use <see cref="IDisplayEvents"/> events to draw to the screen.</summary>
        public static bool IsInDrawLoop { get; internal set; }

        /****
        ** Multiplayer
        ****/
        /// <summary>The unique ID of the current screen in split-screen mode. A screen is always assigned a new ID when it's opened (so a player who quits and rejoins has a new screen ID).</summary>
        public static int ScreenId => 0; // @todo

        /// <summary>Whether the game is running in multiplayer or split-screen mode (regardless of whether any other players are connected). See <see cref="IsSplitScreen"/> and <see cref="HasRemotePlayers"/> for more specific checks.</summary>
        public static bool IsMultiplayer => false; // @todo

        /// <summary>Whether this player is running on the main player's computer. This is true for both the main player and split-screen players.</summary>
        public static bool IsOnHostComputer => Context.IsMainPlayer || Context.IsSplitScreen;

        /// <summary>Whether the current player is playing in a split-screen. This is only applicable when <see cref="IsOnHostComputer"/> is true, since split-screen players on another computer are just regular remote players.</summary>
        public static bool IsSplitScreen => false; // @todo

        /// <summary>Whether there are players connected over the network.</summary>
        public static bool HasRemotePlayers => false; // @todo

        /// <summary>Whether the current player is the main player. This is always true in single-player, and true when hosting in multiplayer.</summary>
        public static bool IsMainPlayer => true; // @todo


        /*********
        ** Public methods
        *********/
        /// <summary>Get whether a screen ID is still active.</summary>
        /// <param name="id">The screen ID.</param>
        public static bool HasScreenId(int id)
        {
            return Context.ActiveScreenIds.Contains(id);
        }
    }
}
