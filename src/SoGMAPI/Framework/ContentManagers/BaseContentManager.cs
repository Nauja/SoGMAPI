using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SoGModdingAPI.Framework.Content;
using SoGModdingAPI.Framework.Exceptions;
using SoGModdingAPI.Framework.Reflection;
using static SoGModdingAPI.Framework.LocalizedContentManager;

namespace SoGModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files from a SMAPI mod folder with support for unpacked files.</summary>
    internal abstract class BaseContentManager : LocalizedContentManager, IContentManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>The central coordinator which manages content managers.</summary>
        protected readonly ContentCoordinator Coordinator;

        /// <summary>The underlying asset cache.</summary>
        protected readonly ContentCache Cache;

        /// <summary>Encapsulates monitoring and logging.</summary>
        protected readonly IMonitor Monitor;

        /// <summary>Whether to enable more aggressive memory optimizations.</summary>
        protected readonly bool AggressiveMemoryOptimizations;

        /// <summary>Whether the content coordinator has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>A callback to invoke when the content manager is being disposed.</summary>
        private readonly Action<BaseContentManager> OnDisposing;

        /// <summary>The language enum values indexed by locale code.</summary>
        protected IDictionary<string, LanguageCode> LanguageCodes { get; }

        /// <summary>A list of disposable assets.</summary>
        private readonly List<WeakReference<IDisposable>> Disposables = new List<WeakReference<IDisposable>>();

        /// <summary>The disposable assets tracked by the base content manager.</summary>
        /// <remarks>This should be kept empty to avoid keeping disposable assets referenced forever, which prevents garbage collection when they're unused. Disposable assets are tracked by <see cref="Disposables"/> instead, which avoids a hard reference.</remarks>
        private readonly List<IDisposable> BaseDisposableReferences;


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public LanguageCode Language => LanguageCode.en;

        /// <inheritdoc />
        public bool IsNamespaced { get; }


        /// <inheritdoc />
        public virtual void OnLocaleChanged() { }

        /// <inheritdoc />
        [Pure]
        public string NormalizePathSeparators(string path)
        {
            return this.Cache.NormalizePathSeparators(path);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "Parameter is only used for assertion checks by design.")]
        public string AssertAndNormalizeAssetName(string assetName)
        {
            // NOTE: the game checks for ContentLoadException to handle invalid keys, so avoid
            // throwing other types like ArgumentException here.
            if (string.IsNullOrWhiteSpace(assetName))
                throw new SContentLoadException("The asset key or local path is empty.");
            if (assetName.Intersect(Path.GetInvalidPathChars()).Any())
                throw new SContentLoadException("The asset key or local path contains invalid characters.");

            return this.Cache.NormalizeKey(assetName);
        }

        /****
        ** Content loading
        ****/
        /// <inheritdoc />
        public string GetLocale()
        {
            return this.GetLocale(LanguageCode.en);
        }

        /// <inheritdoc />
        public string GetLocale(LanguageCode language)
        {
            return "en";
        }

        public void Dispose()
        {
        }
    }
}
