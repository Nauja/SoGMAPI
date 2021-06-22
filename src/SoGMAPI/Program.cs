﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using SoGModdingAPI.Framework;

namespace SoGModdingAPI
{
    /// <summary>The main entry point for SMAPI, responsible for hooking into and launching the game.</summary>
    internal class Program
    {
        /*********
        ** Fields
        *********/
        /// <summary>The absolute path to search for SMAPI's internal DLLs.</summary>
        internal static readonly string DllSearchPath = EarlyConstants.InternalFilesPath;


        /*********
        ** Public methods
        *********/
        /// <summary>The main entry point which hooks into and launches the game.</summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            Console.Title = $"SoGMAPI {EarlyConstants.RawApiVersion}{(EarlyConstants.IsWindows64BitHack ? " 64-bit" : "")} - {Console.Title}";

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += Program.CurrentDomain_AssemblyResolve;
                Program.AssertGamePresent();
                Program.AssertGameVersion();
                Program.Start(args);
            }
            catch (BadImageFormatException ex) when (ex.FileName == "SecretsOfGrindea" || ex.FileName == "Secrets Of Grindea") // don't use EarlyConstants.GameAssemblyName, since we want to check both possible names
            {
                Console.WriteLine($"SoGMAPI failed to initialize because your game's {ex.FileName}.exe seems to be invalid.\nThis may be a pirated version which modified the executable in an incompatible way; if so, you can try a different download or buy a legitimate version.\n\nTechnical details:\n{ex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SoGMAPI failed to initialize: {ex}");
                Program.PressAnyKeyToExit(true);
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Method called when assembly resolution fails, which may return a manually resolved assembly.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
        {
            try
            {
                AssemblyName name = new AssemblyName(e.Name);
                foreach (FileInfo dll in new DirectoryInfo(Program.DllSearchPath).EnumerateFiles("*.dll"))
                {
                    if (name.Name.Equals(AssemblyName.GetAssemblyName(dll.FullName).Name, StringComparison.OrdinalIgnoreCase))
                        return Assembly.LoadFrom(dll.FullName);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving assembly: {ex}");
                return null;
            }
        }

        /// <summary>Assert that the game is available.</summary>
        /// <remarks>This must be checked *before* any references to <see cref="Constants"/>, and this method should not reference <see cref="Constants"/> itself to avoid errors in Mono or when the game isn't present.</remarks>
        private static void AssertGamePresent()
        {
            try
            {
                _ = Type.GetType($"SoG.Game1, {EarlyConstants.GameAssemblyName}", throwOnError: true);
            }
            catch (Exception ex)
            {
                // file doesn't exist
                if (!File.Exists(Path.Combine(EarlyConstants.ExecutionPath, $"{EarlyConstants.GameAssemblyName}.exe")))
                    Program.PrintErrorAndExit("Oops! SoGMAPI can't find the game. Make sure you're running SoGModdingAPI.exe in your game folder.");

                // can't load file
                Program.PrintErrorAndExit(
                    message: "Oops! SoGMAPI couldn't load the game executable. The technical details below may have more info.",
                    technicalMessage: $"Technical details: {ex}"
                );
            }
        }

        /// <summary>Assert that the game version is within <see cref="Constants.MinimumGameVersion"/> and <see cref="Constants.MaximumGameVersion"/>.</summary>
        private static void AssertGameVersion()
        {
            // min version
            if (Constants.GameVersion.IsOlderThan(Constants.MinimumGameVersion))
            {
                ISemanticVersion suggestedApiVersion = Constants.GetCompatibleApiVersion(Constants.GameVersion);
                Program.PrintErrorAndExit(suggestedApiVersion != null
                    ? $"Oops! You're running Secrets Of Grindea {Constants.GameVersion}, but the oldest supported version is {Constants.MinimumGameVersion}. You can install SoGMAPI {suggestedApiVersion} instead to fix this error, or update your game to the latest version."
                    : $"Oops! You're running Secrets Of Grindea {Constants.GameVersion}, but the oldest supported version is {Constants.MinimumGameVersion}. Please update your game before using SoGMAPI."
                );
            }

            // max version
            else if (Constants.MaximumGameVersion != null && Constants.GameVersion.IsNewerThan(Constants.MaximumGameVersion))
                Program.PrintErrorAndExit($"Oops! You're running Secrets Of Grindea {Constants.GameVersion}, but this version of SoGMAPI is only compatible up to Secrets Of Grindea {Constants.MaximumGameVersion}. Please check for a newer version of SoGMAPI: https://smapi.io.");
        }

        /// <summary>Initialize SMAPI and launch the game.</summary>
        /// <param name="args">The command-line arguments.</param>
        /// <remarks>This method is separate from <see cref="Main"/> because that can't contain any references to assemblies loaded by <see cref="CurrentDomain_AssemblyResolve"/> (e.g. via <see cref="Constants"/>), or Mono will incorrectly show an assembly resolution error before assembly resolution is set up.</remarks>
        private static void Start(string[] args)
        {
            // get flags
            bool writeToConsole = !args.Contains("--no-terminal") && Environment.GetEnvironmentVariable("SOGMAPI_NO_TERMINAL") == null;

            // get mods path
            string modsPath;
            {
                string rawModsPath = null;

                // get from command line args
                int pathIndex = Array.LastIndexOf(args, "--mods-path") + 1;
                if (pathIndex >= 1 && args.Length >= pathIndex)
                    rawModsPath = args[pathIndex];

                // get from environment variables
                if (string.IsNullOrWhiteSpace(rawModsPath))
                    rawModsPath = Environment.GetEnvironmentVariable("SOGMAPI_MODS_PATH");

                // normalise
                modsPath = !string.IsNullOrWhiteSpace(rawModsPath)
                    ? Path.Combine(Constants.ExecutionPath, rawModsPath)
                    : Constants.DefaultModsPath;
            }

            // load SMAPI
            using SCore core = new SCore(modsPath, writeToConsole);
            core.RunInteractively();
        }

        /// <summary>Write an error directly to the console and exit.</summary>
        /// <param name="message">The error message to display.</param>
        /// <param name="technicalMessage">An additional message to log with technical details.</param>
        private static void PrintErrorAndExit(string message, string technicalMessage = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();

            if (technicalMessage != null)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(technicalMessage);
                Console.ResetColor();
                Console.WriteLine();
            }

            Program.PressAnyKeyToExit(showMessage: true);
        }

        /// <summary>Show a 'press any key to exit' message, and exit when they press a key.</summary>
        /// <param name="showMessage">Whether to print a 'press any key to exit' message to the console.</param>
        private static void PressAnyKeyToExit(bool showMessage)
        {
            if (showMessage)
                Console.WriteLine("Game has ended. Press any key to exit.");
            Thread.Sleep(100);
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
