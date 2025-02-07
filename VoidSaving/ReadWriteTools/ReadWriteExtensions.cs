using CG.Game.Scenarios;
using CG.Ship.Modules;
using Gameplay.Atmosphere;
using Gameplay.Enhancements;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

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


        public static void WriteByteArray(this BinaryWriter Writer, byte[] bytes)
        {
            Writer.Write(bytes.Length);
            Writer.Write(bytes);
        }

        public static byte[] ReadByteArray(this BinaryReader Reader)
        {
            return Reader.ReadBytes(Reader.ReadInt32());
        }


        public static void WriteSByteArray(this BinaryWriter Writer, sbyte[] bytes)
        {
            int length = bytes.Length;
            Writer.Write(bytes.Length);
            for (int i = 0; i < length; i++)
            {
                Writer.Write(bytes[i]);
            }
        }

        public static sbyte[] ReadSByteArray(this BinaryReader Reader)
        {
            int length = Reader.ReadInt32();
            sbyte[] bytes = new sbyte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = Reader.ReadSByte();
            }
            return bytes;
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
                floats.Add(Reader.ReadSingle());
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
                Writer.Write(Enhancement.ParentModuleID);
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
                    ActivationTimeStart = reader.ReadInt32(),
                    ActivationTimeEnd = reader.ReadInt32(),
                    CooldownTimeStart = reader.ReadInt32(),
                    CooldownTimeEnd = reader.ReadInt32(),
                    FailureTimeStart = reader.ReadInt32(),
                    FailureTimeEnd = reader.ReadInt32(),
                    LastGrade = reader.ReadSingle(),
                    LastDurationMult = reader.ReadSingle(),
                    ParentModuleID = reader.ReadInt16(),
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


        public static void Write(this BinaryWriter Writer, AtmosphereValues[] Values)
        {
            Writer.Write(Values.Length);
            for (int i = 0; i < Values.Length; i++)
            {
                AtmosphereValues value = Values[i];
                Writer.Write(value.Pressure);
                Writer.Write(value.Oxygen);
                Writer.Write(value.Temperature);
            }
        }

        public static AtmosphereValues[] ReadAtmosphereValues(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            AtmosphereValues[] data = new AtmosphereValues[count];
            for (int i = 0; i < count; i++)
            {
                AtmosphereValues value = new AtmosphereValues();
                value.Pressure = reader.ReadSingle();
                value.Oxygen = reader.ReadSingle();
                value.Temperature = reader.ReadSingle();
                value.AtmosphericForce = default(AtmosphericForceProbe);
                data[i] = value;
            }

            return data;
        }

        public static void Write(this BinaryWriter Writer, List<SimpleSectorData> Values)
        {
            int count = Values.Count;
            Writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                Writer.Write(Values[i].SolarSystemIndex);
                Writer.Write(Values[i].SectorContainerGUID);
            }
        }

        public static List<SimpleSectorData> ReadSimpleSectorDatas(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<SimpleSectorData> values = new List<SimpleSectorData>(count);
            for (int i = 0; i < count; i++)
            {
                values.Add( new SimpleSectorData(reader.ReadInt32(), reader.ReadGUIDUnion()) );
            }
            return values;
        }


        public static void Write(this BinaryWriter Writer, FullSectorData[] sectorDatas)
        {
            Writer.Write(sectorDatas.Length);
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Writing {sectorDatas.Length} sector datas");
            foreach (FullSectorData sectorData in sectorDatas)
            {
                Writer.Write((byte)sectorData.SolarSystemIndex);
                Writer.Write(sectorData.SectorContainerGUID);
                Writer.Write(sectorData.ObjectiveGUID);
                Writer.Write((byte)sectorData.Difficulty);
                Writer.Write((byte)sectorData.State);
                Writer.Write(sectorData.IsMainObjective);
            }
        }

        public static FullSectorData[] ReadFullSectorDatas(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            FullSectorData[] sectors = new FullSectorData[length];
            if (VoidManager.BepinPlugin.Bindings.IsDebugMode) BepinPlugin.Log.LogInfo($"Reading {length} sector datas");
            for (int i = 0; i < length; i++)
            {
                sectors[i] = new FullSectorData()
                {
                    SolarSystemIndex = reader.ReadByte(),
                    SectorContainerGUID = reader.ReadGUIDUnion(),
                    ObjectiveGUID = reader.ReadGUIDUnion(),
                    Difficulty = (DifficultyModifier)reader.ReadByte(),
                    State = (ObjectiveState)reader.ReadByte(),
                    IsMainObjective = reader.ReadBoolean()
                };
            }

            return sectors;
        }

        
        public static void Write(this BinaryWriter Writer, SectionData sectionData)
        {
            Writer.Write(sectionData.ObjectiveSectors);
            Writer.Write(sectionData.SolarSystemIndex);
        }

        public static SectionData ReadSectionData(this BinaryReader Reader)
        {
            return new SectionData()
            {
                ObjectiveSectors = Reader.ReadFullSectorDatas(),
                SolarSystemIndex = Reader.ReadInt32(),
            };
        }


        public static void Write(this BinaryWriter Writer, SectionData[] sections)
        {
            int length = sections.Length;
            Writer.Write(length);
            for (int i = 0; i < sections.Length; i++)
            {
                Writer.Write(sections[i]);
            }
        }

        public static SectionData[] ReadSectionDatas(this BinaryReader Reader)
        {
            int length = Reader.ReadInt32();
            SectionData[] sections = new SectionData[length];
            for (int i = 0; i < length; i++)
            {
                sections[i] = Reader.ReadSectionData();
            }
            return sections;
        }
    }
}
