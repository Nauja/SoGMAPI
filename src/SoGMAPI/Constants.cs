using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using SoGModdingAPI.Enums;
using SoGModdingAPI.Framework;
#if SOGMAPI_DEPRECATED
using SoGModdingAPI.Framework.Deprecations;
#endif
using SoGModdingAPI.Framework.ModLoading;
using SoGModdingAPI.Toolkit.Framework;
using SoGModdingAPI.Toolkit.Utilities;
using SoG;

namespace SoGModdingAPI
{
    /// <summary>Contains constants that are accessed before the game itself has been loaded.</summary>
    /// <remarks>Most code should use <see cref="Constants"/> instead of this class directly.</remarks>
    internal static class EarlyConstants
    {
        //
        // Note: this class *must not* depend on any external DLL beyond .NET Framework itself.
        // That includes the game or SoGMAPI toolkit, since it's accessed before those are loaded.
        //
        // Adding an external dependency may seem to work in some cases, but will prevent SoGMAPI
        // from showing a human-readable error if the game isn't available. To test this, just
        // rename "Secrets Of Grindea.exe" in the game folder; you should see an error like "Oops!
        // SoGMAPI can't find the game", not a technical exception.
        //

        /*********
        ** Accessors
        *********/
        /// <summary>The path to the game folder.</summary>
        public static string GamePath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        /// <summary>The absolute path to the folder containing SoGMAPI's internal files.</summary>
        public static readonly string InternalFilesPath = Path.Combine(EarlyConstants.GamePath, "sogmapi-internal");

        /// <summary>The target game platform.</summary>
        internal static GamePlatform Platform { get; } = (GamePlatform)Enum.Parse(typeof(GamePlatform), LowLevelEnvironmentUtility.DetectPlatform());

        /// <summary>The game framework running the game.</summary>
        internal static GameFramework GameFramework { get; } = GameFramework.MonoGame;

        /// <summary>The game's assembly name.</summary>
        internal static string GameAssemblyName { get; } = "SoG";

        /// <summary>The <see cref="Context.ScreenId"/> value which should appear in the SoGMAPI log, if any.</summary>
        internal static int? LogScreenId { get; set; }

        /// <summary>SoGMAPI's current raw semantic version.</summary>
        internal static string RawApiVersion = "3.18.2";
    }

    /// <summary>Contains SoGMAPI's constants and assumptions.</summary>
    public static class Constants
    {
        /*********
        ** Accessors
        *********/
        /****
        ** Public
        ****/
        /// <summary>SoGMAPI's current semantic version.</summary>
        public static ISemanticVersion ApiVersion { get; } = new Toolkit.SemanticVersion(EarlyConstants.RawApiVersion);

        /// <summary>The minimum supported version of Stardew Valley.</summary>
        public static ISemanticVersion MinimumGameVersion { get; } = new GameVersion("1.5.6");

        /// <summary>The maximum supported version of Stardew Valley, if any.</summary>
        public static ISemanticVersion? MaximumGameVersion { get; } = new GameVersion("1.5.6");

        /// <summary>The target game platform.</summary>
        public static GamePlatform TargetPlatform { get; } = EarlyConstants.Platform;

        /// <summary>The game framework running the game.</summary>
        public static GameFramework GameFramework { get; } = EarlyConstants.GameFramework;

#if SOGMAPI_DEPRECATED
        /// <summary>The path to the game folder.</summary>
        [Obsolete($"Use {nameof(Constants)}.{nameof(GamePath)} instead. This property will be removed in SoGMAPI 4.0.0.")]
        public static string ExecutionPath
        {
            get
            {
                SCore.DeprecationManager.Warn(
                    source: null,
                    nounPhrase: $"{nameof(Constants)}.{nameof(Constants.ExecutionPath)}",
                    version: "3.14.0",
                    severity: DeprecationLevel.PendingRemoval
                );

                return Constants.GamePath;
            }
        }
#endif

        /// <summary>The path to the game folder.</summary>
        public static string GamePath { get; } = EarlyConstants.GamePath;

        /// <summary>The path to the game's <c>Content</c> folder.</summary>
        public static string ContentPath { get; } = Constants.GetContentFolderPath();

        /// <summary>The directory path containing Stardew Valley's app data.</summary>
        public static string DataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");

        /// <summary>The directory path in which error logs should be stored.</summary>
        public static string LogDir { get; } = Path.Combine(Constants.DataPath, "ErrorLogs");

        /// <summary>The directory path where all saves are stored.</summary>
        public static string SavesPath { get; } = Path.Combine(Constants.DataPath, "Saves");

        /// <summary>The name of the current save folder (if save info is available, regardless of whether the save file exists yet).</summary>
        public static string? SaveFolderName => Constants.GetSaveFolderName();

        /// <summary>The absolute path to the current save folder (if save info is available and the save file exists).</summary>
        public static string? CurrentSavePath => Constants.GetSaveFolderPathIfExists();

        /****
        ** Internal
        ****/
        /// <summary>Whether SoGMAPI was compiled in debug mode.</summary>
        internal const bool IsDebugBuild =
#if DEBUG
            true;
#else
            false;
#endif

