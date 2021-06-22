using System.Diagnostics.Contracts;
using ToolkitPathUtilities = SoGModdingAPI.Toolkit.Utilities.PathUtilities;

namespace SoGModdingAPI.Utilities
{
    /// <summary>Provides utilities for normalizing file paths.</summary>
    public static class PathUtilities
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get the segments from a path (e.g. <c>/usr/bin/example</c> => <c>usr</c>, <c>bin</c>, and <c>example</c>).</summary>
        /// <param name="path">The path to split.</param>
        /// <param name="limit">The number of segments to match. Any additional segments will be merged into the last returned part.</param>
        [Pure]
        public static string[] GetSegments(string path, int? limit = null)
        {
            return ToolkitPathUtilities.GetSegments(path, limit);
        }

        /// <summary>Normalize separators in a file path.</summary>
        /// <param name="path">The file path to normalize.</param>
        [Pure]
        public static string NormalizePath(string path)
        {
            return ToolkitPathUtilities.NormalizePath(path);
        }

        /// <summary>Get whether a path is relative and doesn't try to climb out of its containing folder (e.g. doesn't contain <c>../</c>).</summary>
        /// <param name="path">The path to check.</param>
        [Pure]
        public static bool IsSafeRelativePath(string path)
        {
            return ToolkitPathUtilities.IsSafeRelativePath(path);
        }

        /// <summary>Get whether a string is a valid 'slug', containing only basic characters that are safe in all contexts (e.g. filenames, URLs, etc).</summary>
        /// <param name="str">The string to check.</param>
        [Pure]
        public static bool IsSlug(string str)
        {
            return ToolkitPathUtilities.IsSlug(str);
        }
    }
}
