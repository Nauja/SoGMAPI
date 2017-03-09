using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SoG.ModLoader.API
{
    /// <summary>
    /// Interface for player palettes.
    /// </summary>
    public interface IPlayerPalette
    {
        /// <summary>
        /// Id.
        /// </summary>
        PlayerPalette.ModId Id
        {
            get;
        }

        /// <summary>
        /// Category.
        /// </summary>
        PlayerPalette.Type Category
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
        /// Pixels colors.
        /// </summary>
        Vector4[] Colors
        {
            get;
        }

        /// <summary>
        /// Portrait color.
        /// </summary>
        string PortraitColor
        {
            get;
        }

        /// <summary>
        /// Load the player palette.
        /// </summary>
        /// <param name="contentManager">Content manager</param>
        void Load(ContentManager contentManager);
    }

    /// <summary>
    /// Player palette.
    /// </summary>
    public class PlayerPalette : IPlayerPalette
    {
        /// <summary>
        /// Mod id for player palettes.
        /// </summary>
        public class ModId : ModId<byte>
        {
            public ModId(byte value) : base(value) { }
            public ModId(IMod mod, byte value) : base(mod, value) { }
        }

        /// <summary>
        /// Categories of player palettes.
        /// </summary>
        public enum Type : byte
        {
            Skin,
            Poncho,
            Shirt,
            Pant,
            Shoe,
            Hair
        }

        public virtual ModId Id
        {
            get;
            private set;
        }

        public virtual Type Category
        {
            get;
            private set;
        }

        public virtual string Name
        {
            get;
            private set;
        }

        public virtual string TexturePath
        {
            get;
            private set;
        }

        public virtual Vector4[] Colors
        {
            get;
            private set;
        }

        public virtual string PortraitColor
        {
            get;
            private set;
        }

        public PlayerPalette(byte id, Type category, string name, string texturePath, string portraitColor = null) : this(null, id, category, name, texturePath, portraitColor)
        { }
        
        public PlayerPalette(IMod mod, byte id, Type category, string name, string texturePath, string portraitColor = null) : this(new ModId(mod, id), category, name, texturePath, portraitColor)
        { }
        
        public PlayerPalette(ModId id, Type category, string name, string texturePath, string portraitColor = null)
        {
            Id = id;
            Category = category;
            Name = name;
            TexturePath = texturePath;
            PortraitColor = portraitColor;
        }

        public PlayerPalette(byte id, Type category, string name, Vector4[] colors, string portraitColor = null) : this(null, id, category, name, colors, portraitColor)
        { }

        public PlayerPalette(IMod mod, byte id, Type category, string name, Vector4[] colors, string portraitColor = null) : this(new ModId(mod, id), category, name, colors, portraitColor)
        { }

        public PlayerPalette(ModId id, Type category, string name, Vector4[] colors, string portraitColor = null)
        {
            Id = id;
            Category = category;
            Name = name;
            Colors = colors;
            PortraitColor = portraitColor;
        }

        public static Vector4[] GetColors(Texture2D texture)
        {
            Color[] data = new Color[texture.Width];
            texture.GetData(data);
            var colors = new Vector4[data.Length];
            for (int index = 0; index < data.Length; ++index)
                colors[index] = data[index].ToVector4();
            return colors;
        }

        public virtual void Load(ContentManager contentManager)
        {
            if (TexturePath == null)
                return;
            var texture = contentManager.Load<Texture2D>(TexturePath);
            Colors = GetColors(texture);
        }
    }
}
