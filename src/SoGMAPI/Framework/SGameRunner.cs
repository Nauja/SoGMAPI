using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoGModdingAPI.Framework.Events;
using SoGModdingAPI.Framework.Input;
using SoGModdingAPI.Framework.Reflection;
using SoG;

namespace SoGModdingAPI.Framework
{
    /// <summary>SMAPI's extension of the game's core <see cref="GameRunner"/>, used to inject SMAPI components.</summary>
    internal class SGameRunner
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging for SMAPI.</summary>
        private readonly Monitor Monitor;

        /// <summary>Manages SMAPI events for mods.</summary>
        private readonly EventManager Events;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        private readonly Action<string> ExitGameImmediately;

        /// <summary>The core SMAPI mod hooks.</summary>
        private readonly SModHooks ModHooks;

        /// <summary>The core multiplayer logic.</summary>
        private readonly SMultiplayer Multiplayer;

        /// <summary>Raised after the game finishes loading its initial content.</summary>
        private readonly Action OnGameContentLoaded;

        /// <summary>Raised when XNA is updating (roughly 60 times per second).</summary>
        private readonly Action<GameTime, Action> OnGameUpdating;

        /// <summary>Raised when the game instance for a local split-screen player is updating (once per <see cref="OnGameUpdating"/> per player).</summary>
        private readonly Action<SGame, GameTime, Action> OnPlayerInstanceUpdating;

        /// <summary>Raised before the game exits.</summary>
        private readonly Action OnGameExiting;


        /*********
        ** Public methods
        *********/
        /// <summary>The singleton instance.</summary>
        public static SGameRunner Instance => null;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging for SMAPI.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="eventManager">Manages SMAPI events for mods.</param>
        /// <param name="modHooks">Handles mod hooks provided by the game.</param>
        /// <param name="multiplayer">The core multiplayer logic.</param>
        /// <param name="exitGameImmediately">Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</param>
        /// <param name="onGameContentLoaded">Raised after the game finishes loading its initial content.</param>
        /// <param name="onGameUpdating">Raised when XNA is updating its state (roughly 60 times per second).</param>
        /// <param name="onPlayerInstanceUpdating">Raised when the game instance for a local split-screen player is updating (once per <see cref="OnGameUpdating"/> per player).</param>
        /// <param name="onGameExiting">Raised before the game exits.</param>
        public SGameRunner(Monitor monitor, Reflector reflection, EventManager eventManager, SModHooks modHooks, SMultiplayer multiplayer, Action<string> exitGameImmediately, Action onGameContentLoaded, Action<GameTime, Action> onGameUpdating, Action<SGame, GameTime, Action> onPlayerInstanceUpdating, Action onGameExiting)
        {
            // init XNA
            // @todo Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // hook into game
            this.ModHooks = modHooks;

            // init SMAPI
            this.Monitor = monitor;
            this.Events = eventManager;
            this.Reflection = reflection;
            this.Multiplayer = multiplayer;
            this.ExitGameImmediately = exitGameImmediately;
            this.OnGameContentLoaded = onGameContentLoaded;
            this.OnGameUpdating = onGameUpdating;
            this.OnPlayerInstanceUpdating = onPlayerInstanceUpdating;
            this.OnGameExiting = onGameExiting;
        }
    }
}
