namespace SoG.ModLoader.SaveConverter
{
    /// <summary>
    /// Associate an identifier with a mod.
    /// </summary>
    /// <typeparam name="T">Id type</typeparam>
    public class ModId<T>
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

        public ModId(T value) : this(0, value)
        { }

        public ModId(int mod, T value)
        {
            Mod = mod;
            Value = value;
        }
    }
}
