using System.Collections.Generic;
using SoGModdingAPI.Framework.Reflection;

namespace SoGModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides metadata about installed mods.</summary>
    internal class ModRegistryHelper : BaseHelper, IModRegistry
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying mod registry.</summary>
        private readonly ModRegistry Registry;

        /// <summary>Encapsulates monitoring and logging for the mod.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The mod IDs for APIs accessed by this instanced.</summary>
        private readonly HashSet<string> AccessedModApis = new HashSet<string>();

        /// <summary>Generates proxy classes to access mod APIs through an arbitrary interface.</summary>
        private readonly InterfaceProxyFactory ProxyFactory;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="registry">The underlying mod registry.</param>
        /// <param name="proxyFactory">Generates proxy classes to access mod APIs through an arbitrary interface.</param>
        /// <param name="monitor">Encapsulates monitoring and logging for the mod.</param>
        public ModRegistryHelper(string modID, ModRegistry registry, InterfaceProxyFactory proxyFactory, IMonitor monitor)
            : base(modID)
        {
            this.Registry = registry;
            this.ProxyFactory = proxyFactory;
            this.Monitor = monitor;
        }

        /// <inheritdoc />
        public IEnumerable<IModInfo> GetAll()
        {
            return this.Registry.GetAll();
        }

        /// <inheritdoc />
        public IModInfo Get(string uniqueID)
        {
            return this.Registry.Get(uniqueID);
        }

        /// <inheritdoc />
        public bool IsLoaded(string uniqueID)
        {
            return this.Registry.Get(uniqueID) != null;
        }

        /// <inheritdoc />
        public object GetApi(string uniqueID)
        {
            // validate ready
            if (!this.Registry.AreAllModsInitialized)
            {
                this.Monitor.Log("Tried to access a mod-provided API before all mods were initialized.", LogLevel.Error);
                return null;
            }

            // get raw API
            IModMetadata mod = this.Registry.Get(uniqueID);
            if (mod?.Api != null && this.AccessedModApis.Add(mod.Manifest.UniqueID))
                this.Monitor.Log($"Accessed mod-provided API for {mod.DisplayName}.", LogLevel.Trace);
            return mod?.Api;
        }

        /// <inheritdoc />
        public TInterface GetApi<TInterface>(string uniqueID) where TInterface : class
        {
            // get raw API
            object api = this.GetApi(uniqueID);
            if (api == null)
                return null;

            // validate mapping
            if (!typeof(TInterface).IsInterface)
            {
                this.Monitor.Log($"Tried to map a mod-provided API to class '{typeof(TInterface).FullName}'; must be a public interface.", LogLevel.Error);
                return null;
            }
            if (!typeof(TInterface).IsPublic)
            {
                this.Monitor.Log($"Tried to map a mod-provided API to non-public interface '{typeof(TInterface).FullName}'; must be a public interface.", LogLevel.Error);
                return null;
            }

            // get API of type
            if (api is TInterface castApi)
                return castApi;
            return this.ProxyFactory.CreateProxy<TInterface>(api, this.ModID, uniqueID);
        }
    }
}
