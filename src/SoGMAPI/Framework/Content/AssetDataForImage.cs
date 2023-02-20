using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoG;

namespace SoGModdingAPI.Framework.Content
{
    /// <summary>Encapsulates access and changes to image content being read from a data file.</summary>
    internal class AssetDataForImage : AssetData<Texture2D>, IAssetDataForImage
    {
        /*********
        ** Fields
        *********/
        /// <summary>The minimum value to consider non-transparent.</summary>
        /// <remarks>On Linux/macOS, fully transparent pixels may have an alpha up to 4 for some reason.</remarks>
        private const byte MinOpacity = 5;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="locale">The content's locale code, if the content is localized.</param>
        /// <param name="assetName">The asset name being read.</param>
        /// <param name="data">The content data being read.</param>
        /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
        /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
        public AssetDataForImage(string? locale, IAssetName assetName, Texture2D data, Func<string, string> getNormalizedPath, Action<Texture2D> onDataReplaced)
            : base(locale, assetName, data, getNormalizedPath, onDataReplaced) { }

        /// <inheritdoc />
        public void PatchImage(IRawTextureData source, Rectangle? sourceArea = null, Rectangle? targetArea = null, PatchMode patchMode = PatchMode.Replace)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Can't patch from null source data.");

            // get normalized bounds
            this.GetPatchBounds(ref sourceArea, ref targetArea, source.Width, source.Height);
            if (source.Data.Length < (sourceArea.Value.Bottom - 1) * source.Width + sourceArea.Value.Right)
                throw new ArgumentException("Can't apply image patch because the source image is smaller than the source area.", nameof(source));
            int areaX = sourceArea.Value.X;
            int areaY = sourceArea.Value.Y;
            int areaWidth = sourceArea.Value.Width;
            int areaHeight = sourceArea.Value.Height;

            // shortcut: if the area width matches the source image, we can apply the image as-is without needing
            // to copy the pixels into a smaller subset. It's fine if the source is taller than the area, since we'll
            // just ignore the extra data at the end of the pixel array.
            if (areaWidth == source.Width)
            {
                this.PatchImageImpl(source.Data, source.Width, source.Height, sourceArea.Value, targetArea.Value, patchMode, areaY);
                return;
            }

            // else copy the pixels within the smaller area & apply that
            int pixelCount = areaWidth * areaHeight;
            Color[] sourceData = ArrayPool<Color>.Shared.Rent(pixelCount);
            try
            {
                for (int y = areaY, maxY = areaY + areaHeight; y < maxY; y++)
                {
                    int sourceIndex = (y * source.Width) + areaX;
                    int targetIndex = (y - areaY) * areaWidth;
                    Array.Copy(source.Data, sourceIndex, sourceData, targetIndex, areaWidth);
                }

                this.PatchImageImpl(sourceData, source.Width, source.Height, sourceArea.Value, targetArea.Value, patchMode);
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(sourceData);
            }
        }

        /// <inheritdoc />
        public void PatchImage(Texture2D source, Rectangle? sourceArea = null, Rectangle? targetArea = null, PatchMode patchMode = PatchMode.Replace)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Can't patch from a null source texture.");

            // get normalized bounds
            this.GetPatchBounds(ref sourceArea, ref targetArea, source.Width, source.Height);
            if (!source.Bounds.Contains(sourceArea.Value))
                throw new ArgumentOutOfRangeException(nameof(sourceArea), "The source area is outside the bounds of the source texture.");

