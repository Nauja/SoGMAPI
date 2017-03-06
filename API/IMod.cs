using System;

namespace SoG.ModLoader.API
{
    public interface IMod
    {
        #region Infos
        /// <summary>
        /// Unique identifier.
        /// </summary>
        int Id
        {
            get;
        }

        /// <summary>
        /// Author name.
        /// </summary>
        string Author
        {
            get;
        }

        /// <summary>
        /// Unique mod name.
        /// </summary>
        string UniqueName
        {
            get;
        }

        /// <summary>
        /// Mod name.
        /// </summary>
        string Name
        {
            get;
        }
        #endregion

        /// <summary>
        /// Mod loader instance.
        /// </summary>
        IModLoader ModLoader
        {
            get;
        }

        /// <summary>
        /// Logging API.
        /// </summary>
        ILoggerAPI Logger
        {
            get;
        }

        /// <summary>
        /// Mod directory.
        /// </summary>
        string Directory
        {
            get;
        }

        /// <summary>
        /// Called when the mod is loaded.
        /// </summary>
        /// <param name="modLoader">Mod loader</param>
        void OnLoad(IModLoader modLoader);
    }
}
