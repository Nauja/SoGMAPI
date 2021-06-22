using System;
using Microsoft.Xna.Framework.Graphics;

namespace SoGModdingAPI.Events
{
    /// <summary>Event arguments for an <see cref="IDisplayEvents.Rendered"/> event.</summary>
    public class RenderedEventArgs : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The sprite batch being drawn. Add anything you want to appear on-screen to this sprite batch.</summary>
        public SpriteBatch SpriteBatch => null;
    }
}
