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
using System.Reflection;
using SoGModdingAPI.Framework.ContentManagers;
using Microsoft.Xna.Framework.Content;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace SoGModdingAPI.Framework
{
    public static class MyExtensions
    {
        public static IEntity FindEntityByTag(this EntityMaster entityMaster, string tag)
        {
            return entityMaster.lxActiveEnemies.First(e => e.sTag == tag);
        }

        public static ChatbubbleRendercomponent AddBubble(this Game1 game, TransformComponent transform, string content, int popupTimer = 180)
        {
            ChatbubbleRendercomponent chatbubbleRendercomponent = new ChatbubbleRendercomponent(ChatbubbleRendercomponent.ChatTileSet.Regular, transform, content);
            game.xRenderMaster.RegisterGUIRenderComponent(chatbubbleRendercomponent, true);
            chatbubbleRendercomponent.xBubble.iPopupTimer = popupTimer;
            chatbubbleRendercomponent.RefreshBubble();
            return chatbubbleRendercomponent;
        }
    }

    internal class LocalizedContentManager : ContentManager
    {
        public static IDictionary<string, string> localizedAssetNames => new Dictionary<string, string>();

        public readonly CultureInfo CurrentCulture;
        public LanguageCode GetCurrentLanguage() { return LanguageCode.en; }

        public enum LanguageCode
        {
            en
        }

        public static LanguageCode CurrentLanguageCode = LanguageCode.en;

        protected LocalizedContentManager(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture)
            : base(serviceProvider, rootDirectory)
        {
            // init
            this.CurrentCulture = currentCulture;
        }

        public LocalizedContentManager(IServiceProvider serviceProvider, string rootDirectory) : this(serviceProvider, rootDirectory, CultureInfo.CurrentCulture)
        {
        }

        public virtual T Load<T>(string assetName, LanguageCode language)
        {
            return base.Load<T>(assetName);
        }

        public virtual T LoadBase<T>(string assetName)
        {
            return base.Load<T>(assetName);
        }

        public string LanguageCodeString(LanguageCode language)
        {
            return "en"; // @todo
        }

        public virtual LocalizedContentManager CreateTemporary()
        {
            return this;
        }

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

        private Action OnContentLoaded;

        /// <summary>Raised when the instance is updating its state (roughly 60 times per second).</summary>
        private Action<SGame, GameTime, Action> OnUpdating;

        private Action<SGame, GameTime, Action> OnPlayerInstanceUpdating;


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

        /// <summary>Construct a content manager to read game content files.</summary>
        /// <remarks>This must be static because the game accesses it before the <see cref="SGame"/> constructor is called.</remarks>
        internal static Func<IServiceProvider, string, GameContentManager> CreateContentManagerImpl;


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
        internal void PreInitialize(PlayerIndex playerIndex, int instanceIndex, Monitor monitor, Reflector reflection, EventManager eventManager, SInputState input, SModHooks modHooks, SMultiplayer multiplayer, Action<string> exitGameImmediately, Action onInitialized, Action onContentLoaded, Action<SGame, GameTime, Action> onUpdating, Action<SGame, GameTime, Action> onPlayerInstanceUpdating)
        {
            this.OnInitialized = onInitialized;
            this.OnContentLoaded = onContentLoaded;
            this.OnUpdating = onUpdating;
            this.OnPlayerInstanceUpdating = onPlayerInstanceUpdating;
        }

        /*********
        ** Protected methods
        *********/
        protected override void Initialize()
        {
            base.Initialize();
            this.OnInitialized();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            // Replace contInitialLoad
            GameContentManager contentManager = CreateContentManager(this.Content.ServiceProvider, this.Content.RootDirectory);
            FieldInfo f = typeof(Game1).GetField("contInitialLoad", BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(this, contentManager);
            RenderMaster.LoadingScreenAssets.txSplash = contentManager.Load<Texture2D>("Splash");

            this.OnContentLoaded();
        }

        protected override void Update(GameTime gameTime)
        {
            this.OnUpdating(this, gameTime, () => base.Update(gameTime));
            this.OnPlayerInstanceUpdating(this, gameTime, () => { });
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Construct a content manager to read game content files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        internal GameContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            if (CreateContentManagerImpl == null)
                throw new InvalidOperationException($"The {nameof(SGame)}.{nameof(CreateContentManagerImpl)} must be set.");

            return CreateContentManagerImpl(serviceProvider, rootDirectory);
        }
    }
}