            // get source data & apply
            int pixelCount = sourceArea.Value.Width * sourceArea.Value.Height;
            Color[] sourceData = ArrayPool<Color>.Shared.Rent(pixelCount);
            try
            {
                source.GetData(0, sourceArea, sourceData, 0, pixelCount);
                this.PatchImageImpl(sourceData, source.Width, source.Height, sourceArea.Value, targetArea.Value, patchMode);
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(sourceData);
            }
        }

        /// <inheritdoc />
        public bool ExtendImage(int minWidth, int minHeight)
        {
            if (this.Data.Width >= minWidth && this.Data.Height >= minHeight)
                return false;

            Texture2D original = this.Data;
            Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, Math.Max(original.Width, minWidth), Math.Max(original.Height, minHeight)).SetName(original.Name);
            this.ReplaceWith(texture);
            this.PatchImage(original);
            return true;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the bounds for an image patch.</summary>
        /// <param name="sourceArea">The source area to set if needed.</param>
        /// <param name="targetArea">The target area to set if needed.</param>
        /// <param name="sourceWidth">The width of the full source image.</param>
        /// <param name="sourceHeight">The height of the full source image.</param>
        private void GetPatchBounds([NotNull] ref Rectangle? sourceArea, [NotNull] ref Rectangle? targetArea, int sourceWidth, int sourceHeight)
        {
            sourceArea ??= new Rectangle(0, 0, sourceWidth, sourceHeight);
            targetArea ??= new Rectangle(0, 0, Math.Min(sourceArea.Value.Width, this.Data.Width), Math.Min(sourceArea.Value.Height, this.Data.Height));
        }

        /// <summary>Overwrite part of the image.</summary>
        /// <param name="sourceData">The image data to patch into the content.</param>
        /// <param name="sourceWidth">The pixel width of the original source image.</param>
        /// <param name="sourceHeight">The pixel height of the original source image.</param>
        /// <param name="sourceArea">The part of the <paramref name="sourceData"/> to copy (or <c>null</c> to take the whole texture). This must be within the bounds of the <paramref name="sourceData"/> texture.</param>
        /// <param name="targetArea">The part of the content to patch (or <c>null</c> to patch the whole texture). The original content within this area will be erased. This must be within the bounds of the existing spritesheet.</param>
        /// <param name="patchMode">Indicates how an image should be patched.</param>
        /// <param name="startRow">The row to start on, for the sourceData.</param>
        /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="targetArea"/> is outside the bounds of the spritesheet.</exception>
        /// <exception cref="InvalidOperationException">The content being read isn't an image.</exception>
        private void PatchImageImpl(Color[] sourceData, int sourceWidth, int sourceHeight, Rectangle sourceArea, Rectangle targetArea, PatchMode patchMode, int startRow = 0)
        {
            // get texture info
            Texture2D target = this.Data;
            int pixelCount = sourceArea.Width * sourceArea.Height;
            int firstPixel = startRow * sourceArea.Width;
            int lastPixel = firstPixel + pixelCount - 1;

            // validate
            if (sourceArea.X < 0 || sourceArea.Y < 0 || sourceArea.Right > sourceWidth || sourceArea.Bottom > sourceHeight)
                throw new ArgumentOutOfRangeException(nameof(sourceArea), "The source area is outside the bounds of the source texture.");
            if (!target.Bounds.Contains(targetArea))
                throw new ArgumentOutOfRangeException(nameof(targetArea), "The target area is outside the bounds of the target texture.");
            if (sourceArea.Size != targetArea.Size)
                throw new InvalidOperationException("The source and target areas must be the same size.");

            // shortcut: replace the entire area
            if (patchMode == PatchMode.Replace)
            {
                target.SetData(0, targetArea, sourceData, firstPixel, pixelCount);
                return;
            }

            // skip transparent pixels at the start & end (e.g. large spritesheet with a few sprites replaced)
            int startIndex = -1;
            int endIndex = -1;
            for (int i = firstPixel; i <= lastPixel; i++)
            {
                if (sourceData[i].A >= AssetDataForImage.MinOpacity)
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex == -1)
                return; // blank texture

            for (int i = lastPixel; i >= startIndex; i--)
            {
                if (sourceData[i].A >= AssetDataForImage.MinOpacity)
                {
                    endIndex = i;
                    break;
                }
            }
            if (endIndex == -1)
                return; // ???

            // update target rectangle
            int sourceOffset;
            {
                int topOffset = startIndex / sourceArea.Width;
                int bottomOffset = endIndex / sourceArea.Width;

                targetArea = new(targetArea.X, targetArea.Y + topOffset - startRow, targetArea.Width, bottomOffset - topOffset + 1);
                pixelCount = targetArea.Width * targetArea.Height;
                sourceOffset = topOffset * sourceArea.Width;
            }

            // apply
            Color[] mergedData = ArrayPool<Color>.Shared.Rent(pixelCount);
            try
            {
                target.GetData(0, targetArea, mergedData, 0, pixelCount);

                for (int i = startIndex; i <= endIndex; i++)
                {
                    int targetIndex = i - sourceOffset;

                    Color above = sourceData[i];
                    Color below = mergedData[targetIndex];

                    // shortcut transparency
                    if (above.A < AssetDataForImage.MinOpacity)
                        continue;
                    if (below.A < AssetDataForImage.MinOpacity || above.A == byte.MaxValue)
                        mergedData[targetIndex] = above;

                    // merge pixels
                    else
                    {
                        // This performs a conventional alpha blend for the pixels, which are already
                        // premultiplied by the content pipeline. The formula is derived from
                        // https://blogs.msdn.microsoft.com/shawnhar/2009/11/06/premultiplied-alpha/.
                        float alphaBelow = 1 - (above.A / 255f);
                        mergedData[targetIndex] = new Color(
                            r: (int)(above.R + (below.R * alphaBelow)),
                            g: (int)(above.G + (below.G * alphaBelow)),
                            b: (int)(above.B + (below.B * alphaBelow)),
                            alpha: Math.Max(above.A, below.A)
                        );
                    }
                }

                target.SetData(0, targetArea, mergedData, 0, pixelCount);
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(mergedData);
            }
        }
    }
}
