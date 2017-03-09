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

        bool ConvertFrom(ISave other);

        bool ConvertTo(ISave other);
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

        public abstract bool ConvertFrom(ISave other);

        public abstract bool ConvertTo(ISave other);
    }

    public static class Save
    {
        public static string v0_675a = "v0.675a";
        public static string m0_675a = "m0.675a";

        public static IEnumerable<string> Versions
        {
            get
            {
                yield return v0_675a;
                yield return m0_675a;
            }
        }
    }
}
