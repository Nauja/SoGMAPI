namespace SoG.ModLoader.SaveConverter
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
        int Mod
        {
            get;
            set;
        }

        /// <summary>
        /// Id value.
        /// </summary>
        T Value
        {
            get;
            set;
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
        public int Mod
        {
            get;
            set;
        }

        /// <summary>
        /// Id value.
        /// </summary>
        public T Value
        {
            get;
            set;
        }

        private ModId(T value) : this(0, value)
        { }

        private ModId(int mod, T value)
        {
            Mod = mod;
            Value = value;
        }
    }
}
