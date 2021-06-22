using System;
using SoGModdingAPI.Events;

namespace SoGModdingAPI.Framework.Events
{
    /// <summary>Events raised when something changes in the world.</summary>
    internal class ModWorldEvents : ModEventsBase, IWorldEvents
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModWorldEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
