using System;
using System.IO;
using System.Collections.Generic;
using SoG.ModLoader.SaveConverter.Vanilla;

namespace SoG.ModLoader.SaveConverter
{
    public interface ICharacterLoader
    {
        ISave Load(BinaryReader br);

        void Save(ISave character, BinaryWriter bw);

        ISave Convert(ISave character);
    }

    public abstract class CharacterLoaderBase<T> : ICharacterLoader where T : ISave, new()
    {
        public abstract ISave Load(BinaryReader br);

        public void Save(ISave character, BinaryWriter bw)
        {
            Save((T)character, bw);
        }

        protected abstract void Save(T character, BinaryWriter bw);

        public virtual ISave Convert(ISave character)
        {
            var res = new T();
            if (res.ConvertFrom(character))
                return res;
            else if (character.ConvertTo(res))
                return res;
            return null;
        }
    }

    public class NullCharacterLoader : ICharacterLoader
    {
        public ISave Convert(ISave character)
        {
            throw new NotImplementedException();
        }

        public ISave Load(BinaryReader br)
        {
            throw new NotImplementedException();
        }

        public void Save(ISave character, BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }

    public static class CharacterLoader
    {
        public static ISave Load(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (var br = new BinaryReader(stream))
                {
                    ICharacterLoader loader = null;
                    var bytes = br.ReadChars(3);
                    // Check header.
                    if (bytes.Length == 3 && bytes[0] == 'M' && bytes[1] == 'O' && bytes[2] == 'D')
                    {
                        var version = br.ReadString();
                        loader = GetForVersion(version);
                    }
                    else
                    {
                        br.BaseStream.Seek(0, SeekOrigin.Begin);
                        var version = br.ReadInt32();
                        br.BaseStream.Seek(0, SeekOrigin.Begin);
                        loader = GetForVersion(version);
                    }
                    if (loader == null)
                        return null;
                    else
                        return loader.Load(br);
                }
            }
        }

        public static void Save(string filename, ISave character)
        {
            using (var stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (var bw = new BinaryWriter(stream))
                {
                    ICharacterLoader loader = GetForVersion(character.Version);
                    loader.Save(character, bw);
                }
            }
        }

        public static void Convert(string src, string dst, string version)
        {
            ISave save = Load(src);
            if (save.Version != version)
            {
                ICharacterLoader loader = GetForVersion(version);
                save = loader.Convert(save);
            }
            Save(dst, save);
        }

        public static ICharacterLoader GetForVersion(int version)
        {
            switch (version)
            {
                case 72:
                    return GetForVersion(SaveConverter.Save.v0_675a);
                default:
                    return null;
            }
        }

        public static ICharacterLoader GetForVersion(string version)
        {
            if (version == SaveConverter.Save.v0_675a)
                return new Vanilla.CharacterLoader_v0_675a();
            else if (version == SaveConverter.Save.m0_675a)
                return new ModLoader.CharacterLoader_v0_675a();
            return new NullCharacterLoader();
        }
    }

