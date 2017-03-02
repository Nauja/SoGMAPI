using System;
using System.Collections.Generic;

namespace SoG.ModLoader.API
{
    /// <summary>
    /// Interface for the player palette API.
    /// </summary>
    public interface IPlayerPaletteAPI
    {
        /// <summary>
        /// Register a player palette.
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="name">Name</param>
        /// <param name="texturePath">Path to texture</param>
        /// <returns>Registered player palette</returns>
        IPlayerPalette RegisterPlayerPalette(PlayerPalette.Type type, string name, string texturePath);

        /// <summary>
        /// Unregister a player palette.
        /// </summary>
        /// <param name="playerPalette">Player palette</param>
        void UnregisterPlayerPalette(IPlayerPalette playerPalette);

        /// <summary>
        /// Get the number of registered player palettes matching a filter.
        /// </summary>
        /// <param name="filter">Filter to match</param>
        /// <returns>Number of registered player palettes</returns>
        int GetNumPlayerPalettes(Func<IPlayerPalette, bool> filter = null);

        /// <summary>
        /// Get an iterator on the registered player palettes matching a filter.
        /// </summary>
        /// <param name="filter">Filter to match</param>
        /// <returns>Player palettes</returns>
        IEnumerable<IPlayerPalette> GetPlayerPalettes(Func<IPlayerPalette, bool> filter = null);
    }
}
