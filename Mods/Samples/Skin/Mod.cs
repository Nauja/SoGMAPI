using SoG.ModLoader.API;
using System.IO;

namespace SoG.ModLoader.Mods.Samples.Skin
{
    public class Mod : ModBase
    {
        public override string Author
        {
            get { return "Nauja"; }
        }

        public override string Name
        {
            get { return "Skin"; }
        }

        public override void OnLoad(IModLoader modLoader)
        {
            base.OnLoad(modLoader);
            modLoader.PlayerPaletteAPI.RegisterPlayerPalette(API.PlayerPalette.Type.Skin, "Bluish", Path.Combine(Directory, "BluishSkin"));
        }
    }
}