        /// <summary>The URL of the SoGMAPI home page.</summary>
        internal const string HomePageUrl = "https://smapi.io";

        /// <summary>The absolute path to the folder containing SoGMAPI's internal files.</summary>
        internal static readonly string InternalFilesPath = EarlyConstants.InternalFilesPath;

        /// <summary>The file path for the SoGMAPI configuration file.</summary>
        internal static string ApiConfigPath => Path.Combine(Constants.InternalFilesPath, "config.json");

        /// <summary>The file path for the overrides file for <see cref="ApiConfigPath"/>, which is applied over it.</summary>
        internal static string ApiUserConfigPath => Path.Combine(Constants.InternalFilesPath, "config.user.json");

        /// <summary>The file path for the SoGMAPI metadata file.</summary>
        internal static string ApiMetadataPath => Path.Combine(Constants.InternalFilesPath, "metadata.json");

        /// <summary>The filename prefix used for all SoGMAPI logs.</summary>
        internal static string LogNamePrefix { get; } = "SoGMAPI-";

        /// <summary>The filename for SoGMAPI's main log, excluding the <see cref="LogExtension"/>.</summary>
        internal static string LogFilename { get; } = $"{Constants.LogNamePrefix}latest";

        /// <summary>The filename extension for SoGMAPI log files.</summary>
        internal static string LogExtension { get; } = "txt";

        /// <summary>The file path for the log containing the previous fatal crash, if any.</summary>
        internal static string FatalCrashLog => Path.Combine(Constants.LogDir, "SoGMAPI-crash.txt");

        /// <summary>The file path which stores a fatal crash message for the next run.</summary>
        internal static string FatalCrashMarker => Path.Combine(Constants.InternalFilesPath, "SoGModdingAPI.crash.marker");

        /// <summary>The file path which stores the detected update version for the next run.</summary>
        internal static string UpdateMarker => Path.Combine(Constants.InternalFilesPath, "SoGModdingAPI.update.marker");

        /// <summary>The default full path to search for mods.</summary>
        internal static string DefaultModsPath { get; } = Path.Combine(Constants.GamePath, "Mods");

        /// <summary>The actual full path to search for mods.</summary>
        internal static string ModsPath { get; set; } = null!; // initialized early during SoGMAPI startup

        /// <summary>The game's current semantic version.</summary>
        internal static ISemanticVersion GameVersion { get; } = new GameVersion(Game1.version);

        /// <summary>The target game platform as a SoGMAPI toolkit constant.</summary>
        internal static Platform Platform { get; } = (Platform)Constants.TargetPlatform;


        /*********
        ** Internal methods
        *********/
        /// <summary>Get the SoGMAPI version to recommend for an older game version, if any.</summary>
        /// <param name="version">The game version to search.</param>
        /// <returns>Returns the compatible SoGMAPI version, or <c>null</c> if none was found.</returns>
        internal static ISemanticVersion? GetCompatibleApiVersion(ISemanticVersion version)
        {
            // This covers all officially supported public game updates. It might seem like version
            // ranges would be better, but the given SoGMAPI versions may not be compatible with
            // intermediate unlisted versions (e.g. private beta updates).
            // 
            // Nonstandard versions are normalized by GameVersion (e.g. 1.07 => 1.0.7).
            switch (version.ToString())
            {
                case "1.4.1":
                case "1.4.0":
                    return new SemanticVersion("3.0.1");

                case "1.3.36":
                    return new SemanticVersion("2.11.2");

                case "1.3.33":
                case "1.3.32":
                    return new SemanticVersion("2.10.2");

                case "1.3.28":
                    return new SemanticVersion("2.7.0");

                case "1.2.33":
                case "1.2.32":
                case "1.2.31":
                case "1.2.30":
                    return new SemanticVersion("2.5.5");

                case "1.2.29":
                case "1.2.28":
                case "1.2.27":
                case "1.2.26":
                    return new SemanticVersion("1.13.1");

                case "1.1.1":
                case "1.1.0":
                    return new SemanticVersion("1.9.0");

                case "1.0.7.1":
                case "1.0.7":
                case "1.0.6":
                case "1.0.5.2":
                case "1.0.5.1":
                case "1.0.5":
                case "1.0.4":
                case "1.0.3":
                case "1.0.2":
                case "1.0.1":
                case "1.0.0":
                    return new SemanticVersion("0.40.0");

                default:
                    return null;
            }
        }

