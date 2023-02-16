using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
#if SOGMAPI_FOR_XNA
using System.Windows.Forms;
#endif
using Newtonsoft.Json;
using SoGModdingAPI.Enums;
using SoGModdingAPI.Events;
using SoGModdingAPI.Framework.Content;
using SoGModdingAPI.Framework.Events;
using SoGModdingAPI.Framework.Exceptions;
using SoGModdingAPI.Framework.Input;
using SoGModdingAPI.Framework.ModHelpers;
using SoGModdingAPI.Framework.ModLoading;
using SoGModdingAPI.Framework.Reflection;
using SoGModdingAPI.Toolkit;
using SoGModdingAPI.Toolkit.Framework.Clients.WebApi;
using SoGModdingAPI.Toolkit.Framework.ModData;
using SoGModdingAPI.Toolkit.Serialization;
using SoGModdingAPI.Toolkit.Utilities;
using SoGModdingAPI.Utilities;
using SoG;
using SoGModdingAPI.Framework.Utilities;
using SoGModdingAPI.Framework.Models;
using SoGModdingAPI.Framework.Logging;
using SoGModdingAPI.Framework.Serialization;
using SoGModdingAPI.Framework.Networking;
using SoGModdingAPI.Framework.Patching;
using SoGModdingAPI.Patches;
using SoGModdingAPI.Framework.ContentManagers;
using SoGModdingAPI.Framework.StateTracking.Comparers;
using static SoGModdingAPI.Framework.Input.InputState;

namespace SoGModdingAPI.Framework
{
    /// <summary>The core class which initializes and manages SMAPI.</summary>
    internal class SCore : IDisposable
    {
        /*********
        ** Fields
        *********/
        /****
        ** Low-level components
        ****/
        /// <summary>Tracks whether the game should exit immediately and any pending initialization should be cancelled.</summary>
        private readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();

        /// <summary>Manages the SMAPI console window and log file.</summary>
        private readonly LogManager LogManager;

        /// <summary>The core logger and monitor for SMAPI.</summary>
        private Monitor Monitor => this.LogManager.Monitor;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection = new Reflector();

        /// <summary>Encapsulates access to SMAPI core translations.</summary>
        private readonly Translator Translator = new Translator();

        /// <summary>The SMAPI configuration settings.</summary>
        private readonly SConfig Settings;

        /// <summary>The mod toolkit used for generic mod interactions.</summary>
        private readonly ModToolkit Toolkit = new ModToolkit();

        /****
        ** Higher-level components
        ****/
        /// <summary>Manages console commands.</summary>
        private readonly CommandManager CommandManager;

        /// <summary>The underlying game instance.</summary>
        private SGame Game;

        /// <summary>SMAPI's content manager.</summary>
        private ContentCoordinator ContentCore;

        /// <summary>The game's core multiplayer utility for the main player.</summary>
        private SMultiplayer Multiplayer;

        /// <summary>Tracks the installed mods.</summary>
        /// <remarks>This is initialized after the game starts.</remarks>
        private readonly ModRegistry ModRegistry = new ModRegistry();

        /// <summary>Manages SMAPI events for mods.</summary>
        private readonly EventManager EventManager;


        /****
        ** State
        ****/
        /// <summary>The path to search for mods.</summary>
        private string ModsPath => Constants.ModsPath;

        /// <summary>Whether the game is currently running.</summary>
        private bool IsGameRunning;

        /// <summary>Whether the program has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>Whether the next content manager requested by the game will be for <see cref="Game1.content"/>.</summary>
        private bool NextContentManagerIsMain;

        /// <summary>Whether post-game-startup initialization has been performed.</summary>
        private bool IsInitialized;

        /// <summary>Whether InitializeBeforeFirstAssetLoaded has been called already.</summary>
        private bool IsFirstAssetLoaded;

        /// <summary>Whether the player just returned to the title screen.</summary>
        public bool JustReturnedToTitle { get; set; }

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from an update error.</summary>
        private readonly Countdown UpdateCrashTimer = new Countdown(60); // 60 ticks = roughly one second

        /// <summary>Asset interceptors added or removed since the last tick.</summary>
        private readonly List<AssetInterceptorChange> ReloadAssetInterceptorsQueue = new List<AssetInterceptorChange>();

        /// <summary>A list of queued commands to parse and execute.</summary>
        /// <remarks>This property must be thread-safe, since it's accessed from a separate console input thread.</remarks>
        private readonly ConcurrentQueue<string> RawCommandQueue = new ConcurrentQueue<string>();

        /// <summary>A list of commands to execute on each screen.</summary>
        private readonly PerScreen<List<Tuple<Command, string, string, string[]>>> ScreenCommandQueue = new PerScreen<List<Tuple<Command, string, string, string[]>>>(() => new List<Tuple<Command, string, string, string[]>>());


        /*********
        ** Accessors
        *********/
        /// <summary>Manages deprecation warnings.</summary>
        /// <remarks>This is initialized after the game starts. This is accessed directly because it's not part of the normal class model.</remarks>
        internal static DeprecationManager DeprecationManager { get; private set; }

        /// <summary>The singleton instance.</summary>
        /// <remarks>This is only intended for use by external code like the Error Handler mod.</remarks>
        internal static SCore Instance { get; private set; }

