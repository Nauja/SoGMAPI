using System;
using SoGModdingAPI.Events;

namespace SoGModdingAPI.Framework.Events
{
    /// <summary>Events raised when the player data changes.</summary>
    internal class ModPlayerEvents : ModEventsBase, IPlayerEvents
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModPlayerEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