        /// <summary>Configure the Mono.Cecil assembly resolver.</summary>
        /// <param name="resolver">The assembly resolver.</param>
        internal static void ConfigureAssemblyResolver(AssemblyDefinitionResolver resolver)
        {
            // add search paths
            resolver.TryAddSearchDirectory(Constants.GamePath);
            resolver.TryAddSearchDirectory(Constants.InternalFilesPath);

            // add SoGMAPI explicitly
            // Normally this would be handled automatically by the search paths, but for some reason there's a specific
            // case involving unofficial 64-bit Stardew Valley when launched through Steam (for some players only)
            // where Mono.Cecil can't resolve references to SoGMAPI.
            resolver.Add(AssemblyDefinition.ReadAssembly(typeof(SGame).Assembly.Location));

            // make sure game assembly names can be resolved
            // The game assembly can have one of three names depending how the mod was compiled:
            //   - 'StardewValley': assembly name on Linux/macOS;
            //   - 'Stardew Valley': assembly name on Windows;
            //   - 'Netcode': an assembly that was separate on Windows only before Stardew Valley 1.5.5.
            resolver.AddWithExplicitNames(AssemblyDefinition.ReadAssembly(typeof(Game1).Assembly.Location), "SoG");
        }

        /// <summary>Get metadata for mapping assemblies to the current platform.</summary>
        /// <param name="targetPlatform">The target game platform.</param>
        internal static PlatformAssemblyMap GetAssemblyMap(Platform targetPlatform)
        {
            var removeAssemblyReferences = new List<string>();
            var targetAssemblies = new List<Assembly>();

            // get assembly renamed in SoGMAPI 3.0
            removeAssemblyReferences.Add("SoGModdingAPI.Toolkit.CoreInterfaces");
            targetAssemblies.Add(typeof(SoGModdingAPI.IManifest).Assembly);

            // XNA Framework before Stardew Valley 1.5.5
            removeAssemblyReferences.AddRange(new[]
            {
                "Microsoft.Xna.Framework",
                "Microsoft.Xna.Framework.Game",
                "Microsoft.Xna.Framework.Graphics",
                "Microsoft.Xna.Framework.Xact"
            });
            targetAssemblies.Add(
                typeof(Microsoft.Xna.Framework.Vector2).Assembly
            );

            // `Netcode.dll` merged into the game assembly in Stardew Valley 1.5.5
            removeAssemblyReferences.Add(
                "Netcode"
            );

            // Stardew Valley reference
            removeAssemblyReferences.Add("StardewValley");
            targetAssemblies.Add(typeof(SoG.Game1).Assembly);

            return new PlatformAssemblyMap(targetPlatform, removeAssemblyReferences.ToArray(), targetAssemblies.ToArray());
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the absolute path to the game's <c>Content</c> folder.</summary>
        private static string GetContentFolderPath()
        {
            //
            // We can't use Path.Combine(Constants.GamePath, Game1.content.RootDirectory) here,
            // since Game1.content isn't initialized until later in the game startup.
            //

            string gamePath = EarlyConstants.GamePath;

            // most platforms
            if (EarlyConstants.Platform != GamePlatform.Mac)
                return Path.Combine(gamePath, "Content");

            // macOS
            string[] paths = new[]
                {
                    // GOG
                    // - game:    Stardew Valley.app/Contents/MacOS
                    // - content: Stardew Valley.app/Resources/Content
                    "../../Resources/Content",

                    // Steam
                    // - game:    StardewValley/Contents/MacOS
                    // - content: StardewValley/Contents/Resources/Content
                    "../Resources/Content"
                }
                .Select(path => Path.GetFullPath(Path.Combine(gamePath, path)))
                .ToArray();

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                    return path;
            }

            return paths.Last();
        }

        /// <summary>Get the name of the save folder, if any.</summary>
        private static string? GetSaveFolderName()
        {
            return Constants.GetSaveFolder()?.Name;
        }

        /// <summary>Get the absolute path to the current save folder, if any.</summary>
        private static string? GetSaveFolderPathIfExists()
        {
            DirectoryInfo? saveFolder = Constants.GetSaveFolder();
            return saveFolder?.Exists == true
                ? saveFolder.FullName
                : null;
        }

        /// <summary>Get the current save folder, if any.</summary>
        private static DirectoryInfo? GetSaveFolder()
        {
            // save not available
            if (Context.LoadStage == LoadStage.None)
                return null;

            // get basic info
            string rawSaveName = Game1.GetSaveGameName(set_value: false);
            ulong saveID = Context.LoadStage == LoadStage.SaveParsed
                ? SaveGame.loaded.uniqueIDForThisGame
                : Game1.uniqueIDForThisGame;

            // get best match (accounting for rare case where folder name isn't sanitized)
            DirectoryInfo? folder = null;
            foreach (string saveName in new[] { rawSaveName, new string(rawSaveName.Where(char.IsLetterOrDigit).ToArray()) })
            {
                try
                {
                    folder = new DirectoryInfo(Path.Combine(Constants.SavesPath, $"{saveName}_{saveID}"));
                    if (folder.Exists)
                        return folder;
                }
                catch (ArgumentException)
                {
                    // ignore invalid path
                }
            }

            // if save doesn't exist yet, return the default one we expect to be created
            return folder;
        }

        /// <summary>Get a display label for the game's build number.</summary>
        internal static string GetBuildVersionLabel()
        {
            string version = typeof(Game1).Assembly.GetName().Version?.ToString() ?? "unknown";

            if (version.StartsWith($"{Game1.version}."))
                version = version.Substring(Game1.version.Length + 1);

            return version;
        }
    }
}
