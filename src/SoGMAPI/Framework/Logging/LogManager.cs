using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SoGModdingAPI.Framework.Commands;
using SoGModdingAPI.Framework.Models;
using SoGModdingAPI.Framework.ModLoading;
using SoGModdingAPI.Internal;
using SoGModdingAPI.Internal.ConsoleWriting;
using SoGModdingAPI.Toolkit.Framework.ModData;
using SoGModdingAPI.Toolkit.Utilities;

namespace SoGModdingAPI.Framework.Logging
{
    /// <summary>Manages the SoGMAPI console window and log file.</summary>
    internal class LogManager : IDisposable
    {
        /*********
        ** Fields
        *********/
        /// <summary>The log file to which to write messages.</summary>
        private readonly LogFileManager LogFile;

        /// <summary>The text writer which intercepts console output.</summary>
        private readonly InterceptingTextWriter ConsoleInterceptor;

        /// <summary>Prefixing a low-level message with this character indicates that the console interceptor should write the string without intercepting it. (The character itself is not written.)</summary>
        private const char IgnoreChar = InterceptingTextWriter.IgnoreChar;

        /// <summary>Create a monitor instance given the ID and name.</summary>
        private readonly Func<string, string, Monitor> GetMonitorImpl;

        /// <summary>Regex patterns which match console non-error messages to suppress from the console and log.</summary>
        private readonly Regex[] SuppressConsolePatterns =
        {
            new(@"^TextBox\.Selected is now '(?:True|False)'\.$", RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new(@"^loadPreferences\(\); begin", RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new(@"^savePreferences\(\); async=", RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new(@"^DebugOutput:\s+(?:added cricket|dismount tile|Ping|playerPos)", RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new(@"^Ignoring keys: ", RegexOptions.Compiled | RegexOptions.CultureInvariant)
        };

        /// <summary>Regex patterns which match console messages to show a more friendly error for.</summary>
        private readonly ReplaceLogPattern[] ReplaceConsolePatterns =
        {
            // Steam not loaded
            new(
                search: new Regex(@"^System\.InvalidOperationException: Steamworks is not initialized\.[\s\S]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                replacement:
#if true
                    "Oops! Steam achievements won't work because Steam isn't loaded. See 'Configure your game client' in the install guide for more info: https://smapi.io/install.",
#else
                    "Oops! Steam achievements won't work because Steam isn't loaded. You can launch the game through Steam to fix that.",
#endif
                logLevel: LogLevel.Error
            ),

            // save file not found error
            new(
                search: new Regex(@"^System\.IO\.FileNotFoundException: [^\n]+\n[^:]+: '[^\n]+[/\\]Saves[/\\]([^'\r\n]+)[/\\]([^'\r\n]+)'[\s\S]+LoadGameMenu\.FindSaveGames[\s\S]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                replacement: "The game can't find the '$2' file for your '$1' save. See https://stardewvalleywiki.com/Saves#Troubleshooting for help.",
                logLevel: LogLevel.Error
            )
        };


        /*********
        ** Accessors
        *********/
        /// <summary>The core logger and monitor for SoGMAPI.</summary>
        public Monitor Monitor { get; }

        /// <summary>The core logger and monitor on behalf of the game.</summary>
        public Monitor MonitorForGame { get; }


        /*********
        ** Public methods
        *********/
        /****
        ** Initialization
        ****/
        /// <summary>Construct an instance.</summary>
        /// <param name="logPath">The log file path to write.</param>
        /// <param name="colorConfig">The colors to use for text written to the SoGMAPI console.</param>
        /// <param name="writeToConsole">Whether to output log messages to the console.</param>
        /// <param name="verboseLogging">The log contexts for which to enable verbose logging, which may show a lot more information to simplify troubleshooting.</param>
        /// <param name="isDeveloperMode">Whether to enable full console output for developers.</param>
        /// <param name="getScreenIdForLog">Get the screen ID that should be logged to distinguish between players in split-screen mode, if any.</param>
        public LogManager(string logPath, ColorSchemeConfig colorConfig, bool writeToConsole, HashSet<string> verboseLogging, bool isDeveloperMode, Func<int?> getScreenIdForLog)
        {
            // init log file
            this.LogFile = new LogFileManager(logPath);

            // init monitor
            this.GetMonitorImpl = (id, name) => new Monitor(name, LogManager.IgnoreChar, this.LogFile, colorConfig, verboseLogging.Contains("*") || verboseLogging.Contains(id), getScreenIdForLog)
            {
                WriteToConsole = writeToConsole,
                ShowTraceInConsole = isDeveloperMode,
                ShowFullStampInConsole = isDeveloperMode
            };
            this.Monitor = this.GetMonitor("SoGMAPI", "SoGMAPI");
            this.MonitorForGame = this.GetMonitor("game", "game");

            // redirect direct console output
            this.ConsoleInterceptor = new InterceptingTextWriter(
                output: Console.Out,
                onMessageIntercepted: writeToConsole
                    ? message => this.HandleConsoleMessage(this.MonitorForGame, message)
                    : _ => { }
            );
            Console.SetOut(this.ConsoleInterceptor);

            // enable Unicode handling on Windows
            // (the terminal defaults to UTF-8 on Linux/macOS)
#if true
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;
#endif
        }

        /// <summary>Get a monitor instance derived from SoGMAPI's current settings.</summary>
        /// <param name="id">The unique ID for the mod context.</param>
        /// <param name="name">The name of the module which will log messages with this instance.</param>
        public Monitor GetMonitor(string id, string name)
        {
            return this.GetMonitorImpl(id, name);
        }

        /// <summary>Set the title of the SoGMAPI console window.</summary>
        /// <param name="title">The new window title.</param>
        public void SetConsoleTitle(string title)
        {
            Console.Title = title;
        }

        /****
        ** Console input
        ****/
        /// <summary>Run a loop handling console input.</summary>
        [SuppressMessage("ReSharper", "FunctionNeverReturns", Justification = "The thread is aborted when the game exits.")]
        public void RunConsoleInputLoop(CommandManager commandManager, Action reloadTranslations, Action<string> handleInput, Func<bool> continueWhile)
        {
            // prepare console
            this.Monitor.Log("Type 'help' for help, or 'help <cmd>' for a command's usage", LogLevel.Info);
            commandManager
                .Add(new HelpCommand(commandManager), this.Monitor)
                .Add(new HarmonySummaryCommand(), this.Monitor)
                .Add(new ReloadI18nCommand(reloadTranslations), this.Monitor);

            // start handling command line input
            Thread inputThread = new(() =>
            {
                while (true)
                {
                    // get input
                    string? input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    // handle command
                    this.Monitor.LogUserInput(input);
                    handleInput(input);
                }
            });
            inputThread.Start();

            // keep console thread alive while the game is running
            while (continueWhile())
                Thread.Sleep(1000 / 10);
        }

        /// <summary>Show a 'press any key to exit' message, and exit when they press a key.</summary>
        public void PressAnyKeyToExit()
        {
            this.Monitor.Log("Game has ended. Press any key to exit.", LogLevel.Info);
            this.PressAnyKeyToExit(showMessage: false);
        }

        /// <summary>Show a 'press any key to exit' message, and exit when they press a key.</summary>
        /// <param name="showMessage">Whether to print a 'press any key to exit' message to the console.</param>
        public void PressAnyKeyToExit(bool showMessage)
        {
            if (showMessage)
                this.Monitor.Log("Game has ended. Press any key to exit.");
            Thread.Sleep(100);
            Console.ReadKey();
            Environment.Exit(0);
        }

        /****
        ** Crash/update handling
        ****/
        /// <summary>Create a crash marker and duplicate the log into the crash log.</summary>
        public void WriteCrashLog()
        {
            try
            {
                File.WriteAllText(Constants.FatalCrashMarker, string.Empty);
                File.Copy(this.LogFile.Path, Constants.FatalCrashLog, overwrite: true);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"SoGMAPI failed trying to track the crash details: {ex.GetLogSummary()}", LogLevel.Error);
            }
        }

        /// <summary>Write an update alert marker file.</summary>
        /// <param name="version">The new version found.</param>
        /// <param name="url">The download URL for the update.</param>
        public void WriteUpdateMarker(string version, string url)
        {
            File.WriteAllText(Constants.UpdateMarker, $"{version}|{url}");
        }

        /// <summary>Check whether SoGMAPI crashed or detected an update during the last session, and display them in the SoGMAPI console.</summary>
        public void HandleMarkerFiles()
        {
            // show update alert
            if (File.Exists(Constants.UpdateMarker))
            {
                string[] rawUpdateFound = File.ReadAllText(Constants.UpdateMarker).Split('|', 2);
                if (SemanticVersion.TryParse(rawUpdateFound[0], out ISemanticVersion? updateFound))
                {
                    if (Constants.ApiVersion.IsPrerelease() && updateFound.IsNewerThan(Constants.ApiVersion))
                    {
                        string url = rawUpdateFound.Length > 1
                            ? rawUpdateFound[1]
                            : Constants.HomePageUrl;

                        this.Monitor.Log("A new version of SoGMAPI was detected last time you played.", LogLevel.Error);
                        this.Monitor.Log($"You can update to {updateFound}: {url}.", LogLevel.Error);
                        this.Monitor.Log("Press any key to continue playing anyway. (This only appears when using a SoGMAPI beta.)", LogLevel.Info);
                        Console.ReadKey();
                    }
                }
                File.Delete(Constants.UpdateMarker);
            }

            // show details if game crashed during last session
            if (File.Exists(Constants.FatalCrashMarker))
            {
                this.Monitor.Log("The game crashed last time you played. If it happens repeatedly, see 'get help' on https://smapi.io.", LogLevel.Error);
                this.Monitor.Log("If you ask for help, make sure to share your SoGMAPI log: https://smapi.io/log.", LogLevel.Error);
                this.Monitor.Log("Press any key to delete the crash data and continue playing.", LogLevel.Info);
                Console.ReadKey();
                File.Delete(Constants.FatalCrashLog);
                File.Delete(Constants.FatalCrashMarker);
            }
        }

        /// <summary>Log a fatal exception which prevents SoGMAPI from launching.</summary>
        /// <param name="exception">The exception details.</param>
        public void LogFatalLaunchError(Exception exception)
        {
            this.MonitorForGame.Log($"The game failed to launch: {exception.GetLogSummary()}", LogLevel.Error);
        }

        /****
        ** General log output
        ****/
        /// <summary>Log the initial header with general SoGMAPI and system details.</summary>
        /// <param name="modsPath">The path from which mods will be loaded.</param>
        /// <param name="customSettings">The custom SoGMAPI settings.</param>
        public void LogIntro(string modsPath, IDictionary<string, object?> customSettings)
        {
            // log platform
            this.Monitor.Log($"SoGMAPI {Constants.ApiVersion} "
#if !SOGMAPI_DEPRECATED
                + "(strict mode) "
#endif
                + $"with Stardew Valley {Constants.GameVersion} (build {Constants.GetBuildVersionLabel()}) on {EnvironmentUtility.GetFriendlyPlatformName(Constants.Platform)}", LogLevel.Info);

            // log basic info
            this.Monitor.Log($"Mods go here: {modsPath}", LogLevel.Info);
            if (modsPath != Constants.DefaultModsPath)
                this.Monitor.Log("(Using custom --mods-path argument.)");
            this.Monitor.Log($"Log started at {DateTime.UtcNow:s} UTC");

            // log custom settings
            if (customSettings.Any())
                this.Monitor.Log($"Loaded with custom settings: {string.Join(", ", customSettings.OrderBy(p => p.Key).Select(p => $"{p.Key}: {p.Value}"))}");

#if !SOGMAPI_DEPRECATED
            this.Monitor.Log("SoGMAPI is running in 'strict mode', which removes all deprecated APIs. This can significantly improve performance, but some mods may not work. You can reinstall SoGMAPI to disable it if you run into problems.", LogLevel.Info);
#endif
        }

        /// <summary>Log details for settings that don't match the default.</summary>
        /// <param name="settings">The settings to log.</param>
        public void LogSettingsHeader(SConfig settings)
        {
            // developer mode
            if (settings.DeveloperMode)
                this.Monitor.Log("You enabled developer mode, so the console will be much more verbose. You can disable it by installing the non-developer version of SoGMAPI.", LogLevel.Info);

            // warnings
            if (!settings.CheckForUpdates)
                this.Monitor.Log("You disabled update checks, so you won't be notified of new SoGMAPI or mod updates. Running an old version of SoGMAPI is not recommended. You can undo this by reinstalling SoGMAPI.", LogLevel.Warn);
            if (!settings.RewriteMods)
                this.Monitor.Log("You disabled rewriting broken mods, so many older mods may fail to load. You can undo this by reinstalling SoGMAPI.", LogLevel.Info);
            if (!this.Monitor.WriteToConsole)
                this.Monitor.Log("Writing to the terminal is disabled because the --no-terminal argument was received. This usually means launching the terminal failed.", LogLevel.Warn);

            // verbose logging
            this.Monitor.VerboseLog("Verbose logging enabled.");
        }

        /// <summary>Log info about loaded mods.</summary>
        /// <param name="loaded">The full list of loaded content packs and mods.</param>
        /// <param name="loadedContentPacks">The loaded content packs.</param>
        /// <param name="loadedMods">The loaded mods.</param>
        /// <param name="skippedMods">The mods which could not be loaded.</param>
        /// <param name="logParanoidWarnings">Whether to log issues for mods which directly use potentially sensitive .NET APIs like file or shell access.</param>
        public void LogModInfo(IModMetadata[] loaded, IModMetadata[] loadedContentPacks, IModMetadata[] loadedMods, IModMetadata[] skippedMods, bool logParanoidWarnings)
        {
            // log loaded mods
            this.Monitor.Log($"Loaded {loadedMods.Length} mods" + (loadedMods.Length > 0 ? ":" : "."), LogLevel.Info);
            foreach (IModMetadata metadata in loadedMods.OrderBy(p => p.DisplayName))
            {
                IManifest manifest = metadata.Manifest;
                this.Monitor.Log(
                    $"   {metadata.DisplayName} {manifest.Version}"
                    + (!string.IsNullOrWhiteSpace(manifest.Author) ? $" by {manifest.Author}" : "")
                    + (!string.IsNullOrWhiteSpace(manifest.Description) ? $" | {manifest.Description}" : ""),
                    LogLevel.Info
                );
            }

            this.Monitor.Newline();

            // log loaded content packs
            if (loadedContentPacks.Any())
            {
                string? GetModDisplayName(string id) => loadedMods.FirstOrDefault(p => p.HasID(id))?.DisplayName;

                this.Monitor.Log($"Loaded {loadedContentPacks.Length} content packs:", LogLevel.Info);
                foreach (IModMetadata metadata in loadedContentPacks.OrderBy(p => p.DisplayName))
                {
                    IManifest manifest = metadata.Manifest;
                    this.Monitor.Log(
                        $"   {metadata.DisplayName} {manifest.Version}"
                        + (!string.IsNullOrWhiteSpace(manifest.Author) ? $" by {manifest.Author}" : "")
                        + $" | for {GetModDisplayName(metadata.Manifest.ContentPackFor!.UniqueID)}"
                        + (!string.IsNullOrWhiteSpace(manifest.Description) ? $" | {manifest.Description}" : ""),
                        LogLevel.Info
                    );
                }

                this.Monitor.Newline();
            }

            // log mod warnings
            this.LogModWarnings(loaded, skippedMods, logParanoidWarnings);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LogFile.Dispose();
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Redirect messages logged directly to the console to the given monitor.</summary>
        /// <param name="gameMonitor">The monitor with which to log messages as the game.</param>
        /// <param name="message">The message to log.</param>
        private void HandleConsoleMessage(IMonitor gameMonitor, string message)
        {
            // detect exception
            LogLevel level = message.Contains("Exception") ? LogLevel.Error : LogLevel.Trace;

            // ignore suppressed message
            if (level != LogLevel.Error && this.SuppressConsolePatterns.Any(p => p.IsMatch(message)))
            {
                this.ConsoleInterceptor.IgnoreNextIfNewline = true;
                return;
            }

            // show friendly error if applicable
            foreach (ReplaceLogPattern entry in this.ReplaceConsolePatterns)
            {
                string newMessage = entry.Search.Replace(message, entry.Replacement);
                if (message != newMessage)
                {
                    gameMonitor.Log(newMessage, entry.LogLevel);
                    gameMonitor.Log(message);
                    return;
                }
            }

            // simplify exception messages
            if (level == LogLevel.Error)
                message = ExceptionHelper.SimplifyExtensionMessage(message);

            // forward to monitor
            gameMonitor.Log(message, level);
            this.ConsoleInterceptor.IgnoreNextIfNewline = true;
        }

        /// <summary>Write a summary of mod warnings to the console and log.</summary>
        /// <param name="mods">The loaded mods.</param>
        /// <param name="skippedMods">The mods which could not be loaded.</param>
        /// <param name="logParanoidWarnings">Whether to log issues for mods which directly use potentially sensitive .NET APIs like file or shell access.</param>
        [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "Manifests aren't guaranteed non-null at this point in the loading process.")]
        private void LogModWarnings(IEnumerable<IModMetadata> mods, IModMetadata[] skippedMods, bool logParanoidWarnings)
        {
            // get mods with warnings
            IModMetadata[] modsWithWarnings = mods.Where(p => p.Warnings != ModWarning.None).ToArray();
            if (!modsWithWarnings.Any() && !skippedMods.Any())
                return;

            // log intro
            {
                int count = modsWithWarnings.Length + skippedMods.Length;
                this.Monitor.Log($"Found {count} mod{(count == 1 ? "" : "s")} with warnings:", LogLevel.Info);
            }

            // log skipped mods
            if (skippedMods.Any())
            {
                var loggedDuplicateIds = new HashSet<string>();

                this.Monitor.Log("   Skipped mods", LogLevel.Error);
                this.Monitor.Log("   " + "".PadRight(50, '-'), LogLevel.Error);
                this.Monitor.Log("      These mods could not be added to your game.", LogLevel.Error);
                this.Monitor.Newline();
                foreach (var list in this.GroupFailedModsByPriority(skippedMods))
                {
                    if (list.Any())
                    {
                        foreach (IModMetadata mod in list.OrderBy(p => p.DisplayName))
                        {
                            string message = $"      - {mod.DisplayName}{(" " + mod.Manifest?.Version?.ToString()).TrimEnd()} because {mod.Error}";

                            // duplicate mod: log first one only, don't show redundant version
                            if (mod.FailReason == ModFailReason.Duplicate && mod.HasManifest())
                            {
                                if (loggedDuplicateIds.Add(mod.Manifest!.UniqueID))
                                    continue; // already logged

                                message = $"      - {mod.DisplayName} because {mod.Error}";
                            }

                            // log message
                            this.Monitor.Log(message, LogLevel.Error);
                            if (mod.ErrorDetails != null)
                                this.Monitor.Log($"        ({mod.ErrorDetails})");
                        }

                        this.Monitor.Newline();
                    }
                }
            }

            // log warnings
            if (modsWithWarnings.Any())
            {
                // broken code
                this.LogModWarningGroup(modsWithWarnings, ModWarning.BrokenCodeLoaded, LogLevel.Error, "Broken mods",
                    "These mods have broken code, but you configured SoGMAPI to load them anyway. This may cause bugs,",
                    "errors, or crashes in-game."
                );

                // changes serializer
                this.LogModWarningGroup(modsWithWarnings, ModWarning.ChangesSaveSerializer, LogLevel.Warn, "Changed save serializer",
                    "These mods change the save serializer. They may corrupt your save files, or make them unusable if",
                    "you uninstall these mods."
                );

                // patched game code
                this.LogModWarningGroup(modsWithWarnings, ModWarning.PatchesGame, LogLevel.Info, "Patched game code",
                    "These mods directly change the game code. They're more likely to cause errors or bugs in-game; if",
                    "your game has issues, try removing these first. Otherwise you can ignore this warning."
                );

                // unvalidated update tick
                this.LogModWarningGroup(modsWithWarnings, ModWarning.UsesUnvalidatedUpdateTick, LogLevel.Info, "Bypassed safety checks",
                    "These mods bypass SoGMAPI's normal safety checks, so they're more likely to cause errors or save",
                    "corruption. If your game has issues, try removing these first."
                );

                // paranoid warnings
                if (logParanoidWarnings)
                {
                    this.LogModWarningGroup(
                        modsWithWarnings,
                        match: mod => mod.HasWarnings(ModWarning.AccessesConsole, ModWarning.AccessesFilesystem, ModWarning.AccessesShell),
                        level: LogLevel.Debug,
                        heading: "Direct system access",
                        blurb: new[]
                        {
                            "You enabled paranoid warnings and these mods directly access the filesystem, shells/processes, or",
                            "SoGMAPI console. (This is usually legitimate and innocent usage; this warning is only useful for",
                            "further investigation.)"
                        },
                        modLabel: mod =>
                        {
                            List<string> labels = new List<string>();
                            if (mod.HasWarnings(ModWarning.AccessesConsole))
                                labels.Add("console");
                            if (mod.HasWarnings(ModWarning.AccessesFilesystem))
                                labels.Add("files");
                            if (mod.HasWarnings(ModWarning.AccessesShell))
                                labels.Add("shells/processes");

                            return $"{mod.DisplayName} ({string.Join(", ", labels)})";
                        }
                    );
                }

                // no update keys
                this.LogModWarningGroup(modsWithWarnings, ModWarning.NoUpdateKeys, LogLevel.Debug, "No update keys",
                    "These mods have no update keys in their manifest. SoGMAPI may not notify you about updates for these",
                    "mods. Consider notifying the mod authors about this problem."
                );
            }
        }

        /// <summary>Group failed mods by the priority players should update them, where mods in earlier groups are more likely to fix multiple mods.</summary>
        /// <param name="failedMods">The failed mods to group.</param>
        private IEnumerable<IList<IModMetadata>> GroupFailedModsByPriority(IList<IModMetadata> failedMods)
        {
            var failedOthers = failedMods.ToList();
            var skippedModIds = new HashSet<string>(from mod in failedMods where mod.HasID() select mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);

            // group B: dependencies which failed
            var failedOtherDependencies = new List<IModMetadata>();
            {
                // get failed dependency IDs
                var skippedDependencyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (IModMetadata mod in failedMods)
                {
                    foreach (string requiredId in skippedModIds.Intersect(mod.GetRequiredModIds()))
                        skippedDependencyIds.Add(requiredId);
                }

                // group matching mods
                this.FilterThrough(
                    fromList: failedOthers,
                    toList: failedOtherDependencies,
                    match: mod => mod.HasID() && skippedDependencyIds.Contains(mod.Manifest.UniqueID)
                );
            }

            // group A: failed root dependencies which other dependencies need
            var failedRootDependencies = new List<IModMetadata>();
            {
                var skippedDependencyIds = new HashSet<string>(failedOtherDependencies.Select(p => p.Manifest.UniqueID));
                this.FilterThrough(
                    fromList: failedOtherDependencies,
                    toList: failedRootDependencies,
                    match: mod =>
                    {
                        // has no failed dependency
                        foreach (string requiredId in mod.GetRequiredModIds())
                        {
                            if (skippedDependencyIds.Contains(requiredId))
                                return false;
                        }

                        // another dependency depends on this mod
                        bool isDependedOn = false;
                        foreach (IModMetadata other in failedOtherDependencies)
                        {
                            if (other.HasRequiredModId(mod.Manifest.UniqueID, includeOptional: false))
                            {
                                isDependedOn = true;
                                break;
                            }
                        }

                        return isDependedOn;
                    }
                );
            }

            // return groups
            return new[]
            {
                failedRootDependencies,
                failedOtherDependencies,
                failedOthers
            };
        }

        /// <summary>Filter matching items from one list and add them to the other.</summary>
        /// <typeparam name="TItem">The list item type.</typeparam>
        /// <param name="fromList">The list to filter.</param>
        /// <param name="toList">The list to which to add filtered items.</param>
        /// <param name="match">Matches items to filter through.</param>
        private void FilterThrough<TItem>(IList<TItem> fromList, IList<TItem> toList, Func<TItem, bool> match)
        {
            for (int i = 0; i < fromList.Count; i++)
            {
                TItem item = fromList[i];
                if (match(item))
                {
                    toList.Add(item);
                    fromList.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>Write a mod warning group to the console and log.</summary>
        /// <param name="mods">The mods to search.</param>
        /// <param name="match">Matches mods to include in the warning group.</param>
        /// <param name="level">The log level for the logged messages.</param>
        /// <param name="heading">A brief heading label for the group.</param>
        /// <param name="blurb">A detailed explanation of the warning, split into lines.</param>
        /// <param name="modLabel">Formats the mod label, or <c>null</c> to use the <see cref="IModMetadata.DisplayName"/>.</param>
        private void LogModWarningGroup(IModMetadata[] mods, Func<IModMetadata, bool> match, LogLevel level, string heading, string[] blurb, Func<IModMetadata, string>? modLabel = null)
        {
            // get matching mods
            string[] modLabels = mods
                .Where(match)
                .Select(mod => modLabel?.Invoke(mod) ?? mod.DisplayName)
                .OrderBy(p => p)
                .ToArray();
            if (!modLabels.Any())
                return;

            // log header/blurb
            this.Monitor.Log("   " + heading, level);
            this.Monitor.Log("   " + "".PadRight(50, '-'), level);
            foreach (string line in blurb)
                this.Monitor.Log("      " + line, level);
            this.Monitor.Newline();

            // log mod list
            foreach (string label in modLabels)
                this.Monitor.Log($"      - {label}", level);

            this.Monitor.Newline();
        }

        /// <summary>Write a mod warning group to the console and log.</summary>
        /// <param name="mods">The mods to search.</param>
        /// <param name="warning">The mod warning to match.</param>
        /// <param name="level">The log level for the logged messages.</param>
        /// <param name="heading">A brief heading label for the group.</param>
        /// <param name="blurb">A detailed explanation of the warning, split into lines.</param>
        private void LogModWarningGroup(IModMetadata[] mods, ModWarning warning, LogLevel level, string heading, params string[] blurb)
        {
            this.LogModWarningGroup(mods, mod => mod.HasWarnings(warning), level, heading, blurb);
        }


        /*********
        ** Protected types
        *********/
        /// <summary>A console log pattern to replace with a different message.</summary>
        private class ReplaceLogPattern
        {
            /*********
            ** Accessors
            *********/
            /// <summary>The regex pattern matching the portion of the message to replace.</summary>
            public Regex Search { get; }

            /// <summary>The replacement string.</summary>
            public string Replacement { get; }

            /// <summary>The log level for the new message.</summary>
            public LogLevel LogLevel { get; }


            /*********
            ** Public methods
            *********/
            /// <summary>Construct an instance.</summary>
            /// <param name="search">The regex pattern matching the portion of the message to replace.</param>
            /// <param name="replacement">The replacement string.</param>
            /// <param name="logLevel">The log level for the new message.</param>
            public ReplaceLogPattern(Regex search, string replacement, LogLevel logLevel)
            {
                this.Search = search;
                this.Replacement = replacement;
                this.LogLevel = logLevel;
            }
        }
    }
}