        /// <summary>The number of update ticks which have already executed. This is similar to <see cref="Game1.ticks"/>, but incremented more consistently for every tick.</summary>
        internal static uint TicksElapsed { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modsPath">The path to search for mods.</param>
        /// <param name="writeToConsole">Whether to output log messages to the console.</param>
        public SCore(string modsPath, bool writeToConsole)
        {
            SCore.Instance = this;

            // init paths
            this.VerifyPath(modsPath);
            this.VerifyPath(Constants.LogDir);
            Constants.ModsPath = modsPath;

            // init log file
            this.PurgeNormalLogs();
            string logPath = this.GetLogPath();

            // init basics
            this.Settings = JsonConvert.DeserializeObject<SConfig>(File.ReadAllText(Constants.ApiConfigPath));
            if (File.Exists(Constants.ApiUserConfigPath))
                JsonConvert.PopulateObject(File.ReadAllText(Constants.ApiUserConfigPath), this.Settings);

            this.LogManager = new LogManager(logPath: logPath, colorConfig: this.Settings.ConsoleColors, writeToConsole: writeToConsole, isVerbose: this.Settings.VerboseLogging, isDeveloperMode: this.Settings.DeveloperMode, getScreenIdForLog: this.GetScreenIdForLog);
            this.CommandManager = new CommandManager(this.Monitor);
            this.EventManager = new EventManager(this.ModRegistry);
            SCore.DeprecationManager = new DeprecationManager(this.Monitor, this.ModRegistry);

            // log SMAPI/OS info
            this.LogManager.LogIntro(modsPath, this.Settings.GetCustomSettings());

            // validate platform
#if SOGMAPI_FOR_WINDOWS
            if (Constants.Platform != Platform.Windows)
            {
                this.Monitor.Log("Oops! You're running Windows, but this version of SoGMAPI is for Linux or macOS. Please reinstall SoGMAPI to fix this.", LogLevel.Error);
                this.LogManager.PressAnyKeyToExit();
            }
#else
            if (Constants.Platform == Platform.Windows)
            {
                this.Monitor.Log($"Oops! You're running {Constants.Platform}, but this version of SoGMAPI is for Windows. Please reinstall SoGMAPI to fix this.", LogLevel.Error);
                this.LogManager.PressAnyKeyToExit();
            }
#endif
        }


        /// <summary>Assert that the game version is within <see cref="Constants.MinimumGameVersion"/> and <see cref="Constants.MaximumGameVersion"/>.</summary>
        private void AssertGameVersion()
        {
            // Fill patch notes to generate version number
            MethodInfo m = typeof(Game1).GetMethod("_Menu_FillPatchNotes", BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(this.Game, new object[] { });
            Constants.SetGameVersion(new SemanticVersion(GlobalData.MainMenu.PatchNoteMenu.lxNotes[0].sPatchName));
            GlobalData.MainMenu.PatchNoteMenu.lxNotes.Clear();

            // min version
            if (Constants.GameVersion.IsOlderThan(Constants.MinimumGameVersion))
            {
                ISemanticVersion suggestedApiVersion = Constants.GetCompatibleApiVersion(Constants.GameVersion);
                this.Monitor.Log(suggestedApiVersion != null
                    ? $"Oops! You're running Secrets Of Grindea {Constants.GameVersion}, but the oldest supported version is {Constants.MinimumGameVersion}. You can install SoGMAPI {suggestedApiVersion} instead to fix this error, or update your game to the latest version."
                    : $"Oops! You're running Secrets Of Grindea {Constants.GameVersion}, but the oldest supported version is {Constants.MinimumGameVersion}. Please update your game before using SoGMAPI.",
                    LogLevel.Error
                );
                this.LogManager.PressAnyKeyToExit();
            }

            // max version
            else if (Constants.MaximumGameVersion != null && Constants.GameVersion.IsNewerThan(Constants.MaximumGameVersion))
            {
                this.Monitor.Log($"Oops! You're running Secrets Of Grindea {Constants.GameVersion}, but this version of SoGMAPI is only compatible up to Secrets Of Grindea {Constants.MaximumGameVersion}. Please check for a newer version of SoGMAPI: https://smapi.io.",
                    LogLevel.Error
                );
                this.LogManager.PressAnyKeyToExit();
            }
        }

        /// <summary>Launch SMAPI.</summary>
        [HandleProcessCorruptedStateExceptions, SecurityCritical] // let try..catch handle corrupted state exceptions
        public void RunInteractively()
        {
            // initialize SMAPI
            try
            {
                JsonConverter[] converters = {
                    new ColorConverter(),
                    new KeybindConverter(),
                    new PointConverter(),
                    new Vector2Converter(),
                    new RectangleConverter()
                };
                foreach (JsonConverter converter in converters)
                    this.Toolkit.JsonHelper.JsonSettings.Converters.Add(converter);

                // add error handlers
#if SOGMAPI_FOR_XNA
                Application.ThreadException += (sender, e) => this.Monitor.Log($"Critical thread exception: {e.Exception.GetLogSummary()}", LogLevel.Error);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
#endif
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => this.Monitor.Log($"Critical app domain exception: {e.ExceptionObject}", LogLevel.Error);

                // add more lenient assembly resolver
                AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => AssemblyLoader.ResolveAssembly(e.Name);

                // hook locale event
                // @todo LocalizedContentManager.OnLanguageChange += locale => this.OnLocaleChanged();

                // override game
                this.Multiplayer = new SMultiplayer(this.Monitor, this.EventManager, this.Toolkit.JsonHelper, this.ModRegistry, this.Reflection, this.OnModMessageReceived, this.Settings.LogNetworkTraffic);
                SGame.CreateContentManagerImpl = this.CreateContentManager; // must be static since the game accesses it before the SGame constructor is called
                this.Game = new SGame();

                // must be checked here
                this.AssertGameVersion();

                this.Game.PreInitialize(
                    playerIndex: 0,
                    instanceIndex: 0,
                    monitor: this.Monitor,
                    reflection: this.Reflection,
                    eventManager: this.EventManager,
                    input: new InputState.SInputState(),
                    modHooks: new SModHooks(),
                    multiplayer: this.Multiplayer,
                    exitGameImmediately: this.ExitGameImmediately,
                    onInitialized: () => { },
                    onContentLoaded: this.OnGameContentLoaded,
                    onUpdating: this.OnGameUpdating,
                    onPlayerInstanceUpdating: this.OnPlayerInstanceUpdating
                );

                // Assign Game1 instance to SoG.Program
                Assembly a = Assembly.GetAssembly(typeof(Game1));
                Type t = a?.GetType("SoG.Program");
                FieldInfo f = t?.GetField("game", BindingFlags.Public | BindingFlags.Static);
                if (f == null)
                {
                    this.Monitor.Log("Unable to find static SoG.Program.game variable.", LogLevel.Error);
                    this.LogManager.PressAnyKeyToExit();
                }
                f.SetValue(null, this.Game);

                // apply game patches
                new GamePatcher(this.Monitor).Apply(
                    new ChatPatch(this.Reflection)
                );

                // add exit handler
                this.CancellationToken.Token.Register(() =>
                {
                    if (this.IsGameRunning)
                    {
                        this.LogManager.WriteCrashLog();
                        this.Game.Exit();
                    }
                });

                // set window titles
                this.UpdateWindowTitles();
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"SoGMAPI failed to initialize: {ex.GetLogSummary()}", LogLevel.Error);
                this.LogManager.PressAnyKeyToExit();
                return;
            }

            // log basic info
            this.LogManager.HandleMarkerFiles();
            this.LogManager.LogSettingsHeader(this.Settings);

            // set window titles
            this.UpdateWindowTitles();

            // start game
            this.Monitor.Log("Starting game...", LogLevel.Debug);
            try
            {
                this.IsGameRunning = true;
                this.Game.Run();
            }
            catch (Exception ex)
            {
                this.LogManager.LogFatalLaunchError(ex);
                this.LogManager.PressAnyKeyToExit();
            }
            finally
            {
                this.Dispose();
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            // skip if already disposed
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;
            this.Monitor.Log("Disposing...");

            // dispose mod data
            foreach (IModMetadata mod in this.ModRegistry.GetAll())
            {
                try
                {
                    (mod.Mod as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    mod.LogAsMod($"Mod failed during disposal: {ex.GetLogSummary()}.", LogLevel.Warn);
                }
            }

            // dispose core components
            this.IsGameRunning = false;
            this.ContentCore?.Dispose();
            this.CancellationToken?.Dispose();
            // @todo this.Game?.Dispose();
            this.LogManager?.Dispose(); // dispose last to allow for any last-second log messages

            // end game (moved from Game1.OnExiting to let us clean up first)
            Process.GetCurrentProcess().Kill();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Initialize mods before the first game asset is loaded. At this point the core content managers are loaded (so mods can load their own assets), but the game is mostly uninitialized.</summary>
        private void InitializeBeforeFirstAssetLoaded()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                this.Monitor.Log("SoGMAPI shutting down: aborting initialization.", LogLevel.Warn);
                return;
            }

            if (this.IsFirstAssetLoaded)
            {
                return;
            }

            IsFirstAssetLoaded = true;

            // load mod data
            ModToolkit toolkit = new ModToolkit();
            ModDatabase modDatabase = toolkit.GetModDatabase(Constants.ApiMetadataPath);

            // load mods
            {
                this.Monitor.Log("Loading mod metadata...");
                ModResolver resolver = new ModResolver();

                // log loose files
                {
                    string[] looseFiles = new DirectoryInfo(this.ModsPath).GetFiles().Select(p => p.Name).ToArray();
                    if (looseFiles.Any())
                        this.Monitor.Log($"  Ignored loose files: {string.Join(", ", looseFiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))}");
                }

                // load manifests
                IModMetadata[] mods = resolver.ReadManifests(toolkit, this.ModsPath, modDatabase).ToArray();

                // filter out ignored mods
                foreach (IModMetadata mod in mods.Where(p => p.IsIgnored))
                    this.Monitor.Log($"  Skipped {mod.GetRelativePathWithRoot()} (folder name starts with a dot).");
                mods = mods.Where(p => !p.IsIgnored).ToArray();

                // load mods
                resolver.ValidateManifests(mods, Constants.ApiVersion, toolkit.GetUpdateUrl);
                mods = resolver.ProcessDependencies(mods, modDatabase).ToArray();
                this.LoadMods(mods, this.Toolkit.JsonHelper, this.ContentCore, modDatabase);

                // check for updates
                this.CheckForUpdatesAsync(mods);
            }

            // update window titles
            this.UpdateWindowTitles();
        }

        /// <summary>Raised after the game finishes initializing.</summary>
        private void OnGameInitialized()
        {
            UpdateWindowTitles();

            // validate XNB integrity
            if (!this.ValidateContentIntegrity())
                this.Monitor.Log("SoGMAPI found problems in your game's content files which are likely to cause errors or crashes. Consider uninstalling XNB mods or reinstalling the game.", LogLevel.Error);

            // start SoGMAPI console
            new Thread(
                () => this.LogManager.RunConsoleInputLoop(
                    commandManager: this.CommandManager,
                    reloadTranslations: this.ReloadTranslations,
                    handleInput: input => this.RawCommandQueue.Enqueue(input),
                    continueWhile: () => this.IsGameRunning && !this.CancellationToken.IsCancellationRequested
                )
            ).Start();
        }

        /// <summary>Raised after the game finishes loading its initial content.</summary>
        private void OnGameContentLoaded()
        {
            InitializeBeforeFirstAssetLoaded();
        }

        /// <summary>Raised when the game is updating its state (roughly 60 times per second).</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="runGameUpdate">Invoke the game's update logic.</param>
        private void OnGameUpdating(SGame instance, GameTime gameTime, Action runGameUpdate)
        {
            try
            {
                /*********
                ** Safe queued work
                *********/
                // print warnings/alerts
                SCore.DeprecationManager.PrintQueued();

                /*********
                ** First-tick initialization
                *********/
                if (!this.IsInitialized)
                {
                    this.IsInitialized = true;
                    this.OnGameInitialized();
                }

                /*********
                ** Special cases
                *********/
                // Abort if SoGMAPI is exiting.
                if (this.CancellationToken.IsCancellationRequested)
                {
                    this.Monitor.Log("SoGMAPI shutting down: aborting update.");
                    return;
                }

                /*********
                ** Reload assets when interceptors are added/removed
                *********/
                if (this.ReloadAssetInterceptorsQueue.Any())
                {
                    // get unique interceptors
                    AssetInterceptorChange[] interceptors = this.ReloadAssetInterceptorsQueue
                        .GroupBy(p => p.Instance, new ObjectReferenceComparer<object>())
                        .Select(p => p.First())
                        .ToArray();
                    this.ReloadAssetInterceptorsQueue.Clear();

                    // log summary
                    this.Monitor.Log("Invalidating cached assets for new editors & loaders...");
                    this.Monitor.Log(
                        "   changed: "
                        + string.Join(", ",
                            interceptors
                                .GroupBy(p => p.Mod)
                                .OrderBy(p => p.Key.DisplayName)
                                .Select(modGroup =>
                                    $"{modGroup.Key.DisplayName} ("
                                    + string.Join(", ", modGroup.GroupBy(p => p.WasAdded).ToDictionary(p => p.Key, p => p.Count()).Select(p => $"{(p.Key ? "added" : "removed")} {p.Value}"))
                                    + ")"
                                )
                        )
                        + "."
                    );

                    // reload affected assets
                    this.ContentCore.InvalidateCache(asset => interceptors.Any(p => p.CanIntercept(asset)));
                }

                /*********
                ** Parse commands
                *********/
                while (this.RawCommandQueue.TryDequeue(out string rawInput))
                {
                    HandleCommand(rawInput);
                }


                /*********
                ** Run game update
                *********/
                runGameUpdate();

                /*********
                ** Reset crash timer
                *********/
                this.UpdateCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occured in the overridden update loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.UpdateCrashTimer.Decrement())
                    this.ExitGameImmediately("The game crashed when updating, and SoGMAPI was unable to recover the game.");
            }
            finally
            {
                SCore.TicksElapsed++;
            }
        }

        public bool HandleCommand(string rawInput)
        {
            // parse command
            string name;
            string[] args;
            Command command;
            int screenId;
            try
            {
                if (!this.CommandManager.TryParse(rawInput, out name, out args, out command, out screenId))
                {
                    this.Monitor.Log("Unknown command; type 'help' for a list of available commands.", LogLevel.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Failed parsing that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                return false;
            }

            // queue command for screen
            this.ScreenCommandQueue.GetValueForScreen(screenId).Add(Tuple.Create(command, rawInput, name, args));
            return true;
        }

        /// <summary>Raised when the game instance for a local player is updating (once per <see cref="OnGameUpdating"/> per player).</summary>
        /// <param name="instance">The game instance being updated.</param>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="runUpdate">Invoke the game's update logic.</param>
        private void OnPlayerInstanceUpdating(SGame instance, GameTime gameTime, Action runUpdate)
        {
            var events = this.EventManager;

            try
            {
                /*********
                ** Reapply overrides
                *********/
                if (this.JustReturnedToTitle)
                {
                    // @todo

                    this.JustReturnedToTitle = false;
                }

                /*********
                ** Execute commands
                *********/
                {
                    var commandQueue = this.ScreenCommandQueue.Value;
                    foreach (var entry in commandQueue)
                    {
                        Command command = entry.Item1;
                        string rawInput = entry.Item2;
                        string name = entry.Item3;
                        string[] args = entry.Item4;

                        // Add command to chat
                        CAS.AddChatMessage($"/{rawInput}");

                        try
                        {
                            command.Callback.Invoke(name, args);
                        }
                        catch (Exception ex)
                        {
                            if (command.Mod != null)
                                command.Mod.LogAsMod($"Mod failed handling that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                            else
                                this.Monitor.Log($"Failed handling that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                        }
                    }
                    commandQueue.Clear();
                }


                /*********
                ** Update input
                *********/
                // This should *always* run, even when suppressing mod events, since the game uses
                // this too. For example, doing this after mod event suppression would prevent the
                // user from doing anything on the overnight shipping screen.
                SInputState inputState = instance.Input;
                if (this.Game.IsActive)
                {
                    // @todo
                }

                // @todo

                /*********
                ** Update context
                *********/
                // @todo

                /*********
                ** Update watchers
                **   (Watchers need to be updated, checked, and reset in one go so we can detect any changes mods make in event handlers.)
                *********/
                // @todo

                /*********
                ** Pre-update events
                *********/
                {
                    /*********
                    ** Game update
                    *********/
                    // game launched (not raised for secondary players in split-screen mode)
                    if (instance.IsFirstTick && !Context.IsGameLaunched)
                    {
                        Context.IsGameLaunched = true;
                        events.GameLaunched.Raise(new GameLaunchedEventArgs());
                    }
                }

                /*********
                ** Game update tick
                *********/
                {
                    bool isOneSecond = SCore.TicksElapsed % 60 == 0;
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    events.UpdateTicking.RaiseEmpty();
                    if (isOneSecond)
                        events.OneSecondUpdateTicking.RaiseEmpty();
                    try
                    {
                        // @todo instance.Input.ApplyOverrides(); // if mods added any new overrides since the update, process them now
                        runUpdate();
                    }
                    catch (Exception ex)
                    {
                        this.LogManager.MonitorForGame.Log($"An error occurred in the base update loop: {ex.GetLogSummary()}", LogLevel.Error);
                    }

                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    events.UpdateTicked.RaiseEmpty();
                    if (isOneSecond)
                        events.OneSecondUpdateTicked.RaiseEmpty();
                }

                /*********
                ** Update events
                *********/
                this.UpdateCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occurred in the overridden update loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.UpdateCrashTimer.Decrement())
                    this.ExitGameImmediately("The game crashed when updating, and SMAPI was unable to recover the game.");
            }
        }

        /// <summary>Handle the game changing locale.</summary>
        private void OnLocaleChanged()
        {
            this.ContentCore.OnLocaleChanged();

            // get locale
            string locale = this.ContentCore.GetLocale();
            LocalizedContentManager.LanguageCode languageCode = this.ContentCore.Language;

            // update core translations
            this.Translator.SetLocale(locale, languageCode);

            // update mod translation helpers
            foreach (IModMetadata mod in this.ModRegistry.GetAll())
                mod.Translations.SetLocale(locale, languageCode);
        }

        /// <summary>Raised when the low-level stage while loading a save changes.</summary>
        /// <param name="newStage">The new load stage.</param>
        internal void OnLoadStageChanged(LoadStage newStage)
        {
            // nothing to do
            if (newStage == Context.LoadStage)
                return;

            // update data
            LoadStage oldStage = Context.LoadStage;
            Context.LoadStage = newStage;
            this.Monitor.VerboseLog($"Context: load stage changed to {newStage}");

            // handle stages
            switch (newStage)
            {
                case LoadStage.ReturningToTitle:
                    this.Monitor.Log("Context: returning to title");
                    this.OnReturningToTitle();
                    break;

                case LoadStage.None:
                    this.JustReturnedToTitle = true;
                    break;

                case LoadStage.Loaded:
                    break;
            }

            // raise events
            // @todo this.EventManager.LoadStageChanged.Raise(new LoadStageChangedEventArgs(oldStage, newStage));
            if (newStage == LoadStage.None)
                this.EventManager.ReturnedToTitle.RaiseEmpty();
        }

        /// <summary>Apply fixes to the save after it's loaded.</summary>
        private void ApplySaveFixes()
        {
            // @todo
        }

        /// <summary>Raised immediately before the player returns to the title screen.</summary>
        private void OnReturningToTitle()
        {
            // perform cleanup
            this.Multiplayer.CleanupOnMultiplayerExit();
            this.ContentCore.OnReturningToTitleScreen();
        }

        /// <summary>Raised before the game exits.</summary>
        private void OnGameExiting()
        {
            // @todo this.Multiplayer.Disconnect(StardewValley.Multiplayer.DisconnectType.ClosedGame);
            this.Dispose();
        }

        /// <summary>Raised when a mod network message is received.</summary>
        /// <param name="message">The message to deliver to applicable mods.</param>
        private void OnModMessageReceived(ModMessageModel message)
        {
            // @todo
        }

        /// <summary>Constructor a content manager to read game content files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        private GameContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            // Game1._temporaryContent initializing from SGame constructor
            if (this.ContentCore == null)
            {
                this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, this.Monitor, this.Reflection, this.Toolkit.JsonHelper, this.InitializeBeforeFirstAssetLoaded, this.Settings.AggressiveMemoryOptimizations);
                if (this.ContentCore.Language != this.Translator.LocaleEnum)
                    this.Translator.SetLocale(this.ContentCore.GetLocale(), this.ContentCore.Language);

                this.NextContentManagerIsMain = true;
                return this.ContentCore.CreateGameContentManager("Game1._temporaryContent");
            }

            // Game1.content initializing from LoadContent
            if (this.NextContentManagerIsMain)
            {
                this.NextContentManagerIsMain = false;
                return this.ContentCore.MainContentManager;
            }

            // any other content manager
            return this.ContentCore.CreateGameContentManager("(generated)");
        }

        /// <summary>Get the current game instance. This may not be the main player if playing in split-screen.</summary>
        private SGame GetCurrentGameInstance()
        {
            throw new InvalidOperationException("The current game instance wasn't created by SMAPI.");
        }

        /// <summary>Look for common issues with the game's XNB content, and log warnings if anything looks broken or outdated.</summary>
        /// <returns>Returns whether all integrity checks passed.</returns>
        private bool ValidateContentIntegrity()
        {
            // @todo
            return true;
        }

        /// <summary>Set the titles for the game and console windows.</summary>
        private void UpdateWindowTitles()
        {
            string smapiVersion = $"{Constants.ApiVersion}{(EarlyConstants.IsWindows64BitHack ? " [64-bit]" : "")}";

            string consoleTitle = $"SoGMAPI {smapiVersion} - running Secrets Of Grindea {Constants.GameVersion}";
            string gameTitle = $"Secrets of Grindea {Constants.GameVersion} - running SoGMAPI {smapiVersion}";

            if (this.ModRegistry.AreAllModsLoaded)
            {
                int modsLoaded = this.ModRegistry.GetAll().Count();
                consoleTitle += $" with {modsLoaded} mods";
                gameTitle += $" with {modsLoaded} mods";
            }

            this.Game.Window.Title = gameTitle;
            this.LogManager.SetConsoleTitle(consoleTitle);
        }

        /// <summary>Asynchronously check for a new version of SMAPI and any installed mods, and print alerts to the console if an update is available.</summary>
        /// <param name="mods">The mods to include in the update check (if eligible).</param>
        private void CheckForUpdatesAsync(IModMetadata[] mods)
        {
            if (!this.Settings.CheckForUpdates)
                return;

            new Thread(() =>
            {
                // create client
                string url = this.Settings.WebApiBaseUrl;
#if !SOGMAPI_FOR_WINDOWS
                url = url.Replace("https://", "http://"); // workaround for OpenSSL issues with the game's bundled Mono on Linux/macOS
#endif
                WebApiClient client = new WebApiClient(url, Constants.ApiVersion);
                this.Monitor.Log("Checking for updates...");

                // check SMAPI version
                {
                    ISemanticVersion updateFound = null;
                    string updateUrl = null;
                    try
                    {
                        // fetch update check
                        ModEntryModel response = client.GetModInfo(new[] { new ModSearchEntryModel("Pathoschild.SMAPI", Constants.ApiVersion, new[] { $"GitHub:{this.Settings.GitHubProjectName}" }) }, apiVersion: Constants.ApiVersion, gameVersion: Constants.GameVersion, platform: Constants.Platform).Single().Value;
                        updateFound = response.SuggestedUpdate?.Version;
                        updateUrl = response.SuggestedUpdate?.Url ?? Constants.HomePageUrl;

                        // log message
                        if (updateFound != null)
                            this.Monitor.Log($"You can update SMAPI to {updateFound}: {updateUrl}", LogLevel.Alert);
                        else
                            this.Monitor.Log("   SMAPI okay.");

                        // show errors
                        if (response.Errors.Any())
                        {
                            this.Monitor.Log("Couldn't check for a new version of SMAPI. This won't affect your game, but you may not be notified of new versions if this keeps happening.", LogLevel.Warn);
                            this.Monitor.Log($"Error: {string.Join("\n", response.Errors)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log("Couldn't check for a new version of SMAPI. This won't affect your game, but you won't be notified of new versions if this keeps happening.", LogLevel.Warn);
                        this.Monitor.Log(ex is WebException && ex.InnerException == null
                            ? $"Error: {ex.Message}"
                            : $"Error: {ex.GetLogSummary()}"
                        );
                    }

                    // show update message on next launch
                    if (updateFound != null)
                        this.LogManager.WriteUpdateMarker(updateFound.ToString(), updateUrl);
                }

                // check Stardew64Installer version
                if (Constants.IsPatchedBySecretsOfGrindea64Installer(out ISemanticVersion patchedByVersion))
                {
                    ISemanticVersion updateFound = null;
                    string updateUrl = null;
                    try
                    {
                        // fetch update check
                        ModEntryModel response = client.GetModInfo(new[] { new ModSearchEntryModel("Steviegt6.Stardew64Installer", patchedByVersion, new[] { $"GitHub:{this.Settings.Stardew64InstallerGitHubProjectName}" }) }, apiVersion: Constants.ApiVersion, gameVersion: Constants.GameVersion, platform: Constants.Platform).Single().Value;
                        updateFound = response.SuggestedUpdate?.Version;
                        updateUrl = response.SuggestedUpdate?.Url ?? Constants.HomePageUrl;

                        // log message
                        if (updateFound != null)
                            this.Monitor.Log($"You can update Stardew64Installer to {updateFound}: {updateUrl}", LogLevel.Alert);
                        else
                            this.Monitor.Log("   Stardew64Installer okay.");

                        // show errors
                        if (response.Errors.Any())
                        {
                            this.Monitor.Log("Couldn't check for a new version of Stardew64Installer. This won't affect your game, but you may not be notified of new versions if this keeps happening.", LogLevel.Warn);
                            this.Monitor.Log($"Error: {string.Join("\n", response.Errors)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log("Couldn't check for a new version of Stardew64Installer. This won't affect your game, but you won't be notified of new versions if this keeps happening.", LogLevel.Warn);
                        this.Monitor.Log(ex is WebException && ex.InnerException == null
                            ? $"Error: {ex.Message}"
                            : $"Error: {ex.GetLogSummary()}"
                        );
                    }
                }

                // check mod versions
                if (mods.Any())
                {
                    try
                    {
                        HashSet<string> suppressUpdateChecks = new HashSet<string>(this.Settings.SuppressUpdateChecks, StringComparer.OrdinalIgnoreCase);

                        // prepare search model
                        List<ModSearchEntryModel> searchMods = new List<ModSearchEntryModel>();
                        foreach (IModMetadata mod in mods)
                        {
                            if (!mod.HasID() || suppressUpdateChecks.Contains(mod.Manifest.UniqueID))
                                continue;

                            string[] updateKeys = mod
                                .GetUpdateKeys(validOnly: true)
                                .Select(p => p.ToString())
                                .ToArray();
                            searchMods.Add(new ModSearchEntryModel(mod.Manifest.UniqueID, mod.Manifest.Version, updateKeys.ToArray(), isBroken: mod.Status == ModMetadataStatus.Failed));
                        }

                        // fetch results
                        this.Monitor.Log($"   Checking for updates to {searchMods.Count} mods...");
                        IDictionary<string, ModEntryModel> results = client.GetModInfo(searchMods.ToArray(), apiVersion: Constants.ApiVersion, gameVersion: Constants.GameVersion, platform: Constants.Platform);

                        // extract update alerts & errors
                        var updates = new List<Tuple<IModMetadata, ISemanticVersion, string>>();
                        var errors = new StringBuilder();
                        foreach (IModMetadata mod in mods.OrderBy(p => p.DisplayName))
                        {
                            // link to update-check data
                            if (!mod.HasID() || !results.TryGetValue(mod.Manifest.UniqueID, out ModEntryModel result))
                                continue;
                            mod.SetUpdateData(result);

                            // handle errors
                            if (result.Errors != null && result.Errors.Any())
                            {
                                errors.AppendLine(result.Errors.Length == 1
                                    ? $"   {mod.DisplayName}: {result.Errors[0]}"
                                    : $"   {mod.DisplayName}:\n      - {string.Join("\n      - ", result.Errors)}"
                                );
                            }

                            // handle update
                            if (result.SuggestedUpdate != null)
                                updates.Add(Tuple.Create(mod, result.SuggestedUpdate.Version, result.SuggestedUpdate.Url));
                        }

                        // show update errors
                        if (errors.Length != 0)
                            this.Monitor.Log("Got update-check errors for some mods:\n" + errors.ToString().TrimEnd());

                        // show update alerts
                        if (updates.Any())
                        {
                            this.Monitor.Newline();
                            this.Monitor.Log($"You can update {updates.Count} mod{(updates.Count != 1 ? "s" : "")}:", LogLevel.Alert);
                            foreach (var entry in updates)
                            {
                                IModMetadata mod = entry.Item1;
                                ISemanticVersion newVersion = entry.Item2;
                                string newUrl = entry.Item3;
                                this.Monitor.Log($"   {mod.DisplayName} {newVersion}: {newUrl}", LogLevel.Alert);
                            }
                        }
                        else
                            this.Monitor.Log("   All mods up to date.");
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log("Couldn't check for new mod versions. This won't affect your game, but you won't be notified of mod updates if this keeps happening.", LogLevel.Warn);
                        this.Monitor.Log(ex is WebException && ex.InnerException == null
                            ? ex.Message
                            : ex.ToString()
                        );
                    }
                }
            }).Start();
        }

        /// <summary>Create a directory path if it doesn't exist.</summary>
        /// <param name="path">The directory path.</param>
        private void VerifyPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                // note: this happens before this.Monitor is initialized
                Console.WriteLine($"Couldn't create a path: {path}\n\n{ex.GetLogSummary()}");
            }
        }

        /// <summary>Load and hook up the given mods.</summary>
        /// <param name="mods">The mods to load.</param>
        /// <param name="jsonHelper">The JSON helper with which to read mods' JSON files.</param>
        /// <param name="contentCore">The content manager to use for mod content.</param>
        /// <param name="modDatabase">Handles access to SMAPI's internal mod metadata list.</param>
        private void LoadMods(IModMetadata[] mods, JsonHelper jsonHelper, ContentCoordinator contentCore, ModDatabase modDatabase)
        {
            this.Monitor.Log("Loading mods...");

            // load mods
            IList<IModMetadata> skippedMods = new List<IModMetadata>();
            using (AssemblyLoader modAssemblyLoader = new AssemblyLoader(Constants.Platform, Constants.GameFramework, this.Monitor, this.Settings.ParanoidWarnings, this.Settings.RewriteMods))
            {
                // init
                HashSet<string> suppressUpdateChecks = new HashSet<string>(this.Settings.SuppressUpdateChecks, StringComparer.OrdinalIgnoreCase);
                InterfaceProxyFactory proxyFactory = new InterfaceProxyFactory();

                // load mods
                foreach (IModMetadata mod in mods)
                {
                    if (!this.TryLoadMod(mod, mods, modAssemblyLoader, proxyFactory, jsonHelper, contentCore, modDatabase, suppressUpdateChecks, out ModFailReason? failReason, out string errorPhrase, out string errorDetails))
                    {
                        failReason ??= ModFailReason.LoadFailed;
                        mod.SetStatus(ModMetadataStatus.Failed, failReason.Value, errorPhrase, errorDetails);
                        skippedMods.Add(mod);
                    }
                }
            }

            IModMetadata[] loaded = this.ModRegistry.GetAll().ToArray();
            IModMetadata[] loadedContentPacks = loaded.Where(p => p.IsContentPack).ToArray();
            IModMetadata[] loadedMods = loaded.Where(p => !p.IsContentPack).ToArray();

            // unlock content packs
            this.ModRegistry.AreAllModsLoaded = true;

            // log mod info
            this.LogManager.LogModInfo(loaded, loadedContentPacks, loadedMods, skippedMods.ToArray(), this.Settings.ParanoidWarnings);

            // initialize translations
            this.ReloadTranslations(loaded);

            // initialize loaded non-content-pack mods
            foreach (IModMetadata metadata in loadedMods)
            {
                // add interceptors
                if (metadata.Mod.Helper.Content is ContentHelper helper)
                {
                    // ReSharper disable SuspiciousTypeConversion.Global
                    if (metadata.Mod is IAssetEditor editor)
                        this.ContentCore.Editors.Add(new ModLinked<IAssetEditor>(metadata, editor));
                    if (metadata.Mod is IAssetLoader loader)
                        this.ContentCore.Loaders.Add(new ModLinked<IAssetLoader>(metadata, loader));
                    // ReSharper restore SuspiciousTypeConversion.Global

                    helper.ObservableAssetEditors.CollectionChanged += (sender, e) => this.OnAssetInterceptorsChanged(metadata, e.NewItems?.Cast<IAssetEditor>(), e.OldItems?.Cast<IAssetEditor>(), this.ContentCore.Editors);
                    helper.ObservableAssetLoaders.CollectionChanged += (sender, e) => this.OnAssetInterceptorsChanged(metadata, e.NewItems?.Cast<IAssetLoader>(), e.OldItems?.Cast<IAssetLoader>(), this.ContentCore.Loaders);
                }

                // call entry method
                try
                {
                    IMod mod = metadata.Mod;
                    mod.Entry(mod.Helper);
                }
                catch (Exception ex)
                {
                    metadata.LogAsMod($"Mod crashed on entry and might not work correctly. Technical details:\n{ex.GetLogSummary()}", LogLevel.Error);
                }

                // get mod API
                try
                {
                    object api = metadata.Mod.GetApi();
                    if (api != null && !api.GetType().IsPublic)
                    {
                        api = null;
                        this.Monitor.Log($"{metadata.DisplayName} provides an API instance with a non-public type. This isn't currently supported, so the API won't be available to other mods.", LogLevel.Warn);
                    }

                    if (api != null)
                        this.Monitor.Log($"   Found mod-provided API ({api.GetType().FullName}).");
                    metadata.SetApi(api);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Failed loading mod-provided API for {metadata.DisplayName}. Integrations with other mods may not work. Error: {ex.GetLogSummary()}", LogLevel.Error);
                }
            }

            // unlock mod integrations
            this.ModRegistry.AreAllModsInitialized = true;
        }

        /// <summary>Raised after a mod adds or removes asset interceptors.</summary>
        /// <typeparam name="T">The asset interceptor type (one of <see cref="IAssetEditor"/> or <see cref="IAssetLoader"/>).</typeparam>
        /// <param name="mod">The mod metadata.</param>
        /// <param name="added">The interceptors that were added.</param>
        /// <param name="removed">The interceptors that were removed.</param>
        /// <param name="list">A list of interceptors to update for the change.</param>
        private void OnAssetInterceptorsChanged<T>(IModMetadata mod, IEnumerable<T> added, IEnumerable<T> removed, IList<ModLinked<T>> list)
        {
            foreach (T interceptor in added ?? new T[0])
            {
                this.ReloadAssetInterceptorsQueue.Add(new AssetInterceptorChange(mod, interceptor, wasAdded: true));
                list.Add(new ModLinked<T>(mod, interceptor));
            }

            foreach (T interceptor in removed ?? new T[0])
            {
                this.ReloadAssetInterceptorsQueue.Add(new AssetInterceptorChange(mod, interceptor, wasAdded: false));
                foreach (ModLinked<T> entry in list.Where(p => p.Mod == mod && object.ReferenceEquals(p.Data, interceptor)).ToArray())
                    list.Remove(entry);
            }
        }

        /// <summary>Load a given mod.</summary>
        /// <param name="mod">The mod to load.</param>
        /// <param name="mods">The mods being loaded.</param>
        /// <param name="assemblyLoader">Preprocesses and loads mod assemblies.</param>
        /// <param name="proxyFactory">Generates proxy classes to access mod APIs through an arbitrary interface.</param>
        /// <param name="jsonHelper">The JSON helper with which to read mods' JSON files.</param>
        /// <param name="contentCore">The content manager to use for mod content.</param>
        /// <param name="modDatabase">Handles access to SMAPI's internal mod metadata list.</param>
        /// <param name="suppressUpdateChecks">The mod IDs to ignore when validating update keys.</param>
        /// <param name="failReason">The reason the mod couldn't be loaded, if applicable.</param>
        /// <param name="errorReasonPhrase">The user-facing reason phrase explaining why the mod couldn't be loaded (if applicable).</param>
        /// <param name="errorDetails">More detailed details about the error intended for developers (if any).</param>
        /// <returns>Returns whether the mod was successfully loaded.</returns>
        private bool TryLoadMod(IModMetadata mod, IModMetadata[] mods, AssemblyLoader assemblyLoader, InterfaceProxyFactory proxyFactory, JsonHelper jsonHelper, ContentCoordinator contentCore, ModDatabase modDatabase, HashSet<string> suppressUpdateChecks, out ModFailReason? failReason, out string errorReasonPhrase, out string errorDetails)
        {
            errorDetails = null;

            // log entry
            {
                string relativePath = mod.GetRelativePathWithRoot();
                if (mod.IsContentPack)
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath}) [content pack]...");
                else if (mod.Manifest?.EntryDll != null)
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath}{Path.DirectorySeparatorChar}{mod.Manifest.EntryDll})..."); // don't use Path.Combine here, since EntryDLL might not be valid
                else
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath})...");
            }

