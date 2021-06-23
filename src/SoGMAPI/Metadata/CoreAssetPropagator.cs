using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SoGModdingAPI.Framework;
using SoGModdingAPI.Framework.ContentManagers;
using SoGModdingAPI.Framework.Reflection;
using SoGModdingAPI.Toolkit.Utilities;
using SoG;

namespace SoGModdingAPI.Metadata
{
    /// <summary>Propagates changes to core assets to the game state.</summary>
    internal class CoreAssetPropagator
    {
        /*********
        ** Fields
        *********/
        /// <summary>The main content manager through which to reload assets.</summary>
        private readonly LocalizedContentManager MainContentManager;

        /// <summary>An internal content manager used only for asset propagation. See remarks on <see cref="GameContentManagerForAssetPropagation"/>.</summary>
        private readonly GameContentManagerForAssetPropagation DisposableContentManager;

        /// <summary>Writes messages to the console.</summary>
        private readonly IMonitor Monitor;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Whether to enable more aggressive memory optimizations.</summary>
        private readonly bool AggressiveMemoryOptimizations;

        /// <summary>Normalizes an asset key to match the cache key and assert that it's valid.</summary>
        private readonly Func<string, string> AssertAndNormalizeAssetName;

        /// <summary>Optimized bucket categories for batch reloading assets.</summary>
        private enum AssetBucket
        {
            /// <summary>NPC overworld sprites.</summary>
            Sprite,

            /// <summary>Villager dialogue portraits.</summary>
            Portrait,

            /// <summary>Any other asset.</summary>
            Other
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Initialize the core asset data.</summary>
        /// <param name="mainContent">The main content manager through which to reload assets.</param>
        /// <param name="disposableContent">An internal content manager used only for asset propagation.</param>
        /// <param name="monitor">Writes messages to the console.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="aggressiveMemoryOptimizations">Whether to enable more aggressive memory optimizations.</param>
        public CoreAssetPropagator(LocalizedContentManager mainContent, GameContentManagerForAssetPropagation disposableContent, IMonitor monitor, Reflector reflection, bool aggressiveMemoryOptimizations)
        {
            this.MainContentManager = mainContent;
            this.DisposableContentManager = disposableContent;
            this.Monitor = monitor;
            this.Reflection = reflection;
            this.AggressiveMemoryOptimizations = aggressiveMemoryOptimizations;

            this.AssertAndNormalizeAssetName = disposableContent.AssertAndNormalizeAssetName;
        }

        /// <summary>Reload one of the game's core assets (if applicable).</summary>
        /// <param name="assets">The asset keys and types to reload.</param>
        /// <param name="ignoreWorld">Whether the in-game world is fully unloaded (e.g. on the title screen), so there's no need to propagate changes into the world.</param>
        /// <param name="propagatedAssets">A lookup of asset names to whether they've been propagated.</param>
        /// <param name="updatedNpcWarps">Whether the NPC pathfinding cache was reloaded.</param>
        public void Propagate(IDictionary<string, Type> assets, bool ignoreWorld, out IDictionary<string, bool> propagatedAssets, out bool updatedNpcWarps)
        {
            // group into optimized lists
            var buckets = assets.GroupBy(p =>
            {
                if (this.IsInFolder(p.Key, "Characters") || this.IsInFolder(p.Key, "Characters\\Monsters"))
                    return AssetBucket.Sprite;

                if (this.IsInFolder(p.Key, "Portraits"))
                    return AssetBucket.Portrait;

                return AssetBucket.Other;
            });

            // reload assets
            propagatedAssets = assets.ToDictionary(p => p.Key, _ => false, StringComparer.OrdinalIgnoreCase);
            updatedNpcWarps = false;
            foreach (var bucket in buckets)
            {
                switch (bucket.Key)
                {
                    default:
                        // @todo
                        break;
                }
            }
        }

        /// <summary>Normalize an asset key to match the cache key and assert that it's valid, but don't raise an error for null or empty values.</summary>
        /// <param name="path">The asset key to normalize.</param>
        private string NormalizeAssetNameIgnoringEmpty(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return this.AssertAndNormalizeAssetName(path);
        }

        /// <summary>Get whether a key starts with a substring after the substring is normalized.</summary>
        /// <param name="key">The key to check.</param>
        /// <param name="rawSubstring">The substring to normalize and find.</param>
        private bool KeyStartsWith(string key, string rawSubstring)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(rawSubstring))
                return false;

            return key.StartsWith(this.NormalizeAssetNameIgnoringEmpty(rawSubstring), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Get whether a normalized asset key is in the given folder.</summary>
        /// <param name="key">The normalized asset key (like <c>Animals/cat</c>).</param>
        /// <param name="folder">The key folder (like <c>Animals</c>); doesn't need to be normalized.</param>
        /// <param name="allowSubfolders">Whether to return true if the key is inside a subfolder of the <paramref name="folder"/>.</param>
        private bool IsInFolder(string key, string folder, bool allowSubfolders = false)
        {
            return
                this.KeyStartsWith(key, $"{folder}\\")
                && (allowSubfolders || this.CountSegments(key) == this.CountSegments(folder) + 1);
        }

        /// <summary>Get the segments in a path (e.g. 'a/b' is 'a' and 'b').</summary>
        /// <param name="path">The path to check.</param>
        private string[] GetSegments(string path)
        {
            return path != null
                ? PathUtilities.GetSegments(path)
                : new string[0];
        }

        /// <summary>Count the number of segments in a path (e.g. 'a/b' is 2).</summary>
        /// <param name="path">The path to check.</param>
        private int CountSegments(string path)
        {
            return this.GetSegments(path).Length;
        }

        /// <summary>Load a texture, and dispose the old one if <see cref="AggressiveMemoryOptimizations"/> is enabled and it's different from the new instance.</summary>
        /// <param name="oldTexture">The previous texture to dispose.</param>
        /// <param name="key">The asset key to load.</param>
        private Texture2D LoadAndDisposeIfNeeded(Texture2D oldTexture, string key)
        {
            // if aggressive memory optimizations are enabled, load the asset from the disposable
            // content manager and dispose the old instance if needed.
            if (this.AggressiveMemoryOptimizations)
            {
                GameContentManagerForAssetPropagation content = this.DisposableContentManager;

                Texture2D newTexture = content.Load<Texture2D>(key);
                if (oldTexture?.IsDisposed == false && !object.ReferenceEquals(oldTexture, newTexture) && content.IsResponsibleFor(oldTexture))
                    oldTexture.Dispose();

                return newTexture;
            }

            // else just (re)load it from the main content manager
            return this.MainContentManager.Load<Texture2D>(key);
        }
    }
}
