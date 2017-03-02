namespace SoG.ModLoader.API
{
    public abstract class ModBase : IMod
    {
        #region Infos
        public virtual string Author
        {
            get;
            protected set;
        }

        public virtual string Name
        {
            get;
            protected set;
        }
        #endregion
        
        public IModLoader ModLoader
        {
            get;
            private set;
        }

        public ILoggerAPI Logger
        {
            get { return ModLoader?.LoggerAPI; }
        }

        public string Directory
        {
            get { return ModLoader?.GetModDirectory(this); }
        }

        public virtual void OnLoad(IModLoader modLoader)
        {
            ModLoader = modLoader;
        }
    }
}
