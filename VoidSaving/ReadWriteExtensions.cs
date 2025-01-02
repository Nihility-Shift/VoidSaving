using Newtonsoft.Json.Linq;
using System;
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
            foreach(GUIDUnion union in unions)
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
            int[] unions = new int[count];
            for (int i = 0; i < count; i++)
            {
                unions[i] = Reader.ReadInt32();
            }
            return unions;
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
    }
}