            // add warning for missing update key
            if (mod.HasID() && !suppressUpdateChecks.Contains(mod.Manifest.UniqueID) && !mod.HasValidUpdateKeys())
                mod.SetWarning(ModWarning.NoUpdateKeys);

            // validate status
            if (mod.Status == ModMetadataStatus.Failed)
            {
                this.Monitor.Log($"      Failed: {mod.ErrorDetails ?? mod.Error}");
                failReason = mod.FailReason;
                errorReasonPhrase = mod.Error;
                return false;
            }

            // validate dependencies
            // Although dependencies are validated before mods are loaded, a dependency may have failed to load.
            foreach (IManifestDependency dependency in mod.Manifest.Dependencies.Where(p => p.IsRequired))
            {
                if (this.ModRegistry.Get(dependency.UniqueID) == null)
                {
                    string dependencyName = mods
                        .FirstOrDefault(otherMod => otherMod.HasID(dependency.UniqueID))
                        ?.DisplayName ?? dependency.UniqueID;
                    errorReasonPhrase = $"it needs the '{dependencyName}' mod, which couldn't be loaded.";
                    failReason = ModFailReason.MissingDependencies;
                    return false;
                }
            }

            // load as content pack
            if (mod.IsContentPack)
            {
                IManifest manifest = mod.Manifest;
                IMonitor monitor = this.LogManager.GetMonitor(mod.DisplayName);
                IContentHelper contentHelper = new ContentHelper(this.ContentCore, mod.DirectoryPath, manifest.UniqueID, mod.DisplayName, monitor);
                TranslationHelper translationHelper = new TranslationHelper(manifest.UniqueID, contentCore.GetLocale(), contentCore.Language);
                IContentPack contentPack = new ContentPack(mod.DirectoryPath, manifest, contentHelper, translationHelper, jsonHelper);
                mod.SetMod(contentPack, monitor, translationHelper);
                this.ModRegistry.Add(mod);

                errorReasonPhrase = null;
                failReason = null;
                return true;
            }

