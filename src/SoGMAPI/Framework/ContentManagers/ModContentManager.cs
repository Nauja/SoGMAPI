using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using SoGModdingAPI.Framework.Content;
using SoGModdingAPI.Framework.Exceptions;
using SoGModdingAPI.Framework.Reflection;
using SoGModdingAPI.Toolkit.Serialization;
using SoGModdingAPI.Toolkit.Utilities;
using SoGModdingAPI.Toolkit.Utilities.PathLookups;
using SoG;




namespace SoGModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files from a SoGMAPI mod folder with support for unpacked files.</summary>
    internal sealed class ModContentManager : BaseContentManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates SoGMAPI's JSON file parsing.</summary>
        private readonly JsonHelper JsonHelper;

        /// <summary>The mod display name to show in errors.</summary>
        private readonly string ModName;

        /// <summary>The game content manager used for map tilesheets not provided by the mod.</summary>
        private readonly IContentManager GameContentManager;

        /// <summary>A lookup for files within the <see cref="ContentManager.RootDirectory"/>.</summary>
        private readonly IFileLookup FileLookup;

        /// <summary>If a map tilesheet's image source has no file extensions, the file extensions to check for in the local mod folder.</summary>
        private static readonly string[] LocalTilesheetExtensions = { ".png", ".xnb" };


        /*********
        ** Accessors
        *********/
#if SOGMAPI_DEPRECATED
        /// <summary>Whether to enable legacy compatibility mode for PyTK scale-up textures.</summary>
        internal static bool EnablePyTkLegacyMode;
#endif


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        /// <param name="gameContentManager">The game content manager used for map tilesheets not provided by the mod.</param>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="modName">The mod display name to show in errors.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localize content.</param>
        /// <param name="coordinator">The central coordinator which manages content managers.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="jsonHelper">Encapsulates SoGMAPI's JSON file parsing.</param>
        /// <param name="onDisposing">A callback to invoke when the content manager is being disposed.</param>
        /// <param name="fileLookup">A lookup for files within the <paramref name="rootDirectory"/>.</param>
        public ModContentManager(string name, IContentManager gameContentManager, IServiceProvider serviceProvider, string modName, string rootDirectory, CultureInfo currentCulture, ContentCoordinator coordinator, IMonitor monitor, Reflector reflection, JsonHelper jsonHelper, Action<BaseContentManager> onDisposing, IFileLookup fileLookup)
            : base(name, serviceProvider, rootDirectory, currentCulture, coordinator, monitor, reflection, onDisposing, isNamespaced: true)
        {
            this.GameContentManager = gameContentManager;
            this.FileLookup = fileLookup;
            this.JsonHelper = jsonHelper;
            this.ModName = modName;

            this.TryLocalizeKeys = false;
        }

        /// <inheritdoc />
        public override bool DoesAssetExist<T>(IAssetName assetName)
        {
            if (base.DoesAssetExist<T>(assetName))
                return true;

            FileInfo file = this.GetModFile<T>(assetName.Name);
            return file.Exists;
        }

        /// <inheritdoc />
        public override T LoadExact<T>(IAssetName assetName, bool useCache)
        {
            // disable caching
            // This is necessary to avoid assets being shared between content managers, which can
            // cause changes to an asset through one content manager affecting the same asset in
            // others (or even fresh content managers). See https://www.patreon.com/posts/27247161
            // for more background info.
            if (useCache)
                throw new InvalidOperationException("Mod content managers don't support asset caching.");

            // resolve managed asset key
            {
                if (this.Coordinator.TryParseManagedAssetKey(assetName.Name, out string? contentManagerID, out IAssetName? relativePath))
                {
                    if (contentManagerID != this.Name)
                        this.ThrowLoadError(assetName, ContentLoadErrorType.AccessDenied, "can't load a different mod's managed asset key through this mod content manager.");
                    assetName = relativePath;
                }
            }

            // get local asset
            T asset;
            try
            {
                // get file
                FileInfo file = this.GetModFile<T>(assetName.Name);
                if (!file.Exists)
                    this.ThrowLoadError(assetName, ContentLoadErrorType.AssetDoesNotExist, "the specified path doesn't exist.");

                // load content
                asset = file.Extension.ToLower() switch
                {
                    ".fnt" => this.LoadFont<T>(assetName, file),
                    ".json" => this.LoadDataFile<T>(assetName, file),
                    ".png" => this.LoadImageFile<T>(assetName, file),
                    ".tbin" or ".tmx" => this.LoadMapFile<T>(assetName, file),
                    ".xnb" => this.LoadXnbFile<T>(assetName),
                    _ => this.HandleUnknownFileType<T>(assetName, file)
                };
            }
            catch (Exception ex)
            {
                if (ex is SContentLoadException)
                    throw;

                this.ThrowLoadError(assetName, ContentLoadErrorType.Other, "an unexpected error occurred.", ex);
                return default;
            }

            // track & return asset
            this.TrackAsset(assetName, asset, useCache: false);
            return asset;
        }

        /// <inheritdoc />
        [Obsolete($"Temporary {nameof(ModContentManager)}s are unsupported")]
        public override LocalizedContentManager CreateTemporary()
        {
            throw new NotSupportedException("Can't create a temporary mod content manager.");
        }

        /// <summary>Get the underlying key in the game's content cache for an asset. This does not validate whether the asset exists.</summary>
        /// <param name="key">The local path to a content file relative to the mod folder.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        public IAssetName GetInternalAssetKey(string key)
        {
            string internalKey = Path.Combine(this.Name, PathUtilities.NormalizeAssetName(key));

            return this.Coordinator.ParseAssetName(internalKey, allowLocales: false);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Load an unpacked font file (<c>.fnt</c>).</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset name relative to the loader root directory.</param>
        /// <param name="file">The file to load.</param>
        private T LoadFont<T>(IAssetName assetName, FileInfo file)
        {
            this.AssertValidType<T>(assetName, file, typeof(XmlSource));

            string source = File.ReadAllText(file.FullName);
            return (T)(object)new XmlSource(source);
        }

        /// <summary>Load an unpacked data file (<c>.json</c>).</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset name relative to the loader root directory.</param>
        /// <param name="file">The file to load.</param>
        private T LoadDataFile<T>(IAssetName assetName, FileInfo file)
        {
            if (!this.JsonHelper.ReadJsonFileIfExists(file.FullName, out T? asset))
                this.ThrowLoadError(assetName, ContentLoadErrorType.InvalidData, "the JSON file is invalid."); // should never happen since we check for file existence before calling this method

            return asset;
        }

        /// <summary>Load an unpacked image file (<c>.png</c>).</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset name relative to the loader root directory.</param>
        /// <param name="file">The file to load.</param>
        private T LoadImageFile<T>(IAssetName assetName, FileInfo file)
        {
            this.AssertValidType<T>(assetName, file, typeof(Texture2D), typeof(IRawTextureData));
            bool returnRawData = typeof(T).IsAssignableTo(typeof(IRawTextureData));

#if SOGMAPI_DEPRECATED
            if (!returnRawData && this.ShouldDisableIntermediateRawDataLoad<T>(assetName, file))
            {
                using FileStream stream = File.OpenRead(file.FullName);
                Texture2D texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream).SetName(assetName);
                this.PremultiplyTransparency(texture);
                return (T)(object)texture;
            }
#endif

            IRawTextureData raw = this.LoadRawImageData(file, returnRawData);

            if (returnRawData)
                return (T)raw;
            else
            {
                Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, raw.Width, raw.Height).SetName(assetName);
                texture.SetData(raw.Data);
                return (T)(object)texture;
            }
        }

