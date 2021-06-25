using System.IO;

namespace SoGModdingAPI.Installer.Framework
{
    /// <summary>Manages paths for the SoGMAPI installer.</summary>
    internal class InstallerPaths
    {
        /*********
        ** Accessors
        *********/
        /****
        ** Main folders
        ****/
        /// <summary>The directory path containing the files to copy into the game folder.</summary>
        public DirectoryInfo BundleDir { get; }

        /// <summary>The directory containing the installed game.</summary>
        public DirectoryInfo GameDir { get; }

        /// <summary>The directory into which to install mods.</summary>
        public DirectoryInfo ModsDir { get; }

        /****
        ** Installer paths
        ****/
        /// <summary>The full path to directory path containing the files to copy into the game folder.</summary>
        public string BundlePath => this.BundleDir.FullName;

        /// <summary>The full path to the backup API user settings folder, if applicable.</summary>
        public string BundleApiUserConfigPath { get; }

        /****
        ** Game paths
        ****/
        /// <summary>The full path to the directory containing the installed game.</summary>
        public string GamePath => this.GameDir.FullName;

        /// <summary>The full path to the directory into which to install mods.</summary>
        public string ModsPath => this.ModsDir.FullName;

        /// <summary>The full path to SoGMAPI's internal configuration file.</summary>
        public string ApiConfigPath { get; }

        /// <summary>The full path to the user's config overrides file.</summary>
        public string ApiUserConfigPath { get; }

        /// <summary>The full path to the installed game executable file.</summary>
        public string ExecutablePath { get; private set; }

        /// <summary>The full path to the vanilla game launcher on Linux/macOS.</summary>
        public string UnixLauncherPath { get; }

        /// <summary>The full path to the installed SoGMAPI launcher on Linux/macOS before it's renamed.</summary>
        public string UnixSogmapiLauncherPath { get; }

        /// <summary>The full path to the vanilla game launcher on Linux/macOS after SoGMAPI is installed.</summary>
        public string UnixBackupLauncherPath { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="bundleDir">The directory path containing the files to copy into the game folder.</param>
        /// <param name="gameDir">The directory path for the installed game.</param>
        /// <param name="gameExecutableName">The name of the game's executable file for the current platform.</param>
        public InstallerPaths(DirectoryInfo bundleDir, DirectoryInfo gameDir, string gameExecutableName)
        {
            this.BundleDir = bundleDir;
            this.GameDir = gameDir;
            this.ModsDir = new DirectoryInfo(Path.Combine(gameDir.FullName, "Mods"));

            this.BundleApiUserConfigPath = Path.Combine(bundleDir.FullName, "sogmapi-internal", "config.user.json");

            this.ExecutablePath = Path.Combine(gameDir.FullName, gameExecutableName);
            this.UnixLauncherPath = Path.Combine(gameDir.FullName, "SecretsOfGrindea");
            this.UnixSogmapiLauncherPath = Path.Combine(gameDir.FullName, "SoGModdingAPI");
            this.UnixBackupLauncherPath = Path.Combine(gameDir.FullName, "SecretsOfGrindea-original");
            this.ApiConfigPath = Path.Combine(gameDir.FullName, "sogmapi-internal", "config.json");
            this.ApiUserConfigPath = Path.Combine(gameDir.FullName, "sogmapi-internal", "config.user.json");
        }

        /// <summary>Override the filename for the <see cref="ExecutablePath"/>.</summary>
        /// <param name="filename">the file name.</param>
        public void SetExecutableFileName(string filename)
        {
            this.ExecutablePath = Path.Combine(this.GamePath, filename);
        }
    }
}
