using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework.Content;
using SoGModdingAPI.Framework.Exceptions;

namespace SoGModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files.</summary>
    internal interface IContentManager : IDisposable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A name for the mod manager. Not guaranteed to be unique.</summary>
        string Name { get; }

        /// <summary>Whether this content manager can be targeted by managed asset keys (e.g. to load assets from a mod folder).</summary>
        bool IsNamespaced { get; }
    }
}
