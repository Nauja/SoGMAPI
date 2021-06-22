using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SoGModdingAPI.Toolkit.Utilities;
using xTile;
using xTile.Layers;
using xTile.Tiles;

namespace SoGModdingAPI.Framework.Content
{
    /// <summary>Encapsulates access and changes to image content being read from a data file.</summary>
    internal class AssetDataForMap : AssetData<Map>, IAssetDataForMap
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="locale">The content's locale code, if the content is localized.</param>
        /// <param name="assetName">The normalized asset name being read.</param>
        /// <param name="data">The content data being read.</param>
        /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
        /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
        public AssetDataForMap(string locale, string assetName, Map data, Func<string, string> getNormalizedPath, Action<Map> onDataReplaced)
            : base(locale, assetName, data, getNormalizedPath, onDataReplaced) { }

        /// <inheritdoc />
        /// <remarks>Derived from <see cref="StardewValley.GameLocation.ApplyMapOverride"/> with a few changes:
        /// - can be applied directly to the maps when loading, before the location is created;
        /// - added support for source/target areas;
        /// - added disambiguation if source has a modified version of the same tilesheet, instead of copying tiles into the target tilesheet;
        /// - changed to always overwrite tiles within the target area (to avoid edge cases where some tiles are only partly applied);
        /// - fixed copying tilesheets (avoid "The specified TileSheet was not created for use with this map" error);
        /// - fixed tilesheets not added at the end (via z_ prefix), which can cause crashes in game code which depends on hardcoded tilesheet indexes;
        /// - fixed issue where different tilesheets are linked by ID.
        /// </remarks>
        public void PatchMap(Map source, Rectangle? sourceArea = null, Rectangle? targetArea = null)
        {
            var target = this.Data;

            // get areas
            {
                Rectangle sourceBounds = this.GetMapArea(source);
                Rectangle targetBounds = this.GetMapArea(target);
                sourceArea ??= new Rectangle(0, 0, sourceBounds.Width, sourceBounds.Height);
                targetArea ??= new Rectangle(0, 0, Math.Min(sourceArea.Value.Width, targetBounds.Width), Math.Min(sourceArea.Value.Height, targetBounds.Height));

                // validate
                if (sourceArea.Value.X < 0 || sourceArea.Value.Y < 0 || sourceArea.Value.Right > sourceBounds.Width || sourceArea.Value.Bottom > sourceBounds.Height)
                    throw new ArgumentOutOfRangeException(nameof(sourceArea), $"The source area ({sourceArea}) is outside the bounds of the source map ({sourceBounds}).");
                if (targetArea.Value.X < 0 || targetArea.Value.Y < 0 || targetArea.Value.Right > targetBounds.Width || targetArea.Value.Bottom > targetBounds.Height)
                    throw new ArgumentOutOfRangeException(nameof(targetArea), $"The target area ({targetArea}) is outside the bounds of the target map ({targetBounds}).");
                if (sourceArea.Value.Width != targetArea.Value.Width || sourceArea.Value.Height != targetArea.Value.Height)
                    throw new InvalidOperationException($"The source area ({sourceArea}) and target area ({targetArea}) must be the same size.");
            }

            // apply tilesheets
            IDictionary<TileSheet, TileSheet> tilesheetMap = new Dictionary<TileSheet, TileSheet>();
            foreach (TileSheet sourceSheet in source.TileSheets)
            {
                // copy tilesheets
                TileSheet targetSheet = target.GetTileSheet(sourceSheet.Id);
                if (targetSheet == null || this.NormalizeTilesheetPathForComparison(targetSheet.ImageSource) != this.NormalizeTilesheetPathForComparison(sourceSheet.ImageSource))
                {
                    // change ID if needed so new tilesheets are added after vanilla ones (to avoid errors in hardcoded game logic)
                    string id = sourceSheet.Id;
                    if (!id.StartsWith("z_", StringComparison.OrdinalIgnoreCase))
                        id = $"z_{id}";

                    // change ID if it conflicts with an existing tilesheet
                    if (target.GetTileSheet(id) != null)
                    {
                        int disambiguator = Enumerable.Range(2, int.MaxValue - 1).First(p => target.GetTileSheet($"{id}_{p}") == null);
                        id = $"{id}_{disambiguator}";
                    }

                    // add tilesheet
                    targetSheet = new TileSheet(id, target, sourceSheet.ImageSource, sourceSheet.SheetSize, sourceSheet.TileSize);
                    for (int i = 0, tileCount = sourceSheet.TileCount; i < tileCount; ++i)
                        targetSheet.TileIndexProperties[i].CopyFrom(sourceSheet.TileIndexProperties[i]);
                    target.AddTileSheet(targetSheet);
                }

                tilesheetMap[sourceSheet] = targetSheet;
            }

            // get layer map
            IDictionary<Layer, Layer> layerMap = source.Layers.ToDictionary(p => p, p => target.GetLayer(p.Id));

            // apply tiles
            for (int x = 0; x < sourceArea.Value.Width; x++)
            {
                for (int y = 0; y < sourceArea.Value.Height; y++)
                {
                    // calculate tile positions
                    Point sourcePos = new Point(sourceArea.Value.X + x, sourceArea.Value.Y + y);
                    Point targetPos = new Point(targetArea.Value.X + x, targetArea.Value.Y + y);

                    // merge layers
                    foreach (Layer sourceLayer in source.Layers)
                    {
                        // get layer
                        Layer targetLayer = layerMap[sourceLayer];
                        if (targetLayer == null)
                        {
                            target.AddLayer(targetLayer = new Layer(sourceLayer.Id, target, target.Layers[0].LayerSize, Layer.m_tileSize));
                            layerMap[sourceLayer] = target.GetLayer(sourceLayer.Id);
                        }

                        // copy layer properties
                        targetLayer.Properties.CopyFrom(sourceLayer.Properties);

                        // copy tiles
                        Tile sourceTile = sourceLayer.Tiles[sourcePos.X, sourcePos.Y];
                        Tile targetTile;
                        switch (sourceTile)
                        {
                            case StaticTile _:
                                targetTile = new StaticTile(targetLayer, tilesheetMap[sourceTile.TileSheet], sourceTile.BlendMode, sourceTile.TileIndex);
                                break;

                            case AnimatedTile animatedTile:
                                {
                                    StaticTile[] tileFrames = new StaticTile[animatedTile.TileFrames.Length];
                                    for (int frame = 0; frame < animatedTile.TileFrames.Length; ++frame)
                                    {
                                        StaticTile frameTile = animatedTile.TileFrames[frame];
                                        tileFrames[frame] = new StaticTile(targetLayer, tilesheetMap[frameTile.TileSheet], frameTile.BlendMode, frameTile.TileIndex);
                                    }
                                    targetTile = new AnimatedTile(targetLayer, tileFrames, animatedTile.FrameInterval);
                                }
                                break;

                            default: // null or unhandled type
                                targetTile = null;
                                break;
                        }
                        targetTile?.Properties.CopyFrom(sourceTile.Properties);
                        targetLayer.Tiles[targetPos.X, targetPos.Y] = targetTile;
                    }
                }
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Normalize a map tilesheet path for comparison. This value should *not* be used as the actual tilesheet path.</summary>
        /// <param name="path">The path to normalize.</param>
        private string NormalizeTilesheetPathForComparison(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            path = PathUtilities.NormalizePath(path);
            if (path.StartsWith($"Maps{PathUtilities.PreferredPathSeparator}", StringComparison.OrdinalIgnoreCase))
                path = path.Substring($"Maps{PathUtilities.PreferredPathSeparator}".Length);
            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - 4);

            return path;
        }

        /// <summary>Get a rectangle which encompasses all layers for a map.</summary>
        /// <param name="map">The map to check.</param>
        private Rectangle GetMapArea(Map map)
        {
            // get max map size
            int maxWidth = 0;
            int maxHeight = 0;
            foreach (Layer layer in map.Layers)
            {
                if (layer.LayerWidth > maxWidth)
                    maxWidth = layer.LayerWidth;
                if (layer.LayerHeight > maxHeight)
                    maxHeight = layer.LayerHeight;
            }

            return new Rectangle(0, 0, maxWidth, maxHeight);
        }
    }
}
