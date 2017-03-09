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
        /// <param name="playerPalette">Player palette</param>
        IPlayerPalette RegisterPlayerPalette(IPlayerPalette playerPalette);

        /// <summary>
        /// Unregister a player palette.
        /// </summary>
        /// <param name="playerPalette">Player palette</param>
        void UnregisterPlayerPalette(IPlayerPalette playerPalette);

        /// <summary>
        /// Get the number of registered player palettes for a category.
        /// </summary>
        /// <param name="category">Category</param>
        /// <returns>Number of registered player palettes</returns>
        int GetNumPlayerPalettes(PlayerPalette.Type category);

        /// <summary>
        /// Get a registered player palette by id.
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns>Player palette</returns>
        IPlayerPalette GetPlayerPalette(PlayerPalette.Type category, PlayerPalette.ModId id);

        /// <summary>
        /// Get a registered player palette by index.
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="index">Index</param>
        /// <returns>Player palette</returns>
        IPlayerPalette GetPlayerPalette(PlayerPalette.Type category, int index);

        /// <summary>
        /// Get the index of a registered player palette.
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns>Index</returns>
        int GetPlayerPaletteIndex(PlayerPalette.Type category, PlayerPalette.ModId id);

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
