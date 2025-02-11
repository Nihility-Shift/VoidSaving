using System;

namespace VoidSaving.ReadWriteTools
{
    //Stolen from zORg Alex, https://stackoverflow.com/a/67604192
    public static class RandomSerializer
    {
        //* Used for Getting and setting System.Random state *//
        private static System.Reflection.FieldInfo[] randomFields;
        public static System.Reflection.FieldInfo[] RandomFields
        {
            get
            {
                if (randomFields == null)
                {
                    randomFields = new System.Reflection.FieldInfo[3];
                    var t = typeof(System.Random);
                    randomFields[0] = t.GetField("_seedArray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    randomFields[1] = t.GetField("_inext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    randomFields[2] = t.GetField("_inextp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                }
                return randomFields;
            }
        }
        /// <summary>
        /// Gets <see cref="System.Random"/> current state array and indexes with Reflection.
        /// </summary>
        /// <param name="rand"></param>
        /// <returns></returns>
        public static int[] GetSeedArray(this System.Random rand)
        {
            var state = new int[58];
            ((int[])RandomFields[0].GetValue(rand)).CopyTo(state, 0);
            state[56] = (int)RandomFields[1].GetValue(rand);
            state[57] = (int)RandomFields[2].GetValue(rand);
            return state;
        }

        /// <summary>
        /// Restores saved <see cref="System.Random"/> state and indexes with Reflection. Use with caution.
        /// </summary>
        /// <param name="rand"></param>
        /// <param name="seedArray"></param>
        public static void SetSeedArray(this System.Random rand, int[] seedArray)
        {
            if (seedArray.Length != 56 + 2) return;

            Array.Copy(seedArray, ((int[])RandomFields[0].GetValue(rand)), 56);
            RandomFields[1].SetValue(rand, seedArray[56]);
            RandomFields[2].SetValue(rand, seedArray[57]);
        }

    }
}