            // load as mod
            else
            {
                IManifest manifest = mod.Manifest;

                // load mod
                string assemblyPath = manifest?.EntryDll != null
                    ? Path.Combine(mod.DirectoryPath, manifest.EntryDll)
                    : null;
                Assembly modAssembly;
                try
                {
                    modAssembly = assemblyLoader.Load(mod, assemblyPath, assumeCompatible: mod.DataRecord?.Status == ModStatus.AssumeCompatible);
                    this.ModRegistry.TrackAssemblies(mod, modAssembly);
                }
                catch (IncompatibleInstructionException) // details already in trace logs
                {
                    string[] updateUrls = new[] { modDatabase.GetModPageUrlFor(manifest.UniqueID), "https://smapi.io/mods" }.Where(p => p != null).ToArray();
                    errorReasonPhrase = $"it's no longer compatible. Please check for a new version at {string.Join(" or ", updateUrls)}";
                    failReason = ModFailReason.Incompatible;
                    return false;
                }
                catch (SAssemblyLoadFailedException ex)
                {
                    errorReasonPhrase = $"its DLL couldn't be loaded: {ex.Message}";
                    failReason = ModFailReason.LoadFailed;
                    return false;
                }
                catch (Exception ex)
                {
                    errorReasonPhrase = "its DLL couldn't be loaded.";
                    errorDetails = $"Error: {ex.GetLogSummary()}";
                    failReason = ModFailReason.LoadFailed;
                    return false;
                }

                // initialize mod
                try
                {
                    // get mod instance
                    if (!this.TryLoadModEntry(modAssembly, out Mod modEntry, out errorReasonPhrase))
                    {
                        failReason = ModFailReason.LoadFailed;
                        return false;
                    }

                    // get content packs
                    IContentPack[] GetContentPacks()
                    {
                        if (!this.ModRegistry.AreAllModsLoaded)
                            throw new InvalidOperationException("Can't access content packs before SMAPI finishes loading mods.");

                        return this.ModRegistry
                            .GetAll(assemblyMods: false)
                            .Where(p => p.IsContentPack && mod.HasID(p.Manifest.ContentPackFor.UniqueID))
                            .Select(p => p.ContentPack)
                            .ToArray();
                    }

                    // init mod helpers
                    IMonitor monitor = this.LogManager.GetMonitor(mod.DisplayName);
                    TranslationHelper translationHelper = new TranslationHelper(manifest.UniqueID, contentCore.GetLocale(), contentCore.Language);
                    IModHelper modHelper;
                    {
                        IContentPack CreateFakeContentPack(string packDirPath, IManifest packManifest)
                        {
                            IMonitor packMonitor = this.LogManager.GetMonitor(packManifest.Name);
                            IContentHelper packContentHelper = new ContentHelper(contentCore, packDirPath, packManifest.UniqueID, packManifest.Name, packMonitor);
                            ITranslationHelper packTranslationHelper = new TranslationHelper(packManifest.UniqueID, contentCore.GetLocale(), contentCore.Language);
                            return new ContentPack(packDirPath, packManifest, packContentHelper, packTranslationHelper, this.Toolkit.JsonHelper);
                        }

                        IModEvents events = new ModEvents(mod, this.EventManager);
                        ICommandHelper commandHelper = new CommandHelper(mod, this.CommandManager);
                        IContentHelper contentHelper = new ContentHelper(contentCore, mod.DirectoryPath, manifest.UniqueID, mod.DisplayName, monitor);
                        IContentPackHelper contentPackHelper = new ContentPackHelper(manifest.UniqueID, new Lazy<IContentPack[]>(GetContentPacks), CreateFakeContentPack);
                        IDataHelper dataHelper = new DataHelper(manifest.UniqueID, mod.DirectoryPath, jsonHelper);
                        IReflectionHelper reflectionHelper = new ReflectionHelper(manifest.UniqueID, mod.DisplayName, this.Reflection);
                        IModRegistry modRegistryHelper = new ModRegistryHelper(manifest.UniqueID, this.ModRegistry, proxyFactory, monitor);
                        IMultiplayerHelper multiplayerHelper = new MultiplayerHelper(manifest.UniqueID, this.Multiplayer);

                        modHelper = new ModHelper(manifest.UniqueID, mod.DirectoryPath, () => this.GetCurrentGameInstance().Input, events, contentHelper, contentPackHelper, commandHelper, dataHelper, modRegistryHelper, reflectionHelper, multiplayerHelper, translationHelper);
                    }

                    // init mod
                    modEntry.ModManifest = manifest;
                    modEntry.Helper = modHelper;
                    modEntry.Monitor = monitor;

                    // track mod
                    mod.SetMod(modEntry, translationHelper);
                    this.ModRegistry.Add(mod);
                    failReason = null;
                    return true;
                }
                catch (Exception ex)
                {
                    errorReasonPhrase = $"initialization failed:\n{ex.GetLogSummary()}";
                    failReason = ModFailReason.LoadFailed;
                    return false;
                }
            }
        }

