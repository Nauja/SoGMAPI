using System;
using SoGModdingAPI.Events;

namespace SoGModdingAPI.Framework.Events
{
    /// <summary>Events related to UI and drawing to the screen.</summary>
    internal class ModDisplayEvents : ModEventsBase, IDisplayEvents
    {
        /*********
        ** Accessors
        *********/

        /// <summary>Raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened. The sprite batch may be closed and reopened multiple times after this event is called, but it's only raised once per draw tick. This event isn't useful for drawing to the screen, since the game will draw over it.</summary>
        public event EventHandler<RenderingEventArgs> Rendering
        {
            add => this.EventManager.Rendering.Add(value, this.Mod);
            remove => this.EventManager.Rendering.Remove(value);
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen. Since the game may open/close the sprite batch multiple times in a draw tick, the sprite batch may not contain everything being drawn and some things may already be rendered to the screen. Content drawn to the sprite batch at this point will be drawn over all vanilla content (including menus, HUD, and cursor).</summary>
        public event EventHandler<RenderedEventArgs> Rendered
        {
            add => this.EventManager.Rendered.Add(value, this.Mod);
            remove => this.EventManager.Rendered.Remove(value);
        }

        /// <summary>Raised after the game window is resized.</summary>
        public event EventHandler<WindowResizedEventArgs> WindowResized
        {
            add => this.EventManager.WindowResized.Add(value, this.Mod);
            remove => this.EventManager.WindowResized.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModDisplayEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