#if SOGMAPI_DEPRECATED
        /// <summary>Get whether to disable loading an image as <see cref="IRawTextureData"/> before building a <see cref="Texture2D"/> instance. This isn't called if the mod requested <see cref="IRawTextureData"/> directly.</summary>
        /// <typeparam name="T">The type of asset being loaded.</typeparam>
        /// <param name="assetName">The asset name relative to the loader root directory.</param>
        /// <param name="file">The file being loaded.</param>
        private bool ShouldDisableIntermediateRawDataLoad<T>(IAssetName assetName, FileInfo file)
        {
            // disable raw data if PyTK will rescale the image (until it supports raw data)
            if (ModContentManager.EnablePyTkLegacyMode)
            {
                // PyTK intercepts Texture2D file loads to rescale them (e.g. for HD portraits),
                // but doesn't support IRawTextureData loads yet. We can't just check if the
                // current file has a '.pytk.json' rescale file though, since PyTK may still
                // rescale it if the original asset or another edit gets rescaled.
                this.Monitor.LogOnce("Enabled compatibility mode for PyTK 1.23.* or earlier. This won't cause any issues, but may impact performance. This will no longer be supported in the upcoming SoGMAPI 4.0.0.", LogLevel.Warn);
                return true;
            }

            return false;
        }
