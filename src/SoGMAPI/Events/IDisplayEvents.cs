using System;

namespace SoGModdingAPI.Events
{
    /// <summary>Events related to UI and drawing to the screen.</summary>
    public interface IDisplayEvents
    {

        /// <summary>Raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened. The sprite batch may be closed and reopened multiple times after this event is called, but it's only raised once per draw tick. This event isn't useful for drawing to the screen, since the game will draw over it.</summary>
        event EventHandler<RenderingEventArgs> Rendering;

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen. Since the game may open/close the sprite batch multiple times in a draw tick, the sprite batch may not contain everything being drawn and some things may already be rendered to the screen. Content drawn to the sprite batch at this point will be drawn over all vanilla content (including menus, HUD, and cursor).</summary>
        event EventHandler<RenderedEventArgs> Rendered;

        /// <summary>Raised after the game window is resized.</summary>
        event EventHandler<WindowResizedEventArgs> WindowResized;
    }
}
