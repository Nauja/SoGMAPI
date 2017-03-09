namespace SoG.ModLoader.API
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

        public ModId(T value) : this(null, value)
        { }

        public ModId(IMod mod, T value)
        {
            Mod = mod;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (this == obj)
                return true;
            var o = obj as ModId<T>;
            if (o == null)
                return false;
            if (Mod != o.Mod)
                return false;
            if (!Value.Equals(o.Value))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int result = 17;
            result = 31 * result + (Mod == null ? 0 : Mod.Id);
            result = 31 * result + Value.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            return string.Format("ModId({0}, {1})", Mod == null ? "null" : Mod.Name, Value);
        }
    }
}
