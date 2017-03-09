using SoG.ModLoader.API;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace SoG.ModLoader.Mods.Samples.Skin
{
    public class Mod : ModBase
    {
        public override string Author
        {
            get { return "Nauja"; }
        }

        public override string UniqueName
        {
            get { return "SoG.ModLoader.Mods.Samples.Skin"; }
        }

        public override string Name
        {
            get { return "Skin"; }
        }

        public override void OnLoad(IModLoader modLoader)
        {
            base.OnLoad(modLoader);
            for (int i = 1; i <= 6; ++i)
                modLoader.PlayerPaletteAPI.RegisterPlayerPalette(new PlayerPalette(null, (byte)(i - 1), PlayerPalette.Type.Skin, "Bluish", Path.Combine(Directory, "Skins/0" + i)));
        }
    }
}
