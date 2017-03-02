namespace SoG.ModLoader.API
{
    /// <summary>
    /// Interface for the logging API.
    /// </summary>
    public interface ILoggerAPI
    {
        /// <summary>
        /// Write the string representation of a value.
        /// </summary>
        /// <param name="value">Value</param>
        void Log(object value);

        /// <summary>
        /// Write the string formatting of multiple values.
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="arg">Values</param>
        void Log(string format, params object[] arg);
    }
}
