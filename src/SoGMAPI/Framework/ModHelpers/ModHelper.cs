using System;
using System.IO;
using SoGModdingAPI.Events;
#if SOGMAPI_DEPRECATED
using SoGModdingAPI.Framework.Deprecations;
#endif
using SoGModdingAPI.Framework.Input;

namespace SoGModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides simplified APIs for writing mods.</summary>
    internal class ModHelper : BaseHelper, IModHelper, IDisposable
    {
#if SOGMAPI_DEPRECATED
        /*********
        ** Fields
        *********/
        /// <summary>The backing field for <see cref="Content"/>.</summary>
        [Obsolete("This only exists to support legacy code and will be removed in SoGMAPI 4.0.0.")]
        private readonly ContentHelper ContentImpl;
#endif


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string DirectoryPath { get; }

        /// <inheritdoc />
        public IModEvents Events { get; }

#if SOGMAPI_DEPRECATED
        /// <inheritdoc />
        [Obsolete($"Use {nameof(IGameContentHelper)} or {nameof(IModContentHelper)} instead.")]
        public IContentHelper Content
        {
            get
            {
                SCore.DeprecationManager.Warn(
                    source: this.Mod,
                    nounPhrase: $"{nameof(IModHelper)}.{nameof(IModHelper.Content)}",
                    version: "3.14.0",
                    severity: DeprecationLevel.PendingRemoval
                );

                return this.ContentImpl;
            }
        }
#endif

        /// <inheritdoc />
        public IGameContentHelper GameContent { get; }

        /// <inheritdoc />
        public IModContentHelper ModContent { get; }

        /// <inheritdoc />
        public IContentPackHelper ContentPacks { get; }

        /// <inheritdoc />
        public IDataHelper Data { get; }

        /// <inheritdoc />
        public IInputHelper Input { get; }

        /// <inheritdoc />
        public IReflectionHelper Reflection { get; }

        /// <inheritdoc />
        public IModRegistry ModRegistry { get; }

        /// <inheritdoc />
        public ICommandHelper ConsoleCommands { get; }

        /// <inheritdoc />
        public IMultiplayerHelper Multiplayer { get; }

        /// <inheritdoc />
        public ITranslationHelper Translation { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod using this instance.</param>
        /// <param name="modDirectory">The full path to the mod's folder.</param>
        /// <param name="currentInputState">Manages the game's input state for the current player instance. That may not be the main player in split-screen mode.</param>
        /// <param name="events">Manages access to events raised by SoGMAPI.</param>
        /// <param name="contentHelper">An API for loading content assets.</param>
        /// <param name="gameContentHelper">An API for loading content assets from the game's <c>Content</c> folder or via <see cref="IModEvents.Content"/>.</param>
        /// <param name="modContentHelper">An API for loading content assets from your mod's files.</param>
        /// <param name="contentPackHelper">An API for managing content packs.</param>
        /// <param name="commandHelper">An API for managing console commands.</param>
        /// <param name="dataHelper">An API for reading and writing persistent mod data.</param>
        /// <param name="modRegistry">an API for fetching metadata about loaded mods.</param>
        /// <param name="reflectionHelper">An API for accessing private game code.</param>
        /// <param name="multiplayer">Provides multiplayer utilities.</param>
        /// <param name="translationHelper">An API for reading translations stored in the mod's <c>i18n</c> folder.</param>
        /// <exception cref="ArgumentNullException">An argument is null or empty.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="modDirectory"/> path does not exist on disk.</exception>
        public ModHelper(
            IModMetadata mod, string modDirectory, Func<SInputState> currentInputState, IModEvents events,
#if SOGMAPI_DEPRECATED
            ContentHelper contentHelper,
#endif
            IGameContentHelper gameContentHelper, IModContentHelper modContentHelper, IContentPackHelper contentPackHelper, ICommandHelper commandHelper, IDataHelper dataHelper, IModRegistry modRegistry, IReflectionHelper reflectionHelper, IMultiplayerHelper multiplayer, ITranslationHelper translationHelper
        )
            : base(mod)
        {
            // validate directory
            if (string.IsNullOrWhiteSpace(modDirectory))
                throw new ArgumentNullException(nameof(modDirectory));
            if (!Directory.Exists(modDirectory))
                throw new InvalidOperationException("The specified mod directory does not exist.");

            // initialize
            this.DirectoryPath = modDirectory;
#if SOGMAPI_DEPRECATED
            this.ContentImpl = contentHelper ?? throw new ArgumentNullException(nameof(contentHelper));
#endif
            this.GameContent = gameContentHelper ?? throw new ArgumentNullException(nameof(gameContentHelper));
            this.ModContent = modContentHelper ?? throw new ArgumentNullException(nameof(modContentHelper));
            this.ContentPacks = contentPackHelper ?? throw new ArgumentNullException(nameof(contentPackHelper));
            this.Data = dataHelper ?? throw new ArgumentNullException(nameof(dataHelper));
            this.Input = new InputHelper(mod, currentInputState);
            this.ModRegistry = modRegistry ?? throw new ArgumentNullException(nameof(modRegistry));
            this.ConsoleCommands = commandHelper ?? throw new ArgumentNullException(nameof(commandHelper));
            this.Reflection = reflectionHelper ?? throw new ArgumentNullException(nameof(reflectionHelper));
            this.Multiplayer = multiplayer ?? throw new ArgumentNullException(nameof(multiplayer));
            this.Translation = translationHelper ?? throw new ArgumentNullException(nameof(translationHelper));
            this.Events = events;
        }

#if SOGMAPI_DEPRECATED
        /// <summary>Get the underlying instance for <see cref="IContentHelper"/>.</summary>
        [Obsolete("This only exists to support legacy code and will be removed in SoGMAPI 4.0.0.")]
        public ContentHelper GetLegacyContentHelper()
        {
            return this.ContentImpl;
        }
#endif

        /****
        ** Mod config file
        ****/
        /// <inheritdoc />
        public TConfig ReadConfig<TConfig>()
            where TConfig : class, new()
        {
            TConfig config = this.Data.ReadJsonFile<TConfig>("config.json") ?? new TConfig();
            this.WriteConfig(config); // create file or fill in missing fields
            return config;
        }

        /// <inheritdoc />
        public void WriteConfig<TConfig>(TConfig config)
            where TConfig : class, new()
        {
            this.Data.WriteJsonFile("config.json", config);
        }

        /****
        ** Disposal
        ****/
        /// <inheritdoc />
        public void Dispose()
        {
            // nothing to dispose yet
        }
    }
}
