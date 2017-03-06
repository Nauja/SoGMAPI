using System;
using System.IO;

namespace SoG.ModLoader.SaveConverter
{
    public abstract class CharacterLoader
    {
        public class NullCharacterLoader : CharacterLoader
        {
        }

        public static void Load(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                using (var br = new BinaryReader(stream))
                {
                    var bytes = br.ReadBytes(3);
                    if (bytes[0] == 'M' && bytes[1] == 'O' && bytes[2] == 'D')

                    //br.BaseStream.se
                }
            }
        }

        public static CharacterLoader GetForVersion(string version)
        {
            if (version == SaveConverter.Save.v0_675a)
                return new Vanilla.CharacterLoader_v0_675a();
            else if (version == SaveConverter.Save.m0_675a)
                return new ModLoader.CharacterLoader_v0_675a();
            return new NullCharacterLoader();
        }

        public abstract ISave Load(BinaryReader br);

        public abstract void Save(Vanilla.Character_v0_675a character, BinaryWriter bw);
    }

    namespace Vanilla
    {
        public class CharacterLoader_v0_675a : CharacterLoader
        {
            public override ISave Load(BinaryReader br)
            {
                var c = new Character_v0_675a();
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
                return c;
            }

            public override void Save(Vanilla.Character_v0_675a c, BinaryWriter bw)
            {
                bw.Write(69);
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
            }
        }
    }

    namespace ModLoader
    {
        public class CharacterLoader_v0_675a : CharacterLoader
        {
            public override ISave Load(BinaryReader br)
            {
                throw new NotImplementedException();
            }

            public override void Save(Character_v0_675a character, BinaryWriter bw)
            {
                throw new NotImplementedException();
            }
        }
    }
}
