from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        ("""using SkiaSharp;""", ""),
        (
            """        /// <summary>Load the raw image data from a file on disk.</summary>
        /// <param name="file">The file whose data to load.</param>
        /// <param name="forRawData">Whether the data is being loaded for an <see cref="IRawTextureData"/> (true) or <see cref="Texture2D"/> (false) instance.</param>
        /// <remarks>This is separate to let framework mods intercept the data before it's loaded, if needed.</remarks>
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "The 'forRawData' parameter is only added for mods which may intercept this method.")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "The 'forRawData' parameter is only added for mods which may intercept this method.")]
        private IRawTextureData LoadRawImageData(FileInfo file, bool forRawData)
        {
            // load raw data
            int width;
            int height;
            SKPMColor[] rawPixels;
            {
                using FileStream stream = File.OpenRead(file.FullName);
                using SKBitmap bitmap = SKBitmap.Decode(stream);

                if (bitmap is null)
                    throw new InvalidDataException($"Failed to load {file.FullName}. This doesn't seem to be a valid PNG image.");

                rawPixels = SKPMColor.PreMultiply(bitmap.Pixels);
                width = bitmap.Width;
                height = bitmap.Height;
            }

            // convert to XNA pixel format
            var pixels = GC.AllocateUninitializedArray<Color>(rawPixels.Length);
            for (int i = 0; i < pixels.Length; i++)
            {
                SKPMColor pixel = rawPixels[i];
                pixels[i] = pixel.Alpha == 0
                    ? Color.Transparent
                    : new Color(r: pixel.Red, g: pixel.Green, b: pixel.Blue, alpha: pixel.Alpha);
            }

            return new RawTextureData(width, height, pixels);
        }

        /// <summary>Load an unpacked image file (<c>.tbin</c> or <c>.tmx</c>).</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset name relative to the loader root directory.</param>
        /// <param name="file">The file to load.</param>
        private T LoadMapFile<T>(IAssetName assetName, FileInfo file)
        {
            this.AssertValidType<T>(assetName, file, typeof(Map));

            FormatManager formatManager = FormatManager.Instance;
            Map map = formatManager.LoadMap(file.FullName);
            map.assetPath = assetName.Name;
            this.FixTilesheetPaths(map, relativeMapPath: assetName.Name, fixEagerPathPrefixes: false);
            return (T)(object)map;
        }

        /// <summary>Load a packed file (<c>.xnb</c>).</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset name relative to the loader root directory.</param>
        private T LoadXnbFile<T>(IAssetName assetName)
        {
            if (typeof(IRawTextureData).IsAssignableFrom(typeof(T)))
                this.ThrowLoadError(assetName, ContentLoadErrorType.Other, $"can't read XNB file as type {typeof(IRawTextureData)}; that type can only be read from a PNG file.");

            // the underlying content manager adds a .xnb extension implicitly, so
            // we need to strip it here to avoid trying to load a '.xnb.xnb' file.
            IAssetName loadName = assetName.Name.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase)
                ? this.Coordinator.ParseAssetName(assetName.Name[..^".xnb".Length], allowLocales: false)
                : assetName;

            // load asset
            T asset = this.RawLoad<T>(loadName, useCache: false);
            if (asset is Map map)
            {
                map.assetPath = loadName.Name;
                this.FixTilesheetPaths(map, relativeMapPath: loadName.Name, fixEagerPathPrefixes: true);
            }

            return asset;
        }""",
            "",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
