namespace SoG.ModLoader.API
{
    namespace PlayerPalette
    { 
        public enum Type : byte
        {
            Skin,
            Poncho,
            Shirt,
            Pant,
            Shoe,
            Hair
        }
    }

    /// <summary>
    /// Interface for player palettes.
    /// </summary>
    public interface IPlayerPalette
    {
        /// <summary>
        /// Type.
        /// </summary>
        PlayerPalette.Type Type
        {
            get;
        }

        /// <summary>
        /// Id.
        /// </summary>
        int Id
        {
            get;
        }

        /// <summary>
        /// Name.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Path to texture.
        /// </summary>
        string TexturePath
        {
            get;
        }
    }
}
