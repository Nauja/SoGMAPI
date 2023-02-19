namespace SoGModdingAPI.Framework.ModLoading
{
    /// <summary>Indicates why a mod could not be loaded.</summary>
    internal enum ModFailReason
    {
        /// <summary>The mod has been disabled by prefixing its folder with a dot.</summary>
        DisabledByDotConvention,

        /// <summary>Multiple copies of the mod are installed.</summary>
        Duplicate,

        /// <summary>The folder is empty or contains only ignored files.</summary>
        EmptyFolder,

        /// <summary>The mod has incompatible code instructions, needs a newer SoGMAPI version, or is marked 'assume broken' in the SoGMAPI metadata list.</summary>
        Incompatible,

        /// <summary>The mod's manifest is missing or invalid.</summary>
        InvalidManifest,

        /// <summary>The mod was deemed compatible, but SoGMAPI failed when it tried to load it.</summary>
        LoadFailed,

        /// <summary>The mod requires other mods which aren't installed, or its dependencies have a circular reference.</summary>
        MissingDependencies,

        /// <summary>The mod is marked obsolete in the SoGMAPI metadata list.</summary>
        Obsolete,

        /// <summary>The folder is an XNB mod, which can't be loaded through SoGMAPI.</summary>
        XnbMod
    }
}
