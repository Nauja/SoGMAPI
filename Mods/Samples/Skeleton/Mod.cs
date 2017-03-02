using SoG.ModLoader.API;

namespace SoG.ModLoader.Mods.Samples.Skeleton
{
    public class Mod : ModBase
    {
        public override string Author
        {
            get { return "Nauja"; }
        }

        public override string Name
        {
            get { return "Skeleton"; }
        }

        public override void OnLoad(IModLoader modLoader)
        {
            base.OnLoad(modLoader);
            Logger.Log("Hello World !");
        }
    }
}
