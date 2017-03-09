namespace SoG.ModLoader.API
{
    public interface IModLoader
    {
        string GameDirectory
        {
            get;
        }

        string ModsDirectory
        {
            get;
        }

        #region Mods
        /// <summary>
        /// Get the instance of a mod.
        /// </summary>
        /// <typeparam name="T">Mod type</typeparam>
        /// <returns>Instance of the mod</returns>
        IMod GetMod<T>() where T : IMod;

        /// <summary>
        /// Get the instance of a mod.
        /// </summary>
        /// <param name="id">Mod id</param>
        /// <returns>Instance of the mod</returns>
        IMod GetMod(int id);

        /// <summary>
        /// Get the absolute path to a mod directory.
        /// </summary>
        /// <typeparam name="T">Mod type</typeparam>
        /// <returns>Absolute path to the mod directory</returns>
        string GetModDirectory<T>() where T : IMod;

        /// <summary>
        /// Get the absolute path to a mod directory.
        /// </summary>
        /// <param name="mod">Mod instance</param>
        /// <returns>Absolute path to the mod directory</returns>
        string GetModDirectory(IMod mod);
        #endregion

        #region API
        /// <summary>
        /// Logging API.
        /// </summary>
        ILoggerAPI LoggerAPI
        {
            get;
        }

        /// <summary>
        /// API for player palettes.
        /// </summary>
        IPlayerPaletteAPI PlayerPaletteAPI
        {
            get;
        }
        #endregion
    }
}
