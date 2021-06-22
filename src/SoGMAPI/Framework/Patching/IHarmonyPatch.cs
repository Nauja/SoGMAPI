#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif

namespace SoGModdingAPI.Framework.Patching
{
    /// <summary>A Harmony patch to apply.</summary>
    internal interface IHarmonyPatch
    {
        /*********
        ** Methods
        *********/
        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
#if HARMONY_2
        void Apply(Harmony harmony);
#else
        void Apply(HarmonyInstance harmony);
#endif
    }
}
