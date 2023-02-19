using System;
using SoGModdingAPI.Events;

namespace SoGModdingAPI.Framework.Events
{
    /// <inheritdoc cref="IWorldEvents" />
    internal class ModWorldEvents : ModEventsBase, IWorldEvents
    {
        /*********
        ** Accessors
        *********/



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
