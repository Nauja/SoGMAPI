using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoG.ModLoader.SaveConverter
{
    namespace Vanilla
    {
        public class Character_v0_675a : SaveBase
        {
            public class Quickslot
            {
                public enum Type
                {
                    Null,
                    Item,
                    Spell
                }

                public Type type;
                public int itemId;
                public ushort spellId;
            }

            public class InventoryItem
            {
                public int id;
                public int amount;
                public uint pickupNumber;
            }

            public class MonsterKilled
            {
                public int id;
                public int amount;
            }

            public class Skill
            {
                public ushort id;
                public byte value;
            }

            public int hatId;
            public int facegearId;
            public char bodyType;
            public int hairdoId;
            public int bufferWeaponId;
            public int shieldId;
            public int armorId;
            public int shoesId;
            public int accessoryA;
            public int accessoryB;
            public int hatStyleId;
            public int facegearStyleId;
            public int weaponStyleId;
            public int shieldStyleId;
            public bool hideHat;
            public bool hideFacegear;
            public int lastOneHand;
            public int lastTwoHand;
            public int lastBow;
            public Quickslot[] quickslots = new Quickslot[10];
            public byte hairColor;
            public byte skinColor;
            public byte ponchoColor;
            public byte shirtColor;
            public byte pantColor;
            public bool isMale;
            public string networkNickname;
            public List<InventoryItem> inventory = new List<InventoryItem>();
            public int pickupNumberPool;
            public List<int> cardAlbum = new List<int>();
            public List<ushort> treasureMaps = new List<ushort>();
            public List<ushort> foundTreasures = new List<ushort>();
            public List<Skill> skills = new List<Skill>();
            public ushort level;
            public uint exp;
            public uint expForNext;
            public uint expForPrevious;
            public ushort talentPoints;
            public ushort skillPointsSilver;
            public ushort skillPointsGold;
            public int money;

            public int arrowGameHighscore;
            public List<ushort> awardedTrophies = new List<ushort>();
            public List<int> uniqueDiscoveredItems = new List<int>();
            public List<int> uniqueCraftedItem = new List<int>();
            public List<int> uniqueFishy = new List<int>();
            public List<MonsterKilled> monsterKilled = new List<MonsterKilled>();
            public int birthdayMonth;
            public int birthdayDay;
            public int collectorId;
            public uint lastAutosaveAt;
            public int saveCarousel;
            public uint timePlayed;
            public byte currentPhaseShiftShape;

            public Character_v0_675a() : base(SaveConverter.Save.v0_675a)
            { }

            public void Convert(ModLoader.Character_m0_675a other)
            {
                hatId = other.hatId;
                facegearId = other.facegearId;
                bodyType = other.bodyType;
                hairdoId = other.hairdoId;
                bufferWeaponId = other.bufferWeaponId;
                shieldId = other.shieldId;
                armorId = other.armorId;
                shoesId = other.shoesId;
                accessoryA = other.accessoryA;
                accessoryB = other.accessoryB;
                hatStyleId = other.hatStyleId;
                facegearStyleId = other.facegearStyleId;
                weaponStyleId = other.weaponStyleId;
                shieldStyleId = other.shieldStyleId;
                hideHat = other.hideHat;
                hideFacegear = other.hideFacegear;
                lastOneHand = other.lastOneHand;
                lastBow = other.lastBow;
                for (var i = 0; i < quickslots.Length; ++i)
                {
                    quickslots[i].type = (Quickslot.Type)other.quickslots[i].type;
                    quickslots[i].itemId = other.quickslots[i].itemId;
                    quickslots[i].spellId = other.quickslots[i].spellId;
                }
                hairColor = (byte)other.hairColor.Value;
                skinColor = (byte)other.skinColor.Value;
                ponchoColor = (byte)other.ponchoColor.Value;
                shirtColor = (byte)other.shirtColor.Value;
                pantColor = (byte)other.pantColor.Value;
            }
        }
    }

    namespace ModLoader
    {
        public class Character_m0_675a : SaveBase
        {
            public class Quickslot
            {
                public enum Type
                {
                    Null,
                    Item,
                    Spell
                }

                public Type type;
                public int itemId;
                public ushort spellId;
            }

            public int hatId;
            public int facegearId;
            public char bodyType;
            public int hairdoId;
            public int bufferWeaponId;
            public int shieldId;
            public int armorId;
            public int shoesId;
            public int accessoryA;
            public int accessoryB;
            public int hatStyleId;
            public int facegearStyleId;
            public int weaponStyleId;
            public int shieldStyleId;
            public bool hideHat;
            public bool hideFacegear;
            public int lastOneHand;
            public int lastTwoHand;
            public int lastBow;
            public Quickslot[] quickslots = new Quickslot[10];
            public IModId<int> hairColor;
            public IModId<int> skinColor;
            public IModId<int> ponchoColor;
            public IModId<int> shirtColor;
            public IModId<int> pantColor;

            public Character_m0_675a() : base(SaveConverter.Save.m0_675a)
            { }

            public void Convert(Vanilla.Character_v0_675a other)
            {
                hatId = other.hatId;
                facegearId = other.facegearId;
                bodyType = other.bodyType;
                hairdoId = other.hairdoId;
                bufferWeaponId = other.bufferWeaponId;
                shieldId = other.shieldId;
                armorId = other.armorId;
                shoesId = other.shoesId;
                accessoryA = other.accessoryA;
                accessoryB = other.accessoryB;
                hatStyleId = other.hatStyleId;
                facegearStyleId = other.facegearStyleId;
                weaponStyleId = other.weaponStyleId;
                shieldStyleId = other.shieldStyleId;
                hideHat = other.hideHat;
                hideFacegear = other.hideFacegear;
                lastOneHand = other.lastOneHand;
                lastBow = other.lastBow;
                for (var i = 0; i < quickslots.Length; ++i)
                {
                    quickslots[i].type = (Quickslot.Type)other.quickslots[i].type;
                    quickslots[i].itemId = other.quickslots[i].itemId;
                    quickslots[i].spellId = other.quickslots[i].spellId;
                }
                hairColor.Value = other.hairColor;
                skinColor.Value = other.skinColor;
                ponchoColor.Value = other.ponchoColor;
                shirtColor.Value = other.shirtColor;
                pantColor.Value = other.pantColor;
            }
        }
    }
}
