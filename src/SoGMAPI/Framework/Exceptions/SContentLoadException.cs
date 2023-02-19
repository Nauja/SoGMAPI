using System;
using Microsoft.Xna.Framework.Content;

namespace SoGModdingAPI.Framework.Exceptions
{
    /// <summary>An implementation of <see cref="ContentLoadException"/> used by SoGMAPI to detect whether it was thrown by SoGMAPI or the underlying framework.</summary>
    internal class SContentLoadException : ContentLoadException
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Why loading the asset through the content pipeline failed.</summary>
        public ContentLoadErrorType ErrorType { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="errorType">Why loading the asset through the content pipeline failed.</param>
        /// <param name="message">The error message.</param>
        /// <param name="ex">The underlying exception, if any.</param>
        public SContentLoadException(ContentLoadErrorType errorType, string message, Exception? ex = null)
            : base(message, ex)
        {
            this.ErrorType = errorType;
        }
    }
}
