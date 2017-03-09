using System.Collections.Generic;

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

            public class SoldItem
            {
                public int id;
                public int value;
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

            public class OwnedPet
            {
                public int id;
                public int npcType;
                public string name;
                public byte level;
                public byte skin;
                public ushort statLevelHp;
                public ushort statLevelSp;
                public ushort statLevelDamage;
                public ushort statLevelCrit;
                public ushort statLevelSpeed;
                public ushort statLevelProgressHp;
                public ushort statLevelProgressSp;
                public ushort statLevelProgressDamage;
                public ushort statLevelProgressCrit;
                public ushort statLevelProgressSpeed;
            }

            public class SubGrade
            {
                public byte gradeType;
                public byte gradeValue;
                public int value;
            }

            public class ChallengeRecord
            {
                public ushort id;
                public byte totalGrade;
                public List<SubGrade> subGrades = new List<SubGrade>();
            }

            public class LocalHousingConfiguration
            {
                public byte key;
                public byte[] value;
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
            public List<SoldItem> soldItems = new List<SoldItem>();
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
            public List<OwnedPet> ownedPets = new List<OwnedPet>();
            public int activePetNpcType;
            public bool hidePet;
            public List<ushort> questsCompletedOnThisCharacter = new List<ushort>();
            public List<int> knownEnemies = new List<int>();
            public List<ChallengeRecord> challengeRecords = new List<ChallengeRecord>();
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
            public List<KeyValuePair<string, float>> arbitraryPersistentCharacterValues = new List<KeyValuePair<string, float>>();
            public List<LocalHousingConfiguration> localHousingConfigurations = new List<LocalHousingConfiguration>();

            public Character_v0_675a() : base(SaveConverter.Save.v0_675a)
            { }

            private T ConvertModId<T>(ModId<T> id, T value)
            {
                if (id.Mod != 0)
                    return value;
                else
                    return id.Value;
            }

            public override bool ConvertFrom(ISave other)
            {
                if (other.Version == SaveConverter.Save.m0_675a)
                    return Convert((ModLoader.Character_m0_675a)other);
                return false;
            }

            public override bool ConvertTo(ISave other)
            {
                if (other.Version == SaveConverter.Save.m0_675a)
                    return other.ConvertFrom(this);
                return false;
            }

            public bool Convert(ModLoader.Character_m0_675a other)
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
                hairColor = ConvertModId(other.hairColor, (byte)1);
                skinColor = ConvertModId(other.skinColor, (byte)1);
                ponchoColor = ConvertModId(other.ponchoColor, (byte)1);
                shirtColor = ConvertModId(other.shirtColor, (byte)1);
                pantColor = ConvertModId(other.pantColor, (byte)1);
                isMale = other.isMale;
                networkNickname = other.networkNickname;
                inventory = other.inventory;
                pickupNumberPool = other.pickupNumberPool;
                soldItems = other.soldItems;
                cardAlbum = other.cardAlbum;
                treasureMaps = other.treasureMaps;
                foundTreasures = other.foundTreasures;
                skills = other.skills;
                level = other.level;
                exp = other.exp;
                expForNext = other.expForNext;
                expForPrevious = other.expForPrevious;
                talentPoints = other.talentPoints;
                skillPointsSilver = other.skillPointsSilver;
                skillPointsGold = other.skillPointsGold;
                money = other.money;
                ownedPets = other.ownedPets;
                activePetNpcType = other.activePetNpcType;
                hidePet = other.hidePet;
                questsCompletedOnThisCharacter = other.questsCompletedOnThisCharacter;
                knownEnemies = other.knownEnemies;
                challengeRecords = other.challengeRecords;
                arrowGameHighscore = other.arrowGameHighscore;
                awardedTrophies = other.awardedTrophies;
                uniqueDiscoveredItems = other.uniqueDiscoveredItems;
                uniqueCraftedItem = other.uniqueCraftedItem;
                uniqueFishy = other.uniqueFishy;
                monsterKilled = other.monsterKilled;
                birthdayMonth = other.birthdayMonth;
                birthdayDay = other.birthdayDay;
                collectorId = other.collectorId;
                lastAutosaveAt = other.lastAutosaveAt;
                saveCarousel = other.saveCarousel;
                timePlayed = other.timePlayed;
                currentPhaseShiftShape = other.currentPhaseShiftShape;
                arbitraryPersistentCharacterValues = other.arbitraryPersistentCharacterValues;
                localHousingConfigurations = other.localHousingConfigurations;
                return true;
            }
        }
    }

    namespace ModLoader
    {
        public class Character_m0_675a : SaveBase
        {
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
            public Vanilla.Character_v0_675a.Quickslot[] quickslots = new Vanilla.Character_v0_675a.Quickslot[10];
            public ModId<byte> hairColor;
            public ModId<byte> skinColor;
            public ModId<byte> ponchoColor;
            public ModId<byte> shirtColor;
            public ModId<byte> pantColor;
            public bool isMale;
            public string networkNickname;
            public List<Vanilla.Character_v0_675a.InventoryItem> inventory = new List<Vanilla.Character_v0_675a.InventoryItem>();
            public int pickupNumberPool;
            public List<Vanilla.Character_v0_675a.SoldItem> soldItems = new List<Vanilla.Character_v0_675a.SoldItem>();
            public List<int> cardAlbum = new List<int>();
            public List<ushort> treasureMaps = new List<ushort>();
            public List<ushort> foundTreasures = new List<ushort>();
            public List<Vanilla.Character_v0_675a.Skill> skills = new List<Vanilla.Character_v0_675a.Skill>();
            public ushort level;
            public uint exp;
            public uint expForNext;
            public uint expForPrevious;
            public ushort talentPoints;
            public ushort skillPointsSilver;
            public ushort skillPointsGold;
            public int money;
            public List<Vanilla.Character_v0_675a.OwnedPet> ownedPets = new List<Vanilla.Character_v0_675a.OwnedPet>();
            public int activePetNpcType;
            public bool hidePet;
            public List<ushort> questsCompletedOnThisCharacter = new List<ushort>();
            public List<int> knownEnemies = new List<int>();
            public List<Vanilla.Character_v0_675a.ChallengeRecord> challengeRecords = new List<Vanilla.Character_v0_675a.ChallengeRecord>();
            public int arrowGameHighscore;
            public List<ushort> awardedTrophies = new List<ushort>();
            public List<int> uniqueDiscoveredItems = new List<int>();
            public List<int> uniqueCraftedItem = new List<int>();
            public List<int> uniqueFishy = new List<int>();
            public List<Vanilla.Character_v0_675a.MonsterKilled> monsterKilled = new List<Vanilla.Character_v0_675a.MonsterKilled>();
            public int birthdayMonth;
            public int birthdayDay;
            public int collectorId;
            public uint lastAutosaveAt;
            public int saveCarousel;
            public uint timePlayed;
            public byte currentPhaseShiftShape;
            public List<KeyValuePair<string, float>> arbitraryPersistentCharacterValues = new List<KeyValuePair<string, float>>();
            public List<Vanilla.Character_v0_675a.LocalHousingConfiguration> localHousingConfigurations = new List<Vanilla.Character_v0_675a.LocalHousingConfiguration>();

            public Character_m0_675a() : base(SaveConverter.Save.m0_675a)
            { }

            public override bool ConvertFrom(ISave other)
            {
                if (other.Version == SaveConverter.Save.v0_675a)
                    return Convert((Vanilla.Character_v0_675a)other);
                return false;
            }

            public override bool ConvertTo(ISave other)
            {
                if (other.Version == SaveConverter.Save.v0_675a)
                    return other.ConvertFrom(this);
                return false;
            }

            public bool Convert(Vanilla.Character_v0_675a other)
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
                quickslots = other.quickslots;
                hairColor = new ModId<byte>(other.hairColor);
                skinColor = new ModId<byte>(other.skinColor);
                ponchoColor = new ModId<byte>(other.ponchoColor);
                shirtColor = new ModId<byte>(other.shirtColor);
                pantColor = new ModId<byte>(other.pantColor);
                isMale = other.isMale;
                networkNickname = other.networkNickname;
                inventory = other.inventory;
                pickupNumberPool = other.pickupNumberPool;
                soldItems = other.soldItems;
                cardAlbum = other.cardAlbum;
                treasureMaps = other.treasureMaps;
                foundTreasures = other.foundTreasures;
                skills = other.skills;
                level = other.level;
                exp = other.exp;
                expForNext = other.expForNext;
                expForPrevious = other.expForPrevious;
                talentPoints = other.talentPoints;
                skillPointsSilver = other.skillPointsSilver;
                skillPointsGold = other.skillPointsGold;
                money = other.money;
                ownedPets = other.ownedPets;
                activePetNpcType = other.activePetNpcType;
                hidePet = other.hidePet;
                questsCompletedOnThisCharacter = other.questsCompletedOnThisCharacter;
                knownEnemies = other.knownEnemies;
                challengeRecords = other.challengeRecords;
                arrowGameHighscore = other.arrowGameHighscore;
                awardedTrophies = other.awardedTrophies;
                uniqueDiscoveredItems = other.uniqueDiscoveredItems;
                uniqueCraftedItem = other.uniqueCraftedItem;
                uniqueFishy = other.uniqueFishy;
                monsterKilled = other.monsterKilled;
                birthdayMonth = other.birthdayMonth;
                birthdayDay = other.birthdayDay;
                collectorId = other.collectorId;
                lastAutosaveAt = other.lastAutosaveAt;
                saveCarousel = other.saveCarousel;
                timePlayed = other.timePlayed;
                currentPhaseShiftShape = other.currentPhaseShiftShape;
                arbitraryPersistentCharacterValues = other.arbitraryPersistentCharacterValues;
                localHousingConfigurations = other.localHousingConfigurations;
                return true;
            }
        }
    }
}
