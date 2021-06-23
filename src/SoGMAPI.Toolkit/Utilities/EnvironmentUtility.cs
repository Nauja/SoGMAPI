using System;
using System.Diagnostics.CodeAnalysis;
using SoGModdingAPI.Toolkit.Framework;

namespace SoGModdingAPI.Toolkit.Utilities
{
    /// <summary>Provides methods for fetching environment information.</summary>
    public static class EnvironmentUtility
    {
        /*********
        ** Fields
        *********/
        /// <summary>The cached platform.</summary>
        private static Platform? CachedPlatform;


        /*********
        ** Public methods
        *********/
        /// <summary>Detect the current OS.</summary>
        public static Platform DetectPlatform()
        {
            Platform? platform = EnvironmentUtility.CachedPlatform;

            if (platform == null)
            {
                string rawPlatform = LowLevelEnvironmentUtility.DetectPlatform();
                EnvironmentUtility.CachedPlatform = platform = (Platform)Enum.Parse(typeof(Platform), rawPlatform, ignoreCase: true);
            }

            return platform.Value;
        }


        /// <summary>Get the human-readable OS name and version.</summary>
        /// <param name="platform">The current platform.</param>
        [SuppressMessage("ReSharper", "EmptyGeneralCatchClause", Justification = "Error suppressed deliberately to fallback to default behaviour.")]
        public static string GetFriendlyPlatformName(Platform platform)
        {
            return LowLevelEnvironmentUtility.GetFriendlyPlatformName(platform.ToString());
        }

        /// <summary>Get the name of the Secrets Of Grindea executable.</summary>
        /// <param name="platform">The current platform.</param>
        public static string GetExecutableName(Platform platform)
        {
            return LowLevelEnvironmentUtility.GetExecutableName(platform.ToString());
        }
    }
}
