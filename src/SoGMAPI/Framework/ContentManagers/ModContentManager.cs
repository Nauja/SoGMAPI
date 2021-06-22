using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SoGModdingAPI.Framework.Exceptions;
using SoGModdingAPI.Framework.Reflection;
using SoGModdingAPI.Toolkit.Serialization;
using SoGModdingAPI.Toolkit.Utilities;

namespace SoGModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files from a SMAPI mod folder with support for unpacked files.</summary>
    internal class ModContentManager : BaseContentManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        private readonly JsonHelper JsonHelper;

        /// <summary>The mod display name to show in errors.</summary>
        private readonly string ModName;

        /// <summary>The game content manager used for map tilesheets not provided by the mod.</summary>
        private readonly IContentManager GameContentManager;

        /// <summary>If a map tilesheet's image source has no file extensions, the file extensions to check for in the local mod folder.</summary>
        private readonly string[] LocalTilesheetExtensions = { ".png", ".xnb" };


        /// <summary>Premultiply a texture's alpha values to avoid transparency issues in the game.</summary>
        /// <param name="texture">The texture to premultiply.</param>
        /// <returns>Returns a premultiplied texture.</returns>
        /// <remarks>Based on <a href="https://gamedev.stackexchange.com/a/26037">code by David Gouveia</a>.</remarks>
        private Texture2D PremultiplyTransparency(Texture2D texture)
        {
            // premultiply pixels
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                var pixel = data[i];
                if (pixel.A == byte.MinValue || pixel.A == byte.MaxValue)
                    continue; // no need to change fully transparent/opaque pixels

                data[i] = new Color(pixel.R * pixel.A / byte.MaxValue, pixel.G * pixel.A / byte.MaxValue, pixel.B * pixel.A / byte.MaxValue, pixel.A); // slower version: Color.FromNonPremultiplied(data[i].ToVector4())
            }

            texture.SetData(data);
            return texture;
        }

        /// <summary>Get the asset key for a tilesheet in the game's <c>Maps</c> content folder.</summary>
        /// <param name="relativePath">The tilesheet image source.</param>
        private string GetContentKeyForTilesheetImageSource(string relativePath)
        {
            string key = relativePath;
            string topFolder = PathUtilities.GetSegments(key, limit: 2)[0];

            // convert image source relative to map file into asset key
            if (!topFolder.Equals("Maps", StringComparison.OrdinalIgnoreCase))
                key = Path.Combine("Maps", key);

            // remove file extension from unpacked file
            if (key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(0, key.Length - 4);

            return key;
        }
    }
}