    namespace Vanilla
    {
        public class CharacterLoader_v0_675a : CharacterLoaderBase<Vanilla.Character_v0_675a>
        {
            public override ISave Load(BinaryReader br)
            {
                var c = new Character_v0_675a();
                br.ReadInt32();
                c.hatId = br.ReadInt32();
                c.facegearId = br.ReadInt32();
                c.bodyType = br.ReadChar();
                c.hairdoId = br.ReadInt32();
                c.bufferWeaponId = br.ReadInt32();
                c.shieldId = br.ReadInt32();
                c.armorId = br.ReadInt32();
                c.shoesId = br.ReadInt32();
                c.accessoryA = br.ReadInt32();
                c.accessoryB = br.ReadInt32();
                c.hatStyleId = br.ReadInt32();
                c.facegearStyleId = br.ReadInt32();
                c.weaponStyleId = br.ReadInt32();
                c.shieldStyleId = br.ReadInt32();
                c.hideHat = br.ReadBoolean();
                c.hideFacegear = br.ReadBoolean();
                c.lastOneHand = br.ReadInt32();
                c.lastTwoHand = br.ReadInt32();
                c.lastBow = br.ReadInt32();
                for (var i = 0; i < c.quickslots.Length; ++i)
                {
                    c.quickslots[i] = new Character_v0_675a.Quickslot();
                    var type = br.ReadByte();
                    if (type == 0)
                        c.quickslots[i].type = Character_v0_675a.Quickslot.Type.Null;
                    else if (type == 1)
                    {
                        c.quickslots[i].type = Character_v0_675a.Quickslot.Type.Item;
                        c.quickslots[i].itemId = br.ReadInt32();
                    }
                    else
                    {
                        c.quickslots[i].type = Character_v0_675a.Quickslot.Type.Spell;
                        c.quickslots[i].spellId = br.ReadUInt16();
                    }
                }
                c.hairColor = br.ReadByte();
                c.skinColor = br.ReadByte();
                c.ponchoColor = br.ReadByte();
                c.shirtColor = br.ReadByte();
                c.pantColor = br.ReadByte();
                c.isMale = br.ReadBoolean();
                c.networkNickname = br.ReadString();
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.InventoryItem();
                    c.inventory.Add(item);
                    item.id = br.ReadInt32();
                    item.amount = br.ReadInt32();
                    item.pickupNumber = br.ReadUInt32();
                }
                c.pickupNumberPool = br.ReadInt32();
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.SoldItem();
                    c.soldItems.Add(item);
                    item.id = br.ReadInt32();
                    item.value = br.ReadInt32();
                }
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                    c.cardAlbum.Add(br.ReadInt32());
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                    c.treasureMaps.Add(br.ReadUInt16());
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                    c.foundTreasures.Add(br.ReadUInt16());
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.Skill();
                    c.skills.Add(item);
                    item.id = br.ReadUInt16();
                    item.value = br.ReadByte();
                }
                c.level = br.ReadUInt16();
                c.exp = br.ReadUInt32();
                c.expForNext = br.ReadUInt32();
                c.expForPrevious = br.ReadUInt32();
                c.talentPoints = br.ReadUInt16();
                c.skillPointsSilver = br.ReadUInt16();
                c.skillPointsGold = br.ReadUInt16();
                c.money = br.ReadInt32();
                for (var i = br.ReadByte() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.OwnedPet();
                    c.ownedPets.Add(item);
                    item.id = br.ReadInt32();
                    item.npcType = br.ReadInt32();
                    item.name = br.ReadString();
                    item.level = br.ReadByte();
                    item.skin = br.ReadByte();
                    item.statLevelHp = br.ReadUInt16();
                    item.statLevelSp = br.ReadUInt16();
                    item.statLevelDamage = br.ReadUInt16();
                    item.statLevelCrit = br.ReadUInt16();
                    item.statLevelSpeed = br.ReadUInt16();
                    item.statLevelProgressHp = br.ReadUInt16();
                    item.statLevelProgressSp = br.ReadUInt16();
                    item.statLevelProgressDamage = br.ReadUInt16();
                    item.statLevelProgressCrit = br.ReadUInt16();
                    item.statLevelProgressSpeed = br.ReadUInt16();
                }
                c.activePetNpcType = br.ReadInt32();
                c.hidePet = br.ReadBoolean();
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.questsCompletedOnThisCharacter.Add(br.ReadUInt16());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.knownEnemies.Add(br.ReadInt32());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.ChallengeRecord();
                    c.challengeRecords.Add(item);
                    item.id = br.ReadUInt16();
                    item.totalGrade = br.ReadByte();
                    for (var j = br.ReadByte() - 1; j >= 0; --j)
                    {
                        var item2 = new Vanilla.Character_v0_675a.SubGrade();
                        item.subGrades.Add(item2);
                        item2.gradeType = br.ReadByte();
                        item2.gradeValue = br.ReadByte();
                        item2.value = br.ReadInt32();
                    }
                }
                c.arrowGameHighscore = br.ReadInt32();
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.awardedTrophies.Add(br.ReadUInt16());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.uniqueDiscoveredItems.Add(br.ReadInt32());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.uniqueCraftedItem.Add(br.ReadInt32());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.uniqueFishy.Add(br.ReadInt32());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.MonsterKilled();
                    c.monsterKilled.Add(item);
                    item.id = br.ReadInt32();
                    item.amount = br.ReadInt32();
                }
                c.birthdayMonth = br.ReadInt32();
                c.birthdayDay = br.ReadInt32();
                c.collectorId = br.ReadInt32();
                c.lastAutosaveAt = br.ReadUInt32();
                c.saveCarousel = br.ReadInt32();
                c.timePlayed = br.ReadUInt32();
                c.currentPhaseShiftShape = br.ReadByte();
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                {
                    var item = new KeyValuePair<string, float>(br.ReadString(), br.ReadSingle());
                    c.arbitraryPersistentCharacterValues.Add(item);
                }
                for (var i = br.ReadByte() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.LocalHousingConfiguration();
                    c.localHousingConfigurations.Add(item);
                    item.key = br.ReadByte();
                    item.value = br.ReadBytes((int)br.ReadUInt32());
                }
                return c;
            }

            protected override void Save(Vanilla.Character_v0_675a c, BinaryWriter bw)
            {
                bw.Write(72);
                bw.Write(c.hatId);
                bw.Write(c.facegearId);
                bw.Write(c.bodyType);
                bw.Write(c.hairdoId);
                bw.Write(c.bufferWeaponId);
                bw.Write(c.shieldId);
                bw.Write(c.armorId);
                bw.Write(c.shoesId);
                bw.Write(c.accessoryA);
                bw.Write(c.accessoryB);
                bw.Write(c.hatStyleId);
                bw.Write(c.facegearStyleId);
                bw.Write(c.weaponStyleId);
                bw.Write(c.shieldStyleId);
                bw.Write(c.hideHat);
                bw.Write(c.hideFacegear);
                bw.Write(c.lastOneHand);
                bw.Write(c.lastTwoHand);
                bw.Write(c.lastBow);
                foreach (var quickslot in c.quickslots)
                {
                    if (quickslot.type == Character_v0_675a.Quickslot.Type.Null)
                        bw.Write((byte)0);
                    else if (quickslot.type == Character_v0_675a.Quickslot.Type.Item)
                    {
                        bw.Write((byte)1);
                        bw.Write(quickslot.itemId);
                    }
                    else
                    {
                        bw.Write((byte)2);
                        bw.Write(quickslot.spellId);
                    }
                }
                bw.Write(c.hairColor);
                bw.Write(c.skinColor);
                bw.Write(c.ponchoColor);
                bw.Write(c.shirtColor);
                bw.Write(c.pantColor);
                bw.Write(c.isMale);
                bw.Write(c.networkNickname);
                bw.Write(c.inventory.Count);
                foreach (var item in c.inventory)
                {
                    bw.Write(item.id);
                    bw.Write(item.amount);
                    bw.Write(item.pickupNumber);
                }
                bw.Write(c.pickupNumberPool);
                bw.Write(c.soldItems.Count);
                foreach (var item in c.soldItems)
                {
                    bw.Write(item.id);
                    bw.Write(item.value);
                }
                bw.Write(c.cardAlbum.Count);
                foreach (var id in c.cardAlbum)
                    bw.Write(id);
                bw.Write(c.treasureMaps.Count);
                foreach (var id in c.treasureMaps)
                    bw.Write(id);
                bw.Write(c.foundTreasures.Count);
                foreach (var id in c.foundTreasures)
                    bw.Write(id);
                bw.Write(c.skills.Count);
                foreach (var skill in c.skills)
                {
                    bw.Write(skill.id);
                    bw.Write(skill.value);
                }
                bw.Write(c.level);
                bw.Write(c.exp);
                bw.Write(c.expForNext);
                bw.Write(c.expForPrevious);
                bw.Write(c.talentPoints);
                bw.Write(c.skillPointsSilver);
                bw.Write(c.skillPointsGold);
                bw.Write(c.money);
                bw.Write((byte)c.ownedPets.Count);
                foreach (var ownedPet in c.ownedPets)
                {
                    bw.Write(ownedPet.id);
                    bw.Write(ownedPet.npcType);
                    bw.Write(ownedPet.name);
                    bw.Write(ownedPet.level);
                    bw.Write(ownedPet.skin);
                    bw.Write(ownedPet.statLevelHp);
                    bw.Write(ownedPet.statLevelSp);
                    bw.Write(ownedPet.statLevelDamage);
                    bw.Write(ownedPet.statLevelCrit);
                    bw.Write(ownedPet.statLevelSpeed);
                    bw.Write(ownedPet.statLevelProgressHp);
                    bw.Write(ownedPet.statLevelProgressSp);
                    bw.Write(ownedPet.statLevelProgressDamage);
                    bw.Write(ownedPet.statLevelProgressCrit);
                    bw.Write(ownedPet.statLevelProgressSpeed);
                }
                bw.Write(c.activePetNpcType);
                bw.Write(c.hidePet);
                bw.Write((ushort)c.questsCompletedOnThisCharacter.Count);
                foreach (var id in c.questsCompletedOnThisCharacter)
                    bw.Write(id);
                bw.Write((ushort)c.knownEnemies.Count);
                foreach (var id in c.knownEnemies)
                    bw.Write(id);
                bw.Write((ushort)c.challengeRecords.Count);
                foreach (var challengeRecord in c.challengeRecords)
                {
                    bw.Write(challengeRecord.id);
                    bw.Write(challengeRecord.totalGrade);
                    bw.Write((byte)challengeRecord.subGrades.Count);
                    foreach (var subGrade in challengeRecord.subGrades)
                    {
                        bw.Write(subGrade.gradeType);
                        bw.Write(subGrade.gradeValue);
                        bw.Write(subGrade.value);
                    }
                }
                bw.Write(c.arrowGameHighscore);
                bw.Write((ushort)c.awardedTrophies.Count);
                foreach (var id in c.awardedTrophies)
                    bw.Write(id);
                bw.Write((ushort)c.uniqueDiscoveredItems.Count);
                foreach (var id in c.uniqueDiscoveredItems)
                    bw.Write(id);
                bw.Write((ushort)c.uniqueCraftedItem.Count);
                foreach (var id in c.uniqueCraftedItem)
                    bw.Write(id);
                bw.Write((ushort)c.uniqueFishy.Count);
                foreach (var id in c.uniqueFishy)
                    bw.Write(id);
                bw.Write((ushort)c.monsterKilled.Count);
                foreach (var mk in c.monsterKilled)
                {
                    bw.Write(mk.id);
                    bw.Write(mk.amount);
                }
                bw.Write(c.birthdayMonth);
                bw.Write(c.birthdayDay);
                bw.Write(c.collectorId);
                bw.Write(c.lastAutosaveAt);
                bw.Write(c.saveCarousel);
                bw.Write(c.timePlayed);
                bw.Write(c.currentPhaseShiftShape);
                bw.Write((ushort)c.arbitraryPersistentCharacterValues.Count);
                foreach (var pv in c.arbitraryPersistentCharacterValues)
                {
                    bw.Write(pv.Key);
                    bw.Write(pv.Value);
                }
                bw.Write((byte)c.localHousingConfigurations.Count);
                foreach (var v in c.localHousingConfigurations)
                {
                    bw.Write(v.key);
                    bw.Write((uint)v.value.Length);
                    bw.Write(v.value, 0, v.value.Length);
                }
            }
        }
    }

    namespace ModLoader
    {
        public class CharacterLoader_v0_675a : CharacterLoaderBase<ModLoader.Character_m0_675a>
        {
            private static ModId<byte> ReadModIdByte(BinaryReader br)
            {
                return new ModId<byte>(br.ReadInt32(), br.ReadByte());
            }

            public override ISave Load(BinaryReader br)
            {
                var c = new ModLoader.Character_m0_675a();
                br.ReadInt32();
                c.hatId = br.ReadInt32();
                c.facegearId = br.ReadInt32();
                c.bodyType = br.ReadChar();
                c.hairdoId = br.ReadInt32();
                c.bufferWeaponId = br.ReadInt32();
                c.shieldId = br.ReadInt32();
                c.armorId = br.ReadInt32();
                c.shoesId = br.ReadInt32();
                c.accessoryA = br.ReadInt32();
                c.accessoryB = br.ReadInt32();
                c.hatStyleId = br.ReadInt32();
                c.facegearStyleId = br.ReadInt32();
                c.weaponStyleId = br.ReadInt32();
                c.shieldStyleId = br.ReadInt32();
                c.hideHat = br.ReadBoolean();
                c.hideFacegear = br.ReadBoolean();
                c.lastOneHand = br.ReadInt32();
                c.lastTwoHand = br.ReadInt32();
                c.lastBow = br.ReadInt32();
                for (var i = 0; i < c.quickslots.Length; ++i)
                {
                    c.quickslots[i] = new Character_v0_675a.Quickslot();
                    var type = br.ReadByte();
                    if (type == 0)
                        c.quickslots[i].type = Character_v0_675a.Quickslot.Type.Null;
                    else if (type == 1)
                    {
                        c.quickslots[i].type = Character_v0_675a.Quickslot.Type.Item;
                        c.quickslots[i].itemId = br.ReadInt32();
                    }
                    else
                    {
                        c.quickslots[i].type = Character_v0_675a.Quickslot.Type.Spell;
                        c.quickslots[i].spellId = br.ReadUInt16();
                    }
                }
                c.hairColor = ReadModIdByte(br);
                c.skinColor = ReadModIdByte(br);
                c.ponchoColor = ReadModIdByte(br);
                c.shirtColor = ReadModIdByte(br);
                c.pantColor = ReadModIdByte(br);
                c.isMale = br.ReadBoolean();
                c.networkNickname = br.ReadString();
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.InventoryItem();
                    c.inventory.Add(item);
                    item.id = br.ReadInt32();
                    item.amount = br.ReadInt32();
                    item.pickupNumber = br.ReadUInt32();
                }
                c.pickupNumberPool = br.ReadInt32();
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.SoldItem();
                    c.soldItems.Add(item);
                    item.id = br.ReadInt32();
                    item.value = br.ReadInt32();
                }
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                    c.cardAlbum.Add(br.ReadInt32());
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                    c.treasureMaps.Add(br.ReadUInt16());
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                    c.foundTreasures.Add(br.ReadUInt16());
                for (var i = br.ReadInt32() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.Skill();
                    c.skills.Add(item);
                    item.id = br.ReadUInt16();
                    item.value = br.ReadByte();
                }
                c.level = br.ReadUInt16();
                c.exp = br.ReadUInt32();
                c.expForNext = br.ReadUInt32();
                c.expForPrevious = br.ReadUInt32();
                c.talentPoints = br.ReadUInt16();
                c.skillPointsSilver = br.ReadUInt16();
                c.skillPointsGold = br.ReadUInt16();
                c.money = br.ReadInt32();
                for (var i = br.ReadByte() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.OwnedPet();
                    c.ownedPets.Add(item);
                    item.id = br.ReadInt32();
                    item.npcType = br.ReadInt32();
                    item.name = br.ReadString();
                    item.level = br.ReadByte();
                    item.skin = br.ReadByte();
                    item.statLevelHp = br.ReadUInt16();
                    item.statLevelSp = br.ReadUInt16();
                    item.statLevelDamage = br.ReadUInt16();
                    item.statLevelCrit = br.ReadUInt16();
                    item.statLevelSpeed = br.ReadUInt16();
                    item.statLevelProgressHp = br.ReadUInt16();
                    item.statLevelProgressSp = br.ReadUInt16();
                    item.statLevelProgressDamage = br.ReadUInt16();
                    item.statLevelProgressCrit = br.ReadUInt16();
                    item.statLevelProgressSpeed = br.ReadUInt16();
                }
                c.activePetNpcType = br.ReadInt32();
                c.hidePet = br.ReadBoolean();
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.questsCompletedOnThisCharacter.Add(br.ReadUInt16());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.knownEnemies.Add(br.ReadInt32());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.ChallengeRecord();
                    c.challengeRecords.Add(item);
                    item.id = br.ReadUInt16();
                    item.totalGrade = br.ReadByte();
                    for (var j = br.ReadByte() - 1; j >= 0; --j)
                    {
                        var item2 = new Vanilla.Character_v0_675a.SubGrade();
                        item.subGrades.Add(item2);
                        item2.gradeType = br.ReadByte();
                        item2.gradeValue = br.ReadByte();
                        item2.value = br.ReadInt32();
                    }
                }
                c.arrowGameHighscore = br.ReadInt32();
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.awardedTrophies.Add(br.ReadUInt16());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.uniqueDiscoveredItems.Add(br.ReadInt32());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.uniqueCraftedItem.Add(br.ReadInt32());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                    c.uniqueFishy.Add(br.ReadInt32());
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.MonsterKilled();
                    c.monsterKilled.Add(item);
                    item.id = br.ReadInt32();
                    item.amount = br.ReadInt32();
                }
                c.birthdayMonth = br.ReadInt32();
                c.birthdayDay = br.ReadInt32();
                c.collectorId = br.ReadInt32();
                c.lastAutosaveAt = br.ReadUInt32();
                c.saveCarousel = br.ReadInt32();
                c.timePlayed = br.ReadUInt32();
                c.currentPhaseShiftShape = br.ReadByte();
                for (var i = br.ReadUInt16() - 1; i >= 0; --i)
                {
                    var item = new KeyValuePair<string, float>(br.ReadString(), br.ReadSingle());
                    c.arbitraryPersistentCharacterValues.Add(item);
                }
                for (var i = br.ReadByte() - 1; i >= 0; --i)
                {
                    var item = new Vanilla.Character_v0_675a.LocalHousingConfiguration();
                    c.localHousingConfigurations.Add(item);
                    item.key = br.ReadByte();
                    item.value = br.ReadBytes((int)br.ReadUInt32());
                }
                return c;
            }

            private static void WriteModId(BinaryWriter bw, ModId<byte> id)
            {
                bw.Write(id.Mod);
                bw.Write(id.Value);
            }

            protected override void Save(ModLoader.Character_m0_675a c, BinaryWriter bw)
            {
                bw.Write(new char[] { 'M', 'O', 'D' });
                bw.Write(SaveConverter.Save.m0_675a);
                bw.Write(72);
                bw.Write(c.hatId);
                bw.Write(c.facegearId);
                bw.Write(c.bodyType);
                bw.Write(c.hairdoId);
                bw.Write(c.bufferWeaponId);
                bw.Write(c.shieldId);
                bw.Write(c.armorId);
                bw.Write(c.shoesId);
                bw.Write(c.accessoryA);
                bw.Write(c.accessoryB);
                bw.Write(c.hatStyleId);
                bw.Write(c.facegearStyleId);
                bw.Write(c.weaponStyleId);
                bw.Write(c.shieldStyleId);
                bw.Write(c.hideHat);
                bw.Write(c.hideFacegear);
                bw.Write(c.lastOneHand);
                bw.Write(c.lastTwoHand);
                bw.Write(c.lastBow);
                foreach (var quickslot in c.quickslots)
                {
                    if (quickslot.type == Vanilla.Character_v0_675a.Quickslot.Type.Null)
                        bw.Write((byte)0);
                    else if (quickslot.type == Vanilla.Character_v0_675a.Quickslot.Type.Item)
                    {
                        bw.Write((byte)1);
                        bw.Write(quickslot.itemId);
                    }
                    else
                    {
                        bw.Write((byte)2);
                        bw.Write(quickslot.spellId);
                    }
                }
                WriteModId(bw, c.hairColor);
                WriteModId(bw, c.skinColor);
                WriteModId(bw, c.ponchoColor);
                WriteModId(bw, c.shirtColor);
                WriteModId(bw, c.pantColor);
                bw.Write(c.isMale);
                bw.Write(c.networkNickname);
                bw.Write(c.inventory.Count);
                foreach (var item in c.inventory)
                {
                    bw.Write(item.id);
                    bw.Write(item.amount);
                    bw.Write(item.pickupNumber);
                }
                bw.Write(c.pickupNumberPool);
                bw.Write(c.soldItems.Count);
                foreach (var item in c.soldItems)
                {
                    bw.Write(item.id);
                    bw.Write(item.value);
                }
                bw.Write(c.cardAlbum.Count);
                foreach (var id in c.cardAlbum)
                    bw.Write(id);
                bw.Write(c.treasureMaps.Count);
                foreach (var id in c.treasureMaps)
                    bw.Write(id);
                bw.Write(c.foundTreasures.Count);
                foreach (var id in c.foundTreasures)
                    bw.Write(id);
                bw.Write(c.skills.Count);
                foreach (var skill in c.skills)
                {
                    bw.Write(skill.id);
                    bw.Write(skill.value);
                }
                bw.Write(c.level);
                bw.Write(c.exp);
                bw.Write(c.expForNext);
                bw.Write(c.expForPrevious);
                bw.Write(c.talentPoints);
                bw.Write(c.skillPointsSilver);
                bw.Write(c.skillPointsGold);
                bw.Write(c.money);
                bw.Write((byte)c.ownedPets.Count);
                foreach (var ownedPet in c.ownedPets)
                {
                    bw.Write(ownedPet.id);
                    bw.Write(ownedPet.npcType);
                    bw.Write(ownedPet.name);
                    bw.Write(ownedPet.level);
                    bw.Write(ownedPet.skin);
                    bw.Write(ownedPet.statLevelHp);
                    bw.Write(ownedPet.statLevelSp);
                    bw.Write(ownedPet.statLevelDamage);
                    bw.Write(ownedPet.statLevelCrit);
                    bw.Write(ownedPet.statLevelSpeed);
                    bw.Write(ownedPet.statLevelProgressHp);
                    bw.Write(ownedPet.statLevelProgressSp);
                    bw.Write(ownedPet.statLevelProgressDamage);
                    bw.Write(ownedPet.statLevelProgressCrit);
                    bw.Write(ownedPet.statLevelProgressSpeed);
                }
                bw.Write(c.activePetNpcType);
                bw.Write(c.hidePet);
                bw.Write((ushort)c.questsCompletedOnThisCharacter.Count);
                foreach (var id in c.questsCompletedOnThisCharacter)
                    bw.Write(id);
                bw.Write((ushort)c.knownEnemies.Count);
                foreach (var id in c.knownEnemies)
                    bw.Write(id);
                bw.Write((ushort)c.challengeRecords.Count);
                foreach (var challengeRecord in c.challengeRecords)
                {
                    bw.Write(challengeRecord.id);
                    bw.Write(challengeRecord.totalGrade);
                    bw.Write((byte)challengeRecord.subGrades.Count);
                    foreach (var subGrade in challengeRecord.subGrades)
                    {
                        bw.Write(subGrade.gradeType);
                        bw.Write(subGrade.gradeValue);
                        bw.Write(subGrade.value);
                    }
                }
                bw.Write(c.arrowGameHighscore);
                bw.Write((ushort)c.awardedTrophies.Count);
                foreach (var id in c.awardedTrophies)
                    bw.Write(id);
                bw.Write((ushort)c.uniqueDiscoveredItems.Count);
                foreach (var id in c.uniqueDiscoveredItems)
                    bw.Write(id);
                bw.Write((ushort)c.uniqueCraftedItem.Count);
                foreach (var id in c.uniqueCraftedItem)
                    bw.Write(id);
                bw.Write((ushort)c.uniqueFishy.Count);
                foreach (var id in c.uniqueFishy)
                    bw.Write(id);
                bw.Write((ushort)c.monsterKilled.Count);
                foreach (var mk in c.monsterKilled)
                {
                    bw.Write(mk.id);
                    bw.Write(mk.amount);
                }
                bw.Write(c.birthdayMonth);
                bw.Write(c.birthdayDay);
                bw.Write(c.collectorId);
                bw.Write(c.lastAutosaveAt);
                bw.Write(c.saveCarousel);
                bw.Write(c.timePlayed);
                bw.Write(c.currentPhaseShiftShape);
                bw.Write((ushort)c.arbitraryPersistentCharacterValues.Count);
                foreach (var pv in c.arbitraryPersistentCharacterValues)
                {
                    bw.Write(pv.Key);
                    bw.Write(pv.Value);
                }
                bw.Write((byte)c.localHousingConfigurations.Count);
                foreach (var v in c.localHousingConfigurations)
                {
                    bw.Write(v.key);
                    bw.Write((uint)v.value.Length);
                    bw.Write(v.value, 0, v.value.Length);
                }
            }
        }
    }
}
