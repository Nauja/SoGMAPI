using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SoGModdingAPI.Framework.Content;
using SoGModdingAPI.Framework.Exceptions;
using SoGModdingAPI.Framework.Reflection;
using SoGModdingAPI.Framework.Utilities;
using static SoGModdingAPI.Framework.LocalizedContentManager;

namespace SoGModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files from the game content folder with support for interception.</summary>
    internal class GameContentManager : BaseContentManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>The assets currently being intercepted by <see cref="IAssetLoader"/> instances. This is used to prevent infinite loops when a loader loads a new asset.</summary>
        private readonly ContextHash<string> AssetsBeingLoaded = new ContextHash<string>();

        /// <summary>Interceptors which provide the initial versions of matching assets.</summary>
        private IList<ModLinked<IAssetLoader>> Loaders => this.Coordinator.Loaders;

        /// <summary>Interceptors which edit matching assets after they're loaded.</summary>
        private IList<ModLinked<IAssetEditor>> Editors => this.Coordinator.Editors;

        /// <summary>Maps asset names to their localized form, like <c>LooseSprites\Billboard => LooseSprites\Billboard.fr-FR</c> (localized) or <c>Maps\AnimalShop => Maps\AnimalShop</c> (not localized).</summary>
        private IDictionary<string, string> LocalizedAssetNames => new Dictionary<string, string>();

        /// <summary>Whether the next load is the first for any game content manager.</summary>
        private static bool IsFirstLoad = true;

        /// <summary>A callback to invoke the first time *any* game content manager loads an asset.</summary>
        private readonly Action OnLoadingFirstAsset;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localize content.</param>
        /// <param name="coordinator">The central coordinator which manages content managers.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="onDisposing">A callback to invoke when the content manager is being disposed.</param>
        /// <param name="onLoadingFirstAsset">A callback to invoke the first time *any* game content manager loads an asset.</param>
        /// <param name="aggressiveMemoryOptimizations">Whether to enable more aggressive memory optimizations.</param>
        public GameContentManager(string name, IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, ContentCoordinator coordinator, IMonitor monitor, Reflector reflection, Action<BaseContentManager> onDisposing, Action onLoadingFirstAsset, bool aggressiveMemoryOptimizations)
            : base()
        {
            this.OnLoadingFirstAsset = onLoadingFirstAsset;
        }

        /// <summary>Parse an asset key that contains an explicit language into its asset name and language, if applicable.</summary>
        /// <param name="rawAsset">The asset key to parse.</param>
        /// <param name="assetName">The asset name without the language code.</param>
        /// <param name="language">The language code removed from the asset name.</param>
        /// <returns>Returns whether the asset key contains an explicit language and was successfully parsed.</returns>
        private bool TryParseExplicitLanguageAssetKey(string rawAsset, out string assetName, out LanguageCode language)
        {
            if (string.IsNullOrWhiteSpace(rawAsset))
                throw new SContentLoadException("The asset key is empty.");

            // extract language code
            int splitIndex = rawAsset.LastIndexOf('.');
            if (splitIndex != -1 && this.LanguageCodes.TryGetValue(rawAsset.Substring(splitIndex + 1), out language))
            {
                assetName = rawAsset.Substring(0, splitIndex);
                return true;
            }

            // no explicit language code found
            assetName = rawAsset;
            language = this.Language;
            return false;
        }

        /// <summary>Load the initial asset from the registered <see cref="Loaders"/>.</summary>
        /// <param name="info">The basic asset metadata.</param>
        /// <returns>Returns the loaded asset metadata, or <c>null</c> if no loader matched.</returns>
        private IAssetData ApplyLoader<T>(IAssetInfo info)
        {
            // find matching loaders
            var loaders = this.Loaders
                .Where(entry =>
                {
                    try
                    {
                        return entry.Data.CanLoad<T>(info);
                    }
                    catch (Exception ex)
                    {
                        entry.Mod.LogAsMod($"Mod failed when checking whether it could load asset '{info.AssetName}', and will be ignored. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                        return false;
                    }
                })
                .ToArray();

            // validate loaders
            if (!loaders.Any())
                return null;
            if (loaders.Length > 1)
            {
                string[] loaderNames = loaders.Select(p => p.Mod.DisplayName).ToArray();
                this.Monitor.Log($"Multiple mods want to provide the '{info.AssetName}' asset ({string.Join(", ", loaderNames)}), but an asset can't be loaded multiple times. SMAPI will use the default asset instead; uninstall one of the mods to fix this. (Message for modders: you should usually use {typeof(IAssetEditor)} instead to avoid conflicts.)", LogLevel.Warn);
                return null;
            }

            // fetch asset from loader
            IModMetadata mod = loaders[0].Mod;
            IAssetLoader loader = loaders[0].Data;
            T data;
            try
            {
                data = loader.Load<T>(info);
                this.Monitor.Log($"{mod.DisplayName} loaded asset '{info.AssetName}'.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                mod.LogAsMod($"Mod crashed when loading asset '{info.AssetName}'. SMAPI will use the default asset instead. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                return null;
            }

            // return matched asset
            return this.TryValidateLoadedAsset(info, data, mod)
                ? new AssetDataForObject(info, data, this.AssertAndNormalizeAssetName)
                : null;
        }

        /// <summary>Apply any <see cref="Editors"/> to a loaded asset.</summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="info">The basic asset metadata.</param>
        /// <param name="asset">The loaded asset.</param>
        private IAssetData ApplyEditors<T>(IAssetInfo info, IAssetData asset)
        {
            IAssetData GetNewData(object data) => new AssetDataForObject(info, data, this.AssertAndNormalizeAssetName);

            // special case: if the asset was loaded with a more general type like 'object', call editors with the actual type instead.
            {
                Type actualType = asset.Data.GetType();
                Type actualOpenType = actualType.IsGenericType ? actualType.GetGenericTypeDefinition() : null;

                if (typeof(T) != actualType && (actualOpenType == typeof(Dictionary<,>) || actualOpenType == typeof(List<>) || actualType == typeof(Texture2D)))
                {
                    return (IAssetData)this.GetType()
                        .GetMethod(nameof(this.ApplyEditors), BindingFlags.NonPublic | BindingFlags.Instance)
                        .MakeGenericMethod(actualType)
                        .Invoke(this, new object[] { info, asset });
                }
            }

            // edit asset
            foreach (var entry in this.Editors)
            {
                // check for match
                IModMetadata mod = entry.Mod;
                IAssetEditor editor = entry.Data;
                try
                {
                    if (!editor.CanEdit<T>(info))
                        continue;
                }
                catch (Exception ex)
                {
                    mod.LogAsMod($"Mod crashed when checking whether it could edit asset '{info.AssetName}', and will be ignored. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                    continue;
                }

                // try edit
                object prevAsset = asset.Data;
                try
                {
                    editor.Edit<T>(asset);
                    this.Monitor.Log($"{mod.DisplayName} edited {info.AssetName}.");
                }
                catch (Exception ex)
                {
                    mod.LogAsMod($"Mod crashed when editing asset '{info.AssetName}', which may cause errors in-game. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                }

                // validate edit
                if (asset.Data == null)
                {
                    mod.LogAsMod($"Mod incorrectly set asset '{info.AssetName}' to a null value; ignoring override.", LogLevel.Warn);
                    asset = GetNewData(prevAsset);
                }
                else if (!(asset.Data is T))
                {
                    mod.LogAsMod($"Mod incorrectly set asset '{asset.AssetName}' to incompatible type '{asset.Data.GetType()}', expected '{typeof(T)}'; ignoring override.", LogLevel.Warn);
                    asset = GetNewData(prevAsset);
                }
            }

            // return result
            return asset;
        }

        /// <summary>Validate that an asset loaded by a mod is valid and won't cause issues.</summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="info">The basic asset metadata.</param>
        /// <param name="data">The loaded asset data.</param>
        /// <param name="mod">The mod which loaded the asset.</param>
        private bool TryValidateLoadedAsset<T>(IAssetInfo info, T data, IModMetadata mod)
        {
            // can't load a null asset
            if (data == null)
            {
                mod.LogAsMod($"SMAPI blocked asset replacement for '{info.AssetName}': mod incorrectly set asset to a null value.", LogLevel.Error);
                return false;
            }

            return true;
        }
    }
}
