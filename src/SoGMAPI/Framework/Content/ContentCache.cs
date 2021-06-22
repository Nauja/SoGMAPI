using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Xna.Framework;
using SoGModdingAPI.Framework.Reflection;
using SoGModdingAPI.Toolkit.Utilities;

namespace SoGModdingAPI.Framework.Content
{
    /// <summary>A low-level wrapper around the content cache which handles reading, writing, and invalidating entries in the cache. This doesn't handle any higher-level logic like localization, loading content, etc. It assumes all keys passed in are already normalized.</summary>
    internal class ContentCache
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying asset cache.</summary>
        private readonly IDictionary<string, object> Cache;

        /// <summary>Applies platform-specific asset key normalization so it's consistent with the underlying cache.</summary>
        private readonly Func<string, string> NormalizeAssetNameForPlatform;


        /*********
        ** Accessors
        *********/
        /// <summary>Get or set the value of a raw cache entry.</summary>
        /// <param name="key">The cache key.</param>
        public object this[string key]
        {
            get => this.Cache[key];
            set => this.Cache[key] = value;
        }

        /// <summary>The current cache keys.</summary>
        public IEnumerable<string> Keys => this.Cache.Keys;


        /*********
        ** Public methods
        *********/
        /****
        ** Constructor
        ****/
        /// <summary>Construct an instance.</summary>
        /// <param name="contentManager">The underlying content manager whose cache to manage.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        public ContentCache(LocalizedContentManager contentManager, Reflector reflection)
        {
            // init
            this.Cache = reflection.GetField<Dictionary<string, object>>(contentManager, "loadedAssets").GetValue();

            // get key normalization logic
            if (Constants.GameFramework == GameFramework.Xna)
            {
                IReflectedMethod method = reflection.GetMethod(typeof(TitleContainer), "GetCleanPath");
                this.NormalizeAssetNameForPlatform = path => method.Invoke<string>(path);
            }
            else if (EarlyConstants.IsWindows64BitHack)
                this.NormalizeAssetNameForPlatform = PathUtilities.NormalizePath;
            else
                this.NormalizeAssetNameForPlatform = key => key.Replace('\\', '/'); // based on MonoGame's ContentManager.Load<T> logic
        }

        /****
        ** Fetch
        ****/
        /// <summary>Get whether the cache contains a given key.</summary>
        /// <param name="key">The cache key.</param>
        public bool ContainsKey(string key)
        {
            return this.Cache.ContainsKey(key);
        }


        /****
        ** Normalize
        ****/
        /// <summary>Normalize path separators in a file path. For asset keys, see <see cref="NormalizeKey"/> instead.</summary>
        /// <param name="path">The file path to normalize.</param>
        [Pure]
        public string NormalizePathSeparators(string path)
        {
            return PathUtilities.NormalizePath(path);
        }

        /// <summary>Normalize a cache key so it's consistent with the underlying cache.</summary>
        /// <param name="key">The asset key.</param>
        [Pure]
        public string NormalizeKey(string key)
        {
            key = this.NormalizePathSeparators(key);
            return key.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase)
                ? key.Substring(0, key.Length - 4)
                : this.NormalizeAssetNameForPlatform(key);
        }

        /****
        ** Remove
        ****/
        /// <summary>Remove an asset with the given key.</summary>
        /// <param name="key">The cache key.</param>
        /// <param name="dispose">Whether to dispose the entry value, if applicable.</param>
        /// <returns>Returns the removed key (if any).</returns>
        public bool Remove(string key, bool dispose)
        {
            // get entry
            if (!this.Cache.TryGetValue(key, out object value))
                return false;

            // dispose & remove entry
            if (dispose && value is IDisposable disposable)
                disposable.Dispose();

            return this.Cache.Remove(key);
        }

        /// <summary>Purge matched assets from the cache.</summary>
        /// <param name="predicate">Matches the asset keys to invalidate.</param>
        /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
        /// <returns>Returns the removed keys (if any).</returns>
        public IEnumerable<string> Remove(Func<string, object, bool> predicate, bool dispose)
        {
            List<string> removed = new List<string>();
            foreach (string key in this.Cache.Keys.ToArray())
            {
                if (predicate(key, this.Cache[key]))
                {
                    this.Remove(key, dispose);
                    removed.Add(key);
                }
            }
            return removed;
        }
    }
}
