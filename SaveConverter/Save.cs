using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SoG.ModLoader.SaveConverter
{
    public interface ISave
    {
        bool IsModLoader
        {
            get;
        }

        bool IsVanilla
        {
            get;
        }

        string Version
        {
            get;
        }
    }

    public abstract class SaveBase : ISave
    {
        public bool IsModLoader
        {
            get { return Version != null && Version.Length > 0 && Version[0] == 'm'; }
        }

        public bool IsVanilla
        {
            get { return Version != null && Version.Length > 0 && Version[0] == 'v'; }
        }

        public string Version
        {
            get;
            private set;
        }

        public SaveBase(string version)
        {
            Version = version;
        }
    }

    public static class Save
    {
        public static string v0_675a = "v0.675a";
        public static string m0_675a = "m0.675a";

        public static T Load<T>(string path)
        {

        }
    }
}
