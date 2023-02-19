#if SOGMAPI_DEPRECATED
using System;
#endif

namespace SoGModdingAPI
{
    /// <summary>The game framework running the game.</summary>
    public enum GameFramework
    {
#if SOGMAPI_DEPRECATED
        /// <summary>The XNA Framework, previously used on Windows.</summary>
        [Obsolete("Stardew Valley no longer uses XNA Framework on any supported platform.  This value will be removed in SoGMAPI 4.0.0.")]
        Xna,
#endif

        /// <summary>The MonoGame framework.</summary>
        MonoGame
    }
}
