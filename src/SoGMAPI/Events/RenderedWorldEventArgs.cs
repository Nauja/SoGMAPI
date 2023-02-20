using System;
using Microsoft.Xna.Framework.Graphics;
using SoG;

namespace SoGModdingAPI.Events
{
    /// <summary>Event arguments for an <see cref="IDisplayEvents.RenderedWorld"/> event.</summary>
    public class RenderedWorldEventArgs : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The sprite batch being drawn. Add anything you want to appear on-screen to this sprite batch.</summary>
        public SpriteBatch SpriteBatch => Game1.spriteBatch;
    }
}
