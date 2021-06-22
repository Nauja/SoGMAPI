using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace SoGModdingAPI.Framework.Content
{
    internal class AssetInfo : IAssetInfo
    {
        /*********
        ** Fields
        *********/
        /// <summary>Normalizes an asset key to match the cache key.</summary>
        protected readonly Func<string, string> GetNormalizedPath;


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string Locale { get; }

        /// <inheritdoc />
        public string AssetName { get; }

        /// <inheritdoc />
        public Type DataType { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="locale">The content's locale code, if the content is localized.</param>
        /// <param name="assetName">The normalized asset name being read.</param>
        /// <param name="type">The content type being read.</param>
        /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
        public AssetInfo(string locale, string assetName, Type type, Func<string, string> getNormalizedPath)
        {
            this.Locale = locale;
            this.AssetName = assetName;
            this.DataType = type;
            this.GetNormalizedPath = getNormalizedPath;
        }

        /// <inheritdoc />
        public bool AssetNameEquals(string path)
        {
            path = this.GetNormalizedPath(path);
            return this.AssetName.Equals(path, StringComparison.OrdinalIgnoreCase);
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Get a human-readable type name.</summary>
        /// <param name="type">The type to name.</param>
        protected string GetFriendlyTypeName(Type type)
        {
            // dictionary
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type[] genericArgs = type.GetGenericArguments();
                return $"Dictionary<{this.GetFriendlyTypeName(genericArgs[0])}, {this.GetFriendlyTypeName(genericArgs[1])}>";
            }

            // texture
            if (type == typeof(Texture2D))
                return type.Name;

            // native type
            if (type == typeof(int))
                return "int";
            if (type == typeof(string))
                return "string";

            // default
            return type.FullName;
        }
    }
}
