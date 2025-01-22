using CG.Game.Scenarios;
using CG.Ship.Modules;
using Gameplay.Quests;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VoidSaving
{
    public static class ReadWriteExtensions
    {
        public static void Write(this BinaryWriter Writer, GUIDUnion union)
        {
            foreach (int item in union.AsIntArray())
            {
                Writer.Write(item);
            }
        }

        public static GUIDUnion ReadGUIDUnion(this BinaryReader Reader)
        {
            int[] GUIDArray = new int[4];
            for (int i = 0; i < 4; i++)
            {
                GUIDArray[i] = Reader.ReadInt32();
            }
            return new GUIDUnion(GUIDArray);
        }


        public static void Write(this BinaryWriter Writer, GUIDUnion[] unions)
        {
            Writer.Write(unions.Length);
            foreach (GUIDUnion union in unions)
            {
                Writer.Write(union);
            }
        }

        public static GUIDUnion[] ReadGUIDUnionArray(this BinaryReader Reader)
        {
            int count = Reader.ReadInt32();
            GUIDUnion[] unions = new GUIDUnion[count];
            for (int i = 0; i < count; i++)
            {
                unions[i] = Reader.ReadGUIDUnion();
            }
            return unions;
        }


        public static void Write(this BinaryWriter Writer, int[] ints)
        {
            Writer.Write(ints.Length);
            foreach (int inty in ints)
            {
                Writer.Write(inty);
            }
        }

        public static int[] ReadInt32Array(this BinaryReader Reader)
        {
            int count = Reader.ReadInt32();
            int[] ints = new int[count];
            for (int i = 0; i < count; i++)
            {
                ints[i] = Reader.ReadInt32();
            }
            return ints;
        }


        public static void Write(this BinaryWriter Writer, bool[] bools)
        {
            Writer.Write(bools.Length);
            foreach (bool booly in bools)
            {
                Writer.Write(booly);
            }
        }

        public static bool[] ReadBooleanArray(this BinaryReader Reader)
        {
            int count = Reader.ReadInt32();
            bool[] bools = new bool[count];
            for (int i = 0; i < count; i++)
            {
                bools[i] = Reader.ReadBoolean();
            }
            return bools;
        }


        public static void Write(this BinaryWriter Writer, float[] floats)
        {
            Writer.Write(floats.Length);
            foreach (float floaty in floats)
            {
                Writer.Write(floaty);
            }
        }

        public static float[] ReadSingleArray(this BinaryReader Reader)
        {
            int count = Reader.ReadInt32();
            float[] floats = new float[count];
            for (int i = 0; i < count; i++)
            {
                floats[i] = Reader.ReadSingle();
            }
            return floats;
        }


        public static void Write(this BinaryWriter Writer, List<float> floats)
        {
            Writer.Write(floats.Count);
            foreach (float floaty in floats)
            {
                Writer.Write(floaty);
            }
        }

        public static List<float> ReadSingleList(this BinaryReader Reader)
        {
            int count = Reader.ReadInt32();
            List<float> floats = new List<float>(count);
            for (int i = 0; i < count; i++)
            {
                floats[i] = Reader.ReadSingle();
            }
            return floats;
        }


        public static void Write(this BinaryWriter Writer, JObject jobject)
        {
            //Converts JObject to string, then writes string, converting to byte[]. It's more preferable to convert the JObject directly to a byte[], but I'm not sure if that's possible
            Writer.Write(jobject.ToString(Newtonsoft.Json.Formatting.None));
        }

        public static JObject ReadJObject(this BinaryReader Reader)
        {
            return JObject.Parse(Reader.ReadString());
        }


        public static void Write(this BinaryWriter Writer, Random random)
        {
            Writer.Write(random.GetSeedArray());
        }

        public static Random ReadRandom(this BinaryReader reader)
        {
            Random returnValue = new();
            returnValue.SetSeedArray(reader.ReadInt32Array());
            return returnValue;
        }


        public static void Write(this BinaryWriter Writer, SectorData[] sectorDatas)
        {
            Writer.Write(sectorDatas.Length);
            foreach (SectorData sectorData in sectorDatas)
            {
                Writer.Write(sectorData.ObjectiveGUID);
                Writer.Write((byte)sectorData.Difficulty);
                Writer.Write((byte)sectorData.State);
            }
        }


        public static SectorData[] ReadSectors(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            SectorData[] sectors = new SectorData[length];

            for (int i = 0; i < length; i++)
            {
                sectors[i] = new SectorData()
                {
                    ObjectiveGUID = reader.ReadGUIDUnion(),
                    Difficulty = (DifficultyModifier)reader.ReadByte(),
                    State = (ObjectiveState)reader.ReadByte()
                };
            }

            return sectors;
        }


        public static void Write(this BinaryWriter Writer, BoosterStatus[] boosterStatuses)
        {
            Writer.Write(boosterStatuses.Length);
            foreach (BoosterStatus boosterStatus in boosterStatuses)
            {
                Writer.Write((byte)boosterStatus.BoosterState);
                Writer.Write(boosterStatus.CooldownTimer);
                Writer.Write(boosterStatus.ChargeTimer);
                Writer.Write(boosterStatus.DischargeTimer);
            }
        }

        public static BoosterStatus[] ReadBoosterStatuses(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            BoosterStatus[] BoosterStatuses = new BoosterStatus[length];

            for (int i = 0; i < length; i++)
            {
                BoosterStatuses[i] = new BoosterStatus()
                {
                    BoosterState = (ThrusterBoosterState)reader.ReadByte(),
                    CooldownTimer = reader.ReadSingle(),
                    ChargeTimer = reader.ReadSingle(),
                    DischargeTimer = reader.ReadSingle(),
                };
            }

            return BoosterStatuses;
        }


        public static void Write(this BinaryWriter Writer, WeaponBullets[] WeaponBullets)
        {
            Writer.Write(WeaponBullets.Length);
            foreach (WeaponBullets WeaponBullet in WeaponBullets)
            {
                Writer.Write(WeaponBullet.AmmoLoaded);
                Writer.Write(WeaponBullet.AmmoReservoir);
            }
        }

        public static WeaponBullets[] ReadWeaponBullets(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            WeaponBullets[] WeaponBullets = new WeaponBullets[length];

            for (int i = 0; i < length; i++)
            {
                WeaponBullets[i] = new WeaponBullets()
                {
                    AmmoLoaded = reader.ReadSingle(),
                    AmmoReservoir = reader.ReadSingle(),
                };
            }

            return WeaponBullets;
        }
    }
}
