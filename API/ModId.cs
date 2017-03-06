namespace SoG.ModLoader.API
{
    /// <summary>
    /// Associate an identifier with a mod.
    /// </summary>
    /// <typeparam name="T">Id type</typeparam>
    public interface IModId<T>
    {
        /// <summary>
        /// Mod instance.
        /// </summary>
        IMod Mod
        {
            get;
        }

        /// <summary>
        /// Id value.
        /// </summary>
        T Value
        {
            get;
        }
    }

    /// <summary>
    /// Associate an identifier with a mod.
    /// </summary>
    /// <typeparam name="T">Id type</typeparam>
    public class ModId<T> : IModId<T>
    {
        /// <summary>
        /// Mod instance.
        /// </summary>
        public IMod Mod
        {
            get;
            private set;
        }

        /// <summary>
        /// Id value.
        /// </summary>
        public T Value
        {
            get;
            private set;
        }

        /// <summary>
        /// Get an id.
        /// </summary>
        /// <param name="value">Id value</param>
        /// <returns>Id</returns>
        public static ModId<T> Get(T value)
        {
            return new ModId<T>(value);
        }

        /// <summary>
        /// Get an id.
        /// </summary>
        /// <param name="mod">Mod instance</param>
        /// <param name="value">Id value</param>
        /// <returns>Id</returns>
        public static ModId<T> Get(IMod mod, T value)
        {
            return new ModId<T>(mod, value);
        }

        private ModId(T value) : this(null, value)
        { }

        private ModId(IMod mod, T value)
        {
            Mod = mod;
            Value = value;
        }
    }
}