#endif



        /// <summary>Handle a request to load a file type that isn't supported by SoGMAPI.</summary>
        /// <typeparam name="T">The expected file type.</typeparam>
        /// <param name="assetName">The asset name relative to the loader root directory.</param>
        /// <param name="file">The file to load.</param>
        private T HandleUnknownFileType<T>(IAssetName assetName, FileInfo file)
        {
            this.ThrowLoadError(assetName, ContentLoadErrorType.InvalidName, $"unknown file extension '{file.Extension}'; must be one of '.fnt', '.json', '.png', '.tbin', '.tmx', or '.xnb'.");
            return default;
        }

        /// <summary>Assert that the asset type is compatible with one of the allowed types.</summary>
        /// <typeparam name="TAsset">The actual asset type.</typeparam>
        /// <param name="assetName">The asset name relative to the loader root directory.</param>
        /// <param name="file">The file being loaded.</param>
        /// <param name="validTypes">The allowed asset types.</param>
        /// <exception cref="SContentLoadException">The <typeparamref name="TAsset"/> is not compatible with any of the <paramref name="validTypes"/>.</exception>
        private void AssertValidType<TAsset>(IAssetName assetName, FileInfo file, params Type[] validTypes)
        {
            if (!validTypes.Any(validType => validType.IsAssignableFrom(typeof(TAsset))))
                this.ThrowLoadError(assetName, ContentLoadErrorType.InvalidData, $"can't read file with extension '{file.Extension}' as type '{typeof(TAsset)}'; must be type '{string.Join("' or '", validTypes.Select(p => p.FullName))}'.");
        }

        /// <summary>Throw an error which indicates that an asset couldn't be loaded.</summary>
        /// <param name="errorType">Why loading an asset through the content pipeline failed.</param>
        /// <param name="assetName">The asset name that failed to load.</param>
        /// <param name="reasonPhrase">The reason the file couldn't be loaded.</param>
        /// <param name="exception">The underlying exception, if applicable.</param>
        /// <exception cref="SContentLoadException" />
        [DoesNotReturn]
        [DebuggerStepThrough, DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowLoadError(IAssetName assetName, ContentLoadErrorType errorType, string reasonPhrase, Exception? exception = null)
        {
            throw new SContentLoadException(errorType, $"Failed loading asset '{assetName}' from {this.Name}: {reasonPhrase}", exception);
        }

        /// <summary>Get a file from the mod folder.</summary>
        /// <typeparam name="T">The expected asset type.</typeparam>
        /// <param name="path">The asset path relative to the content folder.</param>
        private FileInfo GetModFile<T>(string path)
        {
            // get exact file
            FileInfo file = this.FileLookup.GetFile(path);

            // try with default image extensions
            if (!file.Exists && typeof(Texture2D).IsAssignableFrom(typeof(T)) && !ModContentManager.LocalTilesheetExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            {
                foreach (string extension in ModContentManager.LocalTilesheetExtensions)
                {
                    FileInfo result = new(file.FullName + extension);
                    if (result.Exists)
                    {
                        file = result;
                        break;
                    }
                }
            }

            return file;
        }

        /// <summary>Premultiply a texture's alpha values to avoid transparency issues in the game.</summary>
        /// <param name="texture">The texture to premultiply.</param>
        /// <returns>Returns a premultiplied texture.</returns>
        /// <remarks>Based on <a href="https://gamedev.stackexchange.com/a/26037">code by David Gouveia</a>.</remarks>
        private void PremultiplyTransparency(Texture2D texture)
        {
            int count = texture.Width * texture.Height;
            Color[] data = ArrayPool<Color>.Shared.Rent(count);
            try
            {
                texture.GetData(data, 0, count);

                bool changed = false;
                for (int i = 0; i < count; i++)
                {
                    ref Color pixel = ref data[i];
                    if (pixel.A is (byte.MinValue or byte.MaxValue))
                        continue; // no need to change fully transparent/opaque pixels

                    data[i] = new Color(pixel.R * pixel.A / byte.MaxValue, pixel.G * pixel.A / byte.MaxValue, pixel.B * pixel.A / byte.MaxValue, pixel.A); // slower version: Color.FromNonPremultiplied(data[i].ToVector4())
                    changed = true;
                }

                if (changed)
                    texture.SetData(data, 0, count);
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(data);
            }
        }

        /// <summary>Fix custom map tilesheet paths so they can be found by the content manager.</summary>
        /// <param name="map">The map whose tilesheets to fix.</param>
        /// <param name="relativeMapPath">The relative map path within the mod folder.</param>
        /// <param name="fixEagerPathPrefixes">Whether to undo the game's eager tilesheet path prefixing for maps loaded from an <c>.xnb</c> file, which incorrectly prefixes tilesheet paths with the map's local asset key folder.</param>
        /// <exception cref="ContentLoadException">A map tilesheet couldn't be resolved.</exception>
        private void FixTilesheetPaths(Map map, string relativeMapPath, bool fixEagerPathPrefixes)
        {
            // get map info
            relativeMapPath = this.AssertAndNormalizeAssetName(relativeMapPath); // Mono's Path.GetDirectoryName doesn't handle Windows dir separators
            string relativeMapFolder = Path.GetDirectoryName(relativeMapPath) ?? ""; // folder path containing the map, relative to the mod folder

            // fix tilesheets
            this.Monitor.VerboseLog($"Fixing tilesheet paths for map '{relativeMapPath}' from mod '{this.ModName}'...");
            foreach (TileSheet tilesheet in map.TileSheets)
            {
                // get image source
                tilesheet.ImageSource = this.NormalizePathSeparators(tilesheet.ImageSource);
                string imageSource = tilesheet.ImageSource;

                // reverse incorrect eager tilesheet path prefixing
                if (fixEagerPathPrefixes && relativeMapFolder.Length > 0 && imageSource.StartsWith(relativeMapFolder))
                    imageSource = imageSource[(relativeMapFolder.Length + 1)..];

                // validate tilesheet path
                string errorPrefix = $"{this.ModName} loaded map '{relativeMapPath}' with invalid tilesheet path '{imageSource}'.";
                if (Path.IsPathRooted(imageSource) || PathUtilities.GetSegments(imageSource).Contains(".."))
                    throw new SContentLoadException(ContentLoadErrorType.InvalidData, $"{errorPrefix} Tilesheet paths must be a relative path without directory climbing (../).");

                // load best match
                try
                {
                    if (!this.TryGetTilesheetAssetName(relativeMapFolder, imageSource, out IAssetName? assetName, out string? error))
                        throw new SContentLoadException(ContentLoadErrorType.InvalidData, $"{errorPrefix} {error}");

                    if (assetName is not null)
                    {
                        if (!assetName.IsEquivalentTo(tilesheet.ImageSource))
                            this.Monitor.VerboseLog($"   Mapped tilesheet '{tilesheet.ImageSource}' to '{assetName}'.");

                        tilesheet.ImageSource = assetName.Name;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SContentLoadException)
                        throw;

                    throw new SContentLoadException(ContentLoadErrorType.InvalidData, $"{errorPrefix} The tilesheet couldn't be loaded.", ex);
                }
            }
        }

        /// <summary>Get the actual asset name for a tilesheet.</summary>
        /// <param name="modRelativeMapFolder">The folder path containing the map, relative to the mod folder.</param>
        /// <param name="relativePath">The tilesheet path to load.</param>
        /// <param name="assetName">The found asset name.</param>
        /// <param name="error">A message indicating why the file couldn't be loaded.</param>
        /// <returns>Returns whether the asset name was found.</returns>
        /// <remarks>See remarks on <see cref="FixTilesheetPaths"/>.</remarks>
        private bool TryGetTilesheetAssetName(string modRelativeMapFolder, string relativePath, out IAssetName? assetName, out string? error)
        {
            error = null;

            // nothing to do
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                assetName = null;
                return true;
            }

            // special case: local filenames starting with a dot should be ignored
            // For example, this lets mod authors have a '.spring_town.png' file in their map folder so it can be
            // opened in Tiled, while still mapping it to the vanilla 'Maps/spring_town' asset at runtime.
            {
                string filename = Path.GetFileName(relativePath);
                if (filename.StartsWith('.'))
                    relativePath = Path.Combine(Path.GetDirectoryName(relativePath) ?? "", filename.TrimStart('.'));
            }

            // get relative to map file
            {
                string localKey = Path.Combine(modRelativeMapFolder, relativePath);
                if (this.GetModFile<Texture2D>(localKey).Exists)
                {
                    assetName = this.GetInternalAssetKey(localKey);
                    return true;
                }
            }

            // get from game assets
            AssetName contentKey = this.Coordinator.ParseAssetName(this.GetContentKeyForTilesheetImageSource(relativePath), allowLocales: false);
            try
            {
                this.GameContentManager.LoadLocalized<Texture2D>(contentKey, this.GameContentManager.Language, useCache: true); // no need to bypass cache here, since we're not storing the asset
                assetName = contentKey;
                return true;
            }
            catch
            {
                // ignore file-not-found errors
                // TODO: while it's useful to suppress an asset-not-found error here to avoid
                // confusion, this is a pretty naive approach. Even if the file doesn't exist,
                // the file may have been loaded through an IAssetLoader which failed. So even
                // if the content file doesn't exist, that doesn't mean the error here is a
                // content-not-found error. Unfortunately XNA doesn't provide a good way to
                // detect the error type.
                if (this.GetContentFolderFileExists(contentKey.Name))
                    throw;
            }

            // not found
            assetName = null;
            error = "The tilesheet couldn't be found relative to either map file or the game's content folder.";
            return false;
        }

        /// <summary>Get whether a file from the game's content folder exists.</summary>
        /// <param name="key">The asset key.</param>
        private bool GetContentFolderFileExists(string key)
        {
            // get file path
            string path = Path.Combine(this.GameContentManager.FullRootDirectory, key);
            if (!path.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                path += ".xnb";

            // get file
            return File.Exists(path);
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
                key = key[..^4];

            return key;
        }
    }
}
