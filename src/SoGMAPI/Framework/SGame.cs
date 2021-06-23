using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoGModdingAPI.Events;
using SoGModdingAPI.Framework.Events;
using SoGModdingAPI.Framework.Input;
using SoGModdingAPI.Framework.Reflection;
using SoGModdingAPI.Utilities;
using SoGModdingAPI.Framework.Utilities;
using SoG;
using static SoGModdingAPI.Framework.Input.InputState;

namespace SoGModdingAPI.Framework
{
    public class LocalizedContentManager
    {
        public class LanguageCode
        {
            public static LanguageCode en = new LanguageCode();
        }

        public static LanguageCode CurrentLanguageCode = LanguageCode.en;
    }

    /// <summary>SMAPI's extension of the game's core <see cref="Game1"/>, used to inject events.</summary>
    public class SGame : Game1
    {
        public static SGame Instance
        {
            get;
            private set;
        }

        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging for SMAPI.</summary>
        private readonly Monitor Monitor;

        /// <summary>Manages SMAPI events for mods.</summary>
        private readonly EventManager Events;

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from a draw error.</summary>
        private readonly Countdown DrawCrashTimer = new Countdown(60); // 60 ticks = roughly one second

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        private readonly Action<string> ExitGameImmediately;

        /// <summary>The initial override for <see cref="Input"/>. This value is null after initialization.</summary>
        private SInputState InitialInput;

        /// <summary>The initial override for <see cref="Multiplayer"/>. This value is null after initialization.</summary>
        private SMultiplayer InitialMultiplayer;

        private Action OnInitialized;

        /// <summary>Raised when the instance is updating its state (roughly 60 times per second).</summary>
        private Action<SGame, GameTime, Action> OnUpdating;


        /*********
        ** Accessors
        *********/
        /// <summary>Manages input visible to the game.</summary>
        public SInputState Input => null;

        /// <summary>Whether the current update tick is the first one for this instance.</summary>
        public bool IsFirstTick = true;

        /// <summary>The number of ticks until SMAPI should notify mods that the game has loaded.</summary>
        /// <remarks>Skipping a few frames ensures the game finishes initializing the world before mods try to change it.</remarks>
        public Countdown AfterLoadTimer { get; } = new Countdown(5);

        /// <summary>Whether the game is saving and SMAPI has already raised <see cref="IGameLoopEvents.Saving"/>.</summary>
        public bool IsBetweenSaveEvents { get; set; }

        /// <summary>Whether the game is creating the save file and SMAPI has already raised <see cref="IGameLoopEvents.SaveCreating"/>.</summary>
        public bool IsBetweenCreateEvents { get; set; }

        /// <summary>The cached <see cref="Farmer.UniqueMultiplayerID"/> value for this instance's player.</summary>
        public long? PlayerId { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SGame()
            : base()
        {
            Instance = this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="instanceIndex">The instance index.</param>
        /// <param name="monitor">Encapsulates monitoring and logging for SMAPI.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="eventManager">Manages SMAPI events for mods.</param>
        /// <param name="input">Manages the game's input state.</param>
        /// <param name="modHooks">Handles mod hooks provided by the game.</param>
        /// <param name="multiplayer">The core multiplayer logic.</param>
        /// <param name="exitGameImmediately">Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</param>
        /// <param name="onUpdating">Raised when the instance is updating its state (roughly 60 times per second).</param>
        internal void PreInitialize(PlayerIndex playerIndex, int instanceIndex, Monitor monitor, Reflector reflection, EventManager eventManager, SInputState input, SModHooks modHooks, SMultiplayer multiplayer, Action<string> exitGameImmediately, Action onInitialized, Action<GameTime, Action> onUpdating)
        {
            this.OnInitialized = onInitialized;
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.OnInitialized();
        }
    }
}
