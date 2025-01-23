using CG.Game.Scenarios;
using CG.Ship.Modules;
using Gameplay.Enhancements;
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


        public static void Write(this BinaryWriter Writer, EnhancementData[] Enhancements)
        {
            Writer.Write(Enhancements.Length);
            foreach (EnhancementData Enhancement in Enhancements)
            {
                Writer.Write((byte)Enhancement.state);
                Writer.Write(Enhancement.ActivationTimeStart);
                Writer.Write(Enhancement.ActivationTimeEnd);
                Writer.Write(Enhancement.CooldownTimeStart);
                Writer.Write(Enhancement.CooldownTimeEnd);
                Writer.Write(Enhancement.FailureTimeStart);
                Writer.Write(Enhancement.FailureTimeEnd);
                Writer.Write(Enhancement.LastGrade);
                Writer.Write(Enhancement.LastDurationMult);
            }
        }

        public static EnhancementData[] ReadEnhancements(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            EnhancementData[] Enhancements = new EnhancementData[length];

            for (int i = 0; i < length; i++)
            {
                Enhancements[i] = new EnhancementData()
                {
                    state = (EnhancementState)reader.ReadByte(),
                    ActivationTimeStart = reader.ReadSingle(),
                    ActivationTimeEnd = reader.ReadSingle(),
                    CooldownTimeStart = reader.ReadSingle(),
                    CooldownTimeEnd = reader.ReadSingle(),
                    FailureTimeStart = reader.ReadSingle(),
                    FailureTimeEnd = reader.ReadSingle(),
                    LastGrade = reader.ReadSingle(),
                    LastDurationMult = reader.ReadSingle(),
                };
            }

            return Enhancements;
        }
        

        public static void Write(this BinaryWriter Writer, CircuitBreakerData Breakers)
        {
            Writer.Write(Breakers.breakers);
            Writer.Write(Breakers.NextBreakTemperature);
            Writer.Write(Breakers.currentTemperature);
        }

        public static CircuitBreakerData ReadBreakers(this BinaryReader reader)
        {
            CircuitBreakerData data = new CircuitBreakerData();
            data.breakers = reader.ReadBooleanArray();
            data.NextBreakTemperature = reader.ReadSingle();
            data.currentTemperature = reader.ReadSingle();

            return data;
        }


        public static void Write(this BinaryWriter Writer, GameSessionStatistics Stats)
        {
            Writer.Write(Stats.TotalEnemiesKilled);
            Writer.Write(Stats.PlayerDeaths);
            Writer.Write(Stats.TotalDamageInflicted);
            Writer.Write(Stats.TotalShipDamageTaken);
            Writer.Write(Stats.TotalAlloysCollected);
            Writer.Write(Stats.TotalBiomassCollected);

            //To save and load timespan properly, store and load via total hours.
            TimeSpan timeSpan = DateTime.Now.Subtract(Stats.QuestStartTime);
            Writer.Write(timeSpan.TotalHours);
        }

        public static GameSessionStatistics ReadSessionStats(this BinaryReader reader)
        {
            GameSessionStatistics data = new GameSessionStatistics();
            data.TotalEnemiesKilled = reader.ReadInt32();
            data.PlayerDeaths = reader.ReadInt32();
            data.TotalDamageInflicted = reader.ReadInt64();
            data.TotalShipDamageTaken = reader.ReadInt64();
            data.TotalAlloysCollected = reader.ReadInt32();
            data.TotalBiomassCollected = reader.ReadInt32();

            TimeSpan elapsedHours = TimeSpan.FromHours(reader.ReadDouble());
            data.QuestStartTime = DateTime.Now.Subtract(elapsedHours);

            return data;
        }


        public static void Write(this BinaryWriter Writer, VoidDriveModuleData VoidDriveData)
        {
            Writer.Write(VoidDriveData.engineChargedStates);
            Writer.Write(VoidDriveData.JumpCharge);
        }

        public static VoidDriveModuleData ReadVoidDriveData(this BinaryReader reader)
        {
            VoidDriveModuleData data = new VoidDriveModuleData();
            data.engineChargedStates = reader.ReadBooleanArray();
            data.JumpCharge = reader.ReadSingle();

            return data;
        }
    }
}
