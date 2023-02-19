namespace SoGModdingAPI.Framework
{
    /// <summary>The SoGMAPI exit state.</summary>
    internal enum ExitState
    {
        /// <summary>SoGMAPI didn't trigger an explicit exit.</summary>
        None,

        /// <summary>The game is exiting normally.</summary>
        GameExit,

        /// <summary>The game is exiting due to an error.</summary>
        Crash
    }
}