        /// <summary>Load a mod's entry class.</summary>
        /// <param name="modAssembly">The mod assembly.</param>
        /// <param name="mod">The loaded instance.</param>
        /// <param name="error">The error indicating why loading failed (if applicable).</param>
        /// <returns>Returns whether the mod entry class was successfully loaded.</returns>
        private bool TryLoadModEntry(Assembly modAssembly, out Mod mod, out string error)
        {
            mod = null;

            // find type
            TypeInfo[] modEntries = modAssembly.DefinedTypes.Where(type => typeof(Mod).IsAssignableFrom(type) && !type.IsAbstract).Take(2).ToArray();
            if (modEntries.Length == 0)
            {
                error = $"its DLL has no '{nameof(Mod)}' subclass.";
                return false;
            }
            if (modEntries.Length > 1)
            {
                error = $"its DLL contains multiple '{nameof(Mod)}' subclasses.";
                return false;
            }

            // get implementation
            mod = (Mod)modAssembly.CreateInstance(modEntries[0].ToString());
            if (mod == null)
            {
                error = "its entry class couldn't be instantiated.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>Reload translations for all mods.</summary>
        private void ReloadTranslations()
        {
            this.ReloadTranslations(this.ModRegistry.GetAll());
        }

        /// <summary>Reload translations for the given mods.</summary>
        /// <param name="mods">The mods for which to reload translations.</param>
        private void ReloadTranslations(IEnumerable<IModMetadata> mods)
        {
            // core SMAPI translations
            {
                var translations = this.ReadTranslationFiles(Path.Combine(Constants.InternalFilesPath, "i18n"), out IList<string> errors);
                if (errors.Any() || !translations.Any())
                {
                    this.Monitor.Log("SMAPI couldn't load some core translations. You may need to reinstall SMAPI.", LogLevel.Warn);
                    foreach (string error in errors)
                        this.Monitor.Log($"  - {error}", LogLevel.Warn);
                }
                this.Translator.SetTranslations(translations);
            }

            // mod translations
            foreach (IModMetadata metadata in mods)
            {
                var translations = this.ReadTranslationFiles(Path.Combine(metadata.DirectoryPath, "i18n"), out IList<string> errors);
                if (errors.Any())
                {
                    metadata.LogAsMod("Mod couldn't load some translation files:", LogLevel.Warn);
                    foreach (string error in errors)
                        metadata.LogAsMod($"  - {error}", LogLevel.Warn);
                }
                metadata.Translations.SetTranslations(translations);
            }
        }

        /// <summary>Read translations from a directory containing JSON translation files.</summary>
        /// <param name="folderPath">The folder path to search.</param>
        /// <param name="errors">The errors indicating why translation files couldn't be parsed, indexed by translation filename.</param>
        private IDictionary<string, IDictionary<string, string>> ReadTranslationFiles(string folderPath, out IList<string> errors)
        {
            JsonHelper jsonHelper = this.Toolkit.JsonHelper;

            // read translation files
            var translations = new Dictionary<string, IDictionary<string, string>>();
            errors = new List<string>();
            DirectoryInfo translationsDir = new DirectoryInfo(folderPath);
            if (translationsDir.Exists)
            {
                foreach (FileInfo file in translationsDir.EnumerateFiles("*.json"))
                {
                    string locale = Path.GetFileNameWithoutExtension(file.Name.ToLower().Trim());
                    try
                    {
                        if (!jsonHelper.ReadJsonFileIfExists(file.FullName, out IDictionary<string, string> data))
                        {
                            errors.Add($"{file.Name} file couldn't be read"); // should never happen, since we're iterating files that exist
                            continue;
                        }

                        translations[locale] = data;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{file.Name} file couldn't be parsed: {ex.GetLogSummary()}");
                    }
                }
            }

            // validate translations
            foreach (string locale in translations.Keys.ToArray())
            {
                // handle duplicates
                HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string key in translations[locale].Keys.ToArray())
                {
                    if (!keys.Add(key))
                    {
                        duplicateKeys.Add(key);
                        translations[locale].Remove(key);
                    }
                }
                if (duplicateKeys.Any())
                    errors.Add($"{locale}.json has duplicate translation keys: [{string.Join(", ", duplicateKeys)}]. Keys are case-insensitive.");
            }

            return translations;
        }

        /// <summary>Get the absolute path to the next available log file.</summary>
        private string GetLogPath()
        {
            // default path
            {
                FileInfo defaultFile = new FileInfo(Path.Combine(Constants.LogDir, $"{Constants.LogFilename}.{Constants.LogExtension}"));
                if (!defaultFile.Exists)
                    return defaultFile.FullName;
            }

            // get first disambiguated path
            for (int i = 2; i < int.MaxValue; i++)
            {
                FileInfo file = new FileInfo(Path.Combine(Constants.LogDir, $"{Constants.LogFilename}.player-{i}.{Constants.LogExtension}"));
                if (!file.Exists)
                    return file.FullName;
            }

            // should never happen
            throw new InvalidOperationException("Could not find an available log path.");
        }

        /// <summary>Delete normal (non-crash) log files created by SMAPI.</summary>
        private void PurgeNormalLogs()
        {
            DirectoryInfo logsDir = new DirectoryInfo(Constants.LogDir);
            if (!logsDir.Exists)
                return;

            foreach (FileInfo logFile in logsDir.EnumerateFiles())
            {
                // skip non-SMAPI file
                if (!logFile.Name.StartsWith(Constants.LogNamePrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                // skip crash log
                if (logFile.FullName == Constants.FatalCrashLog)
                    continue;

                // delete file
                try
                {
                    FileUtilities.ForceDelete(logFile);
                }
                catch (IOException)
                {
                    // ignore file if it's in use
                }
            }
        }

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        /// <param name="message">The fatal log message.</param>
        private void ExitGameImmediately(string message)
        {
            this.Monitor.LogFatal(message);
            this.CancellationToken.Cancel();
        }

        /// <summary>Get the screen ID that should be logged to distinguish between players in split-screen mode, if any.</summary>
        private int? GetScreenIdForLog()
        {
            // @todo
            return 0;
        }
    }
}
